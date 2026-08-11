# Revue des maquettes Scanner (Gemini + GPT), 11/08/2026

**Contexte** : Maxime a fait produire 4 maquettes du Scanner, 1 par Gemini et 3 par GPT. Il aime le
visuel de Gemini et les informations affichées par GPT. Ce document les critique en tant que design,
puis, et c'est le plus important, croise chaque élément affiché avec ce que le code sait réellement
produire aujourd'hui, pour séparer ce qui est du câblage d'UI de ce qui est une fonctionnalité à
construire.

**Découverte pendant cette revue** : ces maquettes ont visiblement été produites avec accès au vrai
code. Le titre « Incomplete 32→64 migration », son texte d'explication et le plafond de confiance à
96 % sont **mot pour mot** ceux de `App/Scenarios.cs`. Le score 38/100 avec la note F est cohérent
avec la vraie formule de `ScanScoring`. Ce ne sont donc pas des maquettes hors sol, et c'est une
très bonne nouvelle : la majorité de ce qu'elles montrent est déjà calculable.

---

## 1. Verdict rapide sur chaque maquette

### Gemini, l'image

C'est une **image générée**, pas une maquette exploitable : plusieurs textes sont du charabia
(« Inspier context-aware suggestione for your Pincall », « yonaim omsade Pincab Todail X »,
« Repair Center » devenu « Repair Center » puis « Tesls »). On ne peut pas l'implémenter, on peut
seulement s'en inspirer.

Ce qu'elle apporte, et c'est réel : une **direction visuelle plus chaleureuse** que celle de GPT.
Les 5 pastilles de sévérité en cartes colorées pleine largeur (3 Critical, 3 Warnings, 5 Notes,
12 Info, 4 OK) se lisent en une demi-seconde, mieux que la liste texte de GPT. Le bandeau
d'en-tête avec le médaillon donne une identité produit que les maquettes GPT n'ont pas.

Ce qui ne va pas : le bandeau d'en-tête mange environ 15 % de la hauteur utile pour de la
décoration, sur un écran dont le métier est d'afficher de la densité. Les mini-courbes dans
« Component Health » sont purement décoratives, elles supposent un historique de scans qui
n'existe pas.

### GPT, les 3 variantes

Meilleure architecture de l'information, plus froides visuellement. La **variante 3, la plus
détaillée, est de loin la meilleure des quatre**, pour une raison précise : elle ajoute un
**mini-schéma de chaîne causale** par cause racine.

```
32-bit ─✗→ 64-bit          DLL → COM → DMD           Table → B2S → Backglass
legacy    current     Not registered, Missing, Not working    Needs BG, File missing, Not found
```

C'est la meilleure idée des quatre maquettes. Un propriétaire de pincab non technique ne comprend
pas « COM_NOT_REGISTERED », mais il comprend une chaîne de trois cases dont la deuxième est rouge.
C'est exactement le saut symptôme → diagnostic que `Scenarios.cs` fait déjà côté code sans que
l'UI le montre.

---

## 2. Le croisement qui compte : maquette vs code réel

### Déjà calculé, il ne manque que l'affichage

| Élément de la maquette | Réalité du code |
|---|---|
| Score 38/100 + note F | `ScanScoring.ComputeScore` / `GradeFor`, formule existante et partagée |
| Compteurs Critical/Warning/Note/Info/OK | `Severity`, déjà compté |
| Cause racine + explication + confiance % | `App/Scenarios.cs`, déjà calculé, y compris le plafond à 96 % |
| « Repair available », « Auto repair, High confidence » | `RepairMode` (Automatic / ConfirmationRequired / Locked / ManualOnly), déjà décidé par le moteur |
| « Est. repair time ~3 min » | `RepairSummary.EstimatedDuration` (`DurationBucket`), déjà calculé |
| « Affected : 22 tables, 9 components » | dérivable des findings, aucun nouveau calcul |
| « Copy for forum » | l'export anonymisé existe (ADR-003) |
| « Export report » | HTML, TXT, Markdown, BBCode, JSON existent déjà. **Seul le PDF manque** |
| Onglets Root causes / All findings / Components / Tables / System | regroupements dérivables des findings existants |

### Petit travail de code, pas une fonctionnalité

| Élément | Ce qu'il faut vraiment |
|---|---|
| **Liste de 4 causes racines** | `Scenarios.Detect` retourne aujourd'hui **une seule** cause, la meilleure (`ScenarioMatch?`). Il faut la faire retourner la liste triée. Changement mineur et localisé |
| **Les 4 causes montrées** | Seulement **2 scénarios sont définis** aujourd'hui (migration 32→64, intégration frontend). « FlexDMD registration issue » et « Backglass files missing » n'existent pas encore comme scénarios. Or ce sont juste des entrées à ajouter dans un tableau de données, et le LOT A de cette session vient précisément de livrer les codes COM qu'il leur faut |
| Liste « Component Health » avec versions | les versions sont lues par plusieurs scanners, il manque l'agrégation et l'écran |

### Fonctionnalités à construire, à ne pas confondre avec du design

- **System Map** (le graphe VPX → VPinMAME / B2S / FlexDMD → DOF). Très beau, c'est un vrai
  chantier, et en WPF ce n'est pas une bibliothèque qu'on installe, c'est du dessin à la main.
- **Historique et tendances** : « First seen 2018 · Last seen 2026 », les mini-courbes de tendance,
  l'entrée de menu « Repair History ». Rien de tout cela n'existe, il faudrait persister les scans
  successifs. Sur une première installation ces informations seraient de toute façon vides.
- **Écrans Backups, Reports, Tools, Repair History** : présents dans la navigation des maquettes,
  inexistants dans l'app, qui a 4 onglets réels (Scanner, Script Diff, Repair, À propos).

### Deux points à corriger avant de coder quoi que ce soit

**« Health engine · Online » et « Database · Up to date » en bas à gauche.** Ces deux voyants
suggèrent un service distant permanent. Le produit est **100 % local, zéro appel réseau** par choix
documenté (ADR-002), à l'exception d'un unique bouton de vérification de mise à jour explicite. Un
voyant « Online » permanent contredit l'argument de vente le plus fort du produit, la vie privée,
et créerait une attente de disponibilité de serveur que MC Automation n'a pas à assumer. À
supprimer.

**La confiance affichée en pourcentage précis (96 %, 78 %, 60 %, 50 %).** Ces nombres existent
vraiment dans le code, mais ils viennent d'une formule volontairement simple, une base plus un
bonus par code trouvé. Les afficher au point de pourcentage près donne une impression de rigueur
statistique que le calcul ne porte pas, ce qui va contre la doctrine du projet, qui est de ne
jamais affirmer plus que ce qui est mesuré (ADR-010). **Recommandation : afficher Élevée / Moyenne /
Faible**, et garder le nombre pour le mode expert ou l'export JSON.

---

## 3. Critique de design proprement dite

### Hiérarchie visuelle

Ce qui accroche l'œil en premier dans les maquettes GPT, c'est le gros bouton orange « SCAN MY
PINCAB » en haut à droite, et c'est correct, c'est bien l'action principale. Mais le score arrive
juste après avec la même force visuelle, et sur les variantes 3 et 4 le score est affiché **deux
fois**, dans la jauge en haut à gauche et dans la barre de statut en bas à droite. À dédoublonner,
garder celui du haut.

### Le ton du score

« 38/100, CRITICAL, F » en rouge vif, sur un cabinet qui, du point de vue de son propriétaire,
fonctionne à peu près. Le risque produit est réel : la première réaction sera « ton outil exagère »
plutôt que « il faut que je répare ». La doctrine du projet est d'être honnête et rassurant, pas
alarmant. Suggestion, garder le chiffre mais adoucir le vocabulaire, « 3 problèmes bloquants
trouvés » informe mieux que la lettre F, qui juge la personne plutôt que l'installation.

### Accessibilité

Le gris utilisé pour les métadonnées (« Based on : VPinMAME, dmddevice », « Module : COM
Registration ») passe probablement sous le ratio de contraste 4,5:1 sur ce fond sombre, c'est le
défaut le plus courant des thèmes sombres générés par IA. À vérifier et remonter d'un cran. Par
ailleurs la sévérité est parfois portée par la seule couleur de la barre latérale gauche des
cartes, il faut systématiquement une icône ou un mot en plus, pour les daltoniens et pour les
captures d'écran postées sur un forum en noir et blanc.

### Faisabilité WPF

Ces maquettes ont une esthétique web. Cartes arrondies, dégradés, courbes, graphe de dépendances.
Tout est faisable en WPF, mais pas au même coût qu'en HTML, et le System Map est à lui seul un
morceau conséquent. Pour une micro-entreprise avec un développeur, l'ordre d'attaque compte plus
que le rendu final.

---

## 4. Ce que je recommande, sans le coder

1. **Prendre la variante 3 de GPT comme base d'architecture**, et lui appliquer la palette et la
   chaleur visuelle de Gemini. Ne pas essayer d'implémenter l'image de Gemini, elle n'est pas
   exploitable telle quelle.
2. **Commencer par les mini-schémas de chaîne causale**, c'est le meilleur rapport valeur/effort de
   tout le lot, et les données existent déjà.
3. **Ajouter des scénarios dans `Scenarios.cs` et faire retourner la liste complète.** C'est le
   changement le moins cher du document et celui qui remplit visuellement l'écran principal. Sans
   lui, l'écran « Root causes (4) » affichera une seule ligne en vrai.
4. **Réduire la navigation à ce qui existe.** Un menu à 9 entrées dont 5 mènent à des écrans vides
   fait plus de mal qu'un menu à 4 entrées honnête.
5. **Supprimer « Health engine Online »**, et passer la confiance en Élevée / Moyenne / Faible.
6. **Repousser System Map et l'historique** à un lot ultérieur, ce sont des fonctionnalités, pas de
   l'habillage.

Aucun de ces points n'a été codé. Ce document est là pour que Maxime tranche l'ordre avant qu'une
ligne d'UI ne soit écrite, comme pour la spec du 10/08.
