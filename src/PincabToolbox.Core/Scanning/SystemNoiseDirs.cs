namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Folder names that are never worth walking into when looking for a pincab install: Windows and
/// package-manager internals, the Recycle Bin, dev-tooling caches. Single source of truth for this
/// list — it started as a private set inside <see cref="DriveInstallFinder"/> only (10/08), which
/// meant <see cref="LayoutDetector.SafeEnumerateDirs"/> had no such guard at all. Confirmed on a
/// real disk (13/08, Maxime's cab, three separate full-C:\ scans): with no guard, a breadth-first
/// walk from "C:\" can reach "$Recycle.Bin\&lt;SID&gt;\" — which after a deletion holds
/// "$R&lt;random&gt;.vpx" remnants that still have the original extension — before it reaches the
/// real "Tables\" folder, so <see cref="LayoutDetector.Detect"/> reports the Recycle Bin as the
/// tables directory instead of the real one. Applying the same exclusion everywhere
/// <see cref="LayoutDetector.SafeEnumerateDirs"/> is used (LayoutDetector itself, BlockedFileScanner,
/// CompletenessScanner) closes that gap at its one real source instead of teaching each caller about
/// it separately, which is exactly how it went missing from LayoutDetector in the first place.
/// </summary>
public static class SystemNoiseDirs
{
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Windows", "$Recycle.Bin", "System Volume Information", "WindowsApps",
        "Package Cache", "node_modules", ".git", "$WinREAgent", "Recovery",
        "PerfLogs", "AppData", "ProgramData",
    };

    /// <summary>
    /// True when this path's own folder name is one of <see cref="Names"/> — never walk into it.
    /// Splits on both separators by hand rather than <see cref="Path.GetFileName(string)"/> — off
    /// Windows, <c>System.IO.Path</c> does not treat <c>\</c> as a separator, so a Windows-style
    /// path would come back whole and never match (same trap documented on
    /// <see cref="PincabToolbox.Core.Scanning.BlockedFileScanner.SeverityFor"/>).
    /// </summary>
    public static bool IsNoise(string dirPath)
    {
        var cut = dirPath.LastIndexOfAny(new[] { '/', '\\' });
        var name = cut >= 0 ? dirPath[(cut + 1)..] : dirPath;
        return Names.Contains(name);
    }
}
