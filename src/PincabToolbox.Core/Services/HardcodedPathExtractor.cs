using System.Text.RegularExpressions;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Pure extraction for the A3 "Hardcoded-Path Linter" (session prompt 18/08): pulls literal,
/// quoted absolute Windows paths (<c>"C:\Users\someone-else\..."</c>-shaped) out of a table
/// script — a drive letter, at least one backslash-separated segment, and a file-like extension at
/// the end, so this only matches something that plausibly names a FILE, not a bare folder.
///
/// <para>
/// Comments are stripped first via <see cref="ScriptAnalyzer.StripComments"/> for the same reason
/// as <see cref="FontReferenceExtractor"/> — a commented-out path must not read as live.
/// </para>
/// </summary>
public static class HardcodedPathExtractor
{
    private static readonly Regex AbsolutePathLiteral = new(
        @"""([A-Za-z]:\\[^""\r\n]*\.[A-Za-z0-9]{1,6})""",
        RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    /// <summary>Distinct absolute path literals found in the script. Empty when none, or on a
    /// regex timeout (never throws).</summary>
    public static IReadOnlyList<string> ExtractAbsolutePaths(string script)
    {
        var stripped = ScriptAnalyzer.StripComments(script);

        MatchCollection matches;
        try { matches = AbsolutePathLiteral.Matches(stripped); }
        catch (RegexMatchTimeoutException) { return Array.Empty<string>(); }

        var paths = new List<string>();
        foreach (Match m in matches)
        {
            var raw = m.Groups[1].Value.Trim();
            if (raw.Length == 0) continue;
            if (!paths.Contains(raw, StringComparer.OrdinalIgnoreCase)) paths.Add(raw);
        }
        return paths;
    }
}
