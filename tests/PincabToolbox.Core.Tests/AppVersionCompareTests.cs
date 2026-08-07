using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>
/// Pure version comparison for the manual "check for updates" button — no network involved here,
/// just the logic that decides whether a GitHub release tag counts as newer than the running app.
/// </summary>
public static class AppVersionCompareTests
{
    public static void Test_NewerPatch_IsNewer()
    {
        Assert.True(AppVersionCompare.IsNewer("v0.1.2", "0.1.1"));
    }

    public static void Test_SameVersion_IsNotNewer()
    {
        Assert.False(AppVersionCompare.IsNewer("v0.1.1", "0.1.1"));
    }

    public static void Test_OlderTag_IsNotNewer()
    {
        Assert.False(AppVersionCompare.IsNewer("v0.1.0", "0.1.1"));
    }

    public static void Test_LeadingV_IsStripped()
    {
        Assert.True(AppVersionCompare.IsNewer("V0.2.0", "0.1.1"));
    }

    public static void Test_NoLeadingV_StillWorks()
    {
        Assert.True(AppVersionCompare.IsNewer("0.2.0", "0.1.1"));
    }

    public static void Test_PreReleaseSuffix_CoreVersionStillCompared()
    {
        // "v0.2.0-alpha" — a suffix must never crash the comparison; the numeric core still counts.
        Assert.True(AppVersionCompare.IsNewer("v0.2.0-alpha", "0.1.1"));
    }

    public static void Test_MalformedLatestTag_TreatedAsNotNewer()
    {
        // A tag that doesn't parse must never produce a false "update available" — silence, not a
        // guess, same doctrine the scanners already follow for unreadable data.
        Assert.False(AppVersionCompare.IsNewer("not-a-version", "0.1.1"));
    }

    public static void Test_MalformedCurrentVersion_TreatedAsNotNewer()
    {
        Assert.False(AppVersionCompare.IsNewer("v0.2.0", "??"));
    }

    public static void Test_BareMajorVersion_IsPadded()
    {
        Assert.True(AppVersionCompare.IsNewer("v1", "0.1.1"));
    }

    public static void Test_MajorVersionBump_IsNewer()
    {
        Assert.True(AppVersionCompare.IsNewer("v1.0.0", "0.9.9"));
    }
}
