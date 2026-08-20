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
            new KillZombiePinUpDisplayAction(new RealProcessControl()),
            new RegisterComComponentAction(new RealProcessLauncher(), new RealElevatedProcessLauncher()));
        // Registered (19/08) but still inert: no pack repairRules entry targets it yet — see
        // RegisterComComponentAction's own header for the remaining, unrelated-to-admin-rights blocker.

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
    ///
    /// <para>
    /// Carries <see cref="RepairPlanItem.Missing"/> through too (13/08, ADR-006 gap closed) — a
    /// <c>ManualOnly</c> item (no automatable change, e.g. ROM_MISSING: Repair cannot invent a ROM
    /// dump) has nothing else to show, and before this it was dropped silently: the App only ever
    /// rendered items with <c>ChangeCount &gt; 0</c>, so a real Critical finding could be invisible
    /// in the Repair tab even though Scanner reported it and the engine had a step-by-step reason
    /// ready. That filtering decision belongs to the caller now, with the full facts in hand.
    /// </para>
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
                item.Changes.Count, targets, item.Missing);
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

    /// <summary>
    /// 13/08 — replaces a raw list of plan IDs as the Undo surface. Maxime, testing on his real cab:
    /// "les intitulés sont pas parlants, on voit pas le detail du plan donc on pourrait très bien
    /// annuler un plan qui a fonctionné" — a plan ID like "plan-20260813-184700-1234" says nothing
    /// about what it did, whether it already ran, or whether Undo would even change anything. This
    /// derives that from the journal alone (no extra state to keep in sync), the same source Undo
    /// itself reads, so the summary can never claim something different from what Undo would do.
    /// </summary>
    public PlanSummary Summarize(string planId)
    {
        var entries = _journal.Read(planId);

        var createdAt = entries.FirstOrDefault(e => e.Event == JournalEvent.PlanCreated)?.AtUtc
                        ?? entries.Select(e => (DateTimeOffset?)e.AtUtc).FirstOrDefault();

        var forcedDryRun = entries.Any(e => e.Event == JournalEvent.ForcedDryRunApplied);

        var completedItemIds = entries
            .Where(e => e.Event == JournalEvent.ItemCompleted && e.ItemId is not null)
            .Select(e => e.ItemId!).Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);

        // ItemUndone is written even for a no-op ("nothing to undo" — e.g. a forced-dry-run plan
        // that never really applied anything), so it alone cannot mean "this item was reverted".
        // A real revert always writes at least one ChangeReverted first.
        var revertedItemIds = entries
            .Where(e => e.Event == JournalEvent.ChangeReverted && e.ItemId is not null)
            .Select(e => e.ItemId!).Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);

        var targets = entries
            .Where(e => e.Event == JournalEvent.ChangeApplied && e.Change is not null)
            .Select(e => LastSegment(e.Change!.Target))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 20/08 — Maxime, testeur réel : la liste "Réparé" ne montrait que les noms de fichiers
        // touchés ("PopRunSQL.exe, x360ce.exe…"), jamais CE QUI avait été fait dessus. La donnée
        // existait déjà dans le journal (chaque ChangeApplied porte ActionId + Before/After, voir
        // PlannedChange) — seule Summarize() la jetait. Un item annulé garde son entrée ici (même
        // logique que `targets` juste au-dessus) : "Annulé" a sa propre ligne dans BuildPlanSummaryText,
        // ce n'est pas à ChangeDetails de trancher ce qui tient encore.
        var changeDetails = entries
            .Where(e => e.Event == JournalEvent.ChangeApplied && e.Change is not null)
            .Select(e => new RepairChangeDetail(e.Change!.ActionId, LastSegment(e.Change.Target), e.Change.Before, e.Change.After))
            .ToList();

        var stillApplied = completedItemIds.Except(revertedItemIds).Count();

        var outcome = forcedDryRun ? PlanOutcome.ForcedDryRun
            : completedItemIds.Count == 0 ? PlanOutcome.NothingApplied
            : revertedItemIds.Count == 0 ? PlanOutcome.Applied
            : stillApplied == 0 ? PlanOutcome.FullyUndone
            : PlanOutcome.PartiallyUndone;

        return new PlanSummary(planId, createdAt, completedItemIds.Count, revertedItemIds.Count, targets, forcedDryRun, outcome, changeDetails);
    }

    /// <summary>Every known plan, summarized, most recent first — what the App's Réparé/Annulé lists render from.</summary>
    public IReadOnlyList<PlanSummary> AllPlanSummaries() => KnownPlanIds().Select(Summarize).ToList();

    /// <summary>Same convention as <c>RepairEngine.ProcessNameFromPath</c> — splits by hand so it also works off Windows.</summary>
    private static string LastSegment(string path)
    {
        var i = path.LastIndexOfAny(new[] { '/', '\\' });
        return i < 0 ? path : path[(i + 1)..];
    }

    /// <summary>True if the journal's most recent write to disk failed — surfaced so the UI can warn rather than silently pretend Undo is guaranteed.</summary>
    public bool LastJournalWriteFailed => _journal.LastWriteFailed;
}

/// <summary>
/// What a plan did, in facts derived purely from the journal — see <see cref="RepairSession.Summarize"/>.
/// </summary>
public sealed record PlanSummary(
    string PlanId,
    DateTimeOffset? CreatedAtUtc,
    int ItemsCompleted,
    int ItemsUndone,
    IReadOnlyList<string> Targets,
    bool ForcedDryRun,
    PlanOutcome Outcome,
    IReadOnlyList<RepairChangeDetail> ChangeDetails);

/// <summary>
/// One applied change, in facts derived purely from the journal (same posture as
/// <see cref="PlanSummary"/> itself) — what <c>BuildPlanSummaryText</c> (App) renders per target
/// instead of a bare filename. <see cref="Before"/>/<see cref="After"/> are the same English text
/// each <c>IRepairAction</c> already writes into <see cref="PlannedChange"/> for the journal export
/// — not yet localized (ADR-0xx candidate if this needs FR/ES phrasing later); the App layer maps
/// <see cref="ActionId"/> to a localized short label where it can, and falls back to this raw text
/// otherwise, so an action added later without App-side wiring still shows something instead of
/// nothing.
/// </summary>
public sealed record RepairChangeDetail(string ActionId, string Target, string Before, string After);

/// <summary>
/// Which of the three lists (Réparé / À faire / Annulé, per Maxime's 13/08 request) a plan belongs
/// in. "À faire" itself is not here — that is the live, not-yet-applied checklist the App already
/// renders from <see cref="RepairSession.Describe"/>, never something the journal would know about.
/// </summary>
public enum PlanOutcome
{
    /// <summary>Nothing was ever completed for this plan (blocked at preflight, or every item was manual/skipped).</summary>
    NothingApplied,
    /// <summary>At least one item applied, nothing undone since — belongs in "Réparé".</summary>
    Applied,
    /// <summary>Some applied items were undone, some are still standing — still "Réparé" for what remains, Undo still offered.</summary>
    PartiallyUndone,
    /// <summary>Every applied item has since been undone — belongs in "Annulé", nothing left to undo.</summary>
    FullyUndone,
    /// <summary>PINCAB_REPAIR_FORCE_DRYRUN was active — nothing was ever really written, shown separately so it can never be mistaken for a real repair.</summary>
    ForcedDryRun,
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
    IReadOnlyList<string> Targets,
    IReadOnlyList<RepairLimitation> Missing);
