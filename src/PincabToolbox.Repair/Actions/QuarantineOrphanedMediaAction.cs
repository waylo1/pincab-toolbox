using PincabToolbox.Core.Services;

namespace PincabToolbox.Repair.Actions;

/// <summary>
/// Moves PinUP Popper media files (POPMedia / PUPVideos) that no longer relate to any installed
/// table into a sibling "_pctb-quarantine" folder — never deletes them. Matches ORPHANED_MEDIA_FILE
/// (<see cref="PincabToolbox.Core.Scanning.OrphanedMediaScanner"/>).
///
/// FIELD-LOG 2026-07-29 ("Nettoyer automatiquement votre dossier PinupSystem"): a community
/// PowerShell script did this with a straight delete, and its first version wrongly removed
/// still-used per-screen loading videos because its matching missed the "(SCREENx)" suffix.
/// Two independent guards against repeating that here: (1) matching goes through the same
/// <see cref="OrphanMediaMatcher"/> the scanner uses, biased towards NOT flagging a file, and
/// (2) even a wrongly-flagged file is only ever MOVED next to itself, quarantined, and backed up
/// by the engine before the move — nothing is ever unrecoverable in one step, unlike a delete.
///
/// Recomputes the candidate list itself from <see cref="RepairContext.Layout"/> rather than
/// trusting a single Finding.FilePath — the scanner's aggregate finding only carries a count, and
/// re-deriving from the real, current filesystem state at plan time is what ADR-006 asks for
/// ("the plan is calculated, never declared").
/// </summary>
public sealed class QuarantineOrphanedMediaAction : IRepairAction
{
    public const string QuarantineFolderName = "_pctb-quarantine";

    private readonly IFileSystem _fs;

    public QuarantineOrphanedMediaAction(IFileSystem fs) => _fs = fs;

    public string ActionId => "quarantine_orphaned_media";
    public ChangeKind Kind => ChangeKind.FileMove;
    public bool IsReversibleByNature => true;

    public ValidationResult ValidateParameters(IReadOnlyDictionary<string, string> p) => ValidationResult.Ok;

    public IReadOnlyList<PlannedChange> Plan(RepairContext ctx, IReadOnlyDictionary<string, string> p)
    {
        if (ctx.Layout is null) return Array.Empty<PlannedChange>();

        var tableNames = ctx.Layout.VpxTables
            .Select(NameNoExt)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        var changes = new List<PlannedChange>();
        foreach (var root in new[] { ctx.Layout.PopMediaDir, ctx.Layout.PupVideosDir })
        {
            if (root is null) continue;
            foreach (var file in OneLevelDeep(root))
            {
                if (IsAlreadyQuarantined(file)) continue;
                if (!OrphanMediaMatcher.IsOrphan(NameNoExt(file), tableNames)) continue;

                var quarantineDir = ParentDir(file) + "/" + QuarantineFolderName;
                var target = quarantineDir + "/" + FileName(file);
                if (_fs.FileExists(target)) continue;   // already quarantined under this name

                changes.Add(new PlannedChange
                {
                    ActionId = ActionId,
                    Kind = Kind,
                    Target = target,
                    Before = file,
                    After = "quarantined (not deleted)",
                    Reversible = true,
                });
            }
        }
        return changes;
    }

    public bool StillApplies(RepairContext ctx) => Plan(ctx, EmptyParams).Count > 0;

    public ExecutionResult Execute(PlannedChange c)
    {
        try
        {
            var dir = ParentDir(c.Target);
            if (!_fs.DirectoryExists(dir)) _fs.CreateDirectory(dir);
            _fs.MoveFile(c.Before, c.Target);
            return ExecutionResult.Ok;
        }
        catch (Exception e) { return ExecutionResult.Fail(e.Message); }
    }

    public ExecutionResult Revert(PlannedChange c)
    {
        try
        {
            if (_fs.FileExists(c.Target)) _fs.MoveFile(c.Target, c.Before);
            return ExecutionResult.Ok;
        }
        catch (Exception e) { return ExecutionResult.Fail(e.Message); }
    }

    // ───────────────────────────── helpers ─────────────────────────────

    private static readonly Dictionary<string, string> EmptyParams = new();

    private IEnumerable<string> OneLevelDeep(string root)
    {
        if (!_fs.DirectoryExists(root)) yield break;
        foreach (var f in _fs.GetFiles(root)) yield return f;
        foreach (var dir in _fs.GetDirectories(root))
        {
            if (FileName(dir) == QuarantineFolderName) continue;
            foreach (var f in _fs.GetFiles(dir)) yield return f;
        }
    }

    private static bool IsAlreadyQuarantined(string path)
        => path.Replace('\\', '/').Contains("/" + QuarantineFolderName + "/", StringComparison.OrdinalIgnoreCase);

    private static string NameNoExt(string path)
    {
        var name = FileName(path);
        var dot = name.LastIndexOf('.');
        return dot > 0 ? name[..dot] : name;
    }

    private static string FileName(string path)
    {
        var i = path.LastIndexOfAny(new[] { '/', '\\' });
        return i < 0 ? path : path[(i + 1)..];
    }

    private static string ParentDir(string path)
    {
        var norm = path.Replace('\\', '/');
        var i = norm.LastIndexOf('/');
        return i < 0 ? "" : norm[..i];
    }
}
