using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Informative-only: flags a likely-incomplete multi-screen setup — a backglass server or DMD
/// component is installed (implying the cab is meant to drive at least a playfield + backglass,
/// often + DMD), but fewer than 2 displays are currently connected.
///
/// Deliberately narrow. The community's most-discussed pain point (FIELD-LOG 2026-07-29,
/// "Changer l'ordre des écrans dans Windows", 52 replies; corroborated on Pinball Nirvana,
/// "monitors on standby") is screen ORDER/assignment, not just count — but that would require
/// knowing which physical monitor PinUP Popper expects for which role, which lives in Popper's
/// own configuration, not in anything this profile-driven scanner currently reads. Rather than
/// guess at an undocumented schema, this reports the one thing it can measure honestly (a
/// count mismatch) and points at the community fix for the rest. No registry correction is
/// attempted here or ever planned for this signal — see docs/adr/ADR-005 (the fix touches
/// system-wide display keys, outside InstallLayout's confinement) and the 2026-07-29 FIELD-LOG
/// entry that reached the same conclusion.
/// </summary>
public sealed class DisplaySetupScanner : IScanner
{
    public string Id => "display";
    public string Name => "Display Setup";

    private static readonly string[] MultiScreenRoles = { "b2s", "dmddevice", "dmddevice64", "flexdmd" };

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        var hasMultiScreenComponent = ctx.Profile.BinaryRoles
            .Where(r => MultiScreenRoles.Contains(r.Role, StringComparer.OrdinalIgnoreCase))
            .Any(r => LayoutDetector.FindFilesByPattern(ResolveScopeRoot(ctx.Layout, r.Scope), r.Pattern, 4).Any());

        var finding = Evaluate(DisplayProbe.TryGetConnectedMonitorCount(), hasMultiScreenComponent, Id);
        if (finding is not null) yield return finding;
    }

    /// <summary>Pure decision, testable without real monitors.</summary>
    public static Finding? Evaluate(int? connectedMonitors, bool hasMultiScreenComponent, string category)
    {
        if (!hasMultiScreenComponent) return null;
        if (connectedMonitors is null) return null;   // unmeasurable (non-Windows, API failure): stay silent
        if (connectedMonitors.Value >= 2) return null;

        return new Finding
        {
            Code = "DISPLAY_SETUP_INCOMPLETE", Severity = Severity.Info, Category = category,
            Subject = $"{connectedMonitors.Value} display",
            Args = new[] { connectedMonitors.Value.ToString() },
            EnglishText = $"This install expects a multi-screen setup (a backglass or DMD component is present) but "
                        + $"only {connectedMonitors.Value} display is currently connected. If your cab normally runs "
                        + "with more screens, they may be asleep, disconnected, or have reconnected in the wrong order.",
            FixHint = "Check cabling and that monitor standby is disabled independently of PC sleep — a very common "
                    + "cause of screens reconnecting in the wrong order after a restart (community guides: Pincab "
                    + "Passion \"Changer l'ordre des écrans dans Windows\").",
        };
    }

    private static string ResolveScopeRoot(InstallLayout layout, string scope) => scope switch
    {
        "root" => layout.RootPath,
        "vpinmame" => layout.VPinMameDir ?? layout.RootPath,
        "tables" => layout.TablesDir ?? layout.RootPath,
        _ => layout.RootPath,
    };
}
