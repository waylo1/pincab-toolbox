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
}
