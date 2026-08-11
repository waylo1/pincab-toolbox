using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Reads the two per-ROM VPinMAME registry values LOT D (spec 10/08) needs to tell "feature
/// installed" apart from "feature actually enabled" — <c>HKEY_CURRENT_USER\Software\Freeware\Visual
/// PinMame\&lt;rom&gt;</c>, same key casing/root <see cref="VpinmameKeyProbe"/> already confirmed
/// empirically.
///
/// <para>
/// <b>Primary-source status, honestly stated (spec §3.1 rule 4 — no invented identifiers).</b>
/// <c>dmd_colorize</c> (AltColor enable) is corroborated by two independent sources: the PinUP
/// Popper wiki's "alt_mode" page, and <c>SteloPin/SetReg_altcolor</c> — a community tool built for
/// the sole purpose of toggling this exact value, whose own docs state "1 = colorize when an
/// altcolor set exists, 0 = disabled". <c>sound_mode</c> (AltSound enable) has one direct source (the
/// same PinUP Popper wiki page) plus indirect corroboration: the research citation this lot is built
/// from ("change the Alt Sound Mode (0-3) from 0 to 1") independently names the same 0-3 range and
/// the same "0 is off" polarity. Neither name was found duplicated verbatim in the PinMAME source
/// tree itself — this reader is therefore intentionally narrow (exactly one candidate name each, no
/// invented alternates) and every read that isn't a clean, positively-typed DWORD degrades to "don't
/// know" (null), never to a guessed 0. Logged as a confidence caveat in FIELD-LOG for Maxime to
/// confirm against a real registry export, same posture as <see cref="DmdDeviceIniParser"/>'s COM
/// port key names.
/// </para>
/// </summary>
public static class AltFeatureRegistry
{
    private const string SubKeyRoot = @"Software\Freeware\Visual PinMame\";

    /// <summary>VPinMAME's AltSound mode selector for this ROM, or null when unreadable/unknown. 0 means "off" per the community's own "Alt Sound Mode (0-3), 0 to 1" description.</summary>
    public static int? TryGetSoundMode(string rom) => TryGetRomDword(rom, "sound_mode");

    /// <summary>VPinMAME's AltColor/DMD colorization toggle for this ROM, or null when unreadable/unknown. 0 means "disabled" per SetReg_altcolor's own documentation.</summary>
    public static int? TryGetDmdColorize(string rom) => TryGetRomDword(rom, "dmd_colorize");

    private static int? TryGetRomDword(string rom, string valueName)
    {
        if (!OperatingSystem.IsWindows()) return null;
        try { return ReadWindows(rom, valueName); }
        catch { return null; } // a missing key, a locked hive, or an unexpected shape must never crash a scan
    }

    [SupportedOSPlatform("windows")]
    private static int? ReadWindows(string rom, string valueName)
    {
        var hkcu = new IntPtr(unchecked((int)0x80000001)); // HKEY_CURRENT_USER
        const int KEY_READ = 0x20019;
        if (RegOpenKeyEx(hkcu, SubKeyRoot + rom, 0, KEY_READ, out var hKey) != 0) return null;
        try
        {
            const uint REG_DWORD = 4;
            uint type = 0, cb = 4;
            var data = new byte[4];
            if (RegQueryValueEx(hKey, valueName, IntPtr.Zero, ref type, data, ref cb) != 0) return null;
            if (type != REG_DWORD || cb != 4) return null; // not a plain DWORD -> don't guess, don't know
            return BitConverter.ToInt32(data, 0);
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
