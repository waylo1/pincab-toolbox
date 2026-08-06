using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure zero-byte selection.</summary>
public static class NvramInspectorTests
{
    public static void Test_FindEmpty_ReturnsOnlyZeroByteFiles()
    {
        var files = new (string, long)[] { ("afm_113b.nv", 0), ("mm_109c.nv", 8192), ("tz_94h.nv", 0) };
        var empty = NvramInspector.FindEmpty(files);
        Assert.Equal(2, empty.Count);
        Assert.True(empty.Contains("afm_113b.nv") && empty.Contains("tz_94h.nv"));
    }

    public static void Test_FindEmpty_NoFiles_IsEmpty()
    {
        Assert.Equal(0, NvramInspector.FindEmpty(Array.Empty<(string, long)>()).Count);
    }

    public static void Test_FindEmpty_AllNonZero_IsEmpty()
    {
        var files = new (string, long)[] { ("afm_113b.nv", 8192), ("mm_109c.nv", 4096) };
        Assert.Equal(0, NvramInspector.FindEmpty(files).Count);
    }
}

/// <summary>End-to-end scanner behaviour, with the folder enumeration injected.</summary>
public static class NvramScannerTests
{
    private static ScanContext Ctx(string? vpinmameDir = "/x/VPinMAME")
    {
        var layout = new InstallLayout { RootPath = "/x", VPinMameDir = vpinmameDir };
        return new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
    }

    public static void Test_NoVPinMameDir_Silent()
    {
        var ctx = Ctx(vpinmameDir: null);
        var findings = new NvramScanner(_ => new[] { ("x.nv", 0L) }).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_EmptyNvramFile_Warns()
    {
        var ctx = Ctx();
        var findings = new NvramScanner(dir => new[] { ("afm_113b.nv", 0L) }).Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "NVRAM_EMPTY"));
        var f = findings.Single(f => f.Code == "NVRAM_EMPTY");
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("afm_113b", f.Subject);
        Assert.True(f.FilePath!.Replace('\\', '/').EndsWith("VPinMAME/nvram/afm_113b.nv"));
    }

    public static void Test_NonEmptyNvramFile_Silent()
    {
        var ctx = Ctx();
        var findings = new NvramScanner(dir => new[] { ("afm_113b.nv", 8192L) }).Scan(ctx).ToList();
        Assert.False(findings.Any(f => f.Code == "NVRAM_EMPTY"));
    }

    public static void Test_NoNvFiles_Silent()
    {
        var ctx = Ctx();
        var findings = new NvramScanner(dir => Array.Empty<(string, long)>()).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_EnumerationThrows_SilentNeverThrows()
    {
        var ctx = Ctx();
        var scanner = new NvramScanner(dir => throw new UnauthorizedAccessException("locked"));
        var findings = scanner.Scan(ctx).ToList(); // must not throw
        Assert.Equal(0, findings.Count);
    }

    public static void Test_MultipleEmptyFiles_AllReported()
    {
        var ctx = Ctx();
        var findings = new NvramScanner(dir => new[] { ("a.nv", 0L), ("b.nv", 0L), ("c.nv", 512L) }).Scan(ctx).ToList();
        Assert.Equal(2, findings.Count(f => f.Code == "NVRAM_EMPTY"));
    }
}
