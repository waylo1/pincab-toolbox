using PincabToolbox.App.Localization;

namespace PincabToolbox.App.Tests;

/// <summary>
/// Real, executed tests for Scenarios.DetectAll/Detect — point 3 of the 13/08 CTO+Produit review.
/// Every test that reads localized text pins Loc.Lang explicitly (via WithLang) rather than relying
/// on whatever the OS culture happens to be: Loc.Lang is process-wide static state, and this test
/// runner executes every Test_ method in one process, so a test that left it in "fr" would silently
/// break every English-reading test that runs after it alphabetically.
/// </summary>
public static class ScenariosTests
{
    private static void WithLang(string lang, Action body)
    {
        var original = Loc.Lang;
        try
        {
            Loc.SetLang(lang);
            body();
        }
        finally
        {
            Loc.SetLang(original);
        }
    }

    private static ISet<string> Codes(params string[] codes) => new HashSet<string>(codes);

    public static void Test_Empty_Set_Yields_No_Matches()
    {
        var matches = Scenarios.DetectAll(Codes());
        Assert.Equal(0, matches.Count);
    }

    public static void Test_Unrelated_Codes_Yield_No_Matches()
    {
        var matches = Scenarios.DetectAll(Codes("SOME_UNRELATED_CODE", "ANOTHER_ONE"));
        Assert.Equal(0, matches.Count);
    }

    public static void Test_VpinmameNotRegistered_Alone_Is_Enough_MinMatch_1()
    {
        var matches = Scenarios.DetectAll(Codes("VPINMAME_NOT_REGISTERED"));
        Assert.Equal(1, matches.Count);
        Assert.Equal(3, matches[0].Chain.Count, "all 3 chain steps require VPINMAME_NOT_REGISTERED, all should show");
    }

    public static void Test_BitnessMigration_Requires_At_Least_Two_Of_Its_Three_Codes()
    {
        // Only one of the three BITNESS_* codes present — below MinMatch = 2, must not fire.
        var matches = Scenarios.DetectAll(Codes("BITNESS_MISMATCH_VPM"));
        Assert.False(matches.Any(m => m.TriggeredBy.Contains("BITNESS_MISMATCH_VPM")),
            "a single BITNESS_* code must not be enough to trigger the migration scenario (MinMatch=2)");
    }

    public static void Test_BitnessMigration_Fires_With_Exactly_MinMatch_Codes()
    {
        var matches = Scenarios.DetectAll(Codes("BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING"));
        Assert.Equal(1, matches.Count);
        Assert.Equal(2, matches[0].TriggeredBy.Count);
    }

    public static void Test_FrontendIntegration_Requires_Both_Of_Its_Two_Codes()
    {
        var oneCode = Scenarios.DetectAll(Codes("POPPER_NOT_REGISTERED"));
        Assert.Equal(0, oneCode.Count);

        var bothCodes = Scenarios.DetectAll(Codes("POPPER_NOT_REGISTERED", "B2S_MISSING"));
        Assert.Equal(1, bothCodes.Count);
    }

    public static void Test_Chain_Only_Shows_Steps_Whose_Backing_Code_Actually_Matched()
    {
        // BITNESS_DMD64_MISSING not present — its chain step ("dmddevice64.dll") must not show,
        // even though the scenario as a whole fires on the other two codes.
        var matches = Scenarios.DetectAll(Codes("BITNESS_MISMATCH_VPM", "BITNESS_HYBRID_INSTALL"));
        Assert.Equal(1, matches.Count);
        var chain = matches[0].Chain;
        Assert.False(chain.Any(c => c.Label.Contains("dmddevice64", StringComparison.OrdinalIgnoreCase)),
            "a chain step must not appear when its RequiresCode did not match");
        Assert.True(chain.Count > 0, "the two matched codes' steps should still show");
    }

    public static void Test_Confidence_Grows_With_Number_Of_Matched_Codes()
    {
        var two = Scenarios.DetectAll(Codes("BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING"))[0].Confidence;
        var three = Scenarios.DetectAll(Codes("BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING", "BITNESS_HYBRID_INSTALL"))[0].Confidence;
        Assert.True(three > two, $"3 matched codes ({three}) should score higher confidence than 2 ({two})");
    }

    public static void Test_Confidence_Is_Capped_At_96()
    {
        // BaseConfidence 74 + 3 codes * PerCode 8 = 98, must clamp to 96.
        var matches = Scenarios.DetectAll(Codes("BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING", "BITNESS_HYBRID_INSTALL"));
        Assert.Equal(96, matches[0].Confidence);
    }

    public static void Test_Codes_Outside_Any_Scenario_Do_Not_Inflate_Confidence()
    {
        var withoutNoise = Scenarios.DetectAll(Codes("VPINMAME_NOT_REGISTERED"))[0].Confidence;
        var withNoise = Scenarios.DetectAll(Codes("VPINMAME_NOT_REGISTERED", "SOME_UNRELATED_CODE"))[0].Confidence;
        Assert.Equal(withoutNoise, withNoise, "an unrelated code present in the scan must not change a scenario's confidence");
    }

    public static void Test_Multiple_Scenarios_Fire_Together_When_All_Their_Codes_Co_Occur()
    {
        var matches = Scenarios.DetectAll(Codes(
            "BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING",
            "POPPER_NOT_REGISTERED", "B2S_MISSING",
            "VPINMAME_NOT_REGISTERED"));
        Assert.Equal(3, matches.Count);
    }

    public static void Test_Matches_Are_Ordered_By_Confidence_Descending()
    {
        var matches = Scenarios.DetectAll(Codes(
            "BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING",
            "POPPER_NOT_REGISTERED", "B2S_MISSING",
            "VPINMAME_NOT_REGISTERED"));
        for (var i = 1; i < matches.Count; i++)
            Assert.True(matches[i - 1].Confidence >= matches[i].Confidence,
                $"expected non-increasing confidence, got {matches[i - 1].Confidence} then {matches[i].Confidence}");
    }

    public static void Test_Detect_Returns_Null_When_Nothing_Matches()
    {
        Assert.Equal(null, Scenarios.Detect(Codes()));
    }

    public static void Test_Detect_Returns_The_Highest_Confidence_Match()
    {
        var all = Scenarios.DetectAll(Codes(
            "BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING",
            "VPINMAME_NOT_REGISTERED"));
        var single = Scenarios.Detect(Codes(
            "BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING",
            "VPINMAME_NOT_REGISTERED"));
        Assert.NotNull(single);
        Assert.Equal(all[0].Title, single!.Title);
        Assert.Equal(all[0].Confidence, single.Confidence);
    }

    public static void Test_French_Text_Used_When_Lang_Is_Fr() => WithLang("fr", () =>
    {
        var m = Scenarios.DetectAll(Codes("VPINMAME_NOT_REGISTERED"))[0];
        Assert.Equal("Enregistrement VPinMAME manquant", m.Title);
        Assert.Contains("Windows ne le connaît pas", m.Player);
    });

    public static void Test_English_Text_Used_When_Lang_Is_En() => WithLang("en", () =>
    {
        var m = Scenarios.DetectAll(Codes("VPINMAME_NOT_REGISTERED"))[0];
        Assert.Equal("VPinMAME registration missing", m.Title);
        Assert.Contains("Windows doesn't know it", m.Player);
    });

    public static void Test_Chain_Step_Text_Also_Follows_Lang() => WithLang("fr", () =>
    {
        var m = Scenarios.DetectAll(Codes("VPINMAME_NOT_REGISTERED"))[0];
        Assert.True(m.Chain.Any(c => c.Status == "✕ non enregistré"),
            "chain step status text should be the French variant when Lang is fr");
    });

    public static void Test_TriggeredBy_Lists_Exactly_The_Codes_That_Matched_Not_All_Scenario_Codes()
    {
        // Frontend scenario needs both codes to fire at all (MinMatch=2), but confirms TriggeredBy
        // reports the actual matched set rather than the scenario's full code list from Codes[].
        var matches = Scenarios.DetectAll(Codes("POPPER_NOT_REGISTERED", "B2S_MISSING", "UNRELATED"));
        var m = matches.Single(x => x.Title == "Incomplete frontend integration");
        Assert.Equal(2, m.TriggeredBy.Count);
        Assert.True(m.TriggeredBy.Contains("POPPER_NOT_REGISTERED"));
        Assert.True(m.TriggeredBy.Contains("B2S_MISSING"));
        Assert.False(m.TriggeredBy.Contains("UNRELATED"));
    }

    // ---- Point 4/6 (13/08): 3 new scenarios, all single-code-or-twin-code MinMatch=1 (see the
    // rationale comments in Scenarios.cs itself for why each one is safe to fire on one code alone). ----

    public static void Test_Vpm32Mismatch_Alone_Is_Enough_MinMatch_1()
    {
        var matches = Scenarios.DetectAll(Codes("BITNESS_MISMATCH_VPM32"));
        Assert.Equal(1, matches.Count);
        Assert.Equal(3, matches[0].Chain.Count, "all 3 chain steps require BITNESS_MISMATCH_VPM32, all should show");
    }

    public static void Test_Vpm32Mismatch_Does_Not_Fire_The_Other_Direction_Bitness_Scenario()
    {
        // BITNESS_MISMATCH_VPM32 must not accidentally satisfy scenario 1's MinMatch=2 on
        // BITNESS_MISMATCH_VPM/BITNESS_DMD64_MISSING/BITNESS_HYBRID_INSTALL — the two Defs share no code.
        var matches = Scenarios.DetectAll(Codes("BITNESS_MISMATCH_VPM32"));
        Assert.False(matches.Any(m => m.Title == "Incomplete 32→64 migration"));
    }

    public static void Test_Vpm32Mismatch_Confidence_Is_BaseConfidence_Plus_One_PerCode()
    {
        var matches = Scenarios.DetectAll(Codes("BITNESS_MISMATCH_VPM32"));
        Assert.Equal(88, matches[0].Confidence, "BaseConfidence 80 + 1 matched code * PerCode 8");
    }

    public static void Test_ComStalePath_Alone_Is_Enough_MinMatch_1()
    {
        var matches = Scenarios.DetectAll(Codes("COM_STALE_PATH"));
        Assert.Equal(1, matches.Count);
        Assert.Equal(3, matches[0].Chain.Count, "all 3 chain steps require COM_STALE_PATH, all should show");
    }

    public static void Test_ComStalePath_Confidence_Is_BaseConfidence_Plus_One_PerCode()
    {
        var matches = Scenarios.DetectAll(Codes("COM_STALE_PATH"));
        Assert.Equal(76, matches[0].Confidence, "BaseConfidence 68 + 1 matched code * PerCode 8");
    }

    public static void Test_AltExtrasNotEnabled_Fires_On_AltSound_Alone_And_Only_Shows_Its_Own_Chain_Pair()
    {
        var matches = Scenarios.DetectAll(Codes("ALTSOUND_PRESENT_NOT_ENABLED"));
        Assert.Equal(1, matches.Count);
        Assert.Equal(2, matches[0].Chain.Count, "only the AltSound pair should show, not AltColor's");
        Assert.False(matches[0].Chain.Any(c => c.Label.Contains("AltColor", StringComparison.OrdinalIgnoreCase)));
    }

    public static void Test_AltExtrasNotEnabled_Fires_On_AltColor_Alone_And_Only_Shows_Its_Own_Chain_Pair()
    {
        var matches = Scenarios.DetectAll(Codes("ALTCOLOR_PRESENT_NOT_ENABLED"));
        Assert.Equal(1, matches.Count);
        Assert.Equal(2, matches[0].Chain.Count, "only the AltColor pair should show, not AltSound's");
        Assert.False(matches[0].Chain.Any(c => c.Label.Contains("AltSound", StringComparison.OrdinalIgnoreCase)));
    }

    public static void Test_AltExtrasNotEnabled_Both_Codes_Show_All_Four_Chain_Steps()
    {
        var matches = Scenarios.DetectAll(Codes("ALTSOUND_PRESENT_NOT_ENABLED", "ALTCOLOR_PRESENT_NOT_ENABLED"));
        Assert.Equal(1, matches.Count);
        Assert.Equal(4, matches[0].Chain.Count);
    }

    public static void Test_AltExtrasNotEnabled_Confidence_Grows_From_One_Code_To_Two()
    {
        var one = Scenarios.DetectAll(Codes("ALTSOUND_PRESENT_NOT_ENABLED"))[0].Confidence;
        var two = Scenarios.DetectAll(Codes("ALTSOUND_PRESENT_NOT_ENABLED", "ALTCOLOR_PRESENT_NOT_ENABLED"))[0].Confidence;
        Assert.Equal(86, one, "BaseConfidence 78 + 1 matched code * PerCode 8");
        Assert.Equal(94, two, "BaseConfidence 78 + 2 matched codes * PerCode 8");
    }

    public static void Test_Point4_Scenarios_Have_French_Text_Too() => WithLang("fr", () =>
    {
        var vpm32 = Scenarios.DetectAll(Codes("BITNESS_MISMATCH_VPM32"))[0];
        Assert.Equal("Installation 32-bit avec VPinMAME 64-bit uniquement", vpm32.Title);

        var stale = Scenarios.DetectAll(Codes("COM_STALE_PATH"))[0];
        Assert.Equal("Composant enregistré vers un emplacement supprimé", stale.Title);

        var extras = Scenarios.DetectAll(Codes("ALTSOUND_PRESENT_NOT_ENABLED"))[0];
        Assert.Equal("Pack son/couleur installé mais désactivé", extras.Title);
    });

    public static void Test_Point4_Scenarios_Can_All_Fire_Alongside_The_Original_Three()
    {
        var matches = Scenarios.DetectAll(Codes(
            "BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING",
            "POPPER_NOT_REGISTERED", "B2S_MISSING",
            "VPINMAME_NOT_REGISTERED",
            "BITNESS_MISMATCH_VPM32", "COM_STALE_PATH", "ALTSOUND_PRESENT_NOT_ENABLED"));
        Assert.Equal(6, matches.Count);
    }
}
