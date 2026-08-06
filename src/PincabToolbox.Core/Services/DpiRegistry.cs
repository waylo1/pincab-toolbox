using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Reads the current user's applied display scaling (DPI) from the Windows registry
/// (HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics, value "AppliedDPI", REG_DWORD). 96 is
/// the well-known baseline for 100% scaling; Windows steps in fixed multiples from there
/// (120=125%, 144=150%, 168=175%, 192=200%…) — audit §4-C2: non-100% scaling is a known cause of a
/// backglass or table window rendering truncated or offset on some cabs.
///
/// Direct advapi32 P/Invoke, same convention as <see cref="VpinmameRegistry"/> (zero external
/// dependency; narrow, single-purpose reader). Returns null on non-Windows or on any failure.
/// </summary>
public static class DpiRegistry
{
    private const string SubKey = @"Control Panel\Desktop\WindowMetrics";
    private const string ValueName = "AppliedDPI";

    /// <summary>The applied DPI value (96 = 100%), or null when unavailable.</summary>
    public static uint? TryGetAppliedDpi()
    {
        if (!OperatingSystem.IsWindows()) return null;
        try { return ReadWindows(); }
        catch { return null; }
    }

    [SupportedOSPlatform("windows")]
    private static uint? ReadWindows()
    {
        var hkcu = new IntPtr(unchecked((int)0x80000001)); // HKEY_CURRENT_USER
        const int KEY_READ = 0x20019;
        if (RegOpenKeyEx(hkcu, SubKey, 0, KEY_READ, out var hKey) != 0) return null;
        try
        {
            const uint REG_DWORD = 4;
            uint type = 0;
            uint cb = 4;
            var data = new byte[4];
            if (RegQueryValueEx(hKey, ValueName, IntPtr.Zero, ref type, data, ref cb) != 0) return null;
            if (type != REG_DWORD || cb != 4) return null;
            return BitConverter.ToUInt32(data, 0);
        }
        finally { RegCloseKey(hKey); }
    }

    [SupportedOSPlatform("windows")]
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegOpenKeyEx(IntPtr hKey, string subKey, int options, int samDesired, out IntPtr phkResult);

    [SupportedOSPlatform("windows")]
    [DllImport("advapi32.dll", EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegQueryValueEx(IntPtr hKey, string valueName, IntPtr reserved, ref uint type, byte[]? data, ref uint cbData);

    [SupportedOSPlatform("windows")]
    [DllImport("advapi32.dll")]
    private static extern int RegCloseKey(IntPtr hKey);
}
