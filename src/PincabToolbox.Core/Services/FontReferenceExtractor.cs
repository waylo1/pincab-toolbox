using System.Text.RegularExpressions;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Pure extraction for the A2 "Font Dependency Checker" (session prompt 18/08): pulls literal,
/// quoted <c>.ttf</c> file name references out of a table script — never a guessed or inferred
/// name (HANDOFF's own instruction: "Aucune devinette de nom").
///
/// <para>
/// Comments are stripped first via <see cref="ScriptAnalyzer.StripComments"/> — the same
/// KPI#1-shaped trap ScriptAnalyzer.AnalyzeRomUsage guards against: a commented-out font reference
/// left over from a template must not read as a live dependency.
/// </para>
/// </summary>
public static class FontReferenceExtractor
{
    // A quoted string ending in ".ttf" (case-insensitive) — deliberately simple: matches whatever
    // literal the script actually names, with no assumption about a "Fonts\" prefix or any other
    // directory convention (those vary by table author and would be a guess).
    private static readonly Regex TtfLiteral = new(
        @"""([^""]*?\.ttf)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    /// <summary>
    /// Distinct .ttf file names (base name only — any path prefix in the literal is stripped, by
    /// hand, on both separators, since a script may embed a Windows-style path) referenced by the
    /// script. Empty when none, or on a regex timeout (never throws).
    /// </summary>
    public static IReadOnlyList<string> ExtractTtfFileNames(string script)
    {
        var stripped = ScriptAnalyzer.StripComments(script);

        MatchCollection matches;
        try { matches = TtfLiteral.Matches(stripped); }
        catch (RegexMatchTimeoutException) { return Array.Empty<string>(); }

        var names = new List<string>();
        foreach (Match m in matches)
        {
            var raw = m.Groups[1].Value.Trim();
            if (raw.Length == 0) continue;

            var cut = raw.LastIndexOfAny(new[] { '/', '\\' });
            var fileName = cut >= 0 ? raw[(cut + 1)..] : raw;

            // ".ttf" alone (no real base name) is not an identifiable file — ambiguous, skip it
            // rather than guess.
            if (fileName.Length <= 4) continue;

            if (!names.Contains(fileName, StringComparer.OrdinalIgnoreCase)) names.Add(fileName);
        }
        return names;
    }
}
