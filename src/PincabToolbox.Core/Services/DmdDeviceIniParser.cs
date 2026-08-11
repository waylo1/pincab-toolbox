using System.Text.RegularExpressions;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Minimal hand-rolled INI reader for dmddevice.ini (Freezy dmd-extensions config) — zero external
/// dependency, same "maison" spirit as AltSoundManifestLinter's CSV parser. Only extracts what
/// <c>DmdComPortScanner</c> needs: for each of the known hardware-DMD sections, whether it's
/// enabled and, if so, the COM port it configures.
///
/// <para>
/// ⚠️ The exact key name dmddevice.ini uses for a COM port varies across the small set of
/// documented conventions for these tools ("port", "comport", "com_port", "serialport" are all
/// seen in the wild); this reader accepts any of them, case-insensitively, and extracts the first
/// "COMn" token from the value. Needs confirmation against a real dmddevice.ini on Maxime's cab
/// before this ships with full confidence — see FIELD-LOG, DÉCISIONS EN ATTENTE.
/// </para>
/// </summary>
public static class DmdDeviceIniParser
{
    /// <summary>Section names this prober understands (audit §4-B3: hardware DMD drivers that use a COM port).</summary>
    public static readonly string[] KnownComPortSections = { "pin2dmd", "zedmd", "pindmd3" };

    private static readonly Regex ComPortToken = new(@"COM\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] PortKeyNames = { "port", "comport", "com_port", "serialport" };

    public sealed record ConfiguredDevice(string Section, string ComPort);

    /// <summary>
    /// Parses INI text and returns each known section that is both enabled and declares a COM
    /// port. Malformed or unrecognized content is silently skipped line by line — this reader
    /// never throws on bad input, it just extracts what it can confidently understand.
    /// </summary>
    public static IReadOnlyList<ConfiguredDevice> ParseEnabledComPortDevices(string iniText)
    {
        var results = new List<ConfiguredDevice>();
        string? currentSection = null;
        bool sectionEnabled = false;
        string? sectionComPort = null;

        void FlushSection()
        {
            if (currentSection is not null && sectionEnabled && sectionComPort is not null
                && Array.IndexOf(KnownComPortSections, currentSection) >= 0)
            {
                results.Add(new ConfiguredDevice(currentSection, sectionComPort));
            }
        }

        foreach (var rawLine in iniText.Split('\n'))
        {
            var line = rawLine.Trim().TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#')) continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                FlushSection();
                currentSection = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                sectionEnabled = false;
                sectionComPort = null;
                continue;
            }

            if (currentSection is null) continue;
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line.Substring(0, eq).Trim().ToLowerInvariant();
            var value = line.Substring(eq + 1).Trim();

            if (key == "enabled")
            {
                sectionEnabled = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
            }
            else if (Array.IndexOf(PortKeyNames, key) >= 0)
            {
                var m = ComPortToken.Match(value);
                if (m.Success) sectionComPort = m.Value.ToUpperInvariant();
            }
        }
        FlushSection();

        return results;
    }

    /// <summary>
    /// Section names understood as "a hardware DMD driver" for LOT C.1's "is anything else enabled
    /// instead" check — deliberately broader than <see cref="KnownComPortSections"/> (which is only
    /// the three drivers that also expose a COM port). Confirmed against the primary source: the
    /// shipped <c>PinMameDevice/DmdDevice.ini</c> in the official <c>freezy/dmd-extensions</c> repo
    /// (fetched 2026-08-11), which lists exactly these hardware sections, each with its own
    /// <c>enabled</c> key: pindmd1/2/3, zedmd + its hd/wifi/hdwifi variants, pin2dmd, pixelcade.
    /// Sections present in the same file but NOT hardware outputs (networkstream, browserstream,
    /// vpdbstream, video, pinup, rawoutput, alphanumeric) are intentionally excluded — enabling one of
    /// those says nothing about whether the user has a physical DMD.
    /// </summary>
    public static readonly string[] HardwareDmdSections =
    {
        "pindmd1", "pindmd2", "pindmd3",
        "zedmd", "zedmdhd", "zedmdwifi", "zedmdhdwifi",
        "pin2dmd", "pixelcade",
    };

    /// <summary>
    /// The <c>[virtualdmd]</c> section's fields relevant to LOT C — key names confirmed against the
    /// same primary source as <see cref="HardwareDmdSections"/> (<c>enabled</c>, <c>left</c>,
    /// <c>top</c>, <c>width</c>, <c>height</c> are exactly the keys the shipped sample INI uses).
    /// Each field is null when the key was simply absent from the file — "absent" is never treated as
    /// a value (spec §3.1 rule 4: no invented identifiers, no guessed defaults).
    /// </summary>
    public sealed record VirtualDmdConfig(bool? Enabled, int? Left, int? Top, int? Width, int? Height);

    /// <summary>
    /// Parses only the <c>[virtualdmd]</c> section. Returns null when that section is not present at
    /// all in the file — distinct from a <see cref="VirtualDmdConfig"/> whose fields are all null,
    /// which means "the section exists but this particular key wasn't set". Never throws on malformed
    /// input, mirroring <see cref="ParseEnabledComPortDevices"/>.
    /// </summary>
    public static VirtualDmdConfig? TryParseVirtualDmdConfig(string iniText)
    {
        string? currentSection = null;
        var sawVirtualDmdSection = false;
        bool? enabled = null;
        int? left = null, top = null, width = null, height = null;

        foreach (var rawLine in iniText.Split('\n'))
        {
            var line = rawLine.Trim().TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#')) continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                if (currentSection == "virtualdmd") sawVirtualDmdSection = true;
                continue;
            }

            if (currentSection != "virtualdmd") continue;
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line.Substring(0, eq).Trim().ToLowerInvariant();
            var value = line.Substring(eq + 1).Trim();

            switch (key)
            {
                case "enabled":
                    enabled = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
                    break;
                case "left": if (int.TryParse(value, out var l)) left = l; break;
                case "top": if (int.TryParse(value, out var t)) top = t; break;
                case "width": if (int.TryParse(value, out var w)) width = w; break;
                case "height": if (int.TryParse(value, out var h)) height = h; break;
            }
        }

        return sawVirtualDmdSection ? new VirtualDmdConfig(enabled, left, top, width, height) : null;
    }

    /// <summary>
    /// True when at least one <see cref="HardwareDmdSections"/> section has <c>enabled = true</c> (or
    /// <c>1</c>) anywhere in the file. Used by LOT C.1 to tell "virtual DMD off by mistake" apart from
    /// "virtual DMD off on purpose because there's a real DMD" — the latter is not a defect.
    /// </summary>
    public static bool AnyHardwareDeviceEnabled(string iniText)
    {
        string? currentSection = null;
        var sectionEnabled = false;
        var any = false;

        void FlushSection()
        {
            if (currentSection is not null && sectionEnabled && Array.IndexOf(HardwareDmdSections, currentSection) >= 0)
                any = true;
        }

        foreach (var rawLine in iniText.Split('\n'))
        {
            var line = rawLine.Trim().TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#')) continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                FlushSection();
                currentSection = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                sectionEnabled = false;
                continue;
            }

            if (currentSection is null) continue;
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line.Substring(0, eq).Trim().ToLowerInvariant();
            var value = line.Substring(eq + 1).Trim();
            if (key == "enabled")
                sectionEnabled = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
        }
        FlushSection();

        return any;
    }
}
