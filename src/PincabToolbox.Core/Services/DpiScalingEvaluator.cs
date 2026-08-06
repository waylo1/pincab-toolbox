namespace PincabToolbox.Core.Services;

/// <summary>Pure decision: is an AppliedDPI reading non-standard, and what percentage is it?</summary>
public static class DpiScalingEvaluator
{
    private const uint BaselineDpi = 96; // 100% scaling

    /// <summary>True when <paramref name="appliedDpi"/> is a real reading that isn't the 100% baseline.</summary>
    public static bool IsNonStandard(uint? appliedDpi) => appliedDpi is > 0 && appliedDpi != BaselineDpi;

    /// <summary>Rounded scaling percentage for a DPI reading (96 -&gt; 100).</summary>
    public static int Percent(uint appliedDpi) => (int)Math.Round(appliedDpi * 100.0 / BaselineDpi);
}
