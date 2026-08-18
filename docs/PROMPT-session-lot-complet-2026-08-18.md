# Prompt pour la prochaine session Cowork — lot complet du 18/08

> À copier-coller tel quel dans une nouvelle session Cowork.
> Structure K.E.R.N.E.L : **K**adrage, **E**nvironnement, **R**éférences, **N**on-négociables,
> **É**tapes, **L**ivrables.
>
> **Recommandation de modèle/effort : Sonnet, effort ÉLEVÉ, pas bas.** Ce lot mélange du câblage
> mécanique (facile) et du jugement fin sur les faux positifs, la conception d'un contrat additif sans
> casser 8 tests existants, et un nouveau flux UX (rescore) — exactement le genre de travail où un
> effort bas produit un résultat qui a l'air fini mais qui devine au lieu de vérifier. Ce projet a déjà
> payé cette facture une fois (l'incident du 30/07, un faux `Warning` qui a coûté de la crédibilité
> publique) — ne reproduis pas ce risque pour économiser du temps de session.

---

## K — KADRAGE (l'objectif, en une phrase)

Tu reprends **Pincab Toolbox / FlipSync** (MC Automation, Maxime Chauvin). Effort élevé, en autonomie
totale — Maxime ne sera pas là pour répondre à des questions pendant cette session.

**Mission : quatre lots indépendants, chacun câblé ET testé, pas seulement écrit.**

1. **LOT SCANNER** — les 4 derniers détecteurs identifiés par l'audit du 05/08 et jamais codés.
2. **LOT REPAIR** — remonter la raison d'échec par item de réparation jusqu'à l'utilisateur.
3. **LOT RESCORE** — afficher le score avant/après un `Apply` réussi (validé par Maxime le 18/08).
4. **LOT TABLE COMPANION TEASER** — capture opt-in « prévenez-moi » sur les findings de colorisation
   déjà émis (validé par Maxime le 18/08).

Chaque lot est indépendant : livre-les dans l'ordre ci-dessous, mais un lot qui bloque ne doit jamais
empêcher les suivants. Chaque lot = son propre commit, annulable d'un `git revert` sans toucher aux
autres.

## E — ENVIRONNEMENT

- **Dépôt** : `/home/claude/pincab-suite` (clone si absent : `https://github.com/waylo1/pincab-toolbox`), branche `main`.
- **`PincabToolbox.App` NE COMPILE PAS dans le sandbox Linux** (`NU1100 : Microsoft.WindowsDesktop.App.Ref`
  introuvable). Fait documenté, pas une régression — vérifie tes changements App par XML bien formé +
  passe de syntaxe Roslyn (plus bas), jamais par compilation réelle.
- **`dotnet` 8.0.129 EST disponible** dans ce sandbox (vérifié le 18/08/2026) :
  ```bash
  dotnet run --project tests/PincabToolbox.Core.Tests -c Release
  dotnet run --project tests/PincabToolbox.Repair.Tests -c Release
  ```
  Ce sont des `TestRunner.cs` maison (pas xunit/vstest) — ça tourne sans réseau. **Baseline vérifiée le
  18/08 : Core 501/501, Repair 156/156.** Lance aussi `-c Debug`. Ne livre rien en dessous de cette
  baseline, sur aucun des 4 lots.
- **Vérifier XAML/C# sans compilateur Windows** :
  ```bash
  python3 -c "import xml.dom.minidom as m; m.parse('src/PincabToolbox.App/MainWindow.xaml')"
  dotnet /usr/lib/dotnet/sdk/8.0.129/Roslyn/bincore/csc.dll -noconfig -target:library \
    -out:/tmp/syntaxcheck.dll src/PincabToolbox.App/MainWindow.xaml.cs \
    src/PincabToolbox.App/Localization/Loc.cs 2>&1 | grep -E 'CS1[0-9]{3}'
  ```
  Zéro ligne en sortie = OK (le reste, `CS0246`/`CS0234`, ce sont des références WPF absentes sous
  Linux, normal).
- **Script de recoupement x:Name/gestionnaires** (`/tmp/verify/xaml_crosscheck.py` s'il subsiste d'une
  session précédente, sinon récris l'équivalent : chaque `Click=` a sa méthode C#, chaque `x:Name`
  utilisé en code-behind existe en XAML). **Baseline connue au 18/08 : 14 « x:Name orphelin »
  attendus** (des `TabItem`/`Border` jamais référencés en code-behind, pas un bug). Une nouvelle
  anomalie que tu n'as pas volontairement introduite et documentée = régression réelle.
- **`git push` REFUSÉ depuis le sandbox.** `git bundle create /home/claude/lot-18-08.bundle main` →
  `SendUserFile` → `mcp__remote-devices__device_commit_files` vers
  `C:\Users\User\Desktop\Pincab suite\pincab-toolbox-v0.1.1-alpha-src\pincab-suite\`. Vérifie que le
  bridge (`mcp__remote-devices__get_device_info`) répond avant d'annoncer une livraison, et regarde le
  champ `rejected` de `device_commit_files`. `git fetch origin` avant de fabriquer le bundle.

## R — RÉFÉRENCES

1. `docs/AUDIT-Scanner-2026-08.md`, `docs/HANDOFF-Sonnet5-scanners-2026-08.md`,
   `docs/SPEC-lot-communaute-2026-08-10.md` (lecture déjà faite pour toi ci-dessous — LOT SCANNER §2).
2. `TRANSMISSION.md` (bloc du haut), `knowledge/FIELD-LOG.md`.
3. ADR-004 (périmètre légal), ADR-010 (doctrine `Note`), ADR-002 (100 % local, zéro télémétrie —
   central pour le LOT TABLE COMPANION TEASER).
4. Le pack de connaissances `knowledge/pack-2026.08.json` est une **deuxième** source de texte
   utilisateur, séparée de `Loc.cs` — un gap de vouvoiement y a été découvert et corrigé le 18/08.
   N'oublie ni l'un ni l'autre pour tout nouveau texte.

---

## LOT SCANNER — les 4 détecteurs restants

### Carte anti-doublon — vérifiée par lecture du code le 18/08/2026

**32 scanners sont déjà écrits ET câblés** (vraie ligne `.Add(...)` dans `MainWindow.xaml.cs`
L.723-757) : `RomValidatorScanner, BitnessScanner, CompletenessScanner, CompatibilityScanner,
VpxVersionScanner, BlockedFileScanner, DependencyScanner, DiskSpaceScanner, LegacyTableScanner,
PinupDisplayZombieScanner, DisplaySetupScanner, OrphanedMediaScanner, UpdateWatcherScanner,
AliasLoopScanner, NvramScanner, AltColorScanner, AltSoundScanner, ScreenTopologyScanner,
JunctionScanner, DirectB2sScanner, PopperPlaylistScanner, AudioStateScanner, DpiScalingScanner,
DmdComPortScanner, LocaleSeparatorScanner, ConfigPhantomScanner, ComHealthScanner, ChainBitnessScanner,
DmdConfigScanner, FeatureEnabledScanner, ScreenResUnparsedScanner, NvramWritabilityScanner`. Ça couvre
tout le Tier A/B du 05/08 et tous les LOT A→J du 10/08, y compris l'extension de `BlockedFileScanner`
à `.exe`/`.ocx`. **Ne recode rien de cette liste.**

**Ce qui reste réellement à faire** :

| Item | Sévérité | Détail |
|---|---|---|
| **`GLOBALCONFIG_B2S_MISSING`** | `Warning` | `GlobalConfig_B2SServer.xml` absent ou introuvable au chemin attendu. Nom de fichier exact confirmé (SPEC §6.1) — c'est déjà désigné comme « candidat n°1 du prochain lot », commence par lui, c'est le plus sûr et le plus petit. |
| **A2 — Font Dependency Checker** | `Note` | Extraire du script les polices `.ttf` requises (scoreboards/DMD), vérifier l'installation Windows. Si le nom extrait est ambigu → silence sur cette police précise. |
| **A3 — Hardcoded-Path Linter** | `Note` | Chemins absolus en dur dans les scripts de table (`"C:\Users\<quelqu'un d'autre>\..."`) pointant vers un fichier absent sous la racine scannée. Risque de FP réel (le chemin peut exister sur CE poste) → biais fort vers le silence, résumé par table, jamais une ligne par occurrence. |
| **A1 — Script Doctor (détection seule)** | `Note` | Copies locales de `core.vbs`/`controller.vbs`/`VPMKeys.vbs`/`nudge.vbs` dans `Tables/`, en présence pure. Le *fix* (fournir le bon fichier via Repair) reste hors périmètre — touche ADR-004, décision produit non prise. **Tu codes uniquement la détection.** |

**Explicitement HORS périmètre, ne les code pas** : DOF/DOFLinx (nouveau domaine produit entier,
cadrage Maxime requis), PuP-Pack au mauvais nom (cadrage « faible », à préciser), copies multiples
d'un composant (touche un scanner existant), chemins Popper invalides (schéma SQLite non confirmé),
`B2STableSettings.xml` local/global (détection partielle non fiable), Runtimes VC++/.NET (rejeté
explicitement — versions changent avec VPX).

### Gabarit et règles

- Une classe pure dans `Core/Services/` (zéro I/O, 100 % testable) + un `IScanner` mince dans
  `Core/Scanning/` (I/O injectée par le constructeur) + un fichier de tests neuf dans
  `tests/PincabToolbox.Core.Tests/`. Clone `VpxVersionComparer`/`VpxVersionScanner`/
  `VpxVersionScannerTests` ou n'importe lequel des 26 lots précédents.
- Aucun scanner existant modifié. Zéro dépendance externe (BCL uniquement).
- Biais silence systématique : fichier/registre illisible, valeur ambiguë, identifiant non confirmé →
  aucun finding.
- `Note` par défaut pour tout ce qui est heuristique. Aucun nouveau `Critical`.
- **Trois langues** (FR/EN/ES, vouvoiement FR « vous » / ES « usted »). Pour chaque nouveau code :
  entrée `Loc.cs` (dictionnaires `Fr`/`En`/`Es`) **et** entrée `knowledge/pack-2026.08.json`
  (`playerFr/En/Es`, `explanationFr/En/Es`, `verificationFr/En/Es`) si le code doit apparaître dans le
  panneau de détail. Vérifie qu'aucun `tu/ton/ta/tes/toi` (FR) ni `tú/tu/tus` (ES) ne s'est glissé.
- Ordre d'abandon si le temps manque : garde `GLOBALCONFIG_B2S_MISSING` → A2 → A3, sacrifie A1 en
  dernier (le plus délicat, le moins urgent).

---

## LOT REPAIR — raison d'échec par item

### Ce qui a déjà été lu pour toi (ne refais pas cette lecture)

Le chemin d'écriture Repair (Preflight/Apply/Undo, journal persistant) est **déjà entièrement câblé**
depuis le 10/08 (ADR-012). Un ancien commentaire du `.csproj` disant le contraire était périmé — corrigé
le 18/08.

Le point exact où la raison d'échec se perd, dans `src/PincabToolbox.Repair/Engine/RepairEngine.cs` :

- `Apply()` (L.344) fait `outcomes[item.ItemId] = ok;` — un simple `bool`, jamais la raison.
- Le backup en échec (L.378-386) écrit déjà `ex.Message` **dans le journal** avant de faire
  `outcomes[item.ItemId] = false;` — le message existe, il est juste jeté à cet endroit.
- `ApplyItem()` (L.420) reçoit déjà un `ExecutionResult.Error` de l'action qui échoue, l'écrit au
  journal (`ChangeFailed`), puis le jette : elle ne retourne que `(bool ok, bool recovery)`.
- `Compensate()` (L.447) : même chose si le rollback échoue.

**La donnée existe déjà partout où elle est produite** — le travail est de la faire remonter jusqu'à
`ApplyResult`, pas de l'inventer.

Côté App, `result.ItemOutcomes` n'est consommé qu'à un seul endroit (`MainWindow.xaml.cs`
L.1997-2006, handler d'Apply) : un comptage affiché dans `RepairApplyStatus` (un `TextBlock` simple).
Côté tests, `ItemOutcomes` est référencé dans **8 endroits** entre `RepairSessionTests.cs` et
`RepairTests.cs`, tous sous la forme `TryGetValue(...) && ok` ou `.Count/.Values.Any(...)`.

### Règles de conception, non négociables

1. **N'élargis PAS le type de `ItemOutcomes`.** Ajoute un nouveau champ à côté, dans `ApplyResult`
   (`Contracts.cs`) :
   ```csharp
   public IReadOnlyDictionary<string, string?> ItemFailureReasons { get; init; } =
       new Dictionary<string, string?>();
   ```
   Une clé = un `ItemId` en échec, une valeur = le message technique (anglais brut, c'est un détail de
   diagnostic comme le fait déjà `RepairLimitation.MessageEn`, pas un texte Finding à traduire).
2. Ne change pas la signature publique de `IRepairEngine.Apply` ni de `RepairSession.Apply`.
3. Si l'item échoue puis que la compensation (rollback) réussit, la raison affichée reste **la cause
   originale**, pas « compensation réussie ». Si la compensation elle-même échoue, `RecoveryRequired`
   reste l'affichage prioritaire existant — pas de raison en double.
4. Backup en échec → raison = `ex.Message` de l'exception attrapée en L.383. Ne relance pas
   l'exception, le comportement actuel (item marqué échoué, plan continue) reste identique.
5. Tests additifs uniquement — n'édite aucune des 8 assertions existantes sur `ItemOutcomes`. Ajoute :
   backup en échec → raison présente · action inconnue → raison contient l'id · `res.Error` d'une
   action → raison = ce message exact · item réussi → pas d'entrée dans `ItemFailureReasons` · dry-run
   forcé → comportement cohérent avec `ItemOutcomes` en dry-run.
6. App (`MainWindow.xaml.cs`, handler d'Apply) : si `failed > 0`, ajoute une ligne par item échoué avec
   sa raison, à la suite du texte existant. Pas besoin d'une clé `Loc.Get` élaborée — nom de l'item +
   `" — "` + raison brute suffit, garde ça simple.

---

## LOT RESCORE — score avant/après un Apply réussi

**Validé par Maxime le 18/08.** Idée : après avoir appliqué des réparations, l'utilisateur veut voir
que ça a marché — un score qui monte est le meilleur argument de conversion du produit (« regarde, 70
→ 92 », exactement ce qu'on veut voir posté sur un forum).

### Contrainte non négociable : aucune donnée inventée

Le projet a une règle stricte (voir `docs/PROMPT-session-refonte-scanner.md` N2) : **jamais de score
estimé ou de delta calculé à partir de ce qui a été « censé » être réparé.** Le score « après » doit
venir d'un **vrai** re-scan, pas d'une soustraction devinée sur les items appliqués. Un item marqué
« appliqué » avec succès ne garantit pas mathématiquement que le Finding correspondant disparaîtra du
prochain scan (cas limite réel : l'utilisateur a modifié autre chose entre-temps, ou le fix touche une
condition que le scanner réévalue différemment).

### Design attendu

1. **Ne PAS relancer un scan automatiquement** après `Apply` — un scan peut prendre de quelques
   secondes à plusieurs dizaines de secondes sur une grosse install, et le lancer sans geste explicite
   gèlerait l'UI au pire moment (juste après avoir cliqué Apply). À la place :
   - Capture `_report.Score` **avant** l'Apply (tu l'as déjà en mémoire, c'est `_report`).
   - Après un `Apply` réussi (`failed == 0`, ou même partiellement réussi), affiche un bouton ou lien
     clairement visible « Revoir mon score » à côté de `RepairApplyStatus` (nouveau `x:Name`, pattern
     `BtnCloseDetail`/boutons existants).
   - Au clic, relance le **même** scan (même root, même profil) — réutilise le chemin de scan existant
     (`BtnScan_Click` ou la méthode qu'il appelle), pas une copie divergente.
   - Une fois le nouveau `_report` disponible, affiche `{ancien} → {nouveau}` quelque part visible
     (zone `RepairApplyStatus` ou juste au-dessus), avec une formulation honnête si le score n'a PAS
     bougé ou a baissé (ça peut arriver, ne le cache pas — c'est cohérent avec ADR-010 « jamais annoncer
     un résultat non vérifié »).
2. Localisation : nouvelle clé `Loc.cs` du genre `["repair.rescore.button"]` /
   `["repair.rescore.result"]` (avec `{0}`/`{1}` pour l'ancien/nouveau score), FR/EN/ES, vouvoiement
   respecté.
3. Teste au moins le cas simple manuellement en lisant le code (l'App ne compile pas ici) — vérifie que
   le nouveau bouton est bien câblé (`Click=`), que la méthode existe, que rien dans le flux existant
   n'est cassé (le re-plan après Apply qui existe déjà, L.2009-2010, doit continuer à tourner).

---

## LOT TABLE COMPANION TEASER — capture opt-in sur les findings colorisation

**Validé par Maxime le 18/08.** Idée : les findings `ALTCOLOR_INCOMPLETE` / `ALTSOUND_SAMPLE_MISSING`
(déjà émis aujourd'hui par les scanners existants) sont exactement le public cible du futur produit
payant Table Companion (audit §8.2). Un simple « prévenez-moi » capté à cet endroit précis, c'est de
la construction de liste d'attente gratuite, sur l'audience la plus qualifiée possible.

### Design imposé — zéro infrastructure, zéro décision supplémentaire requise de Maxime

**Pas de formulaire, pas de service tiers, pas d'appel réseau.** Un simple lien `mailto:` — ça respecte
ADR-002 à la lettre (aucun appel réseau, même pas optionnel), ça ne demande à Maxime de choisir aucun
service de capture d'e-mails aujourd'hui, et ça reste un vrai geste explicite (l'utilisateur écrit et
envoie l'e-mail lui-même, rien n'est automatique).

1. Dans le panneau de détail d'un Finding `ALTCOLOR_INCOMPLETE` ou `ALTSOUND_SAMPLE_MISSING` (le
   `Border x:Name="DetailPanel"` du Lot 7, onglet Scanner → Tous les résultats), ajoute une ligne
   discrète, visible seulement pour ces deux codes précis : un `TextBlock`/`Hyperlink` qui ouvre
   ```
   mailto:TODO-ADRESSE-DE-CONTACT@exemple.com?subject=Table%20Companion&body=...
   ```
   **Laisse `TODO-ADRESSE-DE-CONTACT` explicitement en placeholder dans le code**, avec un commentaire
   `<!-- TODO Maxime: remplacer par ta vraie adresse de contact avant publication -->` — c'est la seule
   décision que Maxime doit prendre lui-même sur ce lot, ne la devine pas.
2. Texte, dans les 3 langues, honnête et sans survendre (le produit n'existe pas encore) : quelque
   chose comme « Table Companion (gestion de la colorisation) n'est pas encore sorti — être prévenu à la
   sortie ? » avec un lien « M'écrire ». Nouvelles clés `Loc.cs` (`["teaser.tablecompanion.text"]`,
   `["teaser.tablecompanion.link"]`), FR/EN/ES.
3. N'affiche ce bloc QUE pour ces deux codes — pas un bandeau général, pas sur tous les Findings. Un
   moyen simple : dans le code qui peuple `DetailPanel`, ajoute une condition sur `finding.Code`.
4. Vérifie XML bien formé + 0 `CS1xxx` après ce changement (même méthode que le reste du lot).

---

## Vérification finale et livraison (pour les 4 lots)

1. Pour chaque lot livré : les 3 contrôles (XML bien formé, 0 `CS1xxx`, crosscheck sans nouvelle
   anomalie non documentée) + Core **et** Repair vert en Debug **et** Release.
2. Commit séparé par lot, message clair, annulable d'un `git revert` sans dépendre des autres lots.
3. `git bundle create /home/claude/lot-18-08.bundle main`, vérifie-le, `SendUserFile`, dépose-le via
   `device_commit_files` (vérifie le bridge d'abord), donne les commandes PowerShell exactes dans
   l'ordre (`git fetch .\lot-18-08.bundle main` puis `git merge FETCH_HEAD` puis `.\build.cmd` —
   Maxime est sur PowerShell, pas cmd.exe).
4. Mets à jour `TRANSMISSION.md` (bloc du haut) et `knowledge/FIELD-LOG.md`.
5. **Revue CTO + Produit en clôture**, comme sur chaque session chez Maxime : le code est-il propre,
   l'architecture reste-t-elle cohérente, les tests sont-ils suffisants, chaque lot apporte-t-il une
   vraie valeur utilisateur, y a-t-il un risque technique ou commercial, une amélioration à faible coût
   à proposer **sans la coder**.
6. Si un lot n'est pas fini faute de temps, dis-le explicitement dans le message final avec ce qui
   reste — ne fais jamais semblant qu'un lot est terminé s'il ne l'est pas.

### À savoir sur Maxime

Il travaille sur **Windows**, dépôt à
`C:\Users\User\Desktop\Pincab suite\pincab-toolbox-v0.1.1-alpha-src\pincab-suite`, build avec
`build.cmd`, shell **PowerShell** (pas cmd.exe — `Remove-Item -Recurse -Force`, pas `rmdir /s /q`).
Chaque aller-retour lui coûte un build complet — une passe complète et vérifiée vaut mieux que cinq
retouches. **Il parle en solo dans tous ses posts publics** — n'écris jamais de texte marketing au
« nous » si ce lot en produit.
