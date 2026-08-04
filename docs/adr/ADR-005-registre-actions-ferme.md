# ADR-005 — Le Knowledge Pack ne peut invoquer que des actions d'un registre fermé

**Statut** : Accepté · **Date** : 25/07/2026 · **Décideur** : Maxime Chauvin (proposé par l'analyse de design Repair)

---

## Contexte

Deux décisions déjà prises entrent en collision dès qu'on écrit Repair :

- `ARCHITECTURE-KnowledgeEngine.md` §4 — **la connaissance est de la donnée**, mise à jour indépendamment de l'application, par-dessus le réseau, comme les définitions d'un antivirus.
- `ARCHITECTURE-KnowledgeEngine.md` §8 — le Knowledge Pack a vocation à devenir **ouvert et contribuable par la communauté**, parce que c'est ce qui fait grandir le moat sans nous coûter des commits.

Tant que le pack ne décrivait que des textes, des causes et des gravités, aucun problème. Repair change la nature du pack : il va y décrire des **opérations d'écriture sur le disque de l'utilisateur**.

Sans garde-fou, la conséquence est directe : **un fichier de données téléchargé, potentiellement rédigé par un tiers, devient un vecteur d'exécution.** Un pack malveillant ou simplement mal relu pourrait décrire une suppression, une écriture registre hors périmètre, ou un déplacement destructeur.

Les notes de phase 2 ne voyaient pas le problème : elles proposaient une interface unique `IFixer` mélangeant la connaissance et l'exécution, ce qui rendait la question invisible.

## Décision

**Séparer strictement ce que le pack décrit de ce que le code sait faire.**

| | Nature | Qui l'écrit | Pouvoir |
|---|---|---|---|
| `RepairRule` | Donnée, dans le Knowledge Pack | Nous, puis la communauté | **Composer** des capacités existantes |
| `IRepairAction` | Code, dans un registre compilé | Nous seuls, dans le dépôt | **Définir** une capacité |

Trois règles qui en découlent :

1. **Une `RepairRule` ne peut nommer qu'un `ActionId` présent dans le registre compilé.** Un `ActionId` inconnu ne provoque pas d'erreur bruyante : la règle est simplement **ignorée**, et le finding retombe en `ManualOnly`. Un pack plus récent que l'app dégrade proprement.
2. **Les paramètres sont typés et validés par l'action elle-même** (`ValidateParameters`), avant toute planification. Pas de chaîne libre interprétée à l'exécution.
3. **Confinement des cibles.** Le moteur — pas l'action, pas la règle — vérifie que chaque `PlannedChange.Target` résout à l'intérieur des racines détectées par `InstallLayout`. Une cible hors périmètre est rejetée et journalisée (`RuleRejected`).

Un pack peut donc apporter **de nouvelles combinaisons de capacités déjà auditées. Jamais une capacité nouvelle.** Ajouter une capacité exige un commit, une revue, une release — c'est-à-dire notre responsabilité éditoriale, qui ne se délègue pas.

## Alternatives écartées

- **Signer les packs et faire confiance à la signature.** Déplace le problème sans le résoudre : il faudrait une infrastructure de clés, et une contribution communautaire signée par nous reste une contribution que nous n'avons pas relue ligne à ligne.
- **Un mini-langage d'actions dans le pack** (conditions, boucles). Puissant, et exactement la porte qu'on cherche à fermer : un langage dans un fichier de données *est* de l'exécution de code.
- **Garder `IFixer` et interdire la contribution communautaire.** Sauverait la simplicité au prix du flywheel décrit en §8 — c'est-à-dire au prix du moat.

## Conséquences

**Positives**
- Le Knowledge Pack peut s'ouvrir à la communauté sans devenir une surface d'attaque. La décision qui rend le moat possible est aussi celle qui le rend sûr.
- La surface auditée est petite et stable : quelques actions, dans le dépôt, versionnées. Tout le reste est de la donnée inerte.
- Le confinement au niveau du moteur est un **second filet** : même une règle qui passerait la validation ne peut pas sortir de l'installation.

**Coût**
- Ajouter une capacité de réparation exige une release de l'application, pas seulement un pack. C'est plus lent — **et c'est l'objectif**. La lenteur est ici une fonctionnalité de sûreté.
- Deux concepts à comprendre au lieu d'un pour qui reprend le code.

**Lien avec le modèle économique** — ADR-002 fait reposer le renouvellement annuel sur l'enrichissement du Knowledge Pack. Cette ADR précise la nature de cet enrichissement : de nouveaux **correctifs** (données) tout le temps, de nouvelles **capacités** (code) au rythme des releases.
