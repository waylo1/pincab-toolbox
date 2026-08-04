namespace PincabToolbox.Core.Services;

/// <summary>
/// Removes the user's identity from anything that leaves the machine.
///
/// The whole product concept is "run the scan, paste the report on the forum". A report is
/// therefore a PUBLIC document, and an absolute Windows path carries the account name.
/// Leaking it is a trust incident, not a cosmetic detail — see ADR-003.
///
/// Pure and deterministic: the user name is passed in rather than read from the environment,
/// so this is fully testable and behaves the same on every OS.
/// </summary>
public static class PathScrubber
{
    public const string Placeholder = "<user>";

    /// <summary>Below this length, scrubbing a bare user name would mangle ordinary words.</summary>
    private const int MinimumUserNameLength = 3;

    private static readonly (string Marker, char Separator)[] HomeMarkers =
    {
        (@"\Users\", '\\'),
        ("/Users/", '/'),
        (@"\home\", '\\'),
        ("/home/", '/'),
        (@"\Documents and Settings\", '\\'),
    };

    /// <summary>
    /// Scrubs a single path or a whole report.
    /// </summary>
    /// <param name="text">Text that may contain absolute paths.</param>
    /// <param name="userName">
    /// Current account name, when known. Catches the case where the name appears outside a
    /// home folder — a table folder named after its owner, for instance.
    /// </param>
    public static string Scrub(string? text, string? userName = null)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";

        var result = ScrubHomeFolders(text);

        if (!string.IsNullOrWhiteSpace(userName) && userName!.Trim().Length >= MinimumUserNameLength)
            result = ReplaceCaseInsensitive(result, userName.Trim(), Placeholder);

        return result;
    }

    /// <summary>True when the text still exposes an account name — used as a release guard in tests.</summary>
    public static bool LeaksIdentity(string? text, string? userName = null)
        => !string.IsNullOrEmpty(text) && Scrub(text, userName) != text;

    private static string ScrubHomeFolders(string text)
    {
        foreach (var (marker, separator) in HomeMarkers)
        {
            var searchFrom = 0;
            while (true)
            {
                var start = text.IndexOf(marker, searchFrom, StringComparison.OrdinalIgnoreCase);
                if (start < 0) break;

                var nameStart = start + marker.Length;
                if (nameStart >= text.Length) break;

                // The account segment ends at the next separator, or at the first character
                // that cannot belong to a folder name (quote, comma, end of line…).
                var nameEnd = nameStart;
                while (nameEnd < text.Length
                       && text[nameEnd] != separator
                       && text[nameEnd] != '\\' && text[nameEnd] != '/'
                       && !char.IsControl(text[nameEnd])
                       && text[nameEnd] != '"' && text[nameEnd] != '\'' && text[nameEnd] != ',')
                    nameEnd++;

                if (nameEnd == nameStart) { searchFrom = nameStart; continue; }   // already scrubbed

                text = text[..nameStart] + Placeholder + text[nameEnd..];
                searchFrom = nameStart + Placeholder.Length;
            }
        }
        return text;
    }

    private static string ReplaceCaseInsensitive(string haystack, string needle, string replacement)
    {
        var sb = new System.Text.StringBuilder();
        var i = 0;
        while (true)
        {
            var found = haystack.IndexOf(needle, i, StringComparison.OrdinalIgnoreCase);
            if (found < 0) { sb.Append(haystack, i, haystack.Length - i); break; }
            sb.Append(haystack, i, found - i).Append(replacement);
            i = found + needle.Length;
        }
        return sb.ToString();
    }
}
