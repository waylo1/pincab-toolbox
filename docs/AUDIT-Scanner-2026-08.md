# AUDIT FONCTIONNEL DU SCANNER + VISION PRODUIT — Pincab Toolbox / FlipSync

**MC Automation — Maxime Chauvin** · Audit du 05/08/2026 · rôle tenu : expert Virtual Pinball (VPX / VPinMAME / PUP Popper / B2S / FlexDMD / DOF) **et** Product Manager / CTO.

> **Statut : document d'analyse et de recommandation. Rien n'est codé à partir de ce fichier sans le double filtre du projet** (ADR-004 d'abord, puis « deux signaux terrain indépendants »). Il ne remplace pas `PROJECT-BRAIN.md` (source de vérité) — il l'alimente.

---

## 0. Réponse directe à la question posée (synthèse exécutive)

**« Une fois les 12 scanners additionnés, reste-t-il des catégories importantes de pannes que Pincab Toolbox ne détecte toujours pas ? »**

Oui — **six catégories** de pannes fréquentes et *statiquement détectables* restent aujourd'hui non couvertes ou seulement effleurées. Par ordre de valeur :

1. **Intégrité des scripts partagés VBScript** (`core.vbs` / `controller.vbs` locaux périmés qui shadowent les scripts globaux) — *table-breaking, touche potentiellement TOUTES les tables*. Preuve terrain la plus forte de tout l'audit.
2. **Topologie d'affichage réelle** (backglass/DMD dont les coordonnées pointent hors écran) — le DisplaySetupScanner actuel a *volontairement* laissé ce trou faute de schéma ; or le schéma existe et est lisible (`ScreenRes.txt` + `B2STableSettings.xml`).
3. **Intégrité colorisation + altsound** (paires AltColor/SERum incomplètes, `altsound.csv` référençant des samples absents) — DMD noir/muet silencieux.
4. **État audio courant** (périphérique par défaut sur HDMI, backends audio absents) — la *détection* manque alors que l'action Repair existe déjà.
5. **Résidus/mismatch d'upgrade Freezy** (déjà au backlog, bloqué faute de cause confirmée) — E0434352, panne très fréquente.
6. **Hygiène système locale** (séparateur décimal FR, jonctions NTFS cassées, DPI ≠ 100 %) — dont le **séparateur décimal FR**, spécifiquement pour ton marché francophone.

**Mais** — et c'est le cœur de la recommandation — **aucune de ces six n'est un « P0 avant v1.0 ».** La règle du projet (`PROJECT-BRAIN` §7 : *« ne pas ajouter de nouveaux checks avant le lancement, un faux positif juste avant le post tue la crédibilité »*) prime. Le seul chantier Scanner justifié avant v1.0 était le **comparateur de version VPX** — livré et vert dans cette session (140/140 Core). Le vrai P0 reste **le test sur cab réel**. Les six catégories ci-dessus sont un **backlog priorisé post-lancement**, à débloquer au rythme des signaux terrain, pas une liste à coder maintenant.

Enfin, l'angle commercial (demande explicite de Maxime) : **la détection reste gratuite (l'aimant), le correctif se vend.** Trois de ces familles amorcent directement une ligne payante — la colorisation/altsound est la ligne **Table Companion**, le tuning rendu/input est **Play Optimizer**, le Script Doctor profond est **Creator Suite**. Détail en §8.

---

## 1. Méthode et discipline appliquées

Cet audit a été construit **dans cet ordre**, pour ne pas retomber dans le piège documenté du 05/08 (« 5 idées sur 6 déjà codées » parce que générées par recherche web sans vérifier le code) :

1. **Lecture du code réel des 12 scanners** (pas seulement l'inventaire) — chaque « non couvert » ci-dessous est vérifié contre la source.
2. **FIELD-LOG** — les signaux terrain réels déjà datés (Pincab Passion, VPForums, Pinball Nirvana, Gregg/FD/itchigo) sont la preuve prioritaire.
3. **ADR-004 en premier filtre**, puis cohérence `PROJECT-BRAIN`.
4. **Recherche web de corroboration** (VPForums, VPUniverse, Pinball Nirvana, wiki nailbuster, issues GitHub vpinball/freezy) — *jamais* comme source primaire, seulement pour confirmer la fréquence.
5. **Arbitrage des deux salves d'idées Gemini** selon la règle maison « pépite / glaise » (`PROJECT-BRAIN` §9) — synthétisées et dédupliquées, pas recopiées.

### Le classement qui compte vraiment : risque de faux positif

Le projet a une aversion asymétrique au faux positif (un FP tue la conversion). J'introduis donc une distinction que la roadmap doit utiliser pour fixer **la barre de preuve avant de coder** :

- 🟢 **Déterministe** — le check constate un fait objectif (fichier de 0 octet, boucle d'alias, XML malformé, coordonnée hors de l'union des écrans, paire de fichiers manquante). **Risque de FP quasi nul → barre de preuve basse** : ces checks sont sûrs à construire même sur un signal terrain modéré.
- 🟡 **Heuristique** — le check porte un jugement (version « périmée », combo de réglages « aberrant », intention de l'utilisateur). **Risque de FP réel → barre de preuve haute** : exige les deux signaux terrain indépendants avant de coder, et un biais explicite vers le silence.

C'est cette colonne, plus que la « valeur », qui doit piloter l'ordre de construction.

> **Rendre un 🟡 sûr sans attendre — la « doctrine Note »** (décisions Maxime 05/08 : *« trouve comment éviter les FP sur les oranges »* + *« faut une nouvelle catégorie que Info, genre note »*). Un **nouveau palier de sévérité `Note`** a été ajouté (entre `Info` et `Warning`, partie **Core livrée verte cette session** : 144/144) : il **ne bouge pas le score** et ne déclenche **jamais** « FIX THIS FIRST » (comme `Info`/`Ok`), mais donne à l'utilisateur un signal distinct « à noter — à toi de voir ». Un 🟡 devient shippable en émettant le **fait** en `Note`, en n'escaladant en `Warning` que sur une **sous-condition déterministe**, et en résumant les checks par-table en **un** finding compté. C'est la généralisation propre du patron `COMPAT_MIN_VERSION`. Conséquence : **les 🟡 se shippent aussi** (en `Note`), au lieu d'attendre. Reste à faire (Sonnet, prérequis) : le **rendu App** de `Note` (libellé FR/EN, couleur, 6 exports). Garde-fous par item dans le handoff. Seuls vrais irréductibles : **F3 quote-safety** et le *fix* core.vbs (ADR).

---

## 2. Cartographie de couverture actuelle (les 12 scanners + le comparateur livré)

Rappel condensé (pour ancrer les « déjà couvert », pas pour redécouvrir) :

| # | Scanner (Id) | Ce qu'il couvre réellement | Codes |
|---|---|---|---|
| 1 | ROM Validator (`rom`) | ROM requise (script → VPinMAME.Controller, commentaires strippés), alias VPMAlias, ROM dézippée, dossier roms multi-lecteur | ROM_MISSING (Crit), ROM_UNZIPPED, ROM_OK, ROM_NOT_REQUIRED, ROMS_DIR_NOT_FOUND… |
| 2 | Bitness Doctor (`bitness`) | 32/64-bit de chaque binaire, mismatch VPX↔VPinMAME, hybride, dmddevice64 manquant | BITNESS_MISMATCH_VPM (Crit), BITNESS_HYBRID_INSTALL, BITNESS_DMD64_MISSING… |
| 3 | Install Auditor (`completeness`) | Backglass `.directb2s` présent, enregistrement Popper, PUP-Pack, backglass orphelin/mal nommé, wheel média manquant | B2S_MISSING, B2S_ORPHAN, POPPER_NOT_REGISTERED, POPPER_MEDIA_MISSING… |
| 4 | Compatibility Linter (`compat`) | Signatures de script (nFozzy, FlexDMD/B2S), **version VPX déclarée** (Info seulement) | COMPAT_MIN_VERSION (Info), COMPAT_SIGNATURE |
| 5 | **VPX Version Check (`vpxversion`) — NOUVEAU (livré 05/08)** | **Version VPX installée (lue au PE) vs requise déclarée → Warning si réel manque, silence sinon** | **VPX_VERSION_OUTDATED (Warning)** |
| 6 | Dependency Check (`dependencies`) | Script utilise FlexDMD / B2S.Server mais DLL absente | FLEXDMD_MISSING, B2S_SERVER_MISSING |
| 7 | Blocked-file / Security (`security`) | Mark-of-the-Web sur DLL (Zone.Identifier) | BLOCKED_DLL (Crit/Warn) |
| 8 | Legacy Tables (`legacy`) | Tables `.vpt` (VP9) invisibles dans Popper | VPT_LEGACY_PRESENT |
| 9 | Disk Space (`disk`) | Disque des tables presque plein (< 5 Gio) | LOW_DISK_SPACE |
| 10 | Stuck Processes (`process`) | `PinUpDisplay.exe` zombie bloquant le lancement | PINUP_DISPLAY_ZOMBIE |
| 11 | Display Setup (`display`) | Composant multi-écran présent mais < 2 écrans connectés (**compte, pas ordre**) | DISPLAY_SETUP_INCOMPLETE |
| 12 | Orphaned Media (`media-orphan`) | Médias Popper ne correspondant à aucune table | ORPHANED_MEDIA_FILE |
| 13 | Update Watcher (`updates`) | Version locale vs VPS (filtre mods/variantes) | UPDATE_AVAILABLE, VPS_MATCH_SUMMARY |

**Ce que cette cartographie révèle en creux** : la couverture est excellente sur **ROM, bitness, complétude backglass/Popper, MotW, processus**. Elle est **quasi absente** sur trois surfaces pourtant très bruyantes dans la communauté : **la colorisation/altsound (DMD)**, **l'intégrité des scripts partagés (core.vbs)**, et **la topologie d'affichage réelle** (au-delà du simple compte d'écrans).

---

## 3. Analyse de couverture par catégorie de problème

Légende : ✅ couvert · 🟡 partiel · ❌ non couvert.

| Catégorie de panne (réelle, observée) | Statut | Détail |
|---|---|---|
| ROM manquante / mal nommée / dézippée | ✅ | ROM Validator, très affûté anti-FP |
| Migration 32↔64-bit incomplète | ✅ | Bitness Doctor |
| DLL bloquée par Windows (MotW) | ✅ | Blocked-file |
| Backglass absent / orphelin / mal nommé | ✅ | Install Auditor |
| Table absente du frontend Popper | ✅ | Install Auditor (POPPER_NOT_REGISTERED) |
| Version VPX installée < requise par la table | ✅ **(livré 05/08)** | VPX Version Check |
| Mises à jour de tables disponibles | ✅ | Update Watcher |
| Processus PinUpDisplay zombie | ✅ | Stuck Processes |
| Espace disque / écran manquant (compte) | ✅ / 🟡 | Disk Space ✅ ; Display 🟡 (compte seulement) |
| **`core.vbs`/`controller.vbs` local périmé shadowant le global** | ❌ | **Aucun check. Table-breaking, preuve terrain forte.** |
| **Backglass/DMD hors écran (coordonnées invalides)** | 🟡→❌ | Display détecte le *compte* ; l'*ordre/position* (ScreenRes+B2STableSettings) est explicitement laissé de côté |
| **Colorisation AltColor/SERum incomplète (paire manquante, 32/64)** | ❌ | Aucun check. DMD mono/crash silencieux. |
| **AltSound : `altsound.csv` référençant des samples absents** | ❌ | Aucun check. Silence/crash au sample. |
| **État audio courant (device par défaut HDMI, backend absent)** | ❌ | Action Repair existe, **détection manque** |
| **Résidus/mismatch upgrade Freezy (zedmd résiduel, 64-bit en PinUP x86)** | ❌ (backloggé, bloqué) | E0434352 « très fréquent » ; attend cause confirmée |
| **Séparateur décimal FR / locale cassant VPX** | ❌ (backlog v0.2) | Spécifique marché FR |
| **DPI Windows ≠ 100 % (affichage tronqué/zoomé)** | ❌ | Lecture registre triviale |
| **Boucle d'alias VPMAlias (crash stack overflow)** | ❌ | Déterministe, on a déjà le parseur d'alias |
| **NVRAM `.nv` de 0 octet (écran noir/freeze)** | ❌ | Déterministe, trivial |
| **`.directb2s` XML malformé (crash B2SBackglassServer)** | ❌ | Déterministe |
| **PlaylistID orphelin dans PUPDatabase (freeze menu)** | ❌ | Déterministe, on lit déjà la base en SQLite |
| **Runtimes VC++/.NET absents (crash au lancement)** | ❌ | Lecture registre |
| **Jonctions NTFS cassées après changement de lettre de lecteur** | ❌ | Déterministe |
| DOF ne réagit pas (feedback matériel) | ❌ | *Frontière Play Optimizer* — voir §8 |
| Input lag / stuttering (réglages rendu) | ❌ | *Frontière Play Optimizer* |
| Double-mapping input (tilt fantôme, batteurs bloqués) | ❌ | *Frontière Play Optimizer* |
| Dépréciation VBScript par Windows | ❌ | *Risque stratégique* `PROJECT-BRAIN` §8 |

---

## 4. Propositions de scanners (« Doctors »), regroupées et arbitrées

Chaque fiche : **ce qu'il vérifie · preuve utilisée · faux positifs · FP-risk · difficulté · valeur · cohérence ADR/BRAIN · angle commercial · ligne produit**. Regroupées par famille pour éviter la prolifération (Maxime : *« je ne veux pas plus de checks »* — l'objectif est la couverture cohérente, pas le nombre).

### Famille A — Script & VBScript Doctor  *(la plus forte)*

**A1. VBScript Shared-Script Doctor** — détecte les copies locales de `core.vbs`, `controller.vbs`, `VPMKeys.vbs`, `Nudge.vbs` dans `Tables/` et lit leur version interne ; signale une version en retard sur un plancher connu, ou un doublon qui shadow le script global.
- *Preuve* : la plus forte de l'audit. VPForums « Can't open Core.vbs — can't open ANY tables » ; issues GitHub vpinball #582 (controller.vbs non chargé), #1666 (WPC.vbs) ; VPINBALL.COM « Object Required: Controller ». **Table-breaking à l'échelle de toute la collection.**
- *Faux positifs* : la *présence* est déterministe ; le jugement « périmé » est heuristique (il faut un plancher de version fiable). 🟡
- *Difficulté* : moyenne. *Valeur* : **très haute**. *ADR* : OK (lecture ; comparaison ≠ fourniture).
- *Commercial* : détection gratuite → **fix Repair payant** = restaurer le bon `core.vbs`. ⚠️ **Décision Maxime requise** : `core.vbs`/`controller.vbs` sont issus de la distribution OSS vpinball — entrent-ils dans l'exception « dépendances open source » d'ADR-004 (au même titre que Freezy/B2S/DOF) ? Si oui, Repair peut les *fournir* légalement. À trancher par ADR avant tout code.
- *Ligne* : Scanner (détecte) + Repair (corrige) + Creator Suite (version profonde). **Priorité P1.**

**A2. Font Dependency Checker** — extrait du script les polices `.ttf` requises (scoreboards/DMD), vérifie l'installation Windows. 🟡 heuristique (extraction de nom). Valeur moyenne. **P2.** Ligne Scanner.

**A3. Hardcoded-Path Linter** — détecte les chemins absolus en dur (`"C:\Users\autrui\..."`) dans les scripts pointant vers un fichier absent (sons/images). 🟡. Valeur moyenne. **P2.** Ligne Scanner/Creator Suite.

### Famille B — DMD, Colorisation & AltSound Doctor  *(→ amorce Table Companion payant)*

**B1. AltColor / SERum Pair Integrity** — dans `altcolor/<rom>/`, vérifie la présence stricte des paires requises (`.vni`+`.pal`, ou `.cRz`/Serum + `.pal`) et la concordance 32/64-bit des DLL de colorisation.
- *Preuve* : forte. VPForums 53452, VPUniverse 10162, freezy #143 (« can't open .vni »), Pinball Nirvana « make work altcolor ».
- *FP* : 🟢 **déterministe** (paire manquante = fait). *Difficulté* : moyenne. *Valeur* : **haute**. *ADR* : OK.
- *Commercial* : détection gratuite → **gestion payante Table Companion** (organiser/vérifier ses colorisations). *Ligne* : Scanner + Table Companion. **P1.**

**B2. AltSound Structural Linter** — parse `altsound/<rom>/altsound.csv` : existence des `.wav`/`.ogg` référencés, syntaxe CSV, cohérence de mode.
- *Preuve* : forte (corroborée). *FP* : 🟢 déterministe (fichier référencé absent = fait). *Difficulté* : moyenne. *Valeur* : **haute**. *ADR* : OK. *Commercial* : Scanner + Table Companion. **P1.**

**B3. dmddevice.ini COM-Probe** — driver matériel activé (`[pin2dmd]`/`[zedmd]`/`[pindmd3]`) alors que le port COM correspondant n'est pas énuméré → freeze de 5-15 s au lancement. 🟡 (le device peut être éteint). Valeur moyenne-haute. **P2.** Ligne Scanner (frontière Play Optimizer).

**B4. Freezy/zedmd Upgrade Residue** *(déjà au backlog, BLOQUÉ)* — DLL Freezy 64-bit en setup PinUP x86, `zedmd.dll`/`zedmd64.dll` résiduels de l'ancienne version. E0434352. *Preuve* forte (FIELD-LOG 2026-07-28) **mais cause non confirmée par l'utilisateur** → reste bloqué par la règle de lancement. 🟡. **P1 dès confirmation.** Ligne Scanner + Repair (quarantaine résidus).

**B5. FlexDMD Dual-Registration Conflict** — FlexDMD enregistré COM global **et** déclaré dans dmddevice.ini → double rendu. Preuve faible (Gemini seul). **P3.**

### Famille C — Display Topology Doctor v2  *(complète le trou volontaire du #11)*

**C1. Screen Topology Check** — croise `ScreenRes.txt` + `B2STableSettings.xml` + la géométrie réelle des écrans (union des rectangles moniteurs) pour détecter un backglass/DMD dont les coordonnées **pointent hors de la zone d'affichage** → invisible.
- *Preuve* : forte. VPForums 29802 (« B2S on wrong monitor, ScreenRes not fixing »), wiki nailbuster `vp_display_issues` + `b2s_dimension_location`, VPUniverse, YouTube « Backglass NOT showing FIXED ».
- *Point clé* : le DisplaySetupScanner a **explicitement** renoncé à l'ORDRE/position faute de schéma (« lives in Popper config, undocumented »). **Or ce schéma existe et est lisible** : `ScreenRes.txt` (racine Tables) + `B2STableSettings.xml`. Ce Doctor **débloque** ce que le #11 ne pouvait pas faire.
- *FP* : 🟢 majoritairement déterministe (coordonnée hors union = fait). *Difficulté* : moyenne-haute (parser ScreenRes + énumérer les moniteurs). *Valeur* : **très haute** (backglass invisible = top-3 des douleurs). *ADR* : OK. *Ligne* : Scanner. **P1.**

**C2. DPI Scaling Trap** — lit `HKCU\...\WindowMetrics\AppliedDPI` ; avertit si ≠ 100 % (backglass/table tronqués/décalés). 🟢/🟡. Difficulté basse. Valeur moyenne-haute. **P2.** Ligne Scanner.

### Famille D — Audio Doctor  *(→ boucle avec l'action Repair audio existante)*

**D1. Audio Current-State Check** — périphérique par défaut pointant sur une sortie HDMI/écran, aucun endpoint activé, volume maître à zéro.
- *Preuve* : forte (FIELD-LOG 2026-07-29 Pincab Passion « aléatoirement au démarrage l'audio par défaut passe sur l'HDMI »).
- *Nuance honnête* : on **ne peut pas** prédire *le reset* (le FIELD-LOG l'a déjà acté). On détecte l'**état courant** mauvais au moment du scan — ce qui est différent et légitime. 🟡.
- *Commercial* : **détection gratuite → action Repair `set_default_audio_device` DÉJÀ codée** (il ne manque que le Finding qui la déclenche). *Ligne* : Scanner + Repair. **P1.**

**D2. Audio Backend Presence** — `BASS.dll` (altsound), VLC (audio PUP) absents. 🟢. Valeur moyenne. **P2.**

### Famille E — VPinMAME / Config Integrity Doctor

**E1. VPMAlias Recursion Trap** — parse `vpmalias.txt`, détecte les cycles (A→B→A) → crash stack overflow immédiat de VPinMAME. *FP* : 🟢 **déterministe, zéro FP** (cycle = fait). *Difficulté* : **basse** (on a déjà `AliasFile.cs`). *Valeur* : moyenne (rare mais crash dur). **P2** (candidat « quick win » : coût faible, FP nul → barre de preuve basse).

**E2. Registry vs INI Phantom Conflict** — clés résiduelles `HKCU\Software\Freeware\Visual PinMAME` écrasant silencieusement `vpinmame.ini`. 🟡. Difficulté moyenne (registre). Valeur moyenne. **P2.**

**E3. VPinMAME COM non enregistré** — `VPinMAME.dll` présente mais COM non enregistré. 🟡. Valeur moyenne. **P2.**

### Famille F — Frontend / Popper Doctor  *(lecture SQLite déjà maîtrisée)*

**F1. PUPDatabase Relational Linter** — jointure `Games`×`Playlists` : jeux sur `PlaylistID` orphelin → freeze silencieux du menu Popper. *FP* : 🟢 déterministe (FK orpheline = fait). *ADR* : OK (**lecture** SQLite ; ADR-007 ne vise que l'écriture). *Difficulté* : moyenne. *Valeur* : moyenne. **P2.**

**F2. Close-Script Cleanup Audit** — les Close Scripts Popper manquent de `TASKKILL` pour `dmdext.exe` / `B2SBackglassServer.exe` / `PinUpDisplay.exe` → **complète le Stuck Processes** (#10) en détectant la *cause config*, pas seulement le symptôme. 🟡. **P2.**

**F3. Launch/Close Quote Safety** — variables non entre guillemets dans les scripts de lancement (noms avec espaces/`&`). 🟡 **FP-prone** (beaucoup de scripts valides). **P3.**

### Famille G — Folder / Hygiene Doctor  *(→ Repair cleanup payant)*

**G1. FR Decimal-Separator Check** — séparateur décimal/liste Windows (virgule) cassant la physique/les scripts VPX. *Preuve* : douleur FR connue, **déjà listée BRAIN v0.2**. 🟢/🟡. Difficulté basse. *Valeur* : **haute pour ton marché FR**. **P1** (différenciateur francophone).

**G2. Path Hygiene** — chemins trop longs (> 260), caractères interdits, permissions (install sous Program Files). 🟢/🟡. **P2.**

**G3. Junction/Symlink Health** — points de jonction NTFS cassés (Tables/roms/PUPVideos déportés sur 2ᵉ SSD) après réattribution de lettre. 🟢 déterministe. Lié au cas multi-lecteur de FD. **P2.** Ligne Scanner + Repair.

**G4. Crash-Temp Sweep** — `.vpx.bak`, `.tmp` résiduels, fichiers de crash 0 octet. 🟢. Valeur faible-moyenne. **P3.** (Repair cleanup.)

### Famille H — Binary Asset Integrity Doctor  *(on a déjà le lecteur OLE)*

**H1. NVRAM 0-Byte Detector** — `VPinMAME/nvram/*.nv` de 0 octet → écran noir/freeze au démarrage. 🟢 **déterministe** (limiter au 0-octet ; « taille ≠ spec » exclu, on n'a pas de base de specs). Difficulté **basse**. **P2.**

**H2. DirectB2S XML Malform** — extrait et parse le XML embarqué dans `.directb2s` ; isole les backglass corrompus qui crashent `B2SBackglassServer.exe`. 🟢 déterministe (XML malformé = fait). Difficulté moyenne. **P2.**

**H3. VPX OLE Texture / VRAM Audit** — résolution des textures embarquées vs VRAM GPU → risque OOM. 🟡 (seuil VRAM = jugement). Difficulté **haute**. **P3.**

**H4. Popper Media Codec Linter** — en-têtes `.mp4` H.265/HEVC → saccades CPU sur vieilles configs. 🟡. **P3.** (Table Companion.)

**H5. Custom POV Integrity** — `.pov` forçant un angle « Desktop » sur un cab « Cabinet ». 🟡. **P3.**

### Famille I — Runtime & Système

**I1. VC++ / .NET Runtime Presence** — redistribuables requis par VPX/plugins absents (lecture registre). 🟢/🟡. Difficulté basse-moyenne. Valeur moyenne-haute. **P2.**

**I2. VBScript Deprecation Watch** *(stratégique)* — détecte l'état de dépréciation/désactivation de VBScript sur ce Windows. Fréquence actuelle faible, **valeur stratégique majeure** (`PROJECT-BRAIN` §8, risque n°1 : « être l'outil qui *explique* le premier »). 🟢. **P1-stratégique.**

### Famille J — Input & Rendu  *(→ Play Optimizer, PAS Scanner)*

**J1. Input Wrapper Conflict** (x360ce/JoyToKey/Xpadder double-mapping → tilt fantôme). **J2. Render Pipeline Auditor** (VPinballX.ini SyncMode/MaxFrames/FPSLimit aberrants → stuttering/lag). **J3. SSF Spatial Mismatch**. Ces trois **tournent pendant le jeu / règlent l'expérience** → **ligne Play Optimizer** (§8), pas Scanner. Notées ici pour mémoire, écartées du périmètre Scanner.

---

## 5. Priorisation P0 → P3

> **P0 avant v1.0 : AUCUN nouveau scanner.** C'est la réponse disciplinée, pas un défaut d'ambition. La règle « pas de nouveau check avant le lancement » (BRAIN §7) prime : un FP juste avant le post tue la crédibilité. Le seul ajout sanctionné — le **comparateur VPX** — est **livré**. Le vrai P0 est **le test sur cab réel**.

| Priorité | Contenu | Justification |
|---|---|---|
| **P0 (avant v1.0)** | *(aucun nouveau scanner)* — test cab réel + comparateur VPX (fait) | Gel Scanner pré-lancement ; le FP est l'ennemi n°1 |
| **P1 (forte valeur, post-lancement)** | A1 Script Doctor · C1 Screen Topology · B1 AltColor + B2 AltSound · D1 Audio State · B4 Freezy résidu *(dès confirmation)* · G1 Séparateur FR · I2 VBScript Watch | Chacun = panne fréquente **et** corroborée terrain ; plusieurs amorcent une ligne payante |
| **P2 (amélioration)** | E1 VPMAlias loop · C2 DPI · H1 NVRAM 0-octet · H2 directb2s XML · F1 PUPDatabase orphelin · G3 Junctions · I1 Runtimes · E2 Registry/INI · F2 Close-script · A2/A3 Font/Hardcoded-path | Bonne valeur ; les 🟢 déterministes (E1,H1,H2,F1,G3) sont des « quick wins » sûrs |
| **P3 (vision long terme)** | H3 Texture/VRAM · H4 Codec · H5 POV · B5 FlexDMD dual-reg · F3 Quote-safety · G4 Temp-sweep · J1-J3 *(→ Play Optimizer)* | Preuve faible, FP-prone, ou hors périmètre Scanner |

**Ordre de construction recommandé (post-lancement, quand deux signaux tombent)** : commencer par les **🟢 déterministes à forte valeur** (C1 Screen Topology, B1/B2 colorisation/altsound) — ils cochent « valeur haute » **et** « FP quasi nul », donc la barre de preuve est basse et le risque de casser la confiance minimal. Garder A1 (Script Doctor) juste derrière car c'est le plus vendeur mais 🟡 (barre de preuve haute).

---

## 6. Vérification de sûreté (étape 5 de la mission)

Aucune proposition de cet audit ne :

| Contrainte | Respectée ? | Note |
|---|---|---|
| Télécharge du contenu (tables/ROM/médias/backglass/colorisation) | ✅ | Toutes sont **lecture seule**. Seule zone à trancher : A1 (fournir `core.vbs` OSS via Repair) — **exception OSS ADR-004 à valider par ADR**, jamais assumée. |
| Redistribue des ROM | ✅ | Aucune. |
| Viole ADR-004 | ✅ | Filtre appliqué en premier. Les colorisations/altsound sont **vérifiées, jamais fournies**. |
| Contredit `PROJECT-BRAIN` | ✅ | P0 respecte le gel Scanner ; frontières de lignes (J → Play Optimizer) respectées ; ADR-007 respecté (F1 = lecture SQLite). |
| Augmente inutilement les faux positifs | ✅ | Classement FP-risk explicite ; les 🟡 exigent deux signaux + biais silence ; priorité aux 🟢. |

---

## 7. Arbitrage des deux salves Gemini (« pépite / glaise »)

Conformément à `PROJECT-BRAIN` §9. Gemini a produit ~25 idées sur deux salves. **Bilan : nettement meilleur que le brainstorm web du 05/08** (là où 5/6 idées étaient déjà codées, ici la grande majorité sont *réellement non couvertes*). Synthèse :

**Pépites retenues (intégrées ci-dessus)** : Screen Topology (ScreenRes+B2STableSettings) → C1 ; VPMAlias recursion → E1 ; AltColor/SERum + AltSound → B1/B2 ; NVRAM 0-octet → H1 ; directb2s XML malform → H2 ; PUPDatabase orphelin → F1 ; DPI trap → C2 ; Registry/INI phantom → E2 ; Junction health → G3 ; COM-probe timeout → B3 ; Close-script cleanup → F2 ; Font/Hardcoded-path → A2/A3 ; VC++ runtime (implicite) → I1 ; core.vbs override (recoupé par ma recherche) → A1.

**Glaise écartée ou rétrogradée** :
- *SSF Spatial Mismatch, Render Pipeline, Input Wrapper, Audio Hook Overlap (PinVol/EqualizerAPO/DOF Night Mode)* → **pas Scanner** : réglage pendant le jeu / matériel → **Play Optimizer** (§8). Écartés du périmètre Scanner, pas de l'univers produit.
- *Quote-safety des scripts de lancement* → **FP-prone**, rétrogradé P3.
- *Texture/VRAM audit, Media Codec, POV integrity, FlexDMD dual-reg* → preuve faible et/ou seuil de jugement → P3.
- *« PUP-Pack Mechanical Mute » (cacophonie son méca + DOF)* → **subjectif** (préférence, pas panne) → écarté.

**Point de méthode important** : même les pépites Gemini **ne se codent pas sur la seule parole de Gemini**. Une IA est *une* source. Les 🟢 déterministes (E1, H1, H2, F1) ont une barre basse parce que leur FP est nul — mais leur *fréquence/valeur* reste à confirmer par le terrain avant de prioriser. Les 🟡 exigent les deux signaux terrain. Gemini a bien nourri la *liste*, pas le *feu vert*.

---

## 8. Vision produit long terme — les besoins mappés aux 5 lignes

Rappel de la frontière (BRAIN §3) : **lit → Scanner · écrit sur l'état statique → Repair · tourne pendant le jeu / règle le matériel → Play Optimizer · agit par table sur le contenu téléchargé → Table Companion · s'adresse au créateur → Creator Suite.**

### 8.1 Architecture de monétisation (réponse à « on ne met pas tout gratuit »)

Le principe ADR-006 se généralise à tout l'audit : **le Scanner DÉTECTE gratuitement (l'aimant), le correctif/la gestion se PAIENT.** Concrètement :

| Besoin détecté (gratuit, Scanner) | Produit payant qui le monétise | Ligne |
|---|---|---|
| `core.vbs` périmé, résidus Freezy, MotW, ROM dézippée, audio device, jonction cassée, temp-sweep | **Repair** — corriger, avec sauvegarde/undo/journal | Repair |
| Colorisation/altsound incomplète, médias, doublons, alias | **Table Companion** — vérifier/organiser/préparer une table fraîchement téléchargée, gérer colorisation & son | Table Companion |
| Réglages rendu/input/SSF/DOF, focus écran, routage audio | **Play Optimizer** — régler l'expérience de jeu et le matériel | Play Optimizer |
| Scripts (API obsolètes, polices, chemins en dur, deps avant publication) | **Creator Suite** — outils pour ceux qui *font* des tables | Creator Suite |

**Conséquence stratégique** : plus le Scanner gratuit détecte de familles de pannes, plus il crée de *raisons d'acheter* le produit payant correspondant. Enrichir la détection **n'est pas** donner du gratuit — c'est **remplir le tunnel de vente** de chaque ligne payante. C'est l'inverse d'un coût : c'est l'aimant qui grossit.

### 8.2 Ce que révèle l'audit sur le meilleur 2ᵉ produit

La **famille B (DMD / colorisation / altsound)** est, de loin, la surface non couverte la mieux corroborée par le terrain (VPForums, VPUniverse, Pinball Nirvana, freezy GitHub). Or `PROJECT-BRAIN` §3 range explicitement **colorisation & son dans Table Companion**, et l'UNIVERS §6 identifie « le pont colorisation/son » comme **le meilleur 2ᵉ pas après le Scanner**. **L'audit terrain confirme indépendamment cette intuition** : la douleur colorisation/altsound est réelle, fréquente, et aujourd'hui *personne* ne la diagnostique. → **Recommandation : après Repair v1, le 2ᵉ produit payant est Table Companion, amorcé par la détection B1/B2 dans le Scanner gratuit.**

### 8.3 Couverture du cycle de vie du propriétaire de pincab

| Étape | Produit | État |
|---|---|---|
| **Installer** | (Baller Installer tiers) + Scanner qui *valide* l'install | Scanner ✅ |
| **Diagnostiquer** | **Scanner** (12 + comparateur ; backlog Doctors ci-dessus) | 🟢 Actif |
| **Réparer** | **Repair** (moteur v1, 5 actions) | 🟢 Moteur prêt |
| **Optimiser** | **Play Optimizer** (rendu, input, écrans, audio/SSF, DOF) | ⚪ Parking — seed = familles C/D/J |
| **Utiliser** | **Table Companion** (colorisation/son/médias/doublons) | ⚪ Parking — seed = famille B |
| **Créer** | **Creator Suite** (script doctor profond, deps) | ⚪ Parking — seed = famille A |

**L'audit remplit les réservoirs de trois des cinq lignes** avec des détections gratuites concrètes : B → Table Companion, C/D/J → Play Optimizer, A → Creator Suite. Aucune nouvelle ligne produit n'est nécessaire : tout ce que le terrain remonte entre dans les 5 cases existantes. *(Un seul besoin déborde légèrement — le pont vers le flipper physique — mais il est déjà parqué hors carte jusqu'au premier euro, BRAIN §3.)*

### 8.4 Infrastructure de distribution — bouton « Mise à jour » (auto-update)  *(demande Maxime 05/08)*

**Le besoin, réel** : aujourd'hui l'utilisateur re-télécharge le zip complet à chaque version sur le forum. Un bouton de mise à jour intégré → il télécharge **une fois**, l'app se met à jour ensuite seule. Améliore la rétention et garantit que tout le monde tourne sur un build récent.

**Placement** : ce n'est **ni un scanner, ni une 6ᵉ ligne** — c'est de l'**infrastructure de distribution**, transversale (elle sert le Scanner gratuit *et* Repair). Elle relève du packaging (territoire ADR-002), pas de la carte produit.

**Le vrai enjeu stratégique — deux canaux, pas un** :
- **Canal Knowledge Pack** (JSON léger, fréquent) — *c'est la valeur que fait payer ADR-002* (« l'abonnement ne porte que sur les mises à jour du Pack »). Le mettre à jour indépendamment du binaire est **le point le plus important** : c'est le tuyau qui délivre le récurrent payant. Léger, sûr (données, pas exécutable), faible risque.
- **Canal binaire** (l'exe, rare) — plus lourd, plus sensible (voir risques). Peut attendre.

**Cohérence ADR — à clarifier par un ADR dédié** :
- **ADR-004 n'est PAS violé** : la règle interdit de télécharger des *tables/ROMs/médias/backglass tiers*. Mettre à jour **notre propre app + notre propre Pack** est du **premier-parti**, catégoriquement différent (comme l'exception « dépendances open source »). Mais comme ADR-004 est « le premier filtre », **écrire un court ADR qui carve-out explicitement l'auto-update premier-parti** évite toute confusion future.
- **Règle « zéro télémétrie » (le trust asset n°1)** : le check doit être **déclenché par l'utilisateur** (bouton, pas de sondage silencieux au démarrage), **n'envoyer aucune donnée** (pas d'ID machine, pas d'usage) — un simple GET sur un manifeste statique. Précédent rassurant : l'Update Watcher fait déjà un GET HTTPS lecture-seule vers la base VPS ; le check de version est du même ordre.

**Risques (honnêtes)** :
1. **Smart App Control / réputation Windows** — *ta propre douleur du 04/08* : un exe fraîchement compilé et **non signé** est bloqué. Un updater qui télécharge un binaire non signé heurtera le **même mur chez les utilisateurs**. → **La signature de code devient nécessaire dès qu'on livre un updater** (certificat ~100-400 €/an, ou service de signature). C'est le vrai coût caché de la fonctionnalité.
2. **Auto-remplacement d'un exe en cours d'exécution** (Windows verrouille l'exe) — nécessite un petit helper (attendre la fermeture, renommer/remplacer, relancer). Fiddly mais standard.
3. **Confiance** — public méfiant (« je ne lance pas l'exe d'un inconnu »). Mitigations : checksum affiché, notes de version visibles avant install, confirmation explicite. **Synergie** avec la **version portable** (backlog v0.2) : un portable qui s'auto-mets à jour évite installeur + UAC, plus propre pour ce public.

**Verdict** : **gratuit** (hygiène de distribution, sert l'aimant). Priorité : **canal Knowledge Pack = P1** (il porte le récurrent payant d'ADR-002) ; **canal binaire = P2** et **conditionné à la signature de code**. À traiter comme chantier infra distinct, pas comme un Doctor. Manifeste possible : GitHub Releases API, la landing, ou Lemon Squeezy.

---

## 9. Note concurrentielle (veille)

- **VPin Studio** (open-source, syd711) — `PROJECT-BRAIN` §8 le désigne comme *« le seul acteur capable de fermer notre trou »*. Gère tables/joueurs/compétitions ; s'il ajoute du **diagnostic**, c'est notre menace directe. Veille trimestrielle.
- **PinCab.Configurator** (xantari, apparu dans la recherche B2S) — outil de **configuration** (ScreenRes, B2S, DMD position). Il *configure*, nous *diagnostiquons en lecture seule* — positionnements différents mais **surface d'overlap réelle** sur la topologie d'affichage (notre C1). À surveiller : s'il ajoute du diagnostic de santé, il croise notre terrain.
- **Baller Installer, ClrVpin** — installent/nettoient, ne diagnostiquent pas. Complémentaires, pas concurrents.

**Différenciateur défendable** : *aucun de ces outils n'explique une install existante* (symptôme → cause → correctif, avec niveau de fiabilité). C'est le trou où le Scanner s'installe, et le moteur Knowledge Engine est le moat.

---

## 10. Recommandations et prochaines actions

1. **Ne rien coder de nouveau au Scanner avant le test cab réel + v1.0.** Le comparateur VPX était la dernière pièce sûre ; elle est posée.
2. **Trancher par ADR la question `core.vbs`/`controller.vbs`** (exception OSS d'ADR-004) — c'est le préalable au Doctor le plus vendeur (A1). Décision produit, pas technique → Maxime/CTO, pas Sonnet.
3. **Post-lancement, ordre de construction** : C1 → B1/B2 → A1 → puis les 🟢 déterministes P2 (E1, H1, H2, F1, G3) comme « quick wins » sûrs, chacun gated par deux signaux terrain (ou, pour les 🟢, un signal + FP nul démontré).
4. **Acter Table Companion comme 2ᵉ produit payant** (après Repair v1), amorcé par la détection colorisation/altsound gratuite. L'audit terrain le confirme indépendamment de l'UNIVERS.
5. **Le handoff d'implémentation** (fichiers, étapes, tests, critères) est dans `docs/HANDOFF-Sonnet5-scanners-2026-08.md` — le comparateur VPX y sert de **gabarit de référence** pour tous les futurs Doctors.
6. **Bouton de mise à jour (§8.4)** : commencer par le **canal Knowledge Pack** (P1, porte le récurrent ADR-002), garder le canal binaire pour après la **signature de code**. Écrire un ADR premier-parti pour carve-out l'auto-update d'ADR-004.

---

### Sources terrain (corroboration)

- core.vbs / controller.vbs : [VPForums 45350](https://www.vpforums.org/index.php?showtopic=45350), [vpinball#582](https://github.com/vpinball/vpinball/issues/582), [vpinball#1666](https://github.com/vpinball/vpinball/issues/1666), [VPINBALL.COM Object Required Controller](https://vpinball.com/forums/topic/object-required-controller/)
- Backglass hors écran / ScreenRes : [VPForums 29802](https://www.vpforums.org/index.php?showtopic=29802), [nailbuster vp_display_issues](https://www.nailbuster.com/wikipinup/doku.php?id=vp_display_issues), [nailbuster b2s_dimension_location](https://www.nailbuster.com/wikipinup/doku.php?id=b2s_dimension_location)
- DOF : [vpinball#1143 (DOF cassé 10.7→10.8)](https://github.com/vpinball/vpinball/issues/1143), [VPUniverse DOF issue](https://vpuniverse.com/forums/topic/6945-dof-issue-one-bumper-does-not-do-anything/), [Cleveland SD DOF troubleshooting](https://pinball-docs.clevelandsoftwaredesign.com/docs/DOF/troubleshooting/)
- AltColor / AltSound : [VPForums 53452](https://www.vpforums.org/index.php?showtopic=53452), [VPUniverse 10162](https://vpuniverse.com/forums/topic/10162-color-dmd-working-in-vpinmame-but-not-playing-table/), [freezy#143](https://github.com/freezy/dmd-extensions/issues/143), [Pinball Nirvana altcolor](https://pinballnirvana.com/forums/threads/please-i-need-help-to-make-work-altcolor.21297/)
- Concurrent config : [PinCab.Configurator (xantari)](https://github.com/xantari/PinCab.Configurator/wiki)

*(Preuves internes non-liées : `knowledge/FIELD-LOG.md` — Pincab Passion audio 2026-07-29, Freezy E0434352 2026-07-28, FD rapport 2026-07-30.)*
