using System;
using System.Collections.Generic;
using System.Linq;
using PincabToolbox.App.Localization;

namespace PincabToolbox.App;

/// <summary>Tone of one causal-chain box — drives the border/status colour in the card.</summary>
public enum ChainTone { Good, Bad, Warn }

/// <summary>
/// One box of a scenario's causal chain, already localized and already filtered: a step only
/// exists here when the finding code that backs it was REALLY present in the scan. The box never
/// says more than what its backing code asserts (ADR-010).
/// </summary>
public sealed record ChainStepMatch
{
    public required string Label { get; init; }
    public required string Status { get; init; }
    public required ChainTone Tone { get; init; }
}

/// <summary>A detected root-cause scenario: several findings correlated into one diagnosis.</summary>
public sealed record ScenarioMatch
{
    public required string Title { get; init; }
    /// <summary>One line for the player — what this means for them, no jargon (maquette 11/08).</summary>
    public required string Player { get; init; }
    public required string Explanation { get; init; }
    public required int Confidence { get; init; }
    public required IReadOnlyList<string> TriggeredBy { get; init; }
    /// <summary>Causal chain limited to the steps whose backing code actually matched.</summary>
    public required IReadOnlyList<ChainStepMatch> Chain { get; init; }
}

/// <summary>
/// Correlates the set of finding codes present in a scan into named root-cause scenarios —
/// the leap from "here is a list of symptoms" to "here is the underlying problem". Conservative
/// by design: a scenario only surfaces when enough related codes co-occur, otherwise the UI
/// falls back to the single most severe finding. Data-driven and easy to grow (add a Def).
/// </summary>
public static class Scenarios
{
    private sealed record ChainStepDef
    {
        public required string LabelEn { get; init; }
        public required string LabelFr { get; init; }
        public required string StatusEn { get; init; }
        public required string StatusFr { get; init; }
        public required ChainTone Tone { get; init; }
        /// <summary>The step is shown ONLY when this code matched — every box is backed by a real result.</summary>
        public required string RequiresCode { get; init; }
    }

    private sealed record Def
    {
        public required string TitleEn { get; init; }
        public required string TitleFr { get; init; }
        public required string PlayerEn { get; init; }
        public required string PlayerFr { get; init; }
        public required string ExplEn { get; init; }
        public required string ExplFr { get; init; }
        public required string[] Codes { get; init; }
        public required int MinMatch { get; init; }
        public required int BaseConfidence { get; init; }
        public required int PerCode { get; init; }
        public required ChainStepDef[] Chain { get; init; }
    }

    private static readonly Def[] Defs =
    {
        new()
        {
            TitleEn = "Incomplete 32→64 migration",
            TitleFr = "Migration 32→64 incomplète",
            PlayerEn = "Two parts of your install are not from the same generation.",
            PlayerFr = "Deux morceaux de ton installation ne sont pas de la même génération.",
            ExplEn = "Your install moved to 64-bit Visual Pinball but components were left in 32-bit or are missing — the common root cause behind your ROM and DMD errors.",
            ExplFr = "Ton installation est passée en Visual Pinball 64-bit mais des composants sont restés en 32-bit ou manquent — c'est la cause commune de tes erreurs ROM et DMD.",
            Codes = new[] { "BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING", "BITNESS_HYBRID_INSTALL" },
            MinMatch = 2, BaseConfidence = 74, PerCode = 8,
            // Chaque case reprend uniquement ce que SON code affirme déjà : BITNESS_MISMATCH_VPM
            // mesure "VPX 64-bit présent + VPinMAME.dll 32-bit seulement" et conclut "ROM tables
            // will fail" ; BITNESS_DMD64_MISSING mesure l'absence de dmddevice64.dll. Le libellé
            // "Visual Pinball X" plutôt que "VPinballX64.exe" : le nom d'exécutable réel varie
            // (VPinballX_GL64.exe…), le code ne garantit que "un VPX 64-bit".
            Chain = new ChainStepDef[]
            {
                new() { LabelEn = "Visual Pinball X", LabelFr = "Visual Pinball X", StatusEn = "✓ 64-bit", StatusFr = "✓ 64-bit", Tone = ChainTone.Good, RequiresCode = "BITNESS_MISMATCH_VPM" },
                new() { LabelEn = "VPinMAME.dll", LabelFr = "VPinMAME.dll", StatusEn = "✕ 32-bit", StatusFr = "✕ 32-bit", Tone = ChainTone.Bad, RequiresCode = "BITNESS_MISMATCH_VPM" },
                new() { LabelEn = "dmddevice64.dll", LabelFr = "dmddevice64.dll", StatusEn = "▲ missing", StatusFr = "▲ absent", Tone = ChainTone.Warn, RequiresCode = "BITNESS_DMD64_MISSING" },
                new() { LabelEn = "ROM tables", LabelFr = "Tables à ROM", StatusEn = "✕ won't start", StatusFr = "✕ ne démarrent pas", Tone = ChainTone.Bad, RequiresCode = "BITNESS_MISMATCH_VPM" },
            },
        },
        new()
        {
            TitleEn = "Incomplete frontend integration",
            TitleFr = "Intégration frontend incomplète",
            PlayerEn = "These tables are installed but won't show up in your menu.",
            PlayerFr = "Ces tables sont installées mais n'apparaîtront pas dans ton menu.",
            ExplEn = "Several tables aren't properly linked to PinUP Popper or their backglass — they won't show up as expected in the frontend.",
            ExplFr = "Plusieurs tables ne sont pas correctement reliées à PinUP Popper ou à leur backglass — elles ne s'afficheront pas comme prévu dans le frontend.",
            Codes = new[] { "POPPER_NOT_REGISTERED", "B2S_MISSING" },
            MinMatch = 2, BaseConfidence = 70, PerCode = 8,
            // MinMatch = 2 sur 2 codes : quand ce scénario sort, POPPER_NOT_REGISTERED ET
            // B2S_MISSING ont tous deux réellement matché — les 4 cases sont donc couvertes.
            Chain = new ChainStepDef[]
            {
                new() { LabelEn = ".vpx file", LabelFr = "Fichier .vpx", StatusEn = "✓ present", StatusFr = "✓ présent", Tone = ChainTone.Good, RequiresCode = "POPPER_NOT_REGISTERED" },
                new() { LabelEn = "Popper database", LabelFr = "Base Popper", StatusEn = "✕ not registered", StatusFr = "✕ non enregistrée", Tone = ChainTone.Bad, RequiresCode = "POPPER_NOT_REGISTERED" },
                new() { LabelEn = "Backglass", LabelFr = "Backglass", StatusEn = "▲ missing", StatusFr = "▲ absent", Tone = ChainTone.Warn, RequiresCode = "B2S_MISSING" },
                new() { LabelEn = "Frontend menu", LabelFr = "Menu frontend", StatusEn = "✕ invisible", StatusFr = "✕ invisible", Tone = ChainTone.Bad, RequiresCode = "POPPER_NOT_REGISTERED" },
            },
        },
        new()
        {
            // LOT A (spec 10/08, décision D-3) — VPINMAME_NOT_REGISTERED est le seul code Critical
            // du lot COM et ses 4 conditions sont toutes MESURÉES (dll présente, ProgID absent des
            // deux vues du registre, aucun repli sur un échec de lecture). Un seul code suffit donc
            // ici (MinMatch = 1) : c'est déjà un diagnostic complet, pas une corrélation fragile.
            TitleEn = "VPinMAME registration missing",
            TitleFr = "Enregistrement VPinMAME manquant",
            PlayerEn = "The component that runs ROM tables is there, but Windows doesn't know it.",
            PlayerFr = "Le composant qui fait tourner les tables à ROM est là, mais Windows ne le connaît pas.",
            ExplEn = "VPinMAME.dll is present on disk but VPinMAME.Controller is not registered in either view of the COM registry — ROM tables cannot load it.",
            ExplFr = "VPinMAME.dll est présent sur le disque mais VPinMAME.Controller n'est enregistré dans aucune des deux vues du registre COM — les tables à ROM ne peuvent pas le charger.",
            Codes = new[] { "VPINMAME_NOT_REGISTERED" },
            MinMatch = 1, BaseConfidence = 80, PerCode = 8,
            Chain = new ChainStepDef[]
            {
                new() { LabelEn = "VPinMAME.dll", LabelFr = "VPinMAME.dll", StatusEn = "✓ present", StatusFr = "✓ présente", Tone = ChainTone.Good, RequiresCode = "VPINMAME_NOT_REGISTERED" },
                new() { LabelEn = "COM registry", LabelFr = "Registre COM", StatusEn = "✕ not registered", StatusFr = "✕ non enregistré", Tone = ChainTone.Bad, RequiresCode = "VPINMAME_NOT_REGISTERED" },
                new() { LabelEn = "ROM tables", LabelFr = "Tables à ROM", StatusEn = "✕ won't start", StatusFr = "✕ ne démarrent pas", Tone = ChainTone.Bad, RequiresCode = "VPINMAME_NOT_REGISTERED" },
            },
        },
    };

    /// <summary>
    /// Every scenario whose codes co-occur in this scan, best confidence first — the "Causes
    /// racines (N)" list of the 11/08 mockup. The screen shows real detections only: no minimum,
    /// no padding, an empty list means the UI falls back to the single most severe finding.
    /// </summary>
    public static IReadOnlyList<ScenarioMatch> DetectAll(ISet<string> present)
    {
        var fr = Loc.Lang == "fr";
        var matches = new List<ScenarioMatch>();
        foreach (var d in Defs)
        {
            var matched = d.Codes.Where(present.Contains).ToList();
            if (matched.Count < d.MinMatch) continue;
            matches.Add(new ScenarioMatch
            {
                Title = fr ? d.TitleFr : d.TitleEn,
                Player = fr ? d.PlayerFr : d.PlayerEn,
                Explanation = fr ? d.ExplFr : d.ExplEn,
                Confidence = System.Math.Min(96, d.BaseConfidence + matched.Count * d.PerCode),
                TriggeredBy = matched,
                Chain = d.Chain
                    .Where(s => matched.Contains(s.RequiresCode))
                    .Select(s => new ChainStepMatch
                    {
                        Label = fr ? s.LabelFr : s.LabelEn,
                        Status = fr ? s.StatusFr : s.StatusEn,
                        Tone = s.Tone,
                    })
                    .ToList(),
            });
        }
        return matches.OrderByDescending(m => m.Confidence).ToList();
    }

    /// <summary>The single best scenario — kept for callers that only want the headline diagnosis.</summary>
    public static ScenarioMatch? Detect(ISet<string> present) => DetectAll(present).FirstOrDefault();
}
