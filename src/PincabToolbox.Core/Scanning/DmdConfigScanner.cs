using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// LOT C (spec 10/08) — two findings about <c>dmddevice.ini</c>'s <c>[virtualdmd]</c> section that
/// <see cref="DmdComPortScanner"/> never looked at (that one only reads the hardware COM-port
/// sections). A NEW scanner, deliberately — <see cref="DmdDeviceIniParser"/> is a service, extending
/// it does not violate "don't touch existing scanners" (spec §3.1 rule 5), but adding these findings
/// to <see cref="DmdComPortScanner"/> itself would.
///
/// <para>
/// C.1 (<c>DMD_VIRTUAL_DISABLED</c>, Note): a Freezy update is known to silently reset
/// <c>[virtualdmd] enabled</c> to <c>false</c> on cabs that have no physical DMD, making the DMD
/// disappear with no error. <c>Note</c>, not <c>Warning</c> — disabling the virtual DMD is a
/// perfectly legitimate choice on a cab with a real hardware DMD, which is exactly why this only
/// fires when no hardware DMD section is enabled either (<see cref="DmdDeviceIniParser.AnyHardwareDeviceEnabled"/>).
/// </para>
///
/// <para>
/// C.2 (<c>DMD_POSITION_OFFSCREEN</c>, Warning): the same "does this rectangle intersect any real
/// monitor" geometry <see cref="ScreenTopologyScanner"/> already uses for ScreenRes.txt
/// (<see cref="ScreenTopologyAnalyzer.IsOffScreen"/>), reused rather than reimplemented. Negative
/// left/top are not treated as errors — a monitor placed left of or above the primary is a normal,
/// valid multi-monitor layout, and the analyzer's intersection test already respects that.
/// </para>
///
/// <para>
/// Both findings are silent by construction whenever a signal can't be measured honestly: ini
/// missing/unreadable, no <c>[virtualdmd]</c> section at all, a key simply absent (never assumed),
/// or monitor geometry unavailable (non-Windows, API failure).
/// </para>
/// </summary>
public sealed class DmdConfigScanner : IScanner
{
    public string Id => "dmd-config";
    public string Name => "dmddevice.ini Config Doctor";

    private readonly Func<string, bool> _fileExists;
    private readonly Func<string, string> _readAllText;
    private readonly Func<IReadOnlyList<MonitorRect>?> _getMonitors;

    /// <param name="fileExists">Given a path, whether it exists. Defaults to a real disk check.</param>
    /// <param name="readAllText">Given a path, its full text. Defaults to a real disk read.</param>
    /// <param name="getMonitors">Every connected monitor's rectangle, or null when unmeasurable. Defaults to <see cref="MonitorTopologyProbe.TryGetMonitorRects"/>.</param>
    public DmdConfigScanner(
        Func<string, bool>? fileExists = null,
        Func<string, string>? readAllText = null,
        Func<IReadOnlyList<MonitorRect>?>? getMonitors = null)
    {
        _fileExists = fileExists ?? File.Exists;
        _readAllText = readAllText ?? File.ReadAllText;
        _getMonitors = getMonitors ?? MonitorTopologyProbe.TryGetMonitorRects;
    }

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        ctx.Cancellation.ThrowIfCancellationRequested();

        if (ctx.Layout.VPinMameDir is null) return Array.Empty<Finding>();
        var iniPath = Path.Combine(ctx.Layout.VPinMameDir, "dmddevice.ini");

        bool exists;
        try { exists = _fileExists(iniPath); }
        catch { return Array.Empty<Finding>(); }
        if (!exists) return Array.Empty<Finding>();

        string text;
        try { text = _readAllText(iniPath); }
        catch { return Array.Empty<Finding>(); } // unreadable -> silence, never a false positive

        DmdDeviceIniParser.VirtualDmdConfig? cfg;
        bool anyHardwareEnabled;
        try
        {
            cfg = DmdDeviceIniParser.TryParseVirtualDmdConfig(text);
            anyHardwareEnabled = DmdDeviceIniParser.AnyHardwareDeviceEnabled(text);
        }
        catch { return Array.Empty<Finding>(); }

        IReadOnlyList<MonitorRect>? monitors;
        try { monitors = _getMonitors(); }
        catch { monitors = null; }

        return Evaluate(cfg, anyHardwareEnabled, monitors, iniPath, Id);
    }

    /// <summary>Pure decision, testable without touching disk or the registry.</summary>
    public static IReadOnlyList<Finding> Evaluate(
        DmdDeviceIniParser.VirtualDmdConfig? cfg,
        bool anyHardwareDeviceEnabled,
        IReadOnlyList<MonitorRect>? monitors,
        string iniPath,
        string category)
    {
        var findings = new List<Finding>();
        if (cfg is null) return findings; // no [virtualdmd] section at all -> nothing to say

        // C.1 — DMD_VIRTUAL_DISABLED (Note): virtual DMD explicitly off AND no hardware DMD picks up the slack.
        if (cfg.Enabled == false && !anyHardwareDeviceEnabled)
        {
            findings.Add(new Finding
            {
                Code = "DMD_VIRTUAL_DISABLED", Severity = Severity.Note, Category = category,
                Subject = "virtualdmd", FilePath = iniPath,
                EnglishText = "dmddevice.ini has the virtual DMD turned off ('[virtualdmd] enabled = false'), and no hardware DMD driver is enabled in the same file either. If you don't have a physical DMD, this will make your DMD disappear with no error message — a Freezy update is known to reset this value on its own.",
                FixHint = "If you don't have a physical DMD, set 'enabled = true' under [virtualdmd] in dmddevice.ini.",
            });
        }

        // C.2 — DMD_POSITION_OFFSCREEN (Warning): rectangle fully off every real monitor.
        if (cfg.Left is int left && cfg.Top is int top && cfg.Width is int width && cfg.Height is int height
            && monitors is { Count: > 0 }
            && ScreenTopologyAnalyzer.IsOffScreen(left, top, width, height, monitors))
        {
            findings.Add(new Finding
            {
                Code = "DMD_POSITION_OFFSCREEN", Severity = Severity.Warning, Category = category,
                Subject = "virtualdmd", FilePath = iniPath,
                Args = new[] { left.ToString(), top.ToString(), width.ToString(), height.ToString() },
                EnglishText = $"dmddevice.ini positions the virtual DMD at ({left},{top}) sized {width}x{height}, which falls entirely outside every connected monitor — it will render invisible even though dmddevice.ini itself loads without error.",
                FixHint = "Reset the [virtualdmd] left/top/width/height values in dmddevice.ini (or delete them to fall back to the defaults) with all monitors connected in their normal cab layout.",
            });
        }

        return findings;
    }
}
