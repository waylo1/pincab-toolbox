using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Notes when dmddevice.ini enables a hardware DMD driver (pin2dmd/zedmd/pindmd3) on a COM port
/// that Windows doesn't currently list as active — audit §4-B3: known to cause a several-second
/// freeze at launch while the driver retries the missing port. <see cref="Severity.Note"/>, not
/// Warning (ADR-010 Doctrine): the device could simply be powered off or disconnected at scan
/// time, which is not a defect — this states the mismatch as a fact, not a verdict on whether it's
/// a problem right now.
/// </summary>
public sealed class DmdComPortScanner : IScanner
{
    public string Id => "dmd-com-port";
    public string Name => "dmddevice.ini COM-Probe";

    private readonly Func<string, bool> _fileExists;
    private readonly Func<string, string> _readAllText;
    private readonly Func<IReadOnlySet<string>> _getActiveComPorts;

    /// <param name="fileExists">Given a path, whether it exists. Defaults to a real disk check.</param>
    /// <param name="readAllText">Given a path, its full text. Defaults to a real disk read.</param>
    /// <param name="getActiveComPorts">Returns the set of COM ports Windows currently lists as active. Defaults to a real registry read.</param>
    public DmdComPortScanner(
        Func<string, bool>? fileExists = null,
        Func<string, string>? readAllText = null,
        Func<IReadOnlySet<string>>? getActiveComPorts = null)
    {
        _fileExists = fileExists ?? File.Exists;
        _readAllText = readAllText ?? File.ReadAllText;
        _getActiveComPorts = getActiveComPorts ?? SerialPortRegistry.TryGetActiveComPorts;
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

        IReadOnlyList<DmdDeviceIniParser.ConfiguredDevice> devices;
        try { devices = DmdDeviceIniParser.ParseEnabledComPortDevices(text); }
        catch { return Array.Empty<Finding>(); }
        if (devices.Count == 0) return Array.Empty<Finding>();

        IReadOnlySet<string> activePorts;
        try { activePorts = _getActiveComPorts(); }
        catch { return Array.Empty<Finding>(); }
        if (activePorts.Count == 0) return Array.Empty<Finding>(); // can't confirm anything -> silence, not "all missing"

        var findings = new List<Finding>();
        foreach (var device in devices)
        {
            if (activePorts.Contains(device.ComPort)) continue;
            findings.Add(new Finding
            {
                Code = "DMD_COM_PORT_NOT_FOUND", Severity = Severity.Note, Category = Id,
                Subject = device.ComPort,
                FilePath = iniPath,
                Args = new[] { device.Section, device.ComPort },
                EnglishText = $"dmddevice.ini enables '{device.Section}' on {device.ComPort}, but Windows doesn't currently list {device.ComPort} as active. If this DMD is connected and powered, this pattern is known to cause a several-second freeze at launch while the driver waits for it.",
                FixHint = "Check the DMD is powered and its USB/serial connection is plugged in, or update dmddevice.ini if the COM port changed.",
            });
        }
        return findings;
    }
}
