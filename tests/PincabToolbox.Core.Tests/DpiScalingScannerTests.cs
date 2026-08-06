using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure DPI-reading decision.</summary>
public static class DpiScalingEvaluatorTests
{
    public static void Test_Baseline96_IsStandard()
    {
        Assert.False(DpiScalingEvaluator.IsNonStandard(96));
    }

    public static void Test_Null_IsStandard()
    {
        Assert.False(DpiScalingEvaluator.IsNonStandard(null));
    }

    public static void Test_Zero_IsStandard()
    {
        Assert.False(DpiScalingEvaluator.IsNonStandard(0));
    }

    public static void Test_120_IsNonStandard_125Percent()
    {
        Assert.True(DpiScalingEvaluator.IsNonStandard(120));
        Assert.Equal(125, DpiScalingEvaluator.Percent(120));
    }

    public static void Test_144_Is150Percent()
    {
        Assert.Equal(150, DpiScalingEvaluator.Percent(144));
    }

    public static void Test_192_Is200Percent()
    {
        Assert.Equal(200, DpiScalingEvaluator.Percent(192));
    }
}

/// <summary>End-to-end scanner behaviour, with the registry read injected.</summary>
public static class DpiScalingScannerTests
{
    private static ScanContext Ctx()
    {
        var layout = new InstallLayout { RootPath = "/x" };
        return new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
    }

    public static void Test_NonStandardDpi_Notes()
    {
        var findings = new DpiScalingScanner(() => 120u).Scan(Ctx()).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "DPI_SCALING_NONSTANDARD"));
        var f = findings.Single();
        Assert.Equal(Severity.Note, f.Severity);
        Assert.Equal("125%", f.Subject);
    }

    public static void Test_StandardDpi_Silent()
    {
        var findings = new DpiScalingScanner(() => 96u).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_UnknownDpi_Silent()
    {
        var findings = new DpiScalingScanner(() => null).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_ReadThrows_Silent()
    {
        var findings = new DpiScalingScanner(() => throw new InvalidOperationException()).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }
}
