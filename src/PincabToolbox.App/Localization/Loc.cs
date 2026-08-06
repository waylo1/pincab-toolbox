using System.Globalization;
using PincabToolbox.Core.Models;

namespace PincabToolbox.App.Localization;

/// <summary>
/// Minimal bilingual string table (EN default, FR). Chosen once at startup from the
/// OS culture; the toolbar button toggles at runtime (the window re-applies texts).
/// </summary>
public static class Loc
{
    public static string Lang { get; private set; } =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("fr", StringComparison.OrdinalIgnoreCase) ? "fr" : "en";

    public static event Action? LanguageChanged;

    public static void Toggle()
    {
        Lang = Lang == "fr" ? "en" : "fr";
        LanguageChanged?.Invoke();
    }

    /// <summary>Force a specific language ("fr"/"en"); used to restore the saved preference at startup.</summary>
    public static void SetLang(string lang)
    {
        lang = lang == "fr" ? "fr" : "en";
        if (lang == Lang) return;
        Lang = lang;
        LanguageChanged?.Invoke();
    }

    public static string Get(string key)
    {
        if (Lang == "fr" && Fr.TryGetValue(key, out var fr)) return fr;
        return En.TryGetValue(key, out var en) ? en : key;
    }

    /// <summary>Localized finding text — falls back to the Core's English rendering.</summary>
    public static string FindingText(Finding f)
    {
        if (Lang != "fr") return f.EnglishText;
        if (!FrFindings.TryGetValue(f.Code, out var template)) return f.EnglishText;
        try
        {
            return string.Format(template, f.Args.Cast<object?>().ToArray());
        }
        catch (FormatException)
        {
            return f.EnglishText;
        }
    }

    /// <summary>Localized fix hint by finding code; falls back to the Core's English FixHint.</summary>
    public static string? FixHintText(Finding f)
    {
        if (f.FixHint is null) return null;
        if (Lang == "fr" && FrFixHints.TryGetValue(f.Code, out var fr)) return fr;
        return f.FixHint;
    }

    public static string SeverityLabel(Severity s) => Get("sev." + s);

    private static readonly Dictionary<string, string> En = new()
    {
        ["app.title"] = "Pincab Toolbox",
        ["tab.scanner"] = "Scanner",
        ["tab.diff"] = "Script Diff",
        ["tab.about"] = "About",
        ["scan.root"] = "Pincab root folder:",
        ["scan.browse"] = "Browse…",
        ["scan.demo"] = "Demo mode",
        ["scan.start"] = "SCAN MY PINCAB",
        ["scan.running"] = "Scanning…",
        ["scan.cancel"] = "Cancel",
        ["scan.export"] = "Export report",
        ["scan.copyforum"] = "Copy for forum",
        ["report.copied"] = "Report copied to clipboard — paste it on the forum.",
        ["scan.placeholder"] = "Select the root folder of your virtual pinball installation (the one that contains Tables, VPinMAME, PinUPSystem…) and press Scan.",
        ["scan.empty"] = "No findings yet.",
        ["scan.hint.notables"] = "No .vpx tables found — check that you selected the right folder.",
        ["filter.critical"] = "Critical",
        ["filter.warning"] = "Warnings",
        ["filter.note"] = "Notes",
        ["filter.info"] = "Info",
        ["filter.ok"] = "OK",
        ["score.a"] = "Healthy install",
        ["score.b"] = "A few things to watch",
        ["score.c"] = "Needs fixing",
        ["score.f"] = "Install in bad shape",
        ["priority.label"] = "FIX THIS FIRST",
        ["priority.watch"] = "WORTH A LOOK",
        ["diagnosis.label"] = "MAIN DIAGNOSIS",
        ["diagnosis.confidence"] = "reliability",
        ["priority.basedon"] = "Based on:",
        ["detail.impact"] = "IMPACT",
        ["detail.cause"] = "PROBABLE CAUSE",
        ["detail.fix"] = "RECOMMENDED FIX",
        // Écran 1 (UX-COPY-Repair.md) — facts computed from the real plan by RepairOfferBuilder,
        // never declared. "soon" because no purchase flow exists yet (ADR-009 not wired).
        ["repair.checks.fixable"] = "✓ Fixable automatically",
        ["repair.checks.backup"] = "✓ Backed up before changing",
        ["repair.checks.reversible"] = "✓ Reversible — one click to undo",
        ["repair.checks.duration.seconds"] = "⏱ A few seconds",
        ["repair.checks.duration.underminute"] = "⏱ Under a minute",
        ["repair.checks.duration.minutes"] = "⏱ A few minutes",
        ["repair.tag"] = "🔒 Repair — coming soon",
        ["repair.summary"] = "Repair could fix {0} of the {1} findings here automatically — coming soon.",
        ["repair.notautomatable"] = "Some steps will always stay manual, licence or not:",
        ["col.severity"] = "Severity",
        ["col.category"] = "Module",
        ["col.subject"] = "Subject",
        ["col.message"] = "Details",
        ["col.action"] = "Action",
        ["search.hint"] = "Search…",
        ["action.folder"] = "Open folder",
        ["action.update"] = "Open update",
        ["action.copy"] = "Copy details",
        ["action.copied"] = "Copied to clipboard.",
        ["sev.Critical"] = "CRITICAL",
        ["sev.Warning"] = "Warning",
        ["sev.Note"] = "Note",
        ["sev.Info"] = "Info",
        ["sev.Ok"] = "OK",
        ["diff.old"] = "Old table (.vpx or .vbs):",
        ["diff.new"] = "New table (.vpx or .vbs):",
        ["diff.compare"] = "Compare scripts",
        ["diff.summary"] = "{0} modified · {1} added · {2} removed",
        ["diff.placeholder"] = "Pick two versions of a table (or two .vbs scripts) to see exactly what changed — before installing an update blindly.",
        ["report.saved"] = "Report saved: ",
        ["status.ready"] = "Ready.",
        ["status.done"] = "Analysis complete · {0} checks — {1} critical, {2} warnings, {3} info, {4} notes.",
        ["scan.demolabel"] = "Demo — sample install",
        ["diff.empty"] = "Compare the script of two versions of a table (.vpx or .vbs) to see exactly what changed. Pick an old and a new file above, then Compare.",
        ["scan.copied"] = "✓ Copied",
        ["cat.rom"] = "ROM",
        ["cat.bitness"] = "32/64-bit",
        ["cat.completeness"] = "Install",
        ["cat.compat"] = "Compatibility",
        ["cat.updates"] = "Updates",
        ["cat.security"] = "Security",
        ["cat.dependencies"] = "Plugins",
        ["cat.aliasloop"] = "VPMAlias",
        ["cat.nvram"] = "NVRAM",
        ["cat.altcolor"] = "AltColor",
        ["cat.altsound"] = "AltSound",
        ["cat.screentopology"] = "Screen Topology",
        ["cat.junctions"] = "Junctions",
        ["cat.directb2s"] = "DirectB2S",
        ["cat.popperplaylist"] = "Playlists",
        ["about.tagline"] = "The mechanic for your pincab.",
        ["about.body"] = "Pincab Toolbox scans your Visual Pinball X / PinUP Popper installation and tells you what is broken, missing or mismatched — before you hit Start on a table.\n\n• 100% local — nothing is uploaded, no telemetry, no account.\n• Read-only — the free scanner never modifies a single file.\n• The Update Watcher uses the open-source Virtual Pinball Spreadsheet database and only ever links you to official pages. It never downloads tables, ROMs or media.",
        ["about.roadmap"] = "Coming next: Repair — optional one-click fixes for some of what the scanner finds, always with a backup first, a preview of every change, and undo. The free scanner always stays read-only. Follow the forum thread to be notified.",
        ["about.version"] = "Version",
        ["onb.title"] = "Welcome to Pincab Toolbox",
        ["onb.lead"] = "A quick health check for your virtual pinball cabinet. About one minute, and completely safe.",
        ["onb.p1"] = "✓  Read-only — it never modifies a single file.",
        ["onb.p2"] = "✓  100% local — nothing is uploaded, no account, no telemetry.",
        ["onb.p3"] = "✓  It finds what's broken, missing or mismatched — and explains how to fix it.",
        ["onb.start"] = "Let's go",
    };

    private static readonly Dictionary<string, string> Fr = new()
    {
        ["app.title"] = "Pincab Toolbox",
        ["tab.scanner"] = "Scanner",
        ["tab.diff"] = "Diff de scripts",
        ["tab.about"] = "À propos",
        ["scan.root"] = "Dossier racine du pincab :",
        ["scan.browse"] = "Parcourir…",
        ["scan.demo"] = "Mode démo",
        ["scan.start"] = "SCANNER MON PINCAB",
        ["scan.running"] = "Scan en cours…",
        ["scan.cancel"] = "Annuler",
        ["scan.export"] = "Exporter le rapport",
        ["scan.copyforum"] = "Copier pour le forum",
        ["report.copied"] = "Rapport copié — colle-le sur le forum.",
        ["scan.placeholder"] = "Sélectionne le dossier racine de ton installation (celui qui contient Tables, VPinMAME, PinUPSystem…) puis lance le scan.",
        ["scan.empty"] = "Aucun résultat pour l'instant.",
        ["scan.hint.notables"] = "Aucune table .vpx trouvée — vérifie que tu as choisi le bon dossier.",
        ["filter.critical"] = "Critiques",
        ["filter.warning"] = "Avertissements",
        ["filter.note"] = "Notes",
        ["filter.info"] = "Infos",
        ["filter.ok"] = "OK",
        ["score.a"] = "Installation saine",
        ["score.b"] = "Quelques points à surveiller",
        ["score.c"] = "Installation à corriger",
        ["score.f"] = "Installation en mauvais état",
        ["priority.label"] = "À CORRIGER EN PRIORITÉ",
        ["priority.watch"] = "À VÉRIFIER",
        ["diagnosis.label"] = "DIAGNOSTIC PRINCIPAL",
        ["diagnosis.confidence"] = "fiabilité",
        ["priority.basedon"] = "Basé sur :",
        ["detail.impact"] = "IMPACT",
        ["detail.cause"] = "CAUSE PROBABLE",
        ["detail.fix"] = "CORRECTIF RECOMMANDÉ",
        // Écran 1 (UX-COPY-Repair.md) — faits calculés depuis le plan réel par RepairOfferBuilder,
        // jamais déclarés. « bientôt » car aucun parcours d'achat n'existe encore (ADR-009 non câblé).
        ["repair.checks.fixable"] = "✓ Réparable automatiquement",
        ["repair.checks.backup"] = "✓ Sauvegarde avant modification",
        ["repair.checks.reversible"] = "✓ Réversible — annulable en un clic",
        ["repair.checks.duration.seconds"] = "⏱ Quelques secondes",
        ["repair.checks.duration.underminute"] = "⏱ Moins d'une minute",
        ["repair.checks.duration.minutes"] = "⏱ Quelques minutes",
        ["repair.tag"] = "🔒 Repair — bientôt disponible",
        ["repair.summary"] = "Repair pourrait corriger {0} problème(s) sur {1} détecté(s) automatiquement — bientôt disponible.",
        ["repair.notautomatable"] = "Certaines étapes resteront toujours manuelles, licence ou pas :",
        ["col.severity"] = "Gravité",
        ["col.category"] = "Module",
        ["col.subject"] = "Sujet",
        ["col.message"] = "Détails",
        ["col.action"] = "Action",
        ["search.hint"] = "Rechercher…",
        ["action.folder"] = "Ouvrir le dossier",
        ["action.update"] = "Voir la mise à jour",
        ["action.copy"] = "Copier les détails",
        ["action.copied"] = "Copié dans le presse-papiers.",
        ["sev.Critical"] = "CRITIQUE",
        ["sev.Warning"] = "Avertissement",
        ["sev.Note"] = "À noter",
        ["sev.Info"] = "Info",
        ["sev.Ok"] = "OK",
        ["diff.old"] = "Ancienne table (.vpx ou .vbs) :",
        ["diff.new"] = "Nouvelle table (.vpx ou .vbs) :",
        ["diff.compare"] = "Comparer les scripts",
        ["diff.summary"] = "{0} modifiées · {1} ajoutées · {2} supprimées",
        ["diff.placeholder"] = "Choisis deux versions d'une table (ou deux scripts .vbs) pour voir exactement ce qui a changé — avant d'installer une mise à jour à l'aveugle.",
        ["report.saved"] = "Rapport enregistré : ",
        ["status.ready"] = "Prêt.",
        ["status.done"] = "Analyse terminée · {0} vérifications — {1} critiques, {2} avertissements, {3} infos, {4} à noter.",
        ["scan.demolabel"] = "Démo — installation d'exemple",
        ["diff.empty"] = "Compare le script de deux versions d'une table (.vpx ou .vbs) pour voir exactement ce qui a changé. Choisis un ancien et un nouveau fichier ci-dessus, puis Comparer.",
        ["scan.copied"] = "✓ Copié",
        ["cat.rom"] = "ROM",
        ["cat.bitness"] = "32/64-bit",
        ["cat.completeness"] = "Install",
        ["cat.compat"] = "Compatibilité",
        ["cat.updates"] = "Mises à jour",
        ["cat.security"] = "Sécurité",
        ["cat.dependencies"] = "Plugins",
        ["cat.aliasloop"] = "VPMAlias",
        ["cat.nvram"] = "NVRAM",
        ["cat.altcolor"] = "AltColor",
        ["cat.altsound"] = "AltSound",
        ["cat.screentopology"] = "Topologie écrans",
        ["cat.junctions"] = "Jonctions",
        ["cat.directb2s"] = "DirectB2S",
        ["cat.popperplaylist"] = "Playlists",
        ["about.tagline"] = "Le mécanicien de ton pincab.",
        ["about.body"] = "Pincab Toolbox scanne ton installation Visual Pinball X / PinUP Popper et te dit ce qui est cassé, manquant ou incompatible — avant que tu ne lances une table.\n\n• 100 % local — rien n'est envoyé, zéro télémétrie, zéro compte.\n• Lecture seule — le scanner gratuit ne modifie jamais le moindre fichier.\n• L'Update Watcher s'appuie sur la base open source Virtual Pinball Spreadsheet et se contente de te donner le lien officiel. Il ne télécharge jamais ni table, ni ROM, ni média.",
        ["about.roadmap"] = "Bientôt : Repair — des réparations optionnelles en un clic pour une partie de ce que le scanner trouve, toujours avec sauvegarde avant, aperçu de chaque changement et annulation. Le scanner gratuit reste toujours en lecture seule. Suis le fil du forum pour être prévenu.",
        ["about.version"] = "Version",
        ["onb.title"] = "Bienvenue dans Pincab Toolbox",
        ["onb.lead"] = "Un diagnostic rapide de ton flipper virtuel. Environ une minute, et totalement sans risque.",
        ["onb.p1"] = "✓  Lecture seule — ne modifie jamais le moindre fichier.",
        ["onb.p2"] = "✓  100 % local — rien n'est envoyé, sans compte, sans télémétrie.",
        ["onb.p3"] = "✓  Il trouve ce qui est cassé, manquant ou incompatible — et explique comment corriger.",
        ["onb.start"] = "C'est parti",
    };

    /// <summary>French templates per finding code ({0}, {1}… map to Finding.Args).</summary>
    private static readonly Dictionary<string, string> FrFindings = new()
    {
        // Ligne de regroupement (ScanReport.Rolled) — {0} = nombre, {1} = code regroupé.
        ["GROUPED"] = "{0} résultats similaires ({1}) — regroupés pour garder la liste lisible. Le rapport texte complet les contient tous.",
        // Tier A (handoff Sonnet 5, 06/08) — B1. {0} = nom de la ROM.
        ["ALTCOLOR_INCOMPLETE"] = "« {0} » a un jeu de colorisation AltColor/Serum incomplet — des fichiers existent dans altcolor/{0}/ mais ne forment pas une paire complète (.vni+.pal, ou un fichier Serum+.pal). Le DMD risque de s'afficher en mono, ou la colorisation peut ne pas se charger du tout.",
        // Tier A (handoff Sonnet 5, 06/08) — B2. {0}=ROM, {1}=nb absents, {2}=nb total référencés.
        ["ALTSOUND_SAMPLE_MISSING"] = "« {0} » : altsound.csv référence {1} échantillon(s) sur {2} qui sont absents de altsound/{0}/ — ces sons resteront silencieux, ou le plugin AltSound peut échouer à se charger.",
        // Tier A (handoff Sonnet 5, 06/08) — C1. {0} = fichier ScreenRes concerné (ScreenRes.txt ou "TableName").
        ["DISPLAY_OFFSCREEN"] = "La position du backglass définie dans « {0} » tombe entièrement en dehors de tous les écrans connectés — elle ne sera jamais visible, même si le fichier se charge sans erreur.",
        // Tier A (handoff Sonnet 5, 06/08) — G3. {0} = chemin du dossier, {1} = cible de la jonction.
        ["BROKEN_JUNCTION"] = "« {0} » est une jonction/lien symbolique pointant vers « {1} », qui n'existe plus — tout ce qui est attendu sous ce dossier est invisible pour Visual Pinball, PinUP Popper et ce scan.",
        // Tier A (handoff Sonnet 5, 06/08) — H2. {0} = nom du fichier .directb2s.
        ["B2S_MALFORMED"] = "« {0} » n'est pas du XML bien formé — B2S Backglass Server refuse de le charger, ce backglass n'apparaîtra donc pas du tout.",
        // Tier A (handoff Sonnet 5, 06/08) — F1. {0} = nb de jeux affectés, {1} = exemples (noms ou GameID).
        ["POPPER_ORPHAN_PLAYLIST"] = "{0} jeu(x) dans la base PinUP Popper sont affectés à une playlist qui n'existe plus — ceci est connu pour figer le menu du frontend Popper à l'ouverture.",
        // Tier A (handoff Sonnet 5, 06/08) — H1. {0} = nom de la ROM (sans extension).
        ["NVRAM_EMPTY"] = "Le fichier de sauvegarde NVRAM de « {0} » est vide (0 octet) — VPinMAME ne peut lire aucun état sauvegardé, la table risque de démarrer sur un écran noir ou de figer.",
        // Tier A (handoff Sonnet 5, 06/08) — E1. {0} = chaîne de la boucle (ex. "a -> b -> a").
        ["VPMALIAS_LOOP"] = "VPMAlias.txt contient une boucle d'alias : {0}. VPinMAME plante (stack overflow) dès qu'une table a besoin de ce nom de ROM.",

        ["ROM_MISSING"] = "« {0} » ne démarrera pas : la ROM « {1} » est absente du dossier roms.",
        ["ROM_OK"] = "« {0} » — ROM trouvée : {1}.",
        ["ROM_NOT_REQUIRED"] = "« {0} » ne nécessite pas de ROM (table originale/EM).",
        ["ROM_UNZIPPED"] = "La ROM de « {0} » est présente sous forme de dossier décompressé « {1} » — VPinMAME charge les ROMs depuis des .zip, elle ne sera donc pas trouvée.",
        ["POPPER_MEDIA_MISSING"] = "{0} jeu(x) enregistré(s) sur {1} n'ont pas d'image de wheel sous POPMedia — ils apparaîtront vides dans la roue PinUP Popper.",
        ["SCRIPT_UNREADABLE"] = "Impossible de lire le script de « {0} » ({1}).",
        ["TABLES_DIR_NOT_FOUND"] = "Aucun dossier de tables trouvé sous la racine choisie — est-ce bien une installation Visual Pinball ?",
        ["ROMS_DIR_NOT_FOUND"] = "Dossier roms de VPinMAME introuvable — vérification des ROMs ignorée.",
        ["BLOCKED_DLL"] = "« {0} » est bloqué par Windows (fichier téléchargé) — il risque de ne pas se charger tant que tu ne l'as pas débloqué.",
        ["BLOCKED_NONE"] = "Aucune DLL bloquée par Windows détectée.",
        ["BITNESS_INVENTORY"] = "{0} — {1} ({2}).",
        ["BITNESS_NOTHING_FOUND"] = "Aucun binaire connu à analyser.",
        ["BITNESS_MISMATCH_VPM"] = "Un Visual Pinball 64-bit est installé mais seul un VPinMAME.dll 32-bit a été trouvé. Le VPX 64-bit ne peut pas utiliser le serveur COM 32-bit — les tables à ROM échoueront.",
        ["BITNESS_MISMATCH_VPM32"] = "Un Visual Pinball 32-bit est installé mais seul un VPinMAME.dll 64-bit a été trouvé. Le VPX 32-bit ne peut pas utiliser le serveur COM 64-bit — les tables à ROM échoueront.",
        ["B2S_SERVER_MISSING"] = "Aucun B2SBackglassServer.dll trouvé sous cette installation alors que des backglass (ou des scripts) en ont besoin — les backglass ne s'afficheront pas tant que le B2S Backglass Server n'est pas installé et enregistré.",
        ["FLEXDMD_MISSING"] = "{0} table(s) utilisent FlexDMD mais aucun FlexDMD.dll n'a été trouvé sous cette installation — leur affichage DMD/score ne fonctionnera pas tant que FlexDMD n'est pas installé et enregistré.",
        ["B2S_SERVER_OK"] = "B2S Backglass Server installé et fichiers backglass présents.",
        ["BITNESS_HYBRID_INSTALL"] = "Des exécutables Visual Pinball 32-bit ET 64-bit sont présents. Une installation hybride fonctionne, mais chaque plugin (dmddevice, B2S, FlexDMD) doit exister dans LES DEUX variantes — ce scan liste ce que tu as.",
        ["BITNESS_DMD64_MISSING"] = "VPX 64-bit détecté mais pas de dmddevice64.dll — les DMD externes ne fonctionneront pas depuis le VPinMAME 64-bit.",
        ["B2S_MISSING"] = "« {0} » n'a pas de fichier backglass .directb2s à côté de la table.",
        ["B2S_ORPHAN"] = "Le backglass « {0}.directb2s » ne correspond à aucune table — B2S charge les backglass par nom de base exact, celui-ci est donc ignoré.",
        ["POPPER_NOT_REGISTERED"] = "« {0} » n'est pas enregistrée dans PinUP Popper — elle n'apparaîtra pas dans le frontend.",
        ["POPPER_DB_NOT_FOUND"] = "Base PinUP Popper introuvable — vérifications frontend ignorées.",
        ["PUPPACK_PRESENT"] = "« {0} » a un PUP-Pack ({1}).",
        ["COMPAT_MIN_VERSION"] = "« {0} » déclare nécessiter VPX {1}+ — vérifie ta version installée avant de lancer.",
        // Rétroactif (comparateur VPX du 05/08, complété 06/08 — R1). {0}=table, {1}=version requise, {2}=version installée.
        ["VPX_VERSION_OUTDATED"] = "« {0} » déclare nécessiter Visual Pinball X {1}+, mais la version VPX la plus récente installée est {2} — cette table risque de ne pas se charger ou de mal fonctionner tant que Visual Pinball X n'est pas mis à jour.",
        ["COMPAT_SIGNATURE"] = "« {0} » : {1}.",
        ["UPDATE_AVAILABLE"] = "« {0} » — tu as la v{1}, la v{2} est répertoriée sur le Virtual Pinball Spreadsheet. Voir {3}.",
        ["VPS_UNAVAILABLE"] = "Base VPS indisponible (hors ligne ?) — vérification des mises à jour ignorée. Elle se fera à la prochaine connexion.",
        ["VPS_MATCH_SUMMARY"] = "Update Watcher : {0}/{1} tables reconnues dans la base VPS (heuristique, bêta). Mods/variantes non comparés : {2} (ils suivent leur propre versionnage).",
        ["SCANNER_ERROR"] = "Le module « {0} » a échoué : {1}.",
        ["LOW_DISK_SPACE"] = "Espace disque faible sur {0} : {1} Go libres. Visual Pinball peut échouer à charger les textures (« Unable to Create Offscreen Texture ») ou les médias quand le disque est presque plein.",
        ["VPT_LEGACY_PRESENT"] = "{0} table(s) .vpt (Visual Pinball 9, ancien format) présentes. Elles n'apparaissent souvent pas dans PinUP Popper car « .vpt » ne fait pas partie des extensions de l'émulateur VPX.",
        ["PINUP_DISPLAY_ZOMBIE"] = "PinUpDisplay.exe est toujours actif alors qu'aucune table n'est en cours — un reste d'une session précédente. Peut bloquer le lancement de la prochaine table tant qu'il n'est pas fermé.",
        ["DISPLAY_SETUP_INCOMPLETE"] = "Cette installation attend une configuration multi-écrans (un composant backglass ou DMD est présent) mais seul {0} écran est actuellement connecté. Si ton cab tourne normalement avec plus d'écrans, ils sont peut-être en veille, débranchés, ou reconnectés dans le mauvais ordre.",
        ["ORPHANED_MEDIA_FILE"] = "{0} fichier(s) média dans les dossiers de PinUP Popper ne correspondent à aucune table installée — probablement des restes de tables supprimées ou renommées.",
    };

    /// <summary>French fix hints per finding code (English fallback is in the Core Finding.FixHint).</summary>
    private static readonly Dictionary<string, string> FrFixHints = new()
    {
        ["BITNESS_MISMATCH_VPM"] = "Installe et enregistre le VPinMAME 64-bit (VPinMAME64.dll) pour le VPX 64-bit, ou lance le VPX 32-bit pour ces tables.",
        ["BITNESS_MISMATCH_VPM32"] = "Installe et enregistre le VPinMAME 32-bit (VPinMAME.dll) pour le VPX 32-bit, ou lance le VPX 64-bit pour ces tables.",
        ["B2S_SERVER_MISSING"] = "Installe le B2S Backglass Server (il enregistre B2SBackglassServer.dll), garde-le dans ton dossier Tables et enregistre-le en administrateur.",
        ["FLEXDMD_MISSING"] = "Télécharge FlexDMD, place FlexDMD.dll dans ton dossier Visual Pinball et enregistre-le (regsvr32 en administrateur).",
        ["BITNESS_DMD64_MISSING"] = "Télécharge le dmddevice64.dll 64-bit depuis les releases open source de Freezy dmd-extensions et place-le à côté de VPinMAME64.",
        ["BLOCKED_DLL"] = "Clic droit sur le fichier → Propriétés → coche « Débloquer » → OK. Ou en PowerShell : Unblock-File « <chemin> »",
        ["B2S_MISSING"] = "Si tu utilises un écran backglass, télécharge le .directb2s correspondant et place-le dans le dossier des tables avec exactement le même nom de base.",
        ["B2S_ORPHAN"] = "Si ce backglass appartient à une table, renomme-le avec le nom de base exact de la table (identique au .vpx). Sinon, c'est un reste que tu peux supprimer.",
        // Audit 2026-08-04 : manquait — ROM_MISSING est le Critical le plus fréquent (8 occurrences
        // sur le propre scan réel de Maxime), c'était donc le message de fix le plus vu qui
        // retombait silencieusement en anglais pour un utilisateur FR.
        ["ALTCOLOR_INCOMPLETE"] = "Retélécharge le jeu de colorisation de cette ROM et extrais tous les fichiers qu'il contient dans le dossier altcolor correspondant — une extraction partielle (par ex. seulement le .pal, ou seulement le .vni) est la cause la plus fréquente.",
        ["ALTSOUND_SAMPLE_MISSING"] = "Réextrais le pack AltSound de cette ROM dans le dossier altsound correspondant — une extraction partielle est la cause la plus fréquente. Si tu as modifié altsound.csv à la main, vérifie la colonne FNAME par rapport aux fichiers réellement présents.",
        ["DISPLAY_OFFSCREEN"] = "Relance B2S_ScreenResIdentifier (ou ton éditeur ScreenRes) avec tous les écrans branchés dans leur disposition normale du cab, puis resauvegarde — un ScreenRes.txt/.res périmé après un changement d'écran ou de carte graphique en est la cause la plus fréquente.",
        ["BROKEN_JUNCTION"] = "Reconnecte le disque/partage vers lequel pointe le lien, ou recrée la jonction (mklink /J) vers son emplacement correct et actuellement disponible. Supprime-la si le dossier lié a disparu pour de bon.",
        ["B2S_MALFORMED"] = "Retélécharge ou réexporte ce backglass — un téléchargement tronqué ou un export interrompu en est la cause la plus fréquente.",
        ["POPPER_ORPHAN_PLAYLIST"] = "Dans l'outil d'administration de PinUP Popper, rouvre et resauvegarde l'affectation de playlist de chaque jeu concerné (ou retire-la), ou recrée la playlist manquante.",
        ["NVRAM_EMPTY"] = "Supprime le fichier .nv vide et lance la table une fois — VPinMAME le recrée avec les valeurs par défaut. Si tu as une sauvegarde .nv d'avant le problème, restaure-la plutôt pour garder tes meilleurs scores.",
        ["VPMALIAS_LOOP"] = "Ouvre VPMAlias.txt et casse la boucle : le dernier alias de la chaîne doit pointer directement vers le vrai nom de set ROM, pas vers un alias déjà vu.",
        ["ROM_MISSING"] = "Place le fichier .zip de la ROM (nom exact, sans le décompresser) dans le dossier roms de VPinMAME.",
        ["ROM_UNZIPPED"] = "Recompresse le dossier de la ROM en un .zip du même nom dans le dossier roms (ne zippe pas un dossier parent supplémentaire autour).",
        ["POPPER_MEDIA_MISSING"] = "Ajoute une image de wheel nommée exactement comme le jeu (son GameName Popper) sous POPMedia\\<émulateur>\\Wheel, ou relance l'import des médias Popper.",
        ["LOW_DISK_SPACE"] = "Libère de l'espace sur ce disque (anciennes sauvegardes, tables/médias inutilisés) — garde au moins quelques Go de marge.",
        ["VPT_LEGACY_PRESENT"] = "N'ajoute pas « .vpt » à l'émulateur VPX existant — l'auteur de PinUP (NailBuster) le déconseille, ça casse le lancement des .vpt. Crée plutôt une entrée d'émulateur legacy VP9 dédiée.",
        ["PINUP_DISPLAY_ZOMBIE"] = "Ferme PinUpDisplay.exe depuis le Gestionnaire des tâches avant de relancer une table.",
        ["DISPLAY_SETUP_INCOMPLETE"] = "Vérifie le câblage et que la veille des moniteurs est désactivée indépendamment de la veille du PC — cause très fréquente d'écrans qui se reconnectent dans le mauvais ordre après un redémarrage (guides communautaires : Pincab Passion « Changer l'ordre des écrans dans Windows »).",
        ["ORPHANED_MEDIA_FILE"] = "Peut être revu à la main, ou mis en quarantaine avec Repair une fois disponible (déplacé de côté avec sauvegarde, jamais supprimé). Ne supprime pas de média à la main sans vérifier d'abord — des variantes comme « (SCREEN2) »/« (SCREEN3) » peuvent encore être utilisées même si le nom de base semble inconnu.",
        // Rétroactif (comparateur VPX du 05/08, complété 06/08 — R1). Pas de {n} : les deux numéros de
        // version sont déjà dans le message ci-dessus, inutile de les répéter ici.
        ["VPX_VERSION_OUTDATED"] = "Mets à jour Visual Pinball X vers la version requise par la table (indiquée dans le message ci-dessus). Tu peux garder ta version actuelle en parallèle — les builds VPX coexistent, les autres tables ne sont pas affectées.",
    };
}
