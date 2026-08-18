using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure name matching.</summary>
public static class SharedScriptDetectorTests
{
    public static void Test_IsKnownSharedScript_MatchesAllFour_CaseInsensitive()
    {
        Assert.True(SharedScriptDetector.IsKnownSharedScript("core.vbs"));
        Assert.True(SharedScriptDetector.IsKnownSharedScript("CORE.VBS"));
        Assert.True(SharedScriptDetector.IsKnownSharedScript("Controller.vbs"));
        Assert.True(SharedScriptDetector.IsKnownSharedScript("VPMKeys.vbs"));
        Assert.True(SharedScriptDetector.IsKnownSharedScript("nudge.vbs"));
    }

    public static void Test_IsKnownSharedScript_UnrelatedFile_False()
    {
        Assert.False(SharedScriptDetector.IsKnownSharedScript("myscript.vbs"));
        Assert.False(SharedScriptDetector.IsKnownSharedScript("core.txt"));
    }
}

/// <summary>End-to-end scanner behaviour, with the Tables/ enumeration injected.</summary>
public static class ScriptDoctorScannerTests
{
    private static ScanContext Ctx(string? tablesDir = "/x/Tables") =>
        new() { Layout = new InstallLayout { RootPath = "/x", TablesDir = tablesDir }, Profile = Fixtures.Profile() };

    public static void Test_NoTablesDir_Silent()
    {
        var scanner = new ScriptDoctorScanner(_ => new[] { "/x/Tables/core.vbs" });
        var findings = scanner.Scan(Ctx(tablesDir: null)).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_LocalCopyFound_Notes()
    {
        var scanner = new ScriptDoctorScanner(dir => new[] { "/x/Tables/core.vbs" });
        var findings = scanner.Scan(Ctx()).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "SHARED_SCRIPT_LOCAL_COPY"));
        var f = findings.Single(f => f.Code == "SHARED_SCRIPT_LOCAL_COPY");
        Assert.Equal(Severity.Note, f.Severity);
        Assert.Equal("core.vbs", f.Subject);
    }

    public static void Test_AllFourPresent_FourFindings()
    {
        var scanner = new ScriptDoctorScanner(dir => new[]
        {
            "/x/Tables/core.vbs", "/x/Tables/controller.vbs",
            "/x/Tables/VPMKeys.vbs", "/x/Tables/nudge.vbs",
        });
        var findings = scanner.Scan(Ctx()).ToList();
        Assert.Equal(4, findings.Count(f => f.Code == "SHARED_SCRIPT_LOCAL_COPY"));
    }

    public static void Test_UnrelatedVbsFile_Silent()
    {
        var scanner = new ScriptDoctorScanner(dir => new[] { "/x/Tables/mytable_helper.vbs" });
        var findings = scanner.Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_NoVbsFiles_Silent()
    {
        var scanner = new ScriptDoctorScanner(dir => Array.Empty<string>());
        var findings = scanner.Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_EnumerationThrows_SilentNeverThrows()
    {
        var scanner = new ScriptDoctorScanner(dir => throw new UnauthorizedAccessException("locked"));
        var findings = scanner.Scan(Ctx()).ToList(); // must not throw
        Assert.Equal(0, findings.Count);
    }
}
