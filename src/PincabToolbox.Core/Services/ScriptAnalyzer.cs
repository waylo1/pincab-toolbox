using System.Text.RegularExpressions;

namespace PincabToolbox.Core.Services;

/// <summary>Result of analysing a table script for ROM usage.</summary>
public sealed class RomRequirement
{
    /// <summary>All ROM set names the script may use (some tables offer alternates).</summary>
    public List<string> Candidates { get; } = new();

    /// <summary>
    /// True when the script creates a VPinMAME controller. This — and only this — means a
    /// VPinMAME ROM is genuinely required. Opening a B2S backglass does NOT (see
    /// <see cref="UsesB2S"/>): originals/homebrew routinely use B2S for their backglass with no
    /// ROM. Treating B2S as a ROM signal was the KPI#1 false positive (FIELD-LOG 2026-07-30).
    /// </summary>
    public bool UsesController { get; set; }

    /// <summary>True when the script opens a B2S backglass server. Not a ROM requirement on its own.</summary>
    public bool UsesB2S { get; set; }

    public string? Primary => Candidates.Count > 0 ? Candidates[0] : null;
}

/// <summary>Extracts ROM requirements from VPX VBScript.</summary>
public static partial class ScriptAnalyzer
{
    [GeneratedRegex("""(?im)^\s*Const\s+cGameName\s*=\s*"([^"]+)"\s*""")]
    private static partial Regex ConstGameName();

    [GeneratedRegex("""(?im)^\s*(?!Const)\w*\s*cGameName\s*=\s*"([^"]+)"\s*""")]
    private static partial Regex AssignGameName();

    [GeneratedRegex("""(?i)\.GameName\s*=\s*"([^"]+)"\s*""")]
    private static partial Regex DirectGameName();

    [GeneratedRegex("""(?i)CreateObject\(\s*"VPinMAME\.Controller"\s*\)""")]
    private static partial Regex VpinmameCreate();

    [GeneratedRegex("""(?i)CreateObject\(\s*"B2S\.Server"\s*\)""")]
    private static partial Regex B2SCreate();

    /// <summary>
    /// Removes VBScript comments (<c>'</c> and <c>REM</c> to end of line) without touching string
    /// literals, so that dead code cannot be read as a declaration.
    ///
    /// <para>
    /// This is the second KPI#1 false-positive source, and the one that survived the first fix.
    /// Original/homebrew tables are overwhelmingly built from a ROM-table template, and the
    /// VPinMAME boilerplate is commented out rather than deleted:
    /// <code>
    /// ' Set Controller = CreateObject("VPinMAME.Controller")
    /// </code>
    /// The CreateObject regex is unanchored, so it matched inside that comment, the table read as
    /// "drives VPinMAME", and a genuine original got a critical ROM_MISSING. Gregg's list of
    /// "criticals I think are originals without a ROM" (FB, Virtual Pinball and VPin Cab Builders,
    /// 2026-08-03) is exactly this shape.
    /// </para>
    ///
    /// <para>
    /// String-awareness matters: VBScript has no escape character — a quote inside a literal is
    /// written <c>""</c> — and an apostrophe inside a table name (<c>"Rocky &amp; Bullwinkle's"</c>)
    /// must not start a comment. Tracking quote parity handles both, since a doubled quote reads
    /// as close-then-reopen and lands on the same parity.
    /// </para>
    /// </summary>
    public static string StripComments(string script)
    {
        var sb = new System.Text.StringBuilder(script.Length);

        foreach (var rawLine in script.Split('\n'))
        {
            var line = rawLine;
            var inString = false;
            var cut = -1;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"') { inString = !inString; continue; }
                if (inString) continue;

                if (c == '\'') { cut = i; break; }

                // REM must be a standalone word: "REM x", not "REMOVE" or "aREM".
                if ((c is 'R' or 'r')
                    && i + 3 <= line.Length
                    && line.AsSpan(i, 3).Equals("REM", StringComparison.OrdinalIgnoreCase)
                    && (i == 0 || !char.IsLetterOrDigit(line[i - 1]) && line[i - 1] != '_')
                    && (i + 3 == line.Length || !char.IsLetterOrDigit(line[i + 3]) && line[i + 3] != '_'))
                {
                    cut = i;
                    break;
                }
            }

            sb.Append(cut >= 0 ? line[..cut] : line).Append('\n');
        }

        return sb.ToString();
    }

    public static RomRequirement AnalyzeRomUsage(string script)
    {
        // Dead code is not a declaration. See StripComments.
        script = StripComments(script);

        var result = new RomRequirement
        {
            UsesController = VpinmameCreate().IsMatch(script),
            UsesB2S = B2SCreate().IsMatch(script),
        };

        void Add(string name)
        {
            name = name.Trim();
            if (name.Length > 0 && !result.Candidates.Contains(name, StringComparer.OrdinalIgnoreCase))
                result.Candidates.Add(name);
        }

        foreach (Match m in ConstGameName().Matches(script)) Add(m.Groups[1].Value);
        foreach (Match m in AssignGameName().Matches(script)) Add(m.Groups[1].Value);
        // .GameName only counts when nothing else matched — avoids picking up B2S table names.
        if (result.Candidates.Count == 0)
            foreach (Match m in DirectGameName().Matches(script)) Add(m.Groups[1].Value);

        return result;
    }
}
