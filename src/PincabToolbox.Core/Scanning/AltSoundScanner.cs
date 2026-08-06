using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Flags AltSound sample packs whose <c>altsound.csv</c> manifest references sound files that
/// don't actually exist under <c>VPinMAME/altsound/&lt;rom&gt;/</c> — the classic "some cues stay
/// silent" AltSound failure mode, distinct from AltColor's "DMD shows in mono" (audit §4/B2).
///
/// <para>
/// Scoped, like <see cref="AltColorScanner"/> and <see cref="CompletenessScanner"/>'s PUP-Pack
/// check, to ROMs a present table actually requires (<see cref="ScriptAnalyzer.AnalyzeRomUsage"/>) —
/// never a blind sweep of the whole altsound/ folder, so an unused leftover pack never generates
/// noise.
/// </para>
///
/// <para>
/// Out of scope on purpose, both logged in FIELD-LOG rather than silently dropped: (1) the legacy
/// <c>.ini</c>-based ("g-sound") AltSound format the audit fiche also mentions — no concrete schema
/// was available to verify against (unlike the CSV format, confirmed against the community "How to
/// create a new altsound project" guide), and inventing one would sacrifice the zero-FP determinism
/// this Warning-severity check's ship decision rests on; (2) reporting CSV syntax errors as a signal
/// distinct from missing samples — the handoff sanctions exactly one finding code
/// (<c>ALTSOUND_SAMPLE_MISSING</c>) for B2, and <see cref="AltSoundManifestLinter"/> already treats
/// an unparseable manifest or a malformed row as "nothing to report" rather than a defect (a
/// half-authored/placeholder row is a legitimate authoring choice — the project-wide bias toward
/// silence over a guessed false positive).
/// </para>
/// </summary>
public sealed class AltSoundScanner : IScanner
{
    public string Id => "altsound";
    public string Name => "AltSound Structural Linter";

    private const int MaxExamplesShown = 8;

    private readonly Func<string, string?> _readCsv;
    private readonly Func<string, bool> _fileExists;

    /// <param name="readCsv">Given an altsound.csv path, returns its text, or null when missing/unreadable. Defaults to a real file read.</param>
    /// <param name="fileExists">Given a sample's full path, returns whether it exists. Defaults to a real disk check.</param>
    public AltSoundScanner(Func<string, string?>? readCsv = null, Func<string, bool>? fileExists = null)
    {
        _readCsv = readCsv ?? ReadFileOrNull;
        _fileExists = fileExists ?? File.Exists;
    }

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        if (ctx.Layout.VPinMameDir is null) yield break;
        var altsoundRoot = Path.Combine(ctx.Layout.VPinMameDir, "altsound");

        // Only ROMs a present table genuinely requires — same anti-FP shape as AltColorScanner.
        var requiredRoms = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in ctx.Tables.Values)
        {
            if (table.Script is null) continue;
            var rom = ScriptAnalyzer.AnalyzeRomUsage(table.Script);
            if (rom.UsesController && rom.Primary is not null) requiredRoms.Add(rom.Primary);
        }

        foreach (var rom in requiredRoms)
        {
            ctx.Cancellation.ThrowIfCancellationRequested();
            var romDir = Path.Combine(altsoundRoot, rom);
            var csvPath = Path.Combine(romDir, "altsound.csv");

            string? csvText;
            try { csvText = _readCsv(csvPath); }
            catch { continue; } // unreadable → silence, never a false positive

            if (csvText is null) continue; // no altsound.csv for this ROM — not a defect

            var referenced = AltSoundManifestLinter.ExtractReferencedSamples(csvText);
            if (referenced.Count == 0) continue; // empty or unrecognised manifest — nothing to check

            // Distinct file names: duplicate IDs are normal (the engine random-picks a variant per
            // cue) and must not inflate "referenced" / "missing" counts for the same physical file.
            var samples = referenced.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var missing = new List<string>();
            foreach (var sample in samples)
            {
                ctx.Cancellation.ThrowIfCancellationRequested();
                var relative = sample.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                var samplePath = Path.Combine(romDir, relative);

                bool exists;
                try { exists = _fileExists(samplePath); }
                catch { exists = true; } // unreadable check → don't guess a defect, skip this one sample
                if (!exists) missing.Add(sample);
            }

            if (missing.Count == 0) continue;

            var examples = string.Join(", ", missing.Take(MaxExamplesShown));
            yield return new Finding
            {
                Code = "ALTSOUND_SAMPLE_MISSING", Severity = Severity.Warning, Category = Id,
                Subject = rom, FilePath = csvPath,
                Args = new[] { rom, missing.Count.ToString(), samples.Count.ToString(), examples },
                EnglishText = $"'{rom}' altsound.csv references {missing.Count} of {samples.Count} sample(s) that don't exist under altsound/{rom}/ — those cues will stay silent, or the AltSound plugin may fail to load altogether.",
                FixHint = $"Re-extract the AltSound package for '{rom}' into altsound/{rom}/ — a partial extraction is the most common cause of missing samples. If you edited altsound.csv by hand, double-check the FNAME column against the files actually on disk.",
            };
        }
    }

    private static string? ReadFileOrNull(string path)
        => File.Exists(path) ? File.ReadAllText(path) : null;
}
