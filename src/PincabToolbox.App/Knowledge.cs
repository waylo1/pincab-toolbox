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
        // Tier A (handoff Sonnet 5, 06/08) — B1 AltColor/SERum Pair Integrity.
        ["ALTCOLOR_INCOMPLETE"] = new()
        {
            ImpactEn = "The DMD is likely to show in mono instead of full color, or the colorization plugin may fail to load this ROM's set at all.",
            ImpactFr = "Le DMD risque de s'afficher en mono au lieu de la colorisation complète, ou le plugin de colorisation peut échouer à charger le jeu de fichiers de cette ROM.",
            CauseEn = "Only part of a colorization set was extracted into altcolor/<rom>/ — e.g. the .pal without its matching .vni, or a Serum file without its .pal. A common result of extracting a downloaded archive without its subfolder, or an interrupted download.",
            CauseFr = "Seule une partie d'un jeu de colorisation a été extraite dans altcolor/<rom>/ — par ex. le .pal sans son .vni, ou un fichier Serum sans son .pal. Résultat fréquent d'une extraction d'archive sans son sous-dossier, ou d'un téléchargement interrompu.",
        },
        // Tier A (handoff Sonnet 5, 06/08) — B2 AltSound Structural Linter.
        ["ALTSOUND_SAMPLE_MISSING"] = new()
        {
            ImpactEn = "Some AltSound cues will stay silent during play, or the AltSound plugin may fail to load this ROM's sound pack altogether.",
            ImpactFr = "Certains sons AltSound resteront silencieux en jeu, ou le plugin AltSound peut échouer à charger le pack sonore de cette ROM.",
            CauseEn = "altsound.csv references one or more .wav/.ogg sample files that aren't present in altsound/<rom>/ — usually a partial extraction or a hand-edited manifest that no longer matches the files on disk.",
            CauseFr = "altsound.csv référence un ou plusieurs fichiers .wav/.ogg absents de altsound/<rom>/ — généralement une extraction partielle, ou un manifeste modifié à la main qui ne correspond plus aux fichiers présents.",
        },
        // Tier A (handoff Sonnet 5, 06/08) — C1 Screen Topology Check.
        ["DISPLAY_OFFSCREEN"] = new()
        {
            ImpactEn = "The backglass window will never be visible — it opens on a position no connected monitor covers, even though B2S Backglass Server itself reports no error.",
            ImpactFr = "La fenêtre du backglass ne sera jamais visible — elle s'ouvre à une position qu'aucun écran connecté ne couvre, même si B2S Backglass Server ne signale aucune erreur de son côté.",
            CauseEn = "ScreenRes.txt (or a table's own .res override) declares a backglass position that was valid for a monitor layout that has since changed — a monitor removed, a GPU swapped, or displays reconnected in a different arrangement.",
            CauseFr = "ScreenRes.txt (ou le .res propre à une table) déclare une position de backglass qui était valide pour une disposition d'écrans qui a changé depuis — un écran retiré, une carte graphique remplacée, ou des écrans rebranchés dans un ordre différent.",
        },
        // Tier A (handoff Sonnet 5, 06/08) — G3 Junction Health.
        ["BROKEN_JUNCTION"] = new()
        {
            ImpactEn = "Everything expected under this folder — an entire ROM set, a whole PUP-Pack collection, a colorization archive — is invisible to Visual Pinball, PinUP Popper and this scan alike, with no error anywhere.",
            ImpactFr = "Tout ce qui est attendu sous ce dossier — un jeu de ROM entier, toute une collection de PUP-Packs, une archive de colorisation — est invisible pour Visual Pinball, PinUP Popper et ce scan, sans la moindre erreur nulle part.",
            CauseEn = "This folder is an NTFS junction or directory symlink pointing at a drive, network share or path that is no longer there — commonly after a second drive is renamed, disconnected, or a NAS share drops offline.",
            CauseFr = "Ce dossier est une jonction NTFS ou un lien symbolique pointant vers un disque, un partage réseau ou un chemin qui n'existe plus — typiquement après le renommage ou la déconnexion d'un second disque, ou un partage NAS hors ligne.",
        },
        // Tier A (handoff Sonnet 5, 06/08) — H2 DirectB2S XML Malform.
        ["B2S_MALFORMED"] = new()
        {
            ImpactEn = "This backglass will not appear at all — B2S Backglass Server refuses to load a file that isn't well-formed XML, typically with just a generic \"not a valid directb2s backglass file\" error.",
            ImpactFr = "Ce backglass n'apparaîtra pas du tout — B2S Backglass Server refuse de charger un fichier qui n'est pas du XML bien formé, en général avec une simple erreur générique « not a valid directb2s backglass file ».",
            CauseEn = "The .directb2s file is truncated, empty, or otherwise not well-formed XML — most often an interrupted download or an export that didn't finish writing.",
            CauseFr = "Le fichier .directb2s est tronqué, vide, ou n'est pas du XML bien formé — le plus souvent un téléchargement interrompu ou un export qui ne s'est pas terminé.",
        },
        // Tier A (handoff Sonnet 5, 06/08) — F1 PUPDatabase Orphan Playlist.
        ["POPPER_ORPHAN_PLAYLIST"] = new()
        {
            ImpactEn = "The PinUP Popper frontend menu is known to freeze when opened, because it can't resolve a game's playlist assignment to a playlist that actually exists.",
            ImpactFr = "Le menu du frontend PinUP Popper est connu pour se figer à l'ouverture, car il ne peut pas résoudre l'affectation de playlist d'un jeu vers une playlist qui existe réellement.",
            CauseEn = "A playlist was deleted from PinUP Popper's admin UI while games were still assigned to it — deleting a playlist only removes the Playlists row, leaving the game assignments behind pointing at nothing.",
            CauseFr = "Une playlist a été supprimée depuis l'interface d'administration de PinUP Popper alors que des jeux y étaient encore affectés — supprimer une playlist ne retire que sa ligne dans Playlists, les affectations de jeux restent en place et pointent dans le vide.",
        },
        // Tier A (handoff Sonnet 5, 06/08) — H1 NVRAM 0-Byte Detector.
        ["NVRAM_EMPTY"] = new()
        {
            ImpactEn = "This table is likely to boot to a black screen or freeze instead of starting fresh — VPinMAME can't read any saved state from a 0-byte file.",
            ImpactFr = "Cette table risque de démarrer sur un écran noir ou de figer au lieu de démarrer proprement — VPinMAME ne peut lire aucun état sauvegardé depuis un fichier de 0 octet.",
            CauseEn = "The .nv save file was truncated to 0 bytes — usually a crash or forced shutdown mid-write, or a full disk at the moment VPinMAME tried to save.",
            CauseFr = "Le fichier de sauvegarde .nv a été tronqué à 0 octet — généralement un plantage ou un arrêt forcé en pleine écriture, ou un disque plein au moment où VPinMAME sauvegardait.",
            // Pas d'AutoFixable : ce flag n'a aucun lecteur dans l'App aujourd'hui (vérifié — le vrai
            // signal "réparable" vient de knowledge/pack-2026.08.json + RepairActionRegistry, un
            // registre fermé séparé, ADR-005). Le laisser à false partout où aucune règle Repair
            // réelle n'existe évite de donner un sens à une donnée qui n'en a pas encore.
        },
        // Tier A (handoff Sonnet 5, 06/08) — E1 VPMAlias Recursion Loop.
        ["VPMALIAS_LOOP"] = new()
        {
            ImpactEn = "VPinMAME crashes with a stack overflow the instant a table needs this ROM name — it never gets to load anything, the process simply dies.",
            ImpactFr = "VPinMAME plante avec un stack overflow dès qu'une table a besoin de ce nom de ROM — il ne charge jamais rien, le processus meurt directement.",
            CauseEn = "VPMAlias.txt contains a circular alias chain (one alias eventually points back to itself) — almost always a manual editing mistake, since a normal alias always resolves to a real ROM set name.",
            CauseFr = "VPMAlias.txt contient une chaîne d'alias circulaire (un alias finit par pointer vers lui-même) — presque toujours une erreur de modification manuelle, un alias normal se résout toujours vers un vrai nom de set ROM.",
        },
        // Rétroactif — comparateur VPX livré le 05/08, Knowledge/Loc complétés le 06/08 (R1 du handoff
        // Sonnet 5 : additif, calqué sur le patron COMPAT_MIN_VERSION voisin).
        ["VPX_VERSION_OUTDATED"] = new()
        {
            ImpactEn = "The table may fail to load or behave incorrectly — this isn't a table that merely mentions a version, it's a confirmed shortfall against the VPX actually installed.",
            ImpactFr = "La table peut échouer à se charger ou mal se comporter — ce n'est pas juste une table qui mentionne une version, c'est un manque confirmé face au VPX réellement installé.",
            CauseEn = "Visual Pinball X on this machine was never updated to the version this table declares it needs, checked against the newest installed VPX executable's real file version (not a guess).",
            CauseFr = "Visual Pinball X sur cette machine n'a jamais été mis à jour vers la version que cette table déclare nécessiter, vérifié contre la version fichier réelle du plus récent exécutable VPX installé (pas une supposition).",
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
