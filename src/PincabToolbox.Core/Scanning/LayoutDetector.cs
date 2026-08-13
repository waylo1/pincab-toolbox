using PincabToolbox.Core.Models;
using PincabToolbox.Core.Profiles;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Resolves the installation layout from a root folder: tries the profile's candidate
/// relative paths first, then falls back to a bounded recursive search.
/// </summary>
public static class LayoutDetector
{
    private const int MaxDepth = 5;

    /// <param name="vpinmameRomPathHint">
    /// Optional roms folder from VPinMAME's own configuration (normally the registry). Used as a
    /// cross-drive fallback so ROM checks are not skipped when tables and VPinMAME live on
    /// different drives (FIELD-LOG 2026-07-30). Null in tests / when unavailable; defaults to the
    /// registry value on Windows.
    /// </param>
    public static InstallLayout Detect(string rootPath, Profile profile, string? vpinmameRomPathHint = null)
    {
        var layout = new InstallLayout { RootPath = rootPath };

        layout.TablesDir = FirstExistingDir(rootPath, profile.Locations.Tables)
                           ?? FindDirContaining(rootPath, "*.vpx", MaxDepth);

        layout.VPinMameDir = FirstExistingDir(rootPath, profile.Locations.VPinMame)
                             ?? FindDirNamed(rootPath, "VPinMAME", MaxDepth);

        // VPinMAME records its roms folder in the registry; use it as a cross-drive fallback,
        // below the layout-relative locations so a normal single-drive install is unaffected.
        var registryRoms = ExistingOrNull(vpinmameRomPathHint ?? VpinmameRegistry.TryGetRomPath() ?? "");

        layout.RomsDir = FirstExistingDir(rootPath, profile.Locations.Roms)
                         ?? (layout.VPinMameDir is not null ? ExistingOrNull(Path.Combine(layout.VPinMameDir, "roms")) : null)
                         ?? FindDirNamed(rootPath, "roms", MaxDepth)
                         ?? registryRoms;

        // If the roms folder was found only via the registry (VPinMAME on another drive), derive
        // its VPinMAME dir from the parent so VPMAlias.txt and bitness checks resolve too.
        if (layout.VPinMameDir is null && layout.RomsDir is not null && layout.RomsDir == registryRoms)
            layout.VPinMameDir = Directory.GetParent(layout.RomsDir)?.FullName;

        if (layout.VPinMameDir is not null)
        {
            var alias = Path.Combine(layout.VPinMameDir, "VPMAlias.txt");
            if (File.Exists(alias)) layout.AliasFilePath = alias;
        }

        layout.PupDatabasePath = FirstExistingFile(rootPath, profile.Locations.PupDatabase)
                                 ?? FindFileNamed(rootPath, "PUPDatabase.db", MaxDepth);

        layout.PopMediaDir = FirstExistingDir(rootPath, profile.Locations.PopMedia)
                             ?? FindDirNamed(rootPath, "POPMedia", MaxDepth);

        layout.PupVideosDir = FirstExistingDir(rootPath, profile.Locations.PupVideos)
                              ?? FindDirNamed(rootPath, "PUPVideos", MaxDepth);

        if (layout.TablesDir is not null)
            layout.VpxTables.AddRange(SafeEnumerateFiles(layout.TablesDir, "*.vpx", SearchOption.TopDirectoryOnly).OrderBy(p => p));

        // Executables: root + tables dir + one level down.
        foreach (var exe in FindFilesByPattern(rootPath, "VPinballX*.exe", MaxDepth))
            layout.VpxExecutables.Add(exe);

        return layout;
    }

    private static string? ExistingOrNull(string path) => Directory.Exists(path) ? path : null;

    private static string? FirstExistingDir(string root, IEnumerable<string> candidates) =>
        candidates.Select(c => Path.Combine(root, NormalizeRel(c))).FirstOrDefault(Directory.Exists);

    private static string? FirstExistingFile(string root, IEnumerable<string> candidates) =>
        candidates.Select(c => Path.Combine(root, NormalizeRel(c))).FirstOrDefault(File.Exists);

    private static string NormalizeRel(string rel) =>
        rel.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

    private static string? FindDirContaining(string root, string pattern, int maxDepth)
    {
        foreach (var dir in SafeEnumerateDirs(root, maxDepth))
            if (SafeEnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly).Any())
                return dir;
        return null;
    }

    private static string? FindDirNamed(string root, string name, int maxDepth) =>
        SafeEnumerateDirs(root, maxDepth).FirstOrDefault(d =>
            string.Equals(Path.GetFileName(d), name, StringComparison.OrdinalIgnoreCase));

    private static string? FindFileNamed(string root, string name, int maxDepth) =>
        SafeEnumerateDirs(root, maxDepth)
            .Select(d => Path.Combine(d, name))
            .FirstOrDefault(File.Exists);

    public static IEnumerable<string> FindFilesByPattern(string root, string pattern, int maxDepth) =>
        SafeEnumerateDirs(root, maxDepth).SelectMany(d => SafeEnumerateFiles(d, pattern, SearchOption.TopDirectoryOnly));

    /// <summary>
    /// Breadth-first bounded directory walk that never throws on access errors. Never descends into
    /// <see cref="SystemNoiseDirs"/> (Recycle Bin, Windows internals, …) — see that class for why:
    /// without this, a "C:\" root walk can surface Recycle Bin remnants as if they were the real
    /// install (FIELD-LOG 13/08). The root itself is always yielded even if its own name would
    /// otherwise be noise — only its children are filtered — matching how a caller-chosen starting
    /// point is never second-guessed, only what the walk goes looking for underneath it.
    /// </summary>
    public static IEnumerable<string> SafeEnumerateDirs(string root, int maxDepth)
    {
        var queue = new Queue<(string dir, int depth)>();
        queue.Enqueue((root, 0));
        while (queue.Count > 0)
        {
            var (dir, depth) = queue.Dequeue();
            yield return dir;
            if (depth >= maxDepth) continue;
            string[] children;
            try { children = Directory.GetDirectories(dir); }
            catch { continue; }
            foreach (var c in children)
            {
                if (SystemNoiseDirs.IsNoise(c)) continue;
                queue.Enqueue((c, depth + 1));
            }
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string dir, string pattern, SearchOption opt)
    {
        try { return Directory.EnumerateFiles(dir, pattern, opt).ToArray(); }
        catch { return Array.Empty<string>(); }
    }
}
