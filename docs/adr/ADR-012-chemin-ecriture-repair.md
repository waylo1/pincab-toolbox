# ADR-012 — Le chemin d'écriture Repair (Preflight → Apply → Undo)

**Statut** : Accepté, codé le 11/08/2026 (session Sonnet, lot communauté 10/08, LOT H).
**Décideur** : Maxime Chauvin.
**Contexte lié** : ADR-005 (registre d'actions fermé), ADR-006 (dry-run gratuit), ADR-009 (paiement,
non câblé), `docs/SPEC-lot-communaute-2026-08-10.md` §5 LOT H/I.

## Contexte

Depuis le 27/07, `IRepairEngine.Preflight`/`Apply`/`Undo` existaient, testés, mais n'étaient appelés
nulle part dans l'App — `RepairOfferBuilder` (Écran 1) s'arrête volontairement à `Plan(licensed:
false)`, en lecture seule. C'est la **première fois que ce produit écrit réellement sur la machine
d'un utilisateur.** Maxime a tranché le 10/08 (spec §4, décision 1) : câbler le chemin d'écriture
dans cette session, avec un bloqueur dur identifié en amont — le journal était encore
`InMemoryRepairJournal`, donc `Undo` ne survivait pas à la fermeture de l'app. D'où l'ordre de
travail imposé : H.1 (journal persistant) avant tout le reste.

Trois questions de conception restaient ouvertes une fois H.1 fait :
1. Où vit la logique de décision (vérifier la licence, construire le plan, décider quoi appliquer) ?
2. Comment garantir qu'aucune écriture n'est silencieuse ni groupée par défaut ?
3. Que faire du LOT I (ré-enregistrement COM), qui introduit une classe de capacité entièrement
   nouvelle — exécuter un processus externe — pendant la même session ?

## Décision

### 1. `RepairSession` — toute la logique dans `PincabToolbox.Repair`, jamais dans l'App

`RepairSession` (`src/PincabToolbox.Repair/Engine/RepairSession.cs`) compose le moteur exactement
comme `RepairOfferBuilder` le fait déjà pour Écran 1, avec deux différences : un
`FileRepairJournal` réel (au lieu du journal en mémoire de la preview) et une licence **vérifiée
par cette classe elle-même** (`VerifyLicense`), jamais transmise par un appelant qui l'aurait
supposée valide.

C'est un écart volontaire par rapport au précédent établi (`RepairOfferBuilder` vit dans
`PincabToolbox.App`). Raison : `PincabToolbox.App` est WPF (`net8.0-windows`) et ne peut être
compilé ni testé automatiquement dans tous les environnements où ce code est travaillé (SDK Windows
Desktop absent hors Windows) — c'est un fait déjà documenté du projet, pas une nouveauté de cette
session. En poussant toute décision du premier chemin d'écriture réel dans un projet `net8.0`
multiplateforme et entièrement testable, la partie la plus risquée de l'histoire du projet reste
dans le filet de sécurité qui tourne partout. Le code côté App (`MainWindow.xaml`/`.xaml.cs`,
l'onglet Repair) reste volontairement fin : du XAML et des gestionnaires de clic qui appellent
`RepairSession` et affichent ce qu'elle retourne — aucune décision d'écriture n'y est prise.

### 2. Sécurité par construction, pas par discipline

- **Licence jamais assumée.** `RepairSession.VerifyLicense` revérifie contre la clé publique
  embarquée à chaque appel. Cette clé est aujourd'hui un `PLACEHOLDER` littéral
  (`LicenseVerifier.EmbeddedPublicKeyBase64`) — `Verify()` retourne donc `Invalid` pour n'importe
  quelle clé tant que `license-tool init` n'a pas été exécuté pour de vrai. Conséquence directe :
  `licensed:false` systématiquement en production aujourd'hui → chaque réparation possible résout en
  `RepairMode.Locked` → `RepairEngine.Apply` ignore tout item `Locked`. **Le chemin d'écriture est
  donc un no-op prouvé en production tant que la vraie clé n'est pas déployée**, quel que soit un bug
  résiduel côté WPF non compilable ici. C'est ce qui rend raisonnable de câbler l'UI maintenant.
- **Jamais de "tout réparer" silencieux.** `RepairSession.Apply(plan, selectedItemIds)` n'applique
  que les items dont l'`ItemId` est explicitement dans l'ensemble sélectionné — indépendamment de ce
  que `RepairMode.Automatic` autoriserait techniquement. C'est la réponse du v1 à la règle H.2 n°3 de
  la spec.
- **Confirmation explicite pour l'irréversible.** Côté App, `BtnRepairApply_Click` construit les
  faits (`RepairSession.Describe`) des items sélectionnés et affiche une boîte de dialogue de
  confirmation dédiée dès qu'un item n'est pas entièrement réversible, avant tout appel à `Apply`
  (règle H.3).
- **Bug corrigé pendant cette session** : `RepairEngine.Apply` ne protégeait pas l'échec de
  `IBackupService.Backup` — une exception s'y propageait sans garde. Corrigé par un `try/catch`
  explicite : un échec de sauvegarde journalise `JournalEvent.BackupFailed`, marque l'item en échec
  et **n'écrit jamais** (règle H.2 n°4). Test dédié : `Test_Apply_BackupFailure_NeverWrites`.
- **Historique d'annulation accessible sans nouveau scan.** `RepairSession.KnownPlanIds()` lit le
  journal sur disque — l'onglet Repair l'affiche dès l'ouverture de l'app, avant même le premier
  scan de la session (règle H.2 n°5).

### 3. LOT I — la capacité est codée et testée, mais délibérément PAS câblée dans une règle vivante

`RegisterComComponentAction` (`src/PincabToolbox.Repair/Actions/RegisterComComponentAction.cs`)
implémente les sept règles de confinement obligatoires de la spec §5 LOT I : liste blanche
d'exécutables en dur, résolution en chemin canonique avant toute vérification (le confinement du
moteur, ADR-005, s'applique aussi à `ChangeKind.ComReregistration` — aucune exemption ajoutée),
zéro argument dérivé du scan, vérification PE + bitness via `PeInspector` avant tout lancement,
timeout obligatoire (`RealProcessLauncher`), vérification d'élévation au moment de l'usage
(`RealElevationProbe`, jamais supposée depuis `app.manifest` seul), et `IsReversibleByNature =
false`.

Elle n'est **volontairement pas ajoutée** au registre construit par `RepairSession`/
`RepairOfferBuilder`, et **aucune `RepairRule` ne la référence** dans
`knowledge/pack-2026.08.json` — sans règle de pack pointant `COM_NOT_REGISTERED`/
`VPINMAME_NOT_REGISTERED`/`COM_BITNESS_GAP` vers `register_com_component`, `RepairEngine.Plan` ne
produit jamais de `PlannedChange` par cette action, quel que soit le contenu du registre de
capacités (ADR-005 : la donnée compose, elle ne peut pas activer une capacité que la donnée
elle-même ne référence pas). **Même précédent déjà établi par `SetDefaultAudioDeviceAction`** :
capacité construite et testée, gardée hors du câblage vivant jusqu'à validation réelle.

Deux inconnues, documentées dans l'en-tête de la classe, restent à valider sur une vraie machine
avant d'ajouter la règle de pack qui l'activerait :
1. Que l'outil d'enregistrement vit bien à côté de la DLL du composant (hypothèse de
   `Plan()`, jamais confirmée par une installation réelle) pour les trois outils.
2. Le comportement réel de chaque outil lancé sans argument — `Setup.exe` de VPinMAME est un
   installeur graphique interactif connu, pas un enregistreur silencieux ; annoncer une
   "réparation appliquée" serait trompeur si l'utilisateur doit encore cliquer lui-même dans une
   fenêtre que l'app vient d'ouvrir pour lui.

C'est l'application directe de la clause de sortie que la spec elle-même prévoit pour ce lot :
« si l'un de ces points ne peut pas être tenu proprement, ne pas livrer le LOT I ». La détection du
LOT A (câblée, testée) reste la valeur livrée cette session sur ce thème.

## Alternatives écartées

- **Construire `RepairSession` dans `PincabToolbox.App`, comme `RepairOfferBuilder`.** Aurait suivi
  le précédent existant, mais aurait laissé la logique de décision la plus critique du projet dans
  la seule partie du code qu'aucun test automatique ne peut exécuter dans cet environnement.
- **Ne pas coder le LOT I du tout, faute de pouvoir le valider sur une vraie machine.** Rejeté :
  la partie testable sans Windows (liste blanche, confinement, portes PE/élévation, contrat
  zéro-argument) peut et doit être écrite et testée maintenant ; seul le CÂBLAGE vivant (la
  `RepairRule` dans le pack) doit attendre une validation réelle. Retenir tout le code aurait aussi
  retardé la revue de sa conception.
- **Ajouter `RegisterComComponentAction` au registre `RepairActionRegistry` quand même, sans règle
  de pack.** Envisagé puis retenu comme second filet inutile : sans règle, l'action est déjà inerte
  (le registre expose des capacités, jamais leur activation) — l'ajouter au registre maintenant
  n'apporterait rien et créerait une fausse impression de "presque prêt". Elle est prête,
  au sens code ; ce qui manque est une validation terrain, pas une ligne de câblage.

## Conséquences

**Positives**
- Le chemin d'écriture existe, est testé (122 → 139 tests Repair après cette session, tous verts),
  et son point d'entrée le plus dangereux (Apply) est prouvé être un no-op en production tant que la
  vraie clé de licence n'est pas déployée.
- L'App reste un simple client de `RepairSession` — un futur portage d'UI (ou des tests d'intégration
  WPF, le jour où l'environnement le permet) n'aurait aucune logique de décision à dupliquer ou à
  revalider.
- LOT I est prêt à être activé par un changement de DONNÉE (une entrée dans le pack), pas de code,
  le jour où les deux inconnues ci-dessus sont validées.

**Coût**
- Deux endroits où une session future pourrait chercher "la" composition Repair
  (`RepairOfferBuilder` pour Écran 1 lecture seule, `RepairSession` pour Écran 2 écriture) —
  documenté dans l'en-tête de chacune des deux classes pour éviter la confusion.
- L'onglet Repair de `MainWindow.xaml`/`.xaml.cs` n'a pu être relu qu'à la main (parenthèses/accolades
  équilibrées vérifiées, XML validé, et un `csc` sans les références WPF confirme l'absence d'erreur
  de syntaxe malgré l'impossibilité de résoudre les types WPF) — jamais compilé ni exécuté dans cette
  session. À vérifier en premier sur la machine de Maxime.

## Non fait

- La `RepairRule` de pack qui activerait `RegisterComComponentAction` (LOT I) — bloquée sur
  validation réelle, voir ci-dessus.
- Les textes `about.body`/`about.roadmap` ont été mis à jour (H.5) pour ne plus annoncer Repair comme
  "à venir" — mais aucun parcours d'achat n'existe encore (ADR-009 non câblé), donc la licence reste
  aujourd'hui impossible à obtenir légitimement ; seul un testeur avec un accès direct à
  `license-tool` peut voir le chemin d'écriture s'activer.
- Aucun test d'intégration bout-en-bout de l'onglet Repair (WPF non exécutable ici) — seule la
  logique qu'il appelle (`RepairSession`) est testée directement.

## Suite — 11/08/2026 : clé de licence réelle déployée, le no-op de production prend fin

Le paragraphe "Licence jamais assumée" de la section 2 ci-dessus décrit un état qui n'est plus
d'actualité : `LicenseVerifier.EmbeddedPublicKeyBase64` n'est **plus** le `PLACEHOLDER` littéral.
Le même jour, Maxime a exécuté `license-tool init` sur sa propre machine (hors ligne, clé privée
jamais transmise ni générée dans une session cloud — voir l'en-tête de `LicenseVerifier.cs`) et a
transmis la clé **publique** résultante, qui est maintenant celle embarquée dans l'App.

Conséquence directe sur le raisonnement de sécurité de cet ADR : **`Apply()` n'est plus un no-op
prouvé en production.** N'importe quelle licence signée par la clé privée correspondante (celle que
seul Maxime détient, sur sa machine, hors de tout repo) fera passer `VerifyLicense` à `Valid`, donc
`licensed:true` dans `RepairSession.Plan`, donc de vrais items en `RepairMode.Automatic` ou
`ConfirmationRequired` plutôt que systématiquement `Locked`. Le filet qui restait pour la session du
10-11/08 (« même si l'onglet Repair a un bug WPF non détecté ici, aucune écriture réelle n'est
possible ») n'existe donc plus une fois cette clé publique effectivement déployée dans un build
distribué.

Ce qui reste vrai, inchangé par cette mise à jour :
- Toutes les autres garanties de la section 2 (sélection explicite jamais groupée, confirmation
  obligatoire pour l'irréversible, échec de backup = aucune écriture, journal persistant) sont des
  propriétés du code, indépendantes de la validité de la clé — elles s'appliquent identiquement
  qu'une licence soit valide ou non.
- ADR-009 (achat automatisé) reste non câblé : aujourd'hui, la seule façon d'obtenir une licence
  valide est que Maxime en émette une lui-même via `license-tool issue`, sur sa machine. Aucun
  chemin d'achat public n'expose donc encore ce risque à un utilisateur non choisi par Maxime.
- LOT I (`RegisterComComponentAction`) reste inerte, clé réelle ou non — sa désactivation tient à
  l'absence de `RepairRule` dans le pack, un mécanisme entièrement indépendant de la licence.

Ce que ce changement demande concrètement avant toute distribution plus large : que Maxime valide
lui-même, sur sa machine, au moins un cycle complet Preflight → Apply → Undo avec une vraie licence
émise par lui, sur les quatre actions déjà câblées (`UnblockFileAction`, `RestoreRomArchiveAction`,
`QuarantineOrphanedMediaAction`, `KillZombiePinUpDisplayAction`) — la couverture de test de cette
session (140 tests Repair, tous verts, y compris `Test_EmbeddedPublicKey_IsARealKey_NotThePlaceholder`
qui verrouille contre un retour accidentel au placeholder) donne une haute confiance dans la logique,
mais aucune de ces exécutions n'a eu lieu sur une vraie installation Windows dans cette session.
