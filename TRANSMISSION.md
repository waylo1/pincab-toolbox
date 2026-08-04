# TRANSMISSION — reprise Pincab Toolbox / FlipSync (session éco)  ·  MAJ 04/08/2026

## 🔒 SCANNER CLOS (03/08, décision Maxime) — l'effort passe sur Repair

**Ne rouvre pas le scanner.** 12 scanners câblés, 35 codes, 100 % traduits FR, tous les codes
Warning/Critical documentés, tous les scanners testés, 128 tests Core verts.

**Règle d'entrée : aucun nouveau check sans DEUX signaux terrain indépendants** (deux utilisateurs,
ou deux forums distincts). Un signal unique se consigne en §2 du FIELD-LOG et attend son deuxième.
Ça vaut aussi pour les idées internes — la fausse alerte KPI#1 du 03/08 le rappelle.

Les 3 derniers chantiers de clôture :
- **`BlockedFileScanner` testé** — c'était le seul scanner sans test, et c'est la détection derrière
  la seule action Repair confirmée deux fois par le terrain. Les 2 décisions pures extraites
  (`SeverityFor`, `IsBlockedZone`), 8 tests. **3ᵉ occurrence du piège `Path.GetFileName` qui ne coupe
  pas sur `\` hors Windows** — split manuel, comme ailleurs dans le code.
- **3 Warnings sans explication documentés** (`COMPAT_SIGNATURE`, `LOW_DISK_SPACE`, `SCANNER_ERROR`).
  Vérif automatisée : plus aucun Warning/Critical sans entrée Knowledge.
- **`ScanReport.Rolled()`** — les findings répétitifs (une ligne PAR TABLE : `ROM_OK`,
  `UPDATE_AVAILABLE`…) se regroupent en une ligne comptée au-delà de 5. **Les Critical ne sont jamais
  regroupés** (300 tables cassées doivent avoir l'air de 300 tables cassées). Groupement par
  (sévérité, code), pas par code seul. `Ordered()` garde tout et sert le rapport texte + le JSON :
  rien n'est perdu, et le message le dit à l'utilisateur. Branché sur écran + HTML + markdown +
  BBCode ; **pas** sur la bannière « FIX THIS FIRST » (elle doit pointer un vrai finding).
- ⚠️ **App non compilable ici (WPF)** : les 4 fichiers App modifiés ont été vérifiés par un **parse
  Roslyn direct** (0 erreur CS1xxx). Ça élimine la faute de frappe, **pas** le besoin d'un
  `build.cmd` sur Windows.

---

## ⏱️ MAJ 04/08 — BLOCAGE #1 LEVÉ : premier build entièrement vert depuis le 30/07 (via CI GitHub)

> **App compile, 128 tests Core + 89 tests Repair verts, publish win-x64 réussi — vérifié de bout
> en bout, pas juste localement.** Chemin emprunté : `build.cmd` local a confirmé que l'App WPF
> compile pour de vrai (Release) et que Repair est vert (89/89) ; les tests Core, eux, sont bloqués
> **en local** par Smart App Control (Windows 11, politique `VerifiedAndReputableDesktop`, pas un
> antivirus — irréversible à désactiver sans réinstallation, donc non traité). Contournement :
> **le code source n'avait en fait jamais été poussé sur `https://github.com/waylo1/pincab-toolbox`**
> (seul un README placeholder y existait) — dépôt initialisé, historiques fusionnés, poussé. La CI
> GitHub (Linux, insensible à Smart App Control) a d'abord révélé deux trous non liés au code :
> l'étape Repair n'existait pas dans `build.yml` malgré une note du 03/08 affirmant le contraire, et
> le job Windows de publish n'avait pas le fallback `-p:RestoreSources=...` que `build.cmd` a
> (NuGet.Config vide les sources par design). Les deux corrigés, repoussés (`68bee0a`) : **run
> entièrement vert, confirmé par Maxime au niveau de chaque job, pas juste du résumé.**
>
> **Conséquence pratique** : le dépôt GitHub fait foi désormais pour la vérification Core+Repair+App
> à chaque session future — pousser les commits fait partie de la routine, pas seulement écrire sur
> le disque local. Smart App Control reste un irritant de dev local sans impact sur la publication.
> Détail complet : FIELD-LOG, entrées du 2026-08-04 (à partir de « Premier build.cmd réel »).
>
> **Piste Mark-of-the-Web infirmée** : `Unblock-File -Recurse` sur toute l'arborescence source n'a
> rien changé, même erreur au mot près. Repair tourne depuis le même dossier sans souci, donc ce
> n'est ni l'emplacement (Desktop) ni le zip source en cause — le blocage vise spécifiquement
> `PincabToolbox.Core.Tests.dll`. Piste la plus probable maintenant : **Smart App Control**
> (fonctionnalité Windows 11). Prochaine étape unique avant de deviner plus loin : lire le nom exact
> de la détection dans Windows Sécurité → Historique de protection. Détail : FIELD-LOG, entrée
> « Unblock-File récursif testé ».
>
> **Dépôt git créé et poussé pour de vrai.** `https://github.com/waylo1/pincab-toolbox` existait
> déjà mais ne contenait qu'un README placeholder — **le code source n'avait jamais été versionné,
> aucune des deux releases publiées n'est passée par un commit git.** `git init` local + fusion
> (`--allow-unrelated-histories`, conflit README résolu) + push : `main` est maintenant à `70fc4e2`
> avec les 150 fichiers du projet. La CI corrigée (Core + Repair) tourne sous Linux et devrait
> donner un vrai résultat sans dépendre de Smart App Control — **à confirmer par Maxime dans
> l'onglet Actions du repo.** Désormais, pousser les commits fait partie de la routine de fin de
> session, pas seulement écrire sur le disque local.
>
> **Cause confirmée par le journal Code Integrity** : c'est le **Contrôle d'application intelligent
> (Smart App Control)**, politique `VerifiedAndReputableDesktop` — pas un antivirus, une politique de
> réputation qui bloque un binaire fraîchement compilé et non signé qu'elle n'a jamais vu. Le
> désactiver est **irréversible sans réinstallation complète de Windows** — décision pour Maxime,
> pas pour cette session.
> ⚠️ **Correction d'une note du 03/08** : « la CI GitHub testait déjà les deux [Core et Repair] »
> était faux, vérifié dans `.github/workflows/build.yml` — seul Core y tournait. Corrigé (étape
> Repair ajoutée). Si le dépôt a un remote GitHub, un `git push` fait tourner la CI sur Linux
> (insensible à Smart App Control) et donne un vrai résultat Core + Repair sans toucher à la
> sécurité de la machine de Maxime.

- Maxime a demandé de « terminer le scanner et Repair » et a donné le feu vert pour coder même
  sans les deux signaux terrain exigés depuis la clôture du 03/08. **Avant de coder au jugé,
  vérification de l'état réel du backlog §2** (même discipline que l'alerte KPI#1) :
  - **`.vpt` invisible dans PinUP** : déjà entièrement codé depuis le 30/07 (`LegacyTableScanner`,
    câblé, traduit, testé) — **seul trou réel : aucune entrée dans `Knowledge.cs`**, alors que
    tous les autres codes en ont une. Corrigé (donnée pure, patron identique aux entrées
    voisines). Détail : FIELD-LOG, entrée du 2026-08-04.
  - **Freezy/zedmd : PAS codé**, malgré le feu vert donné. Ce n'est pas un problème de signal
    manquant, c'est une **cause non confirmée** par l'utilisateur qui a remonté le cas — coder une
    détection dessus serait parier sur une hypothèse, exactement le mécanisme qui a produit la
    fausse alerte KPI#1 du 03/08. Laissé en l'état, signalé plutôt que deviné.
- ⚠️ **Aucun SDK .NET accessible cette session** (ni le sandbox cloud, ni le bridge vers la VM
  Linux de la machine de Maxime n'ont `dotnet`/`csc`). Contrairement à la session du 03/08, même
  pas un parse Roslyn n'a été possible sur l'édition de `Knowledge.cs` — donnée pure, patron
  identique aux entrées existantes, mais **non vérifiée syntaxiquement**. `build.cmd` sur Windows
  reste l'unique moyen de vérifier quoi que ce soit codé depuis le 03/08 (WPF inclus).
- Décision UI Repair (HANDOFF 27/07) **redemandée à Maxime, réponse : pas encore, priorité au
  build.** Toujours non câblée, conforme à la consigne.

---

## ⏱️ MAJ 03/08 (nuit) — alerte KPI#1 infirmée, 2 vrais bugs trouvés, frictions d'achat traitées

> **À lire en premier si tu reprends ici.** Une entrée de FIELD-LOG affirmait que le fix KPI#1
> (« B2S ≠ signal ROM ») était documenté comme livré mais absent du code. **C'est faux** — vérifié
> dans les sources, dans les tests, et jusque dans l'exe livré. En vérifiant, deux vrais défauts
> ont été trouvés, plus deux failles commerciales dans le moteur Repair. Tout est corrigé.

- ❌ **Alerte KPI#1 INFIRMÉE.** `ScriptAnalyzer.cs` a bien deux regex séparées ; le test « B2S-only +
  `Const cGameName` résolvable » a été écrit **avant** toute modif et **passait déjà** ; la chaîne
  `uses a B2S backglass but does not drive VPinMAME` est présente **dans le binaire du 30/07**
  (celui des 65 téléchargements). **Leçon** : une entrée qui dit « ce fix manque » se vérifie dans le
  code ET dans le binaire avant d'être crue — re-coder un fix déjà présent aurait cassé du code sain.
- 🐛 **Vrai bug #1 — garde d'entrée `RomValidatorScanner`.** Elle lisait `!UsesController && !UsesB2S` :
  B2S restait donc structurellement un **signal d'entrée équivalent** au contrôleur, et une table
  B2S-only n'échappait à la validation ROM que grâce à un `else if` en aval. Effet observable : une
  originale B2S dont le `cGameName` existe par hasard dans le dossier roms sortait étiquetée
  **`ROM_OK`**. Corrigé : `UsesController` est le **seul** signal d'entrée, décision à un seul endroit.
- 🐛 **Vrai bug #2 — les commentaires VBScript comptaient comme du code.** Les regex `CreateObject` ne
  sont pas ancrées : une ligne **commentée** `' Set Controller = CreateObject("VPinMAME.Controller")`
  valait signal ROM. Or les originales sont massivement bâties sur un template de table à ROM dont la
  plomberie est **commentée plutôt que supprimée** → `ROM_MISSING` critique sur une vraie originale.
  **C'est très probablement le mécanisme derrière la liste de Gregg.** Corrigé
  (`ScriptAnalyzer.StripComments`, gère `'` et `REM`, conscient des littéraux pour ne pas casser sur
  une apostrophe type « Rocky & Bullwinkle's »).
- 💰 **Faille commerciale #1 — `RepairModeResolver`.** Les portes s'évaluaient commercial → sécurité,
  donc une règle de confiance < 70 donnait `Locked` **sans** licence (« un correctif existe, achète »)
  et `ManualOnly` **avec** licence (« débrouille-toi »). Le gratuit vendait un correctif que l'achat
  ne délivrait pas. Corrigé : **sécurité avant commercial**. Test exhaustif sur les 101 valeurs de
  confiance × réversible/non : tout `Locked` devient forcément actionnable une fois licencié.
- 💰 **Faille commerciale #2 — items sans changement.** Une action qui `Plan()` zéro changement (échec
  propre volontaire) gardait `Mode = Locked` : après achat, l'item ne faisait rien. Corrigé →
  `ManualOnly` + une entrée `Missing` qui dit pourquoi.
- 🆕 **`RepairOffer`** (`src/PincabToolbox.Repair/Engine/RepairOffer.cs`) — la surface d'offre agrégée
  qui manquait : combien de problèmes une licence règle vraiment, lesquels restent manuels,
  réversibilité et backup **unanimes ou faux**, durée, et **ce que Repair ne fera pas**, affiché AVANT
  l'achat. `RepairOffer.From` **refuse un plan licencié** (`ArgumentException`) → ADR-006 devient une
  contrainte de type, plus une convention. **Ce n'est PAS de l'UI** : c'est la surface moteur sur
  laquelle l'UI se branchera, testable ici, ce qui réduit le futur câblage à une liaison de données.
- 🔇 **Bruit UpdateWatcher** — `TableVariantDetector` : les mods (`MOD`/`BIGUS`/`BIGGUS`, tokens
  entiers) ne sont plus comparés à la table de base et sont **comptés dans le résumé**. Répond à Chad
  et à Gregg d'un coup. Volontairement étroit : classer à tort une table de base **cacherait une vraie
  mise à jour**, ce qui coûte plus cher qu'une ligne de bruit.
- 🟡 **Lien direct VPS** — id VPS exposé + `UpdateSource.GameUrlTemplate` (`{id}`), **laissé vide** :
  le front VPS a changé d'hôte et le format de route n'a pas pu être confirmé. **Action Maxime :
  ouvrir une fiche table sur le site, coller le format dans `profiles/vpx-popper.json`.**
- 📋 **§2 du FIELD-LOG mentait sur son propre état** : « score trompeur », « roms multi-lecteur » et
  « check espace disque » étaient marqués à faire alors qu'ils sont livrés depuis le 30/07. Corrigé.
- ✅ **201 tests verts** (112 Core + 89 Repair), Debug ET Release. Pack : schéma OK, règles métier OK
  (3 avertissements connus, inchangés), 12/12 garde-fous du selftest.
- ⚠️ **Écrasement disque** : la réécriture des fichiers sur la machine de Maxime a écrasé
  `knowledge/FIELD-LOG.md`, dont les 2 entrées du 03/08 ajoutées entre-temps. **Elles ont été
  reconstruites** (titres conservés, contenu réécrit) et sont marquées ⚠️ ENTRÉE RECONSTRUITE. Le
  verbatim de Gregg et sa liste exacte de tables sont **perdus, à redemander**.

---

## ⏱️ MAJ 03/08 (soir) — reste du backlog §2 codé (feu vert Maxime), Repair engine étendu
**Feu vert explicite de Maxime : coder tout le reste du backlog §2 (scanner + candidats Repair),
même hors signal de demande — la règle "on attend un signal" ne s'applique plus. Fait, moteur
Repair étendu (pas refait) : 61 tests + 2 actions existants intacts, + 18 tests + 3 actions.**

- ✅ **Scanner** (3 nouveaux checks, actifs dès le prochain build) : `PinupDisplayZombieScanner`
  (`PINUP_DISPLAY_ZOMBIE`, Warning — process actif sans table active), `DisplaySetupScanner`
  (`DISPLAY_SETUP_INCOMPLETE`, Info — composant b2s/DMD présent mais <2 écrans connectés ;
  **portée réduite** vs l'idée d'origine "ordre des écrans" — le mapping écran↔rôle vit dans la
  config PinUP Popper, schéma non documenté, pas reconstruit pour ne pas deviner), `OrphanedMediaScanner`
  (`ORPHANED_MEDIA_FILE`, Info — médias POPMedia/PUPVideos sans table correspondante, biaisé pour
  ne PAS signaler, régression testée contre l'incident communautaire des fichiers `(SCREENx)`).
- ✅ **Repair** (3 nouvelles `IRepairAction`, testées, ADR-005/006 respectés) : `kill_zombie_pinup_display`
  (non réversible assumé, jamais Automatic), `set_default_audio_device` (réversible, **pas encore
  relié à un Finding** — pas de detection statique possible, surface de déclenchement à décider avec
  Maxime), `quarantine_orphaned_media` (déplace en quarantaine locale, jamais suppression).
- ⚠️ **`RealAudioDeviceControl` passe par l'API COM non documentée `IPolicyConfig`** (celle que NirCMD
  utilise en interne — pas d'API publique Windows pour changer le device par défaut). Non vérifiable
  en sandbox Linux, potentiellement cassée sur Windows 11 (l'interface a déjà changé une fois côté
  Microsoft). **À tester sur cab réel avant toute release**, comme prévu pour tout code Windows à
  effet d'écriture (kill process, nettoyage média : mêmes réserves, un cran en dessous en risque).
- 🐛 **Bug trouvé et corrigé en cours de route** : l'exemption du garde-fou "process bloquant" (pour
  laisser `kill_zombie_pinup_display` tuer PinUpDisplay malgré sa présence dans la liste des process
  bloquants) utilisait `Path.GetFileNameWithoutExtension`, qui ne reconnaît pas `\` comme séparateur
  hors Windows — corrigé avec un split manuel, capturé par un test avant toute livraison.
- ✅ `build.cmd` ne lançait QUE les tests Core (jamais Repair, alors que la CI GitHub testait déjà les
  deux) — corrigé, lance maintenant les deux avant le publish.
- **UI Repair toujours NON câblée** (décision HANDOFF du 27/07, à reconfirmer avant tout câblage —
  pas fait cette session, conforme à la consigne). Détail technique complet : FIELD-LOG, entrée du
  2026-08-03 (soir).
- **`PincabToolbox.App` (WPF, `net8.0-windows`) n'a pas pu être compilé dans ce sandbox Linux** —
  Core et Repair (+ leurs tests) si, et sont 87/87 et 79/79 verts en Debug ET Release. Un
  `build.cmd` complet sur Windows reste à faire avant toute release, en particulier pour valider
  `RealAudioDeviceControl`.

---

## ⏱️ MAJ 30/07 (soir) — v0.1.1 codée & buildée
**5 chantiers scanner faits, testés (build vert, 72 tests dont 15 de non-régression), livrés dans les sources :**
- ✅ **#1 Score global trompeur** — infos ne comptent plus comme défauts, warnings plafonnés (un volume ne fait plus tomber en F), « FIX THIS FIRST » réservé aux criticals. (`ScanReport.cs`, `CompatibilityScanner.cs`→COMPAT_MIN_VERSION passé en Info, `MainWindow.xaml.cs`, `Loc.cs`)
- ✅ **#2 FP `ROM_MISSING` (KPI #1)** — B2S.Server ≠ signal ROM ; seul `VPinMAME.Controller` l'est. Originales/homebrew à backglass plus flaguées critique. (`ScriptAnalyzer.cs`, `RomValidatorScanner.cs`)
- ✅ **#3 FN roms multi-lecteur** — lecture registre VPinMAME `rompath` (P/Invoke, Core sans dépendance). (`VpinmameRegistry.cs` NEW, `LayoutDetector.cs`)
- ✅ **Check espace disque** `LOW_DISK_SPACE` (`DiskSpaceScanner.cs` NEW) · ✅ **`.vpt` legacy** informatif (`LegacyTableScanner.cs` NEW).

**Reste = tranche Repair/écrans → v0.2, à coder ET TESTER SUR CAB RÉEL (décision Maxime).** Code Windows à effet d'écriture (kill process, audio COM, suppression PinupSystem, énum écrans), invérifiable en sandbox — ne PAS bundler dans une release annoncée sans test cab. Détail des 4 items : FIELD-LOG §2.

**Release/comms** : notes de release + posts MAJ FR/EN rédigés (`marketing/RELEASE-NOTES-*` à créer depuis le fichier livré). Asset release = **le même `.zip`** (exe + profiles/ + DemoData/), la dette « exe unique embarqué » n'est PAS faite. Landing : badge « Lancement d'abord sur Pincab Passion » retiré (fichier `flipsync-site/landing/index.html` édité) — **reste à faire : `npx vercel --prod`**.

---

> **But de la PROCHAINE session (mis à jour 04/08) : (A) build.cmd — FAIT, formellement vérifié via
> CI (voir encadré MAJ 04/08 ci-dessus). Reste, dans l'ordre : (B)** **répondre à Gregg** — le fix
> des commentaires VBScript devrait déjà faire disparaître une partie de sa liste, donc lui proposer
> de **relancer le scan avec le nouveau build avant** d'investiguer plus loin, et lui redemander sa
> liste exacte (elle a été perdue, cf. l'avertissement en haut) ; **(C)** décider du câblage de l'UI
> Repair sur `RepairOffer` (HANDOFF du 27/07, redemandé le 04/08 — réponse : pas encore, priorité au
> build, donc **toujours à trancher**) ; **(D)** tester `kill_zombie_pinup_display`,
> `set_default_audio_device` et `quarantine_orphaned_media` sur un cab réel —
> `RealAudioDeviceControl` en particulier (COM non documenté, jamais exécuté hors sandbox, potentiel
> souci Windows 11) ; **(E)** reboucler avec FD (son cas roms multi-lecteur est corrigé depuis le
> 30/07, jamais confirmé auprès de lui) ; **(F)** coller le format d'URL VPS dans
> `profiles/vpx-popper.json` (une ligne JSON, Maxime doit ouvrir une fiche table sur le site).
> Reste bloqué sans action : résidus Freezy/zedmd (cause pas confirmée par l'utilisateur — **ne pas
> deviner**, cf. FIELD-LOG 04/08). Hors scope assumé : support Future Pinball. **L'annonce
> (`marketing/ANNONCE-maj-et-repair.md`) est prête et volontairement en attente — Maxime décide
> seul quand il la publie, ne pas la relancer.**
> **Ne relis PAS toute la doc.** Source principale : `knowledge/FIELD-LOG.md` (§1 = retours détaillés
> avec analyse et recommandation, §2 = backlog priorisé — entrée technique complète du chantier de
> code datée du 2026-08-03 soir). Au besoin seulement en plus : `HANDOFF.md`, `docs/SUCCESS-METRICS.md`.
> Pour retoucher du code Repair, lire aussi **ADR-005** (registre fermé / confinement InstallLayout)
> et **ADR-006** (dry-run gratuit) — cités plusieurs fois dans le FIELD-LOG comme garde-fous à respecter.
> Pour la **gestion communauté** (réponses aux commentaires) : `marketing/FAQ-objections.md`
> suffit, **NE charge PAS le code source** pour une tâche de rédaction/réponse.
> Modèle recommandé : **Sonnet**.

## But de cette session (décidé par Maxime le 30/07)
**On sort de la fenêtre des 48h critiques du lancement (règle "on consigne, on ne code pas" levée).**
Objectif n°1 : **améliorer le scanner** à partir des retours terrain déjà analysés et priorisés
dans `knowledge/FIELD-LOG.md`. Tout est déjà dégrossi (cause probable identifiée, recommandation
donnée) — pas besoin de repartir de zéro, juste dérouler dans l'ordre ci-dessous.

### Chantiers prêts à coder, par priorité
1. **🔴 Score global trompeur ("0/100 · F" + "Install in bad shape" + "FIX THIS FIRST" alors que 0 critical)**
   — confirmé par le rapport complet de FD (grosse collection 2090 tables, aucun problème réel) et sa
   capture d'écran de l'app. Le score/les libellés comptent des "info" (mises à jour dispo) et des
   warnings mineurs comme si c'était grave. **Impact le plus large** : touche systématiquement toute
   grosse collection bien tenue, pas un cas isolé. Détail complet + verbatims dans FIELD-LOG (entrées
   du 30/07). Revoir formule de score et/ou séparer "problèmes réels" (warning+critical) de "informations".
2. **FP `ROM_MISSING` sur tables originales/homebrew sans ROM** (KPI #1, ex. Guardians of the Galaxy,
   Harry Potter homebrew) — RomValidatorScanner/ScriptAnalyzer ne distinguent pas "doit avoir une ROM"
   de "originale, pas de ROM attendue". Piste de donnée : catalogue Orbitalpin.com (tables originales,
   cite justement Harry Potter) en complément de la base VPS déjà utilisée (VpsDatabase.cs).
3. **FN `ROM_FOLDER_NOT_FOUND_MULTIDRIVE`** — le module `rom` skip tout le contrôle si VPinMAME est sur
   un lecteur différent du dossier Tables (cas confirmé : Tables sur E, VPX sur D). Piste : lire le
   registre VPinMAME (`HKEY_CURRENT_USER\Software\Freeware\Visual PinMame\globals`, valeur `rompath`)
   plutôt qu'un chemin relatif fixe.
4. **Repair — nettoyage `PinUpDisplay.exe` zombie** — action simple et sûre (terminer un processus),
   origine VPForums. Bon premier `IRepairAction` à ajouter.
5. **Repair — définir le périphérique audio par défaut** — **décision prise : action ponctuelle à la
   demande** (pas de script Startup persistant), cohérent avec le modèle `IRepairAction` existant.
6. **Détection informative "ordre/résolution écrans incohérent avec le profil PinUP"** — **PAS de
   correctif registre** (hors confinement ADR-005, geste physique non automatisable) : finding
   informatif + lien FAQ seulement.
7. Reste du backlog (voir FIELD-LOG §2 pour le détail) : nettoyage dossier PinupSystem (dry-run +
   backup obligatoires — un script communautaire a déjà supprimé des fichiers par erreur), tables
   `.vpt` legacy invisibles dans la recherche PinUP (finding informatif, ne PAS suivre le raccourci
   déconseillé par l'éditeur PinUP), check générique d'espace disque, résidus upgrade Freezy/zedmd
   (DMD noir, E0434352).

### Cas ouverts à suivre (pas encore de code à écrire)
- **Harley Kirkegard (FB)** : ".json error" au lancement, précisions demandées (message exact / capture),
  pas encore répondu par lui. Hypothèse la plus probable : zip mal extrait (exe seul, sans `profiles/`+
  `DemoData/`) — variante du bug packaging du 29/07. Ne pas coder tant que la cause n'est pas confirmée.
- **Demande Future Pinball** (Donald Parker, FB) — hors scope actuel (moteur `.fpt` différent de VPX),
  répondu honnêtement, gardé en veille de demande, pas d'engagement de date.

## État (J+2, sortie de la fenêtre critique)
- Scanner **v0.1.0-alpha LANCÉ**, `PincabToolbox.zip` (exe + profiles/ + DemoData/) sur la release GitHub
  `waylo1/pincab-toolbox`. Landing **pincab-toolbox.vercel.app** live.
- **Traction J+1/J+2** : ~40 téléchargements sur le zip repackagé + 20+ sur l'ancien exe nu (avant retrait) —
  bon signal, relevé fait en fin de journée donc sous-estimé.
- **Communauté** : posts en ligne sur Pincab Passion (recommandé par l'admin, section Logiciels divers),
  VPForums.org et PinballNirvana.com (priorisés après repérage — VPDB.io/Pinsimdb.org/Orbitalpin.com
  écartés comme cibles de post, pas des forums). Post FB "Visual Pinball Addicts" toujours actif (20 likes,
  plusieurs commentaires traités : FP ROM_MISSING assumé, rapport complet FD analysé, question Future
  Pinball répondue, cas .json ouvert).
- **KPI #2 anonymisation : 0 incident** — path-scrubbing confirmé fonctionnel sur le rapport réel de FD.

## Tâches en attente
1. ✅ **Retouche landing FAITE** (30/07 soir) : badge « Lancement d'abord sur Pincab Passion » retiré dans
   `flipsync-site/landing/index.html`, « Développé avec la communauté » gardé. **Reste : `npx vercel --prod`** (déploiement = Maxime).
2. **Dette v0.1.1 (à froid)** : ré-embarquer profil + démo dans un exe unique pour supprimer le format zip. Toujours ouverte.
3. **v0.2 — tranche Repair/écrans : CODÉE (03/08), reste À TESTER SUR CAB RÉEL avant release** (voir MAJ
   du haut + FIELD-LOG §2) : kill PinUpDisplay, audio par défaut (COM non documenté, pas encore relié
   à un Finding), nettoyage PinupSystem (dry-run+backup obligatoires, jamais de suppression), détection
   informative écrans (signal de compte, pas d'ordre). UI Repair toujours non câblée (HANDOFF à
   reconfirmer). + check résidus Freezy/zedmd **toujours bloqué tant que la cause n'est pas confirmée
   par l'utilisateur**.
4. **Répondre à Gregg** (FB « Virtual Pinball and VPin Cab Builders ») — sa liste exacte de tables a été
   perdue dans l'écrasement disque, à redemander. Lui proposer de **relancer le scan avec le prochain
   build** : le fix des commentaires VBScript devrait déjà faire tomber une partie de ses « criticals ».
   Cas ouverts à trancher avec lui : **Rocky & Bullwinkle** (vraie table à ROM `rab_*` sauf re-thème,
   donc critical probablement correct) et le **B2S Bigus(MOD)**.
5. **Répondre à Chad Greenaway** — sa demande de filtre mods est **faite**, celle du lien direct VPS est
   à moitié faite : il manque le format d'URL. **Ouvrir une fiche table sur le site VPS, copier le
   format, le coller dans `gameUrlTemplate` de `profiles/vpx-popper.json`** — une ligne, pas un rebuild.
6. **Confirmer à FD** que le cas roms multi-lecteur est résolu (fix livré le 30/07, jamais reboucler avec
   lui alors que c'est lui qui l'a remonté).

## Règles absolues (rappel)
Lecture seule · zéro télémétrie · rien de sur-promis · feu vert avant toute action publique. Détail : HANDOFF.

## Consignes ÉCO (important)
- S'appuyer sur cette transmission + `knowledge/FIELD-LOG.md`, **pas** de relecture massive du reste.
- Pour coder les chantiers Repair listés ci-dessus, lire ADR-005 et ADR-006 (courts, ciblés) plutôt que
  toute la doc d'architecture.
- Communauté/rédaction = **ne pas ouvrir le code, ne pas piloter le PC**.
- Éviter les grosses captures quand le texte suffit ; automatisation desktop seulement si vraiment nécessaire.
- **Piste demandée par Maxime** : Obsidian (+ graph/« graphify ») pour centraliser le savoir projet en
  **UN index concis** que Claude lit au lieu de N docs. Toujours à explorer, pas fait cette session.
