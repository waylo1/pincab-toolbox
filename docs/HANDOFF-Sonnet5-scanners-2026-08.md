# HANDOFF → Sonnet 5 — Chantiers Scanner (post-audit 05/08/2026)

**But de ce document** : permettre à Sonnet 5 d'exécuter les chantiers Scanner priorisés **sans reperdre le contexte**, en clonant un gabarit déjà livré et validé. À lire avec `docs/AUDIT-Scanner-2026-08.md` (le *quoi/pourquoi*) ; ce fichier-ci est le *comment*.

> **Règle d'or non négociable** : ce handoff décrit *comment* coder proprement quand un chantier est débloqué. Il ne débloque rien tout seul. Chaque nouveau scanner reste soumis au double filtre : **ADR-004 d'abord**, puis **deux signaux terrain indépendants** (FIELD-LOG §1). Pour les checks 🟢 *déterministes* (FP nul), la barre descend à *un* signal + démonstration du FP nul. Ne jamais coder sur la seule parole d'une IA.

---

## ⚡ DIRECTIVE D'AUTONOMIE — À LIRE EN PREMIER, À APPLIQUER À LA LETTRE

**Contexte de ta session (Sonnet 5, demain)** : Maxime **ne sera pas là** et **n'interviendra pas**. Tu tournes **seul, en effort maximum, sans jamais poser de question**. Toutes les décisions dont tu as besoin sont **déjà prises ci-dessous**. Ton travail est de la *pure exécution* d'une file cadrée, pas de la conception.

### Règles d'or de l'autonomie
1. **Ne pose AUCUNE question.** Si un point te semble ambigu, applique le *défaut documenté* (règles R1-R6 ci-dessous). Il y a toujours un défaut.
2. **Ne t'arrête JAMAIS pour attendre.** Si un item est réellement bloqué par une décision que tu n'as pas le droit de prendre (liste R3), **ne stoppe pas** : écris-le dans `FIELD-LOG.md` sous une section `## DÉCISIONS EN ATTENTE (pour Maxime)`, et **passe à l'item suivant de la file.** La session ne finit jamais « en attente » — elle finit par un récap.
3. **Un item = un commit sur le disque + une entrée FIELD-LOG.** Livre au fil de l'eau, ne garde rien pour la fin.
4. **Le vert est non négociable** : après chaque item, Core 128+/… et Repair 105/105, **Debug ET Release**. Un item n'est « fait » que vert. Si tu n'obtiens pas le vert, tu reviens en arrière (l'item reste non livré) et tu logges pourquoi.

### Décisions DÉJÀ PRISES (tu n'as pas à les reposer)
- **R1 — Knowledge.cs + Loc.cs : AUTORISÉ.** Tu **dois** ajouter, pour chaque nouveau code Warning/Critical (et rétroactivement pour `VPX_VERSION_OUTDATED`), une entrée `Knowledge.cs` (impact + cause) et `Loc.cs` (FR **et** EN), en calquant **exactement** le patron des entrées voisines. C'est additif et sûr. Vérifie chaque fichier App édité par un **parse Roslyn** (0 erreur CSxxxx) — l'App WPF ne compile pas sous Linux, donc c'est ta seule vérif syntaxique.
- **R2 — Scanners 🟢 déterministes : BUILD ET SHIP (le gel est LEVÉ).** Maxime a **levé le gel Scanner le 05/08** (« je sonne le dégel du gel ») — cette décision supersède le « SCANNER GELÉ » du 03/08 et `PROJECT-BRAIN` §7 (à reporter dans le Brain/un ADR). Tu **construis entièrement ET tu actives** les scanners marqués 🟢 *déterministes* : fichiers neufs + tests + Knowledge/Loc + **vraie ligne `.Add(new XxxScanner())`** dans `MainWindow.xaml.cs` (exactement comme le comparateur). Garde chaque ajout **atomique** (une ligne = un scanner) pour que Maxime puisse en désactiver un seul au besoin. ⚠️ **Ce qui n'est PAS levé** : la règle anti-faux-positif. Un check 🟢 *déterministe* a un FP nul par construction → OK pour ship. Un check 🟡 *heuristique* (jugement, FP possible) attend **toujours un signal terrain** avant d'être codé — ce n'était pas le gel qui le bloquait, c'est le risque de FP (la fausse alerte KPI#1 venait exactement de là). **🟢 → ship maintenant (Warning). 🟡 → shippables AUSSI, mais via la « Doctrine Note » (section juste après §0-bis) : émettre le FAIT dans le nouveau palier `Note`, jamais le jugement en Warning. Seuls vrais exclus : F3 quote-safety + le FIX core.vbs (ADR).**
- **R3 — STOPS NETS (tu ne touches JAMAIS, tu logges et tu passes)** : (a) les 12 fichiers scanners existants ; (b) l'Écran 2 / bouton Apply de Repair ; (c) le fix `B2S_ORPHAN` ; (d) la fonctionnalité auto-update ; (e) le **fix Repair core.vbs** (question ADR OSS non tranchée — la *détection* seule reste permise en `Note`, cf. Doctrine) ; (f) le scanner **F3 quote-safety** (non rendable sûr à coût raisonnable, FP même en `Note`). *(Les autres 🟡 ne sont plus des stops : ils se shippent en `Note` via la Doctrine ci-dessous — File Tier B.)*
- **R4 — Sévérité par défaut = Warning.** `Critical` uniquement sur une panne certaine et non-heuristique. Au moindre doute → **biais silence** (pas de Finding).
- **R5 — Zéro dépendance, fichiers neufs uniquement + la ligne commentée.** Jamais de NuGet. Jamais un fichier scanner existant modifié.
- **R6 — Gabarit obligatoire** : classe pure dans `Services/` + `IScanner` mince à I/O injectée dans `Scanning/` + fichier de tests neuf. Clone la structure du comparateur (`VpxVersionComparer` / `VpxVersionScanner` / `VpxVersionScannerTests`). Ne réinvente rien.

### TA FILE DE TRAVAIL DE DEMAIN (ordre strict, tout est débloqué ET activable)
> Tous 🟢 déterministes ou additifs → **zéro décision requise, ship autorisé** (dégel, R2). Chaque item = fichiers neufs + tests + Knowledge/Loc + **vraie ligne `.Add`** + vert Debug/Release + commit disque + entrée FIELD-LOG.

0. **Setup** : lis TRANSMISSION (MAJ la plus récente) → ce handoff → `docs/AUDIT-Scanner-2026-08.md`. Monte l'environnement (§2). Confirme le baseline **Core 140/140, Repair 105/105** avant de toucher quoi que ce soit.
1. **Knowledge.cs + Loc.cs pour `VPX_VERSION_OUTDATED`** (R1) — rétroactif. Parse Roslyn OK.
2. **E1 — VPMAlias Recursion** (🟢 trivial, échauffement) — cf. §4. Code `VPMALIAS_LOOP`.
3. **H1 — NVRAM 0-Byte** (🟢 trivial) — cf. §4. Code `NVRAM_EMPTY`.
4. **B1 — AltColor / SERum Pair Integrity** (🟢 forte valeur) — cf. §3. Ne signaler qu'une paire **réellement** incomplète pour une ROM **réellement** requise (croiser `ScriptAnalyzer.AnalyzeRomUsage`). Code `ALTCOLOR_INCOMPLETE`.
5. **B2 — AltSound Linter** (🟢 forte valeur) — cf. §3. Ne signaler que des samples **référencés-mais-absents** + erreur de syntaxe CSV. Code `ALTSOUND_SAMPLE_MISSING`.
6. **C1 — Screen Topology** (🟢 très forte valeur, plus complexe) — cf. §3. ⚠️ **SCOPE STRICT au déterministe** : ne signaler QUE « coordonnées hors de l'union de TOUS les écrans » (invisible = fait objectif). **JAMAIS** « mauvais écran / mauvais ordre » (heuristique → interdit, R3). Fichiers (`ScreenRes.txt`/`B2STableSettings.xml`) ou moniteurs illisibles → **silence**. Code `DISPLAY_OFFSCREEN`.
7. **G3 — Junction Health** (🟢) — cf. §4. Code `BROKEN_JUNCTION`.
8. **H2 — DirectB2S XML Malform** (🟢) — cf. §4. ⚠️ un `.directb2s` peut être du XML **brut OU compressé** — gère les deux ; illisible → **silence**. Code `B2S_MALFORMED`.
9. **F1 — PUPDatabase Orphan Playlist** (🟢) — cf. §4 (lecture SQLite via `SqliteReader`, ADR-007 respecté). Code `POPPER_ORPHAN_PLAYLIST`.
10. **Clôture** : TRANSMISSION (nouveau bloc « MAJ … (autonome Sonnet) ») récapitulant livré + la section `DÉCISIONS EN ATTENTE`. **Ne finis jamais en attente.**

**Puis, si tu épuises Tier A, enchaîne sur la File Tier B (🟡 rendus sûrs, sévérité `Note`)** — **d'abord le rendu App de `Note`** (prérequis, voir Doctrine), ensuite :
11. **D1 Audio** (Note) · 12. **C2 DPI** (Note) · 13. **A1 core.vbs détection** (Note, fix exclu) · 14. **B3 COM-probe** (Note) · 15. **G1 séparateur FR** (Note) · puis E2 / A2 / A3 (Note résumé). Même gabarit R6, Doctrine Note obligatoire, `.Add` activée, vert D+R, commit + FIELD-LOG.

Va **aussi loin que tu peux** (Tier A puis Tier B) ; chaque item est indépendamment expédiable ; ne t'arrête que file épuisée ou budget épuisé. **Seuls interdits** : F3 quote-safety et le *fix* core.vbs (R3). Reste de temps → Knowledge/Loc d'anciens codes ou renfort des tests des scanners livrés.

---

## 🟡 → SÛR : la « Doctrine Note » (shipper les heuristiques sans faux positif)

Réponse à « trouve comment éviter les FP sur les oranges » **+ « faut une nouvelle catégorie que Info, genre note »** (Maxime, 05/08).

**⚠️ NOUVEAU PALIER `Note`.** Un palier de sévérité `Severity.Note` a été ajouté **entre `Info` et `Warning`** — sa partie **Core est faite et verte cette session** (score-neutre, sous le seuil « watch », 4 tests). C'est le foyer honnête des findings heuristiques : on énonce le fait observé, l'utilisateur décide.
> **PRÉREQUIS avant tout scanner Tier B** : compléter le **rendu App** de `Note` — libellé (FR « À noter » / EN « Note »), couleur/icône distincte d'Info, présence dans les **6 exports** (écran, txt, md, BBCode, HTML, JSON) et le score/wording (jamais « FIX THIS FIRST »). **À faire AVANT le 1er scanner qui émet `Note`**, sinon un `switch` App non exhaustif sur `Severity` plante au runtime. Parse Roslyn de contrôle.
>
> *Terminologie : dans les fiches ci-dessous, tout « Info » associé à un check 🟡/heuristique se lit désormais `Note`. `Info` reste réservé aux confirmations neutres (« ROM trouvée »).*

**Les 5 règles** (même logique que `COMPAT_MIN_VERSION`, jusqu'ici émis en Info faute de palier dédié) :

1. **Sévérité `Note`, pas `Warning`.** `Note` (comme `Info`/`Ok`) **ne bouge pas le score** et ne déclenche **jamais** « FIX THIS FIRST » — il est sous le seuil « watch » de Warning. Un Finding `Note` est donc *structurellement incapable* de reproduire le désastre du 30/07 (un Warning qui plombe la note). **C'est le levier principal.**
2. **Énonce le FAIT, pas le verdict.** « Le périphérique audio par défaut est une sortie HDMI ‹X› » (fait) — jamais « ton audio est cassé » (jugement). L'utilisateur lit et décide.
3. **Escalade en `Warning` UNIQUEMENT sur une sous-condition déterministe** (ex. version `core.vbs` < plancher profil = comparaison de nombres).
4. **Résume les heuristiques par-table en UN finding compté** (patron `POPPER_MEDIA_MISSING`) — jamais une ligne par table → pas de bruit (2ᵉ leçon du 30/07 : le rapport FD à 2711 lignes info).
5. **Biais silence** sur tout échec de lecture/parse.

**Garde-fous par item 🟡 (tous en `Note`)** :
- **D1 Audio** : Info « périphérique par défaut = sortie HDMI/écran ‹X› ». Silence si device normal ou illisible. Gate optionnel : seulement si composant multi-écran présent (comme #11).
- **C2 DPI** : Info « mise à l'échelle Windows = {N} % » quand N ≠ 100. Jamais « ça casse ».
- **A1 core.vbs (détection seule)** : Info quand **deux copies de versions différentes** du même script partagé coexistent (fait), ou version locale < plancher profil. *Fix* bloqué ADR. Escalade Warning si version < plancher.
- **B3 dmddevice.ini COM** : Info quand un DMD matériel est `enabled` mais le port COM absent (fait).
- **E2 Registry/INI** : Info quand clés résiduelles `Visual PinMame` **et** `vpinmame.ini` coexistent (fait).
- **A2 Font / A3 Hardcoded-path** : Info **résumé compté**, uniquement un fichier nommé (`.ttf`, ou chemin absolu de load) **effectivement absent** (fait). Aucune devinette de nom.
- **G1 Séparateur FR** : Info « séparateur décimal Windows = ‹,› » (fait).

**Restent hors file (non rendables sûrs à coût raisonnable)** : **F3 quote-safety** (trop de formes valides, FP même en Info) ; le **fix** Repair `core.vbs` (question ADR OSS — la détection Info, elle, est permise).

---

## 0. État à la reprise (ce qui est DÉJÀ fait)

- ✅ **Comparateur de version VPX livré et vert** (cette session, 05/08). C'est le **gabarit de référence** de tout ce document.
  - Fichiers neufs : `src/PincabToolbox.Core/Services/VpxVersionComparer.cs` (pur), `src/PincabToolbox.Core/Scanning/VpxVersionScanner.cs` (IScanner), `tests/PincabToolbox.Core.Tests/VpxVersionScannerTests.cs`.
  - Une ligne dans `src/PincabToolbox.App/MainWindow.xaml.cs` : `.Add(new VpxVersionScanner())`.
  - Vert : **Core 140/140, Repair 105/105, Debug ET Release.**
  - ⚠️ **Loose end connu** : le code `VPX_VERSION_OUTDATED` (Warning) **n'a pas** d'entrée `Knowledge.cs` ni de traduction `Loc.cs` (resté dans le périmètre strict « fichiers neufs + 1 ligne »). Le Finding s'affiche via son `EnglishText` de repli (pas de crash), mais l'invariant « tout Warning/Critical documenté + traduit FR » demande ces deux ajouts → **voir §5, à valider par Maxime** (ce sont des fichiers App existants).

---

## 1. Le gabarit (recette pour TOUT nouveau Doctor)

Tout scanner suit exactement ce moule — celui du comparateur. **Ne pas inventer d'autre structure.**

1. **Une classe pure dans `Core/Services/`** (`XxxAnalyzer` / `XxxComparer`), zéro I/O, zéro dépendance → 100 % testable avec des entrées en dur. C'est là que vit la *décision*.
2. **Un `IScanner` dans `Core/Scanning/`** (`XxxScanner`) : I/O mince (lecture fichier/registre/PE), qui appelle la classe pure. **Injecter les I/O par le constructeur** (délégué avec défaut réel), comme `VpxVersionScanner(Func<string,string?>? reader = null)` et `UpdateWatcherScanner(vps)` → le chemin de décision se teste sans vrai binaire/registre Windows.
3. **Un fichier de tests neuf dans `tests/PincabToolbox.Core.Tests/`** — le `TestRunner` découvre par réflexion toute méthode `public static void Test_*` : **aucune modif du csproj ni des tests existants**.
4. **Une seule ligne** `.Add(new XxxScanner())` dans `MainWindow.xaml.cs` (l.325-338, la chaîne `new ScanEngine()...`). **C'est le seul point de composition à toucher côté App.**
5. **Entrées `Knowledge.cs` + `Loc.cs` (FR/EN)** pour chaque nouveau code Warning/Critical (voir §5 — fichiers App, périmètre à confirmer avec Maxime).

**Invariants à ne jamais casser** :
- **Zéro dépendance externe** dans Core (et Repair). BCL uniquement (`FileVersionInfo`, `System.Text.RegularExpressions`, etc.). Pas de NuGet.
- **Aucun fichier scanner existant modifié.** Uniquement des fichiers neufs + la ligne `.Add`. (Accord Maxime pour le Scanner ; **reconfirmer** pour tout chantier qui semble exiger de toucher l'existant.)
- **Discipline anti-FP** : version indétectable / entrée ambiguë → **silence**, jamais un Finding. Biais explicite vers le silence. Regarder comment `VpxVersionScanner` et `CompatibilityScanner` traitent chaque cas limite.
- **Impact score** : un nouveau `Warning` compte dans `ScanReport.Score` (rendements décroissants plafonnés −30) ; un `Critical` pèse −15 sans plafond et déclenche « FIX THIS FIRST ». **Ne mettre `Critical` que sur une panne certaine et non-heuristique.** Par défaut, préférer `Warning` (cf. justification du comparateur).

---

## 2. Environnement de build/vérif dans le sandbox cloud (reproductible)

Le SDK .NET **s'installe** dans le sandbox (Ubuntu 24.04 « noble ») :

```bash
apt-get update && apt-get install -y dotnet-sdk-8.0    # dispo dans noble/main ; dotnet 8.0.129 OK
```

Assembler une copie compilable (l'App WPF n'est PAS nécessaire pour les tests) :
1. `device_stage_files` de tout `src/PincabToolbox.Core`, `src/PincabToolbox.Repair`, `tests/PincabToolbox.Core.Tests`, `tests/PincabToolbox.Repair.Tests`, `profiles/`, `knowledge/`, `tests/fixtures/make_fixtures.py` (≈ 74 fichiers, 2 appels).
2. Copier vers `/tmp/pcb` en préservant l'arborescence. Ajouter un `NuGet.Config` avec `<packageSources><clear/></packageSources>` (restauration hors-ligne, projets zéro-dépendance).
3. Générer les fixtures : `cd /tmp/pcb && python3 tests/fixtures/make_fixtures.py` → `tests/fixtures/out/`.
4. Tester :
```bash
FIXTURES_DIR=/tmp/pcb/tests/fixtures/out dotnet run --project tests/PincabToolbox.Core.Tests   -c Release
FIXTURES_DIR=/tmp/pcb/tests/fixtures/out dotnet run --project tests/PincabToolbox.Repair.Tests -c Release
# répéter en -c Debug
```
5. **App WPF non compilable sous Linux** → édition + **revue structurelle** (XAML = XML valide, accolades/parenthèses de l'expression équilibrées, la ligne `.Add` calquée sur ses voisines, `using PincabToolbox.Core.Scanning;` déjà présent l.13). Maxime recompile via `build.cmd`. C'est la pratique établie du projet, pas un contournement.

**Écriture sur le disque de Maxime** : `SendUserFile` chaque fichier → récupérer le `file_uuid` → `device_commit_files` (fileUuid → devicePath). Re-stage frais juste avant si un fichier existant peut avoir bougé.

---

## 3. Fiches d'implémentation — batch P1 (à débloquer un par un)

> Ordre recommandé : **C1 → B1 → B2 → A1** (les 🟢 déterministes forte-valeur d'abord ; A1 le plus vendeur mais 🟡 et bloqué par une décision ADR). D1, G1 en parallèle possibles. **Chacune reste gatée** par le terrain.

### C1 — Screen Topology Check  *(🟢, forte valeur, débloque le trou volontaire du DisplaySetupScanner)*
- **Fichiers neufs** : `Services/ScreenTopologyAnalyzer.cs` (pur) + `Scanning/ScreenTopologyScanner.cs` (IScanner) + tests.
- **Décision pure** : entrées = (a) liste des rectangles moniteurs `List<(int x,int y,int w,int h)>`, (b) coordonnées backglass/DMD lues dans `ScreenRes.txt` et/ou `B2STableSettings.xml`. Sortie = Finding si une zone déclarée **ne recoupe aucun** rectangle moniteur (hors de l'union des écrans → invisible). Déterministe.
- **I/O (injectée)** : parser `ScreenRes.txt` (racine `Tables/`, format : lignes de coordonnées), `B2STableSettings.xml` (clés position/taille backglass), énumération moniteurs (`DisplayProbe` existe déjà pour le compte — étendre par un lecteur de rectangles injectable, ne PAS modifier `DisplayProbe.cs`, créer un helper neuf).
- **FP** : biaiser silence si `ScreenRes.txt`/`B2STableSettings.xml` absent ou illisible, ou si l'énumération moniteurs échoue (non-Windows). Ne signaler que « hors de TOUTE zone » (pas « sur le mauvais écran » — trop heuristique).
- **Tests** : coord dans l'union → silence ; coord hors union → Warning ; fichiers absents → silence ; moniteurs indétectables → silence.
- **Code** : `DISPLAY_OFFSCREEN` (Warning). **Knowledge + Loc** requis.
- **Preuve/gate** : VPForums 29802, wiki nailbuster ; forte. Chercher un 2ᵉ signal terrain avant de coder.

### B1 — AltColor / SERum Pair Integrity  *(🟢, forte valeur, seed Table Companion)*
- **Fichiers neufs** : `Services/AltColorInspector.cs` (pur) + `Scanning/AltColorScanner.cs` + tests.
- **Décision pure** : pour chaque `altcolor/<rom>/`, vérifier la présence des paires attendues (`.vni`+`.pal`, ou fichier Serum + `.pal`) ; signaler paire incomplète ou concordance 32/64 manquante. Déterministe.
- **I/O** : énumérer `VPinMAME/altcolor/*` (chemin dérivé de `InstallLayout.VPinMameDir`). Croiser avec les ROMs utilisées (réutiliser `ScriptAnalyzer.AnalyzeRomUsage`).
- **FP** : ne signaler que pour les ROMs **effectivement requises** par une table présente (pas tout le dossier altcolor). Silence si dossier absent.
- **Code** : `ALTCOLOR_INCOMPLETE` (Warning). Knowledge + Loc.
- **Preuve** : VPForums 53452, VPUniverse 10162, freezy #143, Pinball Nirvana. Forte.

### B2 — AltSound Structural Linter  *(🟢, forte valeur, seed Table Companion)*
- **Fichiers neufs** : `Services/AltSoundManifestLinter.cs` (pur) + `Scanning/AltSoundScanner.cs` + tests.
- **Décision pure** : parser `altsound/<rom>/altsound.csv` (ou `.ini` g-sound) → lister les échantillons référencés ; signaler ceux dont le fichier `.wav`/`.ogg` est absent, + erreurs de syntaxe CSV. Déterministe.
- **I/O** : lecture CSV + existence fichiers. **Parser CSV maison** (zéro dépendance).
- **FP** : silence si pas d'altsound.csv. Ne signaler que des fichiers **référencés-mais-absents** (fait objectif).
- **Code** : `ALTSOUND_SAMPLE_MISSING` (Warning). Knowledge + Loc.
- **Preuve** : corroborée (recherche 05/08). Forte.

### A1 — VBScript Shared-Script Doctor  *(🟡, LE plus vendeur, BLOQUÉ par une décision produit)*
- ⚠️ **NE PAS coder avant qu'un ADR tranche** : `core.vbs`/`controller.vbs` sont OSS vpinball — entrent-ils dans l'exception « dépendances open source » d'ADR-004 (pour que **Repair** puisse *fournir* le bon fichier) ? **Décision Maxime/CTO, pas Sonnet.** La *détection* seule (Scanner, lecture) est sûre ; c'est le *fix* qui touche ADR-004.
- **Fichiers neufs** (détection) : `Services/SharedScriptInspector.cs` (pur) + `Scanning/SharedScriptScanner.cs` + tests.
- **Décision pure** : présence de `core.vbs`/`controller.vbs`/`VPMKeys.vbs`/`nudge.vbs` locaux dans `Tables/` ; extraire leur version interne (chaîne de version en tête de fichier) ; comparer à un plancher connu (donnée de profil, PAS en dur). 🟡 (le jugement « périmé » est heuristique → deux signaux requis).
- **FP** : la *présence* est un fait ; la version « périmée » exige un plancher fiable. Biais silence si version illisible.
- **Code** : `SHARED_SCRIPT_OUTDATED` (Warning). Knowledge + Loc.
- **Preuve** : la plus forte de l'audit (VPForums 45350, vpinball #582/#1666). Mais **gate = décision ADR d'abord**.

### D1 — Audio Current-State  ·  G1 — FR Decimal-Separator  *(voir audit §4-D1/§4-G1)*
- **D1** : `Services/AudioStateEvaluator.cs` (pur : device par défaut = HDMI/écran ? endpoint activé ? volume 0 ?) + scanner à I/O injectée (Core Audio/MMDevice). Code `AUDIO_DEFAULT_SUSPECT` (Warning). **Boucle avec l'action Repair `set_default_audio_device` déjà codée** → détection = le Finding manquant. 🟡 (biais silence, on ne prédit pas le reset).
- **G1** : `Services/LocaleSeparatorCheck.cs` (pur) + scanner lisant `HKCU\Control Panel\International` (sDecimal/sList). Code `LOCALE_DECIMAL_SEPARATOR` (Warning/Info). **Différenciateur FR** — fort pour ton marché. 🟢/🟡, difficulté basse.

---

## 4. Batch P2 « quick wins » 🟢 (barre de preuve basse, FP nul)

À traiter quand P1 avance. Même gabarit. Tous déterministes :
- **E1 VPMAlias Recursion** — `Services/AliasGraph.cs` (détection de cycle A→B→A) réutilisant le parseur `AliasFile.cs` existant (lecture seule) ; code `VPMALIAS_LOOP` (Warning). Trivial, crash dur évité.
- **H1 NVRAM 0-Byte** — énumérer `VPinMAME/nvram/*.nv`, signaler taille 0 ; code `NVRAM_EMPTY` (Warning). *(Se limiter au 0-octet ; « taille ≠ spec » exclu.)*
- **H2 DirectB2S XML Malform** — extraire+parser le XML des `.directb2s` (le lecteur OLE/`CompoundFileReader` existe si besoin) ; code `B2S_MALFORMED` (Warning).
- **F1 PUPDatabase Orphan Playlist** — jointure `Games`×`Playlists` via `SqliteReader` (lecture, ADR-007 respecté) ; code `POPPER_ORPHAN_PLAYLIST` (Info/Warning).
- **G3 Junction Health** — points de jonction NTFS cassés (`FileAttributes.ReparsePoint` + cible inexistante) ; code `BROKEN_JUNCTION` (Warning).

---

## 5. Invariant Knowledge.cs + Loc.cs (à trancher avec Maxime)

Chaque nouveau code **Warning/Critical** devrait avoir : une entrée `src/PincabToolbox.App/Knowledge.cs` (impact + cause) et une traduction `src/PincabToolbox.App/Localization/Loc.cs` (FR **et** EN). **Ce sont des fichiers App existants** — hors du périmètre « fichiers neufs + 1 ligne » accordé pour le comparateur.

- **Aucun test ne le vérifie** (Core.Tests ne référence pas App) → l'omettre ne casse PAS le vert ; le Finding s'affiche via `EnglishText`.
- Mais l'invariant produit « tout Warning/Critical documenté + traduit FR » (BRAIN §7, vérif manuelle) le demande.
- **DÉCISION PRISE (règle R1, autonomie)** : **OUI, autorisé** — Sonnet ajoute ces 2 entrées par code, y compris **rétroactivement pour `VPX_VERSION_OUTDATED`**, en calquant le patron voisin, avec parse Roslyn de contrôle. C'est additif et sans risque. Plus de question à poser là-dessus.

---

## 6. Points de vigilance / risques résiduels

- **FP = ennemi n°1.** Un seul faux positif public tue la conversion (`PROJECT-BRAIN` §8). Tout 🟡 = deux signaux + biais silence.
- **Gel Scanner pré-v1.0** : rien de neuf au Scanner avant le test cab réel + v1.0. Ces fiches sont un **backlog post-lancement**.
- **ADR-004 premier filtre** : aucune de ces détections ne télécharge. Le seul point chaud = A1-fix (fournir core.vbs) → ADR à écrire d'abord.
- **Impact score/wording** : re-vérifier qu'un nouveau Warning ne refait pas le cas FD (une note bénigne présentée comme « FIX THIS FIRST »). Le modèle score plafonné + wording doux pour Warning est déjà en place — ne pas le contourner.
- **Bouton mise à jour (audit §8.4)** : chantier **infra distinct**, pas un Doctor. Canal Knowledge Pack (P1) avant canal binaire (P2, conditionné signature de code). Écrire un ADR premier-parti.
- **App non compilable ici** → toujours revue structurelle + build.cmd Maxime pour la ligne `.Add`.

---

## 7. Critères de validation (definition of done, par scanner)

- Fichiers **neufs** uniquement + 1 ligne `.Add` ; aucun scanner existant modifié.
- Tests : décision pure couverte (cas nominal + tous les cas limites → silence) + scanner à I/O injectée (présence → Finding, indétectable → silence, pas de FP).
- **Core 128+/… et Repair 105/105, Debug ET Release, tout vert.**
- Revue structurelle de la ligne `MainWindow.xaml.cs`.
- (Si acté §5) entrées Knowledge + Loc FR/EN.
- FIELD-LOG (décision + preuve + gate) et TRANSMISSION à jour ; fichiers réécrits sur le disque de Maxime (re-stage frais avant commit).

---

## 8. Opus/Maxime vs Sonnet 5 — répartition

**Restent à Maxime SEUL (hors file autonome — Sonnet ne les touche pas, cf. R3)** :
- Trancher l'**ADR core.vbs OSS** (débloque A1-fix).
- Acter **Table Companion** comme 2ᵉ produit et le **modèle de monétisation** par famille.
- Le **carve-out ADR auto-update** (§8.4 audit).
- Décider quels 🟡 passent le seuil « deux signaux terrain » et sont **activés** (les 🟢 sont déjà buildables sans lui, cf. R2).
- *(Le périmètre Knowledge/Loc est désormais tranché — R1 — plus une question ouverte.)*

**Délégable à Sonnet 5 (implémentation cadrée)** :
- Coder chaque Doctor débloqué **en suivant le gabarit §1** (le comparateur est le modèle exact à cloner).
- Les 🟢 déterministes P2 (E1, H1, H2, F1, G3) : quasi mécaniques, FP nul, forte valeur/effort.
- La recette build/test du §2 est reproductible → Sonnet peut livrer vert de bout en bout.

**En clair** : la *conception* et les *arbitrages produit/ADR* sont finis ou explicitement listés ici ; l'*implémentation* de chaque scanner débloqué est un exercice de clonage de gabarit que Sonnet 5 peut mener avec un minimum d'aller-retours.
