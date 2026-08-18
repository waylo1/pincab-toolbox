# Prompt pour la prochaine session Cowork — raison d'échec par item Repair

> À copier-coller tel quel dans une nouvelle session Cowork.
> Structure K.E.R.N.E.L : **K**adrage, **E**nvironnement, **R**éférences, **N**on-négociables,
> **É**tapes, **L**ivrables.

---

## K — KADRAGE (l'objectif, en une phrase)

Tu reprends **Pincab Toolbox / FlipSync** (MC Automation, Maxime Chauvin). Effort élevé, en autonomie
totale — Maxime ne sera pas là pendant cette session.

**Mission : quand un item de réparation échoue, l'utilisateur doit voir POURQUOI, pas juste
« 1 échec ».** Aujourd'hui `ApplyResult.ItemOutcomes` est un `bool` par item — la vraie raison
(exception de sauvegarde, erreur de l'action, action inconnue) existe déjà et est déjà écrite dans le
journal disque, mais elle est jetée avant d'atteindre l'App. C'est un trou plus petit qu'il n'y
paraissait au premier chiffrage — **ce document a déjà fait la lecture de code**, ne la refais pas :
va directement à la section É.

## E — ENVIRONNEMENT

- **Dépôt** : `/home/claude/pincab-suite` (clone si absent : `https://github.com/waylo1/pincab-toolbox`), branche `main`.
- **`PincabToolbox.App` NE COMPILE PAS dans le sandbox Linux** — fait documenté, vérifie tes changements
  App par XML bien formé + passe de syntaxe Roslyn (voir plus bas), jamais par compilation réelle.
- **`dotnet` 8.0.129 EST disponible** dans ce sandbox (vérifié le 18/08/2026) :
  ```bash
  dotnet run --project tests/PincabToolbox.Core.Tests -c Release
  dotnet run --project tests/PincabToolbox.Repair.Tests -c Release
  ```
  Ce sont des `TestRunner.cs` maison (pas xunit/vstest), donc ça tourne sans réseau. **Baseline
  vérifiée le 18/08 : Core 501/501, Repair 156/156.** Lance aussi `-c Debug`. Ne livre rien en dessous
  de cette baseline.
- Vérif XAML/C# sans compilateur Windows, mêmes commandes que le reste du projet :
  ```bash
  python3 -c "import xml.dom.minidom as m; m.parse('src/PincabToolbox.App/MainWindow.xaml')"
  dotnet /usr/lib/dotnet/sdk/8.0.129/Roslyn/bincore/csc.dll -noconfig -target:library \
    -out:/tmp/syntaxcheck.dll src/PincabToolbox.App/MainWindow.xaml.cs \
    src/PincabToolbox.App/Localization/Loc.cs 2>&1 | grep -E 'CS1[0-9]{3}'
  ```
  Zéro `CS1xxx` = OK (le reste, `CS0246` etc., ce sont des références WPF absentes sous Linux, normal).
- **`git push` REFUSÉ depuis le sandbox.** `git bundle create /home/claude/repair-raison-echec.bundle main`
  → `SendUserFile` → `mcp__remote-devices__device_commit_files` vers
  `C:\Users\User\Desktop\Pincab suite\pincab-toolbox-v0.1.1-alpha-src\pincab-suite\`. Vérifie que le
  bridge (`mcp__remote-devices__get_device_info`) répond avant d'annoncer une livraison, et regarde le
  champ `rejected` de `device_commit_files`.

## R — RÉFÉRENCES ET LECTURE DE CODE DÉJÀ FAITE (ne la refais pas)

Le chemin d'écriture Repair (Preflight/Apply/Undo, journal persistant) **est déjà entièrement câblé
depuis le lot du 10/08** (`docs/SPEC-lot-communaute-2026-08-10.md` §5 LOT H, `docs/adr/ADR-012-chemin-ecriture-repair.md`).
**Un ancien commentaire dans `PincabToolbox.App.csproj` disant « Preflight/Apply/Undo are never called
from the App » est FAUX/PÉRIMÉ** — corrigé le 18/08 dans le même lot que ce document. Ne le crois pas
si tu le revois quelque part.

**Le point exact où la raison se perd**, vérifié en lisant `src/PincabToolbox.Repair/Engine/RepairEngine.cs` :

- `Apply()` (L.344) boucle sur `pre.RetainedItems` et fait `outcomes[item.ItemId] = ok;` — un simple
  `bool`, jamais la raison.
- Le **backup qui échoue** (L.378-386) attrape déjà l'exception et écrit `ex.Message` **dans le
  journal** (`_journal.Write(...BackupFailed...)`) avant de faire `outcomes[item.ItemId] = false;` —
  le message existe, il est juste jeté à cet endroit précis.
- `ApplyItem()` (L.420) appelle `action.Execute(c)` qui retourne déjà un `ExecutionResult` avec un
  `Error` **string** (`Contracts.cs` L.189-193) — si `!res.Success`, ce message est écrit au journal
  (`ChangeFailed`) puis, encore une fois, jeté : `ApplyItem` retourne juste `(bool ok, bool recovery)`.
  Une action inconnue (`unknown action {c.ActionId}`) suit exactement le même chemin.
- `Compensate()` (L.447) : si le rollback lui-même échoue, le message existe aussi (`res.Error`),
  écrit au journal (`RecoveryRequired`), et jeté de la même façon.

**Conclusion du chiffrage** : la donnée existe déjà partout où elle est produite ; le travail est de la
**faire remonter** jusqu'à `ApplyResult`, pas de l'inventer. C'est un changement additif, pas une
refonte — voir §N pour la contrainte précise qui garde ça petit.

Côté App, `result.ItemOutcomes` n'est consommé qu'à un seul endroit aujourd'hui
(`MainWindow.xaml.cs` L.1997-2006, dans le handler `BtnRepairApply_Click` ou équivalent) : juste un
comptage `ok`/`failed` affiché dans `RepairApplyStatus` (un simple `TextBlock`, pas de liste). Pas
d'autre UI à réconcilier.

Côté tests, `ItemOutcomes` est référencé dans **8 endroits** entre `RepairSessionTests.cs` et
`RepairTests.cs`, tous sous la forme `result.ItemOutcomes.TryGetValue("i1", out var ok) && ok` ou
`.Count(...)`/`.Values.Any(...)`. **Ce sont ces 8 tests qui dictent la contrainte de conception
ci-dessous — ne casse aucun d'eux.**

## N — NON-NÉGOCIABLES

1. **N'élargis PAS le type de `ItemOutcomes`.** Ne le fais pas passer de
   `IReadOnlyDictionary<string, bool>` à un type enrichi (record, tuple...) — ça casserait les 8 tests
   existants et tout code futur qui suppose un `bool`. **Ajoute un nouveau champ à côté**, dans
   `ApplyResult` (`Contracts.cs`) :
   ```csharp
   public IReadOnlyDictionary<string, string?> ItemFailureReasons { get; init; } =
       new Dictionary<string, string?>();
   ```
   Une clé = un `ItemId` qui a échoué (`outcomes[id] == false`), une valeur = le message technique
   (anglais, brut — c'est un détail de diagnostic, pas un texte UI à traduire mot pour mot, voir N3).
   Un item réussi n'a pas d'entrée dans ce dictionnaire (pas de `null` bruyant).
2. **Ne modifie pas la signature publique de `IRepairEngine.Apply`** — seul le contenu de
   `ApplyResult` change (additif), pas la forme de l'appel. `RepairSession.Apply` (le point d'entrée
   réel de l'App) n'a rien à changer côté signature non plus.
3. **Le message doit rester un détail technique lisible par un utilisateur bloqué, pas un texte
   marketing.** Réutilise le patron déjà établi par `Blocker`/`RepairLimitation` (`Contracts.cs`) :
   `MessageEn` toujours peuplé (c'est souvent une `Exception.Message` .NET, en anglais, c'est normal et
   déjà le cas ailleurs dans ce moteur), pas besoin de traduction FR/ES pour CE champ précis — ce
   n'est pas un texte de Finding, c'est un détail d'erreur technique, exactement comme le fait déjà
   `RecoveryRequired`/`res.Error` dans le journal. Ne complique pas cette session en essayant de
   traduire des messages d'exception .NET.
4. **N'invente rien côté `Compensate`.** Si l'item échoue puis que la compensation (rollback) réussit,
   la raison affichée doit rester **la cause originale de l'échec** (celle de `ApplyItem`), pas
   « compensation réussie ». Si la compensation elle-même échoue (`RecoveryRequired = true`), ce cas a
   déjà son propre affichage (`repair.apply.recovery` + `BackupPath`) — n'ajoute pas de raison en
   double dedans, `RecoveryRequired` reste prioritaire dans l'UI existante.
5. **Backup en échec** : la raison est `ex.Message` de l'exception attrapée en L.383 de
   `RepairEngine.cs` — ne relance pas cette exception plus haut, le comportement actuel (item marqué
   échoué, plan continue) doit rester identique, seul le message doit maintenant voyager jusqu'à
   `ApplyResult`.
6. **Tests : additifs uniquement.** N'édite aucune des 8 assertions existantes sur `ItemOutcomes`.
   Ajoute de nouvelles assertions sur `ItemFailureReasons` dans les mêmes tests (ou des tests neufs) :
   backup en échec → raison présente et non vide · action inconnue → raison contient l'id de l'action ·
   `res.Error` d'une action qui échoue → raison = ce message exact · item réussi → pas d'entrée dans
   `ItemFailureReasons` · dry-run forcé → comportement inchangé (vérifie si une raison est aussi
   attendue en mode simulation, en cohérence avec ce que fait déjà `ItemOutcomes` en dry-run).
7. **Vérification finale obligatoire** : XML bien formé, 0 erreur `CS1xxx` sur les fichiers App
   touchés, Core **et** Repair vert Debug+Release (viser Repair 156+/156, en ayant ajouté des tests,
   donc probablement plus que 156).

## É — ÉTAPES

1. Confirme la baseline (Core 501/501, Repair 156/156) avant de toucher quoi que ce soit.
2. `Contracts.cs` : ajoute `ItemFailureReasons` à `ApplyResult` (règle N1).
3. `RepairEngine.cs` :
   - `ApplyItem` : fais-la retourner aussi la raison (change son tuple de retour interne, méthode
     privée, aucune contrainte de compat à respecter dessus) — `res.Error` de l'action qui échoue, ou
     `"unknown action {c.ActionId}"` si le registre ne connaît pas l'`ActionId`.
   - `Apply` : au point `outcomes[item.ItemId] = ok;`, si `!ok`, alimente aussi
     `ItemFailureReasons[item.ItemId]` — depuis la raison remontée par `ApplyItem`, ou depuis
     `ex.Message` au point d'échec du backup (L.383).
   - Construis le dictionnaire final et passe-le au `return new ApplyResult { ... }` de fin de méthode
     (et à celui du chemin « bloqué au preflight », vide dans ce cas comme `ItemOutcomes` l'est déjà).
4. `MainWindow.xaml.cs` (App), dans le handler qui construit `RepairApplyStatus.Text` (L.1997-2006
   actuellement) : si `failed > 0`, ajoute une ligne par item échoué avec sa raison, à la suite du
   texte existant (`"{0} applied, {1} failed."` reste le résumé, les raisons s'ajoutent en dessous,
   même patron que `repair.apply.recovery` qui s'ajoute déjà conditionnellement). Pas besoin d'une
   nouvelle clé `Loc.Get` élaborée — un simple préfixe technique suffit (ex. le nom de l'item + `" — "`
   + la raison brute), garde ça simple, ce n'est pas une nouvelle fonctionnalité UI majeure.
5. Étends les tests (règle N6). Vise au moins 5-6 nouveaux tests dans `RepairTests.cs`/
   `RepairSessionTests.cs`.
6. Vérifie tout (§N7), commit unique et propre, annulable d'un `git revert`.
7. Fabrique le bundle, dépose-le, donne les commandes exactes à Maxime dans l'ordre.
8. Mets à jour `TRANSMISSION.md` (bloc du haut) et `knowledge/FIELD-LOG.md`.
9. **Revue CTO + Produit en clôture**, comme sur chaque session : code propre, architecture cohérente,
   tests suffisants, vraie valeur utilisateur, risque technique/commercial, une amélioration à faible
   coût proposée **sans la coder**.

## L — LIVRABLES

- Le champ `ItemFailureReasons` livré, câblé de bout en bout (moteur → App), testé.
- Un bundle déposé + commandes prêtes à copier.
- `TRANSMISSION.md` et `knowledge/FIELD-LOG.md` à jour.
- La revue CTO + Produit, en texte, en fin de message.

### Une chose à savoir sur Maxime

Il travaille sur **Windows**, dépôt à
`C:\Users\User\Desktop\Pincab suite\pincab-toolbox-v0.1.1-alpha-src\pincab-suite`, build avec
`build.cmd`. Chaque aller-retour lui coûte un build complet — une passe complète et vérifiée vaut
mieux que cinq retouches.
