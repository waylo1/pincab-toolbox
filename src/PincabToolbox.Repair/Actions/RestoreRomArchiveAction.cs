using System.IO.Compression;

namespace PincabToolbox.Repair.Actions;

/// <summary>
/// A ROM was extracted into a folder inside the roms directory. VPinMAME only loads ROMs
/// from .zip archives, so the table behaves as if the ROM were missing.
///
/// This re-zips the folder contents under the expected name and moves the original folder
/// aside (never deletes it) — which is what makes the action reversible.
/// Matches ROM_UNZIPPED, AutoFixable in Knowledge.cs.
/// </summary>
public sealed class RestoreRomArchiveAction : IRepairAction
{
    /// <summary>Suffix used to park the original folder. Kept, never deleted.</summary>
    public const string ParkedSuffix = ".pctb-parked";

    private readonly IFileSystem _fs;

    public RestoreRomArchiveAction(IFileSystem fs) => _fs = fs;

    public string ActionId => "restore_rom_archive";
    public ChangeKind Kind => ChangeKind.FileMove;
    public bool IsReversibleByNature => true;

    public ValidationResult ValidateParameters(IReadOnlyDictionary<string, string> p)
        => ValidationResult.Ok;

    public IReadOnlyList<PlannedChange> Plan(RepairContext ctx, IReadOnlyDictionary<string, string> p)
    {
        var folder = ctx.Finding.FilePath;
        if (string.IsNullOrWhiteSpace(folder)) return Array.Empty<PlannedChange>();
        if (!_fs.DirectoryExists(folder)) return Array.Empty<PlannedChange>();

        var zip = folder + ".zip";
        if (_fs.FileExists(zip)) return Array.Empty<PlannedChange>();   // already fine

        var count = _fs.GetFiles(folder).Count;
        return new[]
        {
            new PlannedChange
            {
                ActionId = ActionId,
                Kind = Kind,
                Target = zip,
                Before = $"extracted folder ({count} file{(count == 1 ? "" : "s")})",
                After = "archive restored, folder kept aside",
                Reversible = true,
            }
        };
    }

    public bool StillApplies(RepairContext ctx)
    {
        var folder = ctx.Finding.FilePath;
        return !string.IsNullOrWhiteSpace(folder)
               && _fs.DirectoryExists(folder)
               && !_fs.FileExists(folder + ".zip");
    }

    public ExecutionResult Execute(PlannedChange c)
    {
        // c.Target is the .zip; the source folder is the same path without the extension.
        var folder = StripZip(c.Target);
        try
        {
            if (!_fs.DirectoryExists(folder)) return ExecutionResult.Fail($"folder not found: {folder}");
            if (_fs.FileExists(c.Target)) return ExecutionResult.Fail($"archive already exists: {c.Target}");

            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var file in _fs.GetFiles(folder))
                {
                    var name = LastSegment(file);
                    var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
                    using var es = entry.Open();
                    var bytes = _fs.ReadAllBytes(file);
                    es.Write(bytes, 0, bytes.Length);
                }
            }
            _fs.WriteAllBytes(c.Target, ms.ToArray());

            // Park the folder rather than delete it — that is what makes this reversible.
            _fs.MoveDirectory(folder, folder + ParkedSuffix);
            return ExecutionResult.Ok;
        }
        catch (Exception e) { return ExecutionResult.Fail(e.Message); }
    }

    public ExecutionResult Revert(PlannedChange c)
    {
        var folder = StripZip(c.Target);
        try
        {
            if (_fs.DirectoryExists(folder + ParkedSuffix))
                _fs.MoveDirectory(folder + ParkedSuffix, folder);
            if (_fs.FileExists(c.Target))
                _fs.DeleteFile(c.Target);
            return ExecutionResult.Ok;
        }
        catch (Exception e) { return ExecutionResult.Fail(e.Message); }
    }

    private static string StripZip(string path)
        => path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? path[..^4] : path;

    private static string LastSegment(string path)
    {
        var i = path.LastIndexOfAny(new[] { '/', '\\' });
        return i < 0 ? path : path[(i + 1)..];
    }
}
