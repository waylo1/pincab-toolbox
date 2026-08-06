using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;
using PincabToolbox.Core.Vpx;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure version-arithmetic decisions for the VPX comparator.</summary>
public static class VpxVersionComparerTests
{
    public static void Test_ParseMajorMinor_HandlesCommonForms()
    {
        int mj, mn;
        Assert.True(VpxVersionComparer.TryParseMajorMinor("10.8", out mj, out mn) && mj == 10 && mn == 8);
        Assert.True(VpxVersionComparer.TryParseMajorMinor("10.7.0", out mj, out mn) && mj == 10 && mn == 7);
        Assert.True(VpxVersionComparer.TryParseMajorMinor("10.8.0.1234", out mj, out mn) && mj == 10 && mn == 8);
        Assert.True(VpxVersionComparer.TryParseMajorMinor("v10.6", out mj, out mn) && mj == 10 && mn == 6);
        Assert.True(VpxVersionComparer.TryParseMajorMinor("Visual Pinball 10.8 (rev abc)", out mj, out mn) && mj == 10 && mn == 8);
    }

    public static void Test_ParseMajorMinor_RejectsAmbiguousOrEmpty()
    {
        Assert.False(VpxVersionComparer.TryParseMajorMinor("10", out _, out _), "no minor is undetectable, not 10.0");
        Assert.False(VpxVersionComparer.TryParseMajorMinor("", out _, out _));
        Assert.False(VpxVersionComparer.TryParseMajorMinor(null, out _, out _));
        Assert.False(VpxVersionComparer.TryParseMajorMinor("no version here", out _, out _));
    }

    public static void Test_IsOutdated_StrictlyBelowOnly()
    {
        Assert.True(VpxVersionComparer.IsOutdated(10, 7, 10, 8), "10.7 < 10.8");
        Assert.False(VpxVersionComparer.IsOutdated(10, 8, 10, 8), "equal is not outdated");
        Assert.False(VpxVersionComparer.IsOutdated(10, 9, 10, 8), "newer is not outdated");
        Assert.True(VpxVersionComparer.IsOutdated(9, 9, 10, 0), "a whole major behind is outdated");
        Assert.False(VpxVersionComparer.IsOutdated(10, 20, 10, 8), "much newer minor is not outdated");
    }

    public static void Test_HighestInstalled_PicksNewestParseable()
    {
        int mj, mn;
        Assert.True(VpxVersionComparer.TryHighestInstalled(new[] { "10.6", "10.8", "10.7" }, out mj, out mn) && mj == 10 && mn == 8);
        Assert.True(VpxVersionComparer.TryHighestInstalled(new string?[] { null, "junk", "10.7.0" }, out mj, out mn) && mj == 10 && mn == 7);
        Assert.False(VpxVersionComparer.TryHighestInstalled(new string?[] { null, "junk", "10" }, out _, out _), "nothing parseable → false");
        Assert.False(VpxVersionComparer.TryHighestInstalled(Array.Empty<string?>(), out _, out _), "no candidates → false");
    }
}

/// <summary>End-to-end scanner behaviour, with the installed-version read injected.</summary>
public static class VpxVersionScannerTests
{
    private static ScanContext CtxWithExe(params (string path, string script)[] tables)
    {
        var layout = new InstallLayout { RootPath = "/x" };
        layout.VpxExecutables.Add("/fake/VPinballX64.exe");
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        foreach (var (path, script) in tables)
            ctx.Tables[path] = new VpxTableData { FilePath = path, Script = script };
        return ctx;
    }

    private static VpxVersionScanner Reading(string? version) => new(_ => version);

    public static void Test_InstalledBelowRequired_Warns()
    {
        var ctx = CtxWithExe(("Foo (2024).vpx", "' This table requires VPX 10.8 to run\nSub X()\nEnd Sub"));
        var findings = Reading("10.7").Scan(ctx).ToList();
        Assert.True(findings.Any(f => f.Code == "VPX_VERSION_OUTDATED" && f.Severity == Severity.Warning),
            "installed 10.7 < required 10.8 → Warning");
        Assert.True(findings.Any(f => f.Args.Contains("10.8") && f.Args.Contains("10.7")),
            "finding carries both the required and the installed version");
    }

    public static void Test_InstalledMeetsRequired_Silent()
    {
        var ctx = CtxWithExe(("Foo (2024).vpx", "' requires VPX 10.8\nSub X()\nEnd Sub"));
        var findings = Reading("10.8").Scan(ctx).ToList();
        Assert.False(findings.Any(f => f.Code == "VPX_VERSION_OUTDATED"),
            "installed 10.8 >= required 10.8 → silent");
    }

    public static void Test_InstalledNewer_MatchesJuly30Shape_Silent()
    {
        // The exact false positive of 2026-07-30: a table declaring an OLD floor (10.5) on a modern install.
        var ctx = CtxWithExe(("AceOfSpeed (2019).vpx", "' requires VPX 10.5+\nSub X()\nEnd Sub"));
        var findings = Reading("10.8").Scan(ctx).ToList();
        Assert.False(findings.Any(f => f.Code == "VPX_VERSION_OUTDATED"),
            "have 10.8, table asks 10.5+ → must stay silent (the July-30 regression must never return)");
    }

    public static void Test_InstalledUndetectable_NeverFalsePositive()
    {
        var ctx = CtxWithExe(("Foo.vpx", "' requires VPX 10.8\nSub X()\nEnd Sub"));
        var findings = Reading(null).Scan(ctx).ToList();
        Assert.False(findings.Any(f => f.Code == "VPX_VERSION_OUTDATED"),
            "installed version unreadable → silent, never a false positive");
    }

    public static void Test_NoVpxExecutables_Silent()
    {
        var layout = new InstallLayout { RootPath = "/x" }; // no executables registered
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = "' requires VPX 10.8\n" };
        var findings = new VpxVersionScanner(_ => "10.9").Scan(ctx).ToList();
        Assert.False(findings.Any(f => f.Code == "VPX_VERSION_OUTDATED"),
            "no VPX executable known → nothing to compare → silent");
    }

    public static void Test_MultipleExes_HighestInstalledWins()
    {
        var layout = new InstallLayout { RootPath = "/x" };
        layout.VpxExecutables.Add("/fake/VPinballX.exe");   // reads 10.6 (older 32-bit left behind)
        layout.VpxExecutables.Add("/fake/VPinballX64.exe"); // reads 10.8 (current)
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = "' requires VPX 10.8\n" };
        var reader = new Func<string, string?>(p => p.Contains("X64") ? "10.8" : "10.6");
        var findings = new VpxVersionScanner(reader).Scan(ctx).ToList();
        Assert.False(findings.Any(f => f.Code == "VPX_VERSION_OUTDATED"),
            "one installed VPX (10.8) satisfies the requirement → silent despite an older 10.6 also present");
    }

    public static void Test_NoDeclaredRequirement_Silent()
    {
        var ctx = CtxWithExe(("Foo.vpx", "Sub X()\nEnd Sub\n' an ordinary table, no version declared"));
        var findings = Reading("10.5").Scan(ctx).ToList();
        Assert.False(findings.Any(f => f.Code == "VPX_VERSION_OUTDATED"),
            "table declares no required version → nothing to compare → silent");
    }

    public static void Test_MultipleOutdatedTables_AllReported()
    {
        var ctx = CtxWithExe(
            ("A.vpx", "' requires VPX 10.8\n"),
            ("B.vpx", "' requires VPX 10.7\n"));
        var findings = Reading("10.6").Scan(ctx).ToList();
        Assert.Equal(2, findings.Count(f => f.Code == "VPX_VERSION_OUTDATED"));
    }
}

/// <summary>The new advisory `Note` severity: distinct from Info, still score-neutral.</summary>
public static class SeverityNoteTests
{
    public static void Test_Note_SortsBetweenInfoAndWarning()
    {
        Assert.True(Severity.Info < Severity.Note, "Note is above Info");
        Assert.True(Severity.Note < Severity.Warning, "Note is below Warning");
    }

    public static void Test_Note_NeverMovesScore()
    {
        var report = new ScanReport { Layout = new InstallLayout { RootPath = "/x" } };
        for (int i = 0; i < 10; i++)
            report.Findings.Add(new Finding { Code = "ADV", Severity = Severity.Note, Category = "c", Subject = $"t{i}", EnglishText = "advisory" });
        Assert.Equal(100, report.Score); // ten notes, still a perfect score
        Assert.Equal("A+", report.Grade);
    }

    public static void Test_Note_BelowWarningThreshold()
    {
        // Anything gated on ">= Warning" (watch banner / FIX THIS FIRST) must exclude Note.
        Assert.False(Severity.Note >= Severity.Warning, "Note must stay out of the actionable tier");
    }

    public static void Test_Note_CollapsesInRollup_LikeNonCritical()
    {
        var report = new ScanReport { Layout = new InstallLayout { RootPath = "/x" } };
        for (int i = 0; i < 8; i++)
            report.Findings.Add(new Finding { Code = "ADV", Severity = Severity.Note, Category = "c", Subject = $"t{i}", EnglishText = "advisory" });
        var rolled = report.Rolled().ToList();
        Assert.True(rolled.All(f => f.Code != "ADV"), "individual notes collapse");
        Assert.True(rolled.Any(f => f.Code == "GROUPED"), "into a single grouped row");
    }
}
