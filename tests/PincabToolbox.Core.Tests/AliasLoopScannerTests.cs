using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure cycle-detection decisions over an alias map.</summary>
public static class AliasGraphTests
{
    private static Dictionary<string, string> Map(params (string, string)[] pairs)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (a, b) in pairs) d[a] = b;
        return d;
    }

    public static void Test_EmptyMap_NoCycles()
    {
        Assert.Equal(0, AliasGraph.FindCycles(Map()).Count);
    }

    public static void Test_FlatAliases_NoCycles()
    {
        // The overwhelmingly common real shape: every alias points straight at a real ROM name
        // that is never itself a key.
        var map = Map(("afm_a", "afm_113b"), ("mm_x", "mm_109c"));
        Assert.Equal(0, AliasGraph.FindCycles(map).Count);
    }

    public static void Test_SelfLoop_IsAOneNodeCycle()
    {
        var cycles = AliasGraph.FindCycles(Map(("loopy", "loopy")));
        Assert.Equal(1, cycles.Count);
        Assert.Equal(1, cycles[0].Count);
        Assert.Equal("loopy", cycles[0][0]);
    }

    public static void Test_TwoNodeCycle_Detected()
    {
        var cycles = AliasGraph.FindCycles(Map(("a", "b"), ("b", "a")));
        Assert.Equal(1, cycles.Count);
        Assert.True(cycles[0].Count == 2, "cycle should have exactly the 2 looping nodes");
        Assert.True(cycles[0].Contains("a") && cycles[0].Contains("b"));
    }

    public static void Test_AcyclicLeadIn_ExcludedFromReportedCycle()
    {
        // a -> b -> c -> b : the real loop is (b, c). "a" merely feeds into it and must not be
        // reported as part of the cycle (it is not one — it has a single, valid resolution path
        // right up until it enters the loop).
        var cycles = AliasGraph.FindCycles(Map(("a", "b"), ("b", "c"), ("c", "b")));
        Assert.Equal(1, cycles.Count);
        Assert.False(cycles[0].Contains("a"), "the acyclic lead-in must not be reported as part of the loop");
        Assert.True(cycles[0].Contains("b") && cycles[0].Contains("c"));
    }

    public static void Test_MultipleDisjointCycles_AllFound()
    {
        var cycles = AliasGraph.FindCycles(Map(("a", "b"), ("b", "a"), ("x", "y"), ("y", "x")));
        Assert.Equal(2, cycles.Count);
    }

    public static void Test_CaseInsensitive_StillDetectsTheLoop()
    {
        // VPMAlias.txt names are matched case-insensitively everywhere else in the codebase
        // (AliasFile.Parse itself uses OrdinalIgnoreCase) — the cycle check must agree.
        var cycles = AliasGraph.FindCycles(Map(("Foo", "BAR"), ("bar", "foo")));
        Assert.Equal(1, cycles.Count);
    }

    public static void Test_LongerChain_NoCycle_IsSilent()
    {
        var cycles = AliasGraph.FindCycles(Map(("a", "b"), ("b", "c"), ("c", "d")));
        Assert.Equal(0, cycles.Count);
    }
}

/// <summary>End-to-end scanner behaviour, driven entirely through ScanContext.Aliases (no I/O of its own).</summary>
public static class AliasLoopScannerTests
{
    private static ScanContext CtxWithAliases(Dictionary<string, string> aliases)
    {
        var layout = new InstallLayout { RootPath = "/x", AliasFilePath = "/x/VPinMAME/VPMAlias.txt" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile(), Aliases = aliases };
        return ctx;
    }

    public static void Test_NoAliases_Silent()
    {
        var ctx = CtxWithAliases(new Dictionary<string, string>());
        var findings = new AliasLoopScanner().Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_AcyclicAliases_Silent()
    {
        var ctx = CtxWithAliases(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["afm_a"] = "afm_113b",
        });
        var findings = new AliasLoopScanner().Scan(ctx).ToList();
        Assert.False(findings.Any(f => f.Code == "VPMALIAS_LOOP"));
    }

    public static void Test_Cycle_EmitsWarning_WithChainInArgs()
    {
        var ctx = CtxWithAliases(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = "b",
            ["b"] = "a",
        });
        var findings = new AliasLoopScanner().Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "VPMALIAS_LOOP"));
        var f = findings.Single(f => f.Code == "VPMALIAS_LOOP");
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("/x/VPinMAME/VPMAlias.txt", f.FilePath);
        Assert.True(f.Args.Count == 1 && f.Args[0].Contains("a") && f.Args[0].Contains("b"));
    }

    public static void Test_MultipleCycles_AllReported()
    {
        var ctx = CtxWithAliases(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = "b",
            ["b"] = "a",
            ["x"] = "y",
            ["y"] = "x",
        });
        var findings = new AliasLoopScanner().Scan(ctx).ToList();
        Assert.Equal(2, findings.Count(f => f.Code == "VPMALIAS_LOOP"));
    }
}
