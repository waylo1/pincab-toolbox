using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// LOT F (spec 10/08) — <see cref="ScreenTopologyScanner"/> requires an explicit <c>#&#160;V2</c>
/// marker (<see cref="ScreenTopologyAnalyzer"/> scope cut #1) and stays totally silent without it,
/// on purpose: the pre-2.0.0 format has a real, unresolvable ambiguity (Backglass and Background
/// blocks can silently swap meaning) that this project refuses to guess at. Research shows ≥5
/// independent discussions about broken <c>ScreenRes.txt</c> files through July 2026 — many on old
/// installs that predate the marker. Total silence there reads as "everything was checked and is
/// fine", which is worse than honestly saying "present, but not in a format I can verify".
///
/// <para>
/// A NEW scanner, deliberately — <see cref="ScreenTopologyScanner"/> is not touched (spec §3.1 rule
/// 5). Reuses <see cref="ScreenTopologyAnalyzer.ParseBackglassPlacement"/> exactly as-is rather than
/// re-parsing: a file this scanner reports on is, by construction, a file where that call already
/// returned null while the file itself was present and readable — the two scanners can never both
/// fire for the same file (one needs a successful parse, the other needs a failed one).
/// </para>
///
/// <para>
/// Deliberately makes zero claim about the file's actual content — no "this looks broken", no
/// suggested fix beyond re-generating it. <see cref="Severity.Note"/>, never higher: this is honesty
/// about the tool's own limits, not a defect report.
/// </para>
/// </summary>
public sealed class ScreenResUnparsedScanner : IScanner
{
    public string Id => "screenres-format";
    public string Name => "ScreenRes.txt Format Honesty";

    private readonly Func<string, string?> _readText;

    /// <param name="readText">Given a ScreenRes.txt/.res path, returns its text, or null when missing/unreadable. Defaults to a real file read.</param>
    public ScreenResUnparsedScanner(Func<string, string?>? readText = null)
        => _readText = readText ?? (p => File.Exists(p) ? File.ReadAllText(p) : null);

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        if (ctx.Layout.TablesDir is null) yield break;

        var globalPath = Path.Combine(ctx.Layout.TablesDir, "ScreenRes.txt");
        var globalFinding = Evaluate(globalPath, "ScreenRes.txt");
        if (globalFinding is not null) yield return globalFinding;

        foreach (var path in ctx.Tables.Keys.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            ctx.Cancellation.ThrowIfCancellationRequested();
            var baseName = Path.GetFileNameWithoutExtension(path);
            var resPath = Path.Combine(ctx.Layout.TablesDir, baseName + ".res");
            var finding = Evaluate(resPath, baseName);
            if (finding is not null) yield return finding;
        }
    }

    private Finding? Evaluate(string path, string subject)
    {
        string? text;
        try { text = _readText(path); } catch { return null; }
        if (text is null) return null; // no file at all — nothing to be honest about

        // Already parseable -> ScreenTopologyScanner owns this file's story, stay out of its way.
        if (ScreenTopologyAnalyzer.ParseBackglassPlacement(text) is not null) return null;

        return new Finding
        {
            Code = "SCREENRES_UNPARSED", Severity = Severity.Note, Category = Id,
            Subject = subject, FilePath = path,
            Args = new[] { subject },
            EnglishText = $"'{Path.GetFileName(path)}' is present but not in a format this tool can verify (no '# V2' marker, or an unrecognised layout) — its backglass/DMD position is not checked, and this is not a claim that anything is wrong with it.",
            FixHint = "If your backglass position looks wrong, re-run B2S_ScreenResIdentifier (or your ScreenRes editor) to regenerate the file in the current format.",
        };
    }
}
