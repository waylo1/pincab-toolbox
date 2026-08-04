namespace PincabToolbox.Repair.Actions;

/// <summary>
/// Terminates a PinUpDisplay.exe process left running with no table currently active — the
/// zombie reported on VPForums' PinUp Player troubleshooting guide (FIELD-LOG 2026-07-29): it
/// blocks the next table launch until killed by hand in Task Manager. Matches PINUP_DISPLAY_ZOMBIE
/// (<see cref="PincabToolbox.Core.Scanning.PinupDisplayZombieScanner"/>).
///
/// Never reversible by nature: there is no meaningful "undo" for terminating a process, only
/// relaunching the frontend, which the user's own frontend already does on the next table. Per
/// <see cref="RepairModeResolver"/>, a non-reversible action never reaches
/// <see cref="RepairMode.Automatic"/> — it always stops at
/// <see cref="RepairMode.ConfirmationRequired"/>, whatever the confidence.
/// </summary>
public sealed class KillZombiePinUpDisplayAction : IRepairAction
{
    /// <summary>Bare process name (no extension), as used across the engine (RealEnvironmentProbe).</summary>
    public const string ProcessName = "PinUpDisplay";

    private readonly IProcessControl _proc;

    public KillZombiePinUpDisplayAction(IProcessControl proc) => _proc = proc;

    public string ActionId => "kill_zombie_pinup_display";
    public ChangeKind Kind => ChangeKind.ProcessTermination;
    public bool IsReversibleByNature => false;

    public ValidationResult ValidateParameters(IReadOnlyDictionary<string, string> p) => ValidationResult.Ok;

    public IReadOnlyList<PlannedChange> Plan(RepairContext ctx, IReadOnlyDictionary<string, string> p)
    {
        if (!_proc.IsRunning(ProcessName)) return Array.Empty<PlannedChange>();

        // Fail CLOSED rather than trust a bare process name: without a resolvable executable
        // path, the engine's containment check (ADR-005) has nothing meaningful to validate
        // against, so this plans nothing sooner than risk acting on the wrong process.
        var path = ctx.Finding.FilePath ?? _proc.PathOf(ProcessName);
        if (string.IsNullOrWhiteSpace(path)) return Array.Empty<PlannedChange>();

        return new[]
        {
            new PlannedChange
            {
                ActionId = ActionId,
                Kind = Kind,
                Target = path,
                Before = "running (no table active — zombie)",
                After = "terminated",
                Reversible = false,
            }
        };
    }

    public bool StillApplies(RepairContext ctx) => _proc.IsRunning(ProcessName);

    public ExecutionResult Execute(PlannedChange c)
        => _proc.Kill(ProcessName)
            ? ExecutionResult.Ok
            : ExecutionResult.Fail($"could not terminate {ProcessName}");

    public ExecutionResult Revert(PlannedChange c)
        => ExecutionResult.Fail("not reversible — relaunch your frontend (PinUP Popper/PinballX) "
                               + "if the display doesn't come back on its own");
}
