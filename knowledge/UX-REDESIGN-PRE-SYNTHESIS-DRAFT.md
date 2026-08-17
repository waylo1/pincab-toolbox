# Refonte UI Pincab Toolbox — pré-synthèse (BROUILLON NON ANCRÉ)

> **Statut : phase 1 non terminée.** Ce run planifié s'est exécuté dans un conteneur cloud
> vierge : le dépôt `pincab-suite` n'y est pas, et une session planifiée n'a jamais accès à la
> machine de Maxime (pas de pont device, pas de MCP local, pas de fichiers locaux). Aucun des
> fichiers demandés (`MainWindow.xaml`, `MainWindow.xaml.cs`, `App.xaml`, `knowledge/FIELD-LOG.md`)
> n'a donc pu être lu.
>
> **Ce document n'est PAS `knowledge/UX-REDESIGN-PLAN.md`.** Il ne contient volontairement
> aucune référence de fichier, de style ou de ligne inventée : produire un plan « précis » sans
> avoir ouvert le code violerait la règle zéro donnée inventée et enverrait la phase 2
> implémenter contre des suppositions. Ce qu'il contient, c'est le travail d'arbitrage qui ne
> dépend pas du code : quoi retenir des trois avis, dans quel ordre, et quoi vérifier avant de
> figer chaque lot.
>
> Phase 2 n'a pas été déclenchée. Voir la fin du document.

---

## 1. Grille d'arbitrage retenue

Le critère n'est pas « est-ce une bonne idée en UI », c'est **impact utilisateur réel / effort
en WPF XAML pur / risque de régression sur une base existante qui marche**. En WPF, ce qui est
bon marché et sûr, ce sont les changements portés par des ressources globales (couleurs, styles
implicites, `Thickness`, `CornerRadius`, `Style`/`DataTrigger`) parce qu'ils se propagent sans
toucher le code-behind ni la logique. Ce qui est cher et risqué, ce sont les changements de
structure de layout et l'ajout de comportements d'animation ou de panneaux flottants.

## 2. Ce qu'on retient, ce qu'on écarte

**Retenu en priorité (fort impact, effort maîtrisé, risque faible)**

La hiérarchie visuelle est le vrai problème pointé par les deux avis, et c'est aussi le moins
cher à corriger : aujourd'hui tout a le même poids. Le duo « score de santé en élément hero » +
« resserrement de l'orange aux seuls CTA, progression, score et icônes de sévérité » traite la
plainte principale des deux avis avec des moyens WPF standards, sans toucher au layout général.
L'orange resserré est même le meilleur rapport impact/effort du lot : c'est une modification de
ressources, elle se propage partout, et elle rend l'accent de nouveau lisible comme signal.

L'espacement sur grille de 8px (Gemini) est la bonne façon d'exécuter le « plus d'air » de GPT :
même objectif, mais une règle vérifiable au lieu d'un réglage à l'œil. À faire dans le même lot
que la respiration des cartes et des tableaux.

Le point de Joey Mahon est traité comme un item de plan à part entière, pas comme une annexe.
C'est le seul retour terrain réel du lot, il vient d'un testeur avec une poignée de tables
seulement, donc le bruit ne peut qu'empirer sur une bibliothèque complète. Impact modéré à fort,
effort raisonnable. Le véhicule (5ᵉ onglet dédié contre vue filtrée dans le Scanner) se tranche
en lisant l'architecture réelle des onglets — voir la liste de vérifications plus bas. Point de
vigilance produit : l'Update Watcher est encore bêta, donc l'onglet doit se comporter
correctement quand il n'y a rien à afficher et ne doit pas donner l'impression d'une
fonctionnalité plus mûre qu'elle ne l'est.

**Retenu mais plus tard (impact réel, effort ou risque plus élevé)**

Les badges/pills de sévérité et les boutons standardisés : bonne idée, mais le projet a déjà ses
couleurs de sévérité définies globalement, donc le lot consiste à les réutiliser dans un style
partagé, surtout pas à en inventer de nouvelles. Les tableaux modernisés (lignes plus hautes,
hover, alignement chiffres à droite) viennent ensuite, en vérifiant d'abord ce qui existe déjà
en tri, recherche et redimensionnement de colonnes pour ne pas redévelopper l'existant.

Les micro-animations discrètes arrivent après le fond : fade court, barre de progression fluide,
étapes de scan nommées. Elles polissent, elles ne corrigent rien. Les faire tôt, c'est décorer
une hiérarchie qui n'est pas encore réglée.

**Écarté ou repoussé en fin de file**

Le panneau latéral droit rétractable façon VS Code : c'est le seul point où les deux avis
convergent sur la valeur et où Gemini lui-même signale le risque de casser le layout. Fort
impact, gros effort, forte surface de régression. Il passe en dernier, isolé dans un composant
neuf plutôt qu'en rafistolant le panneau bas actuel, et il doit être testé avec le contenu réel
le plus long possible, typiquement un Repair non automatisable avec plusieurs raisons — parce
que masquer ou tronquer ces étapes casserait une contrainte projet, pas juste une maquette.

La migration Tauri/React de Gemini est hors sujet : la stack reste WPF/C#. Le changement de
police vers une échelle typographique complète est à traiter avec prudence, une police non
installée sur la machine cible dégrade silencieusement le rendu ; à valider avant de s'engager.
Le bloc de confiance de GPT est largement déjà couvert par l'onglet About, donc c'est un
enrichissement, pas une création.

## 3. Découpage en lots proposé (un commit par lot)

L'ordre est celui-ci parce que chaque lot rend le suivant plus facile à juger : la palette et
l'espacement d'abord, la hiérarchie ensuite, les composants après, le structurel en dernier.

1. Fondations : jetons d'espacement 8px et resserrement de l'orange aux seuls éléments de signal.
2. Hiérarchie de l'écran Scanner : score de santé en hero, actions prioritaires, puis détail.
3. Onglet ou vue dédiée aux mises à jour de tables (retour Joey), retirant ces alertes du flux principal.
4. Composants partagés : styles de boutons, badges de sévérité réutilisant les couleurs existantes.
5. Tableaux : densité, hover, alignement, en complétant l'existant sans le dupliquer.
6. Polish : animations courtes, progression par étapes nommées, logs en timeline.
7. Panneau de détail latéral, isolé, seulement si les lots précédents sont stables.

## 4. À vérifier impérativement à l'ouverture du code (avant de figer le plan)

Le format actuel du score de santé, pour savoir s'il s'agit déjà d'un état qualitatif ou d'un
pourcentage à convertir, ADR-010 interdisant la confiance en pourcentage. Ce que `ListFindings`
sait déjà faire en tri, recherche et colonnes, pour ne pas redévelopper l'existant. La structure
réelle des quatre onglets et la façon dont les findings `UPDATE_AVAILABLE` sont produits et
filtrés, qui détermine le véhicule du point de Joey. Les noms exacts des ressources de couleur
de sévérité dans `App.xaml`. Enfin, où passent `PathScrubber.Scrub`, les étapes non
automatisables du Repair et l'absence d'indicateur réseau, pour garantir qu'aucun lot visuel ne
les efface.

## 5. Suite

Phase 1 doit être rejouée là où le dépôt existe. Le trigger de phase 2 n'a délibérément pas été
appelé : sans plan ancré dans le code, la session d'implémentation partirait de suppositions, et
elle tomberait de toute façon sur le même conteneur vide.
