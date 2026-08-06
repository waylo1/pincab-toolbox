using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure broken-junction decision.</summary>
public static class JunctionInspectorTests
{
    public static void Test_ReparsePointWithMissingTarget_IsBroken()
    {
        Assert.True(JunctionInspector.IsBroken(isReparsePoint: true, targetExists: false));
    }

    public static void Test_ReparsePointWithExistingTarget_IsNotBroken()
    {
        Assert.False(JunctionInspector.IsBroken(isReparsePoint: true, targetExists: true));
    }

    public static void Test_NotAReparsePoint_IsNotBroken()
    {
        Assert.False(JunctionInspector.IsBroken(isReparsePoint: false, targetExists: false));
        Assert.False(JunctionInspector.IsBroken(isReparsePoint: false, targetExists: true));
    }
}

/// <summary>End-to-end scanner behaviour, with reparse-point reads + directory listing injected.</summary>
public static class JunctionScannerTests
{
    private static ScanContext CtxWithRoot(string root = "/x")
    {
        var layout = new InstallLayout { RootPath = root };
        return new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
    }

    public static void Test_NotAReparsePoint_Silent()
    {
        var ctx = CtxWithRoot();
        var findings = new JunctionScanner(_ => null, _ => true, _ => Array.Empty<string>()).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_BrokenJunction_OnRootPath_Warns()
    {
        var ctx = CtxWithRoot("/x");
        var findings = new JunctionScanner(
            p => p == "/x" ? "/gone" : null,
            _ => false,
            _ => Array.Empty<string>()).Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "BROKEN_JUNCTION"));
        var f = findings.Single(f => f.Code == "BROKEN_JUNCTION");
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("/x", f.FilePath);
        Assert.Equal("/gone", f.Args[1]);
    }

    public static void Test_HealthyJunction_TargetExists_Silent()
    {
        var ctx = CtxWithRoot("/x");
        var findings = new JunctionScanner(
            p => p == "/x" ? "/healthy-target" : null,
            _ => true,
            _ => Array.Empty<string>()).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_BrokenJunction_OnSubdirectory_Warns()
    {
        var ctx = CtxWithRoot("/x");
        var findings = new JunctionScanner(
            p => p == "/x/roms" ? "/gone" : null,
            _ => false,
            p => p == "/x" ? new[] { "/x/roms" } : Array.Empty<string>()).Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "BROKEN_JUNCTION"));
        Assert.Equal("/x/roms", findings.Single().FilePath);
    }

    public static void Test_GetLinkTargetThrows_Silent()
    {
        var ctx = CtxWithRoot("/x");
        var findings = new JunctionScanner(
            _ => throw new UnauthorizedAccessException(),
            _ => true,
            _ => Array.Empty<string>()).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_DirectoryExistsThrows_Silent()
    {
        var ctx = CtxWithRoot("/x");
        var findings = new JunctionScanner(
            p => p == "/x" ? "/gone" : null,
            _ => throw new IOException(),
            _ => Array.Empty<string>()).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_ListSubdirectoriesThrows_ContinuesToNextRoot()
    {
        var layout = new InstallLayout { RootPath = "/x", TablesDir = "/y" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var findings = new JunctionScanner(
            p => p == "/y" ? "/gone" : null,
            _ => false,
            p => p == "/x" ? throw new UnauthorizedAccessException() : Array.Empty<string>()
        ).Scan(ctx).ToList(); // must not throw
        Assert.Equal(1, findings.Count(f => f.Code == "BROKEN_JUNCTION"));
        Assert.Equal("/y", findings.Single().FilePath);
    }

    public static void Test_RootAlsoListedAsChild_EvaluatedOnce()
    {
        var layout = new InstallLayout { RootPath = "/x", TablesDir = "/x/Tables" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var calls = 0;
        var findings = new JunctionScanner(
            p =>
            {
                if (p == "/x/Tables") calls++;
                return null;
            },
            _ => true,
            p => p == "/x" ? new[] { "/x/Tables" } : Array.Empty<string>()
        ).Scan(ctx).ToList();
        Assert.Equal(1, calls); // visited once (as TablesDir root), not again as RootPath's child
    }

    public static void Test_MultipleBrokenJunctions_AllReported()
    {
        var layout = new InstallLayout { RootPath = "/x", TablesDir = "/y", VPinMameDir = "/z" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var findings = new JunctionScanner(
            _ => "/gone",
            _ => false,
            _ => Array.Empty<string>()
        ).Scan(ctx).ToList();
        Assert.Equal(3, findings.Count(f => f.Code == "BROKEN_JUNCTION"));
    }
}
