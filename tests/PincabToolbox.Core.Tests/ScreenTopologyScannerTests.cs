using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;
using PincabToolbox.Core.Vpx;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure parsing + geometry, verified against the real B2S Backglass Server wiki examples.</summary>
public static class ScreenTopologyAnalyzerTests
{
    // Verbatim (trimmed to what the parser reads) from the official wiki's dual-screen worked
    // example: github.com/vpinball/b2s-backglass/wiki/Dual-Screen-examples.
    private const string RealV2Example =
        "# V2.0.0-c311a21\n" +
        "# File is saved with B2S_ScreenResIdentifier release 2.0.0\n" +
        "# Playfield Screen resolution width/height\n" +
        "1920\n" +
        "1080\n" +
        "# width/height of the Backglass\n" +
        "1280\n" +
        "655\n" +
        "# Define Backglass using the screen index (=x) -> It is always the second screen from left\n" +
        "=2\n" +
        "# Backglass x/y position relative to the upper left corner Of the screen selected\n" +
        "0\n" +
        "0\n" +
        "# width/height Of the B2S (or Full) DMD area In pixels\n" +
        "676\n" +
        "320\n";

    // Verbatim from the shipped ScreenResTemplate.txt (repo root) — pre-2.0.0 shape, no "# V2" marker.
    private const string TemplateNoV2 =
        "# This is a ScreenRes file for the B2SBackglassServer.\n" +
        "# Playfield Screen resolution width/height\n" +
        "1920\n" +
        "1080\n" +
        "# Backglass width/height\n" +
        "800\n" +
        "600\n" +
        "# Backglass Display Devicename screen number\n" +
        "1\n" +
        "# Backglass x/y position relative to the upper left corner of the screen selected\n" +
        "0\n" +
        "0\n";

    public static void Test_ParseBackglassPlacement_ValidV2File_ExtractsFields()
    {
        var placement = ScreenTopologyAnalyzer.ParseBackglassPlacement(RealV2Example);
        Assert.True(placement.HasValue);
        Assert.Equal("=2", placement!.Value.Selector);
        Assert.Equal(0, placement.Value.X);
        Assert.Equal(0, placement.Value.Y);
        Assert.Equal(1280, placement.Value.Width);
        Assert.Equal(655, placement.Value.Height);
    }

    public static void Test_ParseBackglassPlacement_NoV2Marker_ReturnsNull()
    {
        // Scope cut #1: pre-2.0.0 files can silently swap Backglass/Background — refused outright.
        var placement = ScreenTopologyAnalyzer.ParseBackglassPlacement(TemplateNoV2);
        Assert.False(placement.HasValue);
    }

    public static void Test_ParseBackglassPlacement_V2MarkerNotFirstLine_StillDetected()
    {
        var text = "# some other comment\n" + RealV2Example;
        var placement = ScreenTopologyAnalyzer.ParseBackglassPlacement(text);
        Assert.True(placement.HasValue);
    }

    public static void Test_ParseBackglassPlacement_TooFewLines_ReturnsNull()
    {
        var placement = ScreenTopologyAnalyzer.ParseBackglassPlacement("# V2\n1920\n1080\n");
        Assert.False(placement.HasValue);
    }

    public static void Test_ParseBackglassPlacement_NonNumericField_ReturnsNull()
    {
        var text = "# V2\n1920\n1080\nNOTANUMBER\n655\n=2\n0\n0\n";
        var placement = ScreenTopologyAnalyzer.ParseBackglassPlacement(text);
        Assert.False(placement.HasValue);
    }

    public static void Test_ParseBackglassPlacement_ZeroWidth_ReturnsNull()
    {
        var text = "# V2\n1920\n1080\n0\n655\n=2\n0\n0\n";
        var placement = ScreenTopologyAnalyzer.ParseBackglassPlacement(text);
        Assert.False(placement.HasValue);
    }

    public static void Test_ParseBackglassPlacement_EmptyText_ReturnsNull()
    {
        Assert.False(ScreenTopologyAnalyzer.ParseBackglassPlacement("").HasValue);
    }

    public static void Test_ResolveScreen_AtSelector_MatchesAbsoluteX()
    {
        var monitors = new[]
        {
            new MonitorRect(0, 0, 1920, 1080, "\\\\.\\DISPLAY1"),
            new MonitorRect(1920, 0, 1280, 1024, "\\\\.\\DISPLAY2"),
        };
        var screen = ScreenTopologyAnalyzer.ResolveScreen("@1920", monitors);
        Assert.True(screen.HasValue);
        Assert.Equal("\\\\.\\DISPLAY2", screen!.Value.DeviceName);
    }

    public static void Test_ResolveScreen_AtSelector_NoMatch_ReturnsNull()
    {
        var monitors = new[] { new MonitorRect(0, 0, 1920, 1080, "\\\\.\\DISPLAY1") };
        Assert.False(ScreenTopologyAnalyzer.ResolveScreen("@9999", monitors).HasValue);
    }

    public static void Test_ResolveScreen_EqualsSelector_PicksNthLeftToRight_OneBased()
    {
        var monitors = new[]
        {
            new MonitorRect(1920, 0, 1280, 1024, "\\\\.\\DISPLAY2"), // rightmost, listed first on purpose
            new MonitorRect(0, 0, 1920, 1080, "\\\\.\\DISPLAY1"),    // leftmost
        };
        var screen = ScreenTopologyAnalyzer.ResolveScreen("=2", monitors);
        Assert.True(screen.HasValue);
        Assert.Equal("\\\\.\\DISPLAY2", screen!.Value.DeviceName); // 2nd from left, not 2nd in the list
    }

    public static void Test_ResolveScreen_EqualsSelector_OutOfRange_ReturnsNull()
    {
        var monitors = new[] { new MonitorRect(0, 0, 1920, 1080, "\\\\.\\DISPLAY1") };
        Assert.False(ScreenTopologyAnalyzer.ResolveScreen("=5", monitors).HasValue);
    }

    public static void Test_ResolveScreen_BareInteger_MatchesDeviceName()
    {
        var monitors = new[]
        {
            new MonitorRect(0, 0, 1920, 1080, "\\\\.\\DISPLAY1"),
            new MonitorRect(1920, 0, 1280, 1024, "\\\\.\\DISPLAY2"),
        };
        var screen = ScreenTopologyAnalyzer.ResolveScreen("2", monitors);
        Assert.True(screen.HasValue);
        Assert.Equal(1920, screen!.Value.X);
    }

    public static void Test_ResolveScreen_BareInteger_NoMatch_ReturnsNull()
    {
        var monitors = new[] { new MonitorRect(0, 0, 1920, 1080, "\\\\.\\DISPLAY1") };
        Assert.False(ScreenTopologyAnalyzer.ResolveScreen("7", monitors).HasValue);
    }

    public static void Test_ResolveScreen_NoMonitors_ReturnsNull()
    {
        Assert.False(ScreenTopologyAnalyzer.ResolveScreen("1", Array.Empty<MonitorRect>()).HasValue);
    }

    public static void Test_ResolveScreen_UnknownSyntax_ReturnsNull()
    {
        var monitors = new[] { new MonitorRect(0, 0, 1920, 1080, "\\\\.\\DISPLAY1") };
        Assert.False(ScreenTopologyAnalyzer.ResolveScreen("!weird", monitors).HasValue);
    }

    public static void Test_IsOffScreen_FullyInsideOneMonitor_False()
    {
        var monitors = new[] { new MonitorRect(0, 0, 1920, 1080, "\\\\.\\DISPLAY1") };
        Assert.False(ScreenTopologyAnalyzer.IsOffScreen(100, 100, 800, 600, monitors));
    }

    public static void Test_IsOffScreen_PartiallyOverlapping_False()
    {
        var monitors = new[] { new MonitorRect(0, 0, 1920, 1080, "\\\\.\\DISPLAY1") };
        // Straddles the right edge — still partly visible, not "off screen" under the strict scope.
        Assert.False(ScreenTopologyAnalyzer.IsOffScreen(1800, 0, 800, 600, monitors));
    }

    public static void Test_IsOffScreen_CompletelyOutsideAllMonitors_True()
    {
        var monitors = new[] { new MonitorRect(0, 0, 1920, 1080, "\\\\.\\DISPLAY1") };
        Assert.True(ScreenTopologyAnalyzer.IsOffScreen(5000, 5000, 800, 600, monitors));
    }

    public static void Test_IsOffScreen_NegativeCoordinatesButOnScreen_False()
    {
        // A monitor positioned left of the primary has a negative virtual-desktop origin — must not
        // be mistaken for "off screen" on that basis alone.
        var monitors = new[]
        {
            new MonitorRect(-1920, 0, 1920, 1080, "\\\\.\\DISPLAY2"),
            new MonitorRect(0, 0, 1920, 1080, "\\\\.\\DISPLAY1"),
        };
        Assert.False(ScreenTopologyAnalyzer.IsOffScreen(-1800, 100, 800, 600, monitors));
    }

    public static void Test_IsOffScreen_InTheGapBetweenTwoMonitors_True()
    {
        // Overlaps neither monitor's rectangle even though it sits within the bounding box of their
        // union — the literal "no rectangle intersects" reading, stricter than a bounding-box check.
        var monitors = new[]
        {
            new MonitorRect(0, 0, 1920, 1080, "\\\\.\\DISPLAY1"),
            new MonitorRect(0, 1200, 1920, 1080, "\\\\.\\DISPLAY2"), // below, with a 120px gap
        };
        Assert.True(ScreenTopologyAnalyzer.IsOffScreen(500, 1090, 200, 100, monitors));
    }
}

/// <summary>End-to-end scanner behaviour, with file reads + monitor enumeration injected.</summary>
public static class ScreenTopologyScannerTests
{
    private static readonly MonitorRect[] OneMonitor = { new(0, 0, 1920, 1080, "\\\\.\\DISPLAY1") };

    private const string OnScreenV2 =
        "# V2\n1920\n1080\n800\n600\n1\n100\n100\n";

    private const string OffScreenV2 =
        "# V2\n1920\n1080\n800\n600\n1\n5000\n5000\n";

    private static ScanContext CtxWithTablesDir(string tablesDir = "/x/Tables")
    {
        var layout = new InstallLayout { RootPath = "/x", TablesDir = tablesDir };
        return new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
    }

    public static void Test_NoTablesDir_Silent()
    {
        var layout = new InstallLayout { RootPath = "/x", TablesDir = null };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var findings = new ScreenTopologyScanner(_ => OffScreenV2, () => OneMonitor).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_NoMonitors_Silent()
    {
        var ctx = CtxWithTablesDir();
        var findings = new ScreenTopologyScanner(_ => OffScreenV2, () => null).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_GetMonitorsThrows_Silent()
    {
        var ctx = CtxWithTablesDir();
        var findings = new ScreenTopologyScanner(_ => OffScreenV2, () => throw new InvalidOperationException()).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_NoScreenResFiles_Silent()
    {
        var ctx = CtxWithTablesDir();
        var findings = new ScreenTopologyScanner(_ => null, () => OneMonitor).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_GlobalScreenResOffScreen_Warns()
    {
        var ctx = CtxWithTablesDir();
        var findings = new ScreenTopologyScanner(p => p.Replace('\\', '/').EndsWith("ScreenRes.txt") ? OffScreenV2 : null, () => OneMonitor).Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "DISPLAY_OFFSCREEN"));
        Assert.Equal("ScreenRes.txt", findings.Single().Subject);
    }

    public static void Test_GlobalScreenResOnScreen_Silent()
    {
        var ctx = CtxWithTablesDir();
        var findings = new ScreenTopologyScanner(p => p.Replace('\\', '/').EndsWith("ScreenRes.txt") ? OnScreenV2 : null, () => OneMonitor).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_NoV2Marker_Silent()
    {
        var ctx = CtxWithTablesDir();
        var noV2Offscreen = "1920\n1080\n800\n600\n1\n5000\n5000\n"; // same off-screen coords, no marker
        var findings = new ScreenTopologyScanner(_ => noV2Offscreen, () => OneMonitor).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_PerTableResOverride_EvaluatedIndependentlyFromGlobal()
    {
        var ctx = CtxWithTablesDir();
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = "Sub S()\nEnd Sub" };

        var findings = new ScreenTopologyScanner(p =>
        {
            var norm = p.Replace('\\', '/');
            if (norm.EndsWith("Foo.res")) return OffScreenV2;   // per-table override: broken
            if (norm.EndsWith("ScreenRes.txt")) return OnScreenV2; // global: healthy
            return null;
        }, () => OneMonitor).Scan(ctx).ToList();

        Assert.Equal(1, findings.Count(f => f.Code == "DISPLAY_OFFSCREEN"));
        Assert.Equal("Foo", findings.Single().Subject);
    }

    public static void Test_ReadTextThrows_SkippedNotCrashed()
    {
        var ctx = CtxWithTablesDir();
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = "Sub S()\nEnd Sub" };
        var findings = new ScreenTopologyScanner(_ => throw new IOException("locked"), () => OneMonitor).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_MultipleTablesEachWithOwnRes_AllEvaluatedIndependently()
    {
        var ctx = CtxWithTablesDir();
        ctx.Tables["A.vpx"] = new VpxTableData { FilePath = "A.vpx", Script = "Sub S()\nEnd Sub" };
        ctx.Tables["B.vpx"] = new VpxTableData { FilePath = "B.vpx", Script = "Sub S()\nEnd Sub" };

        var findings = new ScreenTopologyScanner(p =>
        {
            var norm = p.Replace('\\', '/');
            if (norm.EndsWith("A.res")) return OffScreenV2;
            if (norm.EndsWith("B.res")) return OffScreenV2;
            return null; // no global ScreenRes.txt
        }, () => OneMonitor).Scan(ctx).ToList();

        Assert.Equal(2, findings.Count(f => f.Code == "DISPLAY_OFFSCREEN"));
    }
}
