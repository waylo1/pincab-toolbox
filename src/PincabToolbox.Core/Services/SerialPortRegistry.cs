using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Enumerates the COM ports Windows currently has active, by reading the value DATA (not names)
/// under HKEY_LOCAL_MACHINE\HARDWARE\DEVICEMAP\SERIALCOMM — the same registry location Windows
/// itself populates for every live serial (including USB-virtual) port, and the standard way any
/// tool checks "is COMn actually there right now" without opening the port itself.
///
/// Direct advapi32 P/Invoke, same convention as <see cref="VpinmameRegistry"/>. Returns an empty
/// set (never null) on non-Windows or on any failure — callers should treat "empty" as "unknown",
/// not as "no ports exist", and bias to silence accordingly (see DmdComPortScanner).
/// </summary>
public static class SerialPortRegistry
{
    private const string SubKey = @"HARDWARE\DEVICEMAP\SERIALCOMM";
    private const int ErrorNoMoreItems = 259;

    private static readonly IReadOnlySet<string> EmptySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlySet<string> TryGetActiveComPorts()
    {
        if (!OperatingSystem.IsWindows()) return EmptySet;
        try { return ReadWindows() ?? EmptySet; }
        catch { return EmptySet; }
    }

    [SupportedOSPlatform("windows")]
    private static IReadOnlySet<string>? ReadWindows()
    {
        var hklm = new IntPtr(unchecked((int)0x80000002)); // HKEY_LOCAL_MACHINE
        const int KEY_READ = 0x20019;
        if (RegOpenKeyEx(hklm, SubKey, 0, KEY_READ, out var hKey) != 0) return null;
        try
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            uint index = 0;
            while (true)
            {
                var nameBuf = new StringBuilder(256);
                uint nameLen = 256;
                uint type = 0;
                var dataBuf = new byte[512];
                uint dataLen = (uint)dataBuf.Length;

                var rc = RegEnumValue(hKey, index, nameBuf, ref nameLen, IntPtr.Zero, ref type, dataBuf, ref dataLen);
                if (rc == ErrorNoMoreItems) break;
                if (rc != 0) break; // any other error: stop, return what we have so far

                const uint REG_SZ = 1;
                if (type == REG_SZ && dataLen > 0)
                {
                    var value = Encoding.Unicode.GetString(dataBuf, 0, (int)dataLen).TrimEnd('\0').Trim();
                    if (value.Length > 0) result.Add(value);
                }
                index++;
            }
            return result;
        }
        finally { RegCloseKey(hKey); }
    }

    [SupportedOSPlatform("windows")]
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegOpenKeyEx(IntPtr hKey, string subKey, int options, int samDesired, out IntPtr phkResult);

    [SupportedOSPlatform("windows")]
    [DllImport("advapi32.dll", EntryPoint = "RegEnumValueW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegEnumValue(IntPtr hKey, uint dwIndex, StringBuilder lpValueName, ref uint lpcchValueName, IntPtr lpReserved, ref uint lpType, byte[] lpData, ref uint lpcbData);

    [SupportedOSPlatform("windows")]
    [DllImport("advapi32.dll")]
    private static extern int RegCloseKey(IntPtr hKey);
}
