using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure screen-output-name decision.</summary>
public static class AudioStateEvaluatorTests
{
    public static void Test_HdmiName_LooksLikeScreenOutput()
    {
        Assert.True(AudioStateEvaluator.LooksLikeScreenOutput("NVIDIA High Definition Audio"));
        Assert.True(AudioStateEvaluator.LooksLikeScreenOutput("Realtek HDMI Output"));
        Assert.True(AudioStateEvaluator.LooksLikeScreenOutput("Intel(R) Display Audio"));
    }

    public static void Test_SpeakerName_DoesNotLookLikeScreenOutput()
    {
        Assert.False(AudioStateEvaluator.LooksLikeScreenOutput("Speakers (Realtek High Definition Audio)"));
        Assert.False(AudioStateEvaluator.LooksLikeScreenOutput("Logitech Z623"));
    }

    public static void Test_NullOrEmpty_DoesNotLookLikeScreenOutput()
    {
        Assert.False(AudioStateEvaluator.LooksLikeScreenOutput(null));
        Assert.False(AudioStateEvaluator.LooksLikeScreenOutput(""));
        Assert.False(AudioStateEvaluator.LooksLikeScreenOutput("   "));
    }
}

/// <summary>End-to-end scanner behaviour, with the device-name read injected.</summary>
public static class AudioStateScannerTests
{
    private static ScanContext Ctx()
    {
        var layout = new InstallLayout { RootPath = "/x" };
        return new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
    }

    public static void Test_ScreenOutputDefault_Notes()
    {
        var findings = new AudioStateScanner(() => "NVIDIA High Definition Audio").Scan(Ctx()).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "AUDIO_DEFAULT_SUSPECT"));
        var f = findings.Single();
        Assert.Equal(Severity.Note, f.Severity);
        Assert.Equal("NVIDIA High Definition Audio", f.Subject);
    }

    public static void Test_SpeakerDefault_Silent()
    {
        var findings = new AudioStateScanner(() => "Speakers (Realtek)").Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_UnknownDefault_Silent()
    {
        var findings = new AudioStateScanner(() => null).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_ReadThrows_Silent()
    {
        var findings = new AudioStateScanner(() => throw new InvalidOperationException()).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }
}
