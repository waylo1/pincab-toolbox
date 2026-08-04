using System.Text.RegularExpressions;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Decides whether a PinUP Popper media file (POPMedia / PUPVideos) is an orphan — no longer
/// matching any installed table — without reproducing the community script's mistake.
///
/// FIELD-LOG 2026-07-29 ("Nettoyer automatiquement votre dossier PinupSystem"): a first version
/// of that script deleted still-used per-screen loading videos because its name matching didn't
/// recognise the "(SCREENx)" suffix as belonging to a known table. Shared by the scanner
/// (<see cref="Scanning.OrphanedMediaScanner"/>, informative) and the Repair action
/// (quarantine, never delete) so the two can never disagree about what counts as an orphan.
///
/// Biased on purpose towards NOT flagging: a fuzzy contains-match against any installed table
/// name is enough to clear a file. Missing a real orphan is a wasted few KB; wrongly flagging
/// one that's still in use is the incident this exists to avoid.
/// </summary>
public static class OrphanMediaMatcher
{
    private static readonly Regex ScreenSuffix = new(@"\s*\(SCREEN\d+\)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TrailingIndex = new(@"[\s_-]*\d+\s*$", RegexOptions.Compiled);

    /// <summary>
    /// True when <paramref name="fileBaseName"/> (file name, no extension) does not relate to
    /// any name in <paramref name="installedTableBaseNames"/> (table file names, no extension).
    /// </summary>
    public static bool IsOrphan(string fileBaseName, IReadOnlyCollection<string> installedTableBaseNames)
    {
        if (string.IsNullOrWhiteSpace(fileBaseName)) return false;

        // Popper's own fallback media — never a per-table file, never an orphan.
        if (fileBaseName.StartsWith("default", StringComparison.OrdinalIgnoreCase)) return false;

        var candidate = ScreenSuffix.Replace(fileBaseName, "").Trim();
        // A second pass strips a lone trailing numeric index (e.g. "TableName01"), but only
        // after the "(SCREENx)" form has already been removed — the two are not stacked.
        candidate = TrailingIndex.Replace(candidate, "").Trim();
        if (candidate.Length == 0) candidate = fileBaseName;

        foreach (var table in installedTableBaseNames)
        {
            if (string.IsNullOrWhiteSpace(table)) continue;
            if (candidate.Contains(table, StringComparison.OrdinalIgnoreCase)) return false;
            if (table.Contains(candidate, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }
}
