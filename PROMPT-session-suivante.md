# Prompt de reprise — session suivante (méthode K.E.R.N.E.L)

*Copier-coller le bloc ci-dessous tel quel au démarrage de la nouvelle session.*

---

## K — CONTEXTE

Pincab Toolbox / FlipSync, micro-entreprise **MC Automation**. Application C#/.NET 8 pour
propriétaires de pincab (Visual Pinball X + PinUP Popper) : un **scanner gratuit** qui diagnostique
une installation, et un **moteur Repair payant** qui la répare — Repair existe mais n'est pas encore
branché dans l'interface.

Sources sur le disque :
`C:\Users\User\Desktop\Pincab suite\pincab-toolbox-v0.1.1-alpha-src\pincab-suite`

Version publique actuelle : **v0.1.1-alpha, build du 30/07**, 65 téléchargements. Elle ne contient
**rien** de ce qui a été codé les 03/08.

Découpage : `PincabToolbox.Core` (net8.0, zéro dépendance, scanners) · `PincabToolbox.Repair`
(net8.0, zéro dépendance, moteur) · `PincabToolbox.App` (net8.0-**windows**, WPF, **ne compile pas
en sandbox Linux**).

Docs de reprise, dans cet ordre et **rien d'autre** : `TRANSMISSION.md` (les 3 encadrés du haut
suffisent), puis `knowledge/FIELD-LOG.md` (§1 = retours terrain, §2 = backlog). Pour toucher au code
Repair, lire en plus **ADR-005** (registre d'actions fermé) et **ADR-006** (dry-run gratuit), courts.
**Mode éco : ne relis pas le reste de la doc.**

## E — ENJEU

Le scanner est **clos et gelé**. Le seul enjeu maintenant : **débloquer la publication**, puis
avancer Repair vers l'état achetable. Rien de ce qui a été codé n'est entre les mains des
utilisateurs tant que le build Windows n'est pas fait.

## R — RÔLE

Développeur C#/.NET senior **et** garde-fou produit. La leçon de la session précédente est à
appliquer littéralement : **vérifier avant de croire**. Une note interne affirmait qu'un correctif
livré était absent du code — c'était faux, vérifié dans les sources, dans les tests **et dans le
binaire publié**. Une affirmation « ce fix manque » se vérifie à ces trois niveaux avant d'être crue,
sinon on re-code un correctif déjà présent et on casse du code sain. En vérifiant, deux vrais bugs
et deux failles commerciales sont tombés — la vérification paie même quand l'alerte est fausse.

## N — INTERDITS (non négociables)

1. **Ne PAS rouvrir le scanner.** Il est gelé (encadré en tête du §2 du FIELD-LOG). Règle d'entrée :
   aucun nouveau check sans **deux signaux terrain indépendants** (deux utilisateurs, ou deux
   forums distincts). Un signal unique se consigne en §2 et attend son deuxième. Ça vaut aussi pour
   les idées internes.
2. **Ne PAS câbler l'UI Repair sans redemander à Maxime.** Décision HANDOFF du 27/07, jamais
   reconfirmée, demandée deux fois déjà.
3. **Ne PAS publier l'annonce.** Elle est rédigée et validée (`marketing/ANNONCE-maj-et-repair.md`,
   4 versions : Facebook EN/FR, Pincab Passion FR long, VPForums/Nirvana EN long). **Maxime la garde
   volontairement de côté** — il postera quand il aura décidé. Ne pas la relancer, ne pas la
   réécrire sans demande.
4. **Aucun téléchargement de ROM ni de table**, jamais (ADR-004). Repair ne résoudra jamais une ROM
   réellement manquante, et c'est assumé publiquement.
5. **Build + tous les tests verts avant toute livraison** (`build.cmd` lance Core ET Repair).
6. **Re-stager un fichier depuis le disque avant de le réécrire.** Une réécriture a déjà écrasé des
   entrées de FIELD-LOG ajoutées entre-temps — données perdues.
7. Consigner chaque chantier dans `knowledge/FIELD-LOG.md` au fur et à mesure.

## E — ÉTAT : ce qui a été fait, ce qui bloque, ce qui reste

### Fait (03/08, non publié)

**Deux vrais bugs KPI#1** (faux « ROM manquante » sur originales/homebrew) :
`RomValidatorScanner` faisait de B2S un signal d'entrée équivalent au contrôleur VPinMAME →
`UsesController` est désormais le seul. Et surtout : **les commentaires VBScript comptaient comme du
code** — une ligne `' Set Controller = CreateObject("VPinMAME.Controller")` valait signal ROM, or les
originales sont bâties sur des templates de tables à ROM dont la plomberie est commentée plutôt que
supprimée. `ScriptAnalyzer.StripComments` ajouté. **C'est très probablement le mécanisme derrière la
liste de « criticals » de Gregg.**

**Deux failles commerciales Repair**, même nature : le gratuit promettait plus que le payant.
`RepairModeResolver` évaluait commercial → sécurité, donc une confiance < 70 donnait `Locked` sans
licence et `ManualOnly` avec (on vendait un correctif que l'achat ne délivrait pas) → **sécurité
avant commercial**. Et un item dont l'action planifie zéro changement gardait `Mode = Locked` →
passe en `ManualOnly` avec un `Missing` qui dit pourquoi.

**`RepairOffer`** (`src/PincabToolbox.Repair/Engine/RepairOffer.cs`) : surface d'offre agrégée
gratuite — combien de problèmes une licence règle vraiment, ce qui reste manuel, réversibilité et
backup **unanimes ou faux**, durée, et ce que Repair **ne fera pas**, affiché avant l'achat. Refuse
un plan licencié par `ArgumentException` : ADR-006 devient une contrainte de type. **C'est la surface
sur laquelle l'UI se branchera.**

**Bruit UpdateWatcher** : `TableVariantDetector` — les mods (`MOD`/`BIGUS`/`BIGGUS`, tokens entiers)
ne sont plus comparés à la table de base et sont comptés dans le résumé. Répond à Chad et à Gregg.

**Clôture du scanner** : `BlockedFileScanner` testé (ses 2 décisions extraites en pur : `SeverityFor`
et `IsBlockedZone`) — c'était le seul scanner sans test, et c'est la détection derrière la seule
action Repair confirmée deux fois par le terrain. Trois Warnings muets documentés
(`COMPAT_SIGNATURE`, `LOW_DISK_SPACE`, `SCANNER_ERROR`). **`ScanReport.Rolled()`** : les findings
répétitifs (une ligne PAR TABLE) se regroupent en une ligne comptée au-delà de 5 — **les Critical ne
sont jamais regroupés**, groupement par (sévérité, code) et non par code seul, et `Ordered()` garde
tout pour le rapport texte et le JSON.

**§2 du FIELD-LOG corrigé** : « score trompeur », « roms multi-lecteur » et « check espace disque »
y étaient marqués à faire alors qu'ils sont livrés depuis le 30/07.

**Tests : 128 Core + 89 Repair, verts en Debug ET Release, zéro warning.** Pack de connaissance
validé (schéma + règles métier, 3 avertissements connus inchangés), 12/12 garde-fous du selftest.

### Blocages à débloquer, par ordre

1. 🔴 **BLOQUANT — `build.cmd` sur Windows.** Quatre fichiers WPF ont été modifiés sans jamais être
   compilés (`MainWindow.xaml.cs`, `Knowledge.cs`, `Localization/Loc.cs`). Ils ont été vérifiés par
   un **parse Roslyn direct** (`csc.dll` du SDK) : zéro erreur de syntaxe — ça élimine la faute de
   frappe, **pas** le besoin d'un vrai build. **Rien ne peut être publié avant.** Premier geste de
   la session si Maxime est sur sa machine Windows.
2. 🔴 **BLOQUANT — validation sur cab réel** des actions à effet d'écriture Windows, en particulier
   `RealAudioDeviceControl` : elle passe par l'interface COM **non documentée** `IPolicyConfig`
   (celle que NirCMD utilise en interne — aucune API publique n'existe), jamais exécutée hors
   sandbox, **potentiellement cassée sur Windows 11**. Idem pour le kill de process et le nettoyage
   média, un cran en dessous en risque.
3. 🟡 **DÉCISION MAXIME — câbler l'UI Repair sur `RepairOffer` ?** Le moteur est prêt et la surface
   d'offre existe ; il ne manque que la liaison de données. C'est le dernier verrou pour que Repair
   soit vendable. **Demander avant de coder.**
4. 🟡 **Format d'URL VPS.** `gameUrlTemplate` est vide dans `profiles/vpx-popper.json` : le front VPS
   a changé d'hôte (`virtual-pinball-spreadsheet.web.app` redirige vers
   `virtualpinballspreadsheet.github.io`) et la route n'a pas pu être confirmée — un lien faux est
   pire que pas de lien. Maxime ouvre une fiche table, copie le format, le colle. Une ligne de JSON.
5. ⚠️ **Donnée perdue à redemander** : la liste exacte de tables de Gregg et son verbatim ont été
   écrasés lors d'une réécriture disque. Les 2 entrées du FIELD-LOG ont été reconstruites et sont
   marquées « ⚠️ ENTRÉE RECONSTRUITE ».
6. ⛔ **Freezy/zedmd** : toujours bloqué, cause non confirmée par l'utilisateur. Ne pas coder.

### Reste à faire, dans l'ordre

- **A.** `build.cmd` complet sur Windows, puis republier le zip.
- **B.** **Répondre à Gregg** (FB « Virtual Pinball and VPin Cab Builders »). Lui proposer de
  **relancer un scan avec le nouveau build avant** d'investiguer sa liste — le correctif des
  commentaires devrait en faire tomber une partie. Redemander la liste exacte. Cas ouverts :
  **Rocky & Bullwinkle** (vraie table à ROM `rab_*` sauf re-thème → le critical est probablement
  correct) et le **B2S Bigus(MOD)**.
- **C.** **Répondre à Chad Greenaway** : le filtre mods est fait, le lien direct VPS attend le format
  d'URL (blocage 4).
- **D.** **Reboucler avec FD** : son cas « roms sur un autre lecteur » est corrigé depuis le 30/07 et
  il n'a jamais eu de retour, alors que c'est lui qui l'a remonté.
- **E.** Décider du câblage UI Repair (blocage 3), puis le faire si feu vert.
- **F.** Tests cab réel (blocage 2).
- **G.** L'annonce est prête et **volontairement en attente** — Maxime décide quand.

## L — LIVRABLE ATTENDU

Selon ce que Maxime demande en début de session. Par défaut, s'il ne précise rien : **commencer par
lui demander où il en est du build Windows** (blocage 1), parce que tout le reste en dépend.

Toute livraison de code : build vert, tous les tests verts en Debug et Release, entrée dans
`knowledge/FIELD-LOG.md`, mise à jour de `TRANSMISSION.md`, puis écriture des fichiers sur le disque
de Maxime — **en re-stageant chaque fichier avant de le réécrire**.
