using PincabToolbox.Core.Models;
using PincabToolbox.Repair.Actions;
using PincabToolbox.Repair.Licensing;

namespace PincabToolbox.Repair;

/// <summary>
/// LOT H (spec 10/08) — the write-path orchestrator ("Écran 2"). Composes the engine exactly the
/// way <c>RepairOfferBuilder</c> (App, Écran 1) already does for the free preview, with the two
/// differences that make writing safe:
///
/// <list type="bullet">
/// <item><b>A persistent journal</b> (<see cref="FileRepairJournal"/>, H.1) instead of the
/// throw-away <c>InMemoryRepairJournal</c> Écran 1 uses for <c>Plan()</c> only — Undo must survive
/// closing the app.</item>
/// <item><b>A license actually verified here</b> (<see cref="VerifyLicense"/>), never assumed —
/// <c>Plan(..., licensed: true)</c> is only ever called with a result this class itself checked.</item>
/// </list>
///
/// <para>
/// Deliberately lives in <c>PincabToolbox.Repair</c> — net8.0, cross-platform, fully unit-testable —
/// rather than in <c>PincabToolbox.App</c> (net8.0-windows/WPF). The App project cannot be compiled
/// or exercised by an automated test in every environment this codebase is worked from (WPF's
/// Windows Desktop SDK is Windows-only); pushing every decision this class makes down into a project
/// that CAN be built and tested everywhere keeps the riskiest code in this project's history — the
/// first real write path — inside the part of the safety net that is always on. The App-side caller
/// is intentionally thin: XAML + event handlers that call into this class and render its results,
/// nothing that decides whether or what to write.
/// </para>
/// </summary>
public sealed class RepairSession
{
    private readonly IRepairEngine _engine;
    private readonly FileRepairJournal _journal;
    private readonly ILicenseVerifier _licenseVerifier;
    private readonly bool _forceDryRun;

    /// <summary>
    /// <c>%APPDATA%\PincabToolbox</c> — the shared root under which the journal and backups both
    /// live, matching the location <c>RepairOfferBuilder</c> already uses for backups. A plain BCL
    /// call, safe to make from this cross-platform project; it only resolves meaningfully on
    /// Windows, which is the only OS the shipped App ever runs on.
    /// </summary>
    public static string DefaultAppDataRoot() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PincabToolbox");

    /// <summary>
    /// Kill switch for a first field test on real hardware, added 11/08/2026 the same day the real
    /// license key was embedded (see ADR-012's "Suite" section) — a way to exercise the whole Repair
    /// UI end-to-end against a real license without trusting that every code path behind it has
    /// already been run on Windows. Reads <c>PINCAB_REPAIR_FORCE_DRYRUN</c> (any of "1"/"true"/"yes",
    /// case-insensitive); unset or anything else means normal behavior. Checked once per session,
    /// same posture as the rest of this class: never assumed, read fresh.
    /// </summary>
    public static bool IsForceDryRunRequestedByEnvironment()
    {
        var raw = Environment.GetEnvironmentVariable("PINCAB_REPAIR_FORCE_DRYRUN");
        return raw is not null &&
               (raw.Equals("1", StringComparison.Ordinal) ||
                raw.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// True when this session will never call the real engine from <see cref="Apply"/>, regardless
    /// of license or selection. The UI must show this plainly — a forced dry-run that looks like a
    /// normal session would be exactly the kind of silent behavior this project's doctrine rejects.
    /// </summary>
    public bool ForceDryRunActive => _forceDryRun;

    public RepairSession(
        IKnowledgePack pack,
        IReadOnlyList<string> confinementRoots,
        InstallLayout? layout = null,
        string? appDataRoot = null,
        ILicenseVerifier? licenseVerifier = null,
        bool? forceDryRun = null)
    {
        var root = appDataRoot ?? DefaultAppDataRoot();
        var fs = new RealFileSystem();

        var registry = new RepairActionRegistry(
            new UnblockFileAction(fs),
            new RestoreRomArchiveAction(fs),
            new QuarantineOrphanedMediaAction(fs),
            new KillZombiePinUpDisplayAction(new RealProcessControl()));

        var backupRoot = Path.Combine(root, "repair-backups");
        _journal = new FileRepairJournal(Path.Combine(root, "repair-journal"));
        _licenseVerifier = licenseVerifier ?? new LicenseVerifier();
        _forceDryRun = forceDryRun ?? IsForceDryRunRequestedByEnvironment();

        _engine = new RepairEngine(
            registry, pack, _journal,
            new FileBackupService(fs, backupRoot),
            new RealEnvironmentProbe(backupRoot),
            new SystemClock(),
            confinementRoots, layout);
    }

    /// <summary>
    /// H.4 — verifies against the embedded public key, never trusts a caller-supplied bool.
    /// A missing/empty key is simply Invalid. The embedded key (<see cref="LicenseVerifier.EmbeddedPublicKeyBase64"/>)
    /// is a real one since 13/08/2026 (Maxime ran `license-tool init` for real) — a validly-signed
    /// license issued against the matching private key now verifies successfully.
    /// </summary>
    public LicenseCheckResult VerifyLicense(string? licenseKey) => _licenseVerifier.Verify(licenseKey);

    public RepairPlan Plan(string scanReportId, IReadOnlyList<Finding> findings, bool licensed)
        => _engine.Plan(scanReportId, findings, licensed);

    /// <summary>H.2 step 1 — blocking. Also re-verifies every finding still holds (step 2) and drops what changed.</summary>
    public PreflightResult Preflight(RepairPlan plan) => _engine.Preflight(plan);

    /// <summary>
    /// H.2 step 3 — everything the confirmation screen needs to say, in the text, per retained
    /// item: what target(s), reversible or not, backup planned or not. Pure — no I/O, no side
    /// effect, safe to call to render a screen before anything is applied.
    /// </summary>
    public static IReadOnlyList<ItemConfirmation> Describe(IReadOnlyList<RepairPlanItem> retainedItems)
        => retainedItems.Select(item =>
        {
            var targets = item.Changes.Select(c => c.Target).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var reversible = item.Summary?.FullyReversible
                ?? (item.Changes.Count > 0 && item.Changes.All(c => c.Reversible));
            var backupPlanned = item.Summary?.BackupPlanned ?? item.Changes.Count > 0;
            return new ItemConfirmation(
                item.ItemId, item.TargetCode, item.Mode, reversible, backupPlanned,
                item.Changes.Count, targets);
        }).ToList();

    /// <summary>
    /// H.2 step 4-5 — applies ONLY the items the caller explicitly selected (opt-in, never a
    /// silent "fix everything"), regardless of what <see cref="RepairMode.Automatic"/> would
    /// technically allow: this session always requires an explicit per-item selection, v1's answer
    /// to H.2 rule 3 ("jamais un tout réparer silencieux"). Backs up before every write and never
    /// writes when the backup itself fails (engine-level guarantee, LOT H.2 rule 4).
    /// </summary>
    public ApplyResult Apply(RepairPlan plan, IReadOnlySet<string> selectedItemIds)
    {
        if (_forceDryRun)
        {
            // Deliberately never touches _engine at all — not "call Apply but no-op inside it",
            // an actual skipped call. That is the whole guarantee: no registered action, no
            // backup service, no per-change journal write can run, no matter what a future bug in
            // any of them might do. Selection is still honored so the reported counts match what a
            // real Apply would have attempted.
            var outcomes = plan.Items
                .Where(i => selectedItemIds.Contains(i.ItemId))
                .ToDictionary(i => i.ItemId, _ => true);

            // The ONE journal write this path makes, and it exists specifically so this event is
            // never mistaken for a real one when read back later, by Undo or by a forum bug
            // report — see JournalEvent.ForcedDryRunApplied.
            _journal.Write(new JournalEntry
            {
                AtUtc = DateTimeOffset.UtcNow,
                Event = JournalEvent.ForcedDryRunApplied,
                PlanId = plan.PlanId,
                Detail = $"{outcomes.Count} item(s) would have been applied — forced dry-run, nothing written",
            });

            return new ApplyResult
            {
                PlanId = plan.PlanId,
                ItemOutcomes = outcomes,
                RecoveryRequired = false,
                ForcedDryRun = true,
            };
        }

        var withSelection = plan with
        {
            Items = plan.Items.Select(i => i with { Selected = selectedItemIds.Contains(i.ItemId) }).ToList(),
        };
        return _engine.Apply(withSelection);
    }

    /// <summary>H.2 step 5 / H.6 — accessible from the UI, not just the engine. Works after an app restart because the journal is on disk.</summary>
    public ExecutionResult Undo(string planId, string? itemId = null) => _engine.Undo(planId, itemId);

    public IReadOnlyDictionary<string, bool> Verify(RepairPlan plan) => _engine.Verify(plan);

    /// <summary>Every plan this session's journal has a record of, most recent first — the accessible Undo surface (H.2 rule 5) needs this to list what CAN be undone.</summary>
    public IReadOnlyList<string> KnownPlanIds() => _journal.KnownPlanIds();

    /// <summary>True if the journal's most recent write to disk failed — surfaced so the UI can warn rather than silently pretend Undo is guaranteed.</summary>
    public bool LastJournalWriteFailed => _journal.LastWriteFailed;
}

/// <summary>
/// Everything the confirmation screen must say about one retained plan item, per H.2 rule 3.
/// Deliberately carries no formatted/localized text — that stays in the App's Loc layer, this is
/// just the facts.
/// </summary>
public sealed record ItemConfirmation(
    string ItemId,
    string TargetCode,
    RepairMode Mode,
    bool Reversible,
    bool BackupPlanned,
    int ChangeCount,
    IReadOnlyList<string> Targets);
