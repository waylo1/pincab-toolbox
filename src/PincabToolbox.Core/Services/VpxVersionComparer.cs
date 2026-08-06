using System.Text.RegularExpressions;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Pure version arithmetic for the VPX version comparator (<see cref="Scanning.VpxVersionScanner"/>).
/// Kept separate and dependency-free so every decision is unit-testable with plain strings — the
/// installed version is read from a PE resource in the scanner, never here.
///
/// <para>
/// Discipline mirrors <c>CompatibilityScanner</c>'s treatment of <c>COMPAT_MIN_VERSION</c>: a required
/// version is a heuristic declaration extracted from a table's script, so anything we cannot parse
/// cleanly must resolve to "no opinion" (silence), never to a defect. A missing/unreadable installed
/// version, an ambiguous string, or a version without an explicit minor all return false so the scanner
/// stays silent — the July-30 false positive (FIELD-LOG 2026-07-30) is never reintroduced.
/// </para>
/// </summary>
public static class VpxVersionComparer
{
    // First "major.minor" integer pair in the string, after any leading label ("v", "Visual Pinball ").
    // An explicit minor is required: "10" alone is ambiguous and must read as undetectable, not "10.0".
    private static readonly Regex MajorMinor =
        new(@"(\d+)\.(\d+)", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Parses the first <c>major.minor</c> found in <paramref name="raw"/>. Returns false — leaving both
    /// out params at 0 — for null/blank input, no dotted pair, or an unparseable/oversized number.
    /// </summary>
    public static bool TryParseMajorMinor(string? raw, out int major, out int minor)
    {
        major = 0;
        minor = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        Match m;
        try { m = MajorMinor.Match(raw); }
        catch (RegexMatchTimeoutException) { return false; }
        if (!m.Success) return false;

        // Non-short-circuit & so both groups are always attempted; either failing means "undetectable".
        return int.TryParse(m.Groups[1].Value, out major)
             & int.TryParse(m.Groups[2].Value, out minor);
    }

    /// <summary>
    /// Highest <c>major.minor</c> among candidate version strings (typically one per installed VPX
    /// executable). Returns false when none is parseable — the caller must then stay silent, having no
    /// trustworthy installed version to compare against. Taking the highest is deliberate: if ANY
    /// installed VPX satisfies a table's requirement the user can launch it there, so a stray older
    /// build never manufactures a false shortfall.
    /// </summary>
    public static bool TryHighestInstalled(IEnumerable<string?> candidates, out int major, out int minor)
    {
        major = 0;
        minor = 0;
        var found = false;
        foreach (var candidate in candidates)
        {
            if (!TryParseMajorMinor(candidate, out var mj, out var mn)) continue;
            if (!found || mj > major || (mj == major && mn > minor))
            {
                major = mj;
                minor = mn;
            }
            found = true;
        }
        return found;
    }

    /// <summary>
    /// True only when the installed version is STRICTLY below the required version (major, then minor).
    /// Equal or newer returns false — the whole point is to never flag a healthy install.
    /// </summary>
    public static bool IsOutdated(int installedMajor, int installedMinor, int requiredMajor, int requiredMinor)
        => installedMajor < requiredMajor
           || (installedMajor == requiredMajor && installedMinor < requiredMinor);
}
