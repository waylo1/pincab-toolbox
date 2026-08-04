# ADR-004 — Périmètre légal : on vérifie, on ne fournit jamais

**Statut** : Accepté · **Date** : 25/07/2026 · **Décideur** : Maxime Chauvin

---

## Contexte

Ces règles existaient, éparpillées dans trois documents dont deux sont aujourd'hui archivés. Elles sont les plus structurantes du projet et **les plus faciles à oublier sous pression commerciale** — le brainstorming multi-IA du 25/07 recommandait le téléchargement automatique de ROMs **plus de quinze fois**, en le présentant comme la fonctionnalité la plus attendue de la communauté.

Elle l'est probablement. Elle reste interdite.

## Décision

Cinq règles, opposables à toute proposition de fonctionnalité, quelle que soit sa valeur commerciale.

### 1. On vérifie et on prépare — on ne fournit jamais
Aucun téléchargement automatique de **tables, ROMs, médias, backglass, colorisations, PUP-Packs**. L'outil peut détecter qu'un fichier manque, dire lequel, et ouvrir le dossier où le placer. Il ne va jamais le chercher.
**Seule exception** : les dépendances **open source** (Freezy, VLC, B2S Server, DOF), dont la licence autorise la redistribution.

### 2. Lecture seule par défaut
Le Scanner ne modifie rien, ne télécharge rien, n'envoie rien. Zéro télémétrie. C'est *pour ça* qu'on lui fera confiance au point d'en faire un réflexe communautaire.

### 3. Pas de scraping des forums communautaires
VPForums, VPUniverse, Pincab Passion sont nos **canaux de distribution**, pas nos sources de données. Les scraper reviendrait à attaquer les gens dont on a besoin. Les sources de données autorisées sont celles publiées pour l'être (Virtual Pinball Spreadsheet, dépôts officiels).

### 4. Marques tierces : usage descriptif uniquement
« Compatible avec Visual Pinball X / PinUP Popper » est acceptable. Un nom de produit contenant une marque tierce ne l'est pas.

### 5. Toute écriture est réversible
Registre, base Popper, fichiers de config : sauvegarde préalable + restauration en un clic, systématiquement. Une action non réversible n'est jamais automatique, quelle que soit la confiance qu'on a dedans.

## Conséquences

- Le « gestionnaire de ROMs avec téléchargement automatique » — fonctionnalité la plus demandée dans toutes les analyses — **ne sera jamais construit**. On construit à la place le meilleur outil qui dit *exactement* quel fichier manque, où le mettre, et vérifie qu'il est bon une fois placé.
- On accepte de paraître moins complet qu'un concurrent hypothétique qui franchirait la ligne. C'est un choix : la confiance est le seul actif qui ne se rachète pas, et une mise en demeure suffit à tuer une micro-entreprise solo.
- **Cette ADR est le premier filtre à appliquer** à toute idée de fonctionnalité, avant même la question du produit auquel elle appartient.
