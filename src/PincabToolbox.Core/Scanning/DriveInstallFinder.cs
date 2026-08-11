using PincabToolbox.Core.Profiles;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Finds every pincab install under a starting folder (typically a whole drive, e.g. "C:\"), so
/// scanning is not limited to a single manually-chosen root (TRANSMISSION #14, 10/08 — "le
/// scanner doit lire tout le disque" ; feu vert explicite de Maxime pour rouvrir ce point du
/// Scanner gelé du 03/08).
///
/// <para>
/// Deliberately does NOT call <see cref="LayoutDetector.Detect"/> on every visited directory —
/// that method does its own bounded depth-5 recursive fallback search, so invoking it at every
/// node of an already-recursive drive walk would be O(visited nodes) × O(depth-5 sub-search),
/// unusable on a real C: drive. Instead each node gets one cheap, shallow, non-recursive check
/// (<see cref="IsCandidateRoot"/>) for the same markers LayoutDetector itself trusts — a Tables
/// folder with .vpx files, a VPinMAME folder, or a PinUP Popper database. Once a node matches, the
/// walk stops recursing into it (that confirmed root's own subfolders are not reported as
/// separate roots) but continues into its siblings, so a drive with multiple independent
/// installs (e.g. one under "Visual Pinball" and another under "PinCab2") finds them all.
/// </para>
/// </summary>
public static class DriveInstallFinder
{
    /// <summary>How far under the starting folder the walk itself descends looking for candidate roots.</summary>
    private const int WalkMaxDepth = 6;

    /// <summary>
    /// Folder names skipped everywhere in the walk — Windows/system internals, package caches,
    /// and dev tooling noise that would otherwise blow up the walk's cost for zero chance of
    /// containing a pincab install.
    /// </summary>
    private static readonly HashSet<string> NoiseDirNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Windows", "$Recycle.Bin", "System Volume Information", "WindowsApps",
        "Package Cache", "node_modules", ".git", "$WinREAgent", "Recovery",
        "PerfLogs", "AppData", "ProgramData",
    };

    /// <summary>
    /// Walks <paramref name="startPath"/> (a drive root or any folder) and yields every directory
    /// that looks like a pincab install root. Never throws on an unreadable subtree — one blocked
    /// folder is skipped, the rest of the walk continues (same doctrine as
    /// <see cref="LayoutDetector.SafeEnumerateDirs"/>).
    /// </summary>
    public static IEnumerable<string> FindCandidateRoots(string startPath, Profile profile, CancellationToken ct = default) =>
        Walk(startPath, profile, 0, ct);

    private static IEnumerable<string> Walk(string dir, Profile profile, int depth, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (IsCandidateRoot(dir, profile))
        {
            yield return dir;
            yield break; // confirmed root: its own subfolders are not reported as separate roots
        }

        if (depth >= WalkMaxDepth) yield break;

        string[] children;
        try { children = Directory.GetDirectories(dir); }
        catch { yield break; } // unreadable (permissions, junction loop…): skip, don't fail the whole walk

        foreach (var child in children)
        {
            ct.ThrowIfCancellationRequested();
            if (NoiseDirNames.Contains(Path.GetFileName(child))) continue;
            foreach (var found in Walk(child, profile, depth + 1, ct))
                yield return found;
        }
    }

    /// <summary>
    /// Cheap, top-level-only probe: true when this exact folder has .vpx tables directly in it,
    /// or when any of the profile's own OWN candidate relative paths for Tables/VPinMAME/Popper
    /// database resolves directly under it. Reuses <see cref="Profile.Locations"/> — the exact
    /// same candidate list <see cref="LayoutDetector"/> tries first — rather than a second,
    /// hardcoded set of folder names that could silently drift from the profile (e.g. a future
    /// non-default profile using different folder names would otherwise never be found by a
    /// drive-wide scan). Deliberately shallow (no recursive fallback search per candidate) so the
    /// drive walk stays affordable; <see cref="ScanEngine.RunAcrossDrive"/> re-runs the real
    /// (deeper) <see cref="LayoutDetector.Detect"/> once a root is confirmed here.
    /// </summary>
    private static bool IsCandidateRoot(string dir, Profile profile)
    {
        try
        {
            if (Directory.EnumerateFiles(dir, "*.vpx", SearchOption.TopDirectoryOnly).Any())
                return true;

            if (profile.Locations.Tables.Any(rel => SafeAny(Path.Combine(dir, NormalizeRel(rel)), "*.vpx")))
                return true;

            if (profile.Locations.VPinMame.Any(rel => Directory.Exists(Path.Combine(dir, NormalizeRel(rel)))))
                return true;

            if (profile.Locations.PupDatabase.Any(rel => File.Exists(Path.Combine(dir, NormalizeRel(rel)))))
                return true;
        }
        catch { return false; } // unreadable: not a usable root, not a failure either
        return false;
    }

    private static string NormalizeRel(string rel) =>
        rel.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

    private static bool SafeAny(string dir, string pattern)
    {
        try { return Directory.Exists(dir) && Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly).Any(); }
        catch { return false; }
    }
}
