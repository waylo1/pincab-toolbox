using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// LOT D (spec 10/08) — "present but not enabled". <see cref="AltSoundScanner"/> and
/// <see cref="AltColorScanner"/> both verify the *files* are correct; neither looks at whether
/// VPinMAME is actually configured to use them. A user with a perfectly installed AltSound pack who
/// simply never flipped the in-game "Alt Sound Mode" option gets an all-green report and still hears
/// nothing — this closes exactly that gap, and only that gap (spec §2's anti-duplication map).
///
/// <para>
/// A NEW scanner, deliberately — neither <see cref="AltSoundScanner"/> nor
/// <see cref="AltColorScanner"/> is touched (spec §3.1 rule 5). Reuses their exact "which ROM is
/// actually required" signal (<see cref="ScriptAnalyzer.AnalyzeRomUsage"/>) and, for AltColor,
/// <see cref="AltColorInspector.IsComplete"/> — no new file-completeness logic invented here.
/// </para>
///
/// <para>
/// Both findings are <see cref="Severity.Note"/>, never higher: a user can legitimately have
/// installed a pack without wanting it active yet (spec explicit). Both degrade to silence, never a
/// guess, whenever the registry value can't be read at all (<see cref="AltFeatureRegistry"/> returns
/// null) — an absent/unreadable value means "don't know", not "disabled".
/// </para>
/// </summary>
public sealed class FeatureEnabledScanner : IScanner
{
    public string Id => "feature-enabled";
    public string Name => "Feature Enabled Doctor";

    private readonly Func<string, bool> _altsoundFolderHasFiles;
    private readonly Func<string, IReadOnlyCollection<string>> _listAltcolorExtensions;
    private readonly Func<string, int?> _getSoundMode;
    private readonly Func<string, int?> _getDmdColorize;

    /// <param name="altsoundFolderHasFiles">Given VPinMAME/altsound/&lt;rom&gt;, whether it exists and contains at least one file. Defaults to a real disk check.</param>
    /// <param name="listAltcolorExtensions">Given VPinMAME/altcolor/&lt;rom&gt;, the lower-case extensions directly inside it. Defaults to a real disk listing (same shape as <see cref="AltColorScanner"/>'s).</param>
    /// <param name="getSoundMode">Given a ROM name, VPinMAME's AltSound mode for it, or null when unknown. Defaults to <see cref="AltFeatureRegistry.TryGetSoundMode"/>.</param>
    /// <param name="getDmdColorize">Given a ROM name, VPinMAME's DMD colorize toggle for it, or null when unknown. Defaults to <see cref="AltFeatureRegistry.TryGetDmdColorize"/>.</param>
    public FeatureEnabledScanner(
        Func<string, bool>? altsoundFolderHasFiles = null,
        Func<string, IReadOnlyCollection<string>>? listAltcolorExtensions = null,
        Func<string, int?>? getSoundMode = null,
        Func<string, int?>? getDmdColorize = null)
    {
        _altsoundFolderHasFiles = altsoundFolderHasFiles ?? DefaultAltsoundFolderHasFiles;
        _listAltcolorExtensions = listAltcolorExtensions ?? DefaultListAltcolorExtensions;
        _getSoundMode = getSoundMode ?? AltFeatureRegistry.TryGetSoundMode;
        _getDmdColorize = getDmdColorize ?? AltFeatureRegistry.TryGetDmdColorize;
    }

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        if (ctx.Layout.VPinMameDir is null) yield break;
        var altsoundRoot = Path.Combine(ctx.Layout.VPinMameDir, "altsound");
        var altcolorRoot = Path.Combine(ctx.Layout.VPinMameDir, "altcolor");

        // Same anti-FP shape as AltSoundScanner/AltColorScanner: only ROMs a present table actually requires.
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

            bool altsoundPresent;
            try { altsoundPresent = _altsoundFolderHasFiles(Path.Combine(altsoundRoot, rom)); }
            catch { altsoundPresent = false; }

            IReadOnlyCollection<string> altcolorExts;
            try { altcolorExts = _listAltcolorExtensions(Path.Combine(altcolorRoot, rom)); }
            catch { altcolorExts = Array.Empty<string>(); }
            var altcolorComplete = AltColorInspector.IsComplete(altcolorExts);

            int? soundMode = null;
            if (altsoundPresent) { try { soundMode = _getSoundMode(rom); } catch { soundMode = null; } }

            int? dmdColorize = null;
            if (altcolorComplete) { try { dmdColorize = _getDmdColorize(rom); } catch { dmdColorize = null; } }

            foreach (var f in EvaluateRom(rom, altsoundPresent, soundMode, altcolorComplete, dmdColorize, Id))
                yield return f;
        }
    }

    /// <summary>Pure decision, testable without touching disk or the registry.</summary>
    public static IReadOnlyList<Finding> EvaluateRom(
        string rom,
        bool altsoundPresentNonEmpty, int? soundMode,
        bool altcolorComplete, int? dmdColorize,
        string category)
    {
        var findings = new List<Finding>();

        if (altsoundPresentNonEmpty && soundMode == 0)
        {
            findings.Add(new Finding
            {
                Code = "ALTSOUND_PRESENT_NOT_ENABLED", Severity = Severity.Note, Category = category,
                Subject = rom,
                Args = new[] { rom },
                EnglishText = $"'{rom}' has an AltSound pack installed under altsound/{rom}/, but VPinMAME's Alt Sound Mode is set to 0 (off) for this ROM — the pack is present but silent.",
                FixHint = $"In VPinMAME's per-game options for '{rom}' (F1 menu, or the VPinMAME setup GUI), switch the Sound Mode away from 0/Original to use the installed AltSound pack.",
            });
        }

        if (altcolorComplete && dmdColorize == 0)
        {
            findings.Add(new Finding
            {
                Code = "ALTCOLOR_PRESENT_NOT_ENABLED", Severity = Severity.Note, Category = category,
                Subject = rom,
                Args = new[] { rom },
                EnglishText = $"'{rom}' has a complete AltColor/Serum colorization set installed under altcolor/{rom}/, but VPinMAME's DMD colorization is turned off for this ROM — the DMD will render in mono.",
                FixHint = $"In VPinMAME's per-game options for '{rom}', enable DMD colorization (\"Colorize DMD\" / external DMD colors) to use the installed set.",
            });
        }

        return findings;
    }

    private static bool DefaultAltsoundFolderHasFiles(string folder)
        => Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any();

    private static IReadOnlyCollection<string> DefaultListAltcolorExtensions(string romFolder)
    {
        if (!Directory.Exists(romFolder)) return Array.Empty<string>();
        return Directory.EnumerateFiles(romFolder)
            .Select(f => Path.GetExtension(f).ToLowerInvariant())
            .ToList();
    }
}
