using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PincabToolbox.Core.Services;

/// <summary>One connected monitor's rectangle in virtual-desktop coordinates, plus its Win32 device name.</summary>
/// <param name="X">Can be negative — a monitor placed left of or above the primary has a negative origin.</param>
/// <param name="DeviceName">Win32 display device name, e.g. <c>\\.\DISPLAY1</c>.</param>
public readonly record struct MonitorRect(int X, int Y, int Width, int Height, string DeviceName);

/// <summary>
/// Enumerates the real rectangle of every connected monitor — the raw material
/// <see cref="ScreenTopologyAnalyzer"/> needs to decide whether a declared backglass position
/// (read from ScreenRes.txt / &lt;table&gt;.res, see <see cref="Scanning.ScreenTopologyScanner"/>)
/// actually lands on a real screen.
///
/// <para>
/// Deliberately a NEW, separate P/Invoke surface rather than an extension of <see cref="DisplayProbe"/>
/// (handoff §3/C1 instruction: do not modify DisplayProbe.cs). <c>DisplayProbe</c> answers a narrower,
/// already-shipped question (just a monitor COUNT, for <see cref="Scanning.DisplaySetupScanner"/>) —
/// touching it to grow rectangles/device-names out of it would risk that existing scanner for no reason.
/// </para>
///
/// <para>
/// Uses <c>EnumDisplayMonitors</c> + <c>GetMonitorInfo</c> — both long-stable, extensively documented
/// Win32 APIs (unlike the two vendor file formats this feeds into, see
/// <see cref="ScreenTopologyAnalyzer"/> for that research trail) — to get each monitor's full
/// <c>rcMonitor</c> bounds (not the work area: a backglass window is never constrained by the taskbar)
/// and its <c>\\.\DISPLAYn</c> device name, which ScreenRes.txt's plain-integer screen selector refers to.
/// </para>
/// </summary>
public static class MonitorTopologyProbe
{
    public static IReadOnlyList<MonitorRect>? TryGetMonitorRects()
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            var rects = new List<MonitorRect>();
            var ok = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
                if (GetMonitorInfo(hMonitor, ref info))
                {
                    rects.Add(new MonitorRect(
                        info.rcMonitor.Left,
                        info.rcMonitor.Top,
                        info.rcMonitor.Right - info.rcMonitor.Left,
                        info.rcMonitor.Bottom - info.rcMonitor.Top,
                        info.szDevice ?? string.Empty));
                }
                return true;
            }, IntPtr.Zero);
            return (ok && rects.Count > 0) ? rects : null;
        }
        catch { return null; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);
}
