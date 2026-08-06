using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Flags a broken NTFS junction or directory symlink among this install's key folders — a very
/// pincab-specific failure mode: owners routinely junction a big folder (roms, PUPVideos, an
/// individual colorization set) out to a second drive or NAS share for space reasons, and when that
/// drive is offline/renamed/disconnected the folder still LOOKS present (it has a normal directory
/// entry) but is completely empty from every tool's point of view — no error anywhere, just "all my
/// ROMs disappeared" (audit §4/G3).
///
/// <para>
/// Scope: this install's own key folders (<see cref="InstallLayout.RootPath"/>,
/// <see cref="InstallLayout.TablesDir"/>, <see cref="InstallLayout.VPinMameDir"/>,
/// <see cref="InstallLayout.RomsDir"/>, <see cref="InstallLayout.PupVideosDir"/>,
/// <see cref="InstallLayout.PopMediaDir"/>) plus each of their immediate (one level, non-recursive)
/// subdirectories — catches both "the whole folder is a broken junction" and "one subfolder inside it
/// is" (e.g. a single ROM's altsound/altcolor set symlinked to removable media), without an unbounded
/// recursive walk of the whole install.
/// </para>
///
/// <para>
/// Detection avoids the one real footgun in this check: a naive existence probe on the junction path
/// itself can silently follow the reparse point and report "doesn't exist" for the very entry we're
/// trying to examine. The real implementation reads the entry's raw attributes (which Windows always
/// resolves on the reparse point itself, never its target) to confirm it IS a reparse point before
/// ever asking about the target — see <see cref="RealGetLinkTarget"/>.
/// </para>
/// </summary>
public sealed class JunctionScanner : IScanner
{
    public string Id => "junctions";
    public string Name => "Junction Health";

    private readonly Func<string, string?> _getLinkTarget;
    private readonly Func<string, bool> _directoryExists;
    private readonly Func<string, IReadOnlyCollection<string>> _listSubdirectories;

    /// <param name="getLinkTarget">Given a directory path, returns its reparse target, or null when it isn't a reparse point (or is unreadable). Defaults to a real, reparse-point-safe attribute read.</param>
    /// <param name="directoryExists">Given a target path, whether it currently resolves. Defaults to a real disk check.</param>
    /// <param name="listSubdirectories">Given a folder path, its immediate (non-recursive) subdirectory paths. Defaults to a real directory listing.</param>
    public JunctionScanner(
        Func<string, string?>? getLinkTarget = null,
        Func<string, bool>? directoryExists = null,
        Func<string, IReadOnlyCollection<string>>? listSubdirectories = null)
    {
        _getLinkTarget = getLinkTarget ?? RealGetLinkTarget;
        _directoryExists = directoryExists ?? Directory.Exists;
        _listSubdirectories = listSubdirectories ?? ListSubdirectoriesOnDisk;
    }

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in CandidateRoots(ctx.Layout))
        {
            ctx.Cancellation.ThrowIfCancellationRequested();

            var rootFinding = Evaluate(root, seen);
            if (rootFinding is not null) yield return rootFinding;

            IReadOnlyCollection<string> children;
            try { children = _listSubdirectories(root); }
            catch { continue; } // unreadable -> silence, never a false positive

            foreach (var child in children)
            {
                ctx.Cancellation.ThrowIfCancellationRequested();
                var childFinding = Evaluate(child, seen);
                if (childFinding is not null) yield return childFinding;
            }
        }
    }

    private static IEnumerable<string> CandidateRoots(InstallLayout layout)
    {
        var roots = new[] { layout.RootPath, layout.TablesDir, layout.VPinMameDir, layout.RomsDir, layout.PupVideosDir, layout.PopMediaDir };
        return roots.Where(r => r is not null).Select(r => r!).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private Finding? Evaluate(string path, HashSet<string> seen)
    {
        if (!seen.Add(path)) return null; // already checked (roots and their children can overlap)

        string? target;
        try { target = _getLinkTarget(path); } catch { return null; } // unreadable -> silence
        if (target is null) return null; // not a reparse point -- nothing to check

        bool exists;
        try { exists = _directoryExists(target); } catch { return null; }
        if (!JunctionInspector.IsBroken(isReparsePoint: true, targetExists: exists)) return null;

        var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return new Finding
        {
            Code = "BROKEN_JUNCTION", Severity = Severity.Warning, Category = Id,
            Subject = name.Length > 0 ? name : path, FilePath = path,
            Args = new[] { path, target },
            EnglishText = $"'{path}' is a junction/symlink pointing to '{target}', which no longer exists — everything expected under this folder is invisible to Visual Pinball, PinUP Popper, and this scan alike.",
            FixHint = "Reconnect the drive/share the link points to, or recreate the junction (mklink /J) pointing at its correct, currently-available location. Remove it if the linked folder is gone for good.",
        };
    }

    private static IReadOnlyCollection<string> ListSubdirectoriesOnDisk(string path)
    {
        if (!Directory.Exists(path)) return Array.Empty<string>();
        return Directory.EnumerateDirectories(path).ToList();
    }

    /// <summary>
    /// Reads raw file attributes first (Windows always resolves these on the reparse point entry
    /// itself, never by following it) to confirm this IS a reparse point before ever asking about its
    /// target — a plain <c>Directory.Exists</c> probe on the junction path can silently follow a
    /// broken link and report "doesn't exist" for the very entry this scanner exists to examine.
    /// </summary>
    private static string? RealGetLinkTarget(string path)
    {
        try
        {
            var attrs = File.GetAttributes(path);
            if (!attrs.HasFlag(FileAttributes.ReparsePoint)) return null;
            return new DirectoryInfo(path).LinkTarget;
        }
        catch { return null; } // no entry at all, or unreadable -> not a defect we can report honestly
    }
}
