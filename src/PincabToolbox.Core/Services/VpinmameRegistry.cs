using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Reads the VPinMAME ROM path from the Windows registry
/// (HKEY_CURRENT_USER\Software\Freeware\Visual PinMame\globals, value "rompath").
///
/// This is where VPinMAME itself records its roms folder, so it is authoritative even when the
/// tables and VPinMAME live on different drives — exactly the case where the relative-path
/// fallback found no roms folder and silently skipped every ROM check (FIELD-LOG 2026-07-30:
/// FD had Tables on E:, VPX/VPinMAME on D:, and got "roms folder not found — ROM checks skipped").
///
/// Implemented with a direct advapi32 P/Invoke so <c>PincabToolbox.Core</c> keeps its
/// zero-external-dependency contract (no Microsoft.Win32.Registry package). Returns null on
/// non-Windows or on any failure — every caller must degrade gracefully.
/// </summary>
public static class VpinmameRegistry
{
    private const string SubKey = @"Software\Freeware\Visual PinMame\globals";
    private const string ValueName = "rompath";

    /// <summary>The configured roms folder, or null when unavailable (non-Windows, key absent, empty…).</summary>
    public static string? TryGetRomPath()
    {
        if (!OperatingSystem.IsWindows()) return null;
        try { return ReadWindows(); }
        catch { return null; }   // a missing key or a locked hive must never crash a scan
    }

    [SupportedOSPlatform("windows")]
    private static string? ReadWindows()
    {
        var hkcu = new IntPtr(unchecked((int)0x80000001)); // HKEY_CURRENT_USER
        const int KEY_READ = 0x20019;
        if (RegOpenKeyEx(hkcu, SubKey, 0, KEY_READ, out var hKey) != 0) return null;
        try
        {
            const uint REG_SZ = 1, REG_EXPAND_SZ = 2;
            uint type = 0, cb = 0;
            if (RegQueryValueEx(hKey, ValueName, IntPtr.Zero, ref type, null, ref cb) != 0) return null;
            if (type != REG_SZ && type != REG_EXPAND_SZ || cb == 0) return null;

            var data = new byte[cb];
            if (RegQueryValueEx(hKey, ValueName, IntPtr.Zero, ref type, data, ref cb) != 0) return null;

            // REG_SZ / REG_EXPAND_SZ payloads are UTF-16LE; drop the trailing null terminator.
            var s = Encoding.Unicode.GetString(data, 0, (int)cb).TrimEnd('\0').Trim();
            if (type == REG_EXPAND_SZ) s = Environment.ExpandEnvironmentVariables(s);
            return string.IsNullOrWhiteSpace(s) ? null : s;
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
