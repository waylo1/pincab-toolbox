# ADR-011 — Scan multi-racines (disque entier)

**Statut** : Accepté, codé le 10/08/2026.
**Décideur** : Maxime.
**Contexte lié** : ADR-005 (registre d'actions fermé), ADR-006 (dry-run gratuit), Scanner gelé (03/08).

## Contexte

Demande de Maxime : « le scanner doit lire tout le disque, pas fichier par fichier ». Clarifié en
session (10/08) en deux temps :
1. Pas un crawl indifférencié de chaque fichier du disque — la couverture doit inclure tous les
   dossiers liés au pincab présents sur `C:`, pas seulement la racine unique choisie à la main.
2. Raison produit donnée après coup : « il faut scanner l'ensemble car des choses d'autres
   dossiers peuvent influencer les autres dossiers » — plusieurs installs/dossiers sur le même
   disque peuvent interagir (VPinMAME partagé, alias ROM, médias orphelins d'un ancien dossier),
   et les scanner isolément un par un peut manquer ces interactions.

Le Scanner est gelé depuis le 03/08 (aucun nouveau check sans deux signaux terrain indépendants).
Un changement de PORTÉE du scan (où il cherche) n'est pas un nouveau check au sens de cette règle,
mais reste une décision d'architecture qui mérite un ADR — Maxime a donné le feu vert explicite le
10/08 pour rouvrir ce point précis : « corrige les adr si il le faut, code le câblage, code ».

## Décision

Ajout d'une couche d'orchestration au-dessus du pipeline existant, **sans modifier aucun scanner
ni la forme de `InstallLayout`/`ScanReport` mono-racine** :

- **`DriveInstallFinder`** — marche bornée (profondeur 6, dossiers bruit système ignorés) sur le
  point de départ (typiquement `C:\`), reconnaît un dossier candidat via les mêmes signaux que
  `LayoutDetector` (candidats `Profile.Locations`), sans jamais appeler le vrai `LayoutDetector.Detect`
  (récursif, profondeur 5) à chaque nœud visité — ça aurait été O(nœuds visités) × O(recherche
  profondeur 5), inutilisable sur un vrai `C:`. Un dossier confirmé stoppe la récursion à l'intérieur
  (pas de faux doublons imbriqués) mais n'empêche pas de trouver d'autres installs indépendants
  ailleurs sur le disque.
- **`ScanEngine.RunAcrossDrive`** — trouve les racines candidates, relance le pipeline `Run`
  existant (inchangé) sur CHACUNE, agrège dans un `DriveScanReport`.
- **`DriveScanReport.ToMergedScanReport()`** — fusionne tout en un `ScanReport` normal (Layout
  synthétique dont `RootPath` = le disque), pour que le reste de l'app (export HTML/MD/BBCode,
  bindings MainWindow) n'ait AUCUN changement à faire pour consommer un scan multi-racines.
- **`ScanScoring`** — extraction pure (Score/Grade/Ordered/Rolled) de `ScanReport` vers une classe
  statique partagée, pour que `DriveScanReport` réutilise exactement la même formule au lieu d'une
  deuxième implémentation qui dériverait avec le temps. Aucun changement de comportement pour le
  cas mono-racine existant.
- **App** : aucun nouvel élément d'UI. Le champ de racine existant détecte lui-même qu'on lui a
  donné une racine de lecteur (`C:\`) plutôt qu'un dossier précis (`DirectoryInfo.Parent is null`)
  et bascule automatiquement sur `RunAcrossDrive`.

## Ce qui NE change PAS (garde-fou explicite)

**Le confinement Repair (ADR-005) reste par install réel, jamais par le disque entier.**

`RepairOfferBuilder.Build` passait déjà un tableau de racines de confinement à `RepairEngine`
(`new[] { report.Layout.RootPath }`) — construit pour un seul cas : un `ScanReport` mono-racine où
`Layout.RootPath` EST le vrai dossier d'install. Un `ScanReport` fusionné par
`ToMergedScanReport()` a un `Layout.RootPath` synthétique qui vaut le DISQUE ENTIER (`C:\`).
Passer ça tel quel à `RepairEngine` aurait autorisé, par construction, n'importe quelle cible sur
tout `C:\` — exactement l'inverse de ce qu'ADR-005 garantit.

Corrigé par un second paramètre explicite : `RepairOfferBuilder.Build(report, confinementRoots)`.
En mode disque entier, l'App passe la vraie liste des racines d'install trouvées par
`DriveInstallFinder` (pas la racine synthétique du rapport fusionné) — chaque action Repair reste
donc confinée à l'install réelle dont elle provient, jamais élargie au disque. Le cas mono-racine
existant (`Build(report)` sans second paramètre) est inchangé bit à bit.

## Conséquences

- Lecture : un scan sur `C:\` trouve et agrège tous les installs pincab du disque en un seul
  rapport, répond à la demande du 10/08.
- Écriture (Repair) : aucun affaiblissement de la doctrine de confinement — vérifié et corrigé
  avant livraison, pas après coup.
- Coût : la marche du disque entier peut prendre du temps sur une grosse machine (des minutes,
  pas des heures — profondeur bornée à 6, dossiers système/bruit ignorés) ; pas mesuré en
  conditions réelles (pas de `dotnet` en sandbox), à observer au premier scan réel de Maxime.
- Le rapport fusionné (`DRIVE_SCAN_SUMMARY`) liste explicitement chaque install trouvé et son
  chemin réel, pour que l'ambiguïté du Layout synthétique reste visible plutôt que cachée.

## Non fait

- Pas de nouvel élément d'UI dédié (case à cocher, bouton) — le champ existant suffit (détection
  automatique d'une racine de lecteur). À revoir si Maxime veut une action plus explicite/visible.
- Pas de test unitaire ajouté (`dotnet` indisponible en sandbox) — à ajouter dès que possible,
  notamment pour `DriveInstallFinder` (pas de doublon imbriqué, dossiers bruit bien ignorés) et
  `RepairOfferBuilder.Build(report, confinementRoots)` (confirme qu'un tableau vide/incorrect ne
  laisse jamais une action passer hors de son install réel).
