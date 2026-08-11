using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;

namespace PincabToolbox.Core.Tests;

/// <summary>LOT F (spec 10/08) — <see cref="ScreenResUnparsedScanner"/>.</summary>
public static class ScreenResUnparsedScannerTests
{
    private const string LegacyNoV2 = "1920\n1080\n800\n600\n1\n100\n100\n";
    private const string OnScreenV2 = "# V2\n1920\n1080\n800\n600\n1\n100\n100\n";

    public static void Test_NoFile_Silent()
    {
        var layout = new InstallLayout { RootPath = "/install", TablesDir = "/install/tables" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new ScreenResUnparsedScanner(readText: _ => null);
        Assert.Equal(0, scanner.Scan(ctx).Count());
    }

    public static void Test_NoTablesDir_Silent()
    {
        var layout = new InstallLayout { RootPath = "/install" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new ScreenResUnparsedScanner(readText: _ => LegacyNoV2);
        Assert.Equal(0, scanner.Scan(ctx).Count());
    }

    public static void Test_PresentWithoutV2Marker_EmitsNote()
    {
        var layout = new InstallLayout { RootPath = "/install", TablesDir = "/install/tables" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new ScreenResUnparsedScanner(readText: p => p.EndsWith("ScreenRes.txt") ? LegacyNoV2 : null);
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "SCREENRES_UNPARSED"));
        Assert.Equal(Severity.Note, findings.Single(f => f.Code == "SCREENRES_UNPARSED").Severity);
    }

    public static void Test_PresentWithV2Marker_Silent_OwnedByScreenTopologyScanner()
    {
        var layout = new InstallLayout { RootPath = "/install", TablesDir = "/install/tables" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new ScreenResUnparsedScanner(readText: p => p.EndsWith("ScreenRes.txt") ? OnScreenV2 : null);
        Assert.Equal(0, scanner.Scan(ctx).Count());
    }

    public static void Test_ReadThrows_Silent_NeverAFalsePositive()
    {
        var layout = new InstallLayout { RootPath = "/install", TablesDir = "/install/tables" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new ScreenResUnparsedScanner(readText: _ => throw new IOException("locked"));
        Assert.Equal(0, scanner.Scan(ctx).Count());
    }

    public static void Test_PerTableResOverride_UnparsedIndependentlyFromGlobal()
    {
        var layout = new InstallLayout { RootPath = "/install", TablesDir = "/install/tables" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        ctx.Tables["/install/tables/mytable.vpx"] = new PincabToolbox.Core.Vpx.VpxTableData { FilePath = "/install/tables/mytable.vpx" };

        var scanner = new ScreenResUnparsedScanner(readText: p =>
            p.EndsWith("ScreenRes.txt") ? OnScreenV2 :
            p.EndsWith("mytable.res") ? LegacyNoV2 : null);

        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "SCREENRES_UNPARSED"));
        Assert.Equal("mytable", findings.Single().Subject);
    }
}
