# Prompt pour la prochaine session Cowork — détecteurs Scanner restants

> À copier-coller tel quel dans une nouvelle session Cowork.
> Structure K.E.R.N.E.L : **K**adrage, **E**nvironnement, **R**éférences, **N**on-négociables,
> **É**tapes, **L**ivrables.

---

## K — KADRAGE (l'objectif, en une phrase)

Tu reprends **Pincab Toolbox / FlipSync** (MC Automation, Maxime Chauvin). Effort élevé, en autonomie
totale — Maxime ne sera pas là pour répondre à des questions pendant cette session.

**Mission : coder, câbler ET tester les derniers détecteurs Scanner qui restent réellement à faire.**
Le mot important est « réellement » — ce document existe précisément parce que deux sessions
précédentes (05/08 et 10/08) ont déjà couvert la quasi-totalité du backlog identifié par l'audit.
**Ne recode rien qui existe déjà** : la carte anti-doublon du §R ci-dessous fait la moitié du travail
à ta place. Chaque item que tu codes doit finir **câblé** (une vraie ligne `.Add(new XxxScanner())`
dans `MainWindow.xaml.cs`) **et testé** (vert Core + Repair, Debug et Release) — un scanner écrit
mais jamais ajouté à la chaîne de scan n'existe pour aucun utilisateur.

## E — ENVIRONNEMENT

- **Dépôt** : `/home/claude/pincab-suite` (clone si absent : `https://github.com/waylo1/pincab-toolbox`), branche `main`.
- **`PincabToolbox.App` NE COMPILE PAS dans le sandbox Linux** (`NU1100 : Microsoft.WindowsDesktop.App.Ref`
  introuvable). Fait documenté, pas une régression — ne perds pas de temps à le contourner.
- **`dotnet` 8.0.129 EST disponible dans ce sandbox** (vérifié le 18/08/2026, contrairement à ce que
  disent d'anciens documents du repo) :
  ```bash
  dotnet run --project tests/PincabToolbox.Core.Tests -c Release
  dotnet run --project tests/PincabToolbox.Repair.Tests -c Release
  ```
  Ce ne sont **pas** des projets xunit/vstest — un `TestRunner.cs` maison avec un vrai `Main()`, donc
  ça tourne sans avoir besoin d'accès réseau à nuget.org pour un testhost. **Baseline actuelle,
  vérifiée le 18/08 : Core 501/501, Repair 156/156.** Ne livre rien qui fasse baisser ces chiffres.
  Lance `-c Debug` en plus avant de considérer un item fini.
- **Vérifier du XAML/C# sans compilateur Windows** (méthode éprouvée du projet) :
  1. `python3 -c "import xml.dom.minidom as m; m.parse('src/PincabToolbox.App/MainWindow.xaml')"`
  2. Passe de syntaxe C# — **seules** les erreurs `CS1xxx` comptent (le reste, `CS0246`/`CS0234`/etc.,
     ce sont des références WPF absentes sous Linux, normal) :
     ```bash
     dotnet /usr/lib/dotnet/sdk/8.0.129/Roslyn/bincore/csc.dll -noconfig -target:library \
       -out:/tmp/syntaxcheck.dll src/PincabToolbox.App/MainWindow.xaml.cs \
       src/PincabToolbox.App/Localization/Loc.cs 2>&1 | grep -E 'CS1[0-9]{3}'
     ```
     Zéro ligne en sortie = OK.
  3. Script de recoupement (`/tmp/verify/xaml_crosscheck.py` s'il existe encore dans une session
     précédente ; sinon, écris l'équivalent : extraire tous les `x:Name` et vérifier qu'aucun contrôle
     utilisé par le code-behind n'a disparu, que chaque `Click=` a bien sa méthode C#). **Baseline
     connue au 18/08 : 14 « x:Name orphelin » attendus** (des `TabItem`/`Border` jamais référencés en
     code-behind, pas un bug) — si ton diff en ajoute d'autres que ceux que tu introduis toi-même
     volontairement (et documentés), c'est une vraie régression.
- **`git push` est REFUSÉ depuis le sandbox.** La méthode qui marche :
  ```bash
  git bundle create /home/claude/scanner-detecteurs.bundle main
  ```
  puis `SendUserFile` du bundle **et** `mcp__remote-devices__device_commit_files` pour le déposer
  directement, chemin exact :
  `C:\Users\User\Desktop\Pincab suite\pincab-toolbox-v0.1.1-alpha-src\pincab-suite\`
  Vérifie toujours que le bridge `mcp__remote-devices__get_device_info` répond avant d'annoncer une
  livraison — un bundle envoyé pendant une déconnexion échoue silencieusement côté device_commit_files
  (regarde le champ `rejected` de la réponse). Fais un `git fetch origin` avant de fabriquer le bundle.

## R — RÉFÉRENCES (ce qui fait autorité, et la carte anti-doublon)

1. `docs/AUDIT-Scanner-2026-08.md` — l'audit initial du 05/08 (le *pourquoi*, les preuves terrain).
2. `docs/HANDOFF-Sonnet5-scanners-2026-08.md` — le premier lot codé (05/08, Tier A + Tier B).
3. `docs/SPEC-lot-communaute-2026-08-10.md` — le deuxième lot codé (10/08, LOT A→J + le câblage du
   chemin d'écriture Repair). **Lis en particulier son §2 (carte anti-doublon) et son §6 (backlog
   spécifié mais pas codé) et son §7 (rejeté, avec la raison — ne rouvre pas ces sujets).**
4. `TRANSMISSION.md` (bloc du haut) et `knowledge/FIELD-LOG.md`.
5. ADR-004 (périmètre légal), ADR-010 (doctrine `Note`, jamais de pourcentage de confiance), ADR-005
   (registre d'actions Repair fermé — non concerné par cette session, tu ne touches pas Repair).

### Carte anti-doublon — VÉRIFIÉE PAR LECTURE DU CODE le 18/08/2026, PAS supposée

Les **32 scanners suivants sont déjà écrits ET câblés** (vraie ligne `.Add(...)` dans
`MainWindow.xaml.cs` L.723-757) : `RomValidatorScanner, BitnessScanner, CompletenessScanner,
CompatibilityScanner, VpxVersionScanner, BlockedFileScanner, DependencyScanner, DiskSpaceScanner,
LegacyTableScanner, PinupDisplayZombieScanner, DisplaySetupScanner, OrphanedMediaScanner,
UpdateWatcherScanner, AliasLoopScanner, NvramScanner, AltColorScanner, AltSoundScanner,
ScreenTopologyScanner, JunctionScanner, DirectB2sScanner, PopperPlaylistScanner, AudioStateScanner,
DpiScalingScanner, DmdComPortScanner, LocaleSeparatorScanner, ConfigPhantomScanner, ComHealthScanner,
ChainBitnessScanner, DmdConfigScanner, FeatureEnabledScanner, ScreenResUnparsedScanner,
NvramWritabilityScanner`. Ça couvre : tout le Tier A/B de HANDOFF (05/08), et tous les LOT A→J de
SPEC-lot-communaute (10/08), y compris l'extension de `BlockedFileScanner` à `.exe`/`.ocx` (LOT E) —
**vérifiée dans le fichier**, ne la refais pas.

**Ce qui reste réellement à faire — c'est ton périmètre, rien d'autre** :

| Item | Source | Sévérité | Pourquoi ce n'est pas encore fait |
|---|---|---|---|
| **A1 — Script Doctor (détection seule)** | AUDIT §4-A1 | `Note` | Le *fix* reste bloqué (fournir `core.vbs` touche ADR-004, décision produit hors de ton périmètre) mais la **détection seule est explicitement autorisée** (HANDOFF R3-e : « la détection seule reste permise en Note ») |
| **A2 — Font Dependency Checker** | AUDIT §4-A2 | `Note` | Jamais codé, P2, valeur moyenne |
| **A3 — Hardcoded-Path Linter** | AUDIT §4-A3 | `Note` | Jamais codé, P2, valeur moyenne |
| **`GLOBALCONFIG_B2S_MISSING`** | SPEC §6.1 | `Warning` | Backlog spécifié le 10/08, jamais codé — **le doc le désigne lui-même comme « candidat n°1 du prochain lot »**, nom de fichier exact confirmé, effort/valeur excellent |

**Explicitement HORS de ton périmètre — ne les code pas, même si tu penses avoir une bonne idée** :
DOF/DOFLinx (SPEC §6.6 — nouveau domaine produit entier, cadrage Maxime requis avant toute spec) ·
PuP-Pack au mauvais nom (SPEC §6.2 — cadrage « faible », à préciser avant de coder) · copies multiples
d'un composant (SPEC §6.3 — suppose de toucher un scanner existant) · chemins Popper invalides
(SPEC §6.4 — bloqué sur confirmation de schéma SQLite) · `B2STableSettings.xml` local/global
(SPEC §6.5 — détection partielle seulement, pas fiable) · tout ce qui est en §7 de SPEC (rejeté avec
raison, ne redébats pas) · Runtimes VC++/.NET (rejeté explicitement §7 — versions changent avec VPX,
un check faux enverrait les gens installer au hasard).

## N — NON-NÉGOCIABLES

1. **Le gabarit à cloner, sans en inventer un autre** : une classe pure dans `Core/Services/`
   (zéro I/O, 100 % testable), un `IScanner` mince dans `Core/Scanning/` (I/O injectée par le
   constructeur), un fichier de tests neuf dans `tests/PincabToolbox.Core.Tests/` (le `TestRunner`
   découvre par réflexion toute méthode `public static void Test_*`, aucune modif du csproj). Regarde
   `VpxVersionComparer`/`VpxVersionScanner`/`VpxVersionScannerTests` ou n'importe lequel des 26 lots
   précédents comme référence.
2. **Aucun scanner existant modifié.** Fichiers neufs uniquement + la ligne `.Add(...)`.
3. **Zéro dépendance externe.** BCL uniquement.
4. **Biais silence, systématiquement.** Fichier/registre illisible, valeur ambiguë, identifiant non
   confirmé par une source primaire → aucun finding, jamais un « je devine ». C'est la règle qui a
   déjà évité tous les faux positifs de ce projet, ne la relâche pas.
5. **Sévérité `Note` par défaut pour tout ce qui est heuristique** (doctrine ADR-010, voir HANDOFF
   §« Doctrine Note » pour le détail). `Note` ne bouge jamais le score, ne déclenche jamais
   « FIX THIS FIRST ». Le seul candidat `Warning` de cette session est `GLOBALCONFIG_B2S_MISSING`
   (présence de fichier = fait déterministe, pas un jugement). **Aucun nouveau `Critical`** — le seul
   `Critical` autorisé depuis le gel (`VPINMAME_NOT_REGISTERED`) a déjà ses 4 conditions et n'est pas
   dans ton périmètre.
6. **Trois langues, pas deux.** Le projet supporte FR/EN/ES depuis le 14/08 et le vouvoiement (FR
   « vous », ES « usted ») est la norme depuis le 17-18/08 dans tout le texte utilisateur — ancien
   piège découvert cette semaine : `knowledge/pack-2026.08.json` (le pack de connaissances, séparé de
   `Loc.cs`) est une **deuxième** source de texte utilisateur, facile à oublier. Pour chaque nouveau
   code `Note`/`Warning`, ajoute une entrée dans **`Loc.cs`** (FR/EN/ES, dictionnaires `Fr`/`En`/`Es` —
   textes courts) **ET** dans **`knowledge/pack-2026.08.json`** (`playerFr/En/Es`, `explanationFr/En/Es`,
   `verificationFr/En/Es`, calque le patron d'une entrée voisine) si le code doit apparaître dans le
   panneau de détail. Vérifie ensuite qu'aucun `tu/ton/ta/tes/toi` (FR) ni `tú/tu/tus` (ES) ne s'est
   glissé dans ce que tu écris.
7. **Vérification finale obligatoire** : les 3 contrôles du §E (XML bien formé, 0 erreur `CS1xxx`,
   crosscheck x:Name/gestionnaires sans nouvelle anomalie non documentée) + Core/Repair vert en Debug
   **et** Release. Un item n'est « fait » que vert des deux côtés — sinon il reste non livré, tu logges
   pourquoi et tu passes au suivant (ne bloque jamais la session entière sur un seul item).

## É — ÉTAPES

1. Confirme la baseline (Core 501/501, Repair 156/156) avant de toucher quoi que ce soit.
2. **`GLOBALCONFIG_B2S_MISSING` d'abord** — le plus petit, le plus sûr, l'échauffement. Fichier
   `GlobalConfig_B2SServer.xml` absent ou introuvable au chemin attendu → `Warning`. Nom de fichier
   exact confirmé par SPEC §6.1, pas de nouvelle recherche nécessaire.
3. **A2 Font Dependency Checker** — extraire du script les polices `.ttf` requises (scoreboards/DMD),
   vérifier l'installation Windows (dossier Fonts, ou énumération GDI si tu trouves un moyen fiable
   sans dépendance). Si le nom exact d'une police extraite est ambigu → silence sur cette police précise,
   pas de finding deviné.
4. **A3 Hardcoded-Path Linter** — détecter des chemins absolus en dur dans les scripts de table
   (`"C:\Users\<quelqu'un d'autre>\..."`) pointant vers un fichier qui n'existe pas sous la racine
   scannée. Le risque de FP est réel (un chemin en dur peut très bien exister sur CE poste) : biaise
   fort vers le silence, `Note` uniquement, résumé par table (pas une ligne par occurrence).
5. **A1 Script Doctor (détection seule)** — le plus délicat. `SharedScriptScanner` détecte des copies
   locales de `core.vbs`/`controller.vbs`/`VPMKeys.vbs`/`nudge.vbs` dans `Tables/`, en présence pure
   (fait) ; si tu trouves un plancher de version fiable et sourcé, tu peux comparer et escalader la
   composante « version » — sinon reste sur la simple présence en `Note`. **Rappel non négociable** :
   tu ne codes QUE la détection. Toute tentation de faire fournir un fichier via Repair est hors
   périmètre (ADR-004, décision Maxime non prise).
6. Pour chaque item livré : Knowledge/Loc (règle N6), `.Add(...)` câblé, tests neufs + Core/Repair vert
   Debug+Release, commit séparé et annulable d'un `git revert`.
7. Fabriquer le bundle, le déposer sur son dépôt, donner les commandes exactes dans l'ordre.
8. Mettre à jour `TRANSMISSION.md` (bloc du haut) et `knowledge/FIELD-LOG.md`.
9. **Revue CTO + Produit en clôture** (attendue par Maxime sur chaque session) : le code est-il propre,
   l'architecture reste-t-elle cohérente, les tests sont-ils suffisants, chaque item apporte-t-il une
   vraie valeur utilisateur, y a-t-il un risque technique ou commercial, et une amélioration à faible
   coût que tu proposes **sans la coder**.

## L — LIVRABLES

- Les scanners livrés (parmi les 4 listés), chacun en un commit propre, câblé et testé.
- Un bundle déposé dans son dépôt + les commandes à lancer, prêtes à copier, dans l'ordre.
- `TRANSMISSION.md` et `knowledge/FIELD-LOG.md` mis à jour, y compris une section explicite
  « ce qui reste » si tu n'as pas eu le temps de finir les 4 items (l'ordre d'abandon en cas de
  budget serré : A1 en dernier — c'est le plus délicat et le moins urgent des quatre, garde
  `GLOBALCONFIG_B2S_MISSING` → A2 → A3 → A1).
- La revue CTO + Produit, en texte, à la fin de ton message final.

### Deux choses à savoir sur Maxime

- Il travaille sur **Windows**, son dépôt est à
  `C:\Users\User\Desktop\Pincab suite\pincab-toolbox-v0.1.1-alpha-src\pincab-suite`, il build avec
  `build.cmd`. Chaque aller-retour lui coûte un build complet — une passe complète et vérifiée vaut
  mieux que cinq retouches.
- **Il parle en solo dans tous ses posts publics** — n'écris jamais de texte marketing/forum à sa
  place qui parlerait au « nous », si jamais cette session en produit.
