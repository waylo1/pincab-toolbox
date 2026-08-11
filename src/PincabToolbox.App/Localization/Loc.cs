using System;
using System.Globalization;
using System.Linq;
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
        ["tab.repair"] = "Repair",
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
        // Bandeau du Scanner (maquette 11/08) — dit le nombre de bloquants plutôt que la seule note.
        ["hero.ok"] = "No blocking problem found",
        ["hero.blocking.one"] = "1 blocking problem is stopping a table from starting",
        ["hero.blocking.many"] = "{0} blocking problems are stopping tables from starting",
        ["priority.label"] = "FIX THIS FIRST",
        ["priority.watch"] = "WORTH A LOOK",
        ["diagnosis.label"] = "MAIN DIAGNOSIS",
        ["diagnosis.confidence"] = "reliability",
        ["priority.basedon"] = "Based on:",
        ["detail.impact"] = "IMPACT",
        ["detail.cause"] = "PROBABLE CAUSE",
        ["detail.fix"] = "RECOMMENDED FIX",
        // Écran 1 (UX-COPY-Repair.md) — facts computed from the real plan by RepairOfferBuilder,
        // never declared.
        ["repair.checks.fixable"] = "✓ Fixable automatically",
        ["repair.checks.backup"] = "✓ Backed up before changing",
        ["repair.checks.reversible"] = "✓ Reversible — one click to undo",
        ["repair.checks.duration.seconds"] = "⏱ A few seconds",
        ["repair.checks.duration.underminute"] = "⏱ Under a minute",
        ["repair.checks.duration.minutes"] = "⏱ A few minutes",
        ["repair.tag"] = "→ See the Repair tab to apply this fix",
        ["repair.summary"] = "Repair could fix {0} of the {1} findings here automatically — see the Repair tab.",
        ["repair.notautomatable"] = "Some steps will always stay manual, licence or not:",
        ["repair.goto"] = "Go to the Repair tab →",

        // Écran 2 (LOT H, spec 10/08) — the write path itself.
        ["repair.intro"] = "Repair can fix some of the findings above automatically: every change is backed up first and can be undone, and nothing is ever applied without your explicit, per-item confirmation. Enter your license key, build the plan, review it, then choose what to apply.",
        ["repair.license.label"] = "License key",
        ["repair.license.hint"] = "Paste the key you received after purchase.",
        ["repair.license.verify"] = "Verify",
        ["repair.license.valid"] = "✓ Valid license.",
        ["repair.license.invalid"] = "Invalid or missing license key — Repair will only show what could be fixed, not apply it.",
        ["repair.forceddryrun.banner"] = "⚠ SIMULATION MODE — PINCAB_REPAIR_FORCE_DRYRUN is set. Apply will report what it would have done, but will change nothing on disk.",
        ["repair.forceddryrun.applied"] = "Simulation only — nothing was written to disk.",
        ["repair.plan.build"] = "Analyze what can be repaired",
        ["repair.plan.status"] = "{0} fixable item(s) found. Review and select what to apply below.",
        ["repair.plan.empty"] = "Nothing to apply right now — either everything is already fine, every fix needs a license, or the remaining steps stay manual.",
        ["repair.needscan"] = "Run a scan first, from the Scanner tab.",
        ["repair.noneselected"] = "Nothing selected — check at least one item before applying.",
        ["repair.reversible.yes"] = "Reversible",
        ["repair.reversible.no"] = "Cannot be undone",
        ["repair.backup.yes"] = "Backed up first",
        ["repair.backup.no"] = "No backup (nothing to restore)",
        ["repair.confirm.title"] = "This cannot be undone",
        ["repair.confirm.nonreversible"] = "At least one selected fix cannot be undone once applied. Do you want to continue?",
        ["repair.apply.button"] = "Apply selected fixes",
        ["repair.apply.running"] = "Applying…",
        ["repair.apply.status"] = "{0} applied, {1} failed.",
        ["repair.apply.recovery"] = "Something went wrong while undoing a partial change — a backup is kept here, restore it by hand if needed:",
        ["repair.undo.label"] = "Undo history",
        ["repair.undo.button"] = "Undo selected plan",
        ["repair.undo.ok"] = "Undone.",
        ["repair.undo.fail"] = "Could not fully undo:",
        ["repair.undo.noneselected"] = "Select a plan from the list first.",
        ["repair.undo.journalwarning"] = "⚠ The last write to the undo journal failed — Undo may be incomplete for the most recent action.",
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
        ["cat.legacy"] = "Legacy Tables",
        ["cat.disk"] = "Disk Space",
        ["cat.process"] = "PinUP Display",
        ["cat.display"] = "Display Setup",
        ["cat.media-orphan"] = "Orphaned Media",
        ["cat.vpxversion"] = "VPX Version",
        // Lot communauté 10/08 (LOT A→G).
        ["cat.com"] = "COM Registration",
        ["cat.chain-bitness"] = "Chain Bitness",
        ["cat.dmd-config"] = "DMD Config",
        ["cat.feature-enabled"] = "Feature Enabled",
        ["cat.screenres-format"] = "ScreenRes Format",
        ["cat.nvram-writable"] = "NVRAM Writability",
        ["about.tagline"] = "The mechanic for your pincab.",
        ["about.body"] = "Pincab Toolbox scans your Visual Pinball X / PinUP Popper installation and tells you what is broken, missing or mismatched — before you hit Start on a table.\n\n• 100% local scanning — your cab, your files and your findings are never uploaded, no telemetry, no account.\n• Read-only — the free scanner never modifies a single file.\n• The Update Watcher uses the open-source Virtual Pinball Spreadsheet database and only ever links you to official pages. It never downloads tables, ROMs or media.\n• The one exception: the \"Check for updates\" button below is manual and opt-in — click it, and it contacts GitHub just to see if a newer version exists. Nothing about your cab, your tables or your scan results is ever sent. It never runs on its own.",
        ["about.roadmap"] = "Repair is here: optional one-click fixes for some of what the scanner finds, always with a backup first, a preview of every change, and one-click undo. It needs a license key (Repair tab) — the free scanner itself always stays read-only, license or not.",
        ["about.version"] = "Version",
        ["about.checkupdate"] = "Check for updates",
        ["about.update.checking"] = "Checking…",
        ["about.update.uptodate"] = "You're up to date ({0}).",
        ["about.update.available"] = "New version {0} available — click to open the release page.",
        ["about.update.error"] = "Couldn't check for updates (offline, or GitHub unreachable). Try again later.",
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
        ["tab.repair"] = "Repair",
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
        // Bandeau du Scanner (maquette 11/08) — dit le nombre de bloquants plutôt que la seule note.
        ["hero.ok"] = "Aucun problème bloquant détecté",
        ["hero.blocking.one"] = "1 problème bloquant empêche une table de démarrer",
        ["hero.blocking.many"] = "{0} problèmes bloquants empêchent des tables de démarrer",
        ["priority.label"] = "À CORRIGER EN PRIORITÉ",
        ["priority.watch"] = "À VÉRIFIER",
        ["diagnosis.label"] = "DIAGNOSTIC PRINCIPAL",
        ["diagnosis.confidence"] = "fiabilité",
        ["priority.basedon"] = "Basé sur :",
        ["detail.impact"] = "IMPACT",
        ["detail.cause"] = "CAUSE PROBABLE",
        ["detail.fix"] = "CORRECTIF RECOMMANDÉ",
        // Écran 1 (UX-COPY-Repair.md) — faits calculés depuis le plan réel par RepairOfferBuilder,
        // jamais déclarés.
        ["repair.checks.fixable"] = "✓ Réparable automatiquement",
        ["repair.checks.backup"] = "✓ Sauvegarde avant modification",
        ["repair.checks.reversible"] = "✓ Réversible — annulable en un clic",
        ["repair.checks.duration.seconds"] = "⏱ Quelques secondes",
        ["repair.checks.duration.underminute"] = "⏱ Moins d'une minute",
        ["repair.checks.duration.minutes"] = "⏱ Quelques minutes",
        ["repair.tag"] = "→ Voir l'onglet Repair pour appliquer ce correctif",
        ["repair.summary"] = "Repair pourrait corriger {0} problème(s) sur {1} détecté(s) automatiquement — voir l'onglet Repair.",
        ["repair.notautomatable"] = "Certaines étapes resteront toujours manuelles, licence ou pas :",
        ["repair.goto"] = "Aller à l'onglet Repair →",

        // Écran 2 (LOT H, spec 10/08) — le chemin d'écriture lui-même.
        ["repair.intro"] = "Repair peut corriger automatiquement certains résultats ci-dessus : chaque modification est sauvegardée avant d'être appliquée et peut être annulée, et rien n'est jamais appliqué sans ta confirmation explicite, poste par poste. Entre ta clé de licence, construis le plan, vérifie-le, puis choisis ce que tu veux appliquer.",
        ["repair.license.label"] = "Clé de licence",
        ["repair.license.hint"] = "Colle la clé reçue après ton achat.",
        ["repair.license.verify"] = "Vérifier",
        ["repair.license.valid"] = "✓ Licence valide.",
        ["repair.license.invalid"] = "Clé de licence absente ou invalide, Repair affichera seulement ce qui pourrait être corrigé, sans l'appliquer.",
        ["repair.forceddryrun.banner"] = "⚠ MODE SIMULATION — PINCAB_REPAIR_FORCE_DRYRUN est actif. Apply annoncera ce qu'il aurait fait, mais rien ne sera écrit sur le disque.",
        ["repair.forceddryrun.applied"] = "Simulation uniquement, rien n'a été écrit sur le disque.",
        ["repair.plan.build"] = "Analyser ce qui peut être réparé",
        ["repair.plan.status"] = "{0} élément(s) réparable(s) trouvé(s). Vérifie et sélectionne ce que tu veux appliquer ci-dessous.",
        ["repair.plan.empty"] = "Rien à appliquer pour l'instant, soit tout va déjà bien, soit chaque correctif nécessite une licence, soit les étapes restantes restent manuelles.",
        ["repair.needscan"] = "Lance d'abord un scan depuis l'onglet Scanner.",
        ["repair.noneselected"] = "Rien n'est sélectionné, coche au moins un élément avant d'appliquer.",
        ["repair.reversible.yes"] = "Réversible",
        ["repair.reversible.no"] = "Non annulable",
        ["repair.backup.yes"] = "Sauvegardé avant modification",
        ["repair.backup.no"] = "Pas de sauvegarde (rien à restaurer)",
        ["repair.confirm.title"] = "Cette action est irréversible",
        ["repair.confirm.nonreversible"] = "Au moins un correctif sélectionné ne pourra pas être annulé une fois appliqué. Veux-tu continuer ?",
        ["repair.apply.button"] = "Appliquer les correctifs sélectionnés",
        ["repair.apply.running"] = "Application en cours…",
        ["repair.apply.status"] = "{0} appliqué(s), {1} échoué(s).",
        ["repair.apply.recovery"] = "Un problème est survenu en annulant une modification partielle, une sauvegarde est conservée ici, restaure-la à la main si besoin :",
        ["repair.undo.label"] = "Historique d'annulation",
        ["repair.undo.button"] = "Annuler le plan sélectionné",
        ["repair.undo.ok"] = "Annulé.",
        ["repair.undo.fail"] = "Annulation incomplète :",
        ["repair.undo.noneselected"] = "Sélectionne d'abord un plan dans la liste.",
        ["repair.undo.journalwarning"] = "⚠ La dernière écriture du journal d'annulation a échoué, Undo pourrait être incomplet pour l'action la plus récente.",
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
        ["cat.legacy"] = "Tables anciennes",
        ["cat.disk"] = "Espace disque",
        ["cat.process"] = "PinUP Display",
        ["cat.display"] = "Configuration écrans",
        ["cat.media-orphan"] = "Médias orphelins",
        ["cat.vpxversion"] = "Version VPX",
        // Lot communauté 10/08 (LOT A→G).
        ["cat.com"] = "Enregistrement COM",
        ["cat.chain-bitness"] = "Chaîne 32/64-bit",
        ["cat.dmd-config"] = "Config DMD",
        ["cat.feature-enabled"] = "Fonction activée",
        ["cat.screenres-format"] = "Format ScreenRes",
        ["cat.nvram-writable"] = "Écriture NVRAM",
        ["about.tagline"] = "Le mécanicien de ton pincab.",
        ["about.body"] = "Pincab Toolbox scanne ton installation Visual Pinball X / PinUP Popper et te dit ce qui est cassé, manquant ou incompatible — avant que tu ne lances une table.\n\n• Scan 100 % local — ta cab, tes fichiers et tes résultats de scan ne sont jamais envoyés, zéro télémétrie, zéro compte.\n• Lecture seule — le scanner gratuit ne modifie jamais le moindre fichier.\n• L'Update Watcher s'appuie sur la base open source Virtual Pinball Spreadsheet et se contente de te donner le lien officiel. Il ne télécharge jamais ni table, ni ROM, ni média.\n• Seule exception : le bouton « Vérifier les mises à jour » ci-dessous est manuel et volontaire — tu cliques, et il contacte GitHub juste pour voir si une nouvelle version existe. Rien concernant ta cab, tes tables ou tes résultats de scan n'est jamais envoyé. Il ne se déclenche jamais tout seul.",
        ["about.roadmap"] = "Bientôt : Repair — des réparations optionnelles en un clic pour une partie de ce que le scanner trouve, toujours avec sauvegarde avant, aperçu de chaque changement et annulation. Le scanner gratuit reste toujours en lecture seule. Suis le fil du forum pour être prévenu.",
        ["about.version"] = "Version",
        ["about.checkupdate"] = "Vérifier les mises à jour",
        ["about.update.checking"] = "Vérification…",
        ["about.update.uptodate"] = "Tu es à jour ({0}).",
        ["about.update.available"] = "Nouvelle version {0} disponible — clique pour ouvrir la page de la release.",
        ["about.update.error"] = "Impossible de vérifier (hors ligne, ou GitHub inaccessible). Réessaie plus tard.",
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
        ["BLOCKED_NONE"] = "Aucun fichier bloqué par Windows détecté.",
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
        // ── Tier B (handoff Sonnet 5, 06/08) — tous Severity.Note (ADR-010 Doctrine).
        // {0} = nom du périphérique de lecture par défaut.
        ["AUDIO_DEFAULT_SUSPECT"] = "Le périphérique de lecture Windows par défaut est actuellement « {0} » — son nom suggère une sortie audio écran/HDMI plutôt que des enceintes dédiées. C'est un point connu où Windows réinitialise silencieusement le défaut au démarrage ; vérifie que c'est bien voulu.",
        // {0} = pourcentage d'échelle (ex. "125", sans le signe %, déjà ajouté dans le gabarit).
        ["DPI_SCALING_NONSTANDARD"] = "La mise à l'échelle d'affichage Windows pour cet utilisateur est réglée à {0} % au lieu de 100 %. C'est une cause connue de fenêtre backglass ou table qui s'affiche tronquée ou décalée sur certains cabs — vérifie ton affichage si tu remarques ça.",
        // {0} = section du pilote (pin2dmd/zedmd/pindmd3), {1} = port COM configuré.
        ["DMD_COM_PORT_NOT_FOUND"] = "dmddevice.ini active « {0} » sur {1}, mais Windows ne liste pas {1} comme actif actuellement. Si ce DMD est connecté et sous tension, ce cas est connu pour causer un gel de plusieurs secondes au lancement, le temps que le pilote l'attende.",
        // {0} = séparateur décimal détecté (ex. ",").
        ["LOCALE_DECIMAL_SEPARATOR"] = "Le séparateur décimal de cet utilisateur Windows est « {0} » au lieu de « . ». Certains scripts de table VPX et analyses de physique/configuration supposent un point, et peuvent mal se comporter avec un séparateur virgule — un point de friction connu pour les installations Windows en français.",
        // Pas d'argument — le constat ne dépend d'aucune valeur variable.
        ["VPINMAME_CONFIG_PHANTOM"] = "Une configuration VPinMAME dans le registre (HKCU\\Software\\Freeware\\Visual PinMame) ET un fichier VPinMAME.ini ont été trouvés. VPinMAME peut être configuré via l'un ou l'autre — si tu modifies l'un sans voir les changements s'appliquer, tu modifies peut-être celui qui n'est pas actuellement utilisé.",

        // ── Lot communauté 10/08 — LOT A (COM Registration Health). {0} = ProgID.
        ["COM_NOT_REGISTERED"] = "« {0} » n'est enregistré dans aucune des deux vues du registre COM (32 et 64-bit), alors que le composant correspondant est présent dans cette installation et qu'au moins une table en a besoin — ça va échouer avec une erreur du type « ActiveX component can't create object » ou « Library not registered (Exception from HRESULT: 0x8002801D) ».",
        // {0} = ProgID, {1} = chemin enregistré.
        ["COM_STALE_PATH"] = "« {0} » est enregistré mais pointe vers « {1} », qui n'existe plus — un reste d'enregistrement d'une installation précédente. Le chargement échouera.",
        ["COM_PATH_OUTSIDE_INSTALL"] = "« {0} » est enregistré, mais vers une copie en dehors de cette installation (« {1} ») — cette installation a aussi sa propre copie du composant. Les tables lancées ici chargeront en réalité la copie enregistrée (l'autre).",
        ["COM_OK"] = "« {0} » est enregistré et pointe à l'intérieur de cette installation.",
        // {0} = ProgID, {1} = architecture manquante ("32-bit"/"64-bit").
        ["COM_BITNESS_GAP"] = "Un Visual Pinball {1} est installé mais « {0} » n'est enregistré que dans l'AUTRE architecture — le processus {1} ne peut pas l'utiliser. C'est le classique problème « le 32-bit et le 64-bit sont deux écosystèmes différents ».",
        // Pas d'argument — les 4 conditions sont déterministes, le constat ne dépend d'aucune valeur variable.
        ["VPINMAME_NOT_REGISTERED"] = "VPinMAME.dll est présent mais VPinMAME.Controller n'est enregistré dans aucune des deux vues du registre COM (32 ou 64-bit), alors qu'au moins une table en a besoin — toutes les tables à ROM vont échouer au démarrage (« ActiveX component can't create object » / « Library not registered »). C'est presque toujours causé par une installation VPX copiée à la main sans jamais avoir lancé le Setup.exe de VPinMAME.",

        // ── LOT B (Chain Bitness Doctor). {0} = nom du composant (B2S/FlexDMD), {1} = architecture ("32-bit"/"64-bit").
        ["CHAIN_BITNESS_GAP"] = "Un Visual Pinball {1} est installé et au moins une table a besoin de {0}, mais aucun binaire {0} en {1} n'a été trouvé sous cette installation — le chargement échouera depuis le processus {1}.",

        // ── LOT C (dmddevice.ini Config Doctor). Pas d'argument.
        ["DMD_VIRTUAL_DISABLED"] = "dmddevice.ini a le DMD virtuel désactivé (« [virtualdmd] enabled = false »), et aucun pilote de DMD matériel n'est activé non plus dans le même fichier. Si tu n'as pas de DMD physique, ton DMD va simplement disparaître sans message d'erreur — une mise à jour de Freezy est connue pour réinitialiser cette valeur toute seule.",
        // {0}=left, {1}=top, {2}=width, {3}=height.
        ["DMD_POSITION_OFFSCREEN"] = "dmddevice.ini positionne le DMD virtuel à ({0},{1}) avec une taille de {2}x{3}, ce qui tombe entièrement en dehors de tous les écrans connectés — il ne sera jamais visible, même si dmddevice.ini se charge sans erreur.",

        // ── LOT D (Feature Enabled Doctor). {0} = nom de la ROM.
        ["ALTSOUND_PRESENT_NOT_ENABLED"] = "« {0} » a un pack AltSound installé sous altsound/{0}/, mais le mode Alt Sound de VPinMAME est réglé sur 0 (désactivé) pour cette ROM — le pack est présent mais silencieux.",
        ["ALTCOLOR_PRESENT_NOT_ENABLED"] = "« {0} » a un jeu de colorisation AltColor/Serum complet installé sous altcolor/{0}/, mais la colorisation DMD de VPinMAME est désactivée pour cette ROM — le DMD s'affichera en mono.",

        // ── LOT F (ScreenRes.txt Format Honesty). {0} = nom du fichier concerné (ScreenRes.txt ou nom de table).
        ["SCREENRES_UNPARSED"] = "« {0} » est présent mais pas dans un format que cet outil sait vérifier (pas de marqueur « # V2 », ou une structure non reconnue) — sa position de backglass/DMD n'est pas vérifiée ; ceci n'est pas une affirmation qu'il y a un problème.",

        // ── LOT G (NVRAM Folder Writability). Pas d'argument.
        ["NVRAM_FOLDER_NOT_WRITABLE"] = "Le dossier nvram de VPinMAME existe mais un vrai test d'écriture a échoué — les meilleurs scores et réglages par table vont échouer à s'enregistrer silencieusement, table après table, sans aucune erreur affichée.",
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
        // Tier B (handoff Sonnet 5, 06/08).
        ["AUDIO_DEFAULT_SUSPECT"] = "Si ce n'est pas la sortie audio que tu veux, redéfinis le périphérique de lecture par défaut sur tes enceintes dans les paramètres Son de Windows.",
        ["DPI_SCALING_NONSTANDARD"] = "Dans les paramètres d'affichage Windows, remets « Échelle » à 100 % pour les écrans du cab, ou vérifie que Visual Pinball / B2S tournent en mode compatible DPI si tu gardes une échelle supérieure à 100 %.",
        ["DMD_COM_PORT_NOT_FOUND"] = "Vérifie que le DMD est sous tension et que sa connexion USB/série est branchée, ou mets à jour dmddevice.ini si le port COM a changé.",
        ["LOCALE_DECIMAL_SEPARATOR"] = "Dans les paramètres régionaux Windows, tu peux régler le « symbole décimal » sur « . » dans le format de nombre avancé — certains propriétaires de pincab font tourner leur compte cab en format numérique anglais (États-Unis) spécifiquement pour éviter ce genre de souci.",
        ["VPINMAME_CONFIG_PHANTOM"] = "Si tu t'appuies sur VPinMAME.ini, vérifie que ses réglages s'appliquent réellement ; sinon, envisage de le supprimer pour éviter l'ambiguïté et garder le registre comme source unique.",

        // ── Lot communauté 10/08 — LOT A→G.
        ["COM_NOT_REGISTERED"] = "Lance l'outil d'enregistrement du composant (son Setup.exe / son application d'enregistrement / regsvr32) en tant qu'administrateur.",
        ["COM_STALE_PATH"] = "Relance l'outil d'enregistrement du composant depuis son emplacement ACTUEL pour écraser l'enregistrement périmé.",
        ["COM_PATH_OUTSIDE_INSTALL"] = "Si tu voulais utiliser la copie de CETTE installation, relance son outil d'enregistrement depuis ici.",
        ["COM_BITNESS_GAP"] = "Enregistre la version du composant qui correspond à l'architecture manquante, ou lance la version de VPX dont l'architecture EST déjà enregistrée.",
        ["VPINMAME_NOT_REGISTERED"] = "Lance le Setup.exe de VPinMAME (dans le dossier VPinMAME) en tant qu'administrateur — il enregistre le composant COM. C'est le correctif le plus courant pour « aucune table à ROM ne démarre ».",
        ["CHAIN_BITNESS_GAP"] = "Installe la version manquante du composant dans l'architecture concernée, à côté du Visual Pinball correspondant.",
        ["DMD_VIRTUAL_DISABLED"] = "Si tu n'as pas de DMD physique, mets « enabled = true » sous [virtualdmd] dans dmddevice.ini.",
        ["DMD_POSITION_OFFSCREEN"] = "Réinitialise les valeurs left/top/width/height de [virtualdmd] dans dmddevice.ini (ou supprime-les pour revenir aux valeurs par défaut) avec tous les écrans branchés dans leur disposition normale du cab.",
        ["ALTSOUND_PRESENT_NOT_ENABLED"] = "Dans les options par jeu de VPinMAME (menu F1, ou l'interface de configuration VPinMAME), change le mode son de 0/Original pour utiliser le pack AltSound installé.",
        ["ALTCOLOR_PRESENT_NOT_ENABLED"] = "Dans les options par jeu de VPinMAME, active la colorisation DMD (« Colorize DMD » / couleurs DMD externes) pour utiliser le jeu installé.",
        ["SCREENRES_UNPARSED"] = "Si la position de ton backglass semble fausse, relance B2S_ScreenResIdentifier (ou ton éditeur ScreenRes) pour régénérer le fichier dans le format actuel.",
        ["NVRAM_FOLDER_NOT_WRITABLE"] = "Vérifie que le dossier nvram n'est pas en lecture seule et que ton compte utilisateur Windows a le droit d'écriture dessus (clic droit → Propriétés → Sécurité).",
    };
}
