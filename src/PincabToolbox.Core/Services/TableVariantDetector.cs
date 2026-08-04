using System.Text.RegularExpressions;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Recognises a table file that is a third-party derivative of a base table rather than the base
/// table itself.
///
/// <para>
/// Why this exists: update checking matches a local file to a VPS game by name + year, then
/// compares version numbers. A derivative carries the same name and year as its base table but
/// versions independently, so the comparison is meaningless — and it reliably produces "you have
/// v1.2, v3.0 is available" on a file that has no v3.0. Two independent reports on the same day
/// (Chad Greenaway, FB: "wish it had filters like avoid biggus mods"; Gregg, FB Virtual Pinball
/// and VPin Cab Builders) plus FD's earlier renaming report all reduce to this. FIELD-LOG
/// 2026-07-31 / 2026-08-03.
/// </para>
///
/// <para>
/// Deliberately narrow. Only markers with direct field evidence are listed, and the check is
/// biased toward NOT classifying: a missed derivative is one noisy Info line, while a
/// misclassified base table silently hides a real update — the more expensive mistake. Variants
/// that share the base table's own versioning (FSS, VR room, hybrid builds, resolution variants)
/// are intentionally absent: they are usually shipped by the original author under the same
/// version, so suppressing them would hide genuine updates.
/// </para>
/// </summary>
public static partial class TableVariantDetector
{
    /// <summary>
    /// Standalone tokens that mark a third-party derivative. Compared case-insensitively against
    /// whole tokens only, never as substrings — "Modern Times", "Bigger Bang" and similar base
    /// titles must not match.
    /// </summary>
    private static readonly string[] Markers =
    {
        "MOD",      // universal community convention for a derivative build
        "BIGUS",    // named explicitly by Chad Greenaway and by Gregg, 2026-08-03
        "BIGGUS",   // the same modder's tag, both spellings seen in the wild
    };

    /// <summary>
    /// Strips only the "(Manufacturer Year)" group — identified by the four-digit year, because a
    /// manufacturer or author name must never be able to trip the detector. Other parenthesised
    /// groups are kept and searched: "(MOD)" as a standalone tag is one of the shapes reported.
    /// </summary>
    [GeneratedRegex(@"\([^)]*\b\d{4}\b[^)]*\)")]
    private static partial Regex ManufacturerYearGroup();

    [GeneratedRegex(@"[^A-Za-z0-9]+")]
    private static partial Regex NonAlnum();

    /// <summary>
    /// Returns the marker found, or null when the name does not look like a derivative.
    /// The marker is returned rather than a bool so the report can say WHY a table was skipped —
    /// an unexplained omission is indistinguishable from a bug.
    /// </summary>
    public static string? DetectDerivativeMarker(string fileNameWithoutExt)
    {
        if (string.IsNullOrWhiteSpace(fileNameWithoutExt)) return null;

        var outsideParens = ManufacturerYearGroup().Replace(fileNameWithoutExt, " ");

        foreach (var token in NonAlnum().Split(outsideParens))
        {
            if (token.Length == 0) continue;
            foreach (var marker in Markers)
                if (token.Equals(marker, StringComparison.OrdinalIgnoreCase))
                    return marker;
        }

        return null;
    }

    /// <summary>Convenience predicate.</summary>
    public static bool IsDerivative(string fileNameWithoutExt)
        => DetectDerivativeMarker(fileNameWithoutExt) is not null;
}
