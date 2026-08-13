using PincabToolbox.Core.Models;

namespace PincabToolbox.App.Tests;

public static class ChainRowPlannerTests
{
    private static ChainStepMatch Step(string label, string status, ChainTone tone) =>
        new() { Label = label, Status = status, Tone = tone };

    public static void Test_Empty_Chain_Yields_Empty_Plan()
    {
        Assert.Equal(0, ChainRowPlanner.Plan(Array.Empty<ChainStepMatch>()).Count);
    }

    public static void Test_First_Step_Never_Has_An_Arrow()
    {
        var plan = ChainRowPlanner.Plan(new[] { Step("A", "ok", ChainTone.Good) });
        Assert.Equal("", plan[0].Arrow);
        Assert.False(plan[0].IsCutPoint);
    }

    public static void Test_Good_To_Bad_Transition_Is_The_Cut_Point()
    {
        var plan = ChainRowPlanner.Plan(new[]
        {
            Step("VPX", "64-bit", ChainTone.Good),
            Step("VPinMAME.dll", "32-bit", ChainTone.Bad),
        });
        Assert.False(plan[0].IsCutPoint);
        Assert.True(plan[1].IsCutPoint);
        Assert.Equal("✕→", plan[1].Arrow);
    }

    public static void Test_Every_GoodToBad_Edge_Is_A_Cut_Point_Not_Just_The_First()
    {
        // The rule is purely local (previous step vs this one), not "have we cut once already" —
        // Good -> Bad -> Good -> Bad marks BOTH breaks. Pinned here on purpose: it would be easy to
        // "fix" this into a global once-only rule while refactoring and not notice the visual
        // change (every later break in the chain would stop reading as ✕→ in red).
        var plan = ChainRowPlanner.Plan(new[]
        {
            Step("A", "s1", ChainTone.Good),
            Step("B", "s2", ChainTone.Bad),
            Step("C", "s3", ChainTone.Good),
            Step("D", "s4", ChainTone.Bad),
        });
        Assert.True(plan[1].IsCutPoint);
        Assert.True(plan[3].IsCutPoint, "the planner marks every good->bad edge, not just the first — this pins the current, intentional behaviour");
    }

    public static void Test_Bad_To_Bad_Is_Not_A_Cut_Point()
    {
        var plan = ChainRowPlanner.Plan(new[]
        {
            Step("A", "s1", ChainTone.Bad),
            Step("B", "s2", ChainTone.Bad),
        });
        Assert.False(plan[1].IsCutPoint);
        Assert.Equal("→", plan[1].Arrow);
    }

    public static void Test_Warn_Step_Never_Counts_As_A_Cut_Point_Source_Or_Target()
    {
        var plan = ChainRowPlanner.Plan(new[]
        {
            Step("A", "s1", ChainTone.Good),
            Step("B", "s2", ChainTone.Warn),
            Step("C", "s3", ChainTone.Bad),
        });
        Assert.False(plan[1].IsCutPoint, "Good->Warn is not a cut");
        Assert.False(plan[2].IsCutPoint, "Warn->Bad is not a cut, only Good->Bad is");
    }

    public static void Test_Plan_Preserves_Label_Status_And_Tone_Unchanged()
    {
        var plan = ChainRowPlanner.Plan(new[] { Step("VPinMAME.dll", "✓ present", ChainTone.Good) });
        Assert.Equal("VPinMAME.dll", plan[0].Label);
        Assert.Equal("✓ present", plan[0].Status);
        Assert.Equal(ChainTone.Good, plan[0].Tone);
    }
}

public static class TableRowPlannerTests
{
    private static Finding F(string code, Severity sev = Severity.Warning, params string[] args) => new()
    {
        Code = code,
        Severity = sev,
        Category = "rom",
        EnglishText = code,
        Args = args,
    };

    // ---- ROM column ----

    public static void Test_PlanRom_No_Finding_Is_Unknown()
    {
        var plan = TableRowPlanner.PlanRom(Array.Empty<Finding>());
        Assert.Equal(RomColumnStatus.Unknown, plan.Status);
    }

    public static void Test_PlanRom_Ok_Carries_The_Rom_Name_From_Args()
    {
        var plan = TableRowPlanner.PlanRom(new[] { F("ROM_OK", Severity.Ok, "Attack From Mars", "afm_113b") });
        Assert.Equal(RomColumnStatus.Ok, plan.Status);
        Assert.Equal("afm_113b", plan.RomName);
    }

    public static void Test_PlanRom_Missing_Is_Critical_Status_Carries_Name()
    {
        var plan = TableRowPlanner.PlanRom(new[] { F("ROM_MISSING", Severity.Critical, "Medieval Madness", "mm_109c.zip") });
        Assert.Equal(RomColumnStatus.Missing, plan.Status);
        Assert.Equal("mm_109c.zip", plan.RomName);
    }

    public static void Test_PlanRom_NotRequired()
    {
        var plan = TableRowPlanner.PlanRom(new[] { F("ROM_NOT_REQUIRED", Severity.Ok, "Original Gem") });
        Assert.Equal(RomColumnStatus.NotRequired, plan.Status);
    }

    public static void Test_PlanRom_Unzipped()
    {
        var plan = TableRowPlanner.PlanRom(new[] { F("ROM_UNZIPPED", Severity.Warning, "X", "afm_113b") });
        Assert.Equal(RomColumnStatus.Unzipped, plan.Status);
    }

    public static void Test_PlanRom_Ignores_Findings_For_Other_Categories()
    {
        // Only ONE rom code should ever exist per table in a real report, but the planner should
        // not get confused by unrelated findings sharing the same findings list.
        var plan = TableRowPlanner.PlanRom(new[] { F("B2S_MISSING"), F("ROM_OK", Severity.Ok, "X", "afm_113b") });
        Assert.Equal(RomColumnStatus.Ok, plan.Status);
    }

    // ---- B2S column ----

    public static void Test_PlanB2s_No_Finding_Is_Present()
    {
        var plan = TableRowPlanner.PlanB2s(Array.Empty<Finding>(), completenessFailed: false);
        Assert.Equal(B2sColumnStatus.Present, plan.Status);
    }

    public static void Test_PlanB2s_Missing_Finding_Carries_Its_Real_Severity()
    {
        var plan = TableRowPlanner.PlanB2s(new[] { F("B2S_MISSING", Severity.Warning) }, completenessFailed: false);
        Assert.Equal(B2sColumnStatus.Missing, plan.Status);
        Assert.Equal(Severity.Warning, plan.Severity);
    }

    public static void Test_PlanB2s_CompletenessFailed_Overrides_Everything_To_Unknown()
    {
        // Even a real B2S_MISSING finding must not be trusted once the completeness scanner itself
        // errored — the whole column goes quiet rather than assert something unverified.
        var plan = TableRowPlanner.PlanB2s(new[] { F("B2S_MISSING", Severity.Critical) }, completenessFailed: true);
        Assert.Equal(B2sColumnStatus.Unknown, plan.Status);
    }

    // ---- Frontend column ----

    public static void Test_PlanFrontend_Null_Popper_Set_Is_Unknown()
    {
        var plan = TableRowPlanner.PlanFrontend("Any Table", null, Array.Empty<Finding>());
        Assert.Equal(FrontendColumnStatus.Unknown, plan.Status);
    }

    public static void Test_PlanFrontend_Registered_When_Name_Is_In_The_Set()
    {
        var set = new HashSet<string> { "Attack From Mars (Bally 1995)" };
        var plan = TableRowPlanner.PlanFrontend("Attack From Mars (Bally 1995)", set, Array.Empty<Finding>());
        Assert.Equal(FrontendColumnStatus.Registered, plan.Status);
    }

    public static void Test_PlanFrontend_NotRegistered_Uses_The_Real_Finding_Severity()
    {
        var set = new HashSet<string>();
        var plan = TableRowPlanner.PlanFrontend("X", set, new[] { F("POPPER_NOT_REGISTERED", Severity.Note) });
        Assert.Equal(FrontendColumnStatus.NotRegistered, plan.Status);
        Assert.Equal(Severity.Note, plan.Severity);
    }

    public static void Test_PlanFrontend_NotRegistered_Falls_Back_To_Info_Without_A_Backing_Finding()
    {
        // Not-in-the-set but no POPPER_NOT_REGISTERED finding either: still NotRegistered, but the
        // severity must default to Info rather than being left at Severity.Ok (0) by accident, or
        // invented as something alarming.
        var set = new HashSet<string>();
        var plan = TableRowPlanner.PlanFrontend("X", set, Array.Empty<Finding>());
        Assert.Equal(FrontendColumnStatus.NotRegistered, plan.Status);
        Assert.Equal(Severity.Info, plan.Severity);
    }
}
