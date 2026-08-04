using PincabToolbox.Core.Models;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Informative-only: reports legacy .vpt (Visual Pinball 9) tables sitting in the tables folder.
/// They frequently don't show up in PinUP Popper because ".vpt" isn't among the VPX emulator's
/// file extensions (FIELD-LOG 2026-07-29, VPForums "Pinup not recognizing some tables").
///
/// Deliberately NOT a repair: PinUP's author (NailBuster) advises against adding ".vpt" to the
/// existing VPX emulator — it breaks .vpt launching — and recommends a dedicated legacy emulator.
/// So we signal and point at the right procedure; we never apply the discouraged shortcut.
/// </summary>
public sealed class LegacyTableScanner : IScanner
{
    public string Id => "legacy";
    public string Name => "Legacy Tables";

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        var count = CountVpt(ctx.Layout.TablesDir);
        if (count == 0) yield break;

        yield return new Finding
        {
            Code = "VPT_LEGACY_PRESENT", Severity = Severity.Info, Category = Id,
            Subject = $"{count} .vpt",
            Args = new[] { count.ToString() },
            EnglishText = $"{count} legacy .vpt (Visual Pinball 9) table(s) are present. These often don't appear "
                        + "in PinUP Popper because '.vpt' isn't among the VPX emulator's file extensions.",
            FixHint = "Don't add '.vpt' to the existing VPX emulator — PinUP's author (NailBuster) advises against it, "
                    + "as it breaks .vpt launching. Set up a dedicated legacy VP9 emulator entry instead.",
        };
    }

    private static int CountVpt(string? tablesDir)
    {
        if (tablesDir is null) return 0;
        try { return Directory.GetFiles(tablesDir, "*.vpt", SearchOption.TopDirectoryOnly).Length; }
        catch { return 0; }
    }
}
