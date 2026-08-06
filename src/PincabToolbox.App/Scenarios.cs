using System;
using System.Collections.Generic;
using System.Linq;
using PincabToolbox.App.Localization;

namespace PincabToolbox.App;

/// <summary>A detected root-cause scenario: several findings correlated into one diagnosis.</summary>
public sealed record ScenarioMatch
{
    public required string Title { get; init; }
    public required string Explanation { get; init; }
    public required int Confidence { get; init; }
    public required IReadOnlyList<string> TriggeredBy { get; init; }
}

/// <summary>
/// Correlates the set of finding codes present in a scan into a named root-cause scenario —
/// the leap from "here is a list of symptoms" to "here is the underlying problem". Conservative
/// by design: a scenario only surfaces when enough related codes co-occur, otherwise the UI
/// falls back to the single most severe finding. Data-driven and easy to grow (add a Def).
/// </summary>
public static class Scenarios
{
    private sealed record Def
    {
        public required string TitleEn { get; init; }
        public required string TitleFr { get; init; }
        public required string ExplEn { get; init; }
        public required string ExplFr { get; init; }
        public required string[] Codes { get; init; }
        public required int MinMatch { get; init; }
        public required int BaseConfidence { get; init; }
        public required int PerCode { get; init; }
    }

    private static readonly Def[] Defs =
    {
        new()
        {
            TitleEn = "Incomplete 32→64 migration",
            TitleFr = "Migration 32→64 incomplète",
            ExplEn = "Your install moved to 64-bit Visual Pinball but components were left in 32-bit or are missing — the common root cause behind your ROM and DMD errors.",
            ExplFr = "Ton installation est passée en Visual Pinball 64-bit mais des composants sont restés en 32-bit ou manquent — c'est la cause commune de tes erreurs ROM et DMD.",
            Codes = new[] { "BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING", "BITNESS_HYBRID_INSTALL" },
            MinMatch = 2, BaseConfidence = 74, PerCode = 8,
        },
        new()
        {
            TitleEn = "Incomplete frontend integration",
            TitleFr = "Intégration frontend incomplète",
            ExplEn = "Several tables aren't properly linked to PinUP Popper or their backglass — they won't show up as expected in the frontend.",
            ExplFr = "Plusieurs tables ne sont pas correctement reliées à PinUP Popper ou à leur backglass — elles ne s'afficheront pas comme prévu dans le frontend.",
            Codes = new[] { "POPPER_NOT_REGISTERED", "B2S_MISSING" },
            MinMatch = 2, BaseConfidence = 70, PerCode = 8,
        },
    };

    public static ScenarioMatch? Detect(ISet<string> present)
    {
        ScenarioMatch? best = null;
        foreach (var d in Defs)
        {
            var matched = d.Codes.Where(present.Contains).ToList();
            if (matched.Count < d.MinMatch) continue;
            var conf = System.Math.Min(96, d.BaseConfidence + matched.Count * d.PerCode);
            if (best is null || conf > best.Confidence)
            {
                best = new ScenarioMatch
                {
                    Title = Loc.Lang == "fr" ? d.TitleFr : d.TitleEn,
                    Explanation = Loc.Lang == "fr" ? d.ExplFr : d.ExplEn,
                    Confidence = conf,
                    TriggeredBy = matched,
                };
            }
        }
        return best;
    }
}
