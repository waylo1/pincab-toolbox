# PROJECT BRAIN — Pincab Toolbox / FlipSync

**Source de vérité unique du projet.** En cas de contradiction avec n'importe quel autre document, **ce fichier gagne.**

MC Automation — Maxime Chauvin · Dernière mise à jour : **06/08/2026** (session autonome Sonnet 5 — dégel Scanner formalisé en ADR-010, files Tier A **et** Tier B livrées, 26 scanners)

> **Règle de maintenance** : toute décision susceptible d'être relue dans six mois entre ici ou dans un ADR, le jour où elle est prise. Un document qui n'est plus vrai est plus dangereux qu'un document qui n'existe pas — on l'archive immédiatement.

---

## 1. Le projet en 30 secondes

**Pincab Toolbox** : outil de diagnostic pour cabinets de flipper virtuel (Visual Pinball X / PinUP Popper), Windows, .NET 8 / WPF. Le **Scanner** est gratuit, 100 % local, lecture seule. Le module **Repair**, payant, corrige ce que le Scanner trouve.

**FlipSync** est la **marque-parapluie** de MC Automation. Ce n'est le nom d'aucun produit. *(Un ancien document nommait « FlipSync » le produit de sauvegarde/migration — cet usage est mort, voir ADR-001.)*

Ce qu'on construit n'est pas un scanner de plus, c'est un **moteur d'expertise** : symptôme → cause → correctif, avec une base de connaissance vivante. Détail technique dans `ARCHITECTURE-KnowledgeEngine.md`.

Réflexe visé dans la communauté : **« Lance Pincab Toolbox → Health Check → poste le rapport. »**

---

## 2. Les règles absolues

Non négociables. Elles priment sur toute opportunité commerciale. Formalisées dans **ADR-004**.

1. **On vérifie et on prépare — on ne fournit jamais.** Aucun téléchargement de tables, ROMs, médias, backglass, colorisations. Seule exception : les dépendances **open source** (Freezy, VLC, B2S, DOF).
2. **Lecture seule par défaut.** Le Scanner ne modifie rien, ne télécharge rien, zéro télémétrie. Jamais bridé pour forcer l'achat.
3. **Repair est un système critique.** Sauvegarde → dry-run → opt-in par correctif → annulation → journal. Une action non réversible n'est jamais automatique.
4. **Pas de scraping** des forums communautaires. Ce sont nos canaux de distribution, pas nos sources de données.
5. **Marques tierces** : usage descriptif seulement (« compatible avec VPX / PinUP Popper »), jamais dans un nom de produit.

**La confiance est le seul actif qui ne se rachète pas.** En cas d'hésitation entre une fonctionnalité et la confiance, la confiance gagne.

---

## 3. Carte produit canonique

Cinq lignes, pas une de plus. Deux sont actives, trois sont **nommées et parquées** — elles servent de filtre : *une idée qui n'entre dans aucune de ces cinq cases est hors périmètre.* Voir **ADR-001**.

| # | Ligne | Ce qu'elle couvre | État |
|---|---|---|---|
| 1 | **Scanner** | Diagnostic en lecture seule. Le produit d'acquisition. | 🟢 **Actif** — alpha 0.1 |
| 2 | **Repair** | Tout ce qui **écrit** sur l'install, sous contrat de sûreté. Inclut **sauvegarde & migration**. | 🟢 **Moteur v1 implémenté** — 55 tests verts, 2 actions |
| 3 | **Play Optimizer** | Ce qui tourne **pendant le jeu** ou règle le **matériel** de la cab : focus, écrans, routage audio/SSF, nudge. | ⚪ Parking |
| 4 | **Table Companion** | La **bibliothèque de tables** : vérifier une table fraîchement téléchargée, médias Popper, doublons, alias, **colorisation & son**. | ⚪ Parking |
| 5 | **Creator Suite** | Pour ceux qui **font** des tables : diff de scripts, Script Doctor, validation des dépendances avant publication. | ⚪ Parking |

### La frontière entre les lignes

Une seule question tranche : **sur quoi l'outil agit-il ?**

- Il **lit** → Scanner.
- Il **écrit sur l'état statique** de l'install (fichiers, registre, base Popper) → Repair.
- Il **tourne pendant que tu joues**, ou il règle le **matériel** → Play Optimizer.
- Il agit **par table**, sur le contenu que tu télécharges → Table Companion.
- Il s'adresse à quelqu'un qui **produit** une table → Creator Suite.

> **Conséquence assumée à connaître** : le *Focus Guardian* — présenté comme « module vedette » dans l'ancienne synthèse — est un résident actif pendant le jeu. Il tombe donc dans **Play Optimizer**, pas dans Repair v1. Ça allège Repair v1, mais ça lui retire son argument le plus vendeur. Si le lancement payant a besoin de ce module, l'option propre est de sortir Play Optimizer *avant* Repair, pas de casser la frontière. À trancher au moment du design Repair, pas avant.

### Ce qui n'est PAS sur la carte

Le **flipper physique** (Switch Matrix Solver, assistant de diagnostic, parseur d'audits Stern) n'est pas une 6ᵉ ligne. C'est un **repackaging futur du même moteur pour un autre public**, hors carte tant que le premier euro n'est pas encaissé. Même chose pour tout élargissement hors flipper (MAME, RetroArch, serveurs de jeux…) : parqué, sans discussion, jusqu'au premier euro.

---

## 4. Modèle économique

Figé dans **ADR-002**.

| Palier | Prix | Contenu |
|---|---|---|
| **Scanner** | **Gratuit à vie** | Diagnostic complet, illimité, jamais bridé |
| **Repair — early bird** | **12 €** | Premiers acheteurs du forum. Licence perpétuelle + 12 mois de mises à jour |
| **Repair — prix normal** | **19 €** | Licence perpétuelle + 12 mois de mises à jour |
| **Renouvellement** | **9 € / an — optionnel** | Prolonge l'accès aux mises à jour de 12 mois |

**La mécanique, en une phrase** : tu achètes une fois, le logiciel est à toi pour toujours ; l'abonnement ne porte que sur les **mises à jour**, et si tu ne renouvelles pas, l'app continue de fonctionner indéfiniment avec le dernier Knowledge Pack reçu.

C'est le modèle JetBrains / Sublime Text. C'est le seul récurrent qu'une communauté allergique à l'abonnement accepte, parce qu'il ne prend jamais l'utilisateur en otage.

**Ce qui justifie le récurrent** : le Knowledge Pack vivant — nouveaux codes de finding, nouveaux correctifs, compatibilité avec les nouvelles versions de VPX. **Si le Knowledge Pack cesse de s'enrichir, le renouvellement devient indéfendable et il faut arrêter de le vendre.** Ce lien est la condition du modèle, pas un détail.

**Packaging** : un seul exécutable, un seul installeur. Le Scanner annonce qu'une réparation existe et ce qu'elle garantit ; la licence déverrouille le **plan détaillé** et son exécution (ADR-006). Vérification 100 % locale (signature hors-ligne), cohérente avec le discours zéro télémétrie.

---

## 5. Plateforme de paiement — décidé : Lemon Squeezy (ADR-009)

**Décidé le 27/07/2026 : Lemon Squeezy — Merchant of Record (voir ADR-009).** Il collecte et reverse la TVA/taxes mondiales à notre place (la réponse au seuil B2C UE de 10 000 € ci-dessous). La vérification de licence reste 100 % locale (ADR-002), donc rien dans l'architecture n'en dépend.

La veille est faite et datée : `docs/PARKING-plateformes-paiement.md` (Stripe, Lemon Squeezy, Paddle,
impact TVA, seuils français).

Deux choses à retenir sans y revenir :

- **Le seuil qui mordra en premier n'est pas la franchise en base** (37 500 €) mais celui des ventes
  B2C dans l'UE (**10 000 €** cumulés), parce que les acheteurs seront internationaux dès le premier
  jour. C'est ce que règle un Merchant of Record.
- **Rien dans l'architecture n'en dépend.** La vérification de licence est 100 % locale (ADR-002) ;
  la plateforme ne fait que générer une clé et encaisser. Changer d'avis plus tard coûtera un
  après-midi de paramétrage, pas une refonte.

## 6. État du projet

| Bloc | État | Compilable dans le cloud ? |
|---|---|---|
| `PincabToolbox.Core` (net8.0, zéro dépendance) | ✅ Stable — **321 tests verts** | ✅ Oui — TDD réel |
| `PincabToolbox.Repair` (net8.0, zéro dépendance) | ✅ Moteur v1 — **105 tests verts**, 5 actions (`unblock_file`, `restore_rom_archive`, `kill_zombie_pinup_display`, `quarantine_orphaned_media`, `set_default_audio_device` — cette dernière pas encore utilisable pour de vrai, voir §7) + module `Licensing/` (ECDSA local, ADR-002/009) | ✅ Oui — TDD réel |
| `tools/PincabToolbox.Repair.Demo` | ✅ Bac à sable — 7 scénarios sur une fausse install | ✅ Oui, et sur ton PC |
| `tools/PincabToolbox.LicenseTool` | ✅ Génère/signe/vérifie des licences, OFFLINE uniquement | ✅ Oui (pas de WPF) |
| `PincabToolbox.App` (net8.0-windows, WPF) | ✅ Compile chez Maxime — Écran 1 de Repair câblé (offre gratuite), Écran 2 (bouton Apply) volontairement PAS câblé | ❌ Non — édition + vérification structurelle, Maxime recompile |
| Landing (`flipsync-site/landing/index.html`) | ✅ Corrigée, validée | Non déployée — **feu vert explicite requis** |
| Documents légaux (CGU / CGV / Terms) | 🟡 Brouillons | Voir §8 |

**26 scanners** enregistrés dans `ScanEngine` (composition unique : `MainWindow.xaml.cs`) : `rom` · `bitness` · `completeness` · `compat` · `vpxversion` · `security` (blocked-file) · `dependencies` · `disk` · `legacy` · `process` (zombie PinUP Display) · `display` (setup) · `media-orphan` · `updates` · `aliasloop` · `nvram` · `altcolor` · `altsound` · `screentopology` · `junctions` · `directb2s` · `popperplaylist` · `audio-state` · `dpi-scaling` · `dmd-com-port` · `locale-separator` · `config-phantom`, + onglet Script Diff. Historique du décompte : 7 (avant 30/07) → 12 (clôture 03/08) → 13 (comparateur VPX, 05/08) → 21 (file Tier A du dégel, 06/08) → **26 (file Tier B, 06/08 — voir §7 et ADR-010)**. Les 5 derniers sont **tous `Severity.Note`** — premiers checks heuristiques du projet, jamais de `Warning`/`Critical` avant eux.

**Score de santé** : `max(0, 100 − 15×Critical − 5×Warning)` · Grades `A+ ≥100 / A ≥90 / B ≥70 / C ≥40 / F`.

**Commandes** :
```
# chez Maxime
cd ...\pincab-suite\src\PincabToolbox.App && dotnet run
build.cmd                                    # → publish\PincabToolbox.exe (self-contained)

# tests (fonctionnent aussi dans le cloud)
python3 tests/fixtures/make_fixtures.py
dotnet run --project tests/PincabToolbox.Core.Tests   -c Release   # 321 passed
dotnet run --project tests/PincabToolbox.Repair.Tests -c Release   # 105 passed

# voir Repair travailler pour de vrai, sans risque (équivalent du mode démo du Scanner)
dotnet run --project tools/PincabToolbox.Repair.Demo

# validation du Knowledge Pack (fait respecter ADR-005)
python3 knowledge/validate_pack.py knowledge/pack-2026.08.json --registry src/PincabToolbox.Repair
python3 knowledge/selftest.py
```

> ⚠️ **Restauration NuGet hors-ligne** : si `dotnet build` échoue sur « Connection reset »,
> les projets étant sans dépendance, un `NuGet.Config` avec `<packageSources><clear /></packageSources>`
> suffit à restaurer sans réseau.

---

## 7. Backlog

### Scanner — dégel du gel (05/08) et file Tier A livrée (06/08) — voir ADR-010
**Le gel « Avant le lancement forum : ne pas ajouter de nouveaux checks » ci-dessous est supersédé.**
Décision Maxime du 05/08 (« je sonne le dégel du gel ») après audit fonctionnel complet
(`docs/AUDIT-Scanner-2026-08.md`, 6 catégories non couvertes identifiées) : le gel de calendrier est
levé, **pas** la règle anti-FP — formalisé dans **ADR-010**. Nouvelle règle d'entrée pour un check :
🟢 déterministe (FP nul démontrable) → ship direct en `Warning`, plus besoin des « deux signaux
terrain » ; 🟡 heuristique → doit passer par le nouveau palier **`Severity.Note`** (score-neutre,
jamais « FIX THIS FIRST », voir ADR-010) plutôt que d'attendre indéfiniment un signal terrain.

Session autonome Sonnet 5 du 06/08 (`docs/HANDOFF-Sonnet5-scanners-2026-08.md`, exécutée seule,
Maxime absent) : **file Tier A (8 checks 🟢 déterministes) livrée intégralement** —
`VPMALIAS_LOOP` (E1) · `NVRAM_EMPTY` (H1) · `ALTCOLOR_INCOMPLETE` (B1) · `ALTSOUND_SAMPLE_MISSING`
(B2) · `DISPLAY_OFFSCREEN` (C1) · `BROKEN_JUNCTION` (G3) · `B2S_MALFORMED` (H2) ·
`POPPER_ORPHAN_PLAYLIST` (F1). Rendu App du palier `Note` (prérequis Tier B) livré en amont de la
file. **Core 144→279/279, Repair 105/105 stable, Debug ET Release à chaque étape ; aucun des 21
scanners existants modifié** (gabarit du comparateur VPX cloné à l'identique 8 fois). Détail complet,
recherches primaire-source par item et réductions de périmètre : `knowledge/FIELD-LOG.md`, entrée
« 2026-08-06 (session Sonnet 5, autonome, effort max) ».

**File Tier B (🟡 heuristique, doctrine Note) : 5/5 livrés**, même session, reprise après confirmation
implicite de Maxime (« ok je vais tester le scaner si tu la finis ») — `AUDIO_DEFAULT_SUSPECT` (D1) ·
`DPI_SCALING_NONSTANDARD` (C2) · `DMD_COM_PORT_NOT_FOUND` (B3) · `LOCALE_DECIMAL_SEPARATOR` (G1) ·
`VPINMAME_CONFIG_PHANTOM` (E2). Tous `Severity.Note`, tous détection seule, aucun scanner existant
modifié. **Core 279→321/321, Repair 105/105 stable, Debug ET Release.** Commit `14894ed`. Détail par
code, déviations loggées (G1 : `CultureInfo` plutôt que lecture registre directe) et incertitude
résiduelle (B3 : nom de clé INI du port COM non confirmé sur un vrai fichier) :
`knowledge/FIELD-LOG.md`, entrée « Item 12 ».

**A1 (Script Doctor) et A2/A3 (Font/Hardcoded-path) restent reportés — décision motivée, pas un
oubli.** A1 a besoin d'un plancher de version par script en donnée de profil
(`profiles/vpx-popper.json`) qui n'existe pas encore et que je ne peux pas deviner (jugement métier :
quelle version de `core.vbs`/`controller.vbs`/etc. compte comme périmée) ; sans lui, détecter la seule
présence produirait un Note sans delta actionnable — ne passe pas la barre valeur utilisateur. A2/A3
restent sous-spécifiés (quelle regex de police, quel seuil « chemin suspect »). Débloquables
rapidement une fois ces décisions prises par Maxime (voir FIELD-LOG, DÉCISIONS EN ATTENTE #9-10).

### Avant le lancement forum — et rien d'autre *(gel de calendrier historique, voir ci-dessus)*
Le set de checks actuel est cohérent et testé. ~~**Ne pas ajouter de nouveaux checks avant le
lancement**~~ — supersédé par le dégel du 05/08 (ADR-010) pour les checks 🟢 déterministes
uniquement ; l'esprit de la règle (un faux positif introduit juste avant le post tue la crédibilité)
reste la raison d'être de la discipline anti-FP elle-même, pas une interdiction totale de coder.

1. **Tester sur le cab réel.** Prioritaire sur tout le reste — ça remontera de vrais bugs.
2. ~~Bouton « Copier le rapport »~~ — **déjà fait** (`BtnCopyForum`, markdown forum), ainsi que les exports txt / md / BBCode / HTML / JSON.
3. ✅ **Anonymisation des rapports** — faite le 27/07. Les six sorties passent par `PathScrubber` avant d'atteindre un fichier ou le presse-papiers.
4. **Lien cliquable** vers virtual-pinball-spreadsheet.web.app dans les lignes « Mises à jour ».
5. *Si le temps le permet* : filtre par module, tri des colonnes, compteur de tables scannées, bouton « Ignorer » par ligne, mémorisation du dernier dossier scanné.

### v0.2 — alimentée par les retours forum, pas par des suppositions
`Evidence` par finding (ADR-003) · version **portable** (coût quasi nul, lève la méfiance « je ne lance pas l'installeur d'un inconnu ») · questionnaire d'intention filtrant · double libellé joueur/expert · Folder Doctor (chemins longs, permissions, **séparateur décimal FR**) · Health Timeline.

### Repair — fait en v1
Moteur complet (plan pur, préflight 5 contrôles, sauvegarde, apply avec compensation, undo, verify,
journal anonymisé), registre d'actions fermé, chargeur de Knowledge Pack JSON, et **cinq actions** :
`unblock_file`, `restore_rom_archive`, `kill_zombie_pinup_display`, `quarantine_orphaned_media`,
`set_default_audio_device` (bug connu, voir ci-dessous). Le playbook Migration 32→64 se déclenche et
s'annonce honnêtement comme partiel. **Écran 1** (offre gratuite, `RepairOfferBuilder`) câblé dans
l'App le 04/08. **Module de licence** (`Licensing/`, ECDSA P-256, 100 % local, ADR-002/009) et
`tools/PincabToolbox.LicenseTool` (génération/signature/vérification de clé, offline) codés et
testés le 04/08 — reste à Maxime de lancer `license-tool init` pour générer sa vraie paire de clés
(actuellement un placeholder qui refuse volontairement toute licence, ne peut pas planter l'App).

### Repair — reste à faire
*(§ corrigée le 06/08 — les points 5 et 6 ci-dessous, décrits comme ouverts depuis le 04/08, étaient
en fait déjà réglés et codés par les décisions (a)/(b) de Maxime le 05/08 soir ; ce fichier n'avait
jamais été remis à jour après coup. Vérifié directement dans le code, pas supposé, avant de corriger.)*
1. **Lancer le bac à sable sur ton PC Windows** — `dotnet run --project tools/PincabToolbox.Repair.Demo`.
   C'est le seul moyen d'exercer pour de vrai le « Mark of the Web » (scénario 1) et l'appel COM audio
   (scénario 7, lecture seule), que les tests ne couvrent pas sous Linux.
2. **Brancher l'UI WPF sur le chemin d'écriture** (Écran 2, le bouton Apply) — Écran 1 fait, le reste
   volontairement pas câblé sans reconfirmation explicite de Maxime (HANDOFF/ADR-010, R3 stop net).
   Le blocage technique (b) ci-dessous est levé depuis le 05/08 ; il ne reste que (a)
   `license-tool init` pas encore lancé pour de vrai **et** la reconfirmation explicite elle-même —
   deux conditions distinctes, aucune des deux réglée par du code.
3. **Trancher ADR-007** — écriture SQLite Popper, à décider quand le terrain le demandera.
4. **Calibrer les confiances** (98 / 88) sur cab réel après le lancement du Scanner.
5. ✅ **Fait le 05/08** (décision (b), TRANSMISSION) — ~~bug `SetDefaultAudioDeviceAction` /
   `IsContained`~~. Vérifié dans le code le 06/08 : `RepairEngine.IsContained` exempte bien
   `ChangeKind.AudioDeviceDefault` (`RepairEngine.cs` l.274-283, même patron que l'exemption
   `ProcessTermination`). L'action est maintenant exécutable techniquement — reste bloquée par le
   point 2 (licence + reconfirmation UI), pas par ce bug.
6. ✅ **Fait le 05/08** (décision (a), TRANSMISSION) — ~~étapes manuelles obligatoires jamais
   affichées~~. Vérifié dans le code le 06/08 : `RepairOffer.NotAutomatable` est bien câblé
   (`MainWindow.xaml.cs` l.542-551, `RepairNotAutomatableLine` dans `MainWindow.xaml`) — affiché
   avant achat, conforme ADR-006.

---

## 8. Risques ouverts

| Risque | Nature | Action |
|---|---|---|
| **Dépréciation de VBScript** par Windows | Menace l'écosystème VPX entier. Déclenchera une vague « mon pincab ne marche plus ». | Surveiller. C'est notre plus grosse opportunité de notoriété : être l'outil qui *explique* le premier. |
| **VPin Studio** ajoute du diagnostic | Seul acteur capable de fermer notre trou. Actif et riche. | Veille légère, une vérification par trimestre. |
| **Le pincab bascule hors Windows** | Notre interface est WPF. VPX tourne déjà sous Linux, mais l'écosystème autour (DOF, Popper, PUP-Packs) suit mal. | 🅿️ Parqué — `docs/PARKING-pincab-hors-windows.md`. Le moteur est déjà multiplateforme ; seuls l'UI et le *pack* sont à refaire le jour venu. Deux garde-fous à coût nul y sont notés. |
| **CGV pas à jour** du modèle de renouvellement | §4 et §10 ne prévoient pas l'abonnement de mises à jour optionnel. | À corriger **avant** la première vente, avec les 4 points déjà listés dans la note du brouillon (TVA, rétractation, médiateur, mentions légales). |
| **Faux positif sur cab réel** | Tue la conversion quel que soit le prix. | Le test terrain est le préalable à tout. |
| ~~Fuite du nom de compte dans un rapport public~~ | Le rapport est conçu pour être collé sur un forum ; les chemins absolus portent le nom Windows. | ✅ **Traité le 27/07** — `PathScrubber` (Core), 14 tests, appliqué aux six sorties. |

---

## 9. Comment travailler sur ce projet

- **Regard Product Manager sur Repair, à chaque session (consigne de Maxime, 04/08/2026).** Sans
  dériver du périmètre défini ici et dans les ADR, vérifier à chaque session si Repair répond
  toujours au besoin principal : simplifier la vie des propriétaires de pincab pour qu'ils passent
  leur temps à jouer, pas à configurer Windows. Analyser régulièrement le FIELD-LOG, les discussions
  communautaires et les concurrents. Si une amélioration augmente significativement la valeur
  commerciale de Repair, **vérifier d'abord qu'elle ne contredit aucune décision existante** (ce
  fichier, les ADR), puis la proposer avec une justification explicite : problème observé,
  fréquence, valeur utilisateur, effort, impact commercial. **Ne jamais créer une fonctionnalité
  uniquement parce qu'elle est techniquement intéressante.** Ne remplace pas la règle « deux signaux
  terrain indépendants » du FIELD-LOG §1 — chercher activement de la valeur commerciale n'excuse pas
  de coder sur un seul signal. Garde-fou tiré d'une vraie erreur (04/08, nuit) : une action Repair
  (`POPPER_NOT_REGISTERED`) a été proposée sans vérifier d'abord qu'ADR-007 l'avait déjà écartée —
  « vérifie d'abord » n'est pas une formule polie, c'est ce qui évite de recoder un risque déjà
  refusé sciemment. Copie de travail dans `TRANSMISSION.md` (lu à chaque session), ce fichier-ci
  fait foi en cas de divergence entre les deux.
- **Revue qualité pré-v1.0 (04/08/2026)** — première passe faite : 5 angles indépendants
  (architecture/ADR, code, sécurité, tests, produit/UX), 5 corrections sûres appliquées (voir §6 et
  §7 pour ce qui reste), 2 trouvailles volontairement laissées pour décision produit. Détail complet :
  FIELD-LOG, entrée « Revue qualité pré-v1.0 » du 04/08 (nuit, quater). À refaire avant la sortie
  réelle de v1.0 (Écran 2 câblé, Lemon Squeezy branché) — une seule passe ne suffit probablement pas
  pour un logiciel qui manipule les fichiers de l'utilisateur contre paiement.
- **WPF non compilable dans le cloud** → éditer prudemment, puis **vérifier structurellement** : XAML = XML valide, accolades équilibrées, chaque `Click`/`TextChanged` a sa méthode, chaque `x:Name` utilisé existe, clés Loc présentes en EN **et** FR. Maxime recompile. **Le Core, lui, se compile et se teste dans le cloud → TDD réel.**
- **Ne jamais publier ni déployer** sans feu vert explicite de Maxime.
- **Résumés en français.** Ton concis, pas de leçon de morale.
- **Quand Maxime colle les sorties de plusieurs IA** : son intention est qu'on **arbitre à sa place** — extraire les pépites, écarter la glaise (diagnostic matériel, usages « demande à une IA », valeurs inventées). Rendre « voici la pépite / voici pourquoi j'écarte », jamais un copier-coller.
- **Piège récurrent** : les IA génèrent des chiffres de marché inventés (« 40 % des utilisateurs veulent X », « 500 K€/an »). Ne jamais les reprendre, ni dans un doc, ni dans un post. Notre seul repère chiffré documenté est un plafond réaliste de **10-30 K€/an** pour la gamme complète.

---

## 10. Index documentaire

**Point d'entrée de session** : `HANDOFF.md` (à la racine du dépôt) → puis ce fichier.

| Document | Rôle | Statut |
|---|---|---|
| `docs/PROJECT-BRAIN.md` | **Ce fichier. Source de vérité.** | 🟢 Vivant |
| `docs/SUCCESS-METRICS.md` | **Tableau de bord produit — KPI (ADR-008).** | 🟢 Vivant |
| `docs/adr/ADR-001` | Carte produit et marque | 🟢 Accepté |
| `docs/adr/ADR-002` | Modèle économique, packaging, licence | 🟢 Accepté |
| `docs/adr/ADR-003` | `Evidence` par Finding | 🟢 Accepté |
| `docs/adr/ADR-004` | Périmètre légal : vérifier, jamais fournir | 🟢 Accepté |
| `docs/adr/ADR-005` | Registre d'actions fermé (sûreté du Knowledge Pack) | 🟢 Accepté |
| `docs/adr/ADR-006` | Le Scanner annonce, Repair vend le plan | 🟢 Accepté |
| `docs/adr/ADR-007` | Écriture SQLite Popper hors v1 | 🟢 Accepté |
| `docs/adr/ADR-008` | Pilotage par indicateurs (roadmap KPI-gated) | 🟢 Accepté |
| `docs/adr/ADR-009` | Plateforme de paiement : Lemon Squeezy (MoR) | 🟢 Accepté |
| `docs/adr/ADR-010` | Dégel du Scanner + doctrine `Severity.Note` | 🟢 Accepté |
| `docs/PARKING-plateformes-paiement.md` | Veille paiement/TVA — **décision reportée** | 🅿️ Parqué |
| `docs/PARKING-pincab-hors-windows.md` | Pincab Linux/macOS — **décision reportée** | 🅿️ Parqué |
| `docs/AUDIT-Scanner-2026-08.md` | Audit fonctionnel Scanner (12→21) + vision produit | 🔵 Référence (05/08) |
| `docs/HANDOFF-Sonnet5-scanners-2026-08.md` | Gabarit + file de travail Tier A/B (base d'ADR-010) | 🟢 Vivant — A1/A2/A3 en attente (voir §7) |
| `docs/DESIGN-Repair-v1.md` | Design du moteur Repair | 🟢 Vivant |
| `docs/UX-COPY-Repair.md` | Copie UX des 4 écrans critiques (FR/EN) | 🟢 Vivant |
| `knowledge/` | Format du Knowledge Pack, pack 2026.08, validateur CI | 🟢 Vivant |
| `knowledge/FIELD-LOG.md` | Journal de terrain (retours → KPI / pack) | 🟢 Vivant |
| `docs/ARCHITECTURE-KnowledgeEngine.md` | Architecture du moteur (pipeline, confiances, Knowledge Pack) | 🟢 Vivant |
| `docs/UNIVERS-FlipSync.md` | **Recherche de marché** sur l'univers du flipper. Ne porte plus aucune décision. | 🔵 Référence |
| `docs/architecture-repair-phase2.md` | Notes v1 sur Repair. À remplacer par le vrai design. | 🟠 Notes |
| `docs/ARBITRAGE-brainstorming-multiIA-2026-07.md` | Tri du brainstorming multi-IA du 25/07 | 🔵 Archive de session |
| `docs/_archive/` | Documents morts, conservés pour l'histoire. **Ne pas s'en servir.** | ⚫ Mort |

**Tous les documents vivent désormais dans le dépôt** (`pincab-suite/`), donc versionnés avec le code. Il n'y a plus de « docs stratégie » d'un côté et « docs projet » de l'autre.
