using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>
/// A scan report is a PUBLIC document — the product asks people to paste it on a forum.
/// These tests are the guard against leaking the account name with it (ADR-003).
/// </summary>
public static class PathScrubberTests
{
    public static void Test_Scrub_WindowsUserFolder()
        => Assert.Equal(@"C:\Users\<user>\Desktop\vpx\a.dll",
            PathScrubber.Scrub(@"C:\Users\Maxime\Desktop\vpx\a.dll"));

    public static void Test_Scrub_UnixHome()
        => Assert.Equal("/home/<user>/vpx/a.dll", PathScrubber.Scrub("/home/maxime/vpx/a.dll"));

    public static void Test_Scrub_LegacyDocumentsAndSettings()
        => Assert.Equal(@"C:\Documents and Settings\<user>\vpx",
            PathScrubber.Scrub(@"C:\Documents and Settings\Maxime\vpx"));

    public static void Test_Scrub_LeavesPathsWithoutAUserFolderAlone()
        => Assert.Equal(@"C:\vpx\Tables\afm.vpx", PathScrubber.Scrub(@"C:\vpx\Tables\afm.vpx"));

    /// <summary>A whole report, not just one path — several occurrences, several lines.</summary>
    public static void Test_Scrub_HandlesEveryOccurrenceInAReport()
    {
        var report = string.Join("\n",
            @"Root: C:\Users\Maxime\Desktop\Pincab",
            @"[CRITICAL] C:\Users\Maxime\Desktop\Pincab\VPinMAME\VPinMAME.dll",
            @"[WARNING]  C:\Users\Maxime\AppData\Local\PinUP\PUPDatabase.db");

        var clean = PathScrubber.Scrub(report);
        Assert.False(clean.Contains("Maxime"), "no occurrence of the account name may survive");
        Assert.Equal(3, clean.Split("<user>").Length - 1);
    }

    public static void Test_Scrub_IsIdempotent()
    {
        var once = PathScrubber.Scrub(@"C:\Users\Maxime\vpx");
        Assert.Equal(once, PathScrubber.Scrub(once));
    }

    /// <summary>The account name can appear outside a home folder — a folder named after its owner.</summary>
    public static void Test_Scrub_AlsoRemovesTheNameOutsideAHomeFolder()
        => Assert.Equal(@"D:\Pincab-<user>\Tables",
            PathScrubber.Scrub(@"D:\Pincab-Maxime\Tables", userName: "Maxime"));

    public static void Test_Scrub_IsCaseInsensitiveOnTheUserName()
        => Assert.Equal(@"D:\<user>\vpx", PathScrubber.Scrub(@"D:\MAXIME\vpx", userName: "maxime"));

    /// <summary>Guard against mangling ordinary words when the account name is very short.</summary>
    public static void Test_Scrub_IgnoresVeryShortUserNames()
        => Assert.Equal(@"D:\Games\Pinball", PathScrubber.Scrub(@"D:\Games\Pinball", userName: "am"));

    public static void Test_Scrub_HandlesNullAndEmpty()
    {
        Assert.Equal("", PathScrubber.Scrub(null));
        Assert.Equal("", PathScrubber.Scrub(""));
    }

    public static void Test_Scrub_HandlesAPathEndingRightAfterTheUserFolder()
        => Assert.Equal(@"C:\Users\<user>", PathScrubber.Scrub(@"C:\Users\Maxime"));

    public static void Test_Scrub_HandlesQuotedPaths()
        => Assert.Equal("\"C:\\Users\\<user>\\vpx\"", PathScrubber.Scrub("\"C:\\Users\\Maxime\\vpx\""));

    /// <summary>The helper the UI uses as a release guard before putting anything on the clipboard.</summary>
    public static void Test_LeaksIdentity_DetectsAnUnscrubbedReport()
    {
        Assert.True(PathScrubber.LeaksIdentity(@"Root: C:\Users\Maxime\vpx"), "must flag a raw path");
        Assert.False(PathScrubber.LeaksIdentity(@"Root: C:\Users\<user>\vpx"), "must accept a clean one");
        Assert.False(PathScrubber.LeaksIdentity(@"Root: C:\vpx"), "must accept a path with no home folder");
    }

    /// <summary>
    /// The exact shape the app puts on the clipboard. Guards the whole chain, not just one path.
    /// </summary>
    public static void Test_Scrub_ARealForumReportCarriesNoIdentity()
    {
        var report = string.Join("\n",
            "**Pincab Toolbox — scan report** · 2026-07-27 21:14",
            "",
            "**Health score: 55/100 (C)** — 3 critical · 0 warnings · 1 info",
            "",
            "### Critical (3)",
            @"- **VPinMAME.dll** — blocked by Windows (C:\Users\Maxime\Desktop\Pincab\VPinMAME\VPinMAME.dll)",
            @"  - fix: unblock it in C:\Users\Maxime\Desktop\Pincab",
            @"- **mm_109c** — ROM extracted into D:\Pincab-Maxime\roms\mm_109c",
            "",
            "_Scanned with Pincab Toolbox_");

        var clean = PathScrubber.Scrub(report, userName: "Maxime");

        Assert.False(clean.Contains("Maxime"), "the account name must not survive anywhere");
        Assert.False(PathScrubber.LeaksIdentity(clean, "Maxime"), "and the guard must agree");
        Assert.Contains("Health score: 55/100 (C)", clean);
        Assert.Contains("VPinMAME.dll", clean);
        Assert.Contains("mm_109c", clean);
    }
}
