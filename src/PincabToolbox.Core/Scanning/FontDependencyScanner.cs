using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// A2 — Font Dependency Checker (session prompt 18/08). Extracts literal <c>.ttf</c> file names a
/// table script references (<see cref="FontReferenceExtractor"/>) and flags the ones not found
/// anywhere under the scanned install — a font a DMD/scoreboard needs that this scan cannot locate
/// is a real risk of the display falling back to a default font or failing to render as intended.
///
/// <para>
/// <b>Deliberate scope decision, documented here so it is not mistaken for an oversight</b>: the
/// audit's own wording asks to "vérifie l'installation Windows" — i.e. check whether the font is
/// registered as a Windows system font. This scanner does NOT read the Windows Fonts registry key.
/// Two reasons: (1) <c>PincabToolbox.Core</c> keeps a zero-external-dependency, direct-P/Invoke
/// discipline for registry reads (see <see cref="VpinmameRegistry"/>/<see cref="DpiRegistry"/>),
/// and enumerating registry VALUES (as opposed to reading one known value) is materially more
/// P/Invoke surface than either of those; (2) this sandbox has no Windows host to exercise that
/// code path against — <c>OperatingSystem.IsWindows()</c> is false here, so any such code would
/// ship unverified, exactly the "devine au lieu de vérifier" risk this session's own brief warns
/// against. Checking "found anywhere under this install" instead is fully cross-platform, testable
/// here, and reuses the same bounded-walk infra every other scanner already trusts
/// (<see cref="LayoutDetector.FindFilesByPattern"/>). The finding text is phrased to match exactly
/// what was verified — "not found under this install" — never "not installed on Windows".
/// </para>
///
/// <para>
/// <see cref="Severity.Note"/> (ADR-010 Doctrine): extracting "this script needs this font" from a
/// literal string is a fact, but whether its absence actually breaks anything for this user is a
/// judgment call (many scoreboard fonts are optional cosmetic touches) — state the observation,
/// not a verdict.
/// </para>
/// </summary>
public sealed class FontDependencyScanner : IScanner
{
    public string Id => "font-dependency";
    public string Name => "Font Dependency Check";

    private readonly Func<string, string, int, IEnumerable<string>> _findFiles;

    /// <param name="findFiles">(root, pattern, maxDepth) → matching file paths under root. Defaults
    /// to a real bounded directory walk; injected in tests.</param>
    public FontDependencyScanner(Func<string, string, int, IEnumerable<string>>? findFiles = null)
        => _findFiles = findFiles ?? LayoutDetector.FindFilesByPattern;

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        ctx.Cancellation.ThrowIfCancellationRequested();
        var root = ctx.Layout.RootPath;
        if (string.IsNullOrEmpty(root)) yield break;

        var confirmedMissing = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var confirmedPresent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var affectedTables = 0;

        foreach (var (_, table) in ctx.Tables)
        {
            ctx.Cancellation.ThrowIfCancellationRequested();
            if (table.Script is null) continue;

            var fonts = FontReferenceExtractor.ExtractTtfFileNames(table.Script);
            if (fonts.Count == 0) continue;

            var tableHasMissingFont = false;
            foreach (var font in fonts)
            {
                if (confirmedMissing.Contains(font)) { tableHasMissingFont = true; continue; }
                if (confirmedPresent.Contains(font)) continue;

                bool found;
                try { found = _findFiles(root, font, 6).Any(); }
                catch { continue; } // unreadable subtree → don't guess, skip this font

                if (found) confirmedPresent.Add(font);
                else { confirmedMissing.Add(font); tableHasMissingFont = true; }
            }
            if (tableHasMissingFont) affectedTables++;
        }

        if (confirmedMissing.Count == 0) yield break;

        var list = confirmedMissing.ToList();
        yield return new Finding
        {
            Code = "FONT_FILE_MISSING", Severity = Severity.Note, Category = Id,
            Subject = list.Count == 1 ? list[0] : $"{list.Count} fonts",
            Args = new[] { affectedTables.ToString(), list.Count.ToString(), string.Join(", ", list.Take(5)) },
            EnglishText = $"{affectedTables} table(s) reference {list.Count} font file(s) ({string.Join(", ", list.Take(5))}{(list.Count > 5 ? "…" : "")}) that weren't found anywhere under this install — the DMD/scoreboard display may fall back to a default font or fail to render as intended.",
            FixHint = "If these are custom scoreboard/DMD fonts, install them (right-click the .ttf → Install) or place them somewhere under this install so a future scan can find them.",
        };
    }
}
