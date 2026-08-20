# Brief pour GPT — état réel de Pincab Toolbox / Repair (20/08/2026)

Contexte : micro-entreprise solo (MC Automation, Maxime), app payante (Repair), un seul dev, un cab
réel de test. Tes idées du 20/08 sont bonnes et déjà globalement alignées avec l'architecture
existante (zéro téléchargement de contenu tiers, diagnostic + correction locale). Ce document sert à
te donner l'état réel du code pour que tu ne repropose pas ce qui existe déjà, et pour que tu saches
sur quoi concentrer les prochaines idées.

## 1. Ce qui existe déjà (ne pas reproposer)

**Score de santé** — existe depuis longtemps : chaque scan a un `Score` (0-100) et un `Grade`
(A à F), calculés à partir de la sévérité des findings. Ce que tu appelles "Pincab Health Score"
est déjà en prod. Ce qui n'existe PAS : un sous-score par composant (VPX / VPinMAME / B2S / FlexDMD
séparés) — aujourd'hui c'est un seul score global.

**Chaîne causale ("cause probable")** — existe déjà, partiellement, depuis le 11/08 : un onglet
"Causes racines" affiche des cartes avec une chaîne de cases (VPX → VPinMAME → B2S → ...),
construites à partir de "scénarios" définis dans le Knowledge Pack (des règles qui disent "si ces
findings sont présents ensemble, c'est probablement CE problème racine"). Ce qui n'existe PAS :
un pourcentage de probabilité affiché ("92%") — c'est une détection par règles (si/alors), pas un
calcul de probabilité.

**Sauvegarde + Annuler + journal** — existe depuis longtemps (LOT H, 10-13/08) : avant CHAQUE
réparation appliquée, une sauvegarde est faite, un journal (fichier JSONL sur disque) enregistre
chaque changement avec un "avant" et un "après" en texte, et un Undo peut annuler après coup — SAUF
quand l'action elle-même n'est pas réversible par nature (ex. lancer l'outil d'enregistrement d'un
composant COM : rien à restaurer, mais la sauvegarde s'exécute quand même par principe). Rien de
tout ça n'est appliqué sans que l'utilisateur coche explicitement la case correspondante.

**Historique** — existe depuis le 13/08 : deux listes "Réparé" / "Annulé", avec date, nombre de
correctifs, et depuis aujourd'hui (20/08) le détail par changement (quoi a été fait, pas juste un
nom de fichier).

**Réparations déjà codées et actives (7 aujourd'hui)** :
- Débloquer une DLL bloquée par Windows (Zone.Identifier)
- Réarchiver une ROM décompressée par erreur
- Tuer un process PinUpDisplay.exe zombie
- Mettre en quarantaine un fichier média orphelin (jamais supprimé)
- Relancer l'outil d'enregistrement officiel d'un composant COM (VPinMAME.Controller,
  B2S.Server, FlexDMD.FlexDMD) — activée aujourd'hui pour 3 cas : VPinMAME pas enregistré du tout,
  composant pas enregistré, composant enregistré dans le mauvais bitness (32/64). **On ne
  réenregistre JAMAIS via le registre Windows directement — toujours l'outil officiel du composant
  lui-même, sur une liste blanche figée dans le code.**

**~35 contrôles de Scanner déjà actifs**, dont : ROM manquante/décompressée, incohérences de
bitness (VPX/VPinMAME/B2S/FlexDMD, y compris "chain bitness" = un plugin absent dans le bitness que
la table utilise), santé de l'enregistrement COM (3 dimensions), fichiers de config B2S manquants,
XML de backglass malformé, NVRAM illisible, médias orphelins, playlists Popper avec des références
mortes, position du DMD hors écran, séparateur décimal du système (casse la physique de certaines
tables), chemins codés en dur dans les scripts de table, espace disque, version VPX vs exigence de
la table, vérification manuelle des mises à jour (GitHub).

## 2. Contraintes d'architecture à connaître avant de proposer

- **Jamais de téléchargement de contenu tiers.** Confirmé, comme tu le dis toi-même dans ta
  dernière réponse. Filet de sécurité déjà décidé, pas à re-débattre.
- **Aucune réparation n'est "automatique" sans un score de confiance ≥95 ET réversible.** En
  dessous de 95 : confirmation explicite obligatoire. En dessous de 70 : pas de réparation du tout,
  juste une procédure manuelle affichée. Une action non réversible par nature (comme relancer un
  installeur externe) n'est JAMAIS automatique, quel que soit le score.
- **Licence obligatoire pour appliquer**, mais le plan (ce qui serait fait) reste visible gratuitement
  — jamais de "on vous dit qu'on peut réparer" caché derrière un paywall sans preuve.
- **Modèle commercial** : achat unique (pas d'abonnement), 3,99 (EUR/USD/GBP sans distinction),
  licence perpétuelle, mises à jour incluses sans limite de durée, encaissement Stripe direct (pas
  de Merchant of Record). Donc pas d'idée de type "plan premium mensuel" ou "crédits à racheter".
- **Toute action qui écrit sur le disque de l'utilisateur passe par le même moteur** (backup
  d'abord, écriture, vérification après coup via un rescan) — une nouvelle réparation doit s'
  intégrer là-dedans, pas être un système parallèle.
- **Trois langues obligatoires (EN/FR/ES)**, EN par défaut. Toute idée avec du texte affiché doit
  prévoir les 3.

## 3. Vraies pépites parmi tes idées — pas encore construites

- **Mode expert / mode débutant** — n'existe pas du tout aujourd'hui, un seul niveau de détail.
  Idée solide, s'intègre bien dans l'architecture i18n existante.
- **Profil machine local + comparaison "ça marchait hier"** — n'existe pas du tout. C'est
  probablement ta meilleure idée du lot : aucune de ces briques n'existe (pas de snapshot de
  config, pas de comparaison avant/après un changement système). Total legal (rien que du texte
  généré localement, pas de fichier protégé).
- **Détection "installations sales" (plusieurs dossiers VPX)** — n'existe pas. Le scanner sait
  scanner un dossier ou un disque entier, mais ne dit jamais "j'ai trouvé 3 installations, voici
  laquelle est active".
- **Assistant migration PC** — n'existe pas.
- **DOF (DirectOutput Framework)** — quasiment aucun contrôle dessus aujourd'hui, juste une mention
  annexe dans un scanner de config B2S. Zone vraiment vide.
- **Vérifications Windows générales** (VC++ Redistributable, .NET Runtime, pilotes GPU) — pas
  vérifiées à ma connaissance, mais je n'ai pas relu 100% du code cette session, à confirmer avec
  Maxime avant de considérer ça comme un vrai trou.
- **Périphérique audio par défaut** — l'action existe et est testée (`SetDefaultAudioDeviceAction`)
  mais n'est déclenchée par AUCUN scanner (aucun moyen de détecter statiquement "le device va se
  réinitialiser au démarrage"). Décision en attente depuis le 03/08 : probablement un bouton dédié
  plutôt qu'un finding.

## 4. Ce qu'on te demande

Vu ce qui précède, concentre le prochain tour d'idées sur : (1) la comparaison de config
locale "avant/après" (le plus gros trou, le plus défendable commercialement), (2) le mode
expert/débutant, (3) parmi les contrôles Scanner qui existent déjà mais sont encore 100% manuels
en Repair, lesquels seraient de bons candidats à une correction automatique SANS toucher au script
d'une table (ça, on l'évite : réécrire un fichier utilisateur non re-téléchargeable est le seul vrai
risque qu'on veuille éviter).
