# Prompt pour la prochaine session Cowork — refonte de l'écran Scanner

> À copier-coller tel quel dans une nouvelle session Cowork.
> Structure K.E.R.N.E.L : **K**adrage, **E**nvironnement, **R**éférences, **N**on-négociables,
> **É**tapes, **L**ivrables. (Si ton K.E.R.N.E.L attend d'autres intitulés, dis-le à la session,
> le contenu reste valable.)

---

## K — KADRAGE (l'objectif, en une phrase)

Tu reprends **Pincab Toolbox / FlipSync** (MC Automation, Maxime Chauvin). Effort élevé.

**Mission unique : faire que l'écran Scanner du vrai logiciel WPF soit un copier-coller fidèle de
la maquette `docs/maquette-scanner-2026-08-11.html`.** Pas « inspiré de », pas « dans l'esprit
de » : la même mise en page, la même densité, les mêmes blocs, aux contraintes techniques près
listées plus bas.

La session précédente a porté le bandeau du haut, la chaîne causale et le logo, mais le reste de
l'écran ne ressemble toujours pas à la maquette, et Maxime l'a dit clairement à trois reprises.
Ne recommence pas par petites retouches successives : chaque itération lui coûte un `build.cmd`.
**Fais le portage complet en une passe, puis livre.**

## E — ENVIRONNEMENT (ce qui va te surprendre, lis-le avant de coder)

- **Dépôt** : `/home/claude/pincab-suite` (clone-le si absent : `https://github.com/waylo1/pincab-toolbox`), branche `main`.
- **`PincabToolbox.App` NE COMPILE PAS dans le sandbox Linux.** `NU1100 : Microsoft.WindowsDesktop.App.Ref`
  introuvable, le SDK Windows Desktop n'existe pas hors Windows. C'est un fait documenté du projet,
  pas une régression. **Ne perds pas de temps à essayer de le contourner.**
- Core et Repair, eux, compilent et se testent normalement :
  `dotnet run --project tests/PincabToolbox.Core.Tests -c Release` → **412 tests**
  `dotnet run --project tests/PincabToolbox.Repair.Tests -c Release` → **145 tests**
  Les deux doivent rester verts. Lance d'abord `python3 tests/fixtures/make_fixtures.py`.
- **Comment vérifier du XAML sans compilateur** (méthode éprouvée, réutilise-la) :
  1. `python3 -c "import xml.etree.ElementTree as ET; ET.parse('...MainWindow.xaml')"`
  2. Passe de syntaxe C# : `dotnet exec /usr/lib/dotnet/sdk/*/Roslyn/bincore/csc.dll -nologo -t:library <fichiers .cs> -out:/tmp/o.dll`
     puis `grep -oE "error CS[0-9]+" | sort -u`. **Seules** `CS0234 CS0246 CS0518 CS0656` sont
     normales (références WPF absentes). Toute erreur `CS1xxx` est une vraie faute de syntaxe.
  3. Script de recoupement : extraire tous les `x:Name` du XAML et vérifier qu'aucun contrôle
     utilisé par le code-behind n'a disparu, et que chaque `Click=`/`MouseLeftButtonUp=` a bien sa
     méthode C#. **Ce script a déjà rattrapé des erreurs réelles, écris-le.**
  4. Vérifier que chaque `ImageSource=`/`Icon=` pointe sur un fichier qui existe vraiment.
- **`git push` est REFUSÉ depuis le sandbox** (le proxy renvoie 403, le dépôt n'est pas dans les
  sources autorisées de la session). Ne t'acharne pas. La méthode qui fonctionne :
  ```
  git bundle create /home/claude/<nom>.bundle origin/main..main
  ```
  puis `SendUserFile` du bundle **et** `mcp__remote-devices__device_commit_files` pour le déposer
  directement dans son dépôt, chemin exact :
  `C:\Users\User\Desktop\Pincab suite\pincab-toolbox-v0.1.1-alpha-src\pincab-suite\`
  Maxime lance ensuite `git pull .\<nom>.bundle main` puis `git push origin main`. Fais toujours
  un `git fetch origin` avant de fabriquer le bundle, sinon tu y remets des commits qu'il a déjà.

## R — RÉFÉRENCES (ce qui fait autorité)

1. **`docs/maquette-scanner-2026-08-11.html`** — LA cible. Ouvre-la, rends-la en image
   (Playwright + `/opt/pw-browsers/chromium-1194/chrome-linux/chrome`), et garde-la sous les yeux.
2. `docs/REVUE-maquettes-scanner-2026-08-11.md` — pourquoi la maquette est faite ainsi, et le
   croisement élément par élément avec ce que le code sait réellement produire.
3. `docs/CROSSCHECK-wishlist-chatgpt-2026-08-11.md` — inventaire réel des 32 scanners et 6 actions.
4. `TRANSMISSION.md` (bloc du haut) et `knowledge/FIELD-LOG.md`.
5. ADR-002 (100 % local, zéro réseau), ADR-006, ADR-010 (ne jamais affirmer plus que ce qui est
   mesuré), ADR-012.

**Déjà fait, ne le refais pas** : bandeau du haut (jauge en anneau, accroche, pastilles-filtres,
boutons), chaîne causale sous le diagnostic, logo dans l'en-tête + icône de fenêtre, image de fond
`Assets/background.png` (1920×430, atelier flou + bokeh).

**Ce qui manque pour ressembler à la maquette** (c'est ton travail) :
- Les **cartes de causes racines** en liste, avec badge de gravité, titre, puce de confiance à
  droite, phrase joueur, phrase d'impact, chaîne causale et pied de carte (nombre de composants et
  de tables touchés, codes concernés).
- La **colonne de droite** : résultats critiques, santé des composants, remarques.
- Les **onglets internes** (Causes racines / Tous les résultats / Composants / Tables / Système).
- Le **tableau des tables** et la densité générale de la maquette.

## N — NON-NÉGOCIABLES

1. **Le tableau des résultats a un plancher de hauteur** (`MinHeight` sur sa `RowDefinition`) et il
   ne bouge pas. Le piège dans lequel la session précédente est tombée : chaque bloc ajouté au-dessus
   prenait ses pixels au tableau, jusqu'à le réduire à une ligne. Si tu ajoutes du contenu, tu enlèves
   de la hauteur ailleurs, tu ne la prends pas au tableau.
2. **Aucune donnée inventée.** Chaque chiffre, chaque libellé affiché doit venir d'un vrai résultat de
   scan. En particulier :
   - `App/Scenarios.cs` ne définit que **DEUX** scénarios et `Detect()` ne retourne que **LE MEILLEUR**.
     Pour afficher une vraie liste « Causes racines (2) », il faut le faire retourner la liste triée.
     Tu peux ajouter des scénarios (c'est une simple table de données) — le LOT A a livré les codes COM
     nécessaires pour écrire « Problème d'enregistrement FlexDMD ».
   - Le panneau **« Santé des composants » de la maquette n'a aucune source de données aujourd'hui.**
     Soit tu l'alimentes depuis de vrais scanners (versions, bitness, présence), soit tu ne l'affiches
     pas. Ne le remplis jamais avec du plausible.
   - Pas d'historique, pas de tendances, pas de « vu pour la première fois en 2018 » : rien ne persiste
     les scans successifs.
3. **Confiance en mots** (élevée / moyenne / faible), jamais en pourcentage — ADR-010.
4. **Aucun voyant réseau** type « Health engine · Online » : le produit est 100 % local (ADR-002), c'est
   son argument de vente.
5. **Aucun artwork de flipper réel** (Attack From Mars, Medieval Madness… appartiennent à Bally/Williams).
   Toutes les illustrations sont originales.
6. **Tous les `x:Name` et gestionnaires existants sont conservés**, ou alors le code-behind est mis à jour
   dans le même commit. Les pastilles de gravité restent des filtres cliquables.
7. **Ne jamais annoncer un résultat non vérifié.** Tu ne peux pas compiler l'App : dis-le explicitement
   dans ton message final, et précise ce que tu as vérifié et comment.

## É — ÉTAPES

1. Lire la maquette et la rendre en image. Lister par écrit les écarts avec l'écran actuel.
2. Vérifier quelles données existent VRAIMENT pour chaque bloc de la maquette (lance le moteur de scan
   sur `src/PincabToolbox.App/DemoData/install` avec un petit programme jetable référençant
   `PincabToolbox.Core` — la session précédente l'a fait, ça marche et ça évite d'inventer).
3. Décider, et **écrire dans le message final**, quels blocs de la maquette sont livrés tels quels,
   lesquels sont adaptés faute de données, lesquels sont écartés et pourquoi.
4. Coder le portage **en une passe**.
5. Vérifier (les 4 contrôles de la section E) + `Core 412` + `Repair 145`.
6. Committer, fabriquer le bundle, le déposer sur sa machine, donner les commandes exactes.

## L — LIVRABLES

- L'écran Scanner porté, en un ou deux commits propres et **annulables d'un `git revert`**.
- Un bundle déposé dans son dépôt + les commandes à lancer, dans cet ordre, prêtes à copier.
- `TRANSMISSION.md` (bloc du haut) et `knowledge/FIELD-LOG.md` mis à jour.
- Une **revue CTO + Produit** en clôture : le code est-il propre, l'architecture cohérente, les tests
  suffisants, la valeur utilisateur réelle, les risques techniques et commerciaux, et une amélioration
  à faible coût **proposée sans être codée**.

### Deux choses à savoir sur Maxime

- Il travaille sur **Windows**, son dépôt est à
  `C:\Users\User\Desktop\Pincab suite\pincab-toolbox-v0.1.1-alpha-src\pincab-suite`, il build avec
  `build.cmd` et il teste en **Mode démo**.
- Chaque aller-retour lui coûte un build complet. **Une passe complète et vérifiée vaut mieux que cinq
  retouches.** Et s'il y a un doute sur ce qu'il veut, une question posée AVANT de coder vaut mieux
  qu'un build de plus.

### Le sujet vraiment important qui attend derrière

Le chemin d'écriture Repair est câblé et la vraie clé de licence est déployée, mais **il n'a jamais été
exécuté sur un vrai cabinet**. Un mode simulation forcée existe pour ça
(`PINCAB_REPAIR_FORCE_DRYRUN=1`). Quand le visuel sera réglé, c'est là qu'est la valeur.
