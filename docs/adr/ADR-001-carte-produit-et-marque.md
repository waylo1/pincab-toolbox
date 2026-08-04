# ADR-001 — Carte produit canonique et architecture de marque

**Statut** : Accepté · **Date** : 25/07/2026 · **Décideur** : Maxime Chauvin

---

## Contexte

Trois cartes produit incompatibles coexistaient dans la documentation :

- `SYNTHESE_Pincab-Suite` (18/07) : 3 produits — Pincab Toolbox / **FlipSync** / Tuning Suite.
- `UNIVERS-FlipSync` (22/07) : une matrice de 10 lignes produit, et **FlipSync défini comme marque-parapluie**.
- Le /goal de session : 5 lignes — Scanner / Repair / Play Optimizer / Creator Suite / Table Companion.

Conséquence concrète : impossible de répondre de façon stable à « cette idée appartient à quel produit ? », qui est pourtant le filtre de priorisation utilisé à chaque session. Et « FlipSync » désignait à la fois la marque et un produit de sauvegarde.

## Décision

**1. FlipSync est la marque-parapluie de MC Automation. Ce n'est le nom d'aucun produit.** L'usage « FlipSync = produit de sauvegarde/migration » est abandonné.

**2. La carte produit compte cinq lignes, définitivement :**

| # | Ligne | Périmètre | État |
|---|---|---|---|
| 1 | Scanner | Diagnostic en lecture seule | Actif |
| 2 | Repair | Tout ce qui écrit sur l'install, sous contrat de sûreté — **y compris sauvegarde & migration** | Prochaine étape |
| 3 | Play Optimizer | Ce qui tourne pendant le jeu, ou règle le matériel | Parking |
| 4 | Table Companion | La bibliothèque de tables, par table — **y compris colorisation & son** | Parking |
| 5 | Creator Suite | Outils pour ceux qui produisent des tables | Parking |

**3. La frontière est déterminée par une seule question : sur quoi l'outil agit-il ?**
Il lit → Scanner. Il écrit sur l'état statique → Repair. Il tourne pendant le jeu ou touche au matériel → Play Optimizer. Il agit par table sur du contenu téléchargé → Table Companion. Il sert un producteur de tables → Creator Suite.

**4. La sauvegarde/migration entre dans Repair**, pas en 6ᵉ ligne. C'est exactement le même moteur : backup → dry-run → apply → undo → journal. Une migration n'est qu'un remap de chemins avec les mêmes garanties.

**5. Le flipper physique n'est pas sur la carte.** C'est un repackaging futur du même moteur pour un autre public, hors périmètre jusqu'au premier euro encaissé — cohérent avec les décisions du 22/07 (ordre des produits : pincab → pont → physique).

## Alternatives écartées

- **3 lignes seulement** : plus court, mais les idées sans case seraient rediscutées à chaque session — exactement le problème qu'on cherche à supprimer.
- **2 lignes (Scanner + Repair) et le reste plus tard** : le plus honnête vis-à-vis de l'état réel du code, mais on perd le filtre de priorisation.
- **Sauvegarde/migration en 6ᵉ ligne** : dépasse le plafond de cinq et duplique tout le moteur de sûreté.

## Conséquences

**Positives** — Toute idée future a une case, ou n'en a pas et est donc hors périmètre : la question « on le fait ? » se répond en dix secondes. Une seule marque, plus d'ambiguïté au moment de la landing et des posts forum.

**À assumer** — Le *Focus Guardian*, présenté comme module vedette dans l'ancienne synthèse, est un résident actif pendant le jeu : il tombe dans **Play Optimizer**, pas dans Repair v1. Repair v1 est donc plus léger, mais perd son argument le plus vendeur. Si le lancement payant en a besoin, la réponse propre est de sortir Play Optimizer *avant* Repair — pas de casser la frontière. À trancher au moment du design Repair.

**Documents impactés** — `SYNTHESE_Pincab-Suite` archivé (les deux copies). `UNIVERS-FlipSync` rétrogradé en document de recherche de marché, ne porte plus de décision.
