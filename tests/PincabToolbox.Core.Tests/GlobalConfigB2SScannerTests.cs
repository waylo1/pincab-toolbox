using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;

namespace PincabToolbox.Core.Tests;

/// <summary>End-to-end scanner behaviour, with the file-system lookups injected.</summary>
public static class GlobalConfigB2SScannerTests
{
    private static ScanContext Ctx() =>
        new() { Layout = new InstallLayout { RootPath = "/x" }, Profile = Fixtures.Profile() };

    public static void Test_NoB2SBinaryFound_Silent()
    {
        var scanner = new GlobalConfigB2SScanner(
            findFiles: (root, pattern, depth) => Array.Empty<string>(),
            fileExists: _ => false);
        var findings = scanner.Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_B2SInstalled_ConfigMissing_Warns()
    {
        var scanner = new GlobalConfigB2SScanner(
            findFiles: (root, pattern, depth) => new[] { "/x/Tables/B2SBackglassServer.dll" },
            fileExists: _ => false);
        var findings = scanner.Scan(Ctx()).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "GLOBALCONFIG_B2S_MISSING"));
        var f = findings.Single(f => f.Code == "GLOBALCONFIG_B2S_MISSING");
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.True(f.FilePath!.Replace('\\', '/').EndsWith("Tables/GlobalConfig_B2SServer.xml"));
    }

    public static void Test_B2SInstalled_ConfigPresent_Silent()
    {
        var scanner = new GlobalConfigB2SScanner(
            findFiles: (root, pattern, depth) => new[] { "/x/Tables/B2SBackglassServer.dll" },
            fileExists: _ => true);
        var findings = scanner.Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_FindFilesThrows_SilentNeverThrows()
    {
        var scanner = new GlobalConfigB2SScanner(
            findFiles: (root, pattern, depth) => throw new UnauthorizedAccessException("locked"),
            fileExists: _ => false);
        var findings = scanner.Scan(Ctx()).ToList(); // must not throw
        Assert.Equal(0, findings.Count);
    }

    public static void Test_FileExistsThrows_SilentNeverThrows()
    {
        var scanner = new GlobalConfigB2SScanner(
            findFiles: (root, pattern, depth) => new[] { "/x/Tables/B2SBackglassServer.dll" },
            fileExists: _ => throw new IOException("locked"));
        var findings = scanner.Scan(Ctx()).ToList(); // must not throw
        Assert.Equal(0, findings.Count);
    }

    public static void Test_NoRootPath_Silent()
    {
        var ctx = new ScanContext { Layout = new InstallLayout { RootPath = "" }, Profile = Fixtures.Profile() };
        var scanner = new GlobalConfigB2SScanner(
            findFiles: (root, pattern, depth) => new[] { "/x/Tables/B2SBackglassServer.dll" },
            fileExists: _ => false);
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }
}
