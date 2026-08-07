# TRANSMISSION — reprise Pincab Toolbox / FlipSync (session éco)  ·  MAJ 07/08/2026

## ⏱️ MAJ 07/08 — test terrain réel sur la cab de Maxime (2 scans) : bug confirmé (2 occurrences), KPI #1 toujours ouvert

> **Maxime a lancé le build Tier B sur sa vraie cab** (app Claude desktop installée en parallèle sur
> la machine, distincte de Pincab Toolbox) — 2 rapports HTML réels : `...0032` (racine `C:\`, par
> erreur) puis `...0040` (racine `C:\Visual Pinball`, corrigée). Cab confirmée par photo : tabletop/
> cocktail, écran unique, **aucun backglass**.
>
> **Bug confirmé par lecture directe du code** (pas juste déduit comme le 06/08) : `BlockedFileScanner.cs`
> (module `security`) protège l'APPEL à `Directory.EnumerateFiles(..., AllDirectories)` par try/catch
> mais pas le `foreach` de consommation qui suit — l'énumération est paresseuse, l'exception part
> pendant le `foreach`, non protégé. **Précision après relecture de `ScanEngine.cs`** : chaque scanner
> tourne déjà dans son propre try/catch au niveau moteur, donc ce bug ne plante PAS l'app — il fait
> juste échouer le module en un `SCANNER_ERROR` (Warning technique brut) au lieu du résultat normal.
> A échoué ainsi sur `C:\Documents and Settings` (jonction NTFS système) quand la racine était `C:\`.
> **Deuxième occurrence identique trouvée** : `CompletenessScanner.CollectWheelStems` a le même patron
> exact (risque plus faible en pratique, mais même bug de fond). `LayoutDetector.SafeEnumerateDirs`/
> `SafeEnumerateFiles` ont déjà le bon patron à répliquer. **Aucun correctif fait** — 2 scanners
> EXISTANTS, jamais touchés sans ton feu vert explicite (toujours sans réponse depuis le 06/08).
> Racine corrigée (00:40) confirme le diagnostic : module `security` propre, aucun `SCANNER_ERROR`.
>
> **8 `ROM_MISSING` critical identiques au relevé du 04/08**, deux sessions distinctes, même liste
> stable — cohérence forte, mais ne tranche toujours pas KPI #1 (originales/homebrew vs vrais hacks).
> **Score 0/100·F revérifié conforme à la formule** (8×15=120 > 100, plancher attendu) — pas un bug.
> **Constat produit, pas un bug** : cab sans backglass confirmée → ~205 findings B2S_MISSING/ORPHAN
> structurellement inévitables sur ce cab précis — piste de dé-emphase notée, pas codée, pas urgente.
>
> Détail complet (root cause ligne par ligne, tous les codes du scan, actions Maxime) :
> `knowledge/FIELD-LOG.md`, entrée du 07/08.
>
> **3 décisions ajoutées à la liste ci-dessous (#11-13), toutes sans réponse.** Rien codé cette
> session (vérification + terrain, pas un chantier). **Action Maxime** : re-scanner racine = dossier
> parent commun (Visual Pinball + PinUP Popper) pour un rapport complet en un coup ; confirmer le
> `git push` des 2 commits Tier B (`14894ed`, `1ab33fc`) — pas de capture reçue depuis.

---

## ⏱️ MAJ 06/08 (ter) — build Windows réel cassé puis réparé + Tier B livrée (5/5)

> **Deux captures d'écran de `build.cmd` sur ta machine, sans texte.** La première montrait un
> **BUILD FAILED** : `CS0103` sur `Path`/`File` dans `RepairOfferBuilder.cs` (lignes 29-30 et 69),
> plus un warning cosmétique `CA1416` dans `MonitorTopologyProbe.cs` (sans impact, inchangé).
>
> **Diagnostic** : `RepairOfferBuilder.cs` utilise `Path`/`File` (`System.IO`) sans avoir déclaré
> `using System.IO;` — un bug **préexistant du 04/08** (date de création du fichier), pas introduit
> cette session. Jamais vu avant parce que la seule vérification possible sur les fichiers App dans
> ce sandbox est un **parse Roslyn syntaxe-seule** (`/tmp/roslyn-check`, mêmes DLL que le SDK) — ça ne
> détecte QUE les erreurs de syntaxe (`CSxxxx` de parsing), jamais une erreur de résolution de symbole
> comme un using manquant, qui n'existe qu'à la compilation réelle. Autrement dit : ce sandbox ne
> pouvait structurellement pas voir ce bug avant que tu lances un vrai `build.cmd` sur Windows.
>
> **Corrigé, puis balayage préventif de tout le projet App** plutôt que de patcher seulement le
> fichier signalé : `RepairOfferBuilder.cs` (le bug réel) + `Loc.cs` et `Scenarios.cs` (même motif
> exact trouvé par un script de recoupement usages-LINQ/IO vs `using` déclarés — 2 bugs **identiques,
> pas encore remontés dans un rapport de build**) + `MainWindow.xaml.cs` (durci par précaution, motif
> similaire mais pas confirmé cassé). Logique : ajouter un `using` en trop coûte zéro, l'omettre s'il
> est réellement nécessaire peut casser un autre build. **Core 279/279 + Repair 105/105 (Debug ET
> Release) revérifiés inchangés** (fichiers App, aucun test n'y touche). Livré sur ton disque, commit
> local `83f9799`. **Ta deuxième capture confirme : build complet, `publish\PincabToolbox.exe`
> produit.** ✅
>
> **Repris Tier B ensuite** (tu avais dit vouloir tester une fois fini) — **5/5 livrables livrés** :
> - **D1 `AUDIO_DEFAULT_SUSPECT`** (Note) — périphérique de lecture par défaut au nom évocateur d'une
>   sortie écran/HDMI. Nouveau lecteur COM read-only côté Core (`AudioEndpointReader`), structurellement
>   incapable d'écrire le périphérique (`IPolicyConfig` absent du fichier). Détection seule — PAS
>   branché sur l'action Repair `set_default_audio_device` déjà codée, il manque un nom de
>   périphérique cible que je ne peux pas deviner pour un cab arbitraire.
> - **C2 `DPI_SCALING_NONSTANDARD`** (Note) — lecture registre `AppliedDPI`, Note si ≠ 96 (100 %).
> - **B3 `DMD_COM_PORT_NOT_FOUND`** (Note) — dmddevice.ini (parseur INI maison) déclare un port COM
>   absent de `HKLM\...\SERIALCOMM` (nouvelle énumération `RegEnumValue`). ⚠️ Nom de clé INI du port
>   supposé (`port`/`comport`/`com_port`/`serialport`), faute de fichier réel pour vérifier — voir
>   DÉCISIONS EN ATTENTE #9.
> - **G1 `LOCALE_DECIMAL_SEPARATOR`** (Note) — séparateur décimal ≠ "." via `CultureInfo` (déviation
>   loggée : plus sûr/simple qu'une lecture registre directe, même fait constaté).
> - **E2 `VPINMAME_CONFIG_PHANTOM`** (Note) — clé registre VPinMAME ET `.ini` présents en même temps
>   (nouveau lecteur étroit, `VpinmameRegistry.cs` existant non touché).
>
> **A1 (Script Doctor) reste reporté — décision motivée** : la fiche demande un plancher de version
> par script en donnée de profil, qui n'existe pas dans `Profile.cs` aujourd'hui — deviner cette
> valeur serait exactement le genre de supposition que ce projet évite. Détection sans comparaison
> produirait un Note creux (« core.vbs présent, v4.5 ») sans valeur utilisateur réelle. A2/A3
> restent reportés aussi (sous-spécifiés). Voir DÉCISIONS EN ATTENTE #10.
>
> **Core 279→321/321 (+42 tests), Repair 105/105, Debug ET Release, tout vert dès le premier run.**
> Roslyn 0 erreur sur les 3 fichiers App touchés. Un seul commit consolidé (`14894ed`, pas 5 séparés
> comme Tier A — les 3 fichiers App touchent les 5 codes à la fois, les séparer aurait été artificiel).
> Détail complet par code : `knowledge/FIELD-LOG.md`, entrée du 06/08, « Item 12 ».
>
> **Revue CTO + Product faite avant clôture** (consigne permanente) : code propre (même gabarit tenu
> à l'identique sur 5 items de plus, aucune régression), architecture cohérente (composition toujours
> unique, aucun scanner existant modifié, premiers checks `Note` du projet en dehors du prérequis de
> rendu), tests suffisants pour la logique pure et le câblage I/O (la sûreté COM/registre réelle reste
> non vérifiable en sandbox Linux — inhérent, pas un trou de cette session), vraie valeur utilisateur
> réelle mais plus modeste que Tier A (5 Notes informatifs, pas des Warnings qui bougent le score —
> c'est voulu, c'est la doctrine), risque commercial faible (Note ne peut pas remettre un F, ne peut
> pas déclencher « FIX THIS FIRST », donc aucun risque de répéter le cas FD), aucun risque technique
> neuf identifié. **Amélioration à coût faible repérée, non codée** : confirmer le nom de clé INI de
> B3 sur un vrai `dmddevice.ini` (5 minutes si tu en as un sous la main) fiabiliserait ce check sans
> toucher au code lui-même si le nom devine juste.
>
> **Git (action Maxime)** :
> ```
> git add src/PincabToolbox.App/RepairOfferBuilder.cs src/PincabToolbox.App/Localization/Loc.cs src/PincabToolbox.App/Scenarios.cs src/PincabToolbox.App/MainWindow.xaml.cs src/PincabToolbox.App/Knowledge.cs src/PincabToolbox.Core/Scanning/AudioStateScanner.cs src/PincabToolbox.Core/Scanning/ConfigPhantomScanner.cs src/PincabToolbox.Core/Scanning/DmdComPortScanner.cs src/PincabToolbox.Core/Scanning/DpiScalingScanner.cs src/PincabToolbox.Core/Scanning/LocaleSeparatorScanner.cs src/PincabToolbox.Core/Services/AudioEndpointReader.cs src/PincabToolbox.Core/Services/AudioStateEvaluator.cs src/PincabToolbox.Core/Services/DmdDeviceIniParser.cs src/PincabToolbox.Core/Services/DpiRegistry.cs src/PincabToolbox.Core/Services/DpiScalingEvaluator.cs src/PincabToolbox.Core/Services/LocaleSeparatorCheck.cs src/PincabToolbox.Core/Services/SerialPortRegistry.cs src/PincabToolbox.Core/Services/VpinmameKeyProbe.cs tests/PincabToolbox.Core.Tests/AudioStateScannerTests.cs tests/PincabToolbox.Core.Tests/ConfigPhantomScannerTests.cs tests/PincabToolbox.Core.Tests/DmdComPortScannerTests.cs tests/PincabToolbox.Core.Tests/DpiScalingScannerTests.cs tests/PincabToolbox.Core.Tests/LocaleSeparatorScannerTests.cs docs/PROJECT-BRAIN.md knowledge/FIELD-LOG.md TRANSMISSION.md
> git commit -m "fix(app): using manquants (build casse) + feat(scanner): Tier B (5/5), tous Severity.Note"
> git push origin main
> ```

---

## ⏱️ MAJ 06/08 (bis) — push GitHub confirmé + les 3 améliorations à coût faible faites

> **Maxime revenu actif** après la clôture ci-dessous : « fais les 3 amélioration a cout faible et
> c'est poussé sur git tu peux verifier ». Vérification faite avant tout code.
>
> **Push confirmé** : `git fetch origin main` depuis le sandbox (la lecture réseau vers GitHub n'est
> pas bloquée, seul le push l'est) → `origin/main` à `403f3d5`, auteur Maxime Chauvin, message de
> commit identique à celui fourni. `git diff --stat` contre la base de session confirme les **34
> fichiers exacts** déjà sur GitHub. **Rien à repousser, c'est bien en ligne.**
>
> **#1 fait** — les 6 `cat.*` manquants ajoutés à `Loc.cs` (En+Fr) : `legacy`, `disk`, `process`,
> `display`, `media-orphan`, `vpxversion`. La colonne Module de ces 6 scanners affiche maintenant un
> libellé au lieu du code brut.
>
> **#2 fait — documenté plutôt que câblé, choix délibéré.** Câbler `AutoFixable` sur un vrai signal
> aurait été faux, pas juste plus cher : la fixabilité dépend de l'état runtime (licence, préflight,
> bugs par action) qu'un bool statique par code ne peut pas représenter — exactement pourquoi
> `RepairOfferBuilder` existe en dehors de ce flag. Doc-comment clair ajouté à la place (vestigial,
> zéro lecteur vérifié .cs **et** .xaml, pointe vers `RepairOfferBuilder`). Risque nul, commentaire
> seul.
>
> **#3 pas codable** (action terrain) — message rédigé pour Gregg/itchigo (anglais, registre
> VPForums), à envoyer par Maxime, demandant de tester C1/H2/F1 en priorité sur le prochain build.
> Donné dans le chat.
>
> **Core 279/279, Repair 105/105, Debug ET Release, Roslyn 0 erreur** sur les 2 fichiers touchés
> (`Knowledge.cs`, `Loc.cs`). Détail complet : FIELD-LOG, entrée du 06/08, Item 11.
>
> **Git (action Maxime)** :
> ```
> git add src/PincabToolbox.App/Knowledge.cs src/PincabToolbox.App/Localization/Loc.cs knowledge/FIELD-LOG.md TRANSMISSION.md
> git commit -m "chore(knowledge): cat.* manquants + AutoFixable documente comme vestigial"
> git push origin main
> ```

---

## ⏱️ MAJ 06/08 (autonome Sonnet 5) — file Tier A du handoff LIVRÉE (8/8), dégel formalisé en ADR-010

> **Session autonome, Maxime absent toute la session** (« Tu avances SEUL »), exécutée sur
> `docs/HANDOFF-Sonnet5-scanners-2026-08.md` dans l'ordre prescrit, zéro question posée. Baseline
> reconfirmée avant tout code (Core 144/144, Repair 105/105) puis file Tier A **exécutée
> intégralement, 8/8** : `VPMALIAS_LOOP` (E1) · `NVRAM_EMPTY` (H1) · `ALTCOLOR_INCOMPLETE` (B1) ·
> `ALTSOUND_SAMPLE_MISSING` (B2) · `DISPLAY_OFFSCREEN` (C1) · `BROKEN_JUNCTION` (G3) ·
> `B2S_MALFORMED` (H2) · `POPPER_ORPHAN_PLAYLIST` (F1) — plus le rétroactif R1
> (Knowledge/Loc pour `VPX_VERSION_OUTDATED`) et le rendu App du palier `Severity.Note`
> (prérequis Tier B, 3 vrais bugs de rendu trouvés et corrigés au passage : `Note` retombait
> silencieusement sur le bucket `Ok` dans 3 switchs non-exhaustifs, y compris l'export Markdown
> forum — le plus utilisé). **Gabarit du comparateur cloné à l'identique sur les 8 items** (pur en
> `Services/` + `IScanner` mince en `Scanning/` + tests + Knowledge/Loc + une ligne `.Add`), aucun
> des 21 scanners jamais touché. **Core 144→279/279 (+135 tests), Repair 105/105 stable, Debug ET
> Release à chaque étape, revérifié une dernière fois à la clôture.** Écrit sur le disque au fil de
> l'eau, 10 commits locaux atomiques dans le sandbox (`a57d414`→`21e4e46`, un par item).
>
> **Quatre fois cette session, une recherche primaire-source a été faite avant d'écrire le moindre
> parseur** (jamais deviné un format sur la seule parole du handoff) — deux fois, ça a corrigé une
> prémisse du handoff lui-même : `B2STableSettings.xml` ne contient **aucune** donnée de position
> (toute la géométrie vit dans `ScreenRes.txt`/`.res`, C1 reconstruit en conséquence) ; aucune preuve
> qu'un `.directb2s` compressé (OLE) existe réellement (H2 le reconnaît sans jamais le décoder). Une
> fois, ça a comblé un vrai trou de doc du dépôt : le schéma `Playlists`/`PlayListDetails` de
> `PUPDatabase.db` (F1) n'était documenté nulle part ici, confirmé via le wiki du créateur de PinUP
> Popper lui-même. Détail complet, par item : `knowledge/FIELD-LOG.md`, entrée du 06/08.
>
> **DÉGEL FORMALISÉ — `docs/adr/ADR-010-degel-scanner-doctrine-note.md` écrit.** Reportait une
> décision déjà prise par Maxime le 05/08 (« je sonne le dégel du gel ») mais jamais montée en ADR.
> Fixe la règle d'entrée pour tout futur check : **🟢 déterministe → ship direct, le FP nul
> démontrable remplace le gate « deux signaux terrain »** (c'est la porte qu'a empruntée toute la
> file de cette session) ; **🟡 heuristique → doit passer par `Severity.Note`** (score-neutre, jamais
> « FIX THIS FIRST ») avant tout ship. `PROJECT-BRAIN` §6 (279/105, 21 scanners listés) et §7 (ancien
> gel marqué supersédé, pas supprimé) mis à jour en conséquence.
>
> **Sur consigne explicite reçue en cours de session (« termine »), la file Tier B n'a pas été
> attaquée** (D1 audio · C2 DPI · A1 core.vbs détection · B3 COM-probe · G1 séparateur FR · puis
> E2/A2/A3) — reportée, pas abandonnée : le prérequis (rendu `Note`) est déjà livré, donc le premier
> item Tier B d'une prochaine session n'a plus aucun prérequis à lever. Décision de cadrage loguée,
> pas silencieuse.
>
> **Revue CTO + Product faite avant clôture** (consigne permanente de Maxime) — verdict résumé : code
> propre (gabarit tenu à l'identique, une régression cosmétique trouvée et corrigée en cours de
> route), architecture cohérente (composition toujours unique, aucun scanner existant modifié),
> 135 tests neufs avec limite assumée (I/O Windows réelle non testable en sandbox Linux, comme tout
> scanner Windows précédent du projet), vraie valeur utilisateur (pannes invisibles réelles et
> courantes en pincab), risque commercial concentré sur 3 items à prémisse *corrigée* par recherche
> plutôt que confirmée terrain (C1/H2/F1 — silence-biaisés mais à surveiller en priorité sur le
> prochain retour réel). Trois améliorations à faible coût identifiées, **aucune codée** (voir
> DÉCISIONS EN ATTENTE). Détail complet : FIELD-LOG, même entrée, section « Revue CTO + Product ».
>
> **Git (action Maxime — le proxy bloque le push depuis le sandbox, comme d'habitude)** — un seul
> commit suffit, tous les fichiers sont déjà à jour sur ton disque :
> ```
> git add knowledge/FIELD-LOG.md src/PincabToolbox.App/App.xaml src/PincabToolbox.App/Knowledge.cs src/PincabToolbox.App/Localization/Loc.cs src/PincabToolbox.App/MainWindow.xaml src/PincabToolbox.App/MainWindow.xaml.cs src/PincabToolbox.Core/Scanning/AliasLoopScanner.cs src/PincabToolbox.Core/Scanning/AltColorScanner.cs src/PincabToolbox.Core/Scanning/AltSoundScanner.cs src/PincabToolbox.Core/Scanning/DirectB2sScanner.cs src/PincabToolbox.Core/Scanning/JunctionScanner.cs src/PincabToolbox.Core/Scanning/NvramScanner.cs src/PincabToolbox.Core/Scanning/PopperPlaylistScanner.cs src/PincabToolbox.Core/Scanning/ScreenTopologyScanner.cs src/PincabToolbox.Core/Services/AliasGraph.cs src/PincabToolbox.Core/Services/AltColorInspector.cs src/PincabToolbox.Core/Services/AltSoundManifestLinter.cs src/PincabToolbox.Core/Services/DirectB2SValidator.cs src/PincabToolbox.Core/Services/JunctionInspector.cs src/PincabToolbox.Core/Services/MonitorTopologyProbe.cs src/PincabToolbox.Core/Services/NvramInspector.cs src/PincabToolbox.Core/Services/PlaylistIntegrityInspector.cs src/PincabToolbox.Core/Services/ScreenTopologyAnalyzer.cs tests/PincabToolbox.Core.Tests/AliasLoopScannerTests.cs tests/PincabToolbox.Core.Tests/AltColorScannerTests.cs tests/PincabToolbox.Core.Tests/AltSoundScannerTests.cs tests/PincabToolbox.Core.Tests/DirectB2sScannerTests.cs tests/PincabToolbox.Core.Tests/JunctionScannerTests.cs tests/PincabToolbox.Core.Tests/NvramScannerTests.cs tests/PincabToolbox.Core.Tests/PopperPlaylistScannerTests.cs tests/PincabToolbox.Core.Tests/ScreenTopologyScannerTests.cs docs/PROJECT-BRAIN.md docs/adr/ADR-010-degel-scanner-doctrine-note.md TRANSMISSION.md
> git commit -m "feat(scanner): file Tier A du handoff (8 checks deterministes) + degel formalise en ADR-010"
> git push origin main
> ```

---

## 🗂️ DÉCISIONS EN ATTENTE (pour Maxime) — liste vivante, mise à jour à chaque session

Rien n'a bloqué la file (aucun item n'a été laissé inachevé) — ce sont des améliorations à faible
coût repérées en cours de route, non codées hors mandat, consolidées ici plutôt que dispersées.
Détail complet par point : `knowledge/FIELD-LOG.md`, section « DÉCISIONS EN ATTENTE ».

1. ✅ **FAIT (MAJ 06/08 bis)** — ~~6 scanners pré-existants sans entrée `cat.*` dans `Loc.cs`~~.
2. ✅ **FAIT (MAJ 06/08 bis)** — ~~`Knowledge.KnowledgeEntry.AutoFixable` est un flag mort~~ — documenté comme vestigial (pas câblé, choix délibéré — voir raisonnement ci-dessus).
3. Format legacy `.ini` (g-sound) pour AltSound — aucun schéma vérifiable trouvé, non couvert.
4. DLL 32/64-bit de colorisation (B1) — aucun nom de fichier distinct confirmé au-delà de `BitnessScanner`.
5. Position DMD (C1) — deux lectures possibles de la doc officielle, jamais recoupées ; non vérifiée (seul le backglass l'est).
6. `.directb2s` compressé (H2) — silence, jamais décodé (aucune preuve qu'une telle variante existe réellement).
7. Sémantique `isFav=2` et nom de colonne « titre » sur `Playlists` (F1) — non confirmés, exclus par prudence.
8. ✅ **FAIT (MAJ 06/08 ter)** — ~~File Tier B entièrement reportée~~ — D1/C2/B3/G1/E2 livrés (5/5, commit `14894ed`). A1/A2/A3 restent reportés, voir #9-10.
9. **B3 — nom de clé INI du port COM non confirmé** sur un vrai `dmddevice.ini` (`port`/`comport`/`com_port`/`serialport` tous acceptés, par prudence). Sans risque (silence si aucun ne matche), mais peut sous-détecter. Action à coût quasi nul : coller un `dmddevice.ini` réel dans le chat, ou juste confirmer le nom de clé.
10. **A1 Script Doctor bloqué par l'absence d'un plancher de version en donnée de profil** — débloquable en ajoutant un champ à `profiles/vpx-popper.json` (ex. `sharedScriptFloors`) avec, par script partagé (`core.vbs`, `controller.vbs`, `VPMKeys.vbs`, `nudge.vbs`), la version en-dessous de laquelle le déclarer périmé. Jugement métier que je ne peux pas deviner — une fois les valeurs données, c'est une session courte.
11. **Bug confirmé (07/08) — énumération paresseuse non protégée dans 2 scanners existants** (`BlockedFileScanner.cs` module `security`, `CompletenessScanner.CollectWheelStems`) — try/catch sur l'appel `Directory.Enumerate*(..., AllDirectories)` mais pas sur le `foreach` de consommation qui suit ; échoue (`SCANNER_ERROR`, contenu par `ScanEngine` — pas un crash d'app) sur une jonction système (`C:\Documents and Settings`). Patron correct déjà dans `LayoutDetector` à répliquer. **Décision requise avant tout correctif** (2 scanners existants) — sans réponse depuis le 06/08.
12. **KPI #1 toujours ouvert** — les 8 `ROM_MISSING` critical (Blood Machines, hpgf-052-DOF, Jurassic Park, leprechaun, Munsters 2020, Stranger Things SE, The Goonies, Willy Wonka Pro) sont-ils de vrais hacks ROM ou des originales/homebrew ? Même liste stable sur 2 sessions (04/08, 07/08). Un seul cas vérifié tranche pour tous.
13. **[Basse priorité] Dé-emphase `B2S_MISSING`/`B2S_ORPHAN` pour cabs sans backglass** — constat du cab réel de Maxime (~205 findings structurellement inévitables sans backglass). Idée produit seulement, pas codée, pas urgente.

---

## ⏱️ MAJ 05/08 (5) — comparateur VPX LIVRÉ (vert) + audit Scanner + handoff Sonnet 5 autonome + DÉGEL du Scanner

> **Mission 1 — comparateur de version VPX : LIVRÉ ET VERT.** Nouveau scanner qui compare la version VPX
> **installée** (lue au PE via `FileVersionInfo`) à la version **requise déclarée** par chaque table, et
> n'émet un `Warning` (`VPX_VERSION_OUTDATED`) que sur un vrai manque (installée < requise) — silence si
> installée indétectable, >=, ou pas de requirement. Même discipline anti-FP que `COMPAT_MIN_VERSION` (le
> faux positif du 30/07 est verrouillé par un test dédié). **3 fichiers neufs** :
> `Core/Services/VpxVersionComparer.cs` (pur), `Core/Scanning/VpxVersionScanner.cs` (IScanner, lecteur PE
> injectable), `tests/Core.Tests/VpxVersionScannerTests.cs` (12 tests). **1 ligne** dans `MainWindow.xaml.cs`.
> Aucun scanner existant touché. **Core 140/140 + Repair 105/105, Debug ET Release** (SDK installé dans le
> sandbox : `apt-get install dotnet-sdk-8.0`). Écrit sur le disque. ⚠️ **Loose end** : `VPX_VERSION_OUTDATED`
> n'a pas encore d'entrée `Knowledge.cs` / `Loc.cs` FR-EN (périmètre strict tenu) — le Finding s'affiche via
> son `EnglishText`. À compléter par Sonnet (R1 du handoff).
>
> **Mission 2 — audit Scanner + vision produit : LIVRÉ** (`docs/AUDIT-Scanner-2026-08.md`). Reste-t-il des
> catégories non détectées ? **Oui, 6** : scripts partagés (core.vbs) ; topologie d'affichage réelle
> (ScreenRes+B2STableSettings) ; colorisation/altsound ; état audio ; résidus Freezy ; hygiène système FR.
> Ancré CODE réel des 12 scanners + FIELD-LOG + corroboration terrain, **pas** web seul. 2 salves Gemini
> arbitrées « pépite/glaise » (bonne pêche cette fois). Priorisation P0-P3, monétisation par ligne (Table
> Companion = meilleur 2ᵉ produit). §8.4 = bouton de MAJ (infra ; canal Knowledge Pack = valeur ADR-002 ;
> canal binaire conditionné à la signature de code).
>
> **DÉGEL DU SCANNER (décision Maxime 05/08 — « je sonne le dégel du gel »).** Supersède « SCANNER GELÉ
> 03/08 » + `PROJECT-BRAIN` §7 (**à reporter dans le Brain + un ADR**). On rouvre le Scanner. **Nuance CTO** :
> le dégel lève le gel de *calendrier*, PAS la règle anti-FP. → **🟢 déterministes shippés en `Warning` ;
> 🟡 heuristiques shippés AUSSI via la « doctrine Note »** — **nouveau palier `Severity.Note`** ajouté à la
> demande de Maxime (« une catégorie que Info, genre note »), entre `Info` et `Warning`, score-neutre et
> jamais « FIX THIS FIRST ». **Core livré vert cette session (144/144, 4 tests dédiés)** ; **reste le rendu
> App de `Note`** (libellé FR/EN, couleur, 6 exports) = prérequis Sonnet avant le 1er scanner Tier B.
> Émettre le fait en `Note`, escalade `Warning` seulement sur du déterministe, résumer par-table.
> Irréductibles hors file : **F3 quote-safety** et le **fix** Repair core.vbs (ADR OSS).
>
> **Mission 3 — handoff Sonnet 5 AUTONOME : LIVRÉ** (`docs/HANDOFF-Sonnet5-scanners-2026-08.md`). Cadré pour
> tourner **seul, effort max, ZÉRO question, sans jamais s'arrêter** : directive d'autonomie, décisions
> pré-tranchées (R1-R6), recette build sandbox, gabarit = le comparateur, **file ordonnée** Tier A (🟢, ship
> Warning : E1 VPMAlias · H1 NVRAM 0-octet · B1 AltColor · B2 AltSound · C1 Screen-Topology *scope
> déterministe* · G3 Junctions · H2 directb2s XML · F1 PUPDatabase orphelin) puis Tier B (🟡 Info : D1 audio ·
> C2 DPI · A1 core.vbs détection · B3 COM-probe · G1 séparateur FR · E2/A2/A3). Protocole « si bloqué → logge
> dans DÉCISIONS EN ATTENTE et passe au suivant ». **Lancer la session de demain en Sonnet effort max, pointée
> sur ce handoff.**
>
> **À formaliser (Maxime/ADR)** : (1) reporter le **dégel** ; (2) **ADR core.vbs OSS** (débloque le fix
> payant) ; (3) acter **Table Companion** 2ᵉ produit ; (4) **ADR carve-out auto-update** premier-parti.
>
> **Git (action Maxime — le proxy bloque le repo depuis le sandbox)** :
> ```
> git add src/PincabToolbox.Core/Services/VpxVersionComparer.cs src/PincabToolbox.Core/Scanning/VpxVersionScanner.cs src/PincabToolbox.Core/Models/Finding.cs tests/PincabToolbox.Core.Tests/VpxVersionScannerTests.cs src/PincabToolbox.App/MainWindow.xaml.cs docs/AUDIT-Scanner-2026-08.md docs/HANDOFF-Sonnet5-scanners-2026-08.md knowledge/FIELD-LOG.md TRANSMISSION.md
> git commit -m "feat: comparateur version VPX + audit Scanner + handoff Sonnet 5 + degel Scanner"
> git push origin main
> ```

---

## ⏱️ MAJ 05/08 (4) — sync GitHub résolue, inventaire Scanner fait, prochain chantier identifié

> **Push GitHub résolu** : Maxime a lancé les 3 commandes depuis sa machine (verrou `.git/index.lock`
> périmé supprimé au passage), commit `749ec4d` poussé avec succès sur `waylo1/pincab-toolbox`. Le
> dépôt distant est à jour. Plus aucune action git en attente.
>
> **Erreur de méthode corrigée** : la recherche produit "6 idées" de tout à l'heure avait été faite
> par recherche web seule, sans vérifier le code existant d'abord — **5 des 6 idées existent déjà**
> dans le Scanner (lecture seule, en prod) : `B2S_MISSING`/`B2S_ORPHAN` (idée backglass),
> `POPPER_NOT_REGISTERED`/`POPPER_MEDIA_MISSING` (idée base Popper — lit déjà `PUPDatabase.db` en
> SQLite pur, lecteur maison sans dépendance), `POPPER_MEDIA_MISSING` encore (idée wheel/médias),
> registre VPinMAME déjà lu (`VpinmameRegistry.cs`) pour localiser le dossier roms (idée mapping
> ROM). Inventaire complet des 12 scanners existants donné à Maxime dans le chat (Scanner en lecture
> seule : ROM Validator, Install Auditor, Orphaned Media, Compatibility Linter, Bitness Doctor,
> Dependency Check, Legacy Tables, Blocked-file check, Disk Space, Stuck Processes, Display Setup,
> Update Watcher).
>
> **Le vrai chantier identifié — pas encore codé** : `CompatibilityScanner.cs` extrait déjà la
> version VPX qu'une table déclare requérir (`COMPAT_MIN_VERSION`) mais compare exprès jamais à la
> version VPX réellement installée — le commentaire du fichier dit littéralement que c'est à faire
> "quand on saura lire la version installée" (un faux positif avait cassé un rapport en juillet
> 2026, FIELD-LOG 2026-07-30). C'est le morceau manquant, sûr et cadré. **Prochaine étape : coder ce
> comparateur dans un fichier neuf, sans toucher `CompatibilityScanner.cs`** (accord Maxime du 05/08
> : nouveaux fichiers, existant jamais touché), le brancher dans `ScanEngine` via un `.Add(...)`
> supplémentaire dans `MainWindow.xaml.cs` (le seul endroit à toucher côté composition).
>
> **Deuxième piste, pas encore designée** : vendre un vrai correctif Repair pour `B2S_ORPHAN`
> (backglass orphelin, mismatch de nom) — aucun fix payant derrière aujourd'hui. Attention : la
> plupart des orphelins sont des restes, pas des matchs cachés — un renommage automatique à
> l'aveugle casserait des installs. Ça demande un choix affiché à l'utilisateur, pas un automatisme
> silencieux — design à montrer à Maxime avant de câbler quoi que ce soit.
>
> **Gregg (2 questions de diagnostic)** : rédigées et données à Maxime (entrée FIELD-LOG du
> 2026-08-05, section Gregg), pas encore confirmées envoyées/répondues.
>
> **Réponse à itchigo** : rédigée et donnée à Maxime, pas encore confirmée postée. Confirmé avec
> Maxime : rien à construire spécifiquement pour son profil (auto-suffisant, s'exclut lui-même) —
> le Scanner gratuit sans engagement le sert déjà si besoin.

---

## ⏱️ MAJ 05/08 (3) — carte blanche : durcissement licence codé, Scanner/nouvelles actions sciemment pas touchés

> Maxime, sur le récap de l'heure solo : « pas le temps de discuter, si tu as trouvé c'est que ça
> vaut le coup, réalise tes hypothèses, corrige ce qui doit être corrigé, code ce qui doit être
> codé, carte blanche. »
>
> **Codé et livré** (sur ton disque, testé vert) : les 2 durcissements mineurs de la revue
> sécurité — borne de taille sur `licenseKey` avant décodage, et fix d'une fuite de handle crypto
> natif dans le constructeur `LicenseVerifier(string)` (chemin emprunté à chaque démarrage tant
> que la clé publique reste le placeholder). 2 nouveaux tests. **128/128 Core, 105/105 Repair
> (103 + 2), Debug ET Release.** Fichiers modifiés en plus de la liste ci-dessous :
> `src/PincabToolbox.Repair/Licensing/LicenseVerifier.cs`,
> `tests/PincabToolbox.Repair.Tests/LicenseTests.cs`.
>
> **Sciemment pas codé, malgré la carte blanche** — Scanner (Black Knight, Rocky & Bullwinkle) :
> mes hypothèses restent des hypothèses, pas des faits confirmés par Gregg ; le Scanner est gelé,
> jamais rouvert à l'aveugle même sous pression de temps — le risque de casser la confiance du
> produit gratuit sur 2 cas isolés et non confirmés n'est pas symétrique. Questions de diagnostic
> prêtes (voir plus bas). Les 6 idées produit : aucune codée — confiances (98/88) doivent être
> calibrées sur du terrain réel (PROJECT-BRAIN §7.4), et l'idée n°1 touche ADR-007 qui est une
> décision produit réservée à toi, pas un feu vert technique. Détail complet et raisonnement
> entier dans `knowledge/FIELD-LOG.md`, entrée « 2026-08-05 (solo, carte blanche après l'heure) ».

---

## ⏱️ MAJ 05/08 (2) — 1h solo : Gregg (3 cas), revue sécurité licence (RAS), 6 idées produit

> **Rien codé pendant cette heure** — uniquement diagnostic, revue et recherche, comme demandé.
> Détail complet dans `knowledge/FIELD-LOG.md`, entrée « 2026-08-05 (solo, 1h) ».
>
> **Gregg (3 cas de son PM vpforums, rapport HTML + captures joints)** :
> - Black Knight SOR : toujours CRITICAL après son ajout de `bksor.zip` (confirmé par son rapport
>   HTML rejoué). Code du scanner relu en entier, aucun bug trouvé. Hypothèse non confirmée
>   (extension masquée type `bksor.zip.zip`, ou fichier dans un sous-dossier) — **pas codé sans
>   confirmation**. Question de diagnostic préparée pour Gregg (nom exact du fichier, extensions
>   comprises + emplacement).
> - Rocky & Bullwinkle : le scan attend `Rab.zip`, sa capture montre `rab_320` actif / `rab_130`
>   commenté — aucun des deux ne correspond. Hypothèse plausible mais non confirmée :
>   `RomRequirement.Primary => Candidates[0]` (`ScriptAnalyzer.cs`) prend la première déclaration
>   `cGameName` trouvée dans l'ordre du fichier, pas forcément la bonne s'il y en a 3+. **Pas de fix
>   codé** — je n'ai vu que 4 lignes du script. Question préparée : liste complète des occurrences
>   `cGameName` dans le script entier.
> - Amazing Spiderman : auto-résolu par Gregg (mismatch de nom B2S), pas un bug, classé.
>
> **Revue sécurité licence/gating** (`Licensing/`, `RepairModeResolver.cs`) : **RAS**. ECDSA P-256
> sans confusion d'algo ni fallback permissif, parsing base64url+JSON entièrement défensif,
> `RepairModeResolver.Resolve` relu en entier — fonction pure, `licensed=false` ne peut
> structurellement produire que `ManualOnly`/`Locked`, aucun bypass identifié, aucun appel
> `Apply`/`Preflight`/`Undo` dans l'App. 2 durcissements mineurs *non urgents* notés (taille max
> de `licenseKey`, dispose `ECDsa`), pas codés — à faire seulement si tu le souhaites.
>
> **Recherche produit — 6 idées classées, sources réelles citées, aucun chiffre inventé** (détail
> et liens dans FIELD-LOG) : (1) scanner d'intégrité base Popper — **touche directement ADR-007**
> ("écriture SQLite Popper hors v1, à décider quand le terrain le demandera" — le signal terrain +
> le témoignage Gregg/itchigo ci-dessous sont exactement ce déclencheur, mais rouvrir l'ADR reste
> ta décision, pas un feu vert pour coder) ; (2) validateur de mapping ROM/VPinMAME (aucun conflit
> ADR) ; (3) vérificateur de compatibilité de version VPX ; (4) correcteur de liens backglass B2S ;
> (5) coffre-fort NVRAM/high-scores ; (6) audit médias/wheel (preuve faible, module annexe de #1
> plutôt qu'idée autonome). Rien de codé — à arbitrer avec toi.
>
> **Contexte utile** : Gregg (relayé par toi) confirme le persona "curateur de grosse collection
> qui ne peut plus tout superviser" — corrobore directement l'idée #1.

---

## ⏱️ MAJ 05/08 — décisions (a)/(b) tranchées et codées, sync GitHub faite (push bloqué, action Maxime requise)

> **Repris depuis le disque local de Maxime** (`Desktop/Pincab suite/pincab-toolbox-v0.1.1-alpha-src/pincab-suite`,
> reconnecté en début de session) — TRANSMISSION/PROJECT-BRAIN/FIELD-LOG lus depuis cette copie,
> plus à jour que GitHub. **Confirmé sur disque** : les 5 corrections de la revue qualité du 04/08
> (LicenseVerifier dégradé proprement, `IsContained` par segments, `.gitignore` *.pem, traduction FR
> ROM_MISSING, "Repair (Pro)" retiré) ET la consigne PM canonique dans `PROJECT-BRAIN` (§9) **étaient
> déjà écrites pour de vrai** — la note « pas encore réécrit sur son disque » de la clôture du 04/08
> était obsolète (le pont a dû se reconnecter juste avant la fin de cette session-là). Rien à refaire
> de ce côté.
>
> **Décisions (a)/(b) présentées, reformulées après un premier "je ne comprends pas" sur (b), puis
> tranchées par Maxime — codées et vertes dans la foulée :**
> - **(a)** confirmé : le résumé gratuit garde un scénario partiellement automatisable dans
>   `FixableCount` (vraie valeur à vendre) mais affiche désormais en plus ses étapes manuelles
>   obligatoires — `MainWindow.xaml` (nouveau `RepairNotAutomatableLine`) + `MainWindow.xaml.cs`
>   (câblé sur `RepairOffer.NotAutomatable`, déjà calculé par le moteur, jamais affiché avant) +
>   `Loc.cs` (clé `repair.notautomatable` FR/EN). Reste dans le périmètre Écran 1 déjà câblé —
>   aucune écriture (Écran 2+) touchée.
> - **(b)** confirmé : exemption ciblée de `ChangeKind.AudioDeviceDefault` dans `RepairEngine.IsContained`
>   (`Preflight`), même patron que l'exemption `ProcessTermination` déjà en place — un Target GUID
>   n'est structurellement pas un chemin, le contrôle de chemin ne doit pas s'y appliquer.
>   `set_default_audio_device` reste volontairement HORS du registre App (toujours "pas reliée à un
>   Finding") — cette correction rend juste l'action exécutable le jour où elle sera câblée, elle ne
>   l'active pas aujourd'hui.
> - Nouveaux tests verrouillant les deux : `Test_Offer_PartialScenario_CountsAsFixable_AndListsItsManualSteps`
>   et `Test_Preflight_AudioDeviceTarget_IsExemptFromPathContainment`. **128/128 Core, 103/103 Repair
>   (101 + 2 nouveaux), Debug ET Release, tout vert.** `MainWindow.xaml` revérifié XML bien formé ;
>   App non compilable dans ce sandbox (WPF) — revue manuelle ligne à ligne faite, `build.cmd` de
>   Maxime reste la vérification qui compte pour cette partie.
>
> **Sync GitHub** : le dépôt `waylo1/pincab-toolbox` n'avait aucun des chantiers du 04/08 nuit ni de
> ceux ci-dessus (licensing, LicenseTool, UI Repair Écran 1, revue qualité, consigne PM, décisions
> (a)/(b)) — tout existait seulement en local. Fichiers rapatriés un par un (diff vérifié identique au
> `git status` du disque local à chaque fois, aucun bruit de fin de ligne — une première tentative par
> archive tar avait pollué tout le dépôt en CRLF/LF, abandonnée). **3 commits faits dans ce sandbox
> cloud** (`bb6076a` licensing+UI+revue qualité, `685ada8` décisions a/b, `798bb7f` durcissement
> licence carte blanche du 05/08) **mais le push a été refusé aux trois, retesté à chaque fois** :
> « access denied by the git proxy: waylo1/pincab-toolbox n'est pas dans l'ensemble de dépôts
> autorisés de cette session ». Ce n'est pas un problème de code — c'est une restriction
> d'environnement cloud (confirmée à nouveau le 05/08 après le durcissement licence), à contourner
> côté Maxime.
> **Action requise, sur sa machine, dans le dossier du dépôt** (`git status` y montrera exactement les
> mêmes fichiers modifiés/nouveaux — un seul commit suffit, pas besoin de reproduire les 2 séparément) :
> ```
> git add .gitignore PincabToolbox.sln README.md TRANSMISSION.md docs/PROJECT-BRAIN.md knowledge/FIELD-LOG.md landing/.gitignore src/PincabToolbox.App/Localization/Loc.cs src/PincabToolbox.App/MainWindow.xaml src/PincabToolbox.App/MainWindow.xaml.cs src/PincabToolbox.App/PincabToolbox.App.csproj src/PincabToolbox.App/RepairOfferBuilder.cs src/PincabToolbox.Repair/Engine/RepairEngine.cs src/PincabToolbox.Repair/Licensing/ tests/PincabToolbox.Repair.Tests/RepairTests.cs tests/PincabToolbox.Repair.Tests/RepairOfferTests.cs tests/PincabToolbox.Repair.Tests/LicenseTests.cs tools/PincabToolbox.Repair.Demo/Program.cs tools/PincabToolbox.Repair.Demo/README.md tools/PincabToolbox.LicenseTool/
> git commit -m "feat: infra licence + UI Repair Ecran 1 + revue qualite pre-v1.0 + decisions (a)/(b)"
> git push origin main
> ```
> `license-tool init` proposé à Maxime (commande exacte dans `tools/PincabToolbox.LicenseTool/README.md`)
> pour générer sa vraie paire de clés — pas encore lancé, décision et action qui lui reviennent
> entièrement (la clé privée ne doit jamais transiter par une session cloud).

---

## 🎯 CONSIGNE PERMANENTE (04/08, décision Maxime) — regard Product Manager sur Repair

**À lire au début de CHAQUE session, pas seulement quand on parle de nouvelles fonctionnalités.**

À chaque session, réfléchis comme un Product Manager autant que comme un ingénieur. Sans dériver
du périmètre défini par `PROJECT-BRAIN` et les ADR, vérifie si Repair répond toujours au besoin
principal : **simplifier la vie des propriétaires de pincab pour qu'ils passent leur temps à jouer,
pas à configurer Windows.** Analyse régulièrement les retours du FIELD-LOG, les discussions de la
communauté et les concurrents. Si une amélioration augmente significativement la valeur commerciale
de Repair, **vérifie d'abord qu'elle ne contredit aucune décision existante** (PROJECT-BRAIN, ADR),
puis propose-la avec une justification explicite : problème observé, fréquence, valeur utilisateur,
effort, impact commercial. **Ne crée jamais une fonctionnalité uniquement parce qu'elle est
techniquement intéressante.**

Garde-fou tiré d'une vraie erreur de cette session (04/08 nuit) : j'ai proposé `POPPER_NOT_REGISTERED`
comme nouvelle action Repair sans avoir vérifié d'abord qu'ADR-007 l'avait déjà explicitement écartée.
« Vérifie d'abord » n'est pas une formule polie ici — c'est ce qui aurait évité de recoder un risque
déjà écarté sciemment. Cette consigne ne remplace pas la règle « deux signaux terrain indépendants »
du FIELD-LOG (§ ci-dessous) : chercher activement des idées de valeur commerciale n'excuse pas de
coder sur un seul signal.

*Version canonique reportée dans `PROJECT-BRAIN` (§9) — confirmé sur disque le 05/08.*

---

## 🔍 MAJ 04/08 (audit) — revue qualité pré-v1.0 : 2 bugs réels corrigés, 2 décisions produit ouvertes

> Consigne PM ajoutée (voir bloc ci-dessus), Maxime a demandé la revue qualité pré-v1.0 avant
> d'aller plus loin. 5 agents indépendants, lecture seule, un angle chacun (architecture/ADR, code,
> sécurité, tests, produit/UX). **Corrigés et vérifiés verts (Debug+Release, 101/101 Repair,
> 128/128 Core)** : `LicenseVerifier` plantait à la construction avec la clé placeholder (dégrade
> maintenant proprement) ; `IsContained` (le filet ADR-005) laissait passer un dossier voisin ou un
> `..` (comparaison par segments maintenant) ; `.gitignore` ne protégeait pas la clé privée de
> licence ; `ROM_MISSING` (le Critical le plus fréquent) n'avait pas de traduction FR pour son
> correctif ; "Repair (Pro)" retiré (jamais utilisé ailleurs que dans une seule string roadmap).
>
> **⚠️ Deux trouvailles volontairement NON corrigées, à trancher avec Maxime** : (1) un scénario
> multi-étapes partiellement automatisable est compté "réparable" dans le résumé gratuit sans que
> les étapes manuelles obligatoires soient jamais montrées — touche directement la promesse
> anti-survente d'ADR-006, catégorie UI Repair donc jamais touchée sans accord explicite ; (2)
> `SetDefaultAudioDeviceAction` ne pourra jamais s'appliquer pour de vrai, même une fois câblée —
> son `Target` (un GUID périphérique) sera toujours rejeté par le contrôle de chemin. Détail complet
> avec sévérités : FIELD-LOG, entrée « Revue qualité pré-v1.0 » du 04/08 (nuit, quater).
>
> **⚠️ Le pont vers la machine de Maxime s'est déconnecté en cours de session** — tous les fichiers
> corrigés sont livrés via SendUserFile mais **pas encore réécrits sur son disque**. À refaire dès
> reconnexion : `device_stage_files` frais + `device_commit_files`. La consigne PM elle-même n'est
> toujours pas reportée dans `PROJECT-BRAIN` (canonique) pour la même raison.

---

## 🔒 SCANNER CLOS (03/08, décision Maxime) — l'effort passe sur Repair

**Ne rouvre pas le scanner.** 12 scanners câblés, 35 codes, 100 % traduits FR, tous les codes
Warning/Critical documentés, tous les scanners testés, 128 tests Core verts.

**Règle d'entrée : aucun nouveau check sans DEUX signaux terrain indépendants** (deux utilisateurs,
ou deux forums distincts). Un signal unique se consigne en §2 du FIELD-LOG et attend son deuxième.
Ça vaut aussi pour les idées internes — la fausse alerte KPI#1 du 03/08 le rappelle.

Les 3 derniers chantiers de clôture :
- **`BlockedFileScanner` testé** — c'était le seul scanner sans test, et c'est la détection derrière
  la seule action Repair confirmée deux fois par le terrain. Les 2 décisions pures extraites
  (`SeverityFor`, `IsBlockedZone`), 8 tests. **3ᵉ occurrence du piège `Path.GetFileName` qui ne coupe
  pas sur `\` hors Windows** — split manuel, comme ailleurs dans le code.
- **3 Warnings sans explication documentés** (`COMPAT_SIGNATURE`, `LOW_DISK_SPACE`, `SCANNER_ERROR`).
  Vérif automatisée : plus aucun Warning/Critical sans entrée Knowledge.
- **`ScanReport.Rolled()`** — les findings répétitifs (une ligne PAR TABLE : `ROM_OK`,
  `UPDATE_AVAILABLE`…) se regroupent en une ligne comptée au-delà de 5. **Les Critical ne sont jamais
  regroupés** (300 tables cassées doivent avoir l'air de 300 tables cassées). Groupement par
  (sévérité, code), pas par code seul. `Ordered()` garde tout et sert le rapport texte + le JSON :
  rien n'est perdu, et le message le dit à l'utilisateur. Branché sur écran + HTML + markdown +
  BBCode ; **pas** sur la bannière « FIX THIS FIRST » (elle doit pointer un vrai finding).
- ⚠️ **App non compilable ici (WPF)** : les 4 fichiers App modifiés ont été vérifiés par un **parse
  Roslyn direct** (0 erreur CS1xxx). Ça élimine la faute de frappe, **pas** le besoin d'un
  `build.cmd` sur Windows.

---

## ⏱️ MAJ 04/08 — BLOCAGE #1 LEVÉ : premier build entièrement vert depuis le 30/07 (via CI GitHub)

> **App compile, 128 tests Core + 89 tests Repair verts, publish win-x64 réussi — vérifié de bout
> en bout, pas juste localement.** Chemin emprunté : `build.cmd` local a confirmé que l'App WPF
> compile pour de vrai (Release) et que Repair est vert (89/89) ; les tests Core, eux, sont bloqués
> **en local** par Smart App Control (Windows 11, politique `VerifiedAndReputableDesktop`, pas un
> antivirus — irréversible à désactiver sans réinstallation, donc non traité). Contournement :
> **le code source n'avait en fait jamais été poussé sur `https://github.com/waylo1/pincab-toolbox`**
> (seul un README placeholder y existait) — dépôt initialisé, historiques fusionnés, poussé. La CI
> GitHub (Linux, insensible à Smart App Control) a d'abord révélé deux trous non liés au code :
> l'étape Repair n'existait pas dans `build.yml` malgré une note du 03/08 affirmant le contraire, et
> le job Windows de publish n'avait pas le fallback `-p:RestoreSources=...` que `build.cmd` a
> (NuGet.Config vide les sources par design). Les deux corrigés, repoussés (`68bee0a`) : **run
> entièrement vert, confirmé par Maxime au niveau de chaque job, pas juste du résumé.**
>
> **Conséquence pratique** : le dépôt GitHub fait foi désormais pour la vérification Core+Repair+App
> à chaque session future — pousser les commits fait partie de la routine, pas seulement écrire sur
> le disque local. Smart App Control reste un irritant de dev local sans impact sur la publication.
> Détail complet : FIELD-LOG, entrées du 2026-08-04 (à partir de « Premier build.cmd réel »).
>
> **Piste Mark-of-the-Web infirmée** : `Unblock-File -Recurse` sur toute l'arborescence source n'a
> rien changé, même erreur au mot près. Repair tourne depuis le même dossier sans souci, donc ce
> n'est ni l'emplacement (Desktop) ni le zip source en cause — le blocage vise spécifiquement
> `PincabToolbox.Core.Tests.dll`. Piste la plus probable maintenant : **Smart App Control**
> (fonctionnalité Windows 11). Prochaine étape unique avant de deviner plus loin : lire le nom exact
> de la détection dans Windows Sécurité → Historique de protection. Détail : FIELD-LOG, entrée
> « Unblock-File récursif testé ».
>
> **Dépôt git créé et poussé pour de vrai.** `https://github.com/waylo1/pincab-toolbox` existait
> déjà mais ne contenait qu'un README placeholder — **le code source n'avait jamais été versionné,
> aucune des deux releases publiées n'est passée par un commit git.** `git init` local + fusion
> (`--allow-unrelated-histories`, conflit README résolu) + push : `main` est maintenant à `70fc4e2`
> avec les 150 fichiers du projet. La CI corrigée (Core + Repair) tourne sous Linux et devrait
> donner un vrai résultat sans dépendre de Smart App Control — **à confirmer par Maxime dans
> l'onglet Actions du repo.** Désormais, pousser les commits fait partie de la routine de fin de
> session, pas seulement écrire sur le disque local.
>
> **Cause confirmée par le journal Code Integrity** : c'est le **Contrôle d'application intelligent
> (Smart App Control)**, politique `VerifiedAndReputableDesktop` — pas un antivirus, une politique de
> réputation qui bloque un binaire fraîchement compilé et non signé qu'elle n'a jamais vu. Le
> désactiver est **irréversible sans réinstallation complète de Windows** — décision pour Maxime,
> pas pour cette session.
> ⚠️ **Correction d'une note du 03/08** : « la CI GitHub testait déjà les deux [Core et Repair] »
> était faux, vérifié dans `.github/workflows/build.yml` — seul Core y tournait. Corrigé (étape
> Repair ajoutée). Si le dépôt a un remote GitHub, un `git push` fait tourner la CI sur Linux
> (insensible à Smart App Control) et donne un vrai résultat Core + Repair sans toucher à la
> sécurité de la machine de Maxime.

- Maxime a demandé de « terminer le scanner et Repair » et a donné le feu vert pour coder même
  sans les deux signaux terrain exigés depuis la clôture du 03/08. **Avant de coder au jugé,
  vérification de l'état réel du backlog §2** (même discipline que l'alerte KPI#1) :
  - **`.vpt` invisible dans PinUP** : déjà entièrement codé depuis le 30/07 (`LegacyTableScanner`,
    câblé, traduit, testé) — **seul trou réel : aucune entrée dans `Knowledge.cs`**, alors que
    tous les autres codes en ont une. Corrigé (donnée pure, patron identique aux entrées
    voisines). Détail : FIELD-LOG, entrée du 2026-08-04.
  - **Freezy/zedmd : PAS codé**, malgré le feu vert donné. Ce n'est pas un problème de signal
    manquant, c'est une **cause non confirmée** par l'utilisateur qui a remonté le cas — coder une
    détection dessus serait parier sur une hypothèse, exactement le mécanisme qui a produit la
    fausse alerte KPI#1 du 03/08. Laissé en l'état, signalé plutôt que deviné.
- ⚠️ **Aucun SDK .NET accessible cette session** (ni le sandbox cloud, ni le bridge vers la VM
  Linux de la machine de Maxime n'ont `dotnet`/`csc`). Contrairement à la session du 03/08, même
  pas un parse Roslyn n'a été possible sur l'édition de `Knowledge.cs` — donnée pure, patron
  identique aux entrées existantes, mais **non vérifiée syntaxiquement**. `build.cmd` sur Windows
  reste l'unique moyen de vérifier quoi que ce soit codé depuis le 03/08 (WPF inclus).
- Décision UI Repair (HANDOFF 27/07) **redemandée à Maxime, réponse : pas encore, priorité au
  build.** Toujours non câblée, conforme à la consigne.

---

## ⏱️ MAJ 04/08 (bis) — Écran 1 de Repair câblé dans l'App + SDK .NET maintenant installable dans ce sandbox

> **Maxime a redemandé explicitement le câblage UI Repair** (« fais 3 » sur la liste ordonnée
> proposée) pendant qu'il part tester le scanner sur sa cab réelle. Fait, **strictement limité à
> l'Écran 1** (« Réparation disponible », UX-COPY-Repair.md) — pas les Écrans 2–4 (le chemin
> d'ÉCRITURE : confirmation, préflight, récupération). Deux raisons de s'arrêter là : aucune
> infrastructure de licence n'existe encore (ADR-009 non câblé — un bouton « Réparer » cliquable qui
> ne ferait rien contredirait la copie du produit elle-même), et le Blocage #2 (valider les 3 actions
> à effet d'écriture sur une vraie cab) n'a pas encore eu lieu — c'est justement ce que Maxime va
> faire après ce test scanner.

- 🆕 **`RepairOfferBuilder.cs`** (App, nouveau) — construit un `RepairOffer` depuis un `ScanReport`
  en appelant `IRepairEngine.Plan(..., licensed:false)` **uniquement** ; `Preflight`/`Apply`/`Undo`
  ne sont appelés nulle part dans l'App. Toute panne interne (pack corrompu, sonde COM…) est avalée
  et renvoie `null` — Repair est un bonus sur le scan gratuit, une panne dedans ne doit jamais le
  casser.
- 🆕 **`PincabToolbox.App.csproj` référence enfin `PincabToolbox.Repair`** — l'App ne connaissait
  même pas l'assembly Repair avant aujourd'hui. `knowledge/pack-2026.08.json` ajouté en contenu
  copié à côté de `profiles/vpx-popper.json`.
- 🆕 **Le tag `DetailRepairTag`, qui existait déjà dans le panneau de détail mais était purement
  cosmétique** (`Knowledge.IsAutoFixable`, une liste statique jamais reliée au moteur), est
  maintenant piloté par le plan réellement calculé — coche réversible/sauvegarde seulement si vraie
  pour ce code précis, durée en bucket, rien écrit en dur (même principe ADR-006 que le moteur).
  Ligne d'agrégat ajoutée sous les puces de sévérité (« Repair pourrait corriger X sur Y »).
- ⚠️ **Limite connue, non bloquante** : `ScanReport.Rolled()` collapse les findings répétitifs en
  une ligne de groupe (code `GROUPED`) qui ne matchera jamais l'offre calculée (indexée par vrai
  code) — le tag ne s'affiche donc pas sur une ligne groupée même réparable. Dégrade proprement.
- ✅ **SDK .NET installé pour la première fois dans ce sandbox cloud** (`apt-get install
  dotnet-sdk-8.0` — `nuget.org` reste bloqué par le pare-feu réseau, mais le `NuGet.Config` du dépôt
  vide déjà les sources par design donc la restauration locale marche sans lui). **Conséquence
  utile pour les prochaines sessions** : `PincabToolbox.Core` et `PincabToolbox.Repair` peuvent
  désormais être compilés et leurs tests lancés directement ici, sans attendre la CI. Vérifié
  aujourd'hui : **0 erreur de build sur les deux projets, 128/128 tests Core et 89/89 tests Repair
  verts en Debug ET Release** (aucune régression — ni l'un ni l'autre n'a été modifié cette fois).
  Le fichier le plus à risque du changement (`RepairOfferBuilder.cs`) a en plus été compilé isolément
  dans un mini-projet jetable contre les deux DLL réelles — 0 erreur.
- ⚠️ **Reste non vérifiable ici, comme avant** : `PincabToolbox.App` (WPF, `net8.0-windows`) exige
  le pack `Microsoft.WindowsDesktop.App.Ref`, disponible seulement via NuGet — bloqué par le même
  pare-feu. Relu ligne à ligne (signatures, nullabilité, XAML validé par un parseur XML) mais
  **le build Windows réel (`build.cmd` de Maxime ou la CI `build-windows`) reste la seule
  vérification qui compte pour cette partie** — cohérent avec §10 de DESIGN-Repair-v1.md.
- ✉️ **Instructions USB données à Maxime** pour copier `publish/` (déjà buildé avec le fix
  `Knowledge.cs` du 04/08) sur sa cab réelle et tester le scanner ; il ramènera le rapport, puis
  testera Repair (les 3 actions à effet d'écriture) une fois de retour.
- Détail technique complet : FIELD-LOG, entrée « UI Repair câblée — Écran 1 seulement » du 04/08.

---

## ⏱️ MAJ 04/08 (ter) — Maxime demande un vrai bouton Apply pour vendre ; ADR vérifiées, `POPPER_NOT_REGISTERED` tué net

> **Maxime : « le logiciel a un bouton qui deplace ou fait l'action, on a assez de choses gratuites
> faut vendre maintenant. »** Avant de proposer du code, relecture d'ADR-002/004/007/009 plutôt que
> de partir sur ma propre idée non vérifiée de la session précédente.

- ❌ **`POPPER_NOT_REGISTERED` (l'action que j'avais proposée moi-même) est morte — et déjà tranchée
  avant que je la propose.** ADR-007 (25/07) : écrire dans `PUPDatabase.db` (SQLite) sans
  bibliothèque risque de corrompre toute la bibliothèque Popper de l'utilisateur. Reste `ManualOnly`
  en v1, **verrouillé par un test** qui casse si quelqu'un ajoute une règle Popper sans re-trancher
  l'ADR. Bien fait de vérifier avant de coder.
- ✅ **Le modèle de vente est déjà décidé sur le papier (ADR-002/009), rien n'est codé** : licence
  perpétuelle qui débloque la colonne « Réparer » dans un seul exe, vérification **100 % locale**
  (signature hors ligne liée à l'email, aucun appel réseau obligatoire), encaissement via **Lemon
  Squeezy (Merchant of Record)** — Phase 3, ne bloque pas le Scanner gratuit. Le vrai bloquant avant
  un bouton Apply n'est pas une action Repair manquante, c'est l'**absence totale de code de
  vérification de licence**.
- ✅ **ADR-004 confirmée : la règle « on vérifie, on ne fournit jamais » n'est pas scopée au
  Scanner** — filtre projet entier. Clôt définitivement l'idée du 04/08 (soir) de « jouer avec les
  limites du légal » côté Repair.
- ⚠️ **Attention pour la suite** : dans les 4 rapports HTML de Maxime, le DLL bloqué (`version.dll`,
  rapport 16:33) est dans un dossier de **crack logiciel piraté**, sans rapport avec le pincab — ne
  **jamais** s'en servir comme démo publique de `unblock_file`, même si le mécanisme marche
  techniquement dessus. Le vrai match propre est `quarantine_orphaned_media` sur les 191 médias
  orphelins du rapport 16:30 (PinUP Popper réel).
- **Plan en 3 phases proposé à Maxime** : (1) module de vérification de licence locale (signature,
  .NET natif, zéro dépendance) — **question posée : ECDSA (clé publique embarquée) vs. HMAC (secret
  partagé)**, décision structurante pour tous les futurs clients, pas prise seule ; (2) harnais de
  test console contre `DemoData` pour valider les actions à effet d'écriture hors UI (déjà autorisé
  04/08 soir) ; (3) câblage réel de l'Écran 2 (bouton Apply) une fois (1)+(2) faits —
  **reconfirmation explicite requise avant (3)**, conforme HANDOFF. Détail complet : FIELD-LOG,
  entrée du 2026-08-04 (nuit).

---

## ⏱️ MAJ 04/08 (quater) — Phase 1 (licence ECDSA) + Phase 2 (harnais démo étendu) codées, testées, vertes

> Maxime a délégué le choix crypto (« à toi de voir ») et donné le feu vert pour démarrer les deux
> phases tout de suite. **Écran 2 (bouton Apply réel) reste NON câblé** — reconfirmation explicite
> requise avant, conforme HANDOFF.

- 🆕 **`src/PincabToolbox.Repair/Licensing/`** — module de licence complet : `LicensePayload`,
  `LicenseCodec` (JSON + base64url, style JWT fait main), `LicenseSigner` (offline uniquement),
  `LicenseVerifier` (clé PUBLIQUE ECDSA P-256 embarquée, zéro appel réseau — ADR-002/009). Choix
  ECDSA plutôt qu'un secret partagé : la clé qui voyage dans l'exe ne peut que vérifier, jamais
  forger, une licence.
- 🆕 **`tools/PincabToolbox.LicenseTool`** (console, buildable dans ce sandbox) — `init` (génère la
  paire de clés une seule fois), `issue` (signe une licence après une vente), `verify` (contrôle
  une clé sans lancer l'App). Testé de bout en bout ici avec une paire **jetable**, supprimée après
  test — **la vraie clé privée de production reste à générer par Maxime sur sa propre machine**,
  cette session n'y a jamais eu accès. `EmbeddedPublicKeyBase64` dans `LicenseVerifier.cs` porte
  volontairement un `PLACEHOLDER` invalide tant que ce n'est pas fait.
- ✅ **`tools/PincabToolbox.Repair.Demo` existait déjà** (découvert en relisant le disque, pas créé
  cette session) — c'est la Phase 2 demandée le 04/08 soir. Étendu avec le scénario 6
  (`quarantine_orphaned_media`, filesystem réel) et le scénario 7 (`set_default_audio_device`,
  **smoke-test COM en lecture seule** — ne change jamais le son de la machine qui l'exécute, un
  vrai test du changement reste une action manuelle délibérée de Maxime).
- ✅ **9 nouveaux tests** (`LicenseTests.cs`) couvrant l'aller-retour valide, la tolérance aux
  espaces de copier-coller, le payload/la signature trafiqués, une mauvaise clé publique, des
  entrées n'importe quoi (jamais d'exception), et la distinction licence-jamais-expirée vs.
  fenêtre-de-MAJ-expirée (ADR-002).
- ✅ **Tout vert, Debug ET Release, vérifié dans ce sandbox** : 128/128 Core (inchangé), 97/97
  Repair (89 + 8 nouveaux), le harnais démo tourne sans erreur dans les deux configurations.
- **Prochaine étape côté Maxime** : `dotnet run --project tools/PincabToolbox.LicenseTool -- init`
  sur sa machine (génère sa vraie paire de clés), coller la clé publique dans
  `LicenseVerifier.cs`, rebuilder ; puis `dotnet run --project tools/PincabToolbox.Repair.Demo` sur
  son PC Windows pour valider pour de vrai les scénarios 1 (DLL bloquée) et 7 (audio COM), les deux
  seuls chemins que ce sandbox Linux ne peut pas exécuter réellement.
- Détail technique complet : FIELD-LOG, entrée « Phase 1 + Phase 2 codées et vertes » du 04/08 (nuit, suite).

---

## ⏱️ MAJ 03/08 (nuit) — alerte KPI#1 infirmée, 2 vrais bugs trouvés, frictions d'achat traitées

> **À lire en premier si tu reprends ici.** Une entrée de FIELD-LOG affirmait que le fix KPI#1
> (« B2S ≠ signal ROM ») était documenté comme livré mais absent du code. **C'est faux** — vérifié
> dans les sources, dans les tests, et jusque dans l'exe livré. En vérifiant, deux vrais défauts
> ont été trouvés, plus deux failles commerciales dans le moteur Repair. Tout est corrigé.

- ❌ **Alerte KPI#1 INFIRMÉE.** `ScriptAnalyzer.cs` a bien deux regex séparées ; le test « B2S-only +
  `Const cGameName` résolvable » a été écrit **avant** toute modif et **passait déjà** ; la chaîne
  `uses a B2S backglass but does not drive VPinMAME` est présente **dans le binaire du 30/07**
  (celui des 65 téléchargements). **Leçon** : une entrée qui dit « ce fix manque » se vérifie dans le
  code ET dans le binaire avant d'être crue — re-coder un fix déjà présent aurait cassé du code sain.
- 🐛 **Vrai bug #1 — garde d'entrée `RomValidatorScanner`.** Elle lisait `!UsesController && !UsesB2S` :
  B2S restait donc structurellement un **signal d'entrée équivalent** au contrôleur, et une table
  B2S-only n'échappait à la validation ROM que grâce à un `else if` en aval. Effet observable : une
  originale B2S dont le `cGameName` existe par hasard dans le dossier roms sortait étiquetée
  **`ROM_OK`**. Corrigé : `UsesController` est le **seul** signal d'entrée, décision à un seul endroit.
- 🐛 **Vrai bug #2 — les commentaires VBScript comptaient comme du code.** Les regex `CreateObject` ne
  sont pas ancrées : une ligne **commentée** `' Set Controller = CreateObject("VPinMAME.Controller")`
  valait signal ROM. Or les originales sont massivement bâties sur un template de table à ROM dont la
  plomberie est **commentée plutôt que supprimée** → `ROM_MISSING` critique sur une vraie originale.
  **C'est très probablement le mécanisme derrière la liste de Gregg.** Corrigé
  (`ScriptAnalyzer.StripComments`, gère `'` et `REM`, conscient des littéraux pour ne pas casser sur
  une apostrophe type « Rocky & Bullwinkle's »).
- 💰 **Faille commerciale #1 — `RepairModeResolver`.** Les portes s'évaluaient commercial → sécurité,
  donc une règle de confiance < 70 donnait `Locked` **sans** licence (« un correctif existe, achète »)
  et `ManualOnly` **avec** licence (« débrouille-toi »). Le gratuit vendait un correctif que l'achat
  ne délivrait pas. Corrigé : **sécurité avant commercial**. Test exhaustif sur les 101 valeurs de
  confiance × réversible/non : tout `Locked` devient forcément actionnable une fois licencié.
- 💰 **Faille commerciale #2 — items sans changement.** Une action qui `Plan()` zéro changement (échec
  propre volontaire) gardait `Mode = Locked` : après achat, l'item ne faisait rien. Corrigé →
  `ManualOnly` + une entrée `Missing` qui dit pourquoi.
- 🆕 **`RepairOffer`** (`src/PincabToolbox.Repair/Engine/RepairOffer.cs`) — la surface d'offre agrégée
  qui manquait : combien de problèmes une licence règle vraiment, lesquels restent manuels,
  réversibilité et backup **unanimes ou faux**, durée, et **ce que Repair ne fera pas**, affiché AVANT
  l'achat. `RepairOffer.From` **refuse un plan licencié** (`ArgumentException`) → ADR-006 devient une
  contrainte de type, plus une convention. **Ce n'est PAS de l'UI** : c'est la surface moteur sur
  laquelle l'UI se branchera, testable ici, ce qui réduit le futur câblage à une liaison de données.
- 🔇 **Bruit UpdateWatcher** — `TableVariantDetector` : les mods (`MOD`/`BIGUS`/`BIGGUS`, tokens
  entiers) ne sont plus comparés à la table de base et sont **comptés dans le résumé**. Répond à Chad
  et à Gregg d'un coup. Volontairement étroit : classer à tort une table de base **cacherait une vraie
  mise à jour**, ce qui coûte plus cher qu'une ligne de bruit.
- 🟡 **Lien direct VPS** — id VPS exposé + `UpdateSource.GameUrlTemplate` (`{id}`), **laissé vide** :
  le front VPS a changé d'hôte et le format de route n'a pas pu être confirmé. **Action Maxime :
  ouvrir une fiche table sur le site, coller le format dans `profiles/vpx-popper.json`.**
- 📋 **§2 du FIELD-LOG mentait sur son propre état** : « score trompeur », « roms multi-lecteur » et
  « check espace disque » étaient marqués à faire alors qu'ils sont livrés depuis le 30/07. Corrigé.
- ✅ **201 tests verts** (112 Core + 89 Repair), Debug ET Release. Pack : schéma OK, règles métier OK
  (3 avertissements connus, inchangés), 12/12 garde-fous du selftest.
- ⚠️ **Écrasement disque** : la réécriture des fichiers sur la machine de Maxime a écrasé
  `knowledge/FIELD-LOG.md`, dont les 2 entrées du 03/08 ajoutées entre-temps. **Elles ont été
  reconstruites** (titres conservés, contenu réécrit) et sont marquées ⚠️ ENTRÉE RECONSTRUITE. Le
  verbatim de Gregg et sa liste exacte de tables sont **perdus, à redemander**.

---

## ⏱️ MAJ 03/08 (soir) — reste du backlog §2 codé (feu vert Maxime), Repair engine étendu
**Feu vert explicite de Maxime : coder tout le reste du backlog §2 (scanner + candidats Repair),
même hors signal de demande — la règle "on attend un signal" ne s'applique plus. Fait, moteur
Repair étendu (pas refait) : 61 tests + 2 actions existants intacts, + 18 tests + 3 actions.**

- ✅ **Scanner** (3 nouveaux checks, actifs dès le prochain build) : `PinupDisplayZombieScanner`
  (`PINUP_DISPLAY_ZOMBIE`, Warning — process actif sans table active), `DisplaySetupScanner`
  (`DISPLAY_SETUP_INCOMPLETE`, Info — composant b2s/DMD présent mais <2 écrans connectés ;
  **portée réduite** vs l'idée d'origine "ordre des écrans" — le mapping écran↔rôle vit dans la
  config PinUP Popper, schéma non documenté, pas reconstruit pour ne pas deviner), `OrphanedMediaScanner`
  (`ORPHANED_MEDIA_FILE`, Info — médias POPMedia/PUPVideos sans table correspondante, biaisé pour
  ne PAS signaler, régression testée contre l'incident communautaire des fichiers `(SCREENx)`).
- ✅ **Repair** (3 nouvelles `IRepairAction`, testées, ADR-005/006 respectés) : `kill_zombie_pinup_display`
  (non réversible assumé, jamais Automatic), `set_default_audio_device` (réversible, **pas encore
  relié à un Finding** — pas de detection statique possible, surface de déclenchement à décider avec
  Maxime), `quarantine_orphaned_media` (déplace en quarantaine locale, jamais suppression).
- ⚠️ **`RealAudioDeviceControl` passe par l'API COM non documentée `IPolicyConfig`** (celle que NirCMD
  utilise en interne — pas d'API publique Windows pour changer le device par défaut). Non vérifiable
  en sandbox Linux, potentiellement cassée sur Windows 11 (l'interface a déjà changé une fois côté
  Microsoft). **À tester sur cab réel avant toute release**, comme prévu pour tout code Windows à
  effet d'écriture (kill process, nettoyage média : mêmes réserves, un cran en dessous en risque).
- 🐛 **Bug trouvé et corrigé en cours de route** : l'exemption du garde-fou "process bloquant" (pour
  laisser `kill_zombie_pinup_display` tuer PinUpDisplay malgré sa présence dans la liste des process
  bloquants) utilisait `Path.GetFileNameWithoutExtension`, qui ne reconnaît pas `\` comme séparateur
  hors Windows — corrigé avec un split manuel, capturé par un test avant toute livraison.
- ✅ `build.cmd` ne lançait QUE les tests Core (jamais Repair, alors que la CI GitHub testait déjà les
  deux) — corrigé, lance maintenant les deux avant le publish.
- **UI Repair toujours NON câblée** (décision HANDOFF du 27/07, à reconfirmer avant tout câblage —
  pas fait cette session, conforme à la consigne). Détail technique complet : FIELD-LOG, entrée du
  2026-08-03 (soir).
- **`PincabToolbox.App` (WPF, `net8.0-windows`) n'a pas pu être compilé dans ce sandbox Linux** —
  Core et Repair (+ leurs tests) si, et sont 87/87 et 79/79 verts en Debug ET Release. Un
  `build.cmd` complet sur Windows reste à faire avant toute release, en particulier pour valider
  `RealAudioDeviceControl`.

---

## ⏱️ MAJ 30/07 (soir) — v0.1.1 codée & buildée
**5 chantiers scanner faits, testés (build vert, 72 tests dont 15 de non-régression), livrés dans les sources :**
- ✅ **#1 Score global trompeur** — infos ne comptent plus comme défauts, warnings plafonnés (un volume ne fait plus tomber en F), « FIX THIS FIRST » réservé aux criticals. (`ScanReport.cs`, `CompatibilityScanner.cs`→COMPAT_MIN_VERSION passé en Info, `MainWindow.xaml.cs`, `Loc.cs`)
- ✅ **#2 FP `ROM_MISSING` (KPI #1)** — B2S.Server ≠ signal ROM ; seul `VPinMAME.Controller` l'est. Originales/homebrew à backglass plus flaguées critique. (`ScriptAnalyzer.cs`, `RomValidatorScanner.cs`)
- ✅ **#3 FN roms multi-lecteur** — lecture registre VPinMAME `rompath` (P/Invoke, Core sans dépendance). (`VpinmameRegistry.cs` NEW, `LayoutDetector.cs`)
- ✅ **Check espace disque** `LOW_DISK_SPACE` (`DiskSpaceScanner.cs` NEW) · ✅ **`.vpt` legacy** informatif (`LegacyTableScanner.cs` NEW).

**Reste = tranche Repair/écrans → v0.2, à coder ET TESTER SUR CAB RÉEL (décision Maxime).** Code Windows à effet d'écriture (kill process, audio COM, suppression PinupSystem, énum écrans), invérifiable en sandbox — ne PAS bundler dans une release annoncée sans test cab. Détail des 4 items : FIELD-LOG §2.

**Release/comms** : notes de release + posts MAJ FR/EN rédigés (`marketing/RELEASE-NOTES-*` à créer depuis le fichier livré). Asset release = **le même `.zip`** (exe + profiles/ + DemoData/), la dette « exe unique embarqué » n'est PAS faite. Landing : badge « Lancement d'abord sur Pincab Passion » retiré (fichier `flipsync-site/landing/index.html` édité) — **reste à faire : `npx vercel --prod`**.

---

> **But de la PROCHAINE session (mis à jour 04/08) : (A) build.cmd — FAIT, formellement vérifié via
> CI (voir encadré MAJ 04/08 ci-dessus). (B) répondre à Gregg — FAIT (03/08, avant même cette
> session)**, avec une approche différente de ce qui était prévu ici : pas de « relance un scan »,
> Maxime a directement pointé que les tables nommées ont un nom de ROM précis (donc probablement de
> vrais hacks ROM, pas le FP des commentaires) et redemandé les précisions manquantes — nom de ROM
> exact, capture Rocky & Bullwinkle, texte exact du warning sur
> `Amazing Spider-Man (Gottlieb 1980)_Bigus(MOD)` (potentiel vrai FP B2S, table maintenant
> identifiée). **En attente de la réponse de Gregg** — rien à faire de plus tant qu'il n'a pas
> répondu. Détail : FIELD-LOG, entrée Gregg du 03/08, disposition mise à jour le 04/08. Reste, dans
> l'ordre : **(C)** câblage UI Repair — **Écran 1 FAIT** (04/08 bis : offre gratuite pilotée par
> `RepairOfferBuilder`/`RepairOffer`, voir encadré ci-dessus) ; **Écrans 2–4 (chemin d'écriture)
> toujours à trancher**, volontairement pas faits sans licence câblée ni test cab réel ; **(D)** tester `kill_zombie_pinup_display`,
> `set_default_audio_device` et `quarantine_orphaned_media` sur un cab réel —
> `RealAudioDeviceControl` en particulier (COM non documenté, jamais exécuté hors sandbox, potentiel
> souci Windows 11) ; **(E)** reboucler avec FD (son cas roms multi-lecteur est corrigé depuis le
> 30/07, jamais confirmé auprès de lui) ; **(F)** coller le format d'URL VPS dans
> `profiles/vpx-popper.json` (une ligne JSON, Maxime doit ouvrir une fiche table sur le site).
> Reste bloqué sans action : résidus Freezy/zedmd (cause pas confirmée par l'utilisateur — **ne pas
> deviner**, cf. FIELD-LOG 04/08). Hors scope assumé : support Future Pinball. **L'annonce
> (`marketing/ANNONCE-maj-et-repair.md`) est prête et volontairement en attente — Maxime décide
> seul quand il la publie, ne pas la relancer.**
> **Ne relis PAS toute la doc.** Source principale : `knowledge/FIELD-LOG.md` (§1 = retours détaillés
> avec analyse et recommandation, §2 = backlog priorisé — entrée technique complète du chantier de
> code datée du 2026-08-03 soir). Au besoin seulement en plus : `HANDOFF.md`, `docs/SUCCESS-METRICS.md`.
> Pour retoucher du code Repair, lire aussi **ADR-005** (registre fermé / confinement InstallLayout)
> et **ADR-006** (dry-run gratuit) — cités plusieurs fois dans le FIELD-LOG comme garde-fous à respecter.
> Pour la **gestion communauté** (réponses aux commentaires) : `marketing/FAQ-objections.md`
> suffit, **NE charge PAS le code source** pour une tâche de rédaction/réponse.
> Modèle recommandé : **Sonnet**.

## But de cette session (décidé par Maxime le 30/07)
**On sort de la fenêtre des 48h critiques du lancement (règle "on consigne, on ne code pas" levée).**
Objectif n°1 : **améliorer le scanner** à partir des retours terrain déjà analysés et priorisés
dans `knowledge/FIELD-LOG.md`. Tout est déjà dégrossi (cause probable identifiée, recommandation
donnée) — pas besoin de repartir de zéro, juste dérouler dans l'ordre ci-dessous.

### Chantiers prêts à coder, par priorité
1. **🔴 Score global trompeur ("0/100 · F" + "Install in bad shape" + "FIX THIS FIRST" alors que 0 critical)**
   — confirmé par le rapport complet de FD (grosse collection 2090 tables, aucun problème réel) et sa
   capture d'écran de l'app. Le score/les libellés comptent des "info" (mises à jour dispo) et des
   warnings mineurs comme si c'était grave. **Impact le plus large** : touche systématiquement toute
   grosse collection bien tenue, pas un cas isolé. Détail complet + verbatims dans FIELD-LOG (entrées
   du 30/07). Revoir formule de score et/ou séparer "problèmes réels" (warning+critical) de "informations".
2. **FP `ROM_MISSING` sur tables originales/homebrew sans ROM** (KPI #1, ex. Guardians of the Galaxy,
   Harry Potter homebrew) — RomValidatorScanner/ScriptAnalyzer ne distinguent pas "doit avoir une ROM"
   de "originale, pas de ROM attendue". Piste de donnée : catalogue Orbitalpin.com (tables originales,
   cite justement Harry Potter) en complément de la base VPS déjà utilisée (VpsDatabase.cs).
3. **FN `ROM_FOLDER_NOT_FOUND_MULTIDRIVE`** — le module `rom` skip tout le contrôle si VPinMAME est sur
   un lecteur différent du dossier Tables (cas confirmé : Tables sur E, VPX sur D). Piste : lire le
   registre VPinMAME (`HKEY_CURRENT_USER\Software\Freeware\Visual PinMame\globals`, valeur `rompath`)
   plutôt qu'un chemin relatif fixe.
4. **Repair — nettoyage `PinUpDisplay.exe` zombie** — action simple et sûre (terminer un processus),
   origine VPForums. Bon premier `IRepairAction` à ajouter.
5. **Repair — définir le périphérique audio par défaut** — **décision prise : action ponctuelle à la
   demande** (pas de script Startup persistant), cohérent avec le modèle `IRepairAction` existant.
6. **Détection informative "ordre/résolution écrans incohérent avec le profil PinUP"** — **PAS de
   correctif registre** (hors confinement ADR-005, geste physique non automatisable) : finding
   informatif + lien FAQ seulement.
7. Reste du backlog (voir FIELD-LOG §2 pour le détail) : nettoyage dossier PinupSystem (dry-run +
   backup obligatoires — un script communautaire a déjà supprimé des fichiers par erreur), tables
   `.vpt` legacy invisibles dans la recherche PinUP (finding informatif, ne PAS suivre le raccourci
   déconseillé par l'éditeur PinUP), check générique d'espace disque, résidus upgrade Freezy/zedmd
   (DMD noir, E0434352).

### Cas ouverts à suivre (pas encore de code à écrire)
- **Harley Kirkegard (FB)** : ".json error" au lancement, précisions demandées (message exact / capture),
  pas encore répondu par lui. Hypothèse la plus probable : zip mal extrait (exe seul, sans `profiles/`+
  `DemoData/`) — variante du bug packaging du 29/07. Ne pas coder tant que la cause n'est pas confirmée.
- **Demande Future Pinball** (Donald Parker, FB) — hors scope actuel (moteur `.fpt` différent de VPX),
  répondu honnêtement, gardé en veille de demande, pas d'engagement de date.

## État (J+2, sortie de la fenêtre critique)
- Scanner **v0.1.0-alpha LANCÉ**, `PincabToolbox.zip` (exe + profiles/ + DemoData/) sur la release GitHub
  `waylo1/pincab-toolbox`. Landing **pincab-toolbox.vercel.app** live.
- **Traction J+1/J+2** : ~40 téléchargements sur le zip repackagé + 20+ sur l'ancien exe nu (avant retrait) —
  bon signal, relevé fait en fin de journée donc sous-estimé.
- **Communauté** : posts en ligne sur Pincab Passion (recommandé par l'admin, section Logiciels divers),
  VPForums.org et PinballNirvana.com (priorisés après repérage — VPDB.io/Pinsimdb.org/Orbitalpin.com
  écartés comme cibles de post, pas des forums). Post FB "Visual Pinball Addicts" toujours actif (20 likes,
  plusieurs commentaires traités : FP ROM_MISSING assumé, rapport complet FD analysé, question Future
  Pinball répondue, cas .json ouvert).
- **KPI #2 anonymisation : 0 incident** — path-scrubbing confirmé fonctionnel sur le rapport réel de FD.

## Tâches en attente
1. ✅ **Retouche landing FAITE** (30/07 soir) : badge « Lancement d'abord sur Pincab Passion » retiré dans
   `flipsync-site/landing/index.html`, « Développé avec la communauté » gardé. **Reste : `npx vercel --prod`** (déploiement = Maxime).
2. **Dette v0.1.1 (à froid)** : ré-embarquer profil + démo dans un exe unique pour supprimer le format zip. Toujours ouverte.
3. **v0.2 — tranche Repair/écrans : CODÉE (03/08), reste À TESTER SUR CAB RÉEL avant release** (voir MAJ
   du haut + FIELD-LOG §2) : kill PinUpDisplay, audio par défaut (COM non documenté, pas encore relié
   à un Finding), nettoyage PinupSystem (dry-run+backup obligatoires, jamais de suppression), détection
   informative écrans (signal de compte, pas d'ordre). UI Repair toujours non câblée (HANDOFF à
   reconfirmer). + check résidus Freezy/zedmd **toujours bloqué tant que la cause n'est pas confirmée
   par l'utilisateur**.
4. **Répondre à Gregg** (FB « Virtual Pinball and VPin Cab Builders ») — sa liste exacte de tables a été
   perdue dans l'écrasement disque, à redemander. Lui proposer de **relancer le scan avec le prochain
   build** : le fix des commentaires VBScript devrait déjà faire tomber une partie de ses « criticals ».
   Cas ouverts à trancher avec lui : **Rocky & Bullwinkle** (vraie table à ROM `rab_*` sauf re-thème,
   donc critical probablement correct) et le **B2S Bigus(MOD)**.
5. **Répondre à Chad Greenaway** — sa demande de filtre mods est **faite**, celle du lien direct VPS est
   à moitié faite : il manque le format d'URL. **Ouvrir une fiche table sur le site VPS, copier le
   format, le coller dans `gameUrlTemplate` de `profiles/vpx-popper.json`** — une ligne, pas un rebuild.
6. **Confirmer à FD** que le cas roms multi-lecteur est résolu (fix livré le 30/07, jamais reboucler avec
   lui alors que c'est lui qui l'a remonté).

## Règles absolues (rappel)
Lecture seule · zéro télémétrie · rien de sur-promis · feu vert avant toute action publique. Détail : HANDOFF.

## Consignes ÉCO (important)
- S'appuyer sur cette transmission + `knowledge/FIELD-LOG.md`, **pas** de relecture massive du reste.
- Pour coder les chantiers Repair listés ci-dessus, lire ADR-005 et ADR-006 (courts, ciblés) plutôt que
  toute la doc d'architecture.
- Communauté/rédaction = **ne pas ouvrir le code, ne pas piloter le PC**.
- Éviter les grosses captures quand le texte suffit ; automatisation desktop seulement si vraiment nécessaire.
- **Piste demandée par Maxime** : Obsidian (+ graph/« graphify ») pour centraliser le savoir projet en
  **UN index concis** que Claude lit au lieu de N docs. Toujours à explorer, pas fait cette session.
