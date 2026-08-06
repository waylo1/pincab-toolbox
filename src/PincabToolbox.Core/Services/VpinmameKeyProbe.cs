using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Checks only whether the VPinMAME registry key (HKEY_CURRENT_USER\Software\Freeware\Visual
/// PinMame) exists — nothing is read from it. A separate, narrower reader from
/// <see cref="VpinmameRegistry"/> on purpose (same convention: one small single-purpose reader per
/// need, not a shared generic registry utility) — that one reads a specific value for the ROM
/// scanner; this one only answers "is there a registry configuration at all", for
/// <c>ConfigPhantomScanner</c> (audit §4-E2).
///
/// Same casing as the key VPinMAME itself actually uses ("PinMame", confirmed empirically —
/// see <see cref="VpinmameRegistry"/>), not the "PinMAME" casing used in the audit doc's prose.
/// </summary>
public static class VpinmameKeyProbe
{
    private const string SubKey = @"Software\Freeware\Visual PinMame";

    public static bool KeyExists()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try { return ReadWindows(); }
        catch { return false; }
    }

    [SupportedOSPlatform("windows")]
    private static bool ReadWindows()
    {
        var hkcu = new IntPtr(unchecked((int)0x80000001)); // HKEY_CURRENT_USER
        const int KEY_READ = 0x20019;
        if (RegOpenKeyEx(hkcu, SubKey, 0, KEY_READ, out var hKey) != 0) return false;
        RegCloseKey(hKey);
        return true;
    }

    [SupportedOSPlatform("windows")]
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegOpenKeyEx(IntPtr hKey, string subKey, int options, int samDesired, out IntPtr phkResult);

    [SupportedOSPlatform("windows")]
    [DllImport("advapi32.dll")]
    private static extern int RegCloseKey(IntPtr hKey);
}
