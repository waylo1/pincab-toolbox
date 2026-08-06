using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure INI-parsing decision.</summary>
public static class DmdDeviceIniParserTests
{
    public static void Test_EnabledSectionWithPort_Extracted()
    {
        var ini = "[pin2dmd]\nenabled = true\nport = COM4\n";
        var devices = DmdDeviceIniParser.ParseEnabledComPortDevices(ini);
        Assert.Equal(1, devices.Count);
        Assert.Equal("pin2dmd", devices[0].Section);
        Assert.Equal("COM4", devices[0].ComPort);
    }

    public static void Test_DisabledSection_Ignored()
    {
        var ini = "[zedmd]\nenabled = false\ncomport = COM5\n";
        var devices = DmdDeviceIniParser.ParseEnabledComPortDevices(ini);
        Assert.Equal(0, devices.Count);
    }

    public static void Test_UnknownSection_Ignored()
    {
        var ini = "[virtualdmd]\nenabled = true\nport = COM6\n";
        var devices = DmdDeviceIniParser.ParseEnabledComPortDevices(ini);
        Assert.Equal(0, devices.Count);
    }

    public static void Test_EnabledWithoutPort_Ignored()
    {
        var ini = "[pindmd3]\nenabled = true\n";
        var devices = DmdDeviceIniParser.ParseEnabledComPortDevices(ini);
        Assert.Equal(0, devices.Count);
    }

    public static void Test_MultipleSections_AllExtracted()
    {
        var ini = "[pin2dmd]\nenabled = 1\nport = COM3\n\n[zedmd]\nenabled = true\nserialport = COM7\n";
        var devices = DmdDeviceIniParser.ParseEnabledComPortDevices(ini);
        Assert.Equal(2, devices.Count);
    }

    public static void Test_EmptyText_NoThrow()
    {
        var devices = DmdDeviceIniParser.ParseEnabledComPortDevices("");
        Assert.Equal(0, devices.Count);
    }
}

/// <summary>End-to-end scanner behaviour, with disk + registry reads injected.</summary>
public static class DmdComPortScannerTests
{
    private static ScanContext Ctx(string? vpinmameDir = "/vpm")
    {
        var layout = new InstallLayout { RootPath = "/x", VPinMameDir = vpinmameDir };
        return new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
    }

    private const string IniWithPin2dmdOnCom4 = "[pin2dmd]\nenabled = true\nport = COM4\n";

    public static void Test_PortNotActive_Notes()
    {
        var findings = new DmdComPortScanner(
            _ => true,
            _ => IniWithPin2dmdOnCom4,
            () => new HashSet<string> { "COM1" }
        ).Scan(Ctx()).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "DMD_COM_PORT_NOT_FOUND"));
        Assert.Equal(Severity.Note, findings.Single().Severity);
    }

    public static void Test_PortActive_Silent()
    {
        var findings = new DmdComPortScanner(
            _ => true,
            _ => IniWithPin2dmdOnCom4,
            () => new HashSet<string> { "COM4" }
        ).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_IniAbsent_Silent()
    {
        var findings = new DmdComPortScanner(
            _ => false,
            _ => throw new InvalidOperationException("should not read"),
            () => new HashSet<string> { "COM1" }
        ).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_NoVPinMameDir_Silent()
    {
        var findings = new DmdComPortScanner(
            _ => true,
            _ => IniWithPin2dmdOnCom4,
            () => new HashSet<string> { "COM1" }
        ).Scan(Ctx(vpinmameDir: null)).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_EmptyActivePortSet_Silent()
    {
        // Can't confirm the enumeration itself worked -> bias to silence, not "all missing".
        var findings = new DmdComPortScanner(
            _ => true,
            _ => IniWithPin2dmdOnCom4,
            () => new HashSet<string>()
        ).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_ReadThrows_Silent()
    {
        var findings = new DmdComPortScanner(
            _ => true,
            _ => throw new IOException(),
            () => new HashSet<string> { "COM1" }
        ).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }
}
