using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>
/// LOT C (spec 10/08) — <see cref="DmdDeviceIniParser"/>'s new <c>[virtualdmd]</c>/hardware-section
/// parsing, plus <see cref="DmdConfigScanner.Evaluate"/> (C.1 <c>DMD_VIRTUAL_DISABLED</c>, C.2
/// <c>DMD_POSITION_OFFSCREEN</c>).
/// </summary>
public static class DmdConfigScannerTests
{
    private static readonly MonitorRect[] OneMonitor1080p = { new(0, 0, 1920, 1080, @"\\.\DISPLAY1") };

    // ───────────────────────── DmdDeviceIniParser.TryParseVirtualDmdConfig ─────────────────────────

    public static void Test_Parser_NoVirtualDmdSection_ReturnsNull()
    {
        var cfg = DmdDeviceIniParser.TryParseVirtualDmdConfig("[pin2dmd]\nenabled = true\n");
        Assert.Equal(null, cfg);
    }

    public static void Test_Parser_VirtualDmdSection_ParsesAllFields()
    {
        var ini = "[virtualdmd]\nenabled = false\nleft = -1920\ntop = 0\nwidth = 1024\nheight = 256\n";
        var cfg = DmdDeviceIniParser.TryParseVirtualDmdConfig(ini);
        Assert.NotNull(cfg);
        Assert.Equal(false, cfg!.Enabled);
        Assert.Equal(-1920, cfg.Left);
        Assert.Equal(0, cfg.Top);
        Assert.Equal(1024, cfg.Width);
        Assert.Equal(256, cfg.Height);
    }

    public static void Test_Parser_MissingKey_StaysNull_NeverAssumed()
    {
        // 'enabled' key simply absent -> Enabled must be null, not false.
        var cfg = DmdDeviceIniParser.TryParseVirtualDmdConfig("[virtualdmd]\nleft = 0\ntop = 0\n");
        Assert.NotNull(cfg);
        Assert.Equal(null, cfg!.Enabled);
        Assert.Equal(null, cfg.Width);
    }

    public static void Test_Parser_OnlyReadsVirtualDmdSection_IgnoresOthers()
    {
        var ini = "[pin2dmd]\nenabled = true\nleft = 999\n[virtualdmd]\nenabled = true\n";
        var cfg = DmdDeviceIniParser.TryParseVirtualDmdConfig(ini);
        Assert.NotNull(cfg);
        Assert.Equal(true, cfg!.Enabled);
        Assert.Equal(null, cfg.Left); // 'left = 999' belongs to [pin2dmd], not [virtualdmd]
    }

    // ───────────────────────── DmdDeviceIniParser.AnyHardwareDeviceEnabled ─────────────────────────

    public static void Test_Parser_HardwareEnabled_Pin2Dmd_DetectsIt()
    {
        Assert.Equal(true, DmdDeviceIniParser.AnyHardwareDeviceEnabled("[pin2dmd]\nenabled = true\n"));
    }

    public static void Test_Parser_HardwareEnabled_ZedmdVariant_DetectsIt()
    {
        Assert.Equal(true, DmdDeviceIniParser.AnyHardwareDeviceEnabled("[zedmdhdwifi]\nenabled = 1\n"));
    }

    public static void Test_Parser_HardwareEnabled_NonHardwareSectionIgnored()
    {
        // [pinup]/[video]/[networkstream] etc. are output sinks, not physical DMDs -> must not count.
        Assert.Equal(false, DmdDeviceIniParser.AnyHardwareDeviceEnabled("[pinup]\nenabled = true\n[video]\nenabled = true\n"));
    }

    public static void Test_Parser_HardwareDisabled_ReturnsFalse()
    {
        Assert.Equal(false, DmdDeviceIniParser.AnyHardwareDeviceEnabled("[pin2dmd]\nenabled = false\n"));
    }

    // ───────────────────────── DmdConfigScanner.Evaluate — C.1 DMD_VIRTUAL_DISABLED ─────────────────────────

    public static void Test_Evaluate_VirtualOff_NoHardware_EmitsNote()
    {
        var cfg = new DmdDeviceIniParser.VirtualDmdConfig(false, null, null, null, null);
        var findings = DmdConfigScanner.Evaluate(cfg, anyHardwareDeviceEnabled: false, monitors: null, "x/dmddevice.ini", "dmd-config");
        Assert.Equal(1, findings.Count(f => f.Code == "DMD_VIRTUAL_DISABLED"));
        Assert.Equal(Severity.Note, findings.Single(f => f.Code == "DMD_VIRTUAL_DISABLED").Severity);
    }

    public static void Test_Evaluate_VirtualOff_HardwareEnabled_Silent()
    {
        // A real DMD is enabled instead -> this is an intentional, legitimate configuration.
        var cfg = new DmdDeviceIniParser.VirtualDmdConfig(false, null, null, null, null);
        var findings = DmdConfigScanner.Evaluate(cfg, anyHardwareDeviceEnabled: true, monitors: null, "x/dmddevice.ini", "dmd-config");
        Assert.Equal(0, findings.Count(f => f.Code == "DMD_VIRTUAL_DISABLED"));
    }

    public static void Test_Evaluate_VirtualEnabledKeyAbsent_NeverAssumedDisabled()
    {
        // Enabled == null (key wasn't present) must NOT be treated as false.
        var cfg = new DmdDeviceIniParser.VirtualDmdConfig(null, null, null, null, null);
        var findings = DmdConfigScanner.Evaluate(cfg, anyHardwareDeviceEnabled: false, monitors: null, "x/dmddevice.ini", "dmd-config");
        Assert.Equal(0, findings.Count(f => f.Code == "DMD_VIRTUAL_DISABLED"));
    }

    public static void Test_Evaluate_VirtualOn_NeverEmitsDisabledNote()
    {
        var cfg = new DmdDeviceIniParser.VirtualDmdConfig(true, null, null, null, null);
        var findings = DmdConfigScanner.Evaluate(cfg, anyHardwareDeviceEnabled: false, monitors: null, "x/dmddevice.ini", "dmd-config");
        Assert.Equal(0, findings.Count(f => f.Code == "DMD_VIRTUAL_DISABLED"));
    }

    // ───────────────────────── DmdConfigScanner.Evaluate — C.2 DMD_POSITION_OFFSCREEN ─────────────────────────

    public static void Test_Evaluate_PositionFullyOffscreen_EmitsWarning()
    {
        var cfg = new DmdDeviceIniParser.VirtualDmdConfig(true, 5000, 5000, 1024, 256);
        var findings = DmdConfigScanner.Evaluate(cfg, anyHardwareDeviceEnabled: false, OneMonitor1080p, "x/dmddevice.ini", "dmd-config");
        Assert.Equal(1, findings.Count(f => f.Code == "DMD_POSITION_OFFSCREEN"));
        Assert.Equal(Severity.Warning, findings.Single(f => f.Code == "DMD_POSITION_OFFSCREEN").Severity);
    }

    public static void Test_Evaluate_PositionOnMonitor_Silent()
    {
        var cfg = new DmdDeviceIniParser.VirtualDmdConfig(true, 0, 0, 1024, 256);
        var findings = DmdConfigScanner.Evaluate(cfg, anyHardwareDeviceEnabled: false, OneMonitor1080p, "x/dmddevice.ini", "dmd-config");
        Assert.Equal(0, findings.Count(f => f.Code == "DMD_POSITION_OFFSCREEN"));
    }

    public static void Test_Evaluate_NegativeCoordinates_NotTreatedAsError_WhenOnAMonitor()
    {
        // A monitor placed left of the primary is valid — negative left/top alone is never the test.
        var monitors = new MonitorRect[] { new(-1920, 0, 1920, 1080, @"\\.\DISPLAY2"), new(0, 0, 1920, 1080, @"\\.\DISPLAY1") };
        var cfg = new DmdDeviceIniParser.VirtualDmdConfig(true, -1920, 0, 1024, 256);
        var findings = DmdConfigScanner.Evaluate(cfg, anyHardwareDeviceEnabled: false, monitors, "x/dmddevice.ini", "dmd-config");
        Assert.Equal(0, findings.Count(f => f.Code == "DMD_POSITION_OFFSCREEN"));
    }

    public static void Test_Evaluate_MonitorsUnmeasurable_Silent()
    {
        var cfg = new DmdDeviceIniParser.VirtualDmdConfig(true, 5000, 5000, 1024, 256);
        var findings = DmdConfigScanner.Evaluate(cfg, anyHardwareDeviceEnabled: false, monitors: null, "x/dmddevice.ini", "dmd-config");
        Assert.Equal(0, findings.Count(f => f.Code == "DMD_POSITION_OFFSCREEN"));
    }

    public static void Test_Evaluate_PositionKeysPartiallyMissing_Silent()
    {
        // width/height absent -> cannot honestly evaluate a rectangle -> silence, not a guess.
        var cfg = new DmdDeviceIniParser.VirtualDmdConfig(true, 5000, 5000, null, null);
        var findings = DmdConfigScanner.Evaluate(cfg, anyHardwareDeviceEnabled: false, OneMonitor1080p, "x/dmddevice.ini", "dmd-config");
        Assert.Equal(0, findings.Count(f => f.Code == "DMD_POSITION_OFFSCREEN"));
    }

    public static void Test_Evaluate_NoVirtualDmdSection_NoFindingsAtAll()
    {
        var findings = DmdConfigScanner.Evaluate(null, anyHardwareDeviceEnabled: false, OneMonitor1080p, "x/dmddevice.ini", "dmd-config");
        Assert.Equal(0, findings.Count);
    }

    // ───────────────────────── End-to-end Scan() ─────────────────────────

    public static void Test_Scan_NoVPinMameDir_Silent()
    {
        var layout = new InstallLayout { RootPath = "/install" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new DmdConfigScanner(fileExists: _ => true, readAllText: _ => "[virtualdmd]\nenabled = false\n");
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_Scan_MissingIni_Silent()
    {
        var layout = new InstallLayout { RootPath = "/install", VPinMameDir = "/install/VPinMAME" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new DmdConfigScanner(fileExists: _ => false);
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_Scan_UnreadableIni_NeverThrows_Silent()
    {
        var layout = new InstallLayout { RootPath = "/install", VPinMameDir = "/install/VPinMAME" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new DmdConfigScanner(fileExists: _ => true, readAllText: _ => throw new IOException("locked"));
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_Scan_EndToEnd_BothFindings()
    {
        var ini = "[virtualdmd]\nenabled = false\nleft = 9000\ntop = 9000\nwidth = 1024\nheight = 256\n";
        var layout = new InstallLayout { RootPath = "/install", VPinMameDir = "/install/VPinMAME" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new DmdConfigScanner(
            fileExists: _ => true,
            readAllText: _ => ini,
            getMonitors: () => OneMonitor1080p);
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "DMD_VIRTUAL_DISABLED"));
        Assert.Equal(1, findings.Count(f => f.Code == "DMD_POSITION_OFFSCREEN"));
    }
}
