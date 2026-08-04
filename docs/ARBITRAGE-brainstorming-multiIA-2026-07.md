# Arbitrage — brainstorming multi-IA (GPT / Mistral / Kimi)

**Date : 25/07/2026** · Entrée : `idées brainstorming avec dautre ia.txt` (5 581 lignes)
**Règle appliquée** : décisions figées du 22/07 non rediscutées, sauf risque majeur (§4).

> Verdict global : **~5 % de pépite, ~95 % de remplissage.** Le document contient 4 idées qui méritent
> d'entrer dans le produit, 1 correction technique importante, et beaucoup de chiffres inventés
> qu'il ne faut jamais citer. Détail ci-dessous.

---

## 1. Les pépites — à garder

Classées par rapport valeur / effort. Chacune répond au filtre : *quelle friction réelle, chez qui, pourquoi maintenant, quel produit.*

### 1.1 — `Evidence` sur chaque Finding ⭐ meilleure idée du lot
**Produit : Scanner** (impacte `Core/Models/Finding.cs` → ADR).

Chaque finding porte la liste des preuves qui l'ont produit :
```
BITNESS_MISMATCH_VPM
Evidence : ✓ PE header VPinMAME.dll (x86)  ✓ PE header VPinballX.exe (x64)  ✓ registre HKCU\...\VPinMAME
```
- **Friction réelle** : « pourquoi je devrais te croire ? » — c'est *la* question du premier post forum.
- **Qui** : tout le monde, mais surtout le sceptique qui décide si l'outil devient un réflexe communautaire.
- **Pourquoi prioritaire** : la priorité absolue est *zéro faux positif*. Evidence ne supprime pas les faux positifs, elle les rend **auditables** — l'utilisateur voit sur quoi on s'est appuyé et peut nous corriger. C'est le meilleur accélérateur de confiance pour un coût quasi nul (l'info est déjà en mémoire au moment du check, on ne fait que la conserver).
- **Effort** : faible côté Core (champ `Evidence[]` sur `Finding`, rempli par chaque scanner), moyen côté UI (bloc repliable).

### 1.2 — Parcours guidé « Migration 32→64 » ⭐ meilleur candidat Repair v1
**Produit : Repair.**

Le conflit 32/64-bit est la friction n°1 du document (et de nos propres constats). On a **déjà** le scénario `Migration 32→64 incomplète` à 90 % de confiance dans `Scenarios.cs`. Il manque uniquement le parcours : *checklist ordonnée + vérification après chaque étape*.

- **Friction réelle** : un utilisateur qui migre casse son install et ne sait pas **où** dans la chaîne.
- **Pourquoi prioritaire** : c'est le seul cas où on a déjà (a) la détection, (b) le diagnostic corrélé, (c) une correction majoritairement *réversible et locale* (relier/copier un binaire 64-bit déjà présent — `BitnessFixer` est déjà prévu dans l'archi Repair).
- **Attention** : rester dans le périmètre `BitnessFixer` du doc archi (*détecter un binaire 64-bit déjà présent chez l'utilisateur*). **Pas de téléchargement** (§2.1).

### 1.3 — Questionnaire d'intention filtrant
**Produit : Scanner.**

L'utilisateur décrit son symptôme (« mon DMD est noir », « ça plante au lancement ») → le rapport se **pré-filtre** sur les catégories concernées. Simple mapping mot-clé → `Finding.Category`, aucune IA, 100 % local.

- **Friction réelle** : un rapport de 40 lignes sur un cab à 200 tables est illisible pour un débutant.
- **Garde-fou** : c'est un **filtre de rapport**, pas un moteur de réponse. Si aucun finding ne correspond, on dit « rien détecté sur cette piste » — on ne bluffe pas. Sinon on recrée la déception des forums.
- Cohérent avec le « Bouton Ignorer » et le « Filtre par module » déjà listés dans `_archive/ameliorations-scanner-avant-lancement.md` — **même brique**, à construire une fois.

### 1.4 — Double libellé « joueur » / « expert » par code
**Produit : Scanner.**

`ROM_MISSING` affiche par défaut « Cette table ne pourra pas démarrer : sa ROM est absente », et `Afficher les détails techniques ▼` révèle le code, le fichier attendu, le chemin.

- On a **déjà** la moitié (Impact/Cause FR dans `Knowledge.cs`). Il manque une phrase « joueur » par code et l'inversion de la hiérarchie d'affichage.
- **Contrainte connue à respecter** : `FrFindings` accepte `string.Format` (placeholders OK), `FrFixHints` **non** — le libellé joueur doit suivre la même règle que le dict dans lequel il atterrit.

### 1.5 — Trois checks statiques bon marché (Folder Doctor)
**Produit : Scanner — mais après le lancement, pas avant.**

Chemins trop longs / caractères interdits · permissions d'écriture sur les dossiers de config · **séparateur décimal de la locale** (`,` vs `.`). Le troisième est spécifiquement un piège français — or notre beachhead est Pincab Passion. Tous lisibles en statique, zéro risque.

> `_archive/ameliorations-scanner-avant-lancement.md` dit explicitement : **ne pas ajouter de checks avant le lancement.** Ces trois-là vont en v0.2, alimentés par les retours forum.

### 1.6 — À garder en tête (déjà au backlog, le doc les confirme)
Health Timeline · Digital Twin · Audio/Display/Script/Duplicate Doctor · version **portable** (coût ~nul, l'exe est déjà self-contained, et ça lève la méfiance « je ne lance pas l'installeur d'un inconnu »).

### 1.7 — Version dégradée d'une idée à moitié bonne : la « Health Signature »
L'idée d'origine (« ta signature correspond à 132 autres utilisateurs ») exige un serveur et de la télémétrie → **contraire** au discours zéro télémétrie / local-first.
**Version retenue** : une empreinte courte calculée **localement** et affichée dans le rapport (`Signature : 4B9A-72F1`). Utilité : sur le forum, deux personnes comparent leurs signatures pour savoir si elles ont la même configuration. Zéro backend, coût nul, et ça fait circuler le nom de l'outil dans les threads. À considérer seulement si le rapport devient un objet d'échange communautaire.

---

## 2. La glaise — écarté, et pourquoi

### 2.1 — Téléchargement automatique de ROMs / backglasses / DLL / médias
Revient **plus de quinze fois** dans le document, présenté comme la fonctionnalité n°1 attendue.
**Non, définitivement.** Règle absolue du projet : on **vérifie et prépare**, on ne **fournit jamais**. C'est un risque licences frontal, et scraper VPUniverse/VPForums reviendrait à attaquer nos propres canaux de distribution. Seule exception déjà actée : les dépendances **open source** (Freezy, VLC, B2S, DOF).

### 2.2 — Dashboard multi-PC, gestion de parc, abonnement 20–100 €/mois
Marché fantôme. Le document postule des salles d'arcade avec 10–20 pincabs comme si c'était un segment ; en pratique il est marginal en France. Et techniquement : agent réseau + backend + notifications push = l'inverse exact de **local-first, zéro serveur, zéro coût fixe**, qui est ce qui rend le projet tenable en solo.

### 2.3 — Tous les chiffres du document
« 40 % des utilisateurs veulent X », « 50-200 K€/an sur VPX », « 1-2 M€/an toutes plateformes », « basé sur 100+ threads ».
**Aucune source, aucune méthode.** Notre propre estimation documentée (`SYNTHESE`) est un plafond réaliste de **10-30 K€/an** pour trois produits. Écart d'un facteur 10 à 100. À ne jamais citer, ni dans un doc, ni dans un post, ni pour se motiver.

### 2.4 — « La voie est 100 % libre, aucun concurrent »
Conclusion non tenable, et c'est le défaut d'analyse le plus grave du document :
- Les recherches ont majoritairement ramené des **scanners OBD2 automobiles** et du **Citrix NetScaler VPX** (autre produit, même acronyme). La conclusion est bâtie sur du bruit.
- Le document **cite lui-même `vpin-studio` dans ses résultats** (deux fois) puis l'oublie dans l'analyse concurrentielle. C'est pourtant l'acteur le plus sérieux du segment : actif, riche, gestion de tables/joueurs/compétitions.
- Il rate aussi ClrVpin et Baller Installer, déjà identifiés dans nos docs.

**Notre formulation reste la bonne** (`UNIVERS-FlipSync` §2) : des outils existent pour **gérer et installer** ; **aucun ne diagnostique**. C'est un trou précis, pas un désert.

### 2.5 — Expansion MAME / RetroArch / LaunchBox / serveurs Minecraft / modding Skyrim / Home Assistant / imprimantes 3D
Environ **1 500 lignes** du document. Décision figée n°1 : on reste sur le flipper jusqu'au premier euro encaissé. Rien ici ne constitue un risque majeur justifiant de rouvrir le sujet. **Classé sans suite** — le fichier reste comme archive d'options futures.

### 2.6 — Assistant Graphique / Game Optimizer / profils par table
Déjà écarté correctement dans le document lui-même. **Je confirme, et j'ajoute une correction technique qui n'y est pas** (§3).

### 2.7 — Le reste, en vrac
Marketplace de profils · crowdsourcing · forum intégré · notation de tables · achievements · mode kiosk · VR · IA de recommandation de tables · sauvegarde cloud managée · compression automatique des vidéos PUP-Pack (réencodage = casse + hors lecture seule) · « mode Sans Souci » qui désactiverait les menus VPX (impossible).
Point commun : **effet de réseau, backend, ou modération** — trois choses qu'un solo ne tient pas, et qui contredisent le filtre local-first.

---

## 3. Correction technique importante

Le document répète comme une évidence : *« on reste sur les fichiers texte (`VPinballX.ini`), on évite les `.vpx` binaires, donc on peut corriger le FOV et le Layback sans risque. »*

**C'est faux dans le cas qui l'intéresse.** Le FOV / Layback / inclinaison sont des réglages de **point de vue par table**, stockés côté table — pas dans le seul `VPinballX.ini` global. Corriger « le plateau est étiré » sur *une* table ne se fait donc pas en éditant un `.ini` global.

Conséquence : la promesse « Repair règle ton FOV » telle que décrite dans le document est **techniquement non tenable en l'état**. Si on veut un jour toucher au POV, il faut d'abord un travail de vérification sérieux sur le stockage réel selon la version de VPX — c'est un sujet à part entière, pas un bonus de Repair v1.

**À faire** : traiter ce point comme une hypothèse à vérifier, jamais comme un acquis. Et ne rien promettre publiquement sur les réglages graphiques avant vérification sur cab réel.

---

## 4. Risques majeurs / incohérences repérés (hors document)

Ce sont les seuls points où je remets en question l'existant, comme convenu.

| # | Constat | Impact | Proposition |
|---|---|---|---|
| R1 | **`_archive/strategie-prix.md` contredit la décision figée n°4.** Le doc prix dit « one-shot 9-19 €, **pas d'abonnement**, mises à jour incluses ». La décision du 22/07 dit « privilégier le **récurrent** : usage perpétuel + MAJ annuelles payantes ». | Un futur toi lira le mauvais doc et fixera le mauvais prix. | **ADR-001** puis mise à jour de `_archive/strategie-prix.md` (la décision du 22/07 gagne). |
| R2 | **« FlipSync » désigne deux choses.** `UNIVERS-FlipSync.md` (22/07) = marque-parapluie. `SYNTHESE_Pincab-Suite` (14/07) = nom du **produit** de backup/migration. | Confusion de marque garantie au moment de la landing et des posts forum. | Marquer `SYNTHESE` comme **superseded**, ou lui ajouter un bandeau en tête. |
| R3 | **Deux cartes produits coexistent** : 3 produits (Toolbox / FlipSync / Tuning Suite) dans `SYNTHESE`, vs les 5 lignes du /goal (Scanner / Repair / Play Optimizer / Creator Suite / Table Companion), vs la matrice à 10 lignes d'`UNIVERS-FlipSync` §7. | Impossible de répondre à « ça appartient à quel produit ? » de façon stable. | **Une seule carte produit canonique** à figer — 30 min de travail, gros gain de clarté. |
| R4 | **VPin Studio n'est surveillé nulle part.** C'est le seul acteur qui pourrait ajouter du diagnostic à son outil de gestion et fermer notre trou. | Risque concurrentiel réel, contrairement à ce que dit le document. | Le mettre dans une veille légère (une vérif par trimestre) à la place de « surveiller vpxtool », qui est un CLI sans intention produit. |
| R5 | **Dépréciation de VBScript** — le document ne la mentionne **jamais**, alors que c'est le seul vrai signal macro identifié (`UNIVERS-FlipSync` §148). | Confirme que notre lecture est plus fine que celle des 4 IA réunies. Aucune action, juste : garder ce signal en tête. | — |

---

## 5. Capitalisation

**ADR proposés**
- **ADR-001 — Modèle de revenu** : usage perpétuel + mises à jour annuelles payantes, scanner gratuit à vie. Tranche R1.
- **ADR-002 — `Evidence` et `Confidence` dans le pipeline** : chaque `Finding` porte ses preuves ; chaque règle Repair porte une confiance. `AutoFixable` **reste** la frontière gratuit/payant (le booléen est une décision business, la confiance est une donnée technique — ne pas les fusionner).
- **ADR-003 — Règle « on vérifie, on ne fournit jamais »** : formaliser en ADR ce qui est aujourd'hui éparpillé dans trois docs. C'est la règle la plus structurante du projet et la plus facile à oublier sous pression.

**Knowledge Engine**
- Les frictions listées §1 du document (32/64-bit, ROM, dmddevice64, backglass, Popper) recoupent nos codes existants → **rien à ajouter**, c'est une confirmation.
- Les frictions non couvertes (permissions, chemins, locale, pilotes GPU, .NET/DirectX) → **candidats à de futurs codes**, à alimenter avec l'**ordre qualitatif** seulement. Ne jamais inscrire les pourcentages du document dans la base : ils sont inventés, et une base de connaissance qui contient une valeur fausse perd toute sa valeur.

**Impact sur les autres produits de la suite**
- `Evidence` et `Confidence` sont des briques du **moteur**, pas du Scanner. Elles se transportent telles quelles vers Repair, vers le module Colorisation & Son, et vers l'assistant de diagnostic physique (où la « preuve » devient « le symptôme que tu m'as décrit »). C'est pour ça qu'elles méritent un ADR plutôt qu'un ticket.

---

## 6. Ce que je ferais ensuite

Rien de ce document ne change la prochaine étape prévue : **concevoir l'architecture Repair**
(`IRepairRule` / `RepairPlan` / `RepairJournal`, flux backup→dry-run→apply→undo).

Deux ajustements seulement, issus de cet arbitrage :
1. Prévoir dès la conception le champ **`Confidence`** sur la règle Repair (ADR-002) — c'est bien plus coûteux à ajouter après.
2. Prendre le **parcours Migration 32→64** (§1.2) comme cas d'usage de référence pour valider le design, plutôt qu'un fixer isolé. S'il tient sur un parcours multi-étapes réversible, il tiendra sur les cas simples.
