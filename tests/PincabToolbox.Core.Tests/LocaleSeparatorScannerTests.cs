using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure separator decision.</summary>
public static class LocaleSeparatorCheckTests
{
    public static void Test_Comma_IsNonStandard()
    {
        Assert.True(LocaleSeparatorCheck.IsNonStandard(","));
    }

    public static void Test_Dot_IsStandard()
    {
        Assert.False(LocaleSeparatorCheck.IsNonStandard("."));
    }

    public static void Test_NullOrEmpty_IsStandard()
    {
        Assert.False(LocaleSeparatorCheck.IsNonStandard(null));
        Assert.False(LocaleSeparatorCheck.IsNonStandard(""));
    }
}

/// <summary>End-to-end scanner behaviour, with the culture read injected.</summary>
public static class LocaleSeparatorScannerTests
{
    private static ScanContext Ctx()
    {
        var layout = new InstallLayout { RootPath = "/x" };
        return new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
    }

    public static void Test_CommaSeparator_Notes()
    {
        var findings = new LocaleSeparatorScanner(() => ",").Scan(Ctx()).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "LOCALE_DECIMAL_SEPARATOR"));
        Assert.Equal(Severity.Note, findings.Single().Severity);
    }

    public static void Test_DotSeparator_Silent()
    {
        var findings = new LocaleSeparatorScanner(() => ".").Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_ReadThrows_Silent()
    {
        var findings = new LocaleSeparatorScanner(() => throw new InvalidOperationException()).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }
}
