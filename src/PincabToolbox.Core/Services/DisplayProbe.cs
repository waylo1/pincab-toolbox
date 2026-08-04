using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Counts currently connected displays.
///
/// Implemented with a direct user32 P/Invoke (SM_CMONITORS) so Core keeps its zero-external
/// -dependency contract. Returns null on non-Windows or on any failure — screen order/count
/// is a recurring cab pain point (FIELD-LOG 2026-07-29, "Changer l'ordre des écrans", 52
/// replies) but this is only ever used as an INFORMATIVE signal, never a repair target
/// (see docs/adr — the registry keys involved sit outside InstallLayout's confinement).
/// </summary>
public static class DisplayProbe
{
    private const int SM_CMONITORS = 80;

    public static int? TryGetConnectedMonitorCount()
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            var count = GetSystemMetrics(SM_CMONITORS);
            return count > 0 ? count : null;
        }
        catch { return null; }
    }

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
