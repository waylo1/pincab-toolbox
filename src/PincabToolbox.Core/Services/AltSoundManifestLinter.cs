using System.Text;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Hand-rolled, dependency-free CSV reader for AltSound's <c>altsound.csv</c> manifest, plus the
/// pure extraction of every sample file it references.
///
/// <para>
/// Header confirmed against the community "How to create a new altsound project" guide (VPINBALL.COM
/// forums): <c>"ID","CHANNEL","DUCK","GAIN","LOOP","STOP","NAME","FNAME"</c> — comma-delimited,
/// double-quoted fields; only <c>ID</c>, <c>NAME</c> and <c>FNAME</c> are required, the rest may be
/// blank; several rows may share one <c>ID</c> (the engine picks one at random for variety) and each
/// such row still names a real file. Only <c>FNAME</c> — the referenced sample path — matters here:
/// this linter's whole purpose is finding samples the manifest promises that the folder doesn't have.
/// </para>
///
/// <para>
/// Biased to silence on anything that isn't a clean read: a header without an <c>FNAME</c> column
/// means this isn't a recognisable altsound.csv (a future format revision, a hand-crafted file for a
/// different tool) — read nothing rather than guess. A data row with fewer fields than the header,
/// or a blank <c>FNAME</c> value, is skipped rather than reported: a placeholder/disabled row is a
/// legitimate authoring choice, not a defect. Only a genuinely referenced-but-absent file is this
/// linter's business, and that decision belongs to <see cref="Scanning.AltSoundScanner"/> (it alone
/// knows whether the file exists on disk).
/// </para>
/// </summary>
public static class AltSoundManifestLinter
{
    public static List<string> ExtractReferencedSamples(string csvText)
    {
        var samples = new List<string>();
        var lines = csvText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        var headerSeen = false;
        var fnameIndex = -1;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;

            var fields = SplitCsvLine(line);

            if (!headerSeen)
            {
                headerSeen = true;
                fnameIndex = fields.FindIndex(f => string.Equals(f.Trim(), "FNAME", StringComparison.OrdinalIgnoreCase));
                if (fnameIndex < 0) return new List<string>(); // not a recognisable altsound.csv — read nothing
                continue;
            }

            if (fnameIndex >= fields.Count) continue; // short row — skip silently, not a reported defect
            var fname = fields[fnameIndex].Trim();
            if (fname.Length > 0) samples.Add(fname);
        }

        return samples;
    }

    /// <summary>Splits one CSV line into fields, honouring double-quoted fields with "" as an escaped quote inside them. No external dependency.</summary>
    internal static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
        }
        fields.Add(sb.ToString());
        return fields;
    }
}
