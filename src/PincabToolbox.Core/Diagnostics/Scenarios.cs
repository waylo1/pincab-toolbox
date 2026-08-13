using System;
using System.Collections.Generic;
using System.Linq;

namespace PincabToolbox.Core.Diagnostics;

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
///
/// <para>
/// Point 5/6 (13/08) — moved here from PincabToolbox.App per ADR-012 ("decision logic belongs in a
/// testable assembly, not App"). The only real change made in the move: <see cref="DetectAll"/> and
/// <see cref="Detect"/> now take an explicit language parameter instead of reading
/// PincabToolbox.App.Localization.Loc.Lang directly — Core cannot reference App (App references
/// Core, never the other way), so the language choice has to arrive as an argument.
/// </para>
///
/// <para>
/// 14/08 — <c>bool fr</c> became <c>string lang</c> ("en"/"fr"/"es") when Spanish was added as a
/// third UI language. The caller (MainWindow.xaml.cs) now passes <c>Loc.Lang</c> directly.
/// </para>
/// </summary>
public static class Scenarios
{
    private sealed record ChainStepDef
    {
        public required string LabelEn { get; init; }
        public required string LabelFr { get; init; }
        public required string LabelEs { get; init; }
        public required string StatusEn { get; init; }
        public required string StatusFr { get; init; }
        public required string StatusEs { get; init; }
        public required ChainTone Tone { get; init; }
        /// <summary>The step is shown ONLY when this code matched — every box is backed by a real result.</summary>
        public required string RequiresCode { get; init; }
    }

    private sealed record Def
    {
        public required string TitleEn { get; init; }
        public required string TitleFr { get; init; }
        public required string TitleEs { get; init; }
        public required string PlayerEn { get; init; }
        public required string PlayerFr { get; init; }
        public required string PlayerEs { get; init; }
        public required string ExplEn { get; init; }
        public required string ExplFr { get; init; }
        public required string ExplEs { get; init; }
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
            TitleEs = "Migración 32→64 incompleta",
            PlayerEn = "Two parts of your install are not from the same generation.",
            PlayerFr = "Deux morceaux de ton installation ne sont pas de la même génération.",
            PlayerEs = "Dos partes de tu instalación no son de la misma generación.",
            ExplEn = "Your install moved to 64-bit Visual Pinball but components were left in 32-bit or are missing — the common root cause behind your ROM and DMD errors.",
            ExplFr = "Ton installation est passée en Visual Pinball 64-bit mais des composants sont restés en 32-bit ou manquent — c'est la cause commune de tes erreurs ROM et DMD.",
            ExplEs = "Tu instalación pasó a Visual Pinball de 64 bits, pero algunos componentes se quedaron en 32 bits o faltan — la causa común de tus errores de ROM y DMD.",
            Codes = new[] { "BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING", "BITNESS_HYBRID_INSTALL" },
            MinMatch = 2, BaseConfidence = 74, PerCode = 8,
            // Chaque case reprend uniquement ce que SON code affirme déjà : BITNESS_MISMATCH_VPM
            // mesure "VPX 64-bit présent + VPinMAME.dll 32-bit seulement" et conclut "ROM tables
            // will fail" ; BITNESS_DMD64_MISSING mesure l'absence de dmddevice64.dll. Le libellé
            // "Visual Pinball X" plutôt que "VPinballX64.exe" : le nom d'exécutable réel varie
            // (VPinballX_GL64.exe…), le code ne garantit que "un VPX 64-bit".
            Chain = new ChainStepDef[]
            {
                new() { LabelEn = "Visual Pinball X", LabelFr = "Visual Pinball X", LabelEs = "Visual Pinball X", StatusEn = "✓ 64-bit", StatusFr = "✓ 64-bit", StatusEs = "✓ 64 bits", Tone = ChainTone.Good, RequiresCode = "BITNESS_MISMATCH_VPM" },
                new() { LabelEn = "VPinMAME.dll", LabelFr = "VPinMAME.dll", LabelEs = "VPinMAME.dll", StatusEn = "✕ 32-bit", StatusFr = "✕ 32-bit", StatusEs = "✕ 32 bits", Tone = ChainTone.Bad, RequiresCode = "BITNESS_MISMATCH_VPM" },
                new() { LabelEn = "dmddevice64.dll", LabelFr = "dmddevice64.dll", LabelEs = "dmddevice64.dll", StatusEn = "▲ missing", StatusFr = "▲ absent", StatusEs = "▲ falta", Tone = ChainTone.Warn, RequiresCode = "BITNESS_DMD64_MISSING" },
                new() { LabelEn = "ROM tables", LabelFr = "Tables à ROM", LabelEs = "Tablas con ROM", StatusEn = "✕ won't start", StatusFr = "✕ ne démarrent pas", StatusEs = "✕ no arrancan", Tone = ChainTone.Bad, RequiresCode = "BITNESS_MISMATCH_VPM" },
            },
        },
        new()
        {
            TitleEn = "Incomplete frontend integration",
            TitleFr = "Intégration frontend incomplète",
            TitleEs = "Integración de frontend incompleta",
            PlayerEn = "These tables are installed but won't show up in your menu.",
            PlayerFr = "Ces tables sont installées mais n'apparaîtront pas dans ton menu.",
            PlayerEs = "Estas tablas están instaladas pero no aparecerán en tu menú.",
            ExplEn = "Several tables aren't properly linked to PinUP Popper or their backglass — they won't show up as expected in the frontend.",
            ExplFr = "Plusieurs tables ne sont pas correctement reliées à PinUP Popper ou à leur backglass — elles ne s'afficheront pas comme prévu dans le frontend.",
            ExplEs = "Varias tablas no están correctamente vinculadas a PinUP Popper o a su backglass — no se mostrarán como se espera en el frontend.",
            Codes = new[] { "POPPER_NOT_REGISTERED", "B2S_MISSING" },
            MinMatch = 2, BaseConfidence = 70, PerCode = 8,
            // MinMatch = 2 sur 2 codes : quand ce scénario sort, POPPER_NOT_REGISTERED ET
            // B2S_MISSING ont tous deux réellement matché — les 4 cases sont donc couvertes.
            Chain = new ChainStepDef[]
            {
                new() { LabelEn = ".vpx file", LabelFr = "Fichier .vpx", LabelEs = "Archivo .vpx", StatusEn = "✓ present", StatusFr = "✓ présent", StatusEs = "✓ presente", Tone = ChainTone.Good, RequiresCode = "POPPER_NOT_REGISTERED" },
                new() { LabelEn = "Popper database", LabelFr = "Base Popper", LabelEs = "Base de datos Popper", StatusEn = "✕ not registered", StatusFr = "✕ non enregistrée", StatusEs = "✕ no registrada", Tone = ChainTone.Bad, RequiresCode = "POPPER_NOT_REGISTERED" },
                new() { LabelEn = "Backglass", LabelFr = "Backglass", LabelEs = "Backglass", StatusEn = "▲ missing", StatusFr = "▲ absent", StatusEs = "▲ falta", Tone = ChainTone.Warn, RequiresCode = "B2S_MISSING" },
                new() { LabelEn = "Frontend menu", LabelFr = "Menu frontend", LabelEs = "Menú del frontend", StatusEn = "✕ invisible", StatusFr = "✕ invisible", StatusEs = "✕ invisible", Tone = ChainTone.Bad, RequiresCode = "POPPER_NOT_REGISTERED" },
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
            TitleEs = "Falta el registro de VPinMAME",
            PlayerEn = "The component that runs ROM tables is there, but Windows doesn't know it.",
            PlayerFr = "Le composant qui fait tourner les tables à ROM est là, mais Windows ne le connaît pas.",
            PlayerEs = "El componente que hace funcionar las tablas con ROM está ahí, pero Windows no lo reconoce.",
            ExplEn = "VPinMAME.dll is present on disk but VPinMAME.Controller is not registered in either view of the COM registry — ROM tables cannot load it.",
            ExplFr = "VPinMAME.dll est présent sur le disque mais VPinMAME.Controller n'est enregistré dans aucune des deux vues du registre COM — les tables à ROM ne peuvent pas le charger.",
            ExplEs = "VPinMAME.dll está presente en el disco, pero VPinMAME.Controller no está registrado en ninguna de las dos vistas del registro COM — las tablas con ROM no pueden cargarlo.",
            Codes = new[] { "VPINMAME_NOT_REGISTERED" },
            MinMatch = 1, BaseConfidence = 80, PerCode = 8,
            Chain = new ChainStepDef[]
            {
                new() { LabelEn = "VPinMAME.dll", LabelFr = "VPinMAME.dll", LabelEs = "VPinMAME.dll", StatusEn = "✓ present", StatusFr = "✓ présente", StatusEs = "✓ presente", Tone = ChainTone.Good, RequiresCode = "VPINMAME_NOT_REGISTERED" },
                new() { LabelEn = "COM registry", LabelFr = "Registre COM", LabelEs = "Registro COM", StatusEn = "✕ not registered", StatusFr = "✕ non enregistré", StatusEs = "✕ no registrado", Tone = ChainTone.Bad, RequiresCode = "VPINMAME_NOT_REGISTERED" },
                new() { LabelEn = "ROM tables", LabelFr = "Tables à ROM", LabelEs = "Tablas con ROM", StatusEn = "✕ won't start", StatusFr = "✕ ne démarrent pas", StatusEs = "✕ no arrancan", Tone = ChainTone.Bad, RequiresCode = "VPINMAME_NOT_REGISTERED" },
            },
        },
        new()
        {
            // Point 4/6 (13/08) — mirror of the first scenario, other direction. BitnessScanner
            // measures this as its own independent condition (has32Main && !hasVpm32 && hasVpm64,
            // BitnessScanner.cs) — a single Critical code that is already a complete diagnosis on
            // its own, same shape as VPINMAME_NOT_REGISTERED just above (MinMatch = 1, no
            // correlation invented). Kept as its OWN Def rather than folded into scenario 1: the
            // chain text of scenario 1 is hardcoded to "VPX 64-bit / VPinMAME 32-bit" and would be
            // wrong read backwards, so a second Def is the honest way to cover the reverse case.
            TitleEn = "32-bit install with 64-bit-only VPinMAME",
            TitleFr = "Installation 32-bit avec VPinMAME 64-bit uniquement",
            TitleEs = "Instalación de 32 bits con VPinMAME solo de 64 bits",
            PlayerEn = "Your Visual Pinball is 32-bit, but only the 64-bit ROM component is installed.",
            PlayerFr = "Ton Visual Pinball est en 32-bit, mais seul le composant ROM 64-bit est installé.",
            PlayerEs = "Tu Visual Pinball es de 32 bits, pero solo está instalado el componente ROM de 64 bits.",
            ExplEn = "This is the reverse of the more common 32→64 migration issue: a 32-bit Visual Pinball executable is present but only VPinMAME64.dll was found — 32-bit VPX cannot load the 64-bit COM server, so every ROM table will fail.",
            ExplFr = "C'est l'inverse du problème de migration 32→64 le plus courant : un exécutable Visual Pinball 32-bit est présent mais seul VPinMAME64.dll a été trouvé — VPX 32-bit ne peut pas charger le serveur COM 64-bit, donc toutes les tables à ROM vont échouer.",
            ExplEs = "Es lo contrario del problema de migración 32→64 más habitual: hay un ejecutable de Visual Pinball de 32 bits, pero solo se encontró VPinMAME64.dll — el VPX de 32 bits no puede cargar el servidor COM de 64 bits, así que todas las tablas con ROM fallarán.",
            Codes = new[] { "BITNESS_MISMATCH_VPM32" },
            MinMatch = 1, BaseConfidence = 80, PerCode = 8,
            Chain = new ChainStepDef[]
            {
                new() { LabelEn = "Visual Pinball X", LabelFr = "Visual Pinball X", LabelEs = "Visual Pinball X", StatusEn = "✓ 32-bit", StatusFr = "✓ 32-bit", StatusEs = "✓ 32 bits", Tone = ChainTone.Good, RequiresCode = "BITNESS_MISMATCH_VPM32" },
                new() { LabelEn = "VPinMAME.dll", LabelFr = "VPinMAME.dll", LabelEs = "VPinMAME.dll", StatusEn = "✕ 64-bit only", StatusFr = "✕ 64-bit uniquement", StatusEs = "✕ solo 64 bits", Tone = ChainTone.Bad, RequiresCode = "BITNESS_MISMATCH_VPM32" },
                new() { LabelEn = "ROM tables", LabelFr = "Tables à ROM", LabelEs = "Tablas con ROM", StatusEn = "✕ won't start", StatusFr = "✕ ne démarrent pas", StatusEs = "✕ no arrancan", Tone = ChainTone.Bad, RequiresCode = "BITNESS_MISMATCH_VPM32" },
            },
        },
        new()
        {
            // Point 4/6 (13/08) — COM_STALE_PATH (ComHealthScanner.cs) already measures every fact
            // this scenario states: the component IS registered (view32/view64 not null) AND the
            // path it's registered to no longer exists on disk. Single Warning-level code, MinMatch
            // = 1, same "one code is already the whole diagnosis" shape as VPINMAME_NOT_REGISTERED.
            // BaseConfidence kept a notch below the two Critical-only scenarios above (80): the
            // underlying finding is a Warning, not a Critical, and confidence should track that.
            TitleEn = "Component registered to a deleted location",
            TitleFr = "Composant enregistré vers un emplacement supprimé",
            TitleEs = "Componente registrado en una ubicación eliminada",
            PlayerEn = "A required component is registered, but Windows is looking for it somewhere that no longer exists.",
            PlayerFr = "Un composant requis est enregistré, mais Windows le cherche à un endroit qui n'existe plus.",
            PlayerEs = "Un componente necesario está registrado, pero Windows lo busca en un sitio que ya no existe.",
            ExplEn = "The COM registration for this component still points to a file path that has been moved, renamed, or deleted — a leftover from a previous install location. Loading it will fail even though a working copy may exist elsewhere.",
            ExplFr = "L'enregistrement COM de ce composant pointe encore vers un chemin de fichier qui a été déplacé, renommé ou supprimé — un reliquat d'un emplacement d'installation précédent. Son chargement échouera même si une copie fonctionnelle existe peut-être ailleurs.",
            ExplEs = "El registro COM de este componente sigue apuntando a una ruta de archivo que fue movida, renombrada o eliminada — un resto de una ubicación de instalación anterior. La carga fallará aunque exista una copia funcional en otro lugar.",
            Codes = new[] { "COM_STALE_PATH" },
            MinMatch = 1, BaseConfidence = 68, PerCode = 8,
            Chain = new ChainStepDef[]
            {
                new() { LabelEn = "Registration entry", LabelFr = "Entrée d'enregistrement", LabelEs = "Entrada de registro", StatusEn = "✓ present", StatusFr = "✓ présente", StatusEs = "✓ presente", Tone = ChainTone.Good, RequiresCode = "COM_STALE_PATH" },
                new() { LabelEn = "Target file", LabelFr = "Fichier cible", LabelEs = "Archivo de destino", StatusEn = "✕ missing", StatusFr = "✕ absent", StatusEs = "✕ ausente", Tone = ChainTone.Bad, RequiresCode = "COM_STALE_PATH" },
                new() { LabelEn = "Component", LabelFr = "Composant", LabelEs = "Componente", StatusEn = "✕ won't load", StatusFr = "✕ ne se charge pas", StatusEs = "✕ no carga", Tone = ChainTone.Bad, RequiresCode = "COM_STALE_PATH" },
            },
        },
        new()
        {
            // Point 4/6 (13/08) — FeatureEnabledScanner.cs (LOT D) emits ALTSOUND_PRESENT_NOT_ENABLED
            // and ALTCOLOR_PRESENT_NOT_ENABLED independently, each already fully measured on its own
            // (pack files verified present/complete on disk AND the matching VPinMAME registry mode
            // read as 0/off — never a guess, per that scanner's own doc). MinMatch = 1: this is NOT
            // "these two unrelated things co-occurring means X" the way scenario 1/2 correlate
            // different scanners — it's the SAME "installed but switched off" pattern measured twice
            // (sound, colour), so either alone is already the complete story and firing on one is
            // honest. Both matching just adds the second chain pair, never inflates past what was
            // actually measured for that.
            TitleEn = "Sound/color pack installed but switched off",
            TitleFr = "Pack son/couleur installé mais désactivé",
            TitleEs = "Pack de sonido/color instalado pero desactivado",
            PlayerEn = "You installed extra sound or color files for a table, but VPinMAME is still set to ignore them.",
            PlayerFr = "Tu as installé des fichiers son ou couleur en plus pour une table, mais VPinMAME est encore réglé pour les ignorer.",
            PlayerEs = "Instalaste archivos extra de sonido o color para una tabla, pero VPinMAME sigue configurado para ignorarlos.",
            ExplEn = "The AltSound and/or AltColor files for at least one ROM are present and complete, but VPinMAME's per-game option to actually use them is switched off — the pack stays silent/inactive until that option is flipped.",
            ExplFr = "Les fichiers AltSound et/ou AltColor pour au moins une ROM sont présents et complets, mais l'option VPinMAME par jeu qui les active réellement est désactivée — le pack reste silencieux/inactif tant que cette option n'est pas basculée.",
            ExplEs = "Los archivos AltSound y/o AltColor de al menos una ROM están presentes y completos, pero la opción de VPinMAME por juego que realmente los activa está desactivada — el pack permanece silencioso/inactivo hasta que se active esa opción.",
            Codes = new[] { "ALTSOUND_PRESENT_NOT_ENABLED", "ALTCOLOR_PRESENT_NOT_ENABLED" },
            MinMatch = 1, BaseConfidence = 78, PerCode = 8,
            Chain = new ChainStepDef[]
            {
                new() { LabelEn = "AltSound pack", LabelFr = "Pack AltSound", LabelEs = "Pack AltSound", StatusEn = "✓ installed", StatusFr = "✓ installé", StatusEs = "✓ instalado", Tone = ChainTone.Good, RequiresCode = "ALTSOUND_PRESENT_NOT_ENABLED" },
                new() { LabelEn = "Sound Mode option", LabelFr = "Option Sound Mode", LabelEs = "Opción Sound Mode", StatusEn = "✕ off", StatusFr = "✕ désactivée", StatusEs = "✕ desactivada", Tone = ChainTone.Bad, RequiresCode = "ALTSOUND_PRESENT_NOT_ENABLED" },
                new() { LabelEn = "AltColor/Serum set", LabelFr = "Set AltColor/Serum", LabelEs = "Set AltColor/Serum", StatusEn = "✓ complete", StatusFr = "✓ complet", StatusEs = "✓ completo", Tone = ChainTone.Good, RequiresCode = "ALTCOLOR_PRESENT_NOT_ENABLED" },
                new() { LabelEn = "DMD colorization option", LabelFr = "Option colorisation DMD", LabelEs = "Opción de colorización DMD", StatusEn = "✕ off", StatusFr = "✕ désactivée", StatusEs = "✕ desactivada", Tone = ChainTone.Bad, RequiresCode = "ALTCOLOR_PRESENT_NOT_ENABLED" },
            },
        },
    };

    /// <summary>
    /// Every scenario whose codes co-occur in this scan, best confidence first — the "Causes
    /// racines (N)" list of the 11/08 mockup. The screen shows real detections only: no minimum,
    /// no padding, an empty list means the UI falls back to the single most severe finding.
    /// </summary>
    /// <param name="present">The set of finding codes present in this scan.</param>
    /// <param name="lang">"en", "fr" or "es" — the caller's own language choice (Core has no
    /// notion of "current UI language" of its own). Anything else falls back to English.</param>
    public static IReadOnlyList<ScenarioMatch> DetectAll(ISet<string> present, string lang)
    {
        var matches = new List<ScenarioMatch>();
        foreach (var d in Defs)
        {
            var matched = d.Codes.Where(present.Contains).ToList();
            if (matched.Count < d.MinMatch) continue;
            matches.Add(new ScenarioMatch
            {
                Title = Pick(lang, d.TitleEn, d.TitleFr, d.TitleEs),
                Player = Pick(lang, d.PlayerEn, d.PlayerFr, d.PlayerEs),
                Explanation = Pick(lang, d.ExplEn, d.ExplFr, d.ExplEs),
                Confidence = Math.Min(96, d.BaseConfidence + matched.Count * d.PerCode),
                TriggeredBy = matched,
                Chain = d.Chain
                    .Where(s => matched.Contains(s.RequiresCode))
                    .Select(s => new ChainStepMatch
                    {
                        Label = Pick(lang, s.LabelEn, s.LabelFr, s.LabelEs),
                        Status = Pick(lang, s.StatusEn, s.StatusFr, s.StatusEs),
                        Tone = s.Tone,
                    })
                    .ToList(),
            });
        }
        return matches.OrderByDescending(m => m.Confidence).ToList();
    }

    private static string Pick(string lang, string en, string fr, string es) => lang switch
    {
        "fr" => fr,
        "es" => es,
        _ => en,
    };

    /// <summary>The single best scenario — kept for callers that only want the headline diagnosis.</summary>
    public static ScenarioMatch? Detect(ISet<string> present, string lang) => DetectAll(present, lang).FirstOrDefault();
}
