# Prompt pour la session Sonnet — implémentation de la refonte UI

> À copier-coller tel quel dans une nouvelle session Cowork (modèle Sonnet).
> Structure K.E.R.N.E.L : **K**adrage, **E**nvironnement, **R**éférences, **N**on-négociables,
> **É**tapes, **L**ivrables.

---

## K — KADRAGE (l'objectif, en une phrase)

Tu reprends **Pincab Toolbox** (MC Automation, Maxime Chauvin), application WPF/C#. Effort élevé.

**Mission unique : implémenter la refonte UI en suivant le plan déjà écrit et validé
`knowledge/UX-REDESIGN-PLAN.md`, lot par lot, un commit par lot, SANS prendre de décision
d'architecture — elles sont toutes déjà tranchées dans le plan.**

Le plan a été produit par une session Opus, ancré sur le vrai code (chaque fichier, ressource XAML,
méthode et ligne y est vérifié). Il découpe la refonte en 7 lots priorisés (impact utilisateur réel /
effort WPF pur / risque de régression). Le logo de marque vient de changer — noir + vert olive-lime +
argent, l'orange n'est plus une couleur de marque — et le Lot 1 du plan intègre déjà la bascule
orange→vert.

## E — ENVIRONNEMENT (à lire avant de coder)

- **Dépôt** : clone `https://github.com/waylo1/pincab-toolbox` (public, pas d'auth pour le clone),
  branche `main` à jour, le plan y est déjà.
- **`PincabToolbox.App` NE COMPILE PAS dans le sandbox Linux** : `NU1100`, le SDK Windows Desktop
  n'existe pas hors Windows. Fait documenté du projet, pas une régression. Ne perds pas de temps à le
  contourner.
- **Core et Repair compilent et se testent normalement** :
  `dotnet run --project tests/PincabToolbox.Core.Tests -c Release` et
  `tests/PincabToolbox.Repair.Tests`. Ils doivent rester VERTS à chaque lot.
- **La maquette du 11/08 est DÉJÀ portée dans le code** (plan §0) : bandeau hero avec jauge de score,
  5 onglets internes du Scanner, cartes de causes racines, colonne de droite, etc. existent déjà. Ne
  reconstruis rien — plusieurs lots sont du polish ou du re-scope, pas de la création.
- **Le push depuis le sandbox est bloqué** (fait connu). Tu livres par git bundle (voir Livrables).

## R — RÉFÉRENCES (à lire en entier avant la première ligne)

- `knowledge/UX-REDESIGN-PLAN.md` — **ta seule source de vérité.** Lis-le intégralement, surtout :
  §0 (état réel du code), §1 (réponses de vérification : format du score, ce que `ListFindings` sait
  déjà faire, structure des onglets, noms exacts des ressources de sévérité, chemins de
  `PathScrubber.Scrub`), §2 (garde-fous), §3 (les 7 lots, avec pour chacun : fichiers exacts,
  ressources par nom réel, ce qu'il ne faut pas toucher, critère de « terminé » vérifiable).
- Les ADR cités par le plan : `docs/adr/ADR-006` (dry-run / Scanner annonce, Repair vend),
  `ADR-010` (doctrine Note, pas de pourcentage de confiance), `ADR-012` (chemin d'écriture Repair).
- `knowledge/FIELD-LOG.md` — pour la méthode de vérification hors Windows des sessions précédentes.

## N — NON-NÉGOCIABLES (invariants, plan §2)

- **Zéro donnée inventée** : une case sans mesure affiche « — », jamais une valeur déduite du silence
  d'un scanner (ADR-010).
- **`Public()`/PathScrubber reste la seule sortie de rapport** : aucun export ni copie ne le contourne
  (ADR-003).
- **Les étapes Repair non-automatisables restent visibles**, jamais tronquées ni masquées (ADR-006 §2).
- **Pas de voyant réseau** (ADR-002). **Pas de pourcentage de confiance** (ADR-010). **`Note` ne bouge
  jamais le score.** **Plafonds de virtualisation intacts.**
- **Marque ≠ sévérité (§2.9)** : SEUL l'orange de MARQUE bascule au vert du logo (`Accent`/`AccentDark`
  + les 5 sites listés au Lot 1). L'orange de sévérité `Warning` (`#F5A524`) et l'émeraude `Ok`
  (`#46C06E`) ne changent JAMAIS. Garde le vert de marque nettement plus jaune que l'émeraude.
- **Les visuels de logo** (`Assets/logo.png` = emblème seul ; `Assets/logo-full.png` = emblème + nom)
  sont fournis par Maxime. S'ils sont déjà dans le dépôt, prélève le vert exact à la pipette dessus ;
  sinon utilise les valeurs échantillonnées : `Accent` olive-lime `#708830` (reflets jusqu'à
  `#94C818`), `AccentDark` `#4E5F22`.
- **Tu implémentes, tu ne re-décides pas.** Le véhicule des mises à jour (6ᵉ onglet interne du
  Scanner), les valeurs de couleur, l'ordre des lots : tout est déjà tranché dans le plan.

## É — ÉTAPES

1. Clone le dépôt, lis `knowledge/UX-REDESIGN-PLAN.md` en entier (surtout §0 et §2).
2. Exécute les **lots 1 à 6 DANS L'ORDRE**, un commit par lot. Pour chaque lot, applique littéralement
   ses 4 rubriques : « Fichiers à toucher » / « Ce qu'il faut faire » / « Ne pas toucher » /
   « Terminé si ».
3. Vérifie chaque lot sans build Windows : XML bien formé, `csc` sans références WPF (zéro CS1xxx),
   script de recoupement x:Name/gestionnaires/assets à 0 erreur, Core + Repair tests VERTS.
4. **NE FAIS PAS le lot 7** (panneau latéral) : le plan le gèle tant que les lots 1-6 ne sont pas
   validés sur la machine Windows de Maxime (§Lot 7, §4). Signale-le comme session suivante.
5. Avant de clôturer, **revue CTO + Produit** : code propre, architecture cohérente avec le plan,
   tests suffisants, vraie valeur utilisateur, risque technique ou commercial, amélioration à faible
   coût éventuelle signalée sans la coder.

## L — LIVRABLES

- Un **git bundle** de tes commits (un par lot) : `git bundle create refonte-ui.bundle main`, livré à
  Maxime via `SendUserFile` — même convention que les `.bundle` déjà présents dans le dépôt. Maxime
  l'importe (`git pull refonte-ui.bundle main`) et pousse lui-même.
- Pour chaque lot livré : le diff, les fichiers touchés, ce qui a été vérifié en sandbox, et le
  **point de contrôle visuel exact** que Maxime doit faire sur Windows (`build.cmd` + Mode démo).
- Ne pousse rien depuis le sandbox. Ne crée aucun fichier hors du périmètre des lots. Ne déclenche
  aucun autre trigger. Ne touche pas au projet Core sauf si un lot le dit explicitement (le Lot 6 n'en
  a pas besoin), ni à la logique de décision Repair (`PincabToolbox.Repair`, ADR-012).
