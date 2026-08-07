Tu reprends le projet Pincab Toolbox / FlipSync (MC Automation, Maxime Chauvin) — outil de diagnostic
et réparation pour cabinets de flipper virtuel, C#/.NET 8, micro-entreprise solo. Tu es Sonnet 5, en
EFFORT MAXIMUM, et tu avances SEUL : Maxime n'interviendra pas de la session.

CONTEXTE (Kontext)
Dépôt local : dossier "Pincab suite" sur la machine de Maxime (à connecter), sous-dossier
pincab-toolbox-v0.1.1-alpha-src/pincab-suite. GitHub waylo1/pincab-toolbox est à jour (poussé le 06/08).
Lis dans l'ordre, sans coder avant : TRANSMISSION.md (bloc « MAJ 06/08 (1) » puis « 05/08 (5) » en tête),
puis docs/HANDOFF-Sonnet5-scanners-2026-08.md (TA FEUILLE DE ROUTE EXACTE — lis-la en entier), puis
docs/AUDIT-Scanner-2026-08.md (le pourquoi), puis docs/PROJECT-BRAIN.md (§2, §3, §7) et les ADR citées.
La session précédente (Opus) a livré et testé vert : le comparateur de version VPX
(VPX_VERSION_OUTDATED) et le nouveau palier de sévérité Severity.Note (partie Core). Baseline à
reconfirmer AVANT de toucher quoi que ce soit : Core 144/144, Repair 105/105, Debug ET Release.

MODE — AUTONOMIE TOTALE (non négociable)
Applique à la lettre la « DIRECTIVE D'AUTONOMIE » en tête du handoff. Zéro question à Maxime. Ne t'arrête
jamais pour attendre : si un item est bloqué (règle R3 du handoff), logge-le dans FIELD-LOG sous
« DÉCISIONS EN ATTENTE » et passe au suivant. Toutes les décisions sont déjà prises (règles R1-R6 +
Doctrine Note). La session ne finit jamais « en attente » — elle finit par un récap + une commande git.

TÂCHE (Narrow scope) — exécute la file du handoff dans l'ordre
1. Knowledge.cs + Loc.cs (FR/EN) pour VPX_VERSION_OUTDATED (règle R1 — rétroactif).
2. Rendu App du palier Severity.Note (PRÉREQUIS avant tout scanner Tier B) : libellé FR « À noter » / EN
   « Note », couleur/icône distincte d'Info, présence dans les 6 exports (écran, txt, md, BBCode, HTML,
   JSON) et le score/wording (jamais « FIX THIS FIRST »). Parse Roslyn de contrôle.
3. File Tier A (scanners 🟢 déterministes → sévérité Warning, activés dans MainWindow) : E1 VPMAlias loop,
   H1 NVRAM 0-octet, B1 AltColor pair, B2 AltSound linter, C1 Screen-Topology (scope déterministe strict),
   G3 Junctions, H2 directb2s XML, F1 PUPDatabase orphelin.
4. Puis File Tier B (scanners 🟡 heuristiques → sévérité Note via la Doctrine Note) : D1 audio, C2 DPI,
   A1 core.vbs *détection*, B3 COM-probe, G1 séparateur FR, puis E2/A2/A3.
Chaque scanner suit LE GABARIT du comparateur (classe pure dans Core/Services + IScanner dans
Core/Scanning à I/O injectée + fichier de tests neuf + entrées Knowledge/Loc + une vraie ligne .Add).
Va aussi loin que le budget le permet ; chaque item est indépendamment expédiable.

CRITÈRES DE SUCCÈS (Easy to verify)
- Après CHAQUE item : Core (128+, en croissance) ET Repair 105/105, Debug ET Release, tout vert. SDK :
  apt-get install -y dotnet-sdk-8.0 ; recette de build/test reproductible complète au §2 du handoff.
- Fichiers NEUFS uniquement + la ligne .Add (+ Knowledge.cs/Loc.cs additifs) ; AUCUN scanner existant
  modifié.
- Chaque item écrit sur le disque de Maxime (re-stage frais juste avant commit) + une entrée FIELD-LOG.
- Clôture : PROJECT-BRAIN §7 mis à jour (dégel officiel), TRANSMISSION.md mis à jour (nouveau bloc), et
  la commande git prête pour Maxime (le proxy bloque le push depuis le sandbox — c'est lui qui pousse).

CONTRAINTES EXPLICITES
- Décisions ACTÉES par Maxime (06/08), à ne pas rediscuter : dégel officiel ; core.vbs = dépendance OSS
  providable (mais le FIX Repair core.vbs demande une passe de DESIGN → NE PAS le coder à l'aveugle,
  seule la détection en Note est permise) ; Table Companion = 2ᵉ produit ; carve-out ADR auto-update.
- STOPS NETS (logge, ne code pas) : Écran 2 / bouton Apply de Repair ; fix B2S_ORPHAN ; scanner F3
  quote-safety ; fix Repair core.vbs (design d'abord) ; canal binaire auto-update (signature de code
  d'abord).
- Doctrine Note obligatoire pour tout 🟡 : émettre le FAIT en Note, jamais le jugement en Warning ;
  escalade Warning seulement sur une sous-condition déterministe ; résumer les checks par-table en UN
  finding compté ; biais silence sur tout parse/lecture raté.
- ADR-004 : jamais télécharger/fournir tables, ROMs, médias, backglass, PUP-Packs. Lecture seule.
- App WPF non compilable dans le sandbox → revue structurelle + parse Roslyn des fichiers App édités ;
  Maxime recompile. Zéro dépendance externe (BCL uniquement).

FORMAT (Logical structure)
Réponds en français, concis, sans réexpliquer ce qui est déjà dans les documents. Logge chaque décision
dans FIELD-LOG au fil de l'eau. Tiens TRANSMISSION.md à jour. Ne finis jamais « en attente » — termine
par un récap de ce qui est livré + prêt-à-pousser (commande git).
