# Refonte UI Pincab Toolbox — plan d'exécution (ANCRÉ SUR LE CODE)

**Statut** : Plan figé, prêt pour exécution Sonnet · **Écrit le** : 17/08/2026 (session Opus, plan seul)
**Portée** : `src/PincabToolbox.App` (WPF/C#, `net8.0-windows`) uniquement. La stack reste WPF/C# —
migration Tauri/React écartée, hors sujet (brouillon §2).
**Source** : `knowledge/UX-REDESIGN-PRE-SYNTHESIS-DRAFT.md` (arbitrage des avis GPT/Gemini), ré-ancré
sur le dépôt réel (`git clone` waylo1/pincab-toolbox, branche main). Chaque nom de fichier, de
ressource, de méthode et chaque numéro de ligne ci-dessous vient d'une lecture réelle du code au
17/08. Là où le brouillon supposait, ce plan corrige.

Ce plan ne code rien. Il découpe la refonte en 7 lots (un commit par lot) exécutables par une
session Sonnet sans qu'elle ait à trancher une seule décision d'architecture.

> **Note marque (17/08).** Nouveau logo « PIN CAB TOOL BOX » : identité **noir + vert olive-lime +
> argent**. L'orange cesse d'être une couleur de marque. L'impact est concentré dans le **lot 1**,
> reformulé en conséquence (bascule orange→vert des 5 sites d'accent de marque). Les couleurs de
> **sévérité** (Warning orange, Ok émeraude, Note violet, etc.) ne sont pas de la marque et ne
> changent pas — voir §2.9.

---

## 0. Correction majeure au brouillon : la maquette du 11/08 est DÉJÀ portée

Le brouillon a été écrit sans accès au code et suppose une UI d'avant-maquette. Le code réel montre
le contraire : la maquette `docs/maquette-scanner-2026-08-11.html` a **déjà été portée dans l'App**
le 13/08 (`FIELD-LOG.md` entrée « ce qui manquait a été porté », lignes 726-752). Sont déjà en place
dans `MainWindow.xaml` :

- le bandeau « hero » sur la scène d'arcade avec **jauge de score de santé** (`ScoreChip`, XAML 331),
  accroche (`HeroHeadline`), sous-titre (`ScoreStatus`) et pastilles de sévérité cliquables ;
- les **5 onglets internes** du Scanner (`ScannerTabs`, XAML 431) : Causes racines, Tous les
  résultats, Composants, Tables, Système ;
- les **cartes de causes racines** (`CauseCardTpl`), la **colonne de droite** (critiques / santé
  composants / remarques), la **carte réparation honnête** et le **tableau des tables** ;
- la **ligne méta** (mode, horodatage, durée, contrôles N/N, tables).

Conséquence directe sur le découpage : le **lot 2 du brouillon (« score de santé en hero ») est en
grande partie déjà fait**, et plusieurs autres lots sont partiels, pas vierges. Chaque lot ci-dessous
ouvre donc par un « État actuel réel » qui dit ce qui existe avant de dire quoi faire. Ne pas
reconstruire ce qui est déjà là.

---

## 1. Réponses aux points de vérification (section 4 du brouillon)

### 1.1 Format du score de santé

Le score **existe déjà** comme nombre **et** grade, et est **déjà l'élément hero**.

- `ScanReport.Score` (`src/PincabToolbox.Core/Models/ScanReport.cs:35`) = entier **0-100**, calculé par
  `ScanScoring.ComputeScore` (`ScanScoring.cs:34`) : base 100, −15 par *code* critique distinct (les
  répétitions du même code décroissent en log, pas en linéaire, `ScanScoring.cs:44-47`), pénalité
  d'avertissements plafonnée à −30 (`WarningPenaltyCap`). `ScanReport.Grade` (`ScanReport.cs:38`) =
  lettre **A+/A/B/C/F** via `ScanScoring.GradeFor` (`ScanScoring.cs:56`).
- Rendu dans `RefreshList()` (`MainWindow.xaml.cs:814`) : `ScoreValue.Text` = le nombre (826),
  `ScoreGrade.Text` = la lettre (827), `ScoreStatus.Text` = une phrase qualitative
  (`score.a/b/c/f`, 828), l'arc `ScoreArc` = jauge circulaire (constante `fullTurn = 35.34`, 838-840),
  couleur par seuil (`scoreBrush`, 829 : ≥90 vert `BrushOk`, ≥70 orange `BrushWarning`, sinon rouge
  `BrushCritical`).

**Le score n'est donc ni « un état qualitatif à créer » ni « un pourcentage à convertir » : c'est déjà
les deux.** Le point ADR-010 du brouillon vise autre chose : ce qui est interdit, ce sont les
**pourcentages de *confiance*** (voir §1.5 et §2), pas le score de santé 0-100. Le score de santé est
conforme à ADR-010 parce que `ComputeScore` ne compte que Critical et Warning — Note/Info/Ok ne le
bougent jamais (`Finding.cs:10-17`, invariant `Test_Note_NeverMovesScore`).

### 1.2 Ce que `ListFindings` sait déjà faire

`ListFindings` (`MainWindow.xaml:580`, onglet `StabResults`) fait **déjà** tri, recherche, filtres,
virtualisation, hover et sélection. À NE PAS redévelopper :

- **Tri** : `Header_Click` (`MainWindow.xaml.cs:1523`) → clés `sev`/`cat`/`subj`/`msg`, bascule
  asc/desc, ordre par sévérité par défaut (application du tri : `RefreshList` 967-980).
- **Recherche** : `TxtSearch` (`MainWindow.xaml:281`) → `TxtSearch_TextChanged` (1517) → filtre
  contains insensible à la casse sur Sujet / Message / Catégorie (958-965).
- **Filtres de sévérité** : les 5 pastilles `PillCritical/Warning/Note/Info/Ok` (XAML 362-391),
  gestionnaires 736-767, champs `_showCritical=true … _showInfo=true, _showOk=false` (202).
- **Virtualisation** : `IsVirtualizing` + `Recycling` (585). **Hover** `#1FFFFFFF` (603),
  **sélection** `#4DFF9F1C` (606).
- **Colonnes** (GridView, redimensionnables au glisser) : `ColSeverity` 120, `ColCategory` 110,
  `ColSubject` 320, `ColMessage` 410, `ColAction` 180.
- Regroupement des findings répétitifs via `Rolled()` (895).

Il n'y a **aucune colonne numérique** dans `ListFindings` : le point « alignement des chiffres à
droite » des avis n'y a pas d'objet (les seuls chiffres alignés à droite sont les numéros de ligne du
Diff, `MainWindow.xaml:847/863`). Le lot 5 est donc de la densité/hover, pas du tri/recherche.

### 1.3 Structure réelle des onglets & production des `UPDATE_AVAILABLE`

- **4 onglets principaux** (`MainTabs`) : `TabScanner`, `TabDiff`, `TabRepair`, `TabAbout`
  (en-têtes `Tab*Header`, libellés via `ApplyTexts` 413-416).
- **5 onglets internes** du Scanner (`ScannerTabs`, XAML 431) : `StabCauses`, `StabResults`,
  `StabComponents`, `StabTables`, `StabSystem` (en-têtes `Stab*Header`, `ApplyTexts` 451-455,
  compteurs `RefreshInnerTabHeaders` 1459).
- Les findings de mise à jour sont produits par `UpdateWatcherScanner`
  (`src/PincabToolbox.Core/Scanning/UpdateWatcherScanner.cs`), `Id`/`Category` = `"updates"`,
  `Name` = `"Update Watcher (beta)"`. Trois codes, **tous en `Severity.Info`** : `UPDATE_AVAILABLE`
  (un par table à jour disponible, `Subject`+`FilePath`+lien VPS, jamais de téléchargement),
  `VPS_MATCH_SUMMARY` (« matched X/Y … beta »), `VPS_UNAVAILABLE` (hors-ligne).
- Comme `_showInfo = true` par défaut, ces findings **sortent aujourd'hui mélangés aux autres Info
  dans `ListFindings`** — exactement le bruit décrit par Joey Mahon (`FIELD-LOG.md:180-197`). C'est ce
  qui tranche le véhicule du lot 3 (voir lot 3 : 6ᵉ onglet interne, pas 5ᵉ onglet principal).

### 1.4 Noms exacts des ressources de couleur de sévérité — et un piège

Il existe **deux sources de vérité** pour les couleurs de sévérité, et elles ont **déjà divergé** sur
le rouge. Le lot 4 (badges réutilisant l'existant) doit les réconcilier, surtout pas en inventer.

Ressources XAML (`App.xaml`, utilisées par les pastilles et les templates) :

| Clé StaticResource | Couleur | Rôle |
|---|---|---|
| `Critical` | `#FFE5484D` | sévérité critique |
| `Warning` | `#FFF5A524` | avertissement |
| `NoteSev` | `#FFB58DF5` | note heuristique (violet, distinct d'Info) |
| `InfoSev` | `#FF3E9CF3` | info neutre |
| `OkSev` | `#FF46C06E` | ok |
| `Accent` | `#FFFF9F1C` → **vert de marque** (lot 1) | accent de marque : CTAs, score, progression — bascule orange→vert, rebranding 17/08 |
| `AccentDark` | `#FFCC7A00` → **olive foncé** (lot 1) | bord / état pressé des boutons d'accent |

Brosses miroir en code-behind (`MainWindow.xaml.cs:218-231`, `SevBrushOf` 251) :
`BrushWarning` `#F5A524`, `BrushNote` `#B58DF5`, `BrushInfo` `#3E9CF3`, `BrushOk` `#46C06E` — **et
`BrushCritical` = `#FF6B6E`, qui NE correspond PAS à `Critical` `#FFE5484D` d'App.xaml.** Le commentaire
222 affirme « kept in sync manually, same pattern as the other 4 pairs » : c'est vrai pour les 4
autres, faux pour le rouge. Les teintes de fond de ligne (`RowCritical` etc., 227-231) utilisent, elles,
le `E5484D` d'App.xaml. Ne pas « corriger » à l'aveugle : voir lot 4 pour la décision.

### 1.5 Chemins de `PathScrubber.Scrub`, étapes non-automatisables, absence d'indicateur réseau

- **`PathScrubber.Scrub`** n'est appelé côté App que via l'assistant `Public(report)`
  (`MainWindow.xaml.cs:1990`) : c'est la **dernière porte avant qu'un rapport quitte la machine**.
  Passent par elle : les 6 formats d'export (`BtnExport_Click` 1937/1941/1947/1952/1956/1960), la
  copie forum (`BtnCopyForum_Click` 1975) et la copie d'une ligne (`DoRowAction` 803). Ailleurs :
  `src/PincabToolbox.Repair/Engine/Journal.cs:93`. (ADR-003.)
- **Étapes non-automatisables du Repair** : jamais masquées (ADR-006 §2). Dans l'App elles vivent dans
  `RepairNotAutomatableLine` (carte réparation de `StabCauses`, `MainWindow.xaml:471`) et dans les
  lignes de l'onglet Repair où `CanApply=false` **désactive** la case au lieu de cacher la ligne
  (`MainWindow.xaml:927`, commentaire 924-926).
- **Absence d'indicateur réseau** : exclusion **délibérée** (`FIELD-LOG.md:742`, renvoi ADR-002 ;
  « voyants réseau » listés parmi les éléments volontairement écartés). Le **seul** appel réseau de
  l'app est `BtnCheckUpdate_Click` (`MainWindow.xaml.cs:514`), manuel, jamais automatique
  (commentaire 508-513). L'Update Watcher lit la base VPS publique mais ne doit pas donner lieu à un
  voyant : ne jamais clamer « 100 % offline » non plus (`FIELD-LOG.md:2190`).

---

## 2. Garde-fous produits — invariants qu'aucun lot ne doit casser

Chaque lot doit se relire contre cette liste avant commit :

1. **Aucune donnée inventée.** Une case sans mesure affiche `« — »`, jamais une valeur plausible
   déduite du silence d'un scanner (ADR-010 ; commentaires `MainWindow.xaml:86, 480` et
   `MainWindow.xaml.cs:988`).
2. **`Public()` reste la seule sortie.** Aucun nouvel export, copie presse-papiers ou écriture de
   rapport ne doit contourner `PathScrubber.Scrub` (ADR-003, §1.5).
3. **Ce que Repair ne sait pas faire reste visible.** Ne jamais masquer, tronquer ni réduire à un
   scroll caché `RepairNotAutomatableLine` ni les lignes `CanApply=false` (ADR-006 §2). Cible de test
   du lot 7.
4. **Pas de voyant réseau**, pas de spinner « connexion… », pas de pastille en/hors-ligne (ADR-002).
5. **Pas de pourcentage de confiance.** La confiance des cartes de cause reste **en mots** (`ConfText`,
   `CauseCardRow.ConfText`, construit 1072-1083) — jamais « 85 % sûr » (ADR-010 ; `FIELD-LOG.md:742`).
6. **`Note` ne bouge jamais le score** et ne déclenche jamais « FIX THIS FIRST » (ADR-010). Ne pas
   introduire de style qui repeindrait une Note comme un Warning.
7. **Plafonds de virtualisation.** `ListFindings` et `TablesListFull` sont virtualisés ; les listes
   `ItemsControl` non virtualisées sont plafonnées volontairement (résumé tables 12 lignes
   `MainWindow.xaml:504-510`, critiques 8 lignes 527-532) — une collection réelle peut faire 2000
   tables. Ne pas remplacer un contrôle virtualisé par un `ItemsControl` non plafonné.
8. **Piège de nommage.** `CauseCardRow.AccentBrush` (`MainWindow.xaml.cs:51`, lié XAML 149/155) porte
   la **couleur de sévérité** (`SevBrushOf`), pas l'orange `Accent`. Ne pas le confondre avec l'accent
   lors du lot 1.
9. **Marque ≠ sévérité (rebranding 17/08).** Seul l'orange de **marque** (`Accent`/`AccentDark` + les
   5 sites du lot 1) bascule au vert du logo. L'orange de **sévérité** `Warning` (`#F5A524`) et le vert
   **émeraude** de la sévérité `Ok` (`#46C06E`) ne changent pas : ce sont des signaux, pas la marque.
   Ne jamais recolorer une couleur de sévérité au nom du rebranding.

---

## 3. Les 7 lots

Ordre inchangé par rapport au brouillon (§3) : fondations → hiérarchie → onglet updates → composants →
tableaux → polish → panneau latéral. Chaque lot rend le suivant plus facile à juger.

Rappel « terminé » commun à tous les lots : **l'App ne compile pas hors Windows** (WPF `net8.0-windows`,
NU1100, fait documenté — `ADR-012` §Coût, `FIELD-LOG.md:746`). La vérification se fait donc sur la
machine de Maxime via `build.cmd` (qui lance Core + Repair tests, puis publie), **plus** un contrôle
visuel en **Mode démo** (bouton `BtnDemo`). Aucun lot ne doit faire échouer `build.cmd` ni rougir les
suites `PincabToolbox.Core.Tests` / `PincabToolbox.Repair.Tests`.

---

### Lot 1 — Fondations : passage à la palette du logo (orange → vert) + jetons d'espacement 8px

**Contexte marque (17/08).** Nouveau logo « PIN CAB TOOL BOX » : identité **noir + vert olive-lime +
argent**. L'orange n'est plus une couleur de marque. Ce lot porte donc deux choses : le basculement de
l'accent de marque orange→vert et les jetons d'espacement. Effet de bord favorable : sortir la marque
de l'orange **libère l'orange pour la seule sévérité `Warning`** — soit exactement l'objectif « l'orange
redevient un signal » des avis, obtenu gratuitement par le rebranding. En contrepartie, un point de
vigilance neuf apparaît (vert de marque vs vert de santé, voir plus bas).

**État actuel réel.** Aucune ressource d'espacement n'existe (zéro `Thickness`/`Space`/`Gap` en
ressource ; tous les `Margin`/`Padding` sont des littéraux inline, ex. `Padding="14,7"`). L'orange de
**marque** apparaît à **5 endroits vérifiés**, distincts de l'orange de **sévérité** `Warning`
`#F5A524` (qui NE bouge pas) :

| Fichier:ligne | Valeur actuelle | Rôle |
|---|---|---|
| `App.xaml:13` | `Accent` `#FFFF9F1C` | accent de marque (CTAs, score, progression, bordures) |
| `App.xaml:14` | `AccentDark` `#FFCC7A00` | bord / état pressé des boutons d'accent |
| `MainWindow.xaml:606` | `#4DFF9F1C` | surbrillance de la ligne sélectionnée (`ListFindings`) |
| `MainWindow.xaml:686` | `#22FF9F1C` + `#55FF9F1C` | fond + bord de `DetailRepairTag` |
| `MainWindow.xaml.cs:2128` | `#FF9F1C` | accent du titre dans le rapport HTML exporté |

**Fichiers à toucher.** `App.xaml` (valeurs `Accent`/`AccentDark` + ajout des jetons d'espacement) ;
`MainWindow.xaml` (teintes codées en dur 606 et 686) ; `MainWindow.xaml.cs` (accent du rapport HTML,
2128) ; `Assets/logo.png` + `Assets/logo-full.png` (nouveaux visuels, fournis par Maxime).

**Ce qu'il faut faire.**
1. **Basculer les 5 sites d'orange de marque vers le vert de marque du logo.** Le logo EST la source de
   marque (pas de charte séparée). Valeurs lues sur le logo, à confirmer à la pipette sur le nouvel
   asset `Assets/logo-full.png` une fois ajouté : accent olive-lime **`#7CB342`**, `AccentDark` olive
   foncé **`#557E27`**. Pour 606/686, recomposer les teintes sur le nouveau vert en gardant les mêmes
   canaux alpha (`4D` / `22` / `55`). Garder le texte des boutons d'accent en quasi-noir (`#FF1A1206`,
   `App.xaml:60`) — contraste texte/bouton élevé sur ce vert clair (> 7:1, AA large).
2. Ajouter dans `App.xaml <Application.Resources>` une échelle d'espacement 8px en ressources
   `Thickness` (`SpaceXs=4`, `SpaceSm=8`, `SpaceMd=16`, `SpaceLg=24`), nommées par rôle, appliquées au
   fil des zones déjà retouchées par les autres lots pour limiter la surface.
3. *(Optionnel, pour coller au logo)* rapprocher le fond sombre bleuté (`Bg` `#FF15151B`, `Panel`
   `#FF1E1E26`) du noir plus neutre du logo. À faire dans un commit séparé et avec prudence : c'est le
   fond de toute l'app, forte surface visuelle pour un gain faible — ne pas le mêler au commit d'accent.

**Remplacer aussi les fichiers de logo.** Le rebranding n'est pas que la couleur d'accent : les deux
assets de logo doivent porter le nouveau visuel — `Assets/logo.png` (emblème seul, en-tête + icône de
fenêtre, `MainWindow.xaml:5` et `225`) et `Assets/logo-full.png` (emblème + nom), tous deux déclarés
`Resource` dans `PincabToolbox.App.csproj:37-38`. Ces images sont fournies par Maxime (hors périmètre
code) ; le plan ne fait que pointer où elles vivent. Attention : `logo.png` est l'**emblème seul** (la
boîte à outils sans le texte), pas le logo complet collé dans l'image du chat.

**Ne pas toucher.** L'orange de **sévérité** `Warning` (`#F5A524` : `App.xaml:16` + brosses code-behind
219/228/239/240/273 + légende du rapport HTML 2136) — c'est un signal, pas la marque (garde-fou §2.9).
Les autres couleurs de sévérité (lot 4). `CauseCardRow.AccentBrush` (149/155 — sévérité, garde-fou
§2.8). Le violet `DemoButton` (`#FF6E56CF`, non-marque).

**Point de vigilance produit (nouveau).** Le vert de marque olive-lime doit rester **visuellement
distinct** du vert **émeraude** de la sévérité `Ok`/santé (`OkSev`/`BrushOk` `#46C06E`, aussi utilisé
par le score ≥90 et l'accroche « tout va bien », `MainWindow.xaml.cs:829/851`). Le risque de confusion
est faible en pratique — la pastille `Ok` est masquée par défaut (`_showOk=false`,
`MainWindow.xaml.cs:202`) — mais choisir un vert de marque nettement plus jaune que l'émeraude, et
vérifier le rendu sur un scan sain en Mode démo.

**Terminé si.** `build.cmd` vert ; en Mode démo, plus aucun orange de marque à l'écran (l'orange ne
subsiste que sur les avertissements `Warning`), CTAs / score / progression en vert de marque, rapport
HTML exporté en vert ; le texte des boutons d'accent passe le contraste AA sur le nouveau vert ;
l'espacement est régulier (multiples de 8) ; le commit énumère les 5 sites basculés et les jetons
ajoutés.

---

### Lot 2 — Hiérarchie de l'écran Scanner (en grande partie déjà en place)

**État actuel réel.** Le score en hero, l'accroche pilotée par le **nombre de bloquants** (et non par
la seule note « F » — décision `docs/REVUE-maquettes-scanner-2026-08-11.md`, appliquée
`MainWindow.xaml.cs:842-851`), les pastilles et les actions **existent déjà** dans le bandeau. Ce lot
n'est donc **pas** une construction : c'est du calage de hiérarchie visuelle.

**Fichiers à toucher.** `MainWindow.xaml` (bandeau `Grid.Row=1`, XAML 297-409 ; ligne méta 414-424).
Idéalement aucun `.cs` (la donnée est déjà branchée).

**Ce qu'il faut faire (uniquement de la mise en forme portée par styles/tailles).**
1. Renforcer le contraste de rang : `ScoreChip` + `HeroHeadline` doivent dominer ; les pastilles et la
   ligne méta descendent d'un cran (taille/opacité/espacement via les jetons du lot 1).
2. Vérifier que rien n'écrase le plancher de contenu : la ligne `Grid` du tableau a un
   `MinHeight="240"` volontaire (XAML 261, commentaire 255-260) — ne pas le retirer.

**Ne pas toucher.** Le calcul du score et de l'accroche (`RefreshList` 824-851). Le fond ImageBrush +
voile dégradé (XAML 299-321) sauf ajustement d'opacité mineur. Les `x:Name`/gestionnaires des
pastilles (ce sont des filtres, garde-fou lot 5).

**Terminé si.** `build.cmd` vert ; en Mode démo (score 68/C, 1 critique, 2 causes — vérité terrain
`FIELD-LOG.md:735-736`), la lecture de l'écran va score → accroche → pastilles → détail sans
ambiguïté ; aucun `x:Name` supprimé (script de recoupement x:Name/gestionnaires à 0 erreur).

---

### Lot 3 — Onglet dédié aux mises à jour de tables (retour Joey Mahon)

**Décision d'architecture (tranchée ici, à ne pas re-décider).** Véhicule = **6ᵉ onglet interne du
Scanner** (`ScannerTabs`), pas un 5ᵉ onglet principal. Raison : toutes les vues « tranche du rapport »
(Composants, Tables, Système) sont déjà des onglets internes alimentés par le même `_report` ; les
updates en sont une de plus. Un onglet principal séparé exigerait sa propre gestion d'état pré-scan et
découplerait les updates du scan qui les produit — plus d'effort, plus de risque, pour le même
bénéfice. Ce choix s'écarte légèrement du mot « tab » de Joey (`FIELD-LOG.md:183`) mais répond
exactement à son besoin réel : **désencombrer la liste principale** sans masquer l'information.

**Fichiers à toucher.**
- `MainWindow.xaml` : ajouter un `TabItem x:Name="StabUpdates"` dans `ScannerTabs` (après `StabTables`
  ou en fin), avec en-tête `StabUpdatesHeader` et un contenu réutilisant `SideRowTpl` (ou une liste
  de `FindingRow` filtrée) + un `TextBlock` d'état vide, sur le modèle de `TablesTabEmpty`
  (XAML 776) / `CompTabEmpty` (719).
- `MainWindow.xaml.cs` : (a) exclure `Category=="updates"` du pipeline de `ListFindings` (filtre à
  poser dans `RefreshList`, chaîne `Rolled().Where(...)` 895-896) ; (b) recalculer `ChipInfo` pour
  ne compter que les Info **hors updates** (818-822) afin que la pastille Info ne promette pas des
  lignes qui ne sont plus là ; (c) peupler `StabUpdates` (nouvelle méthode `RefreshUpdatesTab`,
  appelée depuis `RefreshList` à côté des autres `Refresh*` 869-876) ; (d) compter l'onglet dans
  `RefreshInnerTabHeaders` (1459-1465).
- `src/PincabToolbox.App/Localization/Loc.cs` : ajouter la clé `stab.updates` dans les **trois**
  dictionnaires (en ~281, fr ~538, es ~780), sur le modèle de `stab.tables`. La clé `cat.updates`
  existe déjà (en 221 / fr 485 / es 729), ne pas la dupliquer.

**Comportement bêta (point de vigilance du brouillon).** Quand aucun `UPDATE_AVAILABLE` n'existe,
l'onglet affiche l'état réel du watcher : la ligne `VPS_MATCH_SUMMARY` (« matched X/Y … beta ») ou
`VPS_UNAVAILABLE` (hors-ligne), pas une promesse de fonctionnalité mûre. L'onglet ne doit jamais
paraître plus avancé que le scanner bêta qu'il reflète (garder le « (beta) » honnête).

**Ne pas toucher.** `UpdateWatcherScanner` ni aucun scanner Core (le lot est purement App/affichage —
la sévérité `Info` et les liens VPS restent tels quels ; ne jamais faire télécharger).

**Terminé si.** `build.cmd` vert ; en Mode démo, les findings de catégorie « Mises à jour »
n'apparaissent plus dans « Tous les résultats » et apparaissent dans le nouvel onglet ; le compteur
Info et le compteur de l'onglet sont cohérents avec ce qui est réellement listé ; avec une base VPS
absente, l'onglet montre l'état bêta au lieu d'être vide et muet ; les 3 langues affichent l'en-tête.

---

### Lot 4 — Composants partagés : styles de boutons + badges de sévérité réutilisant l'existant

**État actuel réel.** Boutons : style implicite `Button` + `AccentButton` (`App.xaml:28-63`),
`DemoButton`/`BrowseButton` (`MainWindow.xaml:71-79`). Badges de sévérité : construits **ad hoc** dans
`CauseCardTpl` (badge via `BadgeText`/`BadgeBg`/`BadgeBorder`, teintes `SevTint` 261) et dans la
colonne severity de `ListFindings` (ellipse + label, `SevBrush`/`SevLabel`). **Aucun style de badge
réutilisable** n'existe encore. Les couleurs de sévérité existent (§1.4) mais en **deux exemplaires
divergents** sur le rouge.

**Fichiers à toucher.** `App.xaml` (nouveau style/gabarit de badge partagé, s'appuyant sur les
`StaticResource` de sévérité existants). `MainWindow.xaml.cs` (réconciliation du rouge). Éventuellement
`MainWindow.xaml` pour pointer les badges existants vers le style partagé.

**Ce qu'il faut faire.**
1. **Réconcilier le rouge** : aligner `BrushCritical` (`MainWindow.xaml.cs:218`, `#FF6B6E`) sur la
   ressource canonique `Critical` d'`App.xaml` (`#FFE5484D`) — c'est cette dernière qui est déjà
   utilisée par les pastilles et les teintes de ligne, donc c'est la source de vérité. Vérifier
   ensuite que le score en très bas (`scoreBrush` 829) et l'accroche bloquante (851) restent lisibles
   avec le rouge canonique. Documenter le changement dans le commit (une couleur, pas cinq).
2. **Extraire un badge de sévérité partagé** dans `App.xaml`, paramétré par la couleur de sévérité,
   réutilisé par les cartes de cause et, si possible, par la cellule severity de `ListFindings` — une
   seule définition, zéro nouvelle couleur (brouillon §2 : « surtout pas en inventer »).
3. Ne standardiser les boutons que si un besoin réel apparaît (ex. bouton « secondaire » récurrent) —
   sinon s'en tenir aux styles existants.

**Ne pas toucher.** Le violet `NoteSev`/`BrushNote` (distinction Note↔Info voulue, ADR-010). La
sémantique `SevBrushOf`/`SevGlyph`.

**Terminé si.** `build.cmd` vert ; il ne reste **qu'une** valeur de rouge critique dans le code
(grep : plus de `0xFF, 0x6B, 0x6E`) ; les badges de cause et la liste des résultats tirent leurs
couleurs des mêmes ressources ; aucune couleur de sévérité nouvelle introduite.

---

### Lot 5 — Tableaux : densité, hover, alignement (compléter, pas dupliquer)

**État actuel réel.** `ListFindings` a **déjà** tri, recherche, filtres, virtualisation, hover et
sélection (§1.2). `TablesListFull` (onglet Tables) est virtualisé (`MainWindow.xaml:752-773`) ; le
résumé `TablesList` (Causes) est plafonné à 12 lignes. Il n'y a **pas** de colonne numérique dans
`ListFindings`.

**Fichiers à toucher.** `MainWindow.xaml` (styles `ListViewItem`/`GridViewColumnHeader` 33-70, padding
des templates `TableRowTpl`/`CompRowTpl`/`SideRowTpl`). Pas de `.cs` attendu.

**Ce qu'il faut faire.** Uniquement de la densité et du confort de lecture portés par styles :
hauteur de ligne / `Padding` régularisés via les jetons du lot 1, hover déjà présent à conserver,
en-têtes de colonnes plus lisibles. Ne PAS ajouter de tri ni de recherche (déjà là). L'« alignement
des chiffres à droite » ne s'applique qu'au Diff s'il faut y toucher (numéros de ligne déjà à droite,
847/863) — sinon sans objet.

**Ne pas toucher.** `VirtualizingPanel.*` (garde-fou §2.7). Les plafonds 12/8 des `ItemsControl` non
virtualisés. Les `x:Name` des pastilles/colonnes utilisés par le tri et les filtres.

**Terminé si.** `build.cmd` vert ; en Mode démo, les lignes sont plus aériennes et lisibles, tri /
recherche / filtres fonctionnent exactement comme avant (aucune régression de comportement), et une
grosse collection reste fluide (virtualisation intacte).

---

### Lot 6 — Polish : animations courtes + progression par étapes nommées

**État actuel réel — et bonne surprise de faisabilité.** Le moteur **rapporte déjà des étapes
nommées** : `ScanEngine.Run(IProgress<string>…)` émet `« Running {scanner.Name}… »`
(`ScanEngine.cs:52`) et `« Reading table i/n: {name} »` (45) ; `RunAcrossDrive` en émet aussi (93-102).
Côté App, `BtnScan_Click` branche `var progress = new Progress<string>(msg => LblStatus.Text = msg)`
(`MainWindow.xaml.cs:637`), mais la barre `ScanProgress` est `IsIndeterminate` (`MainWindow.xaml:1014`)
et le message n'est qu'un texte fugace dans `LblStatus`. **Les étapes nommées existent donc déjà
côté données — le lot 6 est purement App, aucun changement Core nécessaire.**

**Fichiers à toucher.** `MainWindow.xaml` (barre de statut 1011-1019) et `MainWindow.xaml.cs`
(`BtnScan_Click` 599-728, callback `progress` 637).

**Ce qu'il faut faire.**
1. Surfacer les étapes : afficher le libellé d'étape courant de façon plus visible (et/ou une
   progression déterminée si l'on dérive un ratio depuis `i/n` déjà présent dans les messages).
2. Micro-animations discrètes uniquement (fondu court, transition de la barre) — jamais bloquantes,
   jamais sur le chemin d'un `MessageBox`/dialogue (contrainte browser-agnostique du projet : pas de
   dialogue modal déclenché par une animation).
3. « Logs en timeline » : optionnel, si retenu, un simple accumulateur des messages `progress` déjà
   reçus — ne pas ouvrir de nouveau canal réseau ni de nouveau fichier.

**Ne pas toucher.** `ScanEngine` ni la signature `IProgress<string>` (Core figé pour ce lot).
`BtnCheckUpdate` (seul appel réseau, ne pas lui coller d'animation « connexion »).

**Terminé si.** `build.cmd` vert ; pendant un scan en Mode démo, l'utilisateur voit défiler des étapes
nommées réelles (noms de scanners / tables) au lieu d'une barre indéterminée muette ; aucune animation
ne bloque l'UI ni ne déclenche de dialogue.

---

### Lot 7 — Panneau de détail latéral, isolé (dernier, le plus risqué)

**État actuel réel.** Le détail d'un finding s'affiche dans `DetailPanel`
(`MainWindow.xaml:655-700`), **ancré en bas** de `StabResults`, `MaxHeight="250"`, déjà dans un
`ScrollViewer`. Il est peuplé par `ListFindings_SelectionChanged` (`MainWindow.xaml.cs:1537-1591`) avec
Sujet, Message, Symptôme, Impact, Cause, Explication, Correctif, Vérification, l'étiquette Repair
(`DetailRepairTag`, checks calculés), le bouton d'action et le chemin.

**Ce qu'il faut faire.** Remplacer le panneau bas par un **panneau latéral droit rétractable**, isolé
dans un **composant neuf** (nouveau `UserControl` ou section XAML dédiée), pas en rafistolant le
panneau bas. Rebrancher `ListFindings_SelectionChanged` et `BtnCloseDetail_Click` (1600) dessus.
Conserver **tous** les champs et le même `ScrollViewer` — le panneau latéral doit défiler, jamais
tronquer.

**Garde-fou de test central (brouillon §2).** Tester avec le **contenu réel le plus long** : un finding
dont le détail est volumineux **et** un cas où le Repair est **non automatisable avec plusieurs
raisons**. Masquer ou tronquer `RepairNotAutomatableLine` / les lignes `CanApply=false` casserait une
contrainte produit (ADR-006 §2), pas juste une maquette. Le panneau doit rester lisible et défilable
sur ce cas, pas rogner le contenu.

**Ne pas toucher.** Le contenu et l'ordre des champs de détail (déjà validés). La logique de sélection
(`SelectionChanged`). Ne faire ce lot **que si les lots 1-6 sont stables** (le brouillon le pose en
dernier, isolé, à forte surface de régression).

**Terminé si.** `build.cmd` vert ; en Mode démo, sélectionner un finding ouvre le panneau à droite, le
referme proprement (`BtnCloseDetail`), et sur un finding à Repair non-automatisable multi-raisons le
panneau montre l'intégralité du texte via défilement, sans troncature ; le tableau des résultats
garde son plancher de hauteur.

---

## 4. Ordre, dépendances, découpage en commits

Un commit par lot, dans l'ordre 1 → 7. Dépendances réelles :

- Le **lot 1 (jetons d'espacement)** est un prérequis de confort pour les lots 2 et 5 (ils
  consomment les jetons). Le faire d'abord évite de re-régler l'espacement deux fois.
- Le **lot 4 (réconciliation du rouge)** devrait précéder tout lot qui retouche des couleurs de
  sévérité, mais reste indépendant des lots 1-3.
- Le **lot 7** dépend de la stabilité des lots 1-6 (surface de régression maximale).
- Les lots 2, 3, 5, 6 sont largement indépendants entre eux une fois le lot 1 posé.

---

## 5. Points non vérifiables ou hors périmètre (honnêteté)

- **Aucune compilation ni test d'UI dans cette session** : l'App WPF ne se compile pas hors Windows
  (NU1100). Ce plan est ancré sur la **lecture** du code, pas sur une exécution. Tout critère
  « terminé » ci-dessus est à valider par Maxime via `build.cmd` + Mode démo.
- **Changement de police / échelle typographique** (brouillon §2) : écarté de ces 7 lots. Une police
  non installée sur la cible dégrade silencieusement le rendu — à valider hors de cette refonte avant
  de s'engager.
- **Bloc de confiance de GPT** : déjà couvert par l'onglet About (`TabAbout`, `AboutBody`,
  `BtnCheckUpdate`) — enrichissement éventuel, pas une création ; hors des 7 lots.
- **Sujets scanner** (ex. intégrité AltColor/DMD, `FIELD-LOG.md:199-218`) : explicitement **hors
  périmètre** de cette refonte UI (décision Maxime, ne pas élargir la tâche en silence).
- Le renvoi `FIELD-LOG.md:742` à **ADR-002** pour l'absence de voyant réseau est cité tel
  qu'écrit dans le FIELD-LOG ; le contenu d'ADR-002 lui-même n'a pas été relu dans cette session
  (hors des ADR-006/010/012 demandés). Le garde-fou tient sur le fait consigné, pas sur une
  interprétation d'ADR-002.
