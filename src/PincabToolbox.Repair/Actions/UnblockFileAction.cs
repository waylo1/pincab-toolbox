namespace PincabToolbox.Repair.Actions;

/// <summary>
/// Removes the Windows "Mark of the Web" (Zone.Identifier alternate data stream) that
/// silently prevents a downloaded DLL from loading.
///
/// The safest fix in the catalogue: it deletes no content, only a marker, and the marker
/// can be put back. Matches BLOCKED_DLL, which is AutoFixable in Knowledge.cs.
/// </summary>
public sealed class UnblockFileAction : IRepairAction
{
    private readonly IFileSystem _fs;

    public UnblockFileAction(IFileSystem fs) => _fs = fs;

    public string ActionId => "unblock_file";
    public ChangeKind Kind => ChangeKind.FileAttribute;
    public bool IsReversibleByNature => true;

    public ValidationResult ValidateParameters(IReadOnlyDictionary<string, string> p)
        => ValidationResult.Ok;   // the target comes from the finding, not from the pack

    public IReadOnlyList<PlannedChange> Plan(RepairContext ctx, IReadOnlyDictionary<string, string> p)
    {
        var target = ctx.Finding.FilePath;
        if (string.IsNullOrWhiteSpace(target)) return Array.Empty<PlannedChange>();
        if (!_fs.FileExists(target)) return Array.Empty<PlannedChange>();
        if (!_fs.HasZoneIdentifier(target)) return Array.Empty<PlannedChange>();

        return new[]
        {
            new PlannedChange
            {
                ActionId = ActionId,
                Kind = Kind,
                Target = target,
                Before = "blocked by Windows",
                After = "unblocked",
                Reversible = true,
            }
        };
    }

    public bool StillApplies(RepairContext ctx)
    {
        var target = ctx.Finding.FilePath;
        return !string.IsNullOrWhiteSpace(target)
               && _fs.FileExists(target)
               && _fs.HasZoneIdentifier(target);
    }

    public ExecutionResult Execute(PlannedChange c)
    {
        try
        {
            _fs.RemoveZoneIdentifier(c.Target);
            return ExecutionResult.Ok;
        }
        catch (Exception e) { return ExecutionResult.Fail(e.Message); }
    }

    public ExecutionResult Revert(PlannedChange c)
    {
        try
        {
            _fs.AddZoneIdentifier(c.Target);
            return ExecutionResult.Ok;
        }
        catch (Exception e) { return ExecutionResult.Fail(e.Message); }
    }
}
