using PincabToolbox.Core.Models;

namespace PincabToolbox.Repair;

/// <summary>Nature of a write. Drives how it is backed up and undone.</summary>
public enum ChangeKind
{
    FileAttribute,
    FileMove,
    IniWrite,
    RegistryWrite,
    SqliteWrite,

    /// <summary>A running process is terminated. Never reversible by nature — see
    /// <see cref="PincabToolbox.Repair.Actions.KillZombiePinUpDisplayAction"/>.</summary>
    ProcessTermination,

    /// <summary>The Windows default playback device is changed. Reversible — the previous
    /// default is captured as <see cref="PlannedChange.Before"/>.</summary>
    AudioDeviceDefault,

    /// <summary>
    /// A component's own registration tool is launched to re-register it with Windows COM
    /// (LOT I, spec 10/08). Never reversible by nature — see
    /// <see cref="PincabToolbox.Repair.Actions.RegisterComComponentAction"/>: the previous
    /// registration (often already broken/stale, that is usually WHY the finding fired) cannot be
    /// reliably restored, so <see cref="PlannedChange.Before"/> is a trace of the observed
    /// pre-operation state, not restore data.
    /// </summary>
    ComReregistration,
}

/// <summary>
/// Result of crossing the two gates: commercial (licence) then safety (confidence).
/// The safety gate can only DOWNGRADE the mode, never upgrade it.
/// </summary>
public enum RepairMode
{
    /// <summary>No rule, or confidence &lt; 70: manual procedure shown, no button.</summary>
    ManualOnly = 0,
    /// <summary>
    /// A rule exists but no licence. The SUMMARY is shown (a fix exists, reversible, backed up,
    /// roughly this long); the detailed plan is not — that is what Repair sells. ADR-006.
    /// </summary>
    Locked = 1,
    /// <summary>Explicit confirmation required, fix by fix.</summary>
    ConfirmationRequired = 2,
    /// <summary>Batchable — still with backup, opt-in and journal.</summary>
    Automatic = 3,
}

public enum Completeness { Full, Partial }

/// <summary>
/// One unit of write, as planned. The SAME object is consumed by the preview and by apply —
/// otherwise the preview would be a lie, and the preview is what earns the trust.
/// </summary>
/// <remarks>
/// <see cref="Before"/> has the same shape as an evidence item (ADR-003): a preview is
/// evidence about a future state. Same UI rendering, same anonymisation on export.
/// </remarks>
public sealed record PlannedChange
{
    public required string ActionId { get; init; }
    public required ChangeKind Kind { get; init; }

    /// <summary>Absolute path, registry key, or table+row id.</summary>
    public required string Target { get; init; }

    public required string Before { get; init; }
    public required string After { get; init; }
    public required bool Reversible { get; init; }
}

/// <summary>A repair rule — DATA, from the Knowledge Pack. See ADR-005.</summary>
public sealed record RepairRule
{
    public required string Id { get; init; }
    public required string TargetCode { get; init; }

    /// <summary>
    /// Must exist in the compiled registry. An unknown ActionId is NOT an error:
    /// the rule is ignored and the finding falls back to ManualOnly, so a pack newer
    /// than the app degrades cleanly.
    /// </summary>
    public required string ActionId { get; init; }

    public IReadOnlyDictionary<string, string> Parameters { get; init; } =
        new Dictionary<string, string>();

    public required int RepairConfidence { get; init; }

    /// <summary>Declared by the pack — but IRepairAction.IsReversibleByNature has the last word.</summary>
    public required bool Reversible { get; init; }

    public bool BackupRequired { get; init; } = true;

    public string? ManualProcedureFr { get; init; }
    public string? ManualProcedureEn { get; init; }
}

public sealed record Blocker
{
    public required string Code { get; init; }
    public required string MessageFr { get; init; }
    public required string MessageEn { get; init; }
}

/// <summary>
/// One reason a step stayed manual (ADR-006). Bilingual by construction, same shape as
/// <see cref="Blocker"/> — the engine must never hand the App an English-only sentence to
/// display verbatim (13/08/2026, FIELD-LOG: the ADR-006 "not automatable" line and the manual-item
/// confirmation text both leaked raw English into the FR UI because <c>Missing</c> used to be a
/// plain <c>string</c>).
/// <para>
/// <see cref="MessageFr"/> is null when the engine only has an English source at hand (e.g. a
/// <see cref="Finding.FixHint"/> — Core has no FR text for those, only the App's Loc table does,
/// keyed by <see cref="Code"/>). The App resolves the final display string: prefer
/// <see cref="MessageFr"/>, else look up <see cref="Code"/> in its own FR table, else fall back to
/// <see cref="MessageEn"/>.
/// </para>
/// </summary>
public sealed record RepairLimitation
{
    /// <summary>Finding code or rule id, when there is one — lets the App look up its own FR text. Null for purely technical/internal reasons.</summary>
    public string? Code { get; init; }
    public required string MessageEn { get; init; }
    public string? MessageFr { get; init; }
}

/// <summary>
/// The unit of TRANSACTION. A scenario playbook is ONE item with several ordered changes —
/// that is what gives compensation the right granularity.
/// </summary>
public sealed record RepairPlanItem
{
    public required string ItemId { get; init; }
    public required string TargetCode { get; init; }
    public required RepairMode Mode { get; init; }

    /// <summary>
    /// ORDERED. Order matters for playbooks.
    /// EMPTY when the plan was produced without a licence — the detail is what Repair sells
    /// (ADR-006). Use <see cref="Summary"/> to render the free view.
    /// </summary>
    public required IReadOnlyList<PlannedChange> Changes { get; init; }

    /// <summary>
    /// The free-tier view: a fix exists, it is reversible, it is backed up, it takes about this long.
    /// Always populated when a repair is available, licensed or not. Computed from the real plan.
    /// </summary>
    public RepairSummary? Summary { get; init; }

    /// <summary>The originating finding, used to re-verify at preflight time.</summary>
    public Finding? SourceFinding { get; init; }

    public string? RuleId { get; init; }

    public Completeness Completeness { get; init; } = Completeness.Full;

    /// <summary>What cannot be automated, and why. Shown BEFORE acting.</summary>
    public IReadOnlyList<RepairLimitation> Missing { get; init; } = Array.Empty<RepairLimitation>();

    public IReadOnlyList<Blocker> Blockers { get; init; } = Array.Empty<Blocker>();

    /// <summary>Opt-in. False by default, always.</summary>
    public bool Selected { get; init; }
}

public sealed record RepairPlan
{
    public required string PlanId { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required string ScanReportId { get; init; }
    public required IReadOnlyList<RepairPlanItem> Items { get; init; }
}

public sealed record ValidationResult(bool IsValid, string? Reason = null)
{
    public static ValidationResult Ok { get; } = new(true);
    public static ValidationResult Fail(string reason) => new(false, reason);
}

public sealed record ExecutionResult(bool Success, string? Error = null)
{
    public static ExecutionResult Ok { get; } = new(true);
    public static ExecutionResult Fail(string error) => new(false, error);
}

public sealed record PreflightResult
{
    public required bool Passed { get; init; }
    public required IReadOnlyList<Blocker> Blockers { get; init; }
    public required IReadOnlyList<RepairPlanItem> RetainedItems { get; init; }
}

public sealed record ApplyResult
{
    public required string PlanId { get; init; }
    public required IReadOnlyDictionary<string, bool> ItemOutcomes { get; init; }

    /// <summary>
    /// True when a rollback itself failed. The recovery screen must be shown, with the
    /// backup path and the exact list of files to restore by hand.
    /// </summary>
    public required bool RecoveryRequired { get; init; }

    public string? BackupPath { get; init; }
    public IReadOnlyList<Blocker> Blockers { get; init; } = Array.Empty<Blocker>();

    /// <summary>
    /// True when this result comes from <see cref="RepairSession"/>'s forced-dry-run mode
    /// (<c>PINCAB_REPAIR_FORCE_DRYRUN</c>) — <see cref="ItemOutcomes"/> reports what WOULD have
    /// happened, but the engine was never called: zero disk I/O occurred. The caller must never
    /// present this the same way as a real apply — see <see cref="RepairSession.Apply"/>.
    /// </summary>
    public bool ForcedDryRun { get; init; } = false;
}
