using PincabToolbox.App.Localization;

namespace PincabToolbox.App;

/// <summary>
/// One knowledge entry for a finding code: the "why it matters" and "why it happens" that turn
/// a raw finding into an explanation. Deliberately does NOT hold the finding message (that stays
/// in Loc.FindingText) nor the fix hint (Loc.FixHintText) — only the added expert context.
/// </summary>
public sealed record KnowledgeEntry
{
    public string? ImpactEn { get; init; }
    public string? ImpactFr { get; init; }
    public string? CauseEn { get; init; }
    public string? CauseFr { get; init; }
    public string[] Refs { get; init; } = System.Array.Empty<string>();

    /// <summary>
    /// True when the future Repair module can fix this locally, safely and reversibly
    /// (no download required). Drives the "one-click fixable (Repair)" tag — the free/paid
    /// boundary lives here, in the data, not in the UI.
    /// </summary>
    public bool AutoFixable { get; init; }
}

/// <summary>
/// The knowledge layer, keyed by Finding.Code. Today an in-code table; designed to be swapped
/// for an externally-updatable JSON "Knowledge Pack" later without touching callers. Unknown
/// codes return null so the UI degrades gracefully to message + fix only.
/// </summary>
public static class Knowledge
{
    public static KnowledgeEntry? For(string? code) =>
        code is not null && Table.TryGetValue(code, out var e) ? e : null;

    public static string? Impact(string? code)
    {
        var e = For(code);
        if (e is null) return null;
        return Loc.Lang == "fr" ? (e.ImpactFr ?? e.ImpactEn) : (e.ImpactEn ?? e.ImpactFr);
    }

    public static string? Cause(string? code)
    {
        var e = For(code);
        if (e is null) return null;
        return Loc.Lang == "fr" ? (e.CauseFr ?? e.CauseEn) : (e.CauseEn ?? e.CauseFr);
    }

    /// <summary>Whether the future Repair module can fix this finding automatically.</summary>
    public static bool IsAutoFixable(string? code) => For(code)?.AutoFixable ?? false;

    private static readonly Dictionary<string, KnowledgeEntry> Table = new()
    {
        ["ROM_MISSING"] = new()
        {
            ImpactEn = "The table won't boot — VPinMAME can't find the ROM it needs to emulate the game.",
            ImpactFr = "La table ne démarrera pas — VPinMAME ne trouve pas la ROM nécessaire pour émuler le jeu.",
            CauseEn = "The ROM zip is absent from the VPinMAME roms folder, misnamed, or was never downloaded.",
            CauseFr = "Le zip de la ROM est absent du dossier roms de VPinMAME, mal nommé, ou n'a jamais été téléchargé.",
        },
        ["BITNESS_MISMATCH_VPM"] = new()
        {
            ImpactEn = "Every ROM-based table will fail to start: 64-bit VPX cannot load the 32-bit VPinMAME COM server.",
            ImpactFr = "Toutes les tables à ROM échoueront au démarrage : le VPX 64-bit ne peut pas charger le serveur COM VPinMAME 32-bit.",
            CauseEn = "Visual Pinball was updated to 64-bit but VPinMAME was left in its 32-bit version (or the 64-bit VPinMAME was never registered).",
            CauseFr = "Visual Pinball est passé en 64-bit mais VPinMAME est resté en 32-bit (ou le VPinMAME 64-bit n'a jamais été enregistré).",
        },
        ["BITNESS_DMD64_MISSING"] = new()
        {
            ImpactEn = "External DMDs won't display from 64-bit VPX — the 64-bit renderer is missing.",
            ImpactFr = "Les DMD externes ne s'afficheront pas depuis le VPX 64-bit — le moteur de rendu 64-bit est absent.",
            CauseEn = "The install was migrated to 64-bit but dmddevice64.dll (Freezy dmd-extensions) was not added next to the 64-bit VPinMAME.",
            CauseFr = "L'install est passée en 64-bit mais dmddevice64.dll (Freezy dmd-extensions) n'a pas été ajouté à côté du VPinMAME 64-bit.",
        },
        ["BLOCKED_DLL"] = new()
        {
            ImpactEn = "Windows silently blocks the DLL from loading, so the plugin it belongs to (VPinMAME, B2S, DMD…) fails without a clear error.",
            ImpactFr = "Windows empêche silencieusement le chargement de la DLL, donc le plugin concerné (VPinMAME, B2S, DMD…) échoue sans erreur claire.",
            CauseEn = "The file was extracted from a downloaded ZIP; Windows attaches a 'Mark of the Web' that blocks it until unblocked.",
            CauseFr = "Le fichier a été extrait d'un ZIP téléchargé ; Windows y attache une « Mark of the Web » qui le bloque tant qu'il n'est pas débloqué.",
            AutoFixable = true,
        },
        ["B2S_MISSING"] = new()
        {
            ImpactEn = "No backglass will show for this table on the backglass screen.",
            ImpactFr = "Aucun backglass ne s'affichera pour cette table sur l'écran backglass.",
            CauseEn = "The .directb2s file is missing next to the table, or is named differently from the .vpx.",
            CauseFr = "Le fichier .directb2s est absent à côté de la table, ou porte un nom différent du .vpx.",
        },
        ["POPPER_NOT_REGISTERED"] = new()
        {
            ImpactEn = "The table is on disk but won't appear in the PinUP Popper frontend menu.",
            ImpactFr = "La table est sur le disque mais n'apparaîtra pas dans le menu du frontend PinUP Popper.",
            CauseEn = "It was added to the tables folder but never imported/registered in the Popper database.",
            CauseFr = "Elle a été ajoutée au dossier des tables mais jamais importée/enregistrée dans la base Popper.",
            AutoFixable = false, // Repair v1 ne reecrit pas la base Popper (SQLite, ADR-007) -- pas de reparation un-clic promise
        },
        // Trois avertissements affichés à l'utilisateur qui n'avaient aucune explication :
        // un warning qu'on ne peut pas comprendre est un warning sur lequel on ne peut pas agir.
        // (FIELD-LOG 2026-08-03, audit de clôture du scanner.)
        ["COMPAT_SIGNATURE"] = new()
        {
            ImpactEn = "A known problem pattern was matched inside the table script. Depending on the pattern this ranges from a cosmetic quirk to a table that will not start — the message itself says which.",
            ImpactFr = "Un motif de problème connu a été reconnu dans le script de la table. Selon le motif, ça va du détail cosmétique à une table qui ne démarre pas — le message lui-même précise lequel.",
            CauseEn = "The signature list in the ecosystem profile (profiles/vpx-popper.json) matched a line of this table's script. Signatures come from community troubleshooting threads, so a match means somebody has already hit this exact pattern.",
            CauseFr = "La liste de signatures du profil d'écosystème (profiles/vpx-popper.json) a reconnu une ligne du script de cette table. Les signatures viennent de fils de dépannage communautaires : une correspondance veut dire que quelqu'un a déjà rencontré exactement ce motif.",
        },
        ["LOW_DISK_SPACE"] = new()
        {
            ImpactEn = "Visual Pinball allocates large textures and streams video at launch. On a nearly full disk this fails in ways that look like a broken table rather than a full drive — \"Unable to Create Offscreen Texture\", missing backglass video, a table that hangs on load.",
            ImpactFr = "Visual Pinball alloue de grosses textures et lit des vidéos au lancement. Sur un disque presque plein, ça échoue d'une manière qui ressemble à une table cassée plutôt qu'à un disque plein — « Unable to Create Offscreen Texture », vidéo de backglass manquante, table qui se fige au chargement.",
            CauseEn = "A pincab fills up quietly: tables, backglasses, PUP packs and media weigh tens of gigabytes, and Popper's media cache grows on its own. The drive holding the install has dropped below a comfortable margin.",
            CauseFr = "Un pincab se remplit sans bruit : tables, backglasses, PUP packs et médias pèsent des dizaines de Go, et le cache média de Popper grossit tout seul. Le disque qui porte l'installation est passé sous une marge confortable.",
            // Pas de règle de réparation, et c'est volontaire : on ne supprime pas des fichiers
            // à la place de l'utilisateur pour libérer de la place.
        },
        ["SCANNER_ERROR"] = new()
        {
            ImpactEn = "One check could not finish, so its part of the diagnosis is missing. Everything else in this report is still valid — but treat the missing module as \"unknown\", not as \"fine\".",
            ImpactFr = "Un contrôle n'a pas pu aller au bout : sa partie du diagnostic manque. Tout le reste du rapport reste valable — mais considère le module absent comme « inconnu », pas comme « bon ».",
            CauseEn = "Usually a permissions problem, a path that disappeared mid-scan, or a file the module could not read. The scanner isolates each module on purpose so one failure never takes the whole scan down with it.",
            CauseFr = "Le plus souvent un souci de permissions, un chemin disparu en cours de scan, ou un fichier illisible par le module. Le scanner isole chaque module exprès pour qu'un échec n'emporte jamais tout le scan avec lui.",
        },
        ["COMPAT_MIN_VERSION"] = new()
        {
            ImpactEn = "The table may render incorrectly — or not launch — on an older Visual Pinball version.",
            ImpactFr = "La table peut mal s'afficher — voire ne pas se lancer — sur une version de Visual Pinball plus ancienne.",
            CauseEn = "The table script declares a minimum VPX version newer than the one that may be installed.",
            CauseFr = "Le script de la table déclare une version VPX minimale plus récente que celle potentiellement installée.",
        },
        ["UPDATE_AVAILABLE"] = new()
        {
            ImpactEn = "You may be missing fixes or improvements shipped in a newer release of this table.",
            ImpactFr = "Tu passes peut-être à côté de correctifs ou d'améliorations d'une version plus récente de cette table.",
            CauseEn = "A newer version is listed on the Virtual Pinball Spreadsheet than the one detected on disk.",
            CauseFr = "Une version plus récente est répertoriée sur le Virtual Pinball Spreadsheet que celle détectée sur le disque.",
        },
        ["BITNESS_MISMATCH_VPM32"] = new()
        {
            ImpactEn = "Every ROM-based table will fail to start: 32-bit VPX cannot load the 64-bit VPinMAME COM server.",
            ImpactFr = "Toutes les tables à ROM échoueront au démarrage : le VPX 32-bit ne peut pas charger le serveur COM VPinMAME 64-bit.",
            CauseEn = "A 64-bit VPinMAME was registered but the executable in use is still the 32-bit Visual Pinball (or the 32-bit VPinMAME was removed).",
            CauseFr = "Un VPinMAME 64-bit a été enregistré mais l'exécutable utilisé est toujours le Visual Pinball 32-bit (ou le VPinMAME 32-bit a été supprimé).",
        },
        ["ROM_UNZIPPED"] = new()
        {
            ImpactEn = "The table won't boot even though the ROM is there: VPinMAME only loads ROMs from .zip archives, not extracted folders.",
            ImpactFr = "La table ne démarrera pas alors que la ROM est là : VPinMAME ne charge les ROMs que depuis des archives .zip, pas des dossiers décompressés.",
            CauseEn = "The ROM zip was extracted into a folder in the roms directory — a common mistake when unpacking downloads.",
            CauseFr = "Le zip de la ROM a été décompressé en dossier dans le répertoire roms — une erreur fréquente en dézippant les téléchargements.",
            AutoFixable = true,
        },
        ["POPPER_MEDIA_MISSING"] = new()
        {
            ImpactEn = "These games look blank in the PinUP Popper wheel — no wheel image is shown for them.",
            ImpactFr = "Ces jeux apparaissent vides dans la roue de PinUP Popper — aucune image de wheel ne s'affiche pour eux.",
            CauseEn = "No wheel image named after the game was found under POPMedia — media was never imported for them.",
            CauseFr = "Aucune image de wheel au nom du jeu n'a été trouvée sous POPMedia — les médias n'ont jamais été importés pour eux.",
        },
        ["B2S_ORPHAN"] = new()
        {
            ImpactEn = "This backglass never displays: B2S matches files to tables by exact base name, and nothing matches this one.",
            ImpactFr = "Ce backglass ne s'affiche jamais : B2S associe les fichiers aux tables par nom de base exact, et aucun ne correspond à celui-ci.",
            CauseEn = "The .directb2s is misnamed (a typo or a different name than the .vpx), or it's a leftover from a table you removed.",
            CauseFr = "Le .directb2s est mal nommé (une faute ou un nom différent du .vpx), ou c'est un reste d'une table que tu as supprimée.",
        },
        ["B2S_SERVER_MISSING"] = new()
        {
            ImpactEn = "No backglass will display for any table: the B2S Backglass Server that renders .directb2s files isn't installed.",
            ImpactFr = "Aucun backglass ne s'affichera pour aucune table : le B2S Backglass Server qui affiche les fichiers .directb2s n'est pas installé.",
            CauseEn = "Backglass files were copied in, but the B2S Backglass Server (B2SBackglassServer.dll) was never installed and registered.",
            CauseFr = "Les fichiers backglass ont été copiés, mais le B2S Backglass Server (B2SBackglassServer.dll) n'a jamais été installé et enregistré.",
        },
        ["FLEXDMD_MISSING"] = new()
        {
            ImpactEn = "Tables that use FlexDMD will run without their DMD/score display — or throw a script error on launch.",
            ImpactFr = "Les tables qui utilisent FlexDMD tourneront sans leur affichage DMD/score — ou déclencheront une erreur de script au lancement.",
            CauseEn = "One or more scripts create a FlexDMD object, but FlexDMD.dll is not installed and registered on this machine.",
            CauseFr = "Un ou plusieurs scripts créent un objet FlexDMD, mais FlexDMD.dll n'est pas installé et enregistré sur cette machine.",
        },
        ["BITNESS_HYBRID_INSTALL"] = new()
        {
            ImpactEn = "It works, but it's fragile: every plugin (DMD, B2S, FlexDMD) must exist in BOTH 32- and 64-bit, or some tables will break.",
            ImpactFr = "Ça fonctionne, mais c'est fragile : chaque plugin (DMD, B2S, FlexDMD) doit exister en 32 ET en 64-bit, sinon certaines tables casseront.",
            CauseEn = "Both 32-bit and 64-bit Visual Pinball executables are installed side by side.",
            CauseFr = "Des exécutables Visual Pinball 32-bit ET 64-bit sont installés côte à côte.",
        },
        ["SCRIPT_UNREADABLE"] = new()
        {
            ImpactEn = "This table is skipped from the ROM and physics checks — the scanner couldn't read its script.",
            ImpactFr = "Cette table est ignorée des vérifications ROM et physique — le scanner n'a pas pu lire son script.",
            CauseEn = "The .vpx file is corrupt, locked by another program, or in an unsupported format.",
            CauseFr = "Le fichier .vpx est corrompu, verrouillé par un autre programme, ou dans un format non pris en charge.",
        },
        ["TABLES_DIR_NOT_FOUND"] = new()
        {
            ImpactEn = "There's nothing to scan — no tables were found under the selected folder.",
            ImpactFr = "Il n'y a rien à scanner — aucune table n'a été trouvée sous le dossier sélectionné.",
            CauseEn = "The chosen folder probably isn't your Visual Pinball install (it should contain a Tables folder).",
            CauseFr = "Le dossier choisi n'est probablement pas ton installation Visual Pinball (il devrait contenir un dossier Tables).",
        },
        ["ROMS_DIR_NOT_FOUND"] = new()
        {
            ImpactEn = "ROM checks were skipped — the scanner can't tell which ROMs you have.",
            ImpactFr = "Les vérifications de ROM ont été ignorées — le scanner ne peut pas savoir quelles ROMs tu possèdes.",
            CauseEn = "VPinMAME's roms folder wasn't found under the selected install.",
            CauseFr = "Le dossier roms de VPinMAME n'a pas été trouvé sous l'installation sélectionnée.",
        },
        ["PINUP_DISPLAY_ZOMBIE"] = new()
        {
            ImpactEn = "The next table can fail to launch (or its backglass window can misbehave) until the leftover process is closed.",
            ImpactFr = "La prochaine table peut échouer à se lancer (ou sa fenêtre backglass peut mal se comporter) tant que le processus résiduel n'est pas fermé.",
            CauseEn = "PinUpDisplay.exe sometimes doesn't exit cleanly when a table closes and is left running with nothing using it.",
            CauseFr = "PinUpDisplay.exe ne se ferme parfois pas proprement quand une table se termine, et reste actif sans rien qui l'utilise.",
            AutoFixable = true,
        },
        ["DISPLAY_SETUP_INCOMPLETE"] = new()
        {
            ImpactEn = "A backglass or DMD is configured but has nowhere to display — it may silently not show up.",
            ImpactFr = "Un backglass ou un DMD est configuré mais n'a nulle part où s'afficher — il peut ne pas apparaître, silencieusement.",
            CauseEn = "Fewer displays are currently connected than the install's components expect — often a cable, a sleeping monitor, or a reconnection-order issue after a restart.",
            CauseFr = "Moins d'écrans sont actuellement connectés que ce que les composants de l'installation attendent — souvent un câble, un moniteur en veille, ou un souci d'ordre de reconnexion après un redémarrage.",
        },
        ["ORPHANED_MEDIA_FILE"] = new()
        {
            ImpactEn = "No functional impact — just wasted disk space that grows over time.",
            ImpactFr = "Aucun impact fonctionnel — juste de l'espace disque perdu qui grossit avec le temps.",
            CauseEn = "Media files (wheel images, videos…) are left behind after a table is removed or renamed.",
            CauseFr = "Des fichiers média (images wheel, vidéos…) restent après la suppression ou le renommage d'une table.",
            AutoFixable = true,
        },
        // Code émis par LegacyTableScanner depuis le 30/07, mais jamais ajouté ici — trou repéré
        // le 04/08 en vérifiant l'état réel du scanner avant de coder autre chose (même discipline
        // que l'alerte KPI#1 du 03/08 : on vérifie le code avant de croire la doc).
        ["VPT_LEGACY_PRESENT"] = new()
        {
            ImpactEn = "The table works fine in a classic Visual Pinball 9 player, but stays invisible in the PinUP Popper wheel until a matching legacy emulator entry exists.",
            ImpactFr = "La table fonctionne très bien dans un lecteur Visual Pinball 9 classique, mais reste invisible dans la roue de PinUP Popper tant qu'aucune entrée d'émulateur legacy correspondante n'existe.",
            CauseEn = "PinUP Popper matches a table to an emulator by file extension. '.vpt' (VP9) isn't listed among the VPX emulator's extensions, and NailBuster advises against adding it there — it breaks '.vpt' launching for that emulator instead of fixing visibility.",
            CauseFr = "PinUP Popper associe une table à un émulateur par extension de fichier. « .vpt » (VP9) ne fait pas partie des extensions de l'émulateur VPX, et NailBuster déconseille de l'y ajouter — ça casse le lancement des .vpt pour cet émulateur au lieu de régler la visibilité.",
        },
    };
}
