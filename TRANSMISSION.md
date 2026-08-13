# TRANSMISSION — reprise Pincab Toolbox / FlipSync (session éco)  ·  MAJ 13/08/2026

## 🧪 MAJ 13/08 (septies) — point 5/6 : `Scenarios.cs` + `RowPlanning.cs` déplacés dans `PincabToolbox.Core.Diagnostics`, `PincabToolbox.App.Tests` retiré

> Point 4/6 clos, signal « GO » de Maxime reçu (avec une note : les autres pistes repérées en cours
> de route — `COM_BITNESS_GAP` notamment — seront reprises après le point 6/6, pas oubliées).
>
> Point 5/6, tel que prévu depuis le départ : `Scenarios.cs` (créé point 3, enrichi point 4) et
> `RowPlanning.cs` (`ChainRowPlanner`/`TableRowPlanner`, extraits point 3 en mini-tranche anticipée)
> déménagent de `PincabToolbox.App` vers **`PincabToolbox.Core.Diagnostics`** (nouveau dossier,
> même convention qu'`Models`/`Scanning`/`Reporting`). Conforme à l'ADR-012 : la logique de décision
> vit dans un assembly testable, pas dans l'App.
>
> **Seul changement de comportement fait par ce déplacement** (et strictement nécessaire, pas un
> effet de bord) : `Scenarios.DetectAll`/`Detect` lisaient `PincabToolbox.App.Localization.Loc.Lang`
> directement. Core ne peut pas référencer App (la dépendance va dans l'autre sens), donc les deux
> méthodes prennent maintenant un paramètre `bool fr` explicite. Le seul appelant
> (`MainWindow.xaml.cs`, ligne ~812) passe maintenant `Loc.Lang == "fr"` — comportement utilisateur
> strictement identique, juste la façon dont la langue voyage jusqu'à la fonction.
>
> **`ChainRowPlanner`/`TableRowPlanner` (RowPlanning.cs) n'ont demandé aucun changement de
> comportement** — ils ne dépendaient déjà que de `PincabToolbox.Core.Models`, le déplacement est un
> simple changement de namespace.
>
> **`PincabToolbox.App.Tests` retiré en entier** (projet, dossier, entrée `.sln`) — c'était
> explicitement un pont temporaire (son propre commentaire de `.csproj` le disait : « point 5 devrait
> retirer ce lien-par-fichier ») dont le seul travail était de tester ces deux fichiers sans le SDK
> Windows Desktop. Une fois les deux dans Core, `PincabToolbox.Core.Tests` les couvre nativement —
> plus besoin du pont. `ScenariosTests.cs`/`RowPlanningTests.cs` déménagent tels quels dans
> `tests/PincabToolbox.Core.Tests/`. Bonus du passage à `fr: bool` explicite : le vieux helper
> `WithLang`/`Loc.SetLang` (qui manipulait un état statique process-wide, documenté comme source de
> fragilité potentielle par le fichier lui-même) disparaît — les tests FR/EN passent juste `fr: true`/
> `fr: false` directement, plus robuste et plus court.
>
> `build.cmd` : étape `[4/6]` (App.Tests) supprimée, renumérotation `[1/5]`→`[5/5]`, commentaire ajouté
> à l'étape Core pour expliquer où vit désormais cette couverture.
>
> Vérifications avant livraison : Core.Tests **488/488** (439 + 49, migration exacte du compte
> App.Tests d'avant), Repair 145/145. Bonus inattendu et bienvenu : `PincabToolbox.Core` compile
> maintenant pour de vrai dans ce sandbox (`dotnet build`, 0 warning/0 erreur) — contrairement à
> l'App, Core n'a jamais eu de dépendance WPF, donc ce déplacement lui fait gagner une vérification
> plus forte que le `csc -t:library` syntax-only utilisé jusqu'ici pour tout le reste. `csc
> -t:library` sur l'App (6 fichiers .cs restants, 2 de moins qu'avant) : toujours uniquement
> CS0234/CS0246/CS0518/CS0656, zéro CS1xxx.

## 🧪 MAJ 13/08 (sexies) — point 4/6 : 3 nouveaux scénarios dans `Scenarios.cs`, 11 tests

> Point 3/6 est clos (signal « GO » de Maxime reçu). Point 4/6 : ajouter des scénarios à
> `Scenarios.DetectAll`. Choix des 3 scénarios fait à partir des codes de finding existants
> **non déjà couverts** par les 3 scénarios en place — zéro corrélation inventée : chacun des trois
> reprend le même patron déjà validé pour « VPinMAME registration missing » (point 3/6 quater) — un
> seul code, ou deux codes qui mesurent littéralement la même chose deux fois, MinMatch = 1, déjà un
> diagnostic complet à lui seul.
>
> 1. **`BITNESS_MISMATCH_VPM32`** (`BitnessScanner.cs`, Critical, jamais utilisé jusqu'ici) — le
>    miroir exact du premier scénario existant, mais dans l'autre sens (VPX 32-bit + VPinMAME
>    64-bit-only au lieu de VPX 64-bit + VPinMAME 32-bit-only). Def séparée plutôt que fusionnée dans
>    le scénario 1 : son texte de chaîne est câblé en dur ("VPX 64-bit / VPinMAME 32-bit") et serait
>    faux lu à l'envers.
> 2. **`COM_STALE_PATH`** (`ComHealthScanner.cs`, Warning) — le composant EST enregistré mais le
>    chemin enregistré n'existe plus sur le disque (les deux faits sont mesurés par le scanner, pas
>    devinés). `BaseConfidence` = 68 (un cran sous les scénarios Critical à 80 : la sévérité sous-
>    jacente est Warning, la confiance doit le refléter).
> 3. **`ALTSOUND_PRESENT_NOT_ENABLED` + `ALTCOLOR_PRESENT_NOT_ENABLED`** (`FeatureEnabledScanner.cs`,
>    Note, LOT D) — "pack son/couleur installé mais l'option VPinMAME qui l'active est encore à 0".
>    MinMatch = 1 : ce n'est pas une corrélation entre deux choses différentes façon scénario 1/2,
>    c'est le MÊME patron mesuré deux fois (son, couleur) — l'un seul suffit déjà comme diagnostic
>    complet, les deux ensemble ajoutent juste la deuxième paire de cases dans la chaîne causale.
>
> 11 nouveaux tests dans `ScenariosTests.cs` : déclenchement à MinMatch=1 pour chacun, filtrage de
> chaîne (le cas AltSound seul ne montre pas les cases AltColor et vice-versa), calcul de confiance
> exact, non-collision avec le scénario 1 existant (même famille BITNESS_* mais Def différente, codes
> disjoints), textes FR, et cohabitation des 6 scénarios (3 anciens + 3 nouveaux) quand tous leurs
> codes co-occurrent dans un même scan.
>
> Vérifications avant livraison : Core 439/439 (inchangé, ce point ne touche pas Core), Repair
> 145/145 (inchangé), App.Tests **49/49** (38 + 11 nouveaux). `csc -t:library` sur les 8 fichiers .cs
> de l'App : toujours uniquement CS0234/CS0246/CS0518/CS0656, zéro CS1xxx. Diff strictement scopé à
> `src/PincabToolbox.App/Scenarios.cs` + `tests/PincabToolbox.App.Tests/ScenariosTests.cs`, rien
> d'autre touché.

## 🧪 MAJ 13/08 (quinquies) — point 3/6 suite : décision de Maxime = extraire maintenant. `ChainRowPlanner` + `TableRowPlanner`, 20 tests, MainWindow rebranché dessus

> Maxime a choisi l'option « extraire maintenant » (mini-tranche anticipée du point 5) plutôt que
> reporter ou se contenter d'une vérification manuelle. Fait, pour 2 des 4 méthodes `Build*`
> bloquées — les deux dont la logique de décision est raisonnablement isolable sans toucher à
> `_report!`/l'état d'instance de fond en comble.
>
> **`ChainRowPlanner.Plan`** (nouveau `src/PincabToolbox.App/RowPlanning.cs`, WPF-free) — sort de
> `BuildChainRows` la décision « quelle est la rupture bon→cassé (le ✕→ rouge), quelle flèche pour
> les autres cases ». 7 tests, dont un qui **documente un comportement qu'on aurait pu casser sans
> le voir** : la règle est purement locale (case précédente vs case courante), donc une chaîne
> Bon→Cassé→Bon→Cassé marque LES DEUX ruptures, pas seulement la première — pas un bug, mais le
> genre de détail qu'un refactor futur pourrait "corriger" par erreur en une règle globale sans que
> personne ne remarque le changement visuel avant un retour terrain.
>
> **`TableRowPlanner`** (même fichier) — sort de `BuildTableRows` les 3 décisions de colonne
> (ROM/Backglass/Frontend) : quel finding gagne, quelle sévérité réelle s'applique, quand la colonne
> entière doit se taire (Backglass si `completenessFailed`, Frontend si la base Popper n'a pas pu
> être lue). 13 tests — dont exactement le genre de garde-fou qui a déjà fait mal ailleurs dans ce
> code (severity par défaut Info, jamais inventée ; silence n'est pas une mesure).
>
> **MainWindow.xaml.cs rebranché** sur les deux planners — comportement identique, juste la décision
> qui vit maintenant dans du code testé au lieu d'être inline dans la boucle WPF. Vérifié : `csc
> -t:library` sur les 7 fichiers de l'App, uniquement CS0234/CS0246/CS0518/CS0656, zéro CS1xxx.
>
> **`BuildCauseCard` et `BuildComponentRows` restent non extraits, volontairement** : les deux
> touchent `_report!`/l'état d'instance de façon plus large (agrégation multi-findings, formatage
> Loc pluriels/singuliers) — une extraction propre y ressemblerait plus à faire le point 5 en entier
> qu'à une mini-tranche. Laissés pour de vrai au point 5.
>
> **Vérifié** : Core 439/439, Repair 145/145, **App.Tests 38/38** (18 Scenarios + 20 nouveaux
> RowPlanning, tous réels/exécutés).

## 🧪 MAJ 13/08 (quater) — point 3/6 : tests `Scenarios.DetectAll` faits (18, réels) ; les `Build*` de MainWindow, structurellement impossibles à tester dans ce sandbox — décision à prendre

> **Moitié claire, moitié bloquée — dit maintenant plutôt que découpé en silence.** Le point 3
> demandait des tests pour `Scenarios.DetectAll` ET les méthodes `Build*` de `MainWindow.xaml.cs`.
> Après investigation, ce sont deux problèmes de nature complètement différente.
>
> **`Scenarios.DetectAll` : fait, 18 tests réels, tous exécutés et verts.** Le fichier ne dépend que
> de `Loc.Lang` (simple champ statique) — zéro type WPF. Nouveau projet
> `tests/PincabToolbox.App.Tests/` (câblé dans `PincabToolbox.sln` et `build.cmd`, étape [4/6]) qui
> compile `Scenarios.cs` et `Loc.cs` **par lien de fichier direct** (`<Compile Include>` vers les
> fichiers de l'App, pas de `ProjectReference` vers `PincabToolbox.App.csproj`, qui exige le SDK
> Windows Desktop absent ici). Couverture : MinMatch respecté par scénario, chaîne causale filtrée
> aux seuls codes réellement matchés, confiance qui grandit avec le nombre de codes ET plafonne à 96,
> un code hors scénario n'inflate rien, tri par confiance, FR/EN, `Detect` vs `DetectAll`.
>
> **Les `Build*` de `MainWindow.xaml.cs` : structurellement impossible à tester dans CE sandbox
> aujourd'hui — pas une histoire de "pas encore essayé", une histoire de "ne peut pas".** Deux
> blocages qui se cumulent : (1) le SDK Windows Desktop (`net8.0-windows` + `UseWPF`) est requis
> pour ne serait-ce que compiler `Brush`/`SolidColorBrush` (utilisés par `BuildCauseCard`,
> `BuildChainRows`, `BuildComponentRows`, `BuildTableRows`) — inobtenable ici, `nuget.org` est
> bloqué par le proxy du sandbox (testé : 403 sur `api.nuget.org`), donc même le paquet de
> référence WPF (sans exécution, juste pour compiler) est hors de portée. (2) Plus fondamental et
> qui touche TOUS les `Build*`, même ceux qui ne touchent aucun `Brush` (`BuildTextReport`,
> `BuildForumMarkdown`, `BuildBBCode`, `BuildHtmlReport`, `BuildPdfLines`,
> `BuildConfirmationText`) : `MainWindow` est la moitié d'une `partial class` dont l'autre moitié
> (tous les champs `TxtRoot`/`BtnScan`/…) est générée par la compilation du XAML — il n'existe pas de
> fichier source à lier séparément comme pour `Scenarios.cs`. Le compilateur doit voir TOUTE la
> classe en un bloc, XAML généré compris. Rien à voir avec le point 2 (l'export PDF a pu être vérifié
> par lecture indépendante `pypdf` justement parce qu'il ne dépend d'aucun état WPF).
>
> **Décision qui revient à Maxime, pas prise seul** : trois options posées en question ci-dessous —
> extraire maintenant la logique de décision pure de ces 4 méthodes vers des fonctions testables
> (mini-tranche anticipée du point 5), reporter entièrement cette moitié du point 3 pour qu'elle se
> résolve naturellement quand le point 5 déplace `Scenarios.cs` et la decision-logic vers Core, ou se
> contenter d'une vérification `csc` + relecture manuelle (pas de vrais tests, juste ce qui existait
> déjà comme filet de sécurité).
>
> **Vérifié** : Core 439/439, Repair 145/145, **App.Tests 18/18** (nouveau, réel, exécuté — pas du
> syntax-check). `.sln` et `build.cmd` mis à jour et cohérents (renumérotation [1/6]→[6/6], le
> script avait déjà une incohérence [1/4]→[5/5] avant ce point, corrigée au passage puisque j'y
> touchais de toute façon).

## 📄 MAJ 13/08 (ter) — point 2/6 : export PDF, écrit à la main (zéro dépendance), avec de vrais tests Core

> **Plus gros que prévu, dit clairement plutôt que découpé en silence** (non-négociable du point 6) :
> `NuGet.Config` du repo est explicite — « Zero-dependency build: no package sources needed » — et
> ça vaut pour `PincabToolbox.App` aussi (son `.csproj` n'a AUCUN `PackageReference`). Pas de
> PdfSharp/QuestPDF/iTextSharp disponibles : il a fallu écrire un générateur PDF minimal à la main
> (objets, xref, trailer, police Helvetica standard, WinAnsiEncoding, retour à la ligne, pagination).
>
> **Décision d'architecture délibérée** : contrairement à HTML/TXT/MD/BBCode/JSON (tous dans
> `MainWindow.xaml.cs`, jamais compilés ni testés dans ce sandbox), la mécanique PDF pure — pas de
> connaissance de `Finding`/`Loc`, juste "ces lignes de texte, en PDF" — vit dans
> `PincabToolbox.Core/Reporting/PdfDocumentBuilder.cs`. Choix assumé, pas neutre : ça crée une
> incohérence (certains exports dans Core, d'autres dans App) que je note ici plutôt que de la
> cacher. Raison : c'est le morceau le plus risqué de ce point (format binaire, zéro filet de
> sécurité d'une lib tierce) et le seul qui PEUT être réellement testé dans ce sandbox (l'App ne
> compile toujours pas, NU1100). App garde tout ce qui a besoin de `Loc`/`Finding` (composition des
> lignes, localisation, scrub) ; Core ne reçoit que du texte déjà prêt à afficher.
>
> **Deux bugs trouvés et corrigés avant tout test** (donc jamais montés à Maxime dans cet état) :
> `Encoding.ASCII.GetBytes` sur le contenu de page aurait transformé chaque caractère accentué
> (é, à, œ…) en `?` — remplacé par `Encoding.Latin1` (1:1 avec WinAnsi pour les octets 0x00-0xFF).
> Et le positionnement des lignes utilisait `Td` (déplacement RELATIF) comme s'il était absolu — les
> lignes auraient dérivé de plus en plus à droite et vers le haut au fil du texte. Les deux étaient
> invisibles à la simple lecture du code, trouvés en écrivant les tests.
>
> **Vérifié, au-delà de la méthodologie habituelle** : 27 nouveaux tests dans
> `PincabToolbox.Core.Tests` (métriques Helvetica, encodage WinAnsi caractère par caractère,
> retour à la ligne glouton + coupure dure d'un mot trop long, structure PDF — xref/trailer/nombre
> de pages/cohérence Kids-Count). **Core 439/439** (412 + 27), **Repair 145/145**, inchangés.
> En plus des tests : un vrai PDF généré via un harnais jetable (rapport simulé, 80 findings avec
> accents français, guillemets, tiret cadratin, glyphe ✓) puis **relu par `pypdf`** (bibliothèque
> tierce, utilisée uniquement pour cette vérification ponctuelle en sandbox — n'entre pas dans le
> produit livré) : 7 pages, texte extrait lisible, accents corrects, ✓ bien transformé en « OK »,
> pieds de page « page N » présents et incrémentés. Vérification indépendante du code qui a produit
> le fichier, pas juste "le code compile et mes propres tests passent". Passe `csc -t:library` sur
> les 7 fichiers `.cs` de l'App (MainWindow.xaml.cs + Loc.cs modifiés, et l'ensemble du projet en
> contrôle) : uniquement CS0234/CS0246/CS0518/CS0656, **zéro CS1xxx**.
>
> **Corrigé au passage (fold-in accepté par Maxime le 13/08)** : le message de regroupement
> (`ScanScoring.RollupCode`/`GROUPED`, visible sur les captures de Gregg — « 273 similar findings ») disait
> vaguement « the full text report has every one of them » ; dit maintenant explicitement
> « Export as .txt, .pdf or .json to see every one individually » (idem Fr). PDF **n'utilise PAS
> `Rolled()`** comme HTML/MD/BBCode — comme TXT/JSON, tout est affiché (`Ordered()`), c'est
> délibérément le format « je veux tout voir, imprimable/archivable », pas un résumé forum.
>
> **Amélioration à faible coût repérée, NON codée** : les 5 autres builders (HTML/TXT/MD/BBCode/JSON)
> restent non testés et non déplacés — les migrer vers Core suivrait exactement le même
> raisonnement que ci-dessus et donnerait une vraie couverture de test à *tous* les exports, pas
> seulement PDF. Pas fait ici : hors périmètre de ce point, gros changement pour du code qui marche
> déjà et n'a reçu aucun signalement de bug depuis son écriture.

## 💬 MAJ 13/08 (bis) — réponse à Gregg (suite du 12/08) : notre réponse précédente avait tort sur l'export, ROM pas codé sans vérif

> Gregg a répondu au message du 12/08, avec captures d'écran, sur 3 points. **Notre propre réponse du
> 12/08 était fausse sur un point, corrigée ici** : on avait affirmé que HTML/MD/BBCode contiennent
> "the full detail […] not just what's shown in the table" — faux, ces 3 formats appellent
> `r.Rolled()` qui regroupe les findings répétitifs (273×`B2S_ORPHAN` chez Gregg) sous une ligne
> résumé ; seuls **TXT et JSON** utilisent `Ordered()` (rien de regroupé). Gregg a très probablement
> exporté en HTML (le choix par défaut du dialogue) et n'a logiquement rien trouvé de plus détaillé.
>
> **Sur les 2 signalements ROM (Full House 1966 = EM sans ROM digitale ; un homebrew qui tourne
> visiblement bien sans sa ROM) : AUCUN CODE TOUCHÉ.** Vérifié dans `ScriptAnalyzer.AnalyzeRomUsage` —
> `UsesController` n'est vrai que sur un `CreateObject("VPinMAME.Controller")` réel, non commenté
> (commentaires retirés avant analyse). Si ces tables sortent Critical, leur script appelle donc
> vraiment VPinMAME — pas un artefact de mot-clé. Hypothèse la plus probable, NON vérifiée : ces
> scripts créent le contrôleur pour une fonctionnalité optionnelle (son/DMD additionnel) et protègent
> le chargement de la ROM, si bien que la table tourne quand même sans elle — nuance que
> `ScriptAnalyzer` ne fait pas aujourd'hui. Pas de correctif à l'aveugle : la dernière fois qu'une
> détection ROM a été détendue sur une hypothèse non vérifiée (KPI#1), ça a rouvert un vrai faux
> positif ailleurs. Question de clarification renvoyée à Gregg avant tout changement (voir brouillon).
>
> Brouillon prêt : `docs/reply-gregg-2026-08-13.md`. Entrée détaillée dans `knowledge/FIELD-LOG.md`
> (2026-08-13). Correctif à faible coût identifié pour la clarté du message de regroupement (nommer
> explicitement .txt/.json au lieu de « rapport texte complet ») — prévu pour le point 2 (export PDF)
> puisque c'est la même zone de code, PAS mélangé dans ce commit-ci.

## 📀 MAJ 13/08 — point 1/6 : zips ROM factices dans le DemoData, le mode démo raconte enfin une vraie histoire

> **Premier des 6 chantiers de la revue CTO+Produit qui suit le portage Scanner du 12/08**, traité
> seul, dans son propre commit, du plus simple au plus complexe (ordre imposé par Maxime). Reprise
> en session fraîche : le sandbox cloud précédent n'existe plus, tout reconstruit depuis le bundle
> `.git` complet du poste de Maxime (`git bundle create --all`, transféré via le pont device →
> conteneur), `.NET 8 SDK` réinstallé (`apt-get install dotnet-sdk-8.0`, absent du nouveau
> conteneur), fixtures binaires de `PincabToolbox.Core.Tests` régénérées
> (`tests/fixtures/make_fixtures.py`, dossier `out/` gitignoré donc jamais livré). **Baseline
> revérifiée avant tout changement : Core 412/412, Repair 145/145, identique au dernier commit
> connu.**
>
> **Fait** : `DemoData/install/VPinMAME/roms/afm_113b.zip` et `afm_113.zip` — exactement la
> proposition à faible coût notée le 12/08, non codée à l'époque. Chaque zip ne contient qu'un
> fichier texte `*.READ-ME-FAKE-ROM.txt` disant explicitement que ce n'est pas une vraie ROM
> (aucune donnée MAME/VPinMAME, rien de sous copyright) — pour que personne ne s'y trompe si le
> zip est un jour ouvert. `mm_109c.zip` (Medieval Madness) volontairement PAS ajouté : c'est ce
> manque qui donne au démo son résultat Critical.
>
> **Zéro ligne de code touchée.** `RomValidatorScanner` compare déjà uniquement des noms de
> fichiers (`ctx.RomSets`, rempli par un glob `*.zip` sur le dossier roms — jamais le contenu),
> `MainWindow.BuildTableRows`/`BuildCauseCards` pilotent déjà la colonne ROM et les cartes
> uniquement par les codes `ROM_OK/ROM_MISSING/ROM_NOT_REQUIRED/ROM_UNZIPPED`, et les clés
> `Loc["tbl.rom.*"]` existent déjà En/Fr. Un pur changement de données. Seul fichier texte modifié :
> `.gitignore` — `*.zip` est une règle globale (protège contre un commit accidentel des gros zips
> de build/dist qui traînent sur le poste de Maxime) ; ajout d'une exception scopée
> `!src/PincabToolbox.App/DemoData/install/VPinMAME/roms/*.zip` plutôt que d'affaiblir la règle
> partout.
>
> **Effet mesuré** (harnais jetable `dotnet run` contre `PincabToolbox.Core` directement, hors
> Windows donc hors résultats COM/écrans/audio du poste de Maxime — comparaison AVANT/APRÈS dans
> le même environnement, seule chose qui compte ici) :
> - Avant : 16 résultats, score 68/C, 1 critique (`ROMS_DIR_NOT_FOUND` explique un `—` partout en
>   colonne ROM — proche des 17/68/C/1 déjà consignés le 12/08, l'écart d'une unité vient d'un
>   scanner réseau exclu du harnais de vérification, pas d'une régression).
> - Après : 19 résultats, score 57/C, 2 critiques. Attack From Mars → `ROM_OK` (afm_113b, match
>   direct). Aliased Table (Test 2020) → `ROM_OK` via `afm_mod → afm_113` — **la résolution
>   VPMAlias s'exécute enfin dans le pipeline démo**, jusqu'ici jamais exercée que par les tests
>   unitaires de `AliasFile`. Medieval Madness → `ROM_MISSING` **Critical**, nouveau résultat
>   vedette du démo, même histoire que le rapport terrain de Gregg (12/08) : une ROM vraiment
>   manquante sur une table qui pilote vraiment VPinMAME. Original Gem (Homebrew) → confirmé
>   `ROM_NOT_REQUIRED` (aucun `GameName`/`CreateObject` VPinMAME dans son script).
>
> **Root cause cards** : aucun des 3 scénarios actuels (`Scenarios.cs`) ne déclenche sur
> `ROM_MISSING` (vérifié en lisant les `RequiresCode` des chaînes causales : seuls
> `BITNESS_MISMATCH_VPM`, `BITNESS_DMD64_MISSING`, `POPPER_NOT_REGISTERED`, `B2S_MISSING`,
> `VPINMAME_NOT_REGISTERED` sont câblés). Le démo garde donc 2 causes racines réelles, et le
> `ROM_MISSING` de Medieval Madness remonte par le repli déjà prévu pour ce cas (« carte construite
> du résultat le plus grave sans scénario ») — comportement attendu, pas un trou.
>
> **Amélioration à faible coût repérée, NON codée (revue CTO+Produit)** : un scénario MinMatch-1
> dédié à `ROM_MISSING` seul donnerait à ce cas — le plus fréquent en usage réel d'après le retour
> de Gregg — sa propre carte avec phrase joueur/impact au lieu du repli générique. Candidat naturel
> pour le point 4 (nouveaux scénarios), pas pour ce point-ci qui reste un changement de données pur.
>
> **Vérifié** : Core 412/412, Repair 145/145 (relancés après l'ajout, aucune régression — attendu,
> aucun test Core/Repair ne référence `DemoData`, vérifié par recherche). `PincabToolbox.App`
> **toujours pas compilable dans ce sandbox** (NU1100, fait documenté, inchangé) — rien à
> revérifier côté XAML/x:Name/Loc puisqu'aucun fichier App n'a bougé. **À vérifier sur la machine
> de Maxime via `build.cmd` + Mode démo** : Attack From Mars et Aliased Table en vert avec leur ROM
> trouvée, Medieval Madness en rouge « ne démarrera pas », Original Gem en vert « aucune requise »,
> et la carte de tête doit maintenant afficher ce Critical au lieu du fallback précédent.

## 🎨 MAJ 12/08 (bis) — fond du bandeau remplacé par une salle d'arcade, voile renforcé

> Maxime a jugé l'illustration vectorielle précédente illisible (« que des taches ») et a fourni
> une image de salle d'arcade générée. **Titres de tables inventés** (« Cosmic Eclipse »,
> « Quantum Quest », « Neon Outrun »…) : aucun artwork Bally/Williams/Stern, la règle
> « illustrations originales uniquement » tient toujours.
>
> **Piège découvert, à retenir avant de retoucher cet asset.** Sur un écran 1920 maximisé, le
> bandeau fait ~1884×190, soit ~9,9:1, alors que l'asset fait 1920×430 (4,47:1).
> `Stretch="UniformToFill"` + `AlignmentY="Center"` met à l'échelle sur la largeur puis rogne
> verticalement : **seule la bande centrale (~45 % de la hauteur) est réellement visible.** Un
> sujet cadré en haut de l'asset ne s'affiche jamais. Le crop retenu (bas de l'image source :
> plateaux + couloir + sol) place donc les machines au centre.
>
> **Voile renforcé** de `A6/73/59/8C` à `DB/B8/A3/C7` sur `#0B111A` (0,86 / 0,72 / 0,64 / 0,78) :
> la photo est bien plus lumineuse que l'illustration qu'elle remplace, l'accroche rouge ne
> passait plus. Valeurs choisies en comparant les rendus, pas au jugé.
>
> **Asset quantifié en PNG 256 couleurs** : 765 Ko → 186 Ko. Écart moyen mesuré *sous le voile*
> 1,44/255 (max 10) — indiscernable à l'œil, vérifié numériquement avant de committer.
>
> Vérifié : XAML valide, recoupement x:Name/gestionnaires 0 erreur, Core 412/412, Repair 145/145.
> Rendu contrôlé au format réel du bandeau avec l'asset et le voile exacts, mais **l'App n'est
> toujours pas compilable dans ce sandbox** — rendu final à confirmer via `build.cmd`.

## 🖥️ MAJ 12/08 — écran Scanner porté sur la maquette du 11/08, en une passe

> **Mission unique de la session : rendre l'écran Scanner fidèle à
> `docs/maquette-scanner-2026-08-11.html`** (3ᵉ demande de Maxime sur ce point). Portage complet
> en une passe, un commit, annulable d'un `git revert`.
>
> **Livré (tout est piloté par les données du vrai scan, rien d'écrit en dur)** :
> - **Ligne méta** sous le bandeau : mode (Démonstration/Dossier), lancé le, durée
>   (`ScanReport.StartedAt/FinishedAt`), contrôles N/N (`ScanEngine.Scanners.Count`, jamais une
>   constante), tables analysées.
> - **Onglets internes** : Causes racines / Tous les résultats / Composants / Tables / Système,
>   compteurs réels dans les en-têtes. Le tableau des résultats vit dans son onglet et garde son
>   plancher (`MinHeight=240` sur la ligne de Grid — le piège des sessions précédentes).
> - **Cartes de causes racines** : `Scenarios.DetectAll` retourne maintenant la LISTE triée
>   (l'ancien `Detect` reste en façade). Badge = gravité MAX réellement mesurée des déclencheurs
>   (sur le démo, « Intégration frontend » sort en *À noter*, pas en *Avertissement* comme la
>   maquette — Info+Note mesurés), confiance en mots (ADR-010), phrase joueur + impact par
>   scénario (FR/EN dans `Scenarios.cs`), chaîne causale par scénario dont CHAQUE case exige son
>   code déclencheur (`RequiresCode`), pied 🧩 composants / 🎰 tables sur N / 🔎 codes / ⚑
>   réparation manuelle (dérivé de `RepairOfferBuilder.ByCode`) + « Voir les étapes → » qui
>   sélectionne le résultat dans Tous les résultats. Repli sans scénario : une carte construite
>   du résultat le plus grave (ancien comportement du bandeau priorité, même format).
>   **Nouveau scénario** : `VPINMAME_NOT_REGISTERED` seul (MinMatch 1 — Critical LOT A, 4
>   conditions toutes mesurées, diagnostic complet à lui seul).
> - **Colonne de droite** : Résultats critiques (réels, clic → détail), **Santé des composants**
>   alimentée UNIQUEMENT par des résultats réels (`BITNESS_INVENTORY` par rôle, `*_MISSING`,
>   COM par sujet, base Popper lue) — la ligne « FlexDMD · non requis » de la maquette est
>   ÉCARTÉE (déduction du silence). Remarques (Rolled). Encadrés plafonnés à 8 lignes + renvoi
>   (total réel dans l'en-tête) : un ItemsControl ne virtualise pas, pense aux 2000 tables.
> - **Tableau des tables** (vue Causes racines, plafonné 12 + onglet Tables complet en ListBox
>   virtualisée) : ROM = findings `ROM_*` par table (« — » sur le démo actuel : PAS de dossier
>   roms dans DemoData, `ROMS_DIR_NOT_FOUND` l'explique) ; Backglass = `B2S_MISSING` sinon
>   « présent » (contrôle inconditionnel par table, désactivé si SCANNER_ERROR completeness) ;
>   Frontend = lecture POSITIVE de la base Popper (`SqliteReader`, même requête que
>   `CompletenessScanner.LoadPopperGames`) — base illisible → « — », jamais déduit du silence.
> - **Carte réparation** (ADR-006) : l'offre réelle quand elle existe, sinon « aucune réparation
>   automatique disponible » + les 4 types réparables ; `RepairSummaryLine` /
>   `RepairNotAutomatableLine` conservés, déplacés dans la carte. **Onglet Système** : méta du
>   dernier scan + dossiers résolus + OS/CPU/mémoire/écrans mesurés (`RuntimeInformation`,
>   registre, `MonitorTopologyProbe`) — pas de GPU (demanderait WMI, hors zéro-dépendance).
>
> **Vérité terrain à connaître avant de comparer à la maquette** : le scan réel du DemoData donne
> (hors Windows) **17 résultats, score 68/C, 1 critique, 2 causes racines** — pas les 27/38/F de
> la maquette, qui supposait un dossier `roms/` inexistant dans DemoData et des scanners
> registre/écrans muets hors Windows. Sur le poste de Maxime, le démo produira EN PLUS les
> résultats COM/écrans/audio de SA machine. Proposition à faible coût, NON codée : ajouter
> `DemoData/install/VPinMAME/roms/` (afm_113b.zip, afm_113.zip) pour que le démo raconte la même
> histoire que la maquette.
>
> **Supprimés (code-behind mis à jour dans le même commit, non-négociable n°6)** :
> `PriorityBanner/PriorityAccent/PriorityLabel/PriorityText/PriorityExplain/PriorityTriggers/
> PriorityFix/ChainNodes` + classe `ChainNode` → remplacés par `CauseCards` (ItemsControl) et
> `CauseCardRow/CauseChainRow/SideRow/CompRow/TableRowVm`. Pastilles de gravité : intactes,
> toujours filtres cliquables.
>
> **Vérifié** : XML bien formé ; passe `csc` sans références WPF → uniquement
> CS0234/CS0246/CS0518/CS0656 (zéro CS1xxx) ; script de recoupement x:Name ↔ code-behind ↔
> gestionnaires ↔ assets : 0 erreur ; clés Loc : 223/223 En/Fr, zéro doublon ; le VRAI
> `Scenarios.cs` exécuté (Loc stubé) contre le vrai scan démo : 2 scénarios, conf 90/86, chaînes
> et pieds conformes, FR et EN ; **Core 412/412, Repair 145/145, tous verts**.
> `PincabToolbox.App` **toujours pas compilable dans ce sandbox** (NU1100, fait documenté) —
> **jamais compilé ni exécuté réellement : à vérifier en premier via `build.cmd` + Mode démo.**

## 💬 MAJ 12/08 — réponse à Gregg (rapport ROM + "où est le rapport complet"), aucun code touché

> Gregg a relancé (suite du 07/08 "treize") avec captures d'écran d'un vrai scan : un Critical
> `ROM_MISSING` sur 'Full House (Williams 1966)', des Warning FlexDMD/B2S manquants, et deux
> questions — comment ouvrir "le rapport complet", et si le scanner peut éviter les alertes ROM sur
> les tables qui n'en ont pas besoin.
>
> **Vérifié dans le code avant de répondre** (`RomValidatorScanner.cs`) : le scanner fait déjà
> exactement ce que Gregg demande. `ROM_NOT_REQUIRED` sort en `Ok` dès que le script d'une table ne
> pilote pas le contrôleur VPinMAME (originaux/homebrew B2S-only) — jamais de Critical dans ce cas.
> 'Full House' pilote réellement VPinMAME, donc son Critical est exact, pas un faux positif : il lui
> manque juste `Full House.zip` dans son dossier roms. Le "rapport complet" existe déjà (bouton
> "Export report" HTML/TXT/MD/BBCode/JSON + "Copy for forum"), Gregg ne l'avait simplement pas repéré.
>
> **Aucun code changé.** Réponse rédigée dans `docs/reply-gregg-2026-08-12.md`, à poster par Maxime.
> Entrée détaillée dans `knowledge/FIELD-LOG.md` (2026-08-12). Idée à faible coût notée pour une
> prochaine revue produit, **pas codée** : rendre "Export report"/"Copy for forum" plus visibles
> (c'est la 2e fois qu'un utilisateur terrain ne les trouve pas) — candidat pour un menu contextuel
> sur le tableau des résultats.

## 🧪 MAJ 11/08 (quater) — coupe-circuit "simulation forcée" pour le premier test réel sur cabinet

> **Suite directe de l'entrée (ter) juste en dessous, même journée.** Maxime a donné le feu vert
> à l'amélioration proposée en revue CTO+Produit : un moyen de tester tout le chemin d'écriture sur
> sa cab réelle, avec une vraie licence, sans risquer une écriture réelle avant d'être serein.
>
> **Fait** : `RepairSession` lit désormais la variable d'environnement `PINCAB_REPAIR_FORCE_DRYRUN`
> (`1`/`true`/`yes`) à la construction. Si elle est active, `Apply()` ne touche **jamais**
> `RepairEngine` — pas un dry-run simulé à l'intérieur du moteur, un appel réellement sauté :
> aucune action enregistrée, aucun service de backup, aucune écriture ne peut s'exécuter, quel que
> soit un bug futur dans l'un de ces composants. `ApplyResult.ForcedDryRun` porte l'information ;
> `RepairSession.ForceDryRunActive` l'expose pour l'UI. Jamais silencieux (doctrine du projet) : un
> bandeau s'affiche dans l'onglet Repair dès qu'un plan est construit sous ce mode, et le message de
> fin d'Apply dit explicitement "simulation uniquement" plutôt que de laisser croire à une vraie
> application.
>
> **Usage pour Maxime** : lancer `PincabToolbox.exe` avec `PINCAB_REPAIR_FORCE_DRYRUN=1` dans
> l'environnement pour un premier passage complet Preflight → Apply → Undo sur la cab réelle, sans
> aucun risque d'écriture, avant de retirer la variable pour un usage normal.
>
> **Vérifié** : le sandbox a bien subi une panne d'outil temporaire (classifieur de sécurité de
> l'environnement indisponible plusieurs minutes) pendant l'écriture de ce correctif, mais une fois
> revenu, Core 412/412 et Repair 145/145 (140→145 pour les 5 nouveaux tests forced-dry-run), tous
> verts — deux erreurs de compilation réelles trouvées et corrigées au passage (paramètre `msg`
> manquant sur deux `A.True`/`A.False`). `PincabToolbox.App` reste non compilable dans ce sandbox
> (fait déjà documenté) : le fichier édité (`MainWindow.xaml.cs`) a été vérifié par la même méthode
> `csc` sans références WPF que le reste de cette session (aucune erreur CS1xxx), et le XAML reparsé
> comme XML valide — jamais compilé ni exécuté réellement, à vérifier en premier via `build.cmd` sur
> la machine de Maxime.

## 🔑 MAJ 11/08 (ter) — clé de licence RÉELLE déployée, `Apply` n'est plus un no-op prouvé

> **Suite directe de l'entrée juste en dessous, même journée, changement matériel de posture de
> sécurité.** Lire `docs/adr/ADR-012-chemin-ecriture-repair.md` (section "Suite — 11/08/2026") avant
> toute reprise sur Repair ou toute distribution du build.
>
> Maxime a exécuté `license-tool init` sur sa propre machine (hors ligne — la clé privée n'a jamais
> transité par un repo ni une session cloud) et a transmis la clé **publique** résultante.
> `LicenseVerifier.EmbeddedPublicKeyBase64` n'est **plus** le `PLACEHOLDER` littéral décrit dans
> l'entrée précédente : c'est maintenant une vraie clé P-256, embarquée et committée.
>
> **Conséquence directe, à ne pas manquer** : la phrase "Apply est un no-op prouvé en production tant
> que la vraie clé n'est pas déployée" de l'entrée précédente **ne tient plus**. N'importe quelle
> licence signée par la clé privée de Maxime rend maintenant `VerifyLicense` valide, donc active pour
> de vrai les quatre actions déjà câblées du LOT H (`UnblockFileAction`, `RestoreRomArchiveAction`,
> `QuarantineOrphanedMediaAction`, `KillZombiePinUpDisplayAction`). Le filet de sécurité qui rendait
> raisonnable de câbler l'onglet Repair sans jamais l'avoir exécuté sur Windows n'existe plus tel quel.
>
> Ce qui protège encore : aucun parcours d'achat public n'existe (ADR-009 non câblé) — seul Maxime,
> via `license-tool issue` sur sa machine, peut émettre une licence valide aujourd'hui. Toutes les
> autres garanties de code (sélection opt-in stricte, confirmation obligatoire, échec de backup =
> aucune écriture, journal persistant) sont inchangées et indépendantes de la validité de la clé.
>
> **Fait cette session, sur ce changement précis** : `LicenseVerifier.cs` mis à jour + commenté avec
> la date et la provenance de la clé ; nouveau test de non-régression
> `Test_EmbeddedPublicKey_IsARealKey_NotThePlaceholder` (verrouille contre un retour accidentel au
> placeholder) ; `RepairSessionTests` renommé/reclarifié en conséquence ; **Core 412/412, Repair
> 140/140, tous verts** (139→140). ADR-012 complété d'une section "Suite" documentant ce changement
> de posture plutôt que de réécrire silencieusement le raisonnement d'origine.
>
> **Recommandation avant toute distribution plus large** : que Maxime valide lui-même, sur sa
> machine, au moins un cycle complet Preflight → Apply → Undo avec une licence qu'il a émise pour
> lui-même, sur les quatre actions déjà câblées, avant de partager ce build ou cette clé publique
> avec qui que ce soit d'autre.

## ✅ MAJ 11/08 — Lot communauté 10/08 codé et câblé de bout en bout (LOTs A→H), LOT I codé mais délibérément non câblé, ADR-012 écrit

> **Suite directe de l'entrée du 10/08 (bis) ci-dessous — spec exécutée intégralement.** Lire
> `docs/adr/ADR-012-chemin-ecriture-repair.md` avant toute reprise sur Repair.
>
> **Codé et câblé, tous les scanners jusqu'au bout (Loc.cs FR+EN, Knowledge.cs, `.Add(...)`)** :
> LOT A (`ComHealthScanner` — `COM_NOT_REGISTERED`, `COM_STALE_PATH`, `COM_PATH_OUTSIDE_INSTALL`,
> `COM_OK`, `COM_BITNESS_GAP`, et `VPINMAME_NOT_REGISTERED` en `Critical`, ses 4 conditions toutes
> mesurées — jamais de repli en `Warning` sur un échec de lecture registre), LOT B
> (`ChainBitnessScanner`), LOT C (`DmdConfigScanner`, format `[VirtualDMD]` confirmé par lecture
> directe du `DmdDevice.ini` de freezy/dmd-extensions sur GitHub), LOT D (`FeatureEnabledScanner` +
> `AltFeatureRegistry`, confiance de source documentée honnêtement en commentaire), LOT E
> (`BlockedFileScanner` étendu à `.exe`/`.ocx`, seul scanner existant touché), LOT F
> (`ScreenResUnparsedScanner`), LOT G (`NvramWritabilityScanner`, sonde d'écriture réelle). Ordre
> d'abandon (G, F, E, D, C) **non utilisé** — tout livré, rien coupé.
>
> **LOT H — chemin d'écriture Repair câblé pour la première fois, entièrement (jamais à moitié,
> comme exigé).** H.1 (journal persistant `FileRepairJournal`) fait en premier. Nouvelle classe
> `RepairSession` dans `PincabToolbox.Repair` (pas `PincabToolbox.App` — voir ADR-012 pour la
> justification : c'est la seule partie du projet que ce sandbox peut compiler ET tester, donc la
> logique de décision la plus critique du projet y vit entièrement). Licence revérifiée à chaque
> appel, sélection opt-in stricte, confirmation explicite obligatoire pour tout item irréversible.
> **Bug réel trouvé et corrigé** : `RepairEngine.Apply` ne protégeait pas un échec de sauvegarde —
> corrigé, testé (`Test_Apply_BackupFailure_NeverWrites`). Onglet "Repair" ajouté à l'App. Textes
> "à venir" retirés (`about.body`/`about.roadmap`, H.5). **La clé de licence embarquée reste un
> PLACEHOLDER** → `Apply` est un no-op prouvé en production tant que `license-tool init` n'a pas
> tourné pour de vrai — c'est ce qui a rendu raisonnable de câbler l'UI sans pouvoir la tester sur
> une vraie machine Windows.
>
> **LOT I — codé et testé, délibérément NON câblé.** `RegisterComComponentAction` implémente les 7
> règles de confinement de la spec (liste blanche en dur, chemin canonique, zéro argument,
> PE+bitness, timeout, vérification d'élévation au moment de l'usage, jamais réversible) — mais
> aucune `RepairRule` du pack ne la référence, donc elle est inerte en production (même précédent
> que `SetDefaultAudioDeviceAction`). Deux inconnues non validables sans machine Windows réelle :
> l'outil de ré-enregistrement vit-il vraiment à côté de la DLL du composant sur une vraie install,
> et comment chaque outil se comporte-t-il lancé sans argument (`Setup.exe` de VPinMAME est un
> installeur graphique interactif connu). Application directe de la clause de sortie de la spec
> elle-même : "si l'un de ces points ne peut pas être tenu proprement, ne pas livrer le LOT I."
>
> **Build/tests** : `dotnet` disponible dans ce sandbox cette fois — **Core 412/412, Repair 139/139,
> tous verts** (122→139 pour les 17 nouveaux tests LOT I). `PincabToolbox.App` **toujours pas
> compilable ici** (`NU1100`, SDK Windows Desktop absent hors Windows — fait déjà documenté, pas une
> régression) : l'onglet Repair XAML/code-behind n'a été vérifié qu'à la main (XML bien formé, `csc`
> sans les références WPF confirmant l'absence d'erreur de syntaxe malgré l'impossibilité de
> résoudre les types WPF) — **jamais compilé ni exécuté réellement, à vérifier en premier sur la
> machine de Maxime.**
>
> **Non fait** : la `RepairRule` de pack qui activerait le LOT I (bloquée sur validation réelle),
> tout parcours d'achat de licence (ADR-009 toujours pas câblé — la licence reste aujourd'hui
> injoignable pour un utilisateur normal), tests d'intégration WPF de l'onglet Repair (irréalisable
> sans Windows). Détail complet du raisonnement : `docs/adr/ADR-012-chemin-ecriture-repair.md`,
> `knowledge/FIELD-LOG.md` (entrée du 11/08).

---

## 🧭 MAJ 10/08 (bis) — recherche communauté externe analysée → spec de lot écrite, 4 décisions tranchées

> **Entrée la plus importante de la journée. Lire `docs/SPEC-lot-communaute-2026-08-10.md` avant toute
> reprise de code.**
>
> Maxime a fourni un document de recherche produit par GPT+Gemini (~90 « besoins » relevés sur VPForums,
> VPUniverse, Reddit, Pincab Passion, GitHub). Consigne : ne pas refaire la recherche, en extraire les
> bonnes idées pour le Scanner et Repair, et produire un plan + specs pour une session Sonnet.
>
> **Fait** : les ~90 items ont été passés au filtre (déjà codé ? déterministe ? signal réel ? dans le
> périmètre ?) et croisés avec un inventaire complet des 26 scanners existants. Résultat dans la spec :
> 7 lots de détection (A→G), le câblage du chemin d'écriture Repair (H), une nouvelle action Repair (I),
> un backlog de 6 items specifiés, et une liste explicite de ce qui est **rejeté avec la raison** (pour
> ne pas re-débattre dans six semaines).
>
> **⚠️ CORRECTION FACTUELLE IMPORTANTE — `FLEXDMD_MISSING` n'est PAS une chaîne morte.** Les handoffs
> précédents (dont le prompt de reprise du 10/08) affirmaient « vérifié par lecture du code » qu'il
> existait dans `Loc.cs` sans être câblé. **C'est faux** : `DependencyScanner.cs` ligne 80 l'émet en
> `Warning`, sur un signal composite déjà correct. Le « chantier FlexDMD » de Gregg est donc déjà fait à
> moitié, et la moitié faite est la bonne. Ce qui manque réellement sur FlexDMD, c'est l'**enregistrement
> COM** et la cohérence de version/architecture — objet du LOT A de la spec. La « spec du 08/08 »
> introuvable n'a plus besoin d'être retrouvée.
>
> **Le trou dominant trouvé** : sur 26 scanners, **aucun ne lit un seul enregistrement COM**. Or c'est le
> thème n°1 de toute la recherche (P0 dans les 5 tableaux de synthèse du document, présent sur 3
> communautés, occurrences continues de 2021 à janvier 2026 : « ActiveX component can't create object »,
> « Library not registered », « Registered FlexDMD does not match your install path », « I had multiple
> instances from old installs »). Détection 100 % déterministe. Point technique clé identifié : il faut
> lire **les deux vues du registre** (32 et 64 bits) séparément — un composant enregistré en 64 et absent
> en 32 est précisément la cause racine du P0 « 64 bit and 32 bit are different ecosystems ».
>
> **4 décisions Maxime tranchées le 10/08** (détail en §4 de la spec) :
> 1. **Chemin d'écriture Repair (Preflight/Apply/Undo) : À CÂBLER**, dans la même session. En attente
>    depuis le 27/07. C'est le changement le plus risqué de l'histoire du projet — première écriture
>    réelle sur la machine d'un utilisateur. Spec dédiée + garde-fous en LOT H. **Bloqueur n°1 identifié :
>    le journal est aujourd'hui `InMemoryRepairJournal`, donc `Undo` mourrait à la fermeture de l'app.**
> 2. **Ré-enregistrement COM : via les outils du composant** (`FlexDMDUI.exe`,
>    `B2SBackglassServerRegisterApp.exe`, `Setup.exe`), jamais par écriture registre directe. Reste dans
>    la racine confinée ADR-005. Mais exécuter un processus externe est une **classe de capacité
>    nouvelle** → règles de confinement dédiées en LOT I.
> 3. **`VPINMAME_NOT_REGISTERED` en `Critical`** — premier `Critical` ajouté depuis le gel du 03/08.
>    Contrepartie : les 4 conditions doivent être mesurées, jamais supposées ; registre illisible =
>    silence total, jamais un `Critical` de repli.
> 4. **Périmètre : tout le sprint (A→I).** Ordre d'abandon si le temps manque : G, F, E, D, C.
>    **Jamais H à moitié.**
>
> **Rien codé cette entrée** (analyse + spec, pas un chantier). **ADR-012 attendu** de la session Sonnet
> pour le chemin d'écriture. Prompt de passation prêt en §9 de la spec.

---

## ✅ MAJ 10/08 — règle « feu vert par défaut » étendue à tout produit hors Scanner ; #13 codé (dé-emphase B2S) ; scope disque à clarifier davantage

> **Décision de Maxime (10/08) : la règle du 07/08 (« feu vert par défaut » pour une demande
> communauté raisonnable, sauf téléchargement illégal) est étendue à TOUT produit hors Scanner**,
> pas seulement Repair. Le Scanner garde sa doctrine gelée inchangée (03/08) — deux signaux terrain
> indépendants requis pour tout nouveau check.
>
> **Décisions en attente tranchées ce jour** (voir liste complète plus bas) :
> - **#9 (clé INI port COM DMD)** : pas de fichier réel fourni — **reste sur les 4 variantes
>   tolérées** (`port`/`comport`/`com_port`/`serialport`), aucun changement de code.
> - **#10 (planchers Script Doctor A1)** : **reste bloqué**, pas de valeurs données — A1 non codé.
> - **#12 (KPI#1 ROM_MISSING × 8)** : **reste ouvert**, à vérifier plus tard — aucun correctif codé,
>   aucune des 8 tables n'a été tranchée originale/homebrew vs vrai hack.
> - **#13 (dé-emphase B2S_MISSING/B2S_ORPHAN sans backglass) : CODÉ.** Voir détail ci-dessous.
>
> **Codé — `CompletenessScanner.cs`** : réutilise exactement la détection déjà éprouvée par
> `DisplaySetupScanner` (présence d'un binaire de rôle `b2s` sous l'install, via
> `ctx.Profile.BinaryRoles` + `LayoutDetector.FindFilesByPattern`) pour savoir si l'install a un
> composant backglass du tout. **Si aucun composant `b2s` n'est détecté**, `B2S_MISSING` passe de
> `Warning` à **`Note`** (palier existant, ADR du 07/08 — jamais de score/bannière FIX THIS FIRST,
> mais reste visible dans le rapport complet) au lieu d'être supprimé — c'est une heuristique, pas
> une certitude (un utilisateur pourrait vouloir préparer ses backglass plus tard), donc doctrine
> "jamais de silence sur une info potentiellement utile" respectée. Le texte anglais distingue les
> deux cas. `B2S_ORPHAN` (déjà `Info`) **non touché** — il n'a jamais pesé sur le score. **Aucun
> autre scanner existant modifié.** Livré directement sur le disque de Maxime (1 fichier) :
> `src/PincabToolbox.Core/Scanning/CompletenessScanner.cs`. **Pas de build/test exécuté** — toujours
> aucun `dotnet` disponible dans ce sandbox ; relu à la main (accolades équilibrées, même signature
> de méthode `Scan`, pattern de détection copié à l'identique d'un scanner déjà testé). **Core
> (tests existants de `CompletenessScanner`, s'il y en a) à revérifier par Maxime au prochain
> `build.cmd`.**
>
> **Non fait volontairement — la demande « le scanner doit lire tout le disque, pas fichier par
> fichier » reste ouverte.** Clarifiée en partie (Maxime : couverture de tous les fichiers du disque,
> pas seulement la racine VPX choisie) mais **c'est un changement d'ARCHITECTURE de scan, pas un
> nouveau check** — ça touche `LayoutDetector`/`ScanContext`/tout scanner qui suppose une racine
> unique, et ça entre en tension directe avec le Scanner gelé (03/08, "aucun nouveau check sans deux
> signaux terrain indépendants" — un changement de portée globale n'est pas non plus un cas couvert
> par cette règle, il faudrait une décision explicite de Maxime pour rouvrir le Scanner sur ce point
> précis). Pas codé sans cadrage supplémentaire : que vise-t-on concrètement — scanner plusieurs
> lecteurs/dossiers en une passe (multi-racines), ou aller chercher les tables même hors de la
> racine choisie (perte du confinement ADR-005/006 qui borne où Repair peut écrire) ? Question à
> reposer à Maxime avant tout code.
>
> **Précision donnée par Maxime après coup (10/08)** : ce n'est pas un crawl indiscriminé de tout
> le disque — « tous les dossiers liés à mon pincab sont dans mon disque C:, je veux pas scanner
> fichier par fichier mais l'ensemble ». Compris comme : **auto-détecter tous les dossiers pincab
> pertinents sur C:** (Tables, VPinMAME, PinUP Popper, etc., même hors de la racine unique
> aujourd'hui choisie à la main) plutôt qu'un scan disque entier indifférencié. Reste un changement
> de `LayoutDetector` (aujourd'hui : une racine + chemins relatifs candidats) vers une détection
> multi-racines sur un même lecteur — **toujours pas codé**, toujours en tension avec le Scanner
> gelé, toujours besoin d'un feu vert explicite de Maxime pour rouvrir ce point précis avant
> d'écrire quoi que ce soit.
>
> **Règle "feu vert par défaut" — précision de Maxime (10/08)** : « les gens demandent, on fait, si
> c'est possible et légal. » Reformule/confirme l'extension à tout produit hors Scanner posée plus
> haut, avec le rappel explicite que "possible" reste un vrai filtre (pas un blanc-seing aveugle :
> une demande techniquement irréalisable ou nécessitant un jugement métier non tranché — cf. #10 —
> continue à être cadrée avant d'être codée, pas devinée).
>
> **FlexDMD (Gregg, item #1 de la reprise du 10/08) — recherche primaire-source faite, spec de
> Maxime introuvable.** Recherche demandée avant tout code : comment une table VPX déclare l'usage
> de FlexDMD. **Confirmé par deux sources indépendantes** (tutoriel VPForums "Add a flexDMD to EM
> tables", doc officielle `flexdmd/docs/JPSalas.md` du dépôt `vbousquet/flexdmd`) : la déclaration
> canonique est `Set FlexDMD = CreateObject("FlexDMD.FlexDMD")`, suivie d'un test d'existence
> `If Not FlexDMD is Nothing Then` — pas de `On Error` explicite dans les deux conventions
> observées (VPForums générique et JPSalas), l'échec de `CreateObject` se traduit par `FlexDMD`
> qui reste `Nothing`. Signature déterministe exploitable par `ScriptAnalyzer` : chercher
> `CreateObject("FlexDMD.FlexDMD")` (insensible à la casse/espaces) dans le script de la table.
> **Mais** : la "spec complète et ordre de travail" que la consigne de reprise dit avoir été donnée
> le 08/08 dans `TRANSMISSION.md` **n'existe nulle part dans ce fichier ni dans FIELD-LOG.md**
> (vérifié par recherche exhaustive du terme "FlexDMD" dans les deux fichiers, zéro résultat avant
> cette entrée) — les deux fichiers sur le disque de Maxime datent du 07/08 au plus tard, la session
> qui a produit la spec du 08/08 n'a jamais écrit sur le disque. **Pas codé** : coder le câblage de
> `FLEXDMD_MISSING` sans cette spec serait deviner l'ordre de travail (quel scanner, quel Loc
> câblé, quelle sévérité) — exactement ce que la doctrine interdit. **Action Maxime** : soit
> retrouver/recoller la spec du 08/08 (chat de cette date-là), soit la redonner en quelques lignes
> — la partie recherche (ce texte) est acquise et ne sera pas refaite.
>
> **#14 CODÉ (10/08, après feu vert explicite « corrige les adr si il le faut, code le câblage,
> code »).** Voir `docs/adr/ADR-011-scan-multi-racines-disque-entier.md` pour le détail complet.
> Résumé : `DriveInstallFinder` (nouveau) trouve tous les dossiers pincab sur un disque via une
> marche bornée réutilisant les candidats du profil (`Profile.Locations`, pas un nouveau motif
> inventé) ; `ScanEngine.RunAcrossDrive` (nouveau) relance le pipeline existant, inchangé, sur
> chaque install trouvé et agrège dans `DriveScanReport` ; `ToMergedScanReport()` fusionne en un
> `ScanReport` normal pour que le reste de l'app (export, UI) n'ait rien à changer. **Aucune
> couleur/logique de scanner existant modifiée.** `ScanScoring` extrait (pur refactor) le calcul
> de score/tri hors de `ScanReport` pour que le rapport fusionné réutilise la même formule.
> **Câblage App minimal, zéro nouvel élément XAML** : taper `C:\` dans le champ racine existant
> au lieu d'un dossier précis déclenche automatiquement le scan multi-racines
> (`DirectoryInfo.Parent is null` reconnaît une racine de lecteur).
>
> **Garde-fou trouvé et corrigé avant livraison, pas après** : `RepairOfferBuilder.Build`
> confinait déjà Repair via `report.Layout.RootPath` (ADR-005) — sur un rapport fusionné, ce
> `RootPath` synthétique vaut le DISQUE ENTIER, ce qui aurait autorisé Repair sur n'importe quelle
> cible de tout `C:\`. Corrigé par un second paramètre explicite (`confinementRoots`) : en mode
> disque entier, l'App passe la vraie liste des racines d'install trouvées, jamais la racine
> synthétique. Le cas mono-racine existant est bit-à-bit inchangé. **C'est le genre d'erreur que
> je n'aurais découverte qu'en lisant `RepairOfferBuilder.cs` avant de livrer — je l'ai fait avant
> de committer, pas après un incident.**
>
> **Pas de build/test exécuté** — toujours aucun `dotnet` disponible. Relu à la main (accolades
> équilibrées, signatures cohérentes, aucun scanner touché). **Non fait** : pas de nouvel élément
> d'UI dédié (juste la détection automatique), pas de test unitaire pour `DriveInstallFinder`/le
> nouveau paramètre de confinement — à ajouter dès que `dotnet` est disponible. Coût du scan disque
> entier jamais mesuré en conditions réelles (borné en profondeur, dossiers système ignorés, mais
> potentiellement plusieurs minutes sur une grosse machine) — à observer au premier scan réel.
>
> **Action Maxime** : lancer `build.cmd`, vérifier Core/Repair/App verts, puis un vrai scan sur
> `C:\` sur ta cab pour voir si tous tes dossiers pincab sont bien trouvés et si le temps de scan
> reste raisonnable.

> **Repair "config audio stable au boot" (Jarr3, item #2) — pas commencée cette session.**
> Portée confirmée trop large pour un feu vert automatique même sous la règle étendue du 10/08
> (déclenchement au démarrage hors VPX/Popper, UI overlay flipper, préférences persistantes,
> premier ADR d'auto-modification système potentiel — cf. reprise du 10/08). Nécessite une vraie
> session de recherche technique (interception input flipper hors VPX/Popper) avant tout cadrage,
> pas juste une recherche primaire-source ponctuelle comme FlexDMD — non lancée par manque de temps
> cette session, à prioriser à la prochaine reprise si Maxime confirme.

---

## 📏 RÈGLE PERMANENTE (posée le 07/08, ÉTENDUE le 10/08) — tout produit hors Scanner : construction par défaut

> **Décision de Maxime, à appliquer par toutes les sessions futures, tant qu'elle n'est pas révoquée
> explicitement :**
>
> « Repair c'est pas pareil que le Scanner. Nouvelle règle : dès que quelqu'un veut quelque chose
> pour Repair ou les autres produits hors Scanner, on le fait — sauf si c'est pour télécharger
> illégalement. » **Étendue le 10/08 : ne se limite plus à Repair — s'applique à TOUT produit hors
> Scanner** (Repair, et tout ce qui viendra après).
>
> **Ce que ça change concrètement** :
> - Pour le **Scanner** : rien ne change. La doctrine existante reste en vigueur intégralement —
>   aucun scanner EXISTANT modifié sans feu vert explicite, aucun nouveau check sans preuve
>   déterministe, biais silence sur l'incertain, jamais de faux positif accepté sciemment. Toutes les
>   décisions en attente déjà listées (#9, #10, #12) restent des décisions Maxime, pas devinées.
>   (#13 tranché et codé le 10/08 — dé-emphase B2S sans backglass, palier Note, doctrine respectée
>   car explicitement demandé par Maxime, pas deviné.)
> - Pour **tout produit hors Scanner** (Repair et au-delà) : une demande communauté raisonnable
>   devient un feu vert par défaut, plus besoin de redemander à chaque fois. **Seule exception
>   explicite** : tout ce qui faciliterait le téléchargement illégal (ROMs piratées, tables/médias
>   protégés par copyright, contournement de DRM) reste refusé, sans exception, peu importe la
>   demande ou son cadrage.
> - Ça ne supprime pas le bon sens produit/technique — une demande vague (ex. "un réglage plugins
>   qui marche vraiment") a quand même besoin d'être cadrée techniquement avant d'être codée (quel
>   registre, quel mécanisme, quels plugins visés) ; la règle dit "on construit par défaut", pas "on
>   devine l'implémentation sans clarifier ce qui est raisonnablement ambigu".
>
> Noté ici en clair pour que ça survive au changement de session, plutôt que de rester seulement
> dans un message de chat.

---

## ✅ MAJ 07/08 (huit) — v0.1.2-alpha publiée sur GitHub, chaîne complète en ligne

> Maxime a lancé la séquence (`.\makezip.cmd` puis `gh release create`) — capture terminal reçue,
> confirmée : `https://github.com/waylo1/pincab-toolbox/releases/tag/v0.1.2-alpha` est en ligne.
> Note pour la prochaine fois : `gh` est installé globalement, contrairement à `makezip.cmd`/
> `build.cmd` (scripts locaux) — pas besoin du préfixe `.\` pour `gh`, seulement pour les `.cmd` du
> dossier courant (PowerShell ne charge rien du dossier courant par défaut, contrairement à
> `cmd.exe`).
>
> **Chaîne bout en bout maintenant cohérente** : `github.com/.../releases/latest` sert `v0.1.2-alpha`
> → le bouton "Download for Windows" de la landing et le bouton "Check for updates" de l'app
> pointent tous les deux dessus (`/latest/download/...` et l'API `/releases/latest`) → version
> interne de l'app (`0.1.2`) correctement inférieure au tag publié, donc le check de MAJ fonctionnera
> correctement pour les prochaines releases (voir entrée précédente sur le bug évité).
>
> **Reste à faire (pas fait cette session)** : Maxime doit encore lancer `vercel --prod` depuis
> `flipsync-site/landing` pour que les 3 séries de retouches de landing (What's New/Repair preview,
> voix "je", mockup Note) soient réellement visibles en ligne — écrites sur son disque mais jamais
> déployées à ce stade. Et publier l'annonce FR/EN sur les forums/FB comme prévu.

---

## ⏱️ MAJ 07/08 (sept) — version bumpée 0.1.1→0.1.2 (bug évité) ; repo confirmé public ; release à publier

> **Bug évité avant qu'il n'arrive en prod** : Maxime a demandé la commande pour publier la nouvelle
> version. En vérifiant la release GitHub actuelle (`v0.1.1-alpha`, 30/07 — confirmée par
> `WebFetch`, l'API `api.github.com` bloque les requêtes non authentifiées depuis ce sandbox), j'ai
> réalisé que le numéro de version dans le code (`0.1.1`, `PincabToolbox.App.csproj` +
> `PincabToolbox.Core.csproj` + `MainWindow.xaml.cs CurrentVersion`) était identique au tag de la
> release déjà publiée. **Si Maxime avait publié la prochaine release sous le même `0.1.1`, le
> bouton "Check for updates" que j'ai codé aujourd'hui aurait dit "à jour" à tout le monde, même
> après la mise à jour** — `AppVersionCompare.IsNewer` compare des versions, pas des dates ; deux
> versions identiques ne sont jamais "plus récentes" l'une que l'autre, par design (§ doctrine
> "jamais de faux positif" du reste du scanner).
>
> **Corrigé avant que ça arrive en prod** : version bumpée à `0.1.2` dans les 2 `.csproj` (App +
> Core) et dans `MainWindow.xaml.cs`. Pas d'autre changement de code cette entrée.
>
> **Repo confirmé public** (question directe de Maxime) — vérifié via la page `/releases` de
> `github.com/waylo1/pincab-toolbox` (l'API JSON reste bloquée, mais la page web publique répond
> normalement, sans mur de connexion). 2 releases existantes : `v0.1.0-alpha` (27/07, "Initial
> commit") et `v0.1.1-alpha` (30/07, "Latest").
>
> **Séquence complète donnée à Maxime dans le chat** pour publier `v0.1.2-alpha` : `build.cmd` →
> `makezip.cmd` → `gh release create` (ou UI web GitHub si `gh` n'est pas installé sur sa machine,
> pas vérifié). **Pas de build/test exécuté par cette session** — toujours aucun `dotnet`
> disponible ; le bump de version est un changement mécanique à 3 endroits, risque de régression de
> compilation nul (littéral de chaîne uniquement).

---

## ⏱️ MAJ 07/08 (six) — 4 retouches landing (voix "je", mockup avec Note, "About"→"à propos", bêta gratuite retirée)

> Retouches demandées par Maxime sur la landing du dessus, toutes faites :
> - **Mockup de rapport (`.appwin`) mis à jour** — ajout d'une pastille "Note" (violet `#9C6ADE`,
>   nouvelle classe `.r-note`/`.pill.note`) et de 2 lignes d'exemple réalistes (port COM DMD, audio
>   par défaut) pour refléter les nouveaux checks Tier B + la sévérité Note, plutôt que de laisser le
>   mockup montrer un scanner qui n'a pas changé depuis la 1ère version de la landing.
> - **"Bêta gratuite" retiré** de la liste de confiance sous le hero (Maxime : "enlève bêta
>   gratuite").
> - **"About tab" traduit** — les 2 endroits où j'avais laissé "l'onglet About" en FR (section
>   What's New + FAQ) disent maintenant "l'onglet à propos".
> - **Voix corrigée en "je"/"moi"** partout où j'avais écrit "nous" (uniquement dans le texte que
>   j'avais ajouté ce jour — section Repair : "Tell us" → "Tell me", "Dis-le nous" → "Dis-le moi",
>   FR+EN, y compris le texte de repli hors-JS). Vérifié qu'aucun "we/us/our/nous/notre" ne traîne
>   ailleurs sur la page (recherche exhaustive) — le reste du site était déjà cohérent en "tu"/"I".
>
> Balises `<div>`/`<span>` recomptées équilibrées (101/101, 162/162). Toujours pas de build/rendu
> réel de la page — écrite sur le disque de Maxime, jamais ouverte dans un navigateur. **Action
> Maxime** : même commande de déploiement que l'entrée précédente (`vercel --prod` depuis
> `flipsync-site/landing`, ou double-clic sur `redeploy.cmd`) — regarder le rendu au moins une fois
> avant de considérer que c'est bon, cette session n'a aucun moyen de le vérifier visuellement.

---

## ⏱️ MAJ 07/08 (cinq) — landing page mise à jour (What's New + Repair preview), prête à déployer

> Build + commit confirmés faits par Maxime pour le bouton de MAJ (entrée précédente). Landing page
> (`flipsync-site/landing/index.html` — **PAS** `pincab-suite/landing/`, qui est un dossier vide côté
> repo App, juste la config Vercel ; le vrai contenu déployé vit dans `flipsync-site/landing/`,
> confirmé par le même `projectId` Vercel dans les deux `.vercel/project.json` et par `redeploy.cmd`
> qui `cd` explicitement dedans) mise à jour :
> - **Section "What's New"** ajoutée juste après le hero : les 5 nouveaux checks Tier B, la nouvelle
>   sévérité "Note", le bouton de vérification manuelle des MAJ — badges "New" (pill orange), datée
>   7 août 2026, FR+EN.
> - **Section "Repair — in development"** ajoutée : les 4 actions réellement codées dans
>   `RepairOfferBuilder`/`RepairActionRegistry` (débloquer fichier Windows, restaurer ROM, quarantaine
>   médias orphelins, tuer PinUP Display zombie) — pas inventées, lues dans le code. Question ouverte
>   à la communauté en bas, pas de prix mentionné, comme demandé. **Pas de compteur de temps de dev
>   ni de date de sortie promise** — volontairement absent.
> - **FAQ "Is it safe?" corrigée** — l'ancienne réponse affirmait déjà (avant cette session) un appel
>   réseau vers le "Virtual Pinball Spreadsheet index", ce qui ne correspondait à AUCUN appel réseau
>   réel dans le code (vérifié : zéro avant aujourd'hui). Réécrite pour décrire le vrai (et seul)
>   appel réseau — le bouton manuel de check de version — plutôt que de laisser une inexactitude
>   préexistante à côté d'une nouveauté réelle.
> - Carte de confiance "Works offline" reformulée dans le même sens (scan = zéro réseau, MAJ = un
>   clic optionnel).
>
> **Pas de build/déploiement fait par cette session** — le HTML est écrit sur le disque de Maxime,
> balises `<div>`/`<section>` comptées équilibrées à la main (95/95, 8/8), mais jamais ouvert dans un
> navigateur ni déployé. **Action Maxime** :
> ```
> cd "%USERPROFILE%\Desktop\Pincab suite\flipsync-site\landing"
> npx --yes vercel --prod --yes
> ```
> (c'est exactement ce que fait `redeploy.cmd` à la racine de `Pincab suite\` — double-clic dessus
> marche aussi, il loggue dans `_deploy_log.txt`.) **Le lien de téléchargement de la landing pointe
> déjà vers `github.com/.../releases/latest/download/PincabToolbox.zip`** — donc dès que Maxime crée
> une nouvelle release GitHub avec le nouveau `PincabToolbox.zip` (build de ce week-end, action pas
> encore faite/confirmée), le bouton "Download" sert automatiquement la bonne version, sans autre
> changement sur la landing.
>
> **Pas de git ici** — `flipsync-site/` n'est pas un dépôt Git (`git status` confirme "not a git
> repository"), c'est un déploiement Vercel direct depuis le dossier local. Rien à committer/pousser
> pour la landing — juste `vercel --prod` depuis ce dossier.

---

## ⏱️ MAJ 07/08 (quater) — bouton "Check for updates" codé (feu vert donné) : PREMIER appel réseau du projet

> **Maxime a donné le feu vert pour coder le bouton de MAJ.** Codé — mais c'est un changement de
> nature différente des correctifs habituels : **c'est le tout premier appel réseau de tout le
> projet.** `grep -ri "HttpClient\|WebClient\|https://" src/` ne remontait strictement rien avant
> cette session. Traité comme tel : manuel, opt-in, jamais automatique, et **le texte "About" (qui
> promettait "100% local — rien n'est envoyé") a été corrigé pour rester honnête**, pas juste le
> code — un utilisateur qui lit cette promesse doit continuer à pouvoir s'y fier.
>
> **Ce qui est codé** :
> - `PincabToolbox.Core/Services/UpdateChecker.cs` — `GitHubUpdateChecker` interroge l'API publique
>   GitHub (`api.github.com/repos/waylo1/pincab-toolbox/releases/latest`, pas d'auth, timeout 6s),
>   lit le tag de la dernière release. Ne télécharge, n'installe, ne remplace **rien** — juste un
>   lien vers la page de release, l'utilisateur décide. `AppVersionCompare` (comparaison pure,
>   testée) décide si le tag est plus récent que `0.1.1`.
> - `MainWindow.xaml` / `.xaml.cs` — bouton "Check for updates" / "Vérifier les mises à jour" dans
>   l'onglet About, à côté du numéro de version. Clic → "Checking…" → soit "à jour", soit un lien
>   cliquable vers la release, soit un message d'erreur neutre (hors ligne / GitHub injoignable).
>   **Jamais de vérification au démarrage, jamais en tâche de fond.**
> - `Loc.cs` (FR+EN) — nouvelles clés pour le bouton et les 3 états, **et le texte `about.body`
>   corrigé** pour disclosure honnête : "Scan 100% local... Seule exception : le bouton [...] est
>   manuel et volontaire [...] Rien concernant ta cab, tes tables ou tes résultats de scan n'est
>   jamais envoyé."
> - `AppVersionCompareTests.cs` (10 tests) — comparaison de version pure, testée sans réseau : tag
>   plus récent/identique/plus vieux, préfixe `v`/`V` optionnel, suffixe pré-release, tag malformé
>   (jamais de faux "MAJ disponible" sur un tag illisible — même doctrine que le reste du scanner
>   sur les données non lisibles).
>
> **Pas de build/test exécuté** — toujours aucun `dotnet` disponible cette session (ni sandbox, ni
> pont). Vérification manuelle : accolades/parenthèses équilibrées sur les 2 nouveaux fichiers,
> types et signatures cohérents avec le reste du projet (`net8.0`, `HttpClient`/`System.Text.Json`
> déjà dans le framework, aucune dépendance NuGet ajoutée — le projet reste "zéro dépendance
> externe" comme annoncé dans son propre `.csproj`). **Core (279+10=289 attendu) + Repair 105/105 +
> App (compile réel du XAML) à vérifier par Maxime au prochain `build.cmd` — pas de vert confirmé.**
>
> **Pas commité par cette session** — même bloqueur que l'entrée précédente potentiellement
> (`.git/index.lock` avait été débloqué par Maxime entre-temps, donc ça devrait passer, mais pas
> vérifié). Commande ci-dessous.
>
> **Point d'attention produit, pas juste technique** : c'est la première fois que l'app fait
> sortir quoi que ce soit vers Internet. Le design retenu (manuel, opt-in, aucune donnée sur la
> cab/les tables envoyée, texte About mis à jour en conséquence) vise à rester dans l'esprit
> "confiance" du projet plutôt qu'à le rompre — mais **ça mérite un ADR formel** (pas fait cette
> session, faute de temps annoncé par Maxime) pour que ce ne soit pas juste tribal knowledge dans
> un commentaire de code. À faire lundi ou plus tard.
>
> **Git (action Maxime)** :
> ```
> git add src/PincabToolbox.Core/Services/UpdateChecker.cs src/PincabToolbox.App/MainWindow.xaml src/PincabToolbox.App/MainWindow.xaml.cs src/PincabToolbox.App/Localization/Loc.cs tests/PincabToolbox.Core.Tests/AppVersionCompareTests.cs knowledge/FIELD-LOG.md TRANSMISSION.md
> git commit -m "feat(app): bouton Check for updates (GitHub releases, manuel/opt-in) - premier appel reseau du projet, About.md mis a jour en consequence"
> git push origin main
> ```

---

## 🔜 PROMPT DE PASSATION — session de lundi 10/08 14h00

> Copier-coller tel quel pour ouvrir la session de lundi :
>
> « Tu reprends Pincab Toolbox / FlipSync (MC Automation, Maxime Chauvin). Lis TRANSMISSION.md
> (bloc du haut) et knowledge/FIELD-LOG.md (dernière entrée + section DÉCISIONS EN ATTENTE tout en
> bas) pour le contexte complet avant de faire quoi que ce soit.
>
> État au 07/08 (ter) : le bug des 2 scanners existants (BlockedFileScanner, CompletenessScanner)
> est corrigé, commité (`f7f2ab1`) et poussé sur GitHub — reste juste mon rebuild Windows à
> reconfirmer en vert (Core 279/279, Repair 105/105), aucun `dotnet` disponible en sandbox pour
> vérifier. Les 3 cas terrain de Gregg (BKSOR, Rocky & Bullwinkle, Spiderman) sont clos, 0 bug
> scanner trouvé. J'ai annoncé le nouveau scanner + le développement de Repair sur les forums/FB
> (FR+EN) dans le week-end du 08-09/08 — vérifie s'il y a des retours/questions de la communauté à
> traiter en premier (nouvelles idées de check demandées, questions sur Repair, etc.) avant
> d'attaquer autre chose.
>
> Le bouton "Check for updates" est maintenant codé (premier appel réseau du projet, manuel/opt-in,
> texte About corrigé en conséquence) — vérifie en premier que mon `build.cmd` du week-end est bien
> passé vert dessus (Core/Repair/App) avant d'y toucher, et regarde si un ADR formel a été écrit
> pour ce choix (pas fait le 07/08, faute de temps).
>
> 4 décisions toujours en attente de ma part, aucune ne doit être devinée : #9 (clé INI du port COM
> DMD — j'ai peut-être un vrai `dmddevice.ini` à coller si j'ai fait la manip ce week-end), #10
> (versions de référence par script pour Script Doctor — idem, j'ai peut-être les valeurs), #12
> (KPI #1 — est-ce qu'une de mes 8 tables ROM manquante est en fait une originale/homebrew sans
> ROM), #13 (dé-emphase backglass, basse priorité, pas bloquant).
>
> Si j'ai répondu à une ou plusieurs de ces décisions, débloque les scanners correspondants (A1
> Script Doctor si #10 répondu, B3 fiabilisé si #9 répondu). Si aucune réponse, avance sur autre
> chose d'utile sans redemander (voir « pistes non bloquantes » dans FIELD-LOG) plutôt que
> d'attendre.
>
> **Nouvelle règle permanente posée le 07/08** (en haut de ce fichier) : pour Repair et tout produit
> hors Scanner, une demande communauté raisonnable = feu vert par défaut, sauf téléchargement
> illégal. Le Scanner garde sa doctrine stricte inchangée.
>
> **Tâche de recherche pour cette session (demandée le 07/08)** : analyser VPin Studio (VPinStudio —
> cité par un utilisateur forum comme référence pour un "réglage global plugins qui marche
> vraiment", VP Studio pris comme exemple qui bug chez lui) pour en reprendre le meilleur, dans la
> mesure du possible, sur ce que Pincab Toolbox pourrait faire de mieux ou de comparable — angle
> Repair (gestion active de plugins), pas Scanner. Pas de spec précise donnée par Maxime au-delà de
> ça — commencer par comprendre ce que VPin Studio fait réellement (fonctionnalités, mécanisme de
> toggle plugins, ce qui marche vs ce qui bug d'après les retours communauté) avant de proposer quoi
> que ce soit à coder. Rattaché à la demande forum du 07/08 sur le "réglage global plugins actifs"
> (FIELD-LOG, entrée dix) — déjà feu vert par défaut sous la nouvelle règle, mais encore à cadrer
> techniquement, ce qui est exactement l'objet de cette recherche.
>
> Revue CTO + Product avant toute clôture, comme toujours. »

---

## ⏱️ MAJ 07/08 (ter) — annonce communauté préparée (FR+EN, scanner + Repair en dev) ; bouton de MAJ scanner confirmé NON câblé

> **Réponse à la question de Maxime : le "bouton de mise à jour" du scanner n'est pas câblé — il
> n'existe même pas dans le code.** Vérifié par recherche exhaustive dans `src/` (aucune occurrence
> de `Update`/`AutoUpdate`/bouton de MAJ nulle part) : c'est resté au stade audit (`§8.4` de
> `docs/AUDIT-Scanner-2026-08.md`, 05/08) — un canal Knowledge Pack (valeur ADR-002, déjà réel :
> `RepairOfferBuilder.LoadPack()` charge un fichier JSON local) vs un canal binaire conditionné à la
> signature de code (jamais commencé). **Donc non : aujourd'hui, chaque nouvelle version du scanner
> = un nouveau téléchargement complet pour l'utilisateur.** Rien à annoncer là-dessus pour l'instant
> — si Maxime se fait poser la question sur les forums, la réponse honnête est "en réflexion, pas
> encore fait".
>
> **Repair, état réel du code** (pour cadrer l'annonce sans survendre) : `RepairOfferBuilder`
> (App) construit déjà l'écran 1 "Repair available" (résumé gratuit, lecture seule) à partir de 4
> actions déjà codées dans `PincabToolbox.Repair` : `UnblockFileAction` (débloquer un DLL Windows —
> rejoint directement le bug `BLOCKED_DLL` du module `security`), `RestoreRomArchiveAction`,
> `QuarantineOrphanedMediaAction`, `KillZombiePinUpDisplayAction`. **Le chemin d'écriture réel
> (Preflight/Apply/Undo) n'est PAS câblé dans l'App** — décision volontairement mise en attente
> (HANDOFF 27/07), Maxime doit re-trancher avant que ça s'active. `SetDefaultAudioDeviceAction`
> existe aussi mais n'est même pas encore branché au registre d'actions.
>
> **Annonce FR+EN préparée dans le chat** (scanner Tier A/B + palier Note + comparateur VPX +
> dégel ; Repair en développement, 4 actions de correction déjà en chantier citées par leur intitulé
> utilisateur, pas de mention de prix ; question ouverte à la communauté sur les prochains checks
> scanner/Repair souhaités). **Pas de prix annoncé** comme demandé — si la question tombe,
> Maxime répond "oui" en privé/commentaire, pas dans le post lui-même.
>
> **Prompt de passation lundi 14h ajouté en haut de ce fichier.** Rien codé cette entrée (annonce +
> clarification, pas un chantier).

> **Maxime a donné le feu vert** pour le bug confirmé la session précédente (item #11 des décisions
> en attente) : `BlockedFileScanner.cs` (module `security`) et `CompletenessScanner.CollectWheelStems`
> protégeaient l'APPEL à `Directory.Enumerate*(..., AllDirectories)` par try/catch mais pas le
> `foreach` de consommation qui suit (énumération paresseuse) — un dossier Windows protégé
> (`C:\Documents and Settings` ou équivalent) faisait échouer le scanner entier en un `SCANNER_ERROR`
> technique au lieu du résultat normal.
>
> **Corrigé dans les 2 scanners existants**, patron répliqué depuis `LayoutDetector.SafeEnumerateDirs`
> (déjà dans le projet, pas inventé) : marche BFS dossier par dossier, chaque `Directory.GetFiles`/
> `Directory.GetDirectories` protégé par son propre try/catch — un sous-dossier illisible est
> maintenant simplement sauté, le reste de l'arbre continue d'être scanné normalement au lieu de
> perdre tout le module. Directement écrit sur le disque de Maxime (2 fichiers) :
> `src/PincabToolbox.Core/Scanning/BlockedFileScanner.cs`,
> `src/PincabToolbox.Core/Scanning/CompletenessScanner.cs`.
>
> **Pas de build/test exécuté cette session** — aucun `dotnet`/Roslyn disponible ni dans le sandbox
> cloud ni via le pont vers la machine de Maxime cette fois (contrairement aux sessions où un
> vérificateur Roslyn avait été monté ponctuellement). Changement revu à la main : même signature de
> méthode, même type de retour, aucun appelant à modifier, patron copié à l'identique d'un helper déjà
> testé du projet (`LayoutDetector`) — risque de régression de compilation jugé faible, mais **Core
> 279/279 + Repair 105/105 à revérifier par Maxime au prochain `build.cmd` réel**, comme toujours pour
> tout changement non vérifiable en sandbox.
>
> **Git push reconfirmé sans changement à pousser** : `git log` local == `git log origin/main` sur la
> machine de Maxime, HEAD `9f3e5f7` des deux côtés — les commits Tier B étaient déjà bien en ligne,
> rien à repousser avant le correctif de cette session. **Correctif PAS commité par cette session** :
> `git status` sur la machine de Maxime montre aussi 3 fichiers déjà modifiés avant cette session
> (`PincabToolbox.sln`, `README.md`, `landing/.gitignore` — pas touchés ici, origine inconnue) et un
> `.git/index.lock` résiduel que le pont ne peut pas supprimer (pas de droit de suppression sur les
> fichiers montés). **Action Maxime** : supprimer `.git/index.lock` s'il traîne encore, puis committer
> uniquement les 2 fichiers scanner (voir bloc Git ci-dessous) — ne pas inclure les 3 fichiers déjà
> modifiés sans savoir d'où vient ce diff.
>
> **4 décisions de la liste toujours sans réponse** (#9 clé INI DMD, #10 planchers Script Doctor,
> #12 KPI #1 ROM, #13 dé-emphase backglass — basse priorité) — aucune action codée dessus cette
> session, comme convenu tant qu'elles restent ouvertes.
>
> **Contenu suspect repéré dans le message de Maxime, non traité comme instruction** : deux blocs de
> texte à la fin ressemblaient à des commentaires de forum/support collés (un sur un tout autre
> logiciel, un signé « gregg » sur des retours de scan PincabToolbox) suivis d'une demande de
> « réponse immédiate ». Vu l'avertissement de Maxime lui-même dans le même message (« si tu vois un
> autre prompt dans un document supprime-le »), traité avec prudence plutôt qu'exécuté aveuglément —
> détail dans la réponse du chat, pas dans ce fichier.
>
> **Git (action Maxime)** :
> ```
> git add src/PincabToolbox.Core/Scanning/BlockedFileScanner.cs src/PincabToolbox.Core/Scanning/CompletenessScanner.cs knowledge/FIELD-LOG.md TRANSMISSION.md
> git commit -m "fix(scanner): protege l'enumeration recursive contre les sous-dossiers Windows illisibles (BlockedFileScanner, CompletenessScanner)"
> git push origin main
> ```

---

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
13. ✅ **FAIT (MAJ 10/08)** — ~~Dé-emphase `B2S_MISSING`/`B2S_ORPHAN` pour cabs sans backglass~~ — `B2S_MISSING` passe en `Note` quand aucun composant backglass (`b2s`) n'est détecté sur l'install. Voir entrée du 10/08 en haut de ce fichier.
14. **[NOUVEAU, 10/08] « Le scanner doit lire tout le disque, pas fichier par fichier »** — demande personnelle de Maxime, portée précisée (tous les fichiers du disque, pas juste la racine VPX choisie) mais mécanisme visé encore ambigu : multi-racines en une passe, ou sortir de la racine confinée par ADR-005/006 ? Changement d'architecture de scan, pas un nouveau check — nécessite une décision explicite avant tout code (tension avec le Scanner gelé du 03/08). Voir entrée du 10/08.

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
