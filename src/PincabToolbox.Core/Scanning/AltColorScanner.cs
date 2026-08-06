using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Flags an incomplete AltColor/Serum colorization set: files exist under
/// <c>VPinMAME/altcolor/&lt;rom&gt;/</c> for a ROM a present table actually requires, but they don't
/// form one of the two recognised complete pairs — the DMD is likely to show in mono, or the
/// colorization plugin fails to load it at all.
///
/// <para>
/// Scoped deliberately to ROMs a table in this install actually uses (reusing
/// <see cref="ScriptAnalyzer.AnalyzeRomUsage"/>, the same signal <see cref="RomValidatorScanner"/>
/// and <see cref="CompletenessScanner"/> already rely on for their own ROM matching) — never the
/// whole <c>altcolor/</c> folder, so a leftover colorization set for a ROM nobody plays never
/// generates noise (audit §4/B1 FP discipline).
/// </para>
///
/// <para>
/// Out of scope on purpose: the audit's B1 fiche also mentions "32/64-bit DLL concordance" for the
/// colorization engine. No concrete, distinct DLL name is specified anywhere for that beyond what
/// <see cref="BitnessScanner"/> already checks (dmddevice64.dll) — inventing a filename to check
/// without evidence would trade the deterministic, zero-FP nature of this check for a guess. Left
/// out; logged in FIELD-LOG rather than silently dropped.
/// </para>
/// </summary>
public sealed class AltColorScanner : IScanner
{
    public string Id => "altcolor";
    public string Name => "AltColor / Serum Integrity";

    private readonly Func<string, IReadOnlyCollection<string>> _listExtensions;

    /// <param name="listExtensions">
    /// Given a ROM's altcolor folder path, returns the lower-case extensions of the files directly
    /// inside it (empty when the folder doesn't exist). Defaults to a real directory listing;
    /// injected in tests.
    /// </param>
    public AltColorScanner(Func<string, IReadOnlyCollection<string>>? listExtensions = null)
        => _listExtensions = listExtensions ?? ListExtensionsOnDisk;

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        if (ctx.Layout.VPinMameDir is null) yield break;
        var altcolorRoot = Path.Combine(ctx.Layout.VPinMameDir, "altcolor");

        // Only ROMs a present table genuinely requires (UsesController + a resolved candidate) —
        // never the raw folder listing. Same anti-FP shape as CompletenessScanner's PUP-Pack check.
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
            var romFolder = Path.Combine(altcolorRoot, rom);

            IReadOnlyCollection<string> exts;
            try { exts = _listExtensions(romFolder); }
            catch { continue; } // unreadable → silence, never a false positive

            if (exts.Count == 0) continue; // no colorization attempted for this ROM — not a defect
            if (AltColorInspector.IsComplete(exts)) continue;

            yield return new Finding
            {
                Code = "ALTCOLOR_INCOMPLETE", Severity = Severity.Warning, Category = Id,
                Subject = rom, FilePath = romFolder,
                Args = new[] { rom },
                EnglishText = $"'{rom}' has an incomplete AltColor/Serum colorization set — files exist under altcolor/{rom}/ but don't form a full pair (.vni+.pal, or a Serum file+.pal). The DMD is likely to show in mono, or the colorization won't load at all.",
                FixHint = $"Re-download the colorization set for '{rom}' and extract every file it ships with into altcolor/{rom}/ — a partial extraction (e.g. only the .pal, or only the .vni) is the most common cause.",
            };
        }
    }

    private static IReadOnlyCollection<string> ListExtensionsOnDisk(string romFolder)
    {
        if (!Directory.Exists(romFolder)) return Array.Empty<string>();
        return Directory.EnumerateFiles(romFolder)
            .Select(f => Path.GetExtension(f).ToLowerInvariant())
            .ToList();
    }
}
