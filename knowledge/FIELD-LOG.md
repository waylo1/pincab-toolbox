# FIELD-LOG — Journal de terrain Pincab Toolbox

*L'instrument manuel qui alimente `docs/SUCCESS-METRICS.md`. Chaque retour de la communauté entre ici, rattaché à un **code** de finding (la clé de jointure du moteur). Rien qui n'y figure ne « compte ». Process complet : la note de session `PROCESS-capture-retours.md`.*

**Règle d'anonymisation :** si tu colles un extrait de rapport ou un chemin, retire le nom de compte Windows avant de l'enregistrer (ADR-003) — même si l'app scrubbe déjà, un utilisateur peut coller un chemin brut.

**Règle de lancement :** un faux négatif ou une idée de nouveau check → on **consigne**, on ne code pas pendant les 48 h critiques (un faux positif ajouté juste après le post tue la crédibilité).

---

## Modèle d'entrée (copier-coller)

```
## AAAA-MM-JJ · [forum #id](lien-du-message)
- code:        ROM_MISSING          (code de finding concerné, ou "NOUVEAU" si à créer)
- bac:         FP | FN | WORDING | FIX | FEATURE
- contexte:    VPX 10.8 64-bit / Popper / table "Xxx (Bally 199x)"
- verbatim:    « ... » (anonymisé)
- analyse:     ...
- disposition: ... · répondu ✔ / à répondre
```

Bacs : **FP** faux positif · **FN** panne ratée · **WORDING** message pas clair · **FIX** résultat d'un correctif (« a marché / n'a pas marché ») · **FEATURE** demande.

---

## 1. Retours (rapports, FP, FN, wording, résultats de fix)

## 2026-08-20 · Maxime, testeur sur son propre cab réel — onglet Repair noyé sous 728 items, historique sans détail, demande d'automatisation
- code:        aucun code de finding — comportement de `RepairEngine.Plan()` et de `RepairSession.Summarize()`
- bac:         FN (bruit qui masque les vrais problèmes) + FEATURE (détail historique) + FEATURE (automatisation)
- contexte:    export Scanner réel (`pincabtoolboxreport202608201335.html`, score 38/100 F, 8 critical/113 warning/6 note/371 info/230 ok = 728 findings) puis capture d'écran de l'onglet Repair du même cab
- verbatim:    « la j'ai pas la case tout coché déja se serait super utile, ya beaucoup la meme chose » · « dans réparé j'ai pas le detail de la réparation d'avant, et ce serais bien de trouver comment reparer automatiquement ce qui est actuellement qu'en manuel »
- analyse:     (1) case « tout sélectionner » cachée : PAS un bug, `ChkRepairSelectAll.Visibility` est lié à `_repairItemRows.Any(r => r.CanApply)`, faux chez lui car aucun de ses findings ne correspond à une action câblée. (2) « beaucoup la même chose » : bug réel confirmé — `RepairEngine.Plan()` transformait CHAQUE finding en item Repair sans filtre de sévérité (728 findings Scanner = 728 « étapes manuelles » Repair, calcul vérifié à l'unité près), noyant les 8 vrais critiques sous 230 `ROM_OK`/`COM_OK` (Ok, « tout va bien ») et 371 Info. (3) historique « Réparé » sans détail : bug réel confirmé — le journal enregistre déjà `ActionId`+`Before`+`After` par changement (`PlannedChange`), mais `RepairSession.Summarize()` les jetait et ne gardait que le nom de fichier. (4) demande d'automatisation : ses items manuels réels (`COM_STALE_PATH` ×2, `COM_BITNESS_GAP` ×2, `CHAIN_BITNESS_GAP` ×2, `SCREENRES_UNPARSED`, `GLOBALCONFIG_B2S_MISSING`, `SCRIPT_HARDCODED_PATH` ×3) n'ont AUCUNE règle dans `pack-2026.08.json` — seuls `BLOCKED_DLL`, `ROM_UNZIPPED`, `PINUP_DISPLAY_ZOMBIE`, `ORPHANED_MEDIA_FILE` sont câblés. Cas particulier : `COM_BITNESS_GAP` a déjà une action écrite et testée (`RegisterComComponentAction`, LOT I) mais délibérément non activée dans le pack (voir entrée du 19/08 juste en dessous) — les deux autres inconnues qui la retiennent (chemin de l'outil de ré-enregistrement jamais vérifié sur un vrai cab, confirmation honnête déjà résolue le 19/08) restent entières. Les 5 autres codes n'ont aucune action écrite du tout — `SCRIPT_HARDCODED_PATH` en particulier réécrirait le script d'une table, risque bien plus élevé que les actions actuelles (fichier utilisateur non remplaçable si mal réécrit).
- disposition: ✅ codé et testé le 20/08 pour (2) et (3) — `RepairEngine.Plan()` exclut désormais les findings Severity.Ok et Info (Note/Warning/Critical restent surfacés, décision Maxime : filtre Ok+Info, 728 → 127 items attendus sur son cab), 1 test dédié (`Test_Plan_OkAndInfoFindings_ProduceNoItems_NoteAndAboveStillDo`). `PlanSummary` porte maintenant `ChangeDetails` (ActionId+Target+Before+After par changement), `BuildPlanSummaryText` (App) affiche une ligne par changement avec un libellé localisé FR/EN/ES au lieu d'une simple liste de noms de fichiers, repli sur le texte brut si l'ActionId n'a pas de traduction. 164 tests Repair verts (build+run confirmés en sandbox). **Partie App/WPF non vérifiable en sandbox (pas de compilateur Windows ici) — à confirmer par `build.cmd` avant de faire confiance à l'affichage.** (1) pas un bug, expliqué à Maxime. (4) pas codé — proposé comme chantier séparé, priorisé par risque (COM_BITNESS_GAP = activer une règle existante après vérif terrain ; SCRIPT_HARDCODED_PATH = nouvelle action, risque plus élevé, pas commencé).

## 2026-08-20 (suite, même jour) · register_com_component activée — feu vert explicite de Maxime, test réel demandé
- code:        VPINMAME_NOT_REGISTERED, COM_NOT_REGISTERED, COM_BITNESS_GAP
- bac:         FEATURE
- contexte:    suite directe de l'entrée ci-dessus — Maxime, dans la foulée : « on l'active et teste en vrai, meme ce qu'on a jamais vu reelement on le fait feu vert, ok pour modifié automatiquement mais l'utilisateur peut faire un retour car yaura une sauvegarde, j'attend pas les retour je suis un retour moi meme »
- analyse:     décision explicite d'activer `RegisterComComponentAction` malgré l'inconnue documentée le 19/08 (chemin de l'outil jamais vérifié sur un vrai cab) — Maxime a un cab réel et teste lui-même, pas besoin d'attendre un tiers. Avant d'activer, deux findings sur trois (COM_NOT_REGISTERED, COM_BITNESS_GAP) avaient un `Finding.FilePath` vide dans le scanner (jamais branché malgré le chemin déjà calculé pour `binaryPresentUnderRoot`) — câbler la règle sans ce correctif aurait produit un item qui ne répare jamais rien (Plan() fail-closed sur FilePath null). Corrigé avant le câblage. **Nuance sur « yaura une sauvegarde »** : la sauvegarde s'exécute quand même à chaque Apply (RepairEngine ne la conditionne à aucun flag), mais `IsReversibleByNature=false` pour cette action (règle 7, LOT I) — il n'y a rien de restaurable pour un lancement d'outil externe, donc pas de bouton Annuler après coup. Le vrai filet de sécurité ici est la boîte de dialogue « irréversible, confirmer ? » avant le clic, pas une sauvegarde restaurable — précisé à Maxime pour ne pas laisser une fausse impression de sécurité.
- disposition: ✅ codé et testé le 20/08. `EvaluateComponent` (Core) porte maintenant `FilePath` pour COM_NOT_REGISTERED et COM_BITNESS_GAP (paramètre optionnel, aucun appel existant cassé). Trois règles ajoutées à `pack-2026.08.json` : VPINMAME_NOT_REGISTERED confiance 80, COM_NOT_REGISTERED confiance 75, COM_BITNESS_GAP confiance 70 (la plus basse, volontairement — voir la règle elle-même pour la réserve documentée sur le dossier retrouvé pouvant être celui du mauvais bitness sur une install hybride séparée, sans danger mais potentiellement inefficace dans ce cas précis). COM_STALE_PATH resté non câblé, structurellement impossible (son FilePath EST le chemin qui n'existe plus). Nouveau test de bout en bout sur le pack réel chargé depuis le disque (`Test_EndToEnd_VpinmameNotRegistered_RealPack_PlansConfirmationRequired`, un vrai VPinMAME.dll + Setup.exe PE sur disque temporaire) : Mode=ConfirmationRequired si licencié, Locked sinon. 165 tests Repair, 540 tests Core, tout vert. **Reste à tester en vrai sur le cab de Maxime — c'est lui le testeur, pas d'attente d'un retour tiers.**

## 2026-08-19 · RegisterComComponentAction — Rule 6 rendue adaptative (fini le pré-check admin sur toute l'appli), toujours éteinte dans le pack pour une autre raison
- code:        aucun code de finding — architecture d'une action Repair déjà écrite (LOT I, spec 10/08)
- bac:         FEATURE
- contexte:    demande explicite de Maxime (19/08) : « faut pas de droit d'admin mais il faut qu'elle existent et soit utilisable »
- analyse:     l'ancienne Rule 6 refusait `Execute()` tant que le PROCESSUS ENTIER (Pincab Toolbox lui-même) ne tournait pas déjà élevé — ce qui aurait forcé à relancer toute l'appli en admin pour une seule réparation optionnelle, à l'opposé de la promesse « pas besoin de droits administrateur » de la FAQ landing et de `app.manifest` (`asInvoker`). Nouvelle conception : `Execute()` tente toujours un lancement normal d'abord (comme les 3 autres outils du whitelist) ; seulement si **Windows lui-même** refuse ce lancement précis avec `ERROR_ELEVATION_REQUIRED`, une seule invite UAC standard est demandée pour cet outil tiers précis (`IElevatedProcessLauncher`, `ShellExecute`+`runas`), jamais pour l'appli. Refus de l'utilisateur → échec calme, pas une erreur. `IElevationProbe`/`RealElevationProbe` (le pré-check bloquant) supprimés proprement (plus aucune référence dans le code), remplacés. Action enregistrée dans les deux `RepairActionRegistry` (App preview + write-path réel) — code prêt et testé de bout en bout.
- disposition: ✅ codé et testé (163 tests Repair, 540 Core, tout vert) · **le petit lot honnêteté proposé est fait** (même jour, « ok pour le petit re scan ») : `BtnRepairApply_Click` appelle `Verify()` après `Apply()` et sépare « réparé » (confirmé disparu) de « lancé, pas encore confirmé » (`repair.apply.pending`, nouvelle ligne dédiée, jamais noyé dans le compte « réparé »), pour les 5 actions, pas seulement celle-ci. **Toujours pas activée dans `knowledge/pack-2026.08.json`** — l'inconnue restante est indépendante de l'admin et de l'honnêteté UI : l'outil de ré-enregistrement est-il vraiment toujours à côté de la DLL sur un vrai cab (fail-closed si faux, jamais vérifié en vrai). Activer la règle est maintenant une décision, pas un blocage technique.

## 2026-08-18 (session éco, fin de journée) · Blocage « Contrôle intelligent des applications » : NON déterministe — verdict cloud temporaire, pas une cause dans le code
- code:        aucun code de finding — incident de build / distribution
- bac:         FIX (résultat d'un diagnostic) + correction d'une conclusion antérieure trop assurée
- contexte:    Après le merge des 4 lots du 18/08, Windows 11 bloque `publish\PincabToolbox.exe`
  (« application potentiellement dangereuse », blocage DUR, aucun bouton « Exécuter quand même »).
  Même symptôme que la veille (17-18/08), que le commit `be0a1ce` attribue « avec certitude » à un
  item MSBuild `<SplashScreen>`.
- analyse:     Bissection menée avec le bon point de comparaison, le commit `20ba4b3` (build de 13h44
  qui se lançait normalement) et non `be0a1ce`. Lecture du diff : `csproj` inchangé hors un
  commentaire, `App.xaml`/`App.xaml.cs` identiques bit à bit, aucun `<SplashScreen>` réintroduit,
  zéro `DllImport` ajouté, aucun `Main()`/`OnStartup` custom, rien qui s'exécute avant le Dispatcher.
  Les 7 nouveaux fichiers Core sont du I/O fichier managé pur.
  Trois exe fabriqués et lancés (`test-sac.cmd`) : **A** = rebuild de `20ba4b3`, hash neuf → se lance ;
  **B** = code actuel sans `IncludeNativeLibrariesForSelfExtract` → se lance ; **contrôle** = l'exe
  `publish\` initialement bloqué, relancé tel quel, sans **aucune** modification → **se lance aussi**.
  C'est le test de contrôle qui tranche : le blocage n'est pas reproductible, donc il n'est pas
  déterministe. Mécanisme réel : Smart App Control interroge le service cloud Microsoft pour tout
  binaire non signé, bloque tant qu'aucun verdict favorable n'est rendu, puis débloque une fois le
  verdict rendu. La fenêtre de blocage est temporelle, pas structurelle.
  ⚠ **Conséquence sur le diagnostic du 17-18/08** : on vient d'observer le même symptôme SANS
  `<SplashScreen>`, résolu seul sans toucher au code. Le retrait du SplashScreen a peut-être corrigé
  quelque chose, ou bien le verdict cloud s'était résolu entre-temps et le mérite a été attribué au
  mauvais changement. Impossible de trancher rétroactivement, mais la certitude affichée dans le
  message de `be0a1ce` n'est plus tenable et ne doit plus être citée comme un fait établi.
  Piège de méthode à retenir : un protocole de bissection qui ne contrôle pas le temps ne prouve
  rien face à un phénomène temporel. Sans le test de contrôle, un « correctif » (retirer
  l'auto-extraction) allait être livré alors qu'il ne corrigeait rien.
- disposition: Aucun changement de code — il n'y avait rien à corriger. Risque commercial consigné :
  chaque nouvelle release non signée expose les utilisateurs à cette même fenêtre de blocage, sans
  contournement possible de leur côté. Signature de code écartée par Maxime (budget nul, 18/08).
  Piste gratuite proposée, NON mise en œuvre, à valider : soumettre l'exe au portail Microsoft
  Security Intelligence (soumission développeur, gratuite) à chaque release pour faire rendre le
  verdict cloud AVANT que les utilisateurs ne téléchargent, plutôt que de le subir après.

## 2026-08-18 (session éco) · Revue CTO+Produit — 4 lots (Scanner, Repair, Rescore, Table Companion teaser)
- code:        `GLOBALCONFIG_B2S_MISSING`, `FONT_FILE_MISSING`, `SCRIPT_HARDCODED_PATH`,
  `SHARED_SCRIPT_LOCAL_COPY` (nouveaux) ; `ALTCOLOR_INCOMPLETE`/`ALTSOUND_SAMPLE_MISSING` (teaser,
  pas de nouveau code)
- bac:         FEATURE (4 lots livrés) + 2 pistes d'amélioration identifiées, NON codées
- contexte:    session pilotée par `docs/PROMPT-session-lot-complet-2026-08-18.md`, sandbox
  resynchronisé sur le vrai poste de Maxime en tout début de session (10 commits + merge en cours
  découverts via une capture d'écran de l'onglet Tutoriel, absent d'ici jusque-là)
- analyse:     LOT SCANNER — les 4 derniers détecteurs de l'audit du 05/08, pattern injected-delegate
  standard, testés. LOT REPAIR — `ApplyResult.ItemFailureReasons` remonte enfin jusqu'à l'UI le motif
  d'échec par item que le moteur calculait déjà en interne et jetait avant l'écran (additif, aucune
  signature touchée, 7 nouveaux tests, 156 → 162). LOT RESCORE — bouton « Revoir mon score » qui
  relance le même scan après un Apply réussi/partiel et affiche l'ancien → nouveau score honnêtement,
  jamais un delta inventé. LOT TABLE COMPANION TEASER — opt-in `mailto:` discret sur les deux findings
  concernés, zéro appel réseau (ADR-002), texte honnête ("pas encore sorti").
- disposition: livré (4 commits séparés + 1 commit de sync, détail dans `TRANSMISSION.md` MAJ 18/08) ·
  Core 540/540, Repair 162/162, Debug **et** Release · XML bien formé + 0 `CS1xxx` + crosscheck
  x:Name/gestionnaires (14 orphelins, baseline inchangée) après chaque lot touchant l'App. Deux
  améliorations à faible coût repérées, NON codées (feu vert à demander à Maxime) : (1)
  `FONT_FILE_MISSING` ne vérifie que la présence sous l'install scannée, jamais le registre Windows
  Fonts (P/Invoke non vérifiable sans hôte Windows dans ce sandbox) — un faux négatif est possible
  pour une police installée globalement mais absente du dossier de l'install, limite documentée dans
  le scanner, pas un bug caché. (2) Le bouton Rescore relit `TxtRoot`/`_demoRoot` au moment du clic
  plutôt que de capturer explicitement le root/profil au moment de l'Apply — si le champ dossier
  changeait entre les deux (flux normal très improbable), la comparaison porterait sur deux installs
  différents ; pas corrigé ici, prudence délibérée plutôt que sur-ingénierie d'un cas limite jamais
  observé en pratique.

## 2026-08-17 (quater) · [commentaire public, VPUniverse] — 3ᵉ occurrence indépendante de B2S_MISSING sur tables PUP-Pack, renforce l'entrée du 07/08
- code:        `B2S_MISSING`
- bac:         FP confirmé sur une sous-catégorie précise (déjà tranché le 07/08), pas re-analysé
- contexte:    Nouveau commentateur (autre que Joey Mahon et l'auteur de l'entrée du 07/08),
  question directe : « Should there be a warning for missing backglasses for games that have a
  puppack installed? ». Même plainte, formulée indépendamment, aucun lien visible avec les deux
  précédentes. A aussi posté une capture d'écran (lien VPUniverse) pour un souci d'install séparé,
  36 installées, "un peu plus de 160 tables VP+FP" ; capture non lisible depuis cette session (lien
  `.url` uniquement, pas l'image elle-même), donc pas encore diagnostiqué, redemandé en direct.
- analyse:     Pas de nouvelle analyse nécessaire, le cas est déjà entièrement tranché techniquement
  dans l'entrée du 07/08 ci-dessous : une table avec PUP-Pack associé n'a structurellement jamais de
  `.directb2s`, ce n'est pas une install cassée. Ce qui change ici, c'est le compteur de signal :
  3ᵉ rapport indépendant du même comportement (07/08 commentaire forum, complétude implicite sur le
  cas POTC/Joey mi-août, et maintenant celui-ci), sur une amélioration déjà identifiée et toujours
  pas codée dix jours après le premier signalement.
- disposition: Répondu honnêtement : confirmé que c'est un comportement connu et documenté, pas un
  bug de son install, toujours pas de correctif livré, sans donner de date. Aucun code changé.
  Le signal cumulé (3 rapports indépendants) mérite d'être remonté à Maxime comme candidat sérieux
  pour la refonte ou un correctif ciblé, pas juste reloggé silencieusement — à traiter en dehors de
  cette réponse publique.

## 2026-08-17 (ter) · [commentaire public] — suite : dossier unique clarifié, hypothèse forte du commentateur sur Leprechaun King (nom de ROM = nom du PUP-Pack)
- code:        ROM_MISSING
- bac:         WORDING (dossier) + FP probable renforcé (pattern Orbital Pin)
- contexte:    Suite du commentaire précédent. La personne clarifie sa vraie structure, un seul
  install, pas deux en parallèle : `vPinball\FuturePinball`, `vPinball\PinUPSystem`,
  `vPinball\VisualPinball`. Mon hypothèse de double-install (calquée sur le cas Joey de la même
  journée) ne s'appliquait donc pas ici, structure différente. Elle précise aussi que Leprechaun
  King tourne très bien SANS que la « ROM » ne soit présente, et propose elle-même l'explication :
  le nom détecté par le scanner ne serait pas une vraie ROM VPinMAME mais le nom du dossier
  PUP-Pack référencé dans le script. Elle note que Leprechaun King et Stranger Things (Stranger
  Edition) sont les deux seules tables Orbital Pin qu'elle possède, et que les deux sont
  concernées.
- analyse:     Deux choses distinctes. (1) Dossier : la racine à donner au scanner est le
  sous-dossier `VisualPinball` précisément (celui qui contient directement `Tables` et
  `VPinMAME`), pas `vPinball` (le parent, qui contient aussi FuturePinball/PinUPSystem, deux
  émulateurs/frontends différents non liés à VPinMAME). (2) L'hypothèse PUP-Pack est solide et
  change la lecture du cas Stranger Things SE (entrée du 15/08) : ce n'est plus un cas isolé, ce
  sont maintenant DEUX tables Orbital Pin indépendantes où le nom détecté comme ROM par
  `ScriptAnalyzer.AnalyzeRomUsage` correspond exactement au nom du dossier PUP-Pack, et où la
  table tourne sans le fichier ROM. Ça pointe vers un pattern spécifique aux tables Orbital Pin :
  le script ouvre bien `VPinMAME.Controller` (donc `UsesController = true`, confirmé par le code
  du scanner) et lui assigne un `GameName` qui sert au hook DMD/PUP-Pack, sans que le jeu ne
  dépende réellement du core VPinMAME pour sa logique (table originale scriptée, pas une
  reproduction pilotée par ROM). Toujours pas vérifié sur le texte réel d'un script Orbital, mais
  deux rapports indépendants qui convergent exactement sur le même mécanisme, c'est un signal
  fort, plus fort qu'une simple coïncidence.
- disposition: Répondu publiquement sur les deux points, dossier `VisualPinball` précis + confirmé
  que l'hypothèse PUP-Pack est plausible et cohérente avec le cas Stranger Things déjà en cours
  d'investigation, sans l'affirmer comme certain (toujours pas de script réel lu). Pas de code
  changé. Si un troisième cas Orbital Pin confirme le même pattern, ou si un script réel est
  obtenu, ça devient un candidat concret pour une règle scanner dédiée (ex. : ne pas traiter le
  `GameName` comme ROM requise quand il matche un dossier PUP-Pack existant sur une table qui
  n'a par ailleurs aucun autre signe de dépendance VPinMAME) — pas encore le cas.

## 2026-08-17 (bis) · [commentaire public] — question sur ROM_MISSING (Leprechaun King + Stranger Things SE) et sur quel dossier scanner
- code:        ROM_MISSING
- bac:         WORDING (question légitime, pas un bug rapporté) + recoupe le FP probable déjà noté le 15/08
- contexte:    Nouveau commentateur (pas Joey), demande pourquoi 'Leprechaun King' (installé par
  défaut par Popper) et 'Stranger Things - Stranger Edition' sont signalés ROM_MISSING alors que
  toutes les tables sont directement dans le dossier Tables, sans sous-dossier. Demande aussi
  explicitement quel dossier pointer comme racine : celui de Visual Pinball ou celui du 'Baller
  Installer' (vPinball).
- analyse:     Deux points bien distincts à répondre. (1) Le check ROM ne regarde jamais la
  structure de sous-dossiers dans Tables, seulement si le script ouvre vraiment
  VPinMAME.Controller et si le zip exact existe dans VPinMAME\roms de la racine scannée — donc la
  vraie question posée (quelle racine pointer) est la bonne à traiter en premier. (2) Si la
  personne a deux installs en parallèle (ancien Visual Pinball + nouveau vPinball du Baller
  Installer), scanner celui qui n'est pas réellement utilisé par Popper donnera de fausses
  alertes ROM_MISSING même quand tout est correct côté install actif — schéma identique à la
  cause racine trouvée chez Joey Mahon le même jour (voir entrée du dessus), mais ici côté ROM
  plutôt que backglass. Sur les deux tables citées : Leprechaun King a déjà été vu ROM_OK
  (`leprechaun`) sur une install correcte (rapport Joey du 15/08), donc c'est une vraie ROM
  requise, pas un FP. Stranger Things - Stranger Edition reste le FP probable déjà documenté
  (STLE.zip ne semble pas être une vraie ROM VPinMAME publiée, cf. recherche web du 15/08),
  toujours pas confirmé sur le vrai script.
- disposition: Répondu publiquement : expliqué le fonctionnement réel du check, conseillé de
  pointer la racine sur l'install réellement utilisée par Popper (pas les deux), confirmé
  Leprechaun King comme ROM légitime et signalé Stranger Things SE comme suspect FP en cours
  d'investigation, sans surpromettre de fix. Aucun changement de code : cette question, plus le
  cas Joey de la même journée, renforce l'idée d'un futur check "plusieurs installs / mauvaise
  racine pointée" déjà loggé en FEATURE — toujours pas codé.

## 2026-08-17 · Messenger — Joey Mahon, cause réelle du backglass/PUP-Pack POTC trouvée : install fantôme d'un ancien setup 2019, pas un bug scanner
- code:        NOUVEAU (candidat) — aucun code existant ne couvre ce cas
- bac:         FIX (confirmé par Joey) + FEATURE
- contexte:    Clôture du long thread POTC (voir les deux entrées précédentes du 15/08). Joey avait
  un cabinet monté en 2019 avec un setup manuel : `C:\DirectOutput`, `C:\Visual Pinball`,
  `C:\PinUpSystem`. Une mise à jour ratée l'a poussé à faire une install fraîche via le "Baller
  Installer" dans `C:\vPinball` (qui ne recrée pas de `DirectOutput` séparé et regroupe VP +
  PinUpSystem). Avec PupEventViewer, il a trouvé qu'au lancement, `vpinball64.exe` du NOUVEL
  install allait chercher un raccourci dans `Tables\Plugins64\Directoutput` qui pointait encore
  vers `C:\Visual Pinball` (L'ANCIEN install de 2019) et son sous-dossier x64, et que
  `PinUpPlayerB2SDriver` dans l'ancien dossier était une version différente (plus ancienne) de
  celle livrée par le nouvel installeur. En remplaçant le driver périmé par celui du nouvel
  install, le backglass ET le PUP-Pack ont fonctionné directement.
- analyse:     Root cause confirmée, hors périmètre du scanner actuel par construction : il ne
  scanne que la racine sélectionnée, il n'a aucune visibilité sur un second install ailleurs sur
  le disque ni sur un raccourci qui pointe vers l'extérieur de cette racine. Le finding
  `BITNESS_MISMATCH`/dépendances actuels vérifient la présence et la bitness des DLL dans la
  racine scannée, pas la cohérence de version entre deux installs, ni où pointent réellement les
  raccourcis de `Tables\Plugins64`. Verbatim de Joey, proposition explicite : « have the tool
  select all pinball related directories instead of just the one so it could possibly check for
  something like that ». Lui-même reconnaît que son cas (fresh install dans un autre dossier
  après un ancien setup manuel de 2019) est probablement rare, pas la majorité des utilisateurs.
- disposition: FIX confirmé côté Joey (rien à changer côté produit pour débloquer son cas
  précis). FEATURE loggée, PAS codée : détecter des installs pinball résiduels/multiples sur le
  disque et des raccourcis de plugin qui pointent hors de la racine scannée serait un chantier
  à part entière (scan hors racine, résolution de raccourcis .lnk, comparaison de versions de
  DLL entre dossiers), pas un ajustement mineur d'un scanner existant. Candidat pour le backlog
  produit, pas pour la refonte UI en cours ni un correctif rapide. Répondu à Joey avec
  remerciement, sans engagement de date.

## 2026-08-15 (soir) · Messenger — Joey Mahon, POTC changé de table (Hanibal's 4K Edition), fichiers complets mais backglass et PUP-Pack toujours muets
- code:        B2S_MISSING (absent du nouveau rapport, plus le bon angle) + suivi ROM_MISSING (Stranger Things, inchangé)
- bac:         FN à confirmer (le scanner ne peut rien voir ici) + suivi
- contexte:    Suite du thread POTC. Joey a abandonné la table 'VP10-Pirates of the Caribbean-1.3'
  d'origine, téléchargé une autre édition (Hanibal's 4K Edition, VPUniverse) avec son propre PUP-Pack
  et un .directb2s pris sur un lien séparé (VPUniverse, "Stern 2006 Alt B2S Full DMD"), renommé pour
  matcher la table. Nouveau scan (`pincabtoolboxreport202608151952.txt`) : ROM trouvée (potc_600as,
  OK), PUP-Pack détecté (potc_600as, OK), plus aucun B2S_MISSING sur cette table, dépendances B2S OK
  globalement. Donc côté présence de fichiers, tout est complet. Pourtant Joey rapporte : backglass
  toujours invisible même avec noms de fichiers qui matchent, et le PUP-Pack ne s'active pas du tout.
  Verbatim : « The backglass did not come up even when the filenames of the B2S matched the table.
  The pup pack did not seem to engage at all ».
- analyse:     Le scanner ne vérifie que la présence/le nommage des fichiers, jamais leur contenu
  interne ni le comportement runtime (registre de ROM déclaré à l'intérieur du .directb2s, process
  PinUP Player lancé ou non, config par-jeu dans Popper). Ce que rapporte Joey est cohérent avec un
  problème hors du périmètre actuel du scanner, pas un FP/FN classique. Hypothèse proposée (pas
  confirmée) : Joey lance POTC en double-cliquant VpinballX.exe/Vpinball64.exe directement plutôt que
  via PinUP Popper (comme il l'a écrit textuellement l'échange précédent) — les PUP-Packs dépendent en
  général de PinUP Player, démarré par Popper au lancement d'une table, pas par l'exe VPX seul. Le B2S
  peut parfois marcher lancé en direct une fois enregistré (cas d'ACDC), mais ça n'explique pas
  pourquoi POTC reste muet même avec fichiers complets. Reste à tester par Joey.
- disposition: Répondu, proposé à Joey de tester le lancement via Popper + de lancer
  B2SBackglassServer.exe seul pour voir s'il ouvre sans erreur. En attente de son retour. Si confirmé
  que c'est un problème de contenu interne du .directb2s (rom name déclaré dedans) plutôt que de
  lancement, ça deviendrait un candidat FEATURE réaliste (vérifier le rom name interne du .directb2s,
  pas juste le nom de fichier) — à ne pas coder avant confirmation.

## 2026-08-15 · Messenger — Joey Mahon, ROM_MISSING critique sur une table originale ScottyWic (Stranger Things), après les deux fix connus
- code:        ROM_MISSING
- bac:         FP (probable, pas encore confirmé à 100%)
- contexte:    Joey teste une nouvelle install, backglass Pirates (POTC) qui pose problème par
  ailleurs (thread séparé, pas un souci scanner). En marge de ce test, il installe la table
  "Stranger Things" par ScottyWic (originale, plus tard retouchée par LoadedWeapon) sur la nouvelle
  install, la table tourne (joue normalement, DMD à part cassé, hors sujet ici), mais le scan la
  remonte en ROM_MISSING critique. Verbatim : « if memory serves and i could be wrong, the stranger
  things table didn't have a rom? ».
- analyse:     Stranger Things (ScottyWic) est une table originale, ne passe pas par VPinMAME,
  ne devrait donc exiger aucune ROM — cohérent avec le rappel de Joey. Ce n'est PAS le même bug que
  les deux causes déjà corrigées et annoncées (post EN "Original and homebrew tables flagged as ROM
  missing... Fixed", lignes VPinMAME commentées + template de table à ROM) : Joey a rescan APRÈS ces
  fix, donc si le faux positif persiste ici c'est une troisième cause distincte, pas une régression
  des deux premières. Pas encore confirmé formellement : il manque le nom de fichier exact / version
  de la table pour vérifier le script réel (`ScriptAnalyzer.AnalyzeRomUsage`) et trouver la vraie
  cause plutôt que de deviner. Demandé à Joey dans la réponse précédente, en attente.
- disposition: FP probable, PAS CORRIGÉ, en attente de la confirmation du nom de fichier avant tout
  changement de code (règle du projet : jamais de fix sans vérifier le vrai script). Rien à coder ici
  tant que cette info n'arrive pas — noté pour ne pas perdre le fil.

### Addendum même jour — rapport de scan réel reçu, hypothèse à corriger

> Joey a envoyé le vrai rapport HTML (`.\VisualPinball\Tables\Stranger Things - SE 1.42.vpx`, ROM
> demandée `STLE.zip`). Deux points qui changent l'analyse ci-dessus :
>
> 1. **Ce n'est PAS le fichier ScottyWic testé la veille au soir** — "SE" = "Stranger Edition", table
>    d'**Orbital Pinball** (confirmé par recherche web : threads VPUniverse/VPinball.com "Stranger
>    Things | Stranger Edition"), pas la table originale ScottyWic mentionnée dans le message nocturne
>    de Joey. Deux tables différentes, ne pas les confondre dans le suivi.
> 2. **Recherche web sur cette table précise** : la communauté documente un fichier séparé
>    `STLE.UltraDMD` (dossier à copier dans `VisualPinball\Tables`) + un dossier `STSE` pour
>    PinUP PopUp Videos — **aucune mention publique d'un `STLE.zip` distribué comme vraie ROM
>    VPinMAME**. Sources : [thread VPUniverse "Where's the DMD?"](https://vpuniverse.com/forums/topic/9355-orbital-pinball-stranger-things-stranger-edition-wheres-the-dmd/),
>    [Stranger Things – Stranger Edition (VPUniverse)](https://vpuniverse.com/files/file/25396-stranger-things/),
>    [topic VPinball.com](https://vpinball.com/forums/topic/stranger-things-stranger-edition/page/11/).
>
> **Signal le plus fort, et il vient de Joey lui-même, pas de la recherche web** : dans son message
> de la veille, avant même de voir ce rapport, il avait écrit avoir testé cette table et
> « the table is playing... it seems to be working fine except the DMD wasn't showing up ». Ça
> contredit directement le texte du Finding Critical, qui affirme *« will not start »*. Une table qui
> tourne et se joue ne « ne démarre pas » — si ce constat de Joey se confirme sur CETTE table précise
> (Stranger Edition, pas ScottyWic), c'est un FP confirmé sur la sévérité/le message, pas seulement
> une hypothèse.
>
> **Cause probable, pas encore vérifiée sur le vrai script** (`ScriptAnalyzer.AnalyzeRomUsage` lit un
> vrai `CreateObject("VPinMAME.Controller")` non commenté pour lever `UsesController`) : le script
> ouvre bien le contrôleur pour de vrai, donc `ROM_NOT_REQUIRED` ne peut pas s'appliquer tel quel —
> mais rien ne prouve que la table CONDITIONNE son démarrage à la présence de la ROM (gestion
> d'erreur genre `On Error Resume Next` autour de l'appel, table qui continue en dégradé). Pas
> vérifiable sans le texte réel du script (non public, embarqué dans le `.vpx`) — **prochaine étape :
> demander à Joey s'il peut exporter/coller le script de cette table précise (clic droit → View
> Script dans l'éditeur VPX)** avant tout changement de code, comme toujours.
>
> **Le vrai problème DMD de Joey, lui, est déjà documenté publiquement et n'a rien à voir avec
> ROM_MISSING** : il manque le dossier `STLE.UltraDMD` (mécanisme UltraDMD, différent du DMD piloté
> par VPinMAME/AltColor) — fix connu et démontré dans le thread VPUniverse ci-dessus, indépendant de
> ce qui est codé ou pas dans le scanner.

## 2026-08-15 · Messenger — Joey Mahon, backglass POTC (thread séparé, pas ROM_MISSING) : bitness résolu, cause isolée à POTC seul
- code:        B2S_MISSING (probable — pas confirmé, voir disposition)
- bac:         FIX (partiel) + FN à confirmer
- contexte:    Suite du thread backglass ouvert plus tôt le même jour (ACDC vs POTC, VPinballX 32-bit
  vs VPinballX64). Joey a ré-enregistré `B2SBackglassServerRegisterApp.exe` en admin côté 32-bit
  (fix trouvé par recherche web, wiki `vpinball/b2s-backglass`). Résultat : ACDC affiche maintenant
  son backglass correctement sur LES DEUX bitness (32 et 64). POTC, en revanche, n'affiche plus RIEN
  du tout (juste le bureau) sur les deux bitness aussi — avant le fix, POTC affichait par erreur le
  backglass d'ACDC en 64-bit. Verbatim (partiel) : « Ran POTC and no backglass just desktop on both
  VpinballX, and X64 ».
- analyse:     Le fix bitness a fonctionné (ACDC le prouve sur les deux exe) — ce n'est donc plus un
  problème de registration/COM, c'est spécifique à POTC. Interprétation : avant le fix, un état
  bloqué/mis en cache faisait apparemment "hériter" le backglass d'ACDC sur n'importe quelle table en
  64-bit (pas vérifié formellement, hypothèse) ; une fois ce cache/état cassé par le re-enregistrement,
  on voit l'état réel de POTC : rien ne se charge du tout. Piste concrète et déjà en notre possession :
  le propre rapport de scan de Joey (reçu plus tôt) contenait la ligne "8 similar findings
  (B2S_MISSING) — collapsed to keep this list readable", jamais désagrégée dans le HTML. POTC est un
  candidat plausible pour faire partie de ces 8, mais ce n'est PAS confirmé — le format HTML/MD/BBCode
  regroupe (`Rolled()`), seuls TXT/JSON/PDF montrent chaque table individuellement (comportement
  documenté, MAJ 13/08 ter).
- disposition: PAS CONFIRMÉ. Demandé à Joey de ré-exporter son scan en .txt ou .json pour voir la
  liste désagrégée des 8 B2S_MISSING et vérifier si POTC y figure, avant toute hypothèse de fix côté
  scanner ou côté install. Rien à coder tant que cette confirmation n'arrive pas.

## 2026-08-15 · Messenger — Joey Mahon, retour positif spontané sur Update Watcher
- code:        UPDATE_AVAILABLE
- bac:         FEATURE (retour positif, pas une demande)
- contexte:    En marge du dépannage backglass, verbatim spontané : « for the record, I do like the
  tool pointing out when there is an updated version of a table that exists out there and that
  clicking the update button brings up the website where the table can be searched for and found ».
- analyse:     Validation terrain directe d'une fonctionnalité déjà livrée (Update Watcher, marquée
  bêta dans le rapport de scan : « matched 2/10 tables against the VPS database »). Rien à changer,
  juste à garder comme signal produit positif.
- disposition: Aucune action requise. Consigné pour `docs/SUCCESS-METRICS.md` / mémoire produit.

## 2026-08-15 · Messenger — Joey Mahon, demande d'un onglet séparé pour les mises à jour de tables
- code:        UPDATE_AVAILABLE
- bac:         FEATURE
- contexte:    Verbatim : « it may be helpful to have a separate tab that shows all the table updates
  specifically without cluttering up the main screen with several info alerts on table updates (or
  maybe you already have that managed and i cant see it yet since i only have a handful of tables
  installed) ».
- analyse:     Vérifié dans le vrai XAML avant de répondre (`MainWindow.xaml`) : seulement 4 onglets
  existent (`TabScanner`, `TabDiff`, `TabRepair`, `TabAbout`), aucun onglet dédié aux mises à jour —
  Joey a raison, ce n'est pas caché, ça n'existe juste pas. Aujourd'hui `UPDATE_AVAILABLE` (Update
  Watcher, encore bêta) sort mélangé aux autres findings Info dans la liste du Scanner, exactement le
  bruit qu'il décrit, et ça ne peut que s'aggraver une fois sa bibliothèque complète installée (il
  n'a qu'une poignée de tables pour l'instant).
- disposition: PAS CODÉ, remonté à Maxime. Recoupe directement le chantier déjà prévu lundi 17/08
  16h (refonte UI/UX, synthèse GPT/Gemini) — candidat naturel à glisser dans le prompt de la tâche
  Opus (plan) plutôt qu'à traiter isolément maintenant. Décision de scope à prendre par Maxime, pas
  prise seule ici (même principe que pour le point AltColor/DMD plus tôt aujourd'hui : ne pas
  élargir les tâches programmées en silence).

## 2026-08-15 · [groupe FB World of Virtual Pinball (WoVP)](https://facebook.com/) — Tony Truong, DMD score invisible/recouvert
- code:        NOUVEAU (pas de code existant — rattaché au backlog B1 "AltColor / SERum Pair Integrity", `docs/AUDIT-Scanner-2026-08.md` §7 Famille B, déjà P1, preuve déjà jugée "forte")
- bac:         FN
- contexte:    table Michael Jackson (Bad / Data East), capture d'écran postée : le DMD affiche un
  encadré jaune vide à la place du score, entouré d'artwork statique (danseurs). Verbatim : « Please
  help dmd is not showing score, looks like it's getting covered up ».
- analyse:     symptôme cohérent avec une paire de colorisation AltColor/Serum incomplète ou mal
  associée à la ROM — le rendu reste bloqué sur une frame statique au lieu du calque dynamique du
  score, ce qui donne visuellement l'impression d'un score "caché". Déjà documenté dans l'audit
  (§7, item B1) avec preuve terrain "forte" (VPForums 53452, VPUniverse 10162, freezy#143, Pinball
  Nirvana) — ce rapport n'est pas le premier signal, c'est une corroboration de plus sur un item déjà
  qualifié P1. Le scanner actuel ne couvre PAS ce cas (aucun scanner d'intégrité de colorisation
  n'existe aujourd'hui) — vérifié avant de répondre publiquement, réponse Facebook donnée sans
  pointer vers le scanner pour ne pas survendre ce qu'il détecte réellement.
- disposition: FN confirmé, pas encore corrigé. **Demande explicite de Maxime : ajouter à la liste
  des sujets à traiter lundi** ("à résoudre ou à détecter"). Point de vigilance à trancher avec lui
  avant d'y toucher : les deux tâches programmées de lundi 17/08 16h (Opus plan + Sonnet impl) sont
  scopées spécifiquement à la refonte UI/UX (synthèse retours GPT/Gemini), pas à ce sujet scanner —
  ne pas les élargir en silence. Ce point reste donc en file d'attente ici, prêt à être repris dans
  une session scanner dédiée (le pack de preuve existe déjà dans l'audit, reste à construire B1).

## 2026-08-14 · Décision produit Maxime — anglais par défaut au premier lancement, espagnol ajouté comme 3ᵉ langue
- code:        aucun code de finding, décision produit + feature de localisation
- bac:         FEATURE
- contexte:    Maxime : « on touche beaucoup plus le public qui parle anglais, même si ça me plaît
  pas on va inverser, l'anglais passe en premier tout le temps sur pincab toolbox donc maintenant
  en/fr. on ouvre l'app on arrive sur l'anglais pas le fr si les gens veulent le fr c'est en haut.
  tu vas rajouter l'espagnol en langue sur le logiciel »
- analyse:     deux changements distincts, livrés en 2 commits séparés (chacun revertible seul).
  (1) `Loc.Lang` choisissait la langue de démarrage via `CultureInfo.CurrentUICulture` (FR si
  Windows est en FR) — changé en constante `"en"` fixe : un Windows FR n'implique plus un cab FR.
  Le choix sauvegardé d'un utilisateur qui revient (`Settings.Lang`, restauré au démarrage) prime
  toujours dessus, seul le tout premier lancement change.
  (2) Espagnol : recensé TOUS les points du code où une langue est choisie avant d'écrire une seule
  ligne — `Loc.cs` (4 dictionnaires : En/Fr + FrFindings + FrFixHints, 340 clés), `Knowledge.cs`
  (51 entrées Impact/Cause), `Scenarios.cs` dans Core (6 scénarios de cause racine + leurs chaînes
  causales, `bool fr` → `string lang`), et côté Repair : `Blocker` (2 messages de blocage Preflight)
  et `PackStep.ReasonFr/ReasonEn` dans le pack JSON (1 scénario, 3 étapes) qui alimentent
  `RepairLimitation` depuis le fix du 13/08 (ADR-006). Un point resté hors périmètre, signalé
  explicitement plutôt que traduit à moitié en silence : les champs riches par entrée du pack JSON
  (`titleFr`/`impactFr`/`causeFr`/`playerFr`/`explanationFr`/`verificationFr`, un par code) ne sont
  PAS lus par `KnowledgePack.Load` (son DTO ne déclare que `code` + `repairRules`) — seul
  `knowledge/selftest.py` les valide, morte pour l'app tournante. Les traduire en espagnol n'aurait
  eu aucun effet visible ; laissés tels quels.
- disposition: FIX/FEATURE, livré. `Loc.Lang` passe de "en"/"fr" à "en"/"fr"/"es" partout où c'était
  un switch binaire (`Toggle()` cycle désormais en→fr→es→en, le bouton affiche la langue active au
  lieu d'un « FR / EN » statique). Vérification automatisée de la parité des clés (script Python,
  pas une relecture à l'œil) : les 3 langues ont exactement le même jeu de clés dans les 4
  dictionnaires de `Loc.cs`, et aucun mismatch de placeholder ({0}/{1}…) entre EN et ES sur aucune
  des 340 entrées. 501 tests Core (3 nouveaux : sélection ES, chaîne causale ES, repli EN sur langue
  inconnue), 153 tests Repair (2 nouveaux : `RepairLimitation.MessageEs` transporté depuis un
  scénario, `Blocker.MessageEs` jamais vide). Pack JSON revérifié avec `selftest.py` (12/12
  garde-fous toujours verts) après ajout des champs `*Es`. App vérifiée par `csc -t:library`
  (0 erreur CS1xxx).

## 2026-08-14 · Décision produit Maxime — champs riches du pack JSON câblés dans le panneau de détail
- code:        BLOCKED_DLL, ROM_UNZIPPED, POPPER_NOT_REGISTERED, BITNESS_MISMATCH_VPM, PINUP_DISPLAY_ZOMBIE, ORPHANED_MEDIA_FILE, BITNESS_DMD64_MISSING
- bac:         FEATURE
- contexte:    suite de l'entrée précédente — Maxime, sur les champs riches du pack laissés morts : « si c'est une valeur produit on le fait ».
- analyse:     avant de coder, revérifié quels champs sont VRAIMENT nouveaux vs déjà couverts
  ailleurs. `impactFr/impactEn` et `causeFr/causeEn` du pack sont des doublons mot pour mot de
  `Knowledge.cs` (comparé BLOCKED_DLL ligne à ligne — texte identique), qui en plus couvre 51 codes
  contre 7 dans le pack : les câbler aurait créé deux sources de vérité pour la même info sans
  aucun bénéfice. `titleFr/titleEn` fait doublon avec la ligne Sujet déjà affichée. Les 3 champs
  génuinement nouveaux — `playerFr/playerEn` (reformulation grand public de ce que le joueur
  remarque), `explanationFr/explanationEn` (mécanisme expliqué simplement, complémentaire à Cause
  qui reste technique) et `verificationFr/verificationEn` (le contrôle diagnostique qui confirme
  vraiment la panne, inédit — rien d'autre dans l'app ne dit comment vérifier) — sont ceux qui
  apportent une vraie valeur utilisateur, notamment pour l'audience non technique.
- disposition: FEATURE, livré. `KnowledgePack` expose désormais `EntryFor(code)` (nouveau record
  `PackEntry`, lu depuis un `EntryDto` élargi) ; nouvelle classe `PackKnowledge` côté App fait le
  choix de langue EN/FR/ES avec repli sur l'anglais, séparée à dessein de `Knowledge.cs` (doc en
  tête de fichier expliquant pourquoi Impact/Cause n'y sont pas dupliqués). Panneau de détail
  (Écran 1) : 3 nouvelles sections optionnelles — « Ce que vous remarquerez » juste après le
  message, « Bon à savoir » après Cause, « Comment vérifier » après le correctif recommandé —
  chacune cachée si absente pour ce code (même tolérance ADR-005 que le reste du pack ; les 44
  codes sans annotation ne changent pas visuellement). Espagnol ajouté directement dans
  `pack-2026.08.json` pour les 7 codes (traduit à la main, pas de repli manquant). 3 nouveaux
  tests : parsing des 9 champs (FR/EN/ES) depuis un pack de test, absence gracieuse quand
  l'entrée n'a pas de texte éditorial, et un test de bout en bout sur le pack RÉELLEMENT livré
  qui vérifie que les 7 codes annotés ont bien leurs 9 champs non vides. 501 Core + 156 Repair
  (+3), `selftest.py` 12/12 toujours vert après l'ajout des champs ES au JSON, App vérifiée par
  `csc -t:library` (0 erreur CS1xxx) et XAML relu (bien formé).

## 2026-08-14 (extension) · Maxime, « build ok feu vert pour ton amélioration » — les 44 codes restants annotés
- code:        les 44 codes qui manquaient (dont ROM_MISSING) — désormais les 51 codes de Knowledge.cs ont tous une entrée pack
- bac:         FEATURE
- contexte:    build local OK sur l'entrée précédente, feu vert de Maxime pour étoffer le pack au-delà des 7 codes déjà faits.
- analyse:     chaque champ a été ancré dans le vrai code du scanner correspondant (`src/PincabToolbox.Core/Scanning/*.cs`)
  avant d'être écrit — pas de paraphrase d'Impact/Cause à l'aveugle, en particulier pour Vérification qui affirme
  un fait diagnostique précis (ex. VPINMAME_NOT_REGISTERED : « VPinMAME.Controller absent des deux vues COM 32/64-bit »,
  vérifié ligne à ligne dans `ComHealthScanner.cs`, pas deviné). `ROM_MISSING` avait été oublié du premier passage
  (44 restants comptés sur la liste de travail, mais Knowledge.cs en a 51 moins les 7 déjà faits = 44 exact, l'erreur
  venait d'un doublon POPPER_NOT_REGISTERED dans la liste de recherche qui masquait l'absence réelle de ROM_MISSING) —
  repéré par une vérification automatisée de parité contre Knowledge.cs, ajouté séparément avant de clore.
- disposition: FEATURE, livré. Les 51 codes ont désormais une entrée pack avec les 9 champs (player/explanation/
  verification × FR/EN/ES), aucun champ vide. **Bug de validateur découvert et corrigé en cours de route** :
  `validate_pack.py` détectait un « TODO résiduel » à tort dès qu'un texte espagnol contenait le mot « todo »
  (= « tout » en espagnol, un mot très courant) car le motif `TODO|FIXME|XXX` était insensible à la casse sans
  délimitation de mot précise pour ce cas — 4 fausses alertes sur du texte ES parfaitement propre. Corrigé :
  TODO/À MIGRER/A MIGRER exigent maintenant leur casse conventionnelle réelle (tout capitales), FIXME/XXX restent
  insensibles à la casse (aucun mot naturel FR/EN/ES ne peut les déclencher par accident). Revérifié avec le test
  existant `TODO résiduel → rejet` (toujours vert) plus le pack réel (plus aucune fausse alerte). Test étendu de
  7 à 51 codes (`Test_ShippedPack_AllFiftyOneKnownCodesExposeEntryInAllThreeLanguages`), avec une assertion de
  sanité sur la taille de la liste pour qu'un futur ajout de code dans Knowledge.cs sans entrée pack correspondante
  échoue bruyamment au lieu de livrer silencieusement un panneau de détail incomplet. 501 Core + 156 Repair (inchangé
  en nombre, un test étendu plutôt qu'ajouté), `selftest.py` 12/12, JSON revalidé, pack réel validé sans avertissement
  bloquant (seuls les 4 avertissements pré-existants et déjà connus subsistent).

## 2026-08-14 (extension bis) · Maxime, « feu vert » — le filet ROM_MISSING automatisé dans le validateur
- code:        aucun code de finding — outillage CI, guard-fou anti-régression sur la couverture éditoriale du pack
- bac:         FEATURE
- contexte:    suite directe de la revue CTO+Produit de l'entrée précédente, où l'amélioration à faible coût
  proposée était : « ajouter un contrôle automatique dans selftest.py qui vérifie qu'un nouveau code ajouté à
  Knowledge.cs a bien son entrée dans le pack, pour que ce genre de rattrapage ne dépende plus d'une relecture
  manuelle ». Maxime : « feu vert ».
- analyse:     le validateur avait déjà exactement ce principe pour ADR-005 (`discover_registry` lit le code C#
  du registre d'actions et compare à ce que le pack déclare) — même geste appliqué à Knowledge.cs. Nouvelle
  fonction `discover_knowledge_codes` : regex sur les clés `["CODE"]` de la table `Knowledge.Table`, même
  logique que la vérification manuelle faite deux fois à la main ce 14/08. Sévérité choisie : avertissement,
  pas rejet — un code sans texte éditorial reste un pack valide et sûr (dégradation ADR-005 normale), seulement
  moins riche ; ce n'est pas une erreur bloquante comme un actionId inconnu du registre.
- disposition: FEATURE, livré. `validate_pack.py --knowledge-cs <chemin>` (optionnel, comme `--registry`) signale
  chaque code de Knowledge.cs absent du pack, et chaque entrée présente mais à qui il manque playerEn/
  explanationEn/verificationEn. Branché dans la CI (`.github/workflows/knowledge-pack.yml`) — sans ce branchement
  le nouveau flag existerait mais ne tournerait jamais, exactement le problème de donnée morte qu'on corrige
  depuis ce matin. Vérifié en le cassant volontairement : retirer ROM_MISSING du pack fait apparaître l'avertissement
  exact attendu. 2 nouveaux cas dans `selftest.py` (14/14 désormais) : le cas cassé confirme l'avertissement, le
  cas sain confirme que `--knowledge-cs` ne fait pas régresser le pack de référence. Pack réel revalidé avec le
  nouveau flag : 0 code manquant, comme attendu après l'entrée précédente.
- code:        BITNESS_DMD64_MISSING, B2S_MISSING, DPI_SCALING_NONSTANDARD, DISPLAY_SETUP_INCOMPLETE (et tout code sans règle du pack, ou étape manuelle de scénario)
- bac:         FP-langue (bug de traduction, pas de logique)
- contexte:    Maxime, capture d'écran de son vrai cab en FR : sous « Aucune réparation automatique
  disponible sur ce scan », la phrase « Certaines étapes resteront toujours manuelles, licence ou pas : »
  (bien en FR) était suivie de 4 phrases entières en anglais collées bout à bout avec « · »
  (dmddevice64.dll, .directb2s, échelle Windows, câblage moniteurs) — « verifie que la traduction soit
  correcte des deux côté ».
- analyse:     `RepairPlanItem.Missing` (ADR-006 : ce qui restera manuel, montré avant achat) était un
  simple `IReadOnlyList<string>`, rempli à la source (`RepairEngine`, dans `PincabToolbox.Repair`, qui
  n'a et ne doit pas avoir de dépendance vers `Loc`/App) soit avec `Finding.FixHint` (toujours en
  anglais, c'est le fallback du Core), soit avec `PackStep.ReasonEn` pour une étape manuelle de
  scénario — en ignorant totalement `PackStep.ReasonFr`, qui existe et était déjà rempli dans le pack,
  juste jamais lu. L'App affichait ensuite ce texte brut tel quel à deux endroits (le résumé ADR-006 et
  le détail d'un item manuel dans l'onglet Repair), sans jamais passer par la table de traduction
  `Loc.FrFixHints` que l'App a pourtant déjà, par code de finding, et qui contenait déjà les 4 phrases
  FR correctes — elles n'étaient simplement jamais consultées pour ce texte-là. Le commentaire déjà
  présent sur `ItemConfirmation` (« Deliberately carries no formatted/localized text — that stays in
  the App's Loc layer ») disait la bonne architecture ; il manquait juste le dernier maillon.
- disposition: FIX. Nouveau type `RepairLimitation` (Core-side, bilingue : `Code`, `MessageEn`,
  `MessageFr?` — même forme que `Blocker` qui existait déjà pour un autre usage) remplace `string` dans
  `Missing`/`NotAutomatable`. Le pack alimente `MessageFr` directement pour les étapes de scénario
  (`ReasonFr`, enfin lu) ; pour le cas "pas de règle du tout" (ex. ROM_MISSING), le Core ne peut fournir
  que l'anglais (`FixHint`), donc `Code` est transporté et l'App résout via `Loc.MissingReasonText` :
  `MessageFr` du reason si présent, sinon `FrFixHints[Code]`, sinon l'anglais en dernier recours —
  jamais de ligne vide. Dédoublonnage de `RepairOffer.NotAutomatable` changé de "par texte brut" à
  "par Code" au passage — plus honnête (même raison, deux formulations, ne comptait pas comme deux
  limitations avant). Vérifié pièce par pièce sur les 4 codes de la capture : les 4 traductions FR
  existaient déjà telles quelles dans `Loc.cs`, jamais branchées. 152 tests Repair (dont 2 nouveaux
  verrouillant le passage bilingue), 498 tests Core, tous verts. App vérifiée par `csc -t:library`
  (0 erreur CS1xxx, sandbox ne peut pas builder le WPF). répondu ✔ (bundle livré, à rebuild côté cab).
- code:        aucun nouveau code de finding
- bac:         FIX (Repair, UI)
- contexte:    Maxime, sur son vrai cab : « ya un bouton pour annuler un plan historique et il annule
  rien tout simplement [...] les intitulés sont pas parlants, on voit pas le detail du plan donc on
  pourrait très bien annuler un plan qui a fonctionné [...] tu fais une colonne plan fait avec les
  corectifs faits, et plan annulé de l'autre, pour l'utilisateur c'est plus simple »
- analyse:     l'ancien "Historique d'annulation" était une seule ListBox de PlanId bruts
  (`plan-20260813-184700-1234`) + un bouton partagé "Annuler le plan sélectionné" qui exigeait de
  sélectionner une ligne d'abord — deux causes réelles à "ça n'annule rien" : (1) VPX/le frontend
  tournait, `RepairEngine.Undo` refuse dans ce cas (même règle que Preflight), message facile à rater ;
  (2) certains plans de l'historique venaient du tout premier test en
  `PINCAB_REPAIR_FORCE_DRYRUN=1` et n'avaient donc jamais rien appliqué pour de vrai — Undo répondait
  "ok" (rien à annuler, correct) sans dire pourquoi. Nouvelle méthode `RepairSession.Summarize(planId)`
  dérive tout du journal seul (jamais un état séparé à tenir synchronisé) : combien d'items complétés,
  combien annulés, quels fichiers touchés, et un `PlanOutcome` (Applied / PartiallyUndone /
  FullyUndone / NothingApplied / ForcedDryRun). Côté App : deux listes distinctes, "Réparé" (bouton
  Annuler individuel par ligne, plus de sélection séparée à oublier) et "Annulé" (lecture seule,
  rien à annuler). Les plans `ForcedDryRun`/`NothingApplied` sont exclus des deux, ni "réparé" ni
  "annulé" ne les décrirait honnêtement.
- disposition: corrigé et livré. 4 nouveaux tests `RepairSessionTests` (plan inconnu, apply réel,
  apply puis undo réel via `restore_rom_archive`, forced-dry-run jamais confondu avec un vrai apply).
  Core.Tests 498/498, Repair.Tests 151/151.

## 2026-08-13 (session éco, fin de journée) · Point 6/6 — le score ne s'effondre plus à 0 pour un même code Critical répété
- code:        aucun nouveau code de finding
- bac:         FIX (ScanScoring)
- contexte:    8 ROM_MISSING réels (8 tables différentes, même code) sur ~500 tables → score 0/100,
  grade F, alors que <2% de l'install est concernée ; Maxime : « il faut désormais qu'un même critical
  ne compte que 1, sinon la note s'effondre »
- analyse:     `ScanScoring.ComputeScore` retirait 15 points PAR INSTANCE de Critical, sans regarder le
  code. 8 occurrences du même code (8 tables cassées pour la même raison structurelle) coûtaient donc
  8×15=120, déjà au-delà de l'échelle 0-100 à elles seules, plafonnant le score à 0 quel que soit le
  reste de l'install, et de façon strictement identique que ce soit 8 ou 80 tables touchées — le score
  ne pouvait plus bouger tant que la dernière occurrence n'était pas corrigée. Pas fait un dédoublonnage
  strict (« ne compte qu'une fois, peu importe le nombre ») — ça rendrait 80 tables cassées identiques
  à 1 seule, ce qui n'est pas honnête non plus. Fait plutôt : la PREMIÈRE occurrence d'un code Critical
  distinct coûte toujours plein tarif (15 pts, un problème différent reste aussi grave qu'avant), les
  répétitions du MÊME code diminuent ensuite de façon logarithmique — même philosophie que les
  warnings plus bas dans le même fichier (`12*log(1+n)`, plafonné à 30). 8 ROM_MISSING coûtent
  maintenant ~32 points au lieu de 120. Des codes Critical distincts (vrais problèmes différents)
  continuent chacun de coûter plein tarif, inchangé.
- disposition: corrigé et livré. 4 nouveaux tests dans `ScoreTests` (répétition même code, comparaison
  explicite avec l'ancienne formule plate, codes distincts toujours plein tarif, occurrence unique
  inchangée). Core.Tests 498/498, Repair.Tests 147/147.

## 2026-08-13 (session éco, encore plus tard) · Point 6/6 — items manuels/verrouillés enfin visibles dans Repair (ADR-006), largeur de l'onglet Tables corrigée
- code:        aucun nouveau code de finding
- bac:         FIX (Repair) + FIX (UI) + FN (score, non traité ici)
- contexte:    rescan de 18h47 sur le vrai cab de Maxime : 8 vrais ROM_MISSING critiques, score 0/100,
  Repair « ne trouve rien à réparer », onglet Tables avec un grand vide noir à droite
- analyse:     (1) confirmé, ROM_MISSING n'a jamais de règle dans `knowledge/pack-2026.08.json` — ce
  n'est pas un oubli, Repair ne peut pas fabriquer un dump de ROM. Mais `RepairEngine.BuildFindingItem`
  laissait `Missing` vide dans ce cas précis (code sans règle du tout, distinct du cas « règle connue
  mais action introuvable » qui lui avait déjà un message), et `RefreshRepairItemsList` côté App ne
  rendait que les items avec `Changes.Count > 0` — les deux bugs cumulés faisaient disparaître
  entièrement les 8 ROM_MISSING de l'onglet Repair, alors que Scanner les affichait très bien avec leur
  indication (« Place le fichier .zip... »). Fixé : `Missing` reprend maintenant le `FixHint` du
  finding quand aucune règle n'existe, et Repair affiche tous les items retenus (case à cocher
  désactivée pour Manuel/Verrouillé, texte d'explication toujours visible). (2) Onglet Tables capé à
  920px de large et aligné à gauche, même réglage que Composants/Système où ça a du sens pour du texte
  court, mais laissait un grand vide sur un tableau à 4 colonnes avec des noms de table tronqués pour
  rien alors que l'espace était disponible. Étendu à toute la largeur.
- disposition: les deux corrigés et livrés. Le score à 0 malgré 8 critiques sur ~500 tables reste un
  sujet ouvert (formule actuelle : -15 par instance, pas par code, pas de normalisation par taille
  d'install) — question posée à Maxime, pas encore tranchée. La refonte demandée de la section Undo
  (3 colonnes Réparé/À faire/Annulé au lieu d'un historique à IDs opaques) reste aussi à faire,
  confirmée par Maxime mais pas encore codée.

## 2026-08-13 (session éco, plus tard) · Point 6/6 — `$Recycle.Bin` exclu du scan (feu vert reçu), 2 diagnostics de terrain rendus (Undo, échec Apply)
- code:        aucun nouveau code de finding
- bac:         FIX (LayoutDetector) + WORDING/FN (les deux diagnostics rendus, non corrigés ici)
- contexte:    rescan de 18h13 sur le vrai cab de Maxime, `$Recycle.Bin` toujours gagnant contre le
  vrai dossier `Tables\` pour la 3ᵉ fois consécutive
- analyse:     (1) `DriveInstallFinder` excluait déjà `$Recycle.Bin` et consorts d'un set privé
  `NoiseDirNames` (10/08) mais seulement pour son propre usage (repérer des racines candidates sur un
  scan multi-installs) — `LayoutDetector.SafeEnumerateDirs`, le walk réellement utilisé pour un scan
  `C:\` en un seul dossier (ce que fait Maxime), n'avait jamais ce filtre. D'où le bug : jamais corrigé
  parce que jamais présent au bon endroit. Fix : liste déplacée dans une seule source de vérité,
  `SystemNoiseDirs`, utilisée par `SafeEnumerateDirs` ET `DriveInstallFinder` — ferme le trou pour
  `LayoutDetector` lui-même, `BlockedFileScanner` et `CompletenessScanner` d'un coup, les trois
  s'appuyant sur ce même walk. (2) Diagnostic rendu sur "Annuler le plan sélectionné" qui semble ne
  rien faire sur certains plans historiques : `RepairEngine.Undo` refuse tant qu'un logiciel du cab
  tourne (même règle que Preflight, `Test_Undo_IsRefusedWhileVpxIsRunning` le couvre déjà), message
  facile à rater sous le bouton ; ou le plan choisi n'a jamais rien appliqué pour de vrai (ex. un plan
  du tout premier test en `PINCAB_REPAIR_FORCE_DRYRUN=1`), auquel cas Undo répond "ok" (rien à annuler,
  légitimement) sans dire clairement pourquoi il n'y a rien à annuler — piste FEATURE, pas encore codée.
  (3) Diagnostic rendu sur le "1 échoué(s)" à l'Apply (DLL bloqué) et sur "Repair ne détecte pas le
  problème pourtant vu par Scanner" en mode administrateur : confirmé dans le code,
  `RefreshRepairItemsList` (MainWindow.xaml.cs) ne montre QUE les items avec `Changes.Count > 0`, les
  items sans changement calculé (`ManualOnly`, y compris un item automatisable qui a échoué à trouver
  quoi que ce soit à changer) sont invisibles dans Repair, contre ADR-006 — piste FEATURE, pas encore
  codée. Cause probable du "1 échoué(s)" lui-même : lever le blocage Windows sur un fichier dans un
  dossier protégé (Program Files) demande d'écrire dessus, ce que Windows refuse sans élévation
  administrateur — pas encore confirmé (aucun message d'erreur par item n'est affiché aujourd'hui,
  autre symptôme du même trou ADR-006).
- disposition: `$Recycle.Bin` corrigé et livré (feu vert explicite de Maxime, 13/08). Les deux
  diagnostics (Undo peu clair, items manuels/échoués invisibles dans Repair) restent à coder, prochains
  points annoncés à Maxime avec son accord sur l'ordre.

## 2026-08-13 (session éco) · Point 6/6 en cours — première vraie clé publique de licence embarquée ; 2 problèmes de terrain trouvés sur le vrai cab de Maxime
- code:        aucun nouveau code de finding
- bac:         FIX (licence) + FN/FEATURE (les deux trouvailles terrain, non corrigées ici)
- contexte:    point 6/6, premier vrai scan de Maxime sur son cab via `PINCAB_REPAIR_FORCE_DRYRUN=1`
- analyse:     (1) Maxime a lancé `license-tool init` chez lui et donné la clé publique générée,
  embarquée dans `LicenseVerifier.EmbeddedPublicKeyBase64` — la clé privée n'a jamais transité par ce
  sandbox, conformément au design du tool. (2) Scanner `C:\` en entier fait gagner `$Recycle.Bin`
  contre le vrai dossier `Tables\` dans `LayoutDetector` (les fichiers `$I.../$R...` de la corbeille
  matchent `*.vpx` avant que le vrai dossier soit atteint) — cas d'usage central pour Maxime
  ("le mieux c'est d'analyser le disque en entier"), pas encore corrigé, en attente de son signal.
  (3) Bug d'affichage mineur : racine sans dossier Tables → chemins rendus `.\C:\Visual
  Pinball\...` (`.\` collé devant un chemin déjà absolu). (4) Repéré en creusant le rapport HTML :
  `PathScrubber.Scrub` protège bien le vrai nom de compte Windows dans les chemins (ADR-003 tient),
  mais le remplacement est appliqué au texte entier du rapport, pas seulement aux chemins — si le nom
  de compte Windows contient "Pincab" (cas plausible pour ce produit), la marque elle-même
  ("Pincab Toolbox", l'URL) se fait écraser en "<user> Toolbox" dans le rapport exporté. Mineur,
  sens de l'erreur sûr (sur-scrubbing, pas fuite), noté pour plus tard.
- disposition: (1) livré (bundle) · Core.Tests 488/488, Repair.Tests 145/145. (2), (3), (4) consignés,
  pas codés dans ce commit — (2) est le plus important, bloque un test Repair vraiment représentatif
  tant que ce n'est pas corrigé ou que Maxime pointe une racine plus précise.

## 2026-08-13 (session éco) · Point 5/6 revue CTO+Produit — Scenarios.cs + RowPlanning.cs déplacés vers PincabToolbox.Core.Diagnostics, PincabToolbox.App.Tests retiré
- code:        aucun nouveau code de finding
- bac:         FEATURE (chantier planifié, point 5/6, signal « GO » de Maxime reçu après clôture du
  point 4/6)
- contexte:    point 5/6 de la revue CTO+Produit ; portée = déplacer Scenarios.cs et la logique de
  décision vers PincabToolbox.Core (ADR-012), avec tests
- analyse:     déplacement propre de `Scenarios.cs` et `RowPlanning.cs`
  (`ChainRowPlanner`/`TableRowPlanner`, extraits en mini-tranche anticipée au point 3) vers un
  nouveau dossier `PincabToolbox.Core/Diagnostics/`, même convention que `Models`/`Scanning`. Seul
  changement de comportement, strictement nécessaire : `Scenarios.DetectAll`/`Detect` lisaient
  `PincabToolbox.App.Localization.Loc.Lang` directement, ce qui n'est plus possible depuis Core (App
  référence Core, jamais l'inverse) — les deux méthodes prennent maintenant un `bool fr` explicite,
  le seul appelant (`MainWindow.xaml.cs`) passe `Loc.Lang == "fr"`, comportement utilisateur
  identique. `RowPlanning.cs` n'a demandé aucun changement, il ne dépendait déjà que de
  `PincabToolbox.Core.Models`. `PincabToolbox.App.Tests` (le projet-pont temporaire créé au point 3
  spécifiquement pour tester ces deux fichiers sans le SDK Windows Desktop) est retiré en entier —
  projet, dossier, entrée `.sln` — conformément à ce que son propre commentaire de `.csproj`
  annonçait déjà. Les deux fichiers de tests migrent tels quels dans
  `tests/PincabToolbox.Core.Tests/`. Effet de bord positif du passage à `fr: bool` explicite : le
  vieux helper `WithLang`/`Loc.SetLang` (état statique process-wide, source de fragilité documentée
  par le fichier lui-même) disparaît des tests, remplacé par un simple `fr: true`/`fr: false` passé
  directement à chaque appel.
- disposition: livré (bundle) · Core.Tests 488/488 (439 + 49, migration exacte du compte App.Tests
  d'avant ce point), Repair 145/145. `PincabToolbox.Core` compile maintenant pour de vrai dans ce
  sandbox (`dotnet build`, 0 warning/0 erreur) — Core n'a jamais eu de dépendance WPF, donc ce
  déplacement lui fait gagner une vérification plus forte que le `csc -t:library` syntax-only utilisé
  pour le reste. `csc -t:library` sur l'App (6 fichiers .cs restants) : toujours uniquement
  CS0234/CS0246/CS0518/CS0656, zéro CS1xxx. `build.cmd` renumérotation `[1/5]`→`[5/5]`. En attente du
  signal de Maxime avant le point 6 (premier run Repair réel sur son cab,
  PINCAB_REPAIR_FORCE_DRYRUN=1).

## 2026-08-13 (session éco) · Point 4/6 revue CTO+Produit — 3 nouveaux scénarios dans Scenarios.cs (BITNESS_MISMATCH_VPM32, COM_STALE_PATH, AltSound/AltColor désactivés)
- code:        aucun nouveau code de finding (les 3 scénarios utilisent des codes existants,
  jusqu'ici non repris par aucun scénario)
- bac:         FEATURE (chantier planifié, point 4/6, signal « GO » de Maxime reçu après clôture du
  point 3/6)
- contexte:    point 4/6 de la revue CTO+Produit ; portée = ajouter des scénarios de corrélation à
  `Scenarios.DetectAll`
- analyse:     3 candidats choisis en repartant de la liste complète des codes de finding existants
  et en cherchant ceux qui, comme `VPINMAME_NOT_REGISTERED` (point 3/6 quater), sont déjà un
  diagnostic complet à eux seuls — donc MinMatch=1, zéro corrélation inventée entre scanners
  différents. `BITNESS_MISMATCH_VPM32` (Critical, `BitnessScanner.cs`) est le miroir jamais utilisé
  du premier scénario (VPX 32-bit + VPinMAME 64-bit-only au lieu de l'inverse) ; gardé comme Def
  séparée plutôt que fusionné dans le scénario 1 dont le texte de chaîne est câblé en dur pour
  l'autre sens. `COM_STALE_PATH` (Warning, `ComHealthScanner.cs`) : le composant est enregistré ET le
  chemin enregistré n'existe plus, les deux faits mesurés par le scanner lui-même ; confiance de base
  fixée à 68 (un cran sous les scénarios Critical à 80) pour refléter la sévérité réelle sous-
  jacente. `ALTSOUND_PRESENT_NOT_ENABLED` + `ALTCOLOR_PRESENT_NOT_ENABLED` (Note,
  `FeatureEnabledScanner.cs`, LOT D) : combinés en MinMatch=1 parce que ce n'est pas une corrélation
  entre deux scanners différents façon scénario 1/2, c'est le même patron « installé mais l'option
  VPinMAME est encore à 0 » mesuré deux fois (son, couleur) — l'un seul est déjà un diagnostic
  complet.
- disposition: livré (bundle) · Core 439/439 (inchangé, ce point ne touche pas Core), Repair 145/145
  (inchangé), App.Tests 49/49 (38 + 11 nouveaux : déclenchement MinMatch=1 pour chacun, filtrage de
  chaîne par code, calcul exact de confiance, non-collision avec le scénario 1 existant, textes FR,
  cohabitation des 6 scénarios ensemble). `csc -t:library` : toujours zéro CS1xxx. Diff strictement
  scopé à `Scenarios.cs` + `ScenariosTests.cs`. En attente du signal de Maxime avant le point 5.

## 2026-08-13 (session éco) · Point 3/6 suite — décision de Maxime = extraire maintenant. ChainRowPlanner + TableRowPlanner, MainWindow rebranché dessus
- code:        aucun nouveau code de finding
- bac:         FEATURE (tests, chantier planifié) — suite directe de l'entrée juste en dessous, la
  question posée à Maxime a une réponse
- contexte:    Maxime a choisi « Extraire la logique maintenant » (mini-tranche anticipée du point 5,
  décrite dans la question comme « sortir la logique de décision pure des 4 méthodes qui touchent des
  Brush vers des fonctions testables, avec de vrais tests »)
- analyse:     2 des 4 méthodes `Build*` de `MainWindow.xaml.cs` avaient une logique de décision assez
  autonome pour être sortie proprement : `BuildChainRows` → `ChainRowPlanner.Plan` (quel step est le
  point de coupure ✕→ dans la chaîne causale) ; `BuildTableRows` → `TableRowPlanner.PlanRom`/`PlanB2s`/
  `PlanFrontend` (quelle finding ROM/B2S/Frontend gagne pour chaque table du tableau). Nouveau fichier
  `src/PincabToolbox.App/RowPlanning.cs`, zéro WPF, lié par chemin dans
  `tests/PincabToolbox.App.Tests` comme `Scenarios.cs`/`Loc.cs` déjà. `MainWindow.xaml.cs` rebranché
  pour appeler ces planners (comportement inchangé, vérifié ligne à ligne avant/après) ; il ne reste
  dans les deux méthodes que la traduction Brush/texte localisé, quelques lookups triviaux. `Build
  CauseCard` et `BuildComponentRows` volontairement PAS extraites cette fois : trop enchevêtrées avec
  l'état d'instance `_report!` et le pluriel/singulier de `Loc` pour être une mini-tranche propre —
  reportées au point 5 en entier, notées comme dette assumée plutôt que bâclées ici. Un test a été
  corrigé en cours de route : son nom/commentaire affirmait que seule la PREMIÈRE transition bon→mauvais
  compte comme point de coupure, mais l'algorithme (et l'assertion elle-même) marque CHAQUE transition
  bon→mauvais indépendamment — renommé `Test_Every_GoodToBad_Edge_Is_A_Cut_Point_Not_Just_The_First`
  avec un commentaire qui documente ce comportement intentionnel-mais-surprenant plutôt que de le
  cacher.
- disposition: livré (bundle) · Core 439/439, Repair 145/145, App.Tests 38/38 (18 Scenarios + 7
  ChainRowPlanner + 13 TableRowPlanner, tous nouveaux/réels). `csc -t:library` sur les 7 fichiers .cs
  de l'App : uniquement CS0234/CS0246/CS0518/CS0656, zéro CS1xxx après le rebranchement. Point 3/6
  maintenant complet dans les limites du sandbox — en attente du signal de Maxime avant le point 4.

## 2026-08-13 (session éco) · Point 3/6 revue CTO+Produit — tests Scenarios.DetectAll (fait) ; Build* de MainWindow (bloqué, décision à prendre)
- code:        aucun nouveau code de finding
- bac:         FEATURE (tests, chantier planifié) — point livré à moitié, l'autre moitié posée en
  question à Maxime plutôt que devinée
- contexte:    point 3/6 de la revue CTO+Produit ; portée initiale = tests pour
  `Scenarios.DetectAll` ET les `Build*` de `MainWindow.xaml.cs`
- analyse:     `Scenarios.DetectAll` ne dépend que de `Loc.Lang` (champ statique, zéro WPF) → 18
  tests réels écrits et exécutés dans un nouveau projet `tests/PincabToolbox.App.Tests/` qui lie
  `Scenarios.cs`/`Loc.cs` par fichier plutôt que de référencer `PincabToolbox.App.csproj` (lequel
  exige le SDK Windows Desktop, absent ici). Les `Build*` de `MainWindow.xaml.cs`, en revanche, sont
  structurellement intestables dans ce sandbox aujourd'hui, pour deux raisons cumulées : le SDK
  Windows Desktop est nécessaire même pour compiler les types `Brush` qu'utilisent 4 des méthodes
  (`BuildCauseCard`, `BuildChainRows`, `BuildComponentRows`, `BuildTableRows`) et `nuget.org` est
  bloqué par le proxy du sandbox (vérifié : 403) donc impossible d'aller chercher même le paquet de
  référence WPF ; ET, plus fondamental, `MainWindow` est une moitié de `partial class` — l'autre
  moitié (champs `TxtRoot`/`BtnScan`/…) vient du XAML compilé, donc même les `Build*` sans aucun
  `Brush` (`BuildTextReport`, `BuildPdfLines`, etc.) ne peuvent pas être isolés par lien de fichier
  comme `Scenarios.cs` l'a été — il n'y a pas de fichier à lier séparément de la classe entière.
- disposition: livré (bundle) pour la moitié faite · Core 439/439, Repair 145/145, App.Tests 18/18
  (nouveau, réel). `.sln` et `build.cmd` mis à jour ([4/6] nouvelle étape, renumérotation complète
  au passage — le script avait déjà [1/4]→[5/5] avant ce point). Décision demandée à Maxime pour la
  moitié `Build*` : extraire la logique de décision maintenant (mini-tranche anticipée du point 5),
  reporter au point 5 en entier, ou se contenter d'une vérification `csc`/relecture manuelle sans
  vrais tests.

## 2026-08-13 (session éco) · Point 2/6 revue CTO+Produit — export PDF (générateur maison, zéro dépendance) + fold-in wording GROUPED
- code:        aucun nouveau code de finding ; message `GROUPED` (rollup) reformulé
- bac:         FEATURE (export PDF, chantier planifié) + WORDING (fold-in accepté par Maxime le
  13/08, voir l'entrée Gregg juste au-dessus)
- contexte:    point 2/6 de la revue CTO+Produit qui suit le portage Scanner du 12/08 ;
  `NuGet.Config` = zéro dépendance pour tout le repo, `PincabToolbox.App.csproj` n'a aucun
  `PackageReference` → pas de lib PDF tierce disponible
- analyse:     générateur PDF minimal écrit à la main dans
  `PincabToolbox.Core/Reporting/PdfDocumentBuilder.cs` (objets/xref/trailer, police Helvetica
  standard + WinAnsiEncoding, retour à la ligne glouton avec coupure dure des mots trop longs,
  pagination A4). Décision d'architecture délibérée, notée comme incohérence assumée : contrairement
  aux 5 autres formats (tous dans `MainWindow.xaml.cs`, jamais testés dans ce sandbox — l'App ne
  compile pas, NU1100), la mécanique PDF pure vit dans Core parce que c'est la partie la plus
  risquée (format binaire fait main) et la seule qui peut être réellement testée ici. Deux bugs
  trouvés en écrivant les tests, avant tout envoi : `Encoding.ASCII` aurait mangé tout caractère
  accentué en `?` (corrigé en `Encoding.Latin1`, 1:1 avec WinAnsi 0x00-0xFF) ; le positionnement des
  lignes traitait `Td` (déplacement RELATIF) comme absolu, ce qui aurait fait dériver chaque ligne
  de plus en plus à droite et vers le haut. PDF n'utilise pas `Rolled()` comme HTML/MD/BBCode — comme
  TXT/JSON, tout est affiché, c'est le format "je veux tout voir". Message `GROUPED` (rollup, visible
  sur les captures de Gregg — "273 similar findings") reformulé pour nommer explicitement .txt/.pdf/
  .json au lieu d'un vague "rapport texte complet" (En + Fr).
- disposition: livré (bundle) · Core 439/439 (412 + 27 nouveaux tests PDF), Repair 145/145, `csc
  -t:library` sur l'App entière (7 fichiers .cs) : uniquement CS0234/CS0246/CS0518/CS0656, zéro
  CS1xxx. Vérification supplémentaire au-delà des tests propres : un vrai PDF généré (rapport
  simulé, 80 findings, accents français, guillemets, tiret, glyphe ✓) relu par `pypdf` (lib tierce
  utilisée uniquement pour cette vérification ponctuelle en sandbox, hors périmètre du produit
  livré) — 7 pages, texte extrait lisible, accents corrects, ✓ transformé en "OK", pieds de page
  présents. Amélioration à faible coût repérée, NON codée : migrer les 5 autres builders d'export
  vers Core suivrait le même raisonnement et leur donnerait enfin une vraie couverture de test — pas
  fait ici, hors périmètre de ce point, gros changement pour du code qui marche déjà sans
  signalement de bug.

## 2026-08-13 (suite) · Gregg répond à la question de clarification ROM · pas assez pour coder, redirige vers JPSalas
- code:        ROM_MISSING (mêmes 2 cas que l'entrée juste en dessous)
- bac:         FP (toujours pas codé — toujours pas assez d'info pour changer la détection en confiance)
- contexte:    réponse de Gregg à la question posée dans `docs/reply-gregg-2026-08-13.md` (« quand tu
  retires le zip, quelque chose change à l'écran ou au son, ou la table tourne identique ? »)
- verbatim:    « That's it .. I do not have any roms (.zip) as mentioned for those tables in my
  vpinmame/roms folder .. so that being mentioned in the tablescript is for ..? As I don't do intens
  vpx scripting I can not answer you I'm affraid. You might get your answer by contacting JPSalas ..
  who did a lot of original tables. »
- analyse:     confirme explicitement l'absence totale du fichier ROM (déjà implicite avant, main-
  tenant sans ambiguïté) et que la table tourne quand même — mais ne répond pas à la question posée
  (rien vu/entendu de différent) et ne peut pas expliquer pourquoi le script appelle quand même
  VPinMAME.Controller, il n'écrit pas de script lui-même. Toujours aucun accès au script réel des deux
  tables. La règle du 12/08 tient : pas de détection `On Error Resume Next`/try-guard codée sur une
  hypothèse non vérifiée, le risque est de rouvrir un vrai faux négatif sur les tables qui plantent
  réellement sans ROM.
- disposition: réponse de clôture de fil rédigée (le remercier, ne pas le relancer puisqu'il a dit ne
  pas pouvoir aider davantage, mention que JPSalas est une piste si Maxime veut la suivre plus tard) —
  à valider par Maxime avant envoi. Wording ROM laissé tel quel, aucun code touché. Fil en pause tant
  qu'on n'a pas le script réel ou un retour de quelqu'un qui sait le lire.

## 2026-08-13 · Gregg (forum, suite du 12/08) · possible FP ROM sur EM/homebrew + notre propre réponse du 12/08 était fausse sur le contenu des exports
- code:        ROM_MISSING (2 cas signalés) + export HTML/MD/BBCode (rollup non mentionné dans notre
  réponse du 12/08)
- bac:         FP (à investiguer, PAS codé — règle des 48h) + WORDING (confirmé, celui-ci vient de
  nous)
- contexte:    Gregg répond à la réponse du 12/08 avec captures d'écran d'un nouveau scan
- verbatim:    « The Full House pinball machine released by Williams in March 1966 is an
  electro-mechanical (EM) game with no digital rom chip. » · « For the homebrew tables […] there is
  a 'rom' mentioned in the vp-script […] but not present in the vpinmame/rom folder. The report
  states that the table will not start, but it runs smoothly without the actual rom. […] These
  tables don't touch VPinMAME, but are flagged? » · « I've downloaded the full report […] but it
  seems that there's no way to open the 'detailed report'? »
- analyse:     **ROM (2 cas, non codé)** — `ScriptAnalyzer.AnalyzeRomUsage` exige un
  `CreateObject("VPinMAME.Controller")` réel et non commenté (regex `VpinmameCreate()`, commentaires
  déjà retirés par `StripComments`) pour poser `UsesController = true` ; un nom de ROM seul
  (`cGameName`) sans cet appel donne `ROM_NOT_REQUIRED`, pas Critical. Donc SI ces tables sont
  vraiment flaggées Critical, leur script contient bien un vrai `CreateObject("VPinMAME.Controller")`
  — pas un artefact de détection sur un mot-clé. L'hypothèse la plus probable : ces scripts créent le
  contrôleur pour une fonctionnalité optionnelle (DMD/son additionnel sur une table EM reproduite en
  VBScript pur, ou reprise homebrew) et encadrent l'appel réel (`Controller.run`/chargement ROM) dans
  une gestion d'erreur qui permet à la table de tourner quand même sans le ROM — ce que
  `ScriptAnalyzer` ne regarde pas aujourd'hui (il détecte l'intention d'utiliser VPinMAME, pas si
  l'appel est protégé). Impossible à confirmer sans le script réel de Gregg (ni Full House ni le
  homebrew ne sont dans `DemoData`). **Ne pas coder une détection de `On Error Resume Next`/
  try-guard à l'aveugle** : bon candidat pour réintroduire un faux négatif sur les tables qui
  plantent vraiment sans ROM (le cas Full House d'origine, KPI#1, était déjà de cette famille).
  Question de clarification à renvoyer à Gregg avant tout code.
  **Export (confirmé, notre erreur)** — la réponse du 12/08 affirmait que "each format includes the
  full detail […] not just what's shown in the table". **Faux pour HTML/MD/BBCode** :
  `BuildHtmlReport`/`BuildForumMarkdown`/`BuildBBCode` appellent tous `r.Rolled()`, qui regroupe les
  findings répétitifs (ex. 273×`B2S_ORPHAN`) sous une seule ligne « collapsed to keep this list
  readable. The full text report has every one of them. » (`ScanScoring.Rolled`, même texte en
  Fr : « Le rapport texte complet les contient tous. »). Seuls **TXT et JSON** utilisent `Ordered()`
  (aucun regroupement). Gregg a très probablement exporté en HTML (option par défaut du dialogue de
  sauvegarde), vu la ligne "273 similar findings" et cherché en vain où voir les 273 — le message ne
  dit pas explicitement "choisis .txt ou .json dans le menu déroulant du dialogue d'export", il dit
  juste "rapport texte complet", ambigu si on ne sait pas que TXT est un format au choix dans le
  MÊME bouton "Export report" qu'il vient d'utiliser.
- disposition: à répondre (brouillon `docs/reply-gregg-2026-08-13.md`, à valider par Maxime avant
  envoi) · ROM : aucun code touché, question de clarification renvoyée à Gregg d'abord · Export :
  correctif de formulation candidat à faible coût (pas de changement de `Rolled()`/`Ordered()`,
  juste rendre le message de regroupement actionnable — ex. nommer explicitement le format .txt/.json
  dans le dialogue), à faire quand le point 2 (export PDF) sera traité puisque c'est la même zone de
  code, PAS mélangé dans ce commit-ci

## 2026-08-13 (session éco) · Point 1/6 revue CTO+Produit — zips ROM factices dans le DemoData
- code:        aucun nouveau code de finding (`ROM_OK`/`ROM_MISSING`/`ROM_NOT_REQUIRED` existaient
  déjà) ; les résultats concernés changent : `ROMS_DIR_NOT_FOUND` disparaît du démo, remplacé par
  2× `ROM_OK` + 1× `ROM_MISSING` (Critical) + 1× `ROM_NOT_REQUIRED`
- bac:         FEATURE (implémentation de l'amélioration à faible coût notée le 2026-08-12, pas
  codée à l'époque)
- contexte:    `DemoData/install/VPinMAME/roms/` n'existait pas → colonne ROM du mode démo à « — »
  partout, `RomValidatorScanner` s'arrêtait sur `ROMS_DIR_NOT_FOUND` avant même de lire les tables
- analyse:     ajout de `afm_113b.zip` et `afm_113.zip` (fixtures vides, un seul fichier texte
  `*.READ-ME-FAKE-ROM.txt` à l'intérieur disant que ce n'est pas une vraie ROM — aucune donnée
  MAME/VPinMAME, rien sous copyright). `mm_109c.zip` (Medieval Madness) volontairement absent.
  Effet, mesuré par harnais `dotnet run` jetable contre `PincabToolbox.Core` (hors Windows) :
  Attack From Mars → `ROM_OK` direct ; Aliased Table (Test 2020) → `ROM_OK` via l'alias VPMAlias
  `afm_mod → afm_113` (chemin de résolution d'alias enfin exercé par le pipeline démo, jusque-là
  seulement couvert par les tests unitaires `AliasFileTests`) ; Medieval Madness → `ROM_MISSING`
  Critical (nouveau résultat vedette du démo, même histoire que le rapport terrain de Gregg du
  12/08 — une ROM manquante sur une table qui pilote réellement VPinMAME) ; Original Gem
  (Homebrew) → confirmé `ROM_NOT_REQUIRED` (aucun `GameName`/`CreateObject` VPinMAME dans son
  script). Score démo (hors Windows) : 68/C avant → 57/C après, 1 critique → 2. Zéro code touché :
  `RomValidatorScanner` compare des noms de fichiers, `MainWindow.BuildTableRows`/`BuildCauseCards`
  et les clés `Loc["tbl.rom.*"]` pilotaient déjà tout par les codes existants. Seule chose non-DemoData
  modifiée : `.gitignore` (exception scopée à la règle globale `*.zip`, pour ne pas l'affaiblir
  ailleurs). Vérifié : aucun des 3 scénarios actuels ne déclenche sur `ROM_MISSING` (RequiresCode
  passés en revue), donc le repli « carte du résultat le plus grave sans scénario » porte
  correctement ce nouveau Critical — comportement attendu.
- disposition: livré (bundle) · à vérifier sur la machine de Maxime via `build.cmd` + Mode démo.
  Core 412/412 + Repair 145/145 verts (sandbox reconstruit en session fraîche : .NET 8 SDK
  réinstallé, fixtures Core.Tests régénérées via `make_fixtures.py`, baseline revérifiée identique
  avant tout changement). Amélioration à faible coût repérée, NON codée : un scénario MinMatch-1
  dédié à `ROM_MISSING` seul (candidat pour le point 4, nouveaux scénarios) donnerait à ce cas —
  le plus fréquent en usage réel d'après Gregg — sa propre carte au lieu du repli générique.

## 2026-08-12 (session éco) · Écran Scanner porté sur la maquette du 11/08 — en une passe
- code:        transverse UI (aucun nouveau code de finding) + `Scenarios.DetectAll` (liste triée au
  lieu du seul meilleur) + nouveau scénario `VPINMAME_NOT_REGISTERED` (MinMatch 1 — les 4 conditions
  du LOT A sont toutes mesurées, un seul code est déjà un diagnostic complet)
- bac:         FIX (3ᵉ reprise du même retour de Maxime : « l'écran ne ressemble pas à la maquette »)
- contexte:    `docs/maquette-scanner-2026-08-11.html` = la cible ; portage complet en une passe
  plutôt que des retouches successives (chaque aller-retour coûte un `build.cmd`).
- analyse:     ce qui manquait a été porté : ligne méta sous le bandeau (mode, lancé le, durée,
  contrôles N/N, tables), onglets internes (Causes racines / Tous les résultats / Composants /
  Tables / Système), cartes de causes racines en liste (badge de gravité MESURÉE, titre, puce de
  confiance en mots, phrase joueur, phrase d'impact, chaîne causale par scénario dont chaque case
  exige son code déclencheur, pied composants/tables/codes/réparation manuelle + « Voir les
  étapes » qui saute sur le résultat), colonne de droite (Résultats critiques, Santé des
  composants, Remarques), carte réparation honnête (offre réelle ou « aucune réparation
  automatique »), tableau des tables (ROM/Backglass/Frontend par table).
  **Vérité terrain consignée** : le scan réel du DemoData (hors Windows) donne 17 résultats,
  score 68/C, 1 critique, 2 causes racines — PAS les 27/38/F/3 critiques de la maquette, qui
  supposait un dossier `roms/` absent du DemoData et des scanners registre/écrans qui ne parlent
  que sous Windows. La colonne ROM du démo affiche donc « — » partout (ROMS_DIR_NOT_FOUND
  l'explique dans la liste). Sur le poste Windows de Maxime, le démo produira EN PLUS les
  résultats COM/écrans/audio de SA machine. Écarté volontairement : ligne « FlexDMD — non
  requis » (déduction du silence d'un scanner, contraire à la doctrine affichée dans l'encadré
  même), voyants réseau (ADR-002), pourcentages de confiance (ADR-010), vignettes-objets SVG
  (hors périmètre énuméré). Badge de la carte « Intégration frontend » : « À noter », pas
  « Avertissement » comme la maquette — c'est la gravité réellement mesurée (Info + Note).
- disposition: livré (bundle) · à vérifier EN PREMIER sur la machine de Maxime via `build.cmd` +
  Mode démo — l'App ne compile toujours pas dans le sandbox (NU1100, fait documenté) ; vérifié ici
  par XML bien formé, passe `csc` sans références WPF (zéro CS1xxx), script de recoupement
  x:Name/gestionnaires/assets (0 erreur), exécution du VRAI `Scenarios.cs` contre le vrai scan
  démo (2 scénarios, conf 90/86, chaînes conformes), Core 412/412 + Repair 145/145 verts.
  Amélioration à faible coût proposée SANS être codée : enrichir `DemoData` d'un dossier
  `roms/` (afm_113b.zip + afm_113.zip) pour que le mode démo raconte la même histoire que la
  maquette (critique ROM réelle sur Medieval Madness, colonne ROM remplie).

## 2026-08-12 · Gregg — suite du 07/08 sur FlexDMD, cette fois avec captures d'écran exploitables
- code:        ROM_MISSING (confirmé exact), FLEXDMD_MISSING, B2SBACKGLASS_MISSING (question d'usage,
  pas un bug), col.message "Details" (source de la confusion sur "comment ouvrir le rapport complet")
- bac:         WORDING (rapport pas assez visible) + question d'usage pure, aucun FP
- contexte:    Gregg a répondu à la relance du 07/08 (treize) avec 3 captures d'écran d'un scan réel :
  avertissements FlexDMD.dll et B2SBackglassServer.dll manquants, un Critical ROM_MISSING sur
  'Full House (Williams 1966)', écran À propos v0.1.2. Deux questions précises cette fois : où trouver
  le rapport texte complet mentionné par la colonne "Details" du tableau, et s'il existe un moyen
  d'éviter les alertes ROM manquant pour les tables qui n'en ont pas besoin.
- analyse: vérifié `RomValidatorScanner.cs` avant de répondre. Le scanner fait déjà exactement ce que
  Gregg demande — `ROM_NOT_REQUIRED` (Ok) sort dès que `!rom.UsesController`, une table originale/
  homebrew qui se contente d'un B2S sans piloter VPinMAME n'est jamais remontée en Critical. 'Full
  House (Williams 1966)' est une vraie table Williams qui pilote VPinMAME : le Critical est exact, pas
  un FP, il lui manque juste `Full House.zip` dans son dossier roms. Le "rapport complet" que Gregg
  cherche existe déjà (bouton "Export report" en HTML/TXT/MD/BBCode/JSON + "Copy for forum" qui copie
  directement le Markdown), mais il ne l'a pas trouvé — signal de découvrabilité faible, pas un bug de
  contenu.
- disposition: réponse rédigée dans `docs/reply-gregg-2026-08-12.md`, à poster par Maxime. Aucune
  correction de code nécessaire, le comportement mesuré est déjà correct sur les deux points. Idée à
  faible coût notée pour une prochaine revue produit (pas codée) : rendre "Export report"/"Copy for
  forum" plus visibles (ex. les répéter dans un menu contextuel du tableau), puisque c'est la 2e fois
  qu'un utilisateur terrain ne les trouve pas.

## 2026-08-11 (session Sonnet 5, autonome, effort élevé) · Lot communauté 10/08 — LOTs A→H codés et câblés, LOT I codé mais délibérément non câblé, ADR-012
- code:        transverse — LOT A (COM_NOT_REGISTERED, COM_STALE_PATH, COM_PATH_OUTSIDE_INSTALL,
  COM_OK, COM_BITNESS_GAP, VPINMAME_NOT_REGISTERED), LOT B (CHAIN_BITNESS_GAP), LOT C
  (DMD_VIRTUAL_DISABLED, DMD_POSITION_OFFSCREEN), LOT D (ALTSOUND_PRESENT_NOT_ENABLED,
  ALTCOLOR_PRESENT_NOT_ENABLED), LOT E (extension BlockedFileScanner à .exe/.ocx), LOT F
  (SCREENRES_UNPARSED), LOT G (NVRAM_FOLDER_NOT_WRITABLE), LOT H (chemin d'écriture Repair, aucun
  nouveau code de finding), LOT I (`register_com_component`, codé et testé, pas câblé)
- bac:         FIX (7 lots de détection) + FEATURE (chemin d'écriture Repair câblé pour la première fois)
- contexte:    Reprise de `docs/SPEC-lot-communaute-2026-08-10.md` (lue intégralement) +
  TRANSMISSION.md + ADR-005/006/010/011, sur mandat explicite de Maxime : coder et câbler les lots
  A→I, LOT H livré entièrement ou pas du tout (H.1 journal persistant en premier), LOT A.3
  (`VPINMAME_NOT_REGISTERED`) avec les 4 conditions mesurées et jamais supposées, ne pas
  re-coder ce qui existe déjà (`FLEXDMD_MISSING` était déjà câblé, cf. correction ci-dessous).
  Ordre d'abandon en cas de manque de temps : G, F, E, D, C — non utilisé, tout a été livré.
- analyse (journal par thème) :

  **Correction factuelle reportée depuis TRANSMISSION.md 10/08 (bis), maintenant actée ici** :
  `FLEXDMD_MISSING` n'est pas une chaîne morte — `DependencyScanner.cs:80` l'émet déjà en `Warning`
  sur un signal composite correct. Ce qui manquait réellement sur FlexDMD était l'enregistrement COM
  et la cohérence bitness, objet du LOT A ci-dessous. Aucune "spec du 08/08" introuvable à retrouver.

  **LOT A — `ComHealthScanner` (nouveau scanner).** Lit les deux vues du registre COM (32/64 bits,
  via le `ComRegistrationProbe` déjà présent) pour VPinMAME.Controller, B2S.Server, FlexDMD.FlexDMD
  (+ PinUpPlayer.PinDisplay plafonné à `Note`, identifiant non recoupé). `VPINMAME_NOT_REGISTERED`
  (premier `Critical` depuis le gel du 03/08) exige ses 4 conditions **toutes mesurées** : DLL
  présente sous la racine, LES DEUX lectures registre ont réussi (un échec de lecture = silence
  total, jamais un `Critical` de repli), le ProgID absent des deux vues, et au moins une table
  l'exige réellement. Testé en écrivant le test d'échec de lecture EN PREMIER, comme demandé.

  **LOTs B→G — 6 scanners neufs, un scanner existant étendu.** `ChainBitnessScanner` (LOT B),
  `DmdConfigScanner` + extension de `DmdDeviceIniParser` pour le format `[VirtualDMD]` de
  dmd-extensions, confirmé par lecture directe de son `DmdDevice.ini` sur GitHub (LOT C),
  `FeatureEnabledScanner` + `AltFeatureRegistry` (clés `sound_mode`/`dmd_colorize` sous
  `HKCU\...\Visual PinMame\<rom>`, confiance de source documentée honnêtement en commentaire — même
  posture que le caveat déjà existant sur la clé de port COM DMD) (LOT D), extension de
  `BlockedFileScanner` à `*.exe`/`*.ocx` en plus de `*.dll`, `CriticalNames` inchangé (LOT E),
  `ScreenResUnparsedScanner`, mutuellement exclusif avec `ScreenTopologyScanner` par construction
  (LOT F), `NvramWritabilityScanner`, sonde d'écriture réelle (créer+supprimer) (LOT G). **Aucun
  scanner existant modifié hors `BlockedFileScanner`** (règle explicite du lot). `.Add(...)` câblés
  dans `MainWindow.xaml.cs`, `Loc.cs` FR+EN et `Knowledge.cs` complétés pour les 13 nouveaux codes.

  **LOT H — chemin d'écriture Repair, câblé pour la première fois.** H.1 (journal persistant,
  `FileRepairJournal`, JSONL sur disque) fait en premier comme exigé — prérequis dur du reste.
  Nouvelle classe `RepairSession` (`PincabToolbox.Repair`, pas `PincabToolbox.App` — voir ADR-012
  pour la justification complète) qui compose Preflight/Apply/Undo avec licence revérifiée à chaque
  appel (jamais assumée) et sélection d'items strictement opt-in (jamais un "tout réparer"
  silencieux). **Bug réel trouvé et corrigé pendant ce chantier** : `RepairEngine.Apply` ne
  protégeait pas l'échec de `IBackupService.Backup` — corrigé par un `try/catch` qui journalise
  `JournalEvent.BackupFailed` et n'écrit jamais si la sauvegarde échoue (règle H.2 n°4, test
  `Test_Apply_BackupFailure_NeverWrites`). Onglet "Repair" ajouté à `MainWindow.xaml`/`.xaml.cs`
  (licence, construction de plan, liste d'items à cocher avec confirmation explicite obligatoire
  pour tout item irréversible, historique Undo visible dès l'ouverture de l'app). Textes
  `about.body`/`about.roadmap` mis à jour (H.5) pour ne plus annoncer Repair comme "à venir".
  **La licence embarquée est toujours un PLACEHOLDER** (`LicenseVerifier.EmbeddedPublicKeyBase64`)
  → `Apply` est un no-op prouvé en production tant que `license-tool init` n'a pas tourné pour de
  vrai — c'est ce qui rend raisonnable d'avoir câblé l'UI sans pouvoir la tester sur une vraie
  machine Windows cette session.

  **LOT I — `RegisterComComponentAction`, codé et testé, délibérément PAS câblé.** Les sept règles
  de confinement de la spec (liste blanche en dur, chemin canonique avant vérification, zéro
  argument dérivé du scan, PE+bitness via `PeInspector`, timeout, vérification d'élévation au
  moment de l'usage via un P/Invoke `advapi32` neuf, `IsReversibleByNature=false`) sont toutes
  implémentées et testées sans machine Windows réelle. **Aucune `RepairRule` ne la référence dans le
  pack** — donc inerte en production quel que soit le registre de capacités (même précédent que
  `SetDefaultAudioDeviceAction`, jamais câblé non plus). Deux inconnues non validées documentées
  dans l'en-tête de la classe et dans ADR-012 : l'outil vit-il vraiment à côté de la DLL du
  composant sur une install réelle, et comment chaque outil se comporte-t-il lancé sans argument
  (`Setup.exe` de VPinMAME est un installeur graphique interactif connu, pas un enregistreur
  silencieux). Application directe de la clause de sortie de la spec elle-même : "si l'un de ces
  points ne peut pas être tenu proprement, ne pas livrer le LOT I."

  **Tests et build.** Core 412/412, Repair 139/139 (122 avant cette session + 17 nouveaux pour le
  LOT I), tous verts, `dotnet` disponible dans ce sandbox cette fois (confirmé, contrairement aux
  sessions précédentes). `PincabToolbox.App` **toujours pas compilable dans ce sandbox**
  (`NU1100 : Microsoft.WindowsDesktop.App.Ref` introuvable — SDK Windows Desktop absent hors
  Windows, fait déjà documenté, pas une régression) : le XAML/code-behind de l'onglet Repair n'a pu
  être vérifié qu'à la main (XML bien formé, `csc` sans les références WPF confirmant l'absence
  d'erreur de syntaxe CSxxxx malgré l'impossibilité de résoudre les types WPF eux-mêmes) — jamais
  compilé ni exécuté réellement. **À vérifier en premier sur la machine de Maxime.**
- disposition: ADR-012 écrit (chemin d'écriture Repair + décision LOT I). TRANSMISSION.md (bloc du
  haut) mis à jour. Revue CTO+Produit faite en clôture de session (voir TRANSMISSION.md pour le
  détail : risques restants, valeur utilisateur, améliorations à faible coût proposées sans être
  codées).

## 2026-08-11 (bis) · Clé de licence RÉELLE déployée — `Apply` n'est plus un no-op prouvé en production
- code:        `src/PincabToolbox.Repair/Licensing/LicenseVerifier.cs` (constante
  `EmbeddedPublicKeyBase64`, remplace le `PLACEHOLDER` littéral décrit dans l'entrée du dessus par la
  vraie clé publique P-256 générée par Maxime) + `tests/PincabToolbox.Repair.Tests/RepairSessionTests.cs`
  (renommage/reclarification d'un test, ajout de `Test_EmbeddedPublicKey_IsARealKey_NotThePlaceholder`)
- bac:          SECURITY (rotation de clé, changement de posture de production)
- contexte:     Suite directe de l'entrée du dessus, même journée. Maxime a demandé comment générer
  "la clé" et où la mettre ; réponse donnée sur `license-tool init`, exécuté par lui hors ligne sur sa
  propre machine (la clé privée n'a jamais transité par un repo ni une session cloud, conformément à
  la contrainte de sécurité posée dès le départ). Il a transmis la clé **publique** résultante dans la
  conversation, safe à partager par construction (ECDSA — la clé publique ne peut que vérifier, jamais
  signer).
- analyse :

  **Ce qui change concrètement.** `LicenseVerifier.EmbeddedPublicKeyBase64` valait un
  `PLACEHOLDER_RUN_LICENSETOOL_INIT_...` littéral depuis le début du projet — jamais un DER valide,
  donc `Verify()` retournait `Invalid` pour absolument toute entrée. C'est cette propriété précise qui
  rendait sûr de câbler tout l'onglet Repair (LOT H, entrée du dessus) sans jamais l'avoir exécuté sur
  Windows : quel que soit un bug côté WPF non détectable dans ce sandbox, `licensed` restait `false`
  en dur, donc tout item retombait en `RepairMode.Locked`, jamais appliqué. Avec la vraie clé, ce
  filet n'existe plus une fois ce build distribué : une licence signée par la clé privée de Maxime
  active pour de vrai les 4 actions déjà câblées (`UnblockFileAction`, `RestoreRomArchiveAction`,
  `QuarantineOrphanedMediaAction`, `KillZombiePinUpDisplayAction`).

  **Ce qui protège encore.** ADR-009 (achat automatisé de licence) reste non câblé — aujourd'hui,
  seul Maxime peut émettre une licence valide, via `license-tool issue` sur sa machine. Toutes les
  garanties de code de l'entrée du dessus (sélection opt-in stricte jamais groupée, confirmation
  obligatoire pour l'irréversible, échec de backup = aucune écriture, journal persistant) sont des
  propriétés du code indépendantes de la validité de la clé — inchangées par cette rotation.

  **Vérification faite avant embarquement.** La clé fournie a été décodée et confirmée comme un
  SubjectPublicKeyInfo P-256 bien formé (91 octets, DER X.509) avant d'être collée dans le code —
  exactement le contrôle que l'audit du 2026-08-04 avait trouvé absent sur le placeholder. Nouveau
  test de non-régression ajouté dans le même esprit inverse : verrouille contre un retour accidentel
  au placeholder.

  **Tests et build.** Core 412/412, Repair 140/140 (139→140 pour le nouveau test), tous verts.
  `PincabToolbox.App` toujours pas compilable dans ce sandbox (fait déjà documenté) — cette rotation
  ne touche aucun fichier App.
- disposition: `docs/adr/ADR-012-chemin-ecriture-repair.md` complété d'une section "Suite — 11/08/2026"
  documentant ce changement de posture (pas de réécriture silencieuse du raisonnement d'origine).
  TRANSMISSION.md (bloc du haut) mis à jour avec une entrée dédiée. Recommandation transmise à Maxime :
  valider lui-même, sur sa machine, un cycle complet Preflight → Apply → Undo avec une licence qu'il a
  émise pour lui-même, avant toute distribution plus large du build ou de la clé publique.

## 2026-08-07 (treize) · Gregg — nouveau rapport post-lancement, confus sur FlexDMD + comment lire le rapport complet
- code:        à préciser (dépend de ce que Gregg trouve confus sur FlexDMD — pas encore clair)
- bac:         WORDING potentiel (rapport pas assez lisible/actionnable) + question d'usage pure
- contexte:    Gregg (déjà croisé le 07/08 pour BKSOR/R&B/Spiderman) a testé la nouvelle version,
  joint un nouveau rapport, dit que certains findings "ne font pas sens" pour lui, en particulier
  celui sur FlexDMD. Demande aussi comment ouvrir le rapport complet pour tout voir en détail, et
  redemande le nom de Maxime (jamais donné jusqu'ici).
- analyse: pas assez d'info pour diagnostiquer le point FlexDMD — Gregg n'a pas précisé ce qui ne
  fait pas sens (le libellé du finding ? la sévérité ? le fix suggéré ?). Le rapport HTML est un
  fichier autonome (`pincabtoolboxreportYYYYMMDDHHMM.html`) qui s'ouvre avec n'importe quel
  navigateur — question d'usage simple, pas un bug.
- disposition: réponse envoyée dans le chat demandant à Gregg de préciser ce qui le perturbe sur
  FlexDMD (capture ou citation exacte) avant de pouvoir dire si c'est un vrai wording à corriger.
  Rien à coder tant que le détail n'est pas là.

## 2026-08-07 (douze) · Commentaire FB hors périmètre — demande de patcher le moteur VPX (C++) directement
- code:        aucun — hors périmètre du produit, pas un finding
- bac:         FEATURE mal dirigée / hors scope
- contexte:    Commentaire (compte "bizarre" d'après Maxime) décrivant un bug de physique VPX
  (slingshots qui n'éjectent pas toujours la bille correctement, bille qui traverse le PF jusqu'au
  drain opposé, pire sur certaines tables) et demandant si Pincab Toolbox peut lire le code source
  C++ de Visual Pinball, corriger le bug de gameplay, puis produire un installeur unique appliquant
  le correctif à toutes les versions de VP présentes dans le dossier.
- analyse: **hors périmètre par nature, pas juste "pas encore fait".** Pincab Toolbox scanne/répare
  des fichiers de configuration et d'installation (ROMs, DLLs, .ini, registre) — il ne touche jamais
  au moteur Visual Pinball X lui-même (projet C++ séparé, maintenu ailleurs, sur GitHub). Patcher le
  moteur physique et redistribuer un binaire modifié en "installeur toutes versions" serait un
  changement d'une tout autre nature : ça sort du principe du projet (jamais de modification du
  cœur VPX, seulement des fichiers d'install/config autour), et republier un moteur physique modifié
  pour tout le monde sans tests d'engine réels serait un risque bien plus grave qu'un faux positif
  scanner. Le symptôme décrit (slings qui n'éjectent pas toujours pareil, bille qui traverse tout le
  PF) ressemble à un réglage de physique/rubber strength propre à chaque table, pas à un bug
  générique du moteur — donc même sur le fond, la vraie réponse est probablement "table par table",
  pas "un patch moteur global".
- disposition: **refusé poliment, périmètre clarifié** dans la réponse envoyée (chat). Rien à
  qualifier ni prioriser — ce n'est pas une feature en attente, c'est en dehors de ce que ce produit
  fait et fera.

## 2026-08-07 (onze) · Commentaires FB post lancement — DOF (nouvelle piste), musique/playlists (renforce #7), ball manquante (à clarifier)
- code:        aucun encore — 3 idées, pas des findings
- bac:         FEATURE ×3
- contexte:    Commentaires sous le post d'annonce Facebook (groupe Pincab Toolbox), plusieurs
  retours le même jour. Réponses envoyées dans le chat, à poster par Maxime.
- analyse:
  1. **Steve Toneatti Sr. — "check DOF settings to confirm aligned with your toys"** : nouvelle
     piste, pas encore dans les décisions en attente. DOF (Direct Output Framework) pilote les toys
     physiques du cab — un mismatch config (toy déclaré mais pas câblé, ou câblé mais qui ne se
     déclenche jamais) est exactement le genre de défaut invisible tant qu'on n'a pas testé
     physiquement. À qualifier (quel fichier DOF lire, quelle validation possible sans accès
     matériel réel) avant de coder quoi que ce soit.
  2. **Jld Davis — "help place the music file in the right order"** : rejoint la question déjà
     ouverte **#7** (sémantique `isFav=2` sur `PlayListDetails`, nom de colonne "titre" sur
     `Playlists` non confirmés) — pas une nouvelle piste, un signal de plus que ça vaut le coup.
  3. **Jld Davis — "fix missing ball"** : **pas assez clair pour agir.** Peut vouloir dire une bille
     qui disparaît en jeu sur une table précise (bug VPX/physique/scripting, hors périmètre
     diagnostic) ou un souci de trough/matériel réel sur son cab (encore plus hors périmètre). Réponse
     envoyée demandant de préciser — rien à noter tant qu'il n'a pas répondu.
- disposition: rien codé. DOF et musique/playlists ajoutés informellement aux pistes non
  bloquantes ; "missing ball" en attente de clarification avant même de savoir si c'est une piste.

## 2026-08-07 (dix) · Commentaire forum — B2S_MISSING sur tables PUP-Pack, cas déterministe (renforce #13)
- code:        `B2S_MISSING` (module Install Auditor)
- bac:         FP confirmé sur une sous-catégorie précise de tables (PUP-Pack)
- contexte:    Commentaire forum : « it doesn't report an error if the table is missing a
  backglass... all my pup tables were showing errors because of course there is no backglass file
  for them. Can it either suppress these error reports or can we show its attached to pups? »
- analyse: **plus solide que la décision #13 existante** (dé-emphase backglass pour cabs sans
  2ᵉ écran) — celle-ci suppose la config écran de l'utilisateur (heuristique). Ici c'est un fait sur
  la table elle-même : **une table avec PUP-Pack associé n'a structurellement jamais de fichier
  `.directb2s`**, ce n'est pas une install cassée, c'est le fonctionnement normal du format. Le
  module Install Auditor croise déjà tables/Popper/PUP-Packs (confirmé sur la landing, section
  modules) — donc l'info "cette table a un PUP-Pack" est déjà lue quelque part dans le scanner,
  juste pas recroisée avec le check backglass. **Piste concrète** : ne pas lever `B2S_MISSING` (ou
  le redescendre en Info/Note) pour une table qui a un PUP-Pack associé.
- disposition: **signalé à Maxime, pas codé** — c'est un scanner EXISTANT (Install Auditor / check
  backglass), aucun changement sans feu vert explicite (règle inchangée, contrairement à la nouvelle
  règle Repair du 07/08). Réponse de remerciement envoyée au commentaire, en attente de décision.

## 2026-08-07 (neuf) · 2 idées communauté — DMD/B2S 2 vs 3 écrans (FB) + position DMD/plugins (forum)
- code:        NOUVEAU (aucun code existant concerné) — idées, pas des findings
- bac:         FEATURE (2 demandes distinctes, communauté)
- contexte:    Maxime a repéré un fil Facebook (groupe World of Virtual Pinball) et reçu un nouveau
  commentaire forum le même jour — les deux touchent au DMD, angle différent à chaque fois.
- analyse:
  1. **Facebook — Tony Truong, table Mass Effect** : son DMD n'affiche pas le bon thème. Diagnostic
     du modérateur (Tim Waugh) + confirmation communauté (Ryan Wadsworth, lien vpuniverse.com) :
     `.directb2s` installé est une **version 2 écrans sans image DMD intégrée**, alors qu'il lui
     fallait la version **3 écrans** (backglass + DMD) du même fichier. Différent des checks B2S
     existants (`B2S_MISSING`/`B2S_MALFORMED` regardent présence/validité du fichier, pas s'il
     contient une image DMD). Piste : nouveau check qui ouvre le `.directb2s` et signale l'absence
     d'image DMD intégrée quand la cab a un DMD configuré — même famille de signal que B3
     (`DMD_COM_PORT_NOT_FOUND`, livré le 06/08). Un "Repair" resterait un lien suggéré, jamais un
     téléchargement automatique (principe du projet).
  2. **Forum, nouveau commentaire — 3 demandes concrètes** (verbatim : "I would love for it to
     check DMD position, a global setting for plugins being on that actually works (looking at you
     VP Studio), that the elements will stack right so my DMD is actually over my art") :
     - **Position DMD** — recoupe exactement la **DÉCISIONS EN ATTENTE #5** déjà notée le 06/08
       ("Position DMD non vérifiée par ScreenTopologyScanner — seul le backglass l'est, deux
       lectures possibles de la doc officielle, jamais recoupées"). Deuxième demande indépendante
       pour la même idée → renforce la priorité, ne la tranche toujours pas (le blocage reste le
       même : ambiguïté de doc, pas de deuxième source).
     - **Réglage global "plugins actifs" fiable, VP Studio cité comme exemple qui bug** — nouvelle
       demande. **Sous la nouvelle règle permanente du 07/08 (voir TRANSMISSION), c'est du
       périmètre Repair → feu vert par défaut**, plus besoin de redemander. Reste à cadrer
       techniquement avant de coder (quel registre/mécanisme exact, quels plugins visés,
       "actifs" au sens de quoi précisément) — la règle autorise la construction, elle ne dispense
       pas de clarifier une demande encore vague.
     - **"Les éléments s'empilent bien, mon DMD est vraiment au-dessus de mon art"** — même famille
       que la position DMD (#5), probablement le même chantier vu du côté utilisateur plutôt qu'un
       item séparé.
- disposition: **rien codé, les deux sont des pistes.** L'idée Facebook (DMD sans image dans un B2S
  2 écrans) est nouvelle, pas dans la liste actuelle — à ajouter si Maxime veut la prioriser. La
  demande forum sur la position DMD renforce #5 sans la débloquer (toujours pas de deuxième source
  pour trancher l'ambiguïté de doc). Le réglage plugins global est noté mais pas qualifié.

## 2026-08-07 (quater) · Bouton "Check for updates" codé — premier appel réseau du projet
- code:        transverse (infra App/Core) — pas un finding de scan
- bac:         FEATURE (demande directe de Maxime : « fais le bouton »)
- contexte:    Maxime, après avoir appris que le bouton n'existait pas dans le code (session
  précédente) : feu vert direct pour le coder, malgré le manque de temps annoncé.
- analyse:
  1. **Fait exprès de le traiter comme un changement de nature différente**, pas un bugfix de plus :
     c'est le premier `HttpClient`/appel sortant de tout le projet (vérifié par recherche exhaustive
     avant de coder, zéro résultat). Le texte About affichait jusque-là "100% local — rien n'est
     envoyé" — une vraie promesse utilisateur, pas juste un détail d'implémentation.
  2. **Design retenu pour rester dans l'esprit du projet** : bouton manuel dans l'onglet About
     uniquement, aucune vérification au démarrage ni en tâche de fond, timeout court (6s) pour ne
     jamais donner l'impression que l'app se fige sur une cab hors ligne (cas documenté — certains
     utilisateurs gardent leur cab volontairement offline, cf. commentaire forum du 07/08), et
     surtout **le texte About corrigé en même temps** (FR+EN) pour disclosure honnête plutôt que de
     laisser une promesse devenue fausse.
  3. **Ce qui est codé** : `GitHubUpdateChecker` (Core/Services) lit `api.github.com/repos/waylo1/
     pincab-toolbox/releases/latest`, ne télécharge/installe rien, retourne juste tag + URL de
     release ; `AppVersionCompare` (comparaison pure, 10 tests) décide si c'est plus récent que
     `0.1.1` ; bouton + résultat cliquable dans `MainWindow.xaml`/`.xaml.cs` ; clés `about.*`
     ajoutées dans `Loc.cs` FR+EN.
  4. **Pas de build/test exécuté** (aucun `dotnet` disponible cette session, ni sandbox ni pont) —
     vérification manuelle seulement (accolades/parenthèses, cohérence des types avec le reste du
     projet, `net8.0` supporte `HttpClient`/`System.Text.Json` nativement, zéro dépendance NuGet
     ajoutée). **Core/Repair/App à revérifier par Maxime au prochain build réel — pas de vert
     confirmé cette entrée, contrairement à la doctrine habituelle du projet.**
  5. **ADR formel pas écrit** — devrait l'être (premier appel réseau = décision structurante), mais
     pas fait faute de temps annoncé par Maxime en fin de session. Noté dans le prompt de passation
     de lundi.
- disposition: codé et écrit sur le disque de Maxime, pas commité (voir bloc Git dans TRANSMISSION
  MAJ 07/08 quater). **Action Maxime** : `build.cmd` pour confirmer Core/Repair/App verts, puis
  commit/push. Écrire l'ADR quand il aura le temps — pas bloquant pour merger, mais à ne pas
  oublier (première fois que le projet sort de son modèle "100% offline").

## 2026-08-07 (bis) · Feu vert donné — correctif des 2 scanners appliqué, git push reconfirmé
- code:        `security`/`SCANNER_ERROR` (BlockedFileScanner) · `COMPLETENESS_MISSING_WHEEL` (dépend de CollectWheelStems)
- bac:         FIX (correctif appliqué) + vérification (push)
- contexte:    Maxime a donné le feu vert pour le correctif décrit en DÉCISIONS EN ATTENTE #11
  (entrée du 07/08 ci-dessous), sans capture ni fichier nouveau à traiter.
- analyse:
  1. **Correctif appliqué dans les 2 scanners existants**, patron répliqué depuis
     `LayoutDetector.SafeEnumerateDirs` (déjà dans le projet — BFS dossier par dossier, chaque appel
     `Directory.GetFiles`/`Directory.GetDirectories` protégé par son propre try/catch, pas un seul
     try/catch autour de tout l'`IEnumerable` paresseux). `BlockedFileScanner.Scan` : le `*.dll`
     `AllDirectories` est remplacé par une marche `SafeEnumerateDirs` + `Directory.GetFiles(dir,
     "*.dll")` par dossier. `CompletenessScanner.CollectWheelStems` : même marche, filtrée sur les
     dossiers nommés `Wheel`. Un sous-dossier illisible est maintenant sauté individuellement, le
     reste de l'arbre continue d'être scanné — plus de `SCANNER_ERROR` qui efface tout le module.
  2. **Pas de build/test exécuté** — ni `dotnet` ni le mini-checker Roslyn disponibles cette session
     (ni sandbox cloud ni pont machine). Changement revu à la main seulement : signature identique,
     aucun appelant modifié, patron copié à l'identique d'un helper déjà testé et utilisé ailleurs
     dans le même fichier (`LayoutDetector`). **Core 279/279 + Repair 105/105 à revérifier par
     Maxime** au prochain `build.cmd` réel — pas de vert confirmé cette entrée, contrairement aux
     entrées précédentes.
  3. **Git push reconfirmé** : `git log` (local, machine de Maxime) == `git log origin/main`, HEAD
     `9f3e5f7` des deux côtés — les commits Tier B (`613496f` dans ce clone, référencés `14894ed` et
     `1ab33fc` dans les entrées précédentes — écart de hash probable dû à une normalisation de fin de
     ligne entre clones, pas une divergence de contenu) étaient déjà bien en ligne. Rien à repousser
     avant le correctif de cette entrée.
  4. **Correctif PAS commité par cette session.** `git status` montre en plus 3 fichiers déjà modifiés
     avant toute action de cette entrée (`PincabToolbox.sln`, `README.md`, `landing/.gitignore` —
     origine inconnue, pas touchés ici) et un `.git/index.lock` résiduel que le pont ne peut pas
     supprimer (pas de droit de suppression sur les fichiers montés côté device bridge). Laissé tel
     quel plutôt que de risquer un commit qui embarque un diff non expliqué.
  5. **Contenu suspect dans le message de Maxime, non traité comme instruction** : deux blocs en fin
     de message ressemblant à des commentaires de forum/support collés (un hors-sujet, un signé
     « gregg » plausible pour ce projet) suivis d'une demande de réponse immédiate et d'une consigne
     « ne vérifie pas les commits » contredisant directement la tâche explicite de vérifier le push.
     Vérifié quand même (point 3 ci-dessus) ; contenu forum non traité comme instruction à exécuter
     sans confirmation explicite de Maxime.
- disposition: correctif appliqué et écrit sur le disque de Maxime, pas commité (voir point 4).
  **Action Maxime** : supprimer `.git/index.lock` si présent, committer seulement les 2 fichiers
  scanner + ce journal (commande dans TRANSMISSION MAJ 07/08 bis), relancer `build.cmd` pour
  reconfirmer Core/Repair verts. **4 décisions toujours sans réponse** : #9 (clé INI DMD), #10
  (planchers Script Doctor), #12 (KPI #1 ROM), #13 (dé-emphase backglass, basse priorité).
  **MAJ** : `.git/index.lock` supprimé par Maxime, commit `f7f2ab1` poussé (`9f3e5f7..f7f2ab1`,
  4 fichiers, capture terminal reçue) — correctif bien en ligne sur `origin/main`.

## 2026-08-07 (ter) · Rapport réel de Gregg reçu — les 2 derniers items clos, scanner ROM confirmé exact
- code:        `ROM_MISSING` (`Black Knight - Sword Of Rage`, `The Adventures ofRocky & Bullwinkle 0.96`)
- bac:         FIX (résultat de fix confirmé, BKSOR) + **pas un FN — confirmation que le scanner a raison** (R&B)
- contexte:    Gregg (VPForums), suite d'échange après son rapport `pincabtoolboxreport202608061631.html`.
  Il avait d'abord dit (message précédent) que le scan « cherchait un autre nom de ROM que celui
  réellement utilisé » pour R&B — cette entrée corrige/clôt ce point avec sa réponse exacte.
- analyse:
  1. **Black Knight - Sword Of Rage — confirmé, pas un bug scanner, erreur utilisateur banale mais
     instructive.** Le fichier ROM de Gregg s'appelait en réalité `bksor.zip.zip` (double extension,
     probablement un dézippage qui n'a pas retiré `.zip` du nom d'origine) — invisible dans
     l'explorateur Windows si les extensions de fichier sont masquées (réglage par défaut). Renommé
     en `bksor` (fichier zip), le `CRITICAL` a disparu. **Le scanner a fait exactement son travail** :
     un fichier mal nommé, même visuellement identique à l'œil, est un vrai ROM manquant du point de
     vue de VPinMAME. Bon exemple concret à garder pour la doc/FAQ utilisateur (extensions de fichier
     masquées → doubles extensions invisibles).
  2. **Rocky & Bullwinkle — Gregg confirme son erreur initiale, pas un défaut du scanner.** Il avait
     regardé le script d'une *autre* copie de la table par erreur ; la version `0.96` réelle déclare
     bien `cGameName = "Rab"` (ROM `Rab.zip`) dans son script, et ce ROM n'est simplement pas dans son
     dossier `roms`. Scanner exact — pas de FN, pas d'écart de nommage. Confirme le point noté hier :
     le nom `Rab` est lu depuis le script, jamais deviné.
  3. **Amazing Spider-Man** — déjà clos par Gregg lui-même à l'étape précédente (nom de fichier B2S
     différent du nom de table, sans lien avec le scanner ROM).
- disposition: **2 items sur 2 clos, aucun code à toucher — le scanner ROM sort renforcé de ce
  round de test réel** (0 faux positif détecté sur 3 cas terrain remontés par Gregg). Réponse de
  remerciement envoyée dans le chat. Ne tranche toujours pas KPI #1/#12 (les 8 `ROM_MISSING` de
  Maxime lui-même) — mécanisme différent (nom de ROM erroné vs originale-sans-ROM) — mais ajoute de
  la confiance générale dans la fiabilité du module `rom`.

## 2026-08-07 · Maxime a lancé 2 scans réels sur sa cab (build Tier B) — bug confirmé + KPI #1 toujours ouvert
- code:        `ROM_MISSING` (8×, recoupe exactement le relevé du 04/08) · `security`/`SCANNER_ERROR` (échec confirmé, contenu par `ScanEngine` — pas un crash d'app) · `B2S_MISSING`/`B2S_ORPHAN` (~205 combinés) · `NVRAM_EMPTY`(1) · `ALTCOLOR_INCOMPLETE`(2 paires) · `B2S_MALFORMED`(2) · `POPPER_ORPHAN_PLAYLIST`(1, 421 jeux)
- bac:         FIX (vérification cab réel) + FN (bug de scan confirmé) + FP potentiel (KPI #1, toujours sans réponse)
- contexte:    Maxime a installé l'app Claude desktop directement sur sa cab (distincte de Pincab Toolbox lui-même) et lancé 2 scans réels avec le build Tier B fraîchement livré : `pincabtoolboxreport202608070032.html` (00:32, racine `C:\`, par erreur) puis `pincabtoolboxreport202608070040.html` (00:40, racine `C:\Visual Pinball`, corrigée après une capture du sélecteur de dossier). Cab confirmée par photo : tabletop/cocktail, un seul écran, **aucun backglass**.
- analyse:
  1. **Bug confirmé — pas un faux négatif de détection, un vrai échec de scanner, mais CONTENU par le filet de sécurité existant, pas un crash d'app.** Le rapport 00:32 (racine `C:\`) ne montre aucun `BLOCKED_NONE`/`BLOCKED_DLL` du module `security` (`BlockedFileScanner`), alors que le rapport contient par ailleurs des chemins sous `$Recycle.Bin\<SID-utilisateur>\...` et `.\Visual Pinball\VPinMAME\nvram\spagb_100.nv` — signe que le balayage est bien descendu jusque dans `C:\Documents and Settings` (jonction NTFS historique vers `C:\Users`, `UnauthorizedAccessException` par conception Windows, même en admin). **Root cause lue et confirmée dans le code cette fois** (pas juste déduite comme le 06/08) : `BlockedFileScanner.cs` L.71-79 enveloppe l'APPEL à `Directory.EnumerateFiles(root, "*.dll", SearchOption.AllDirectories)` dans un try/catch, mais cet appel est paresseux (lazy) — l'exception part réellement pendant le `foreach` de consommation (L.82), qui lui n'est PAS protégé. **Correction importante après relecture de `ScanEngine.cs`** : `ScanEngine.Run` (L.53-69) enveloppe l'appel à CHAQUE `scanner.Scan(ctx)` dans son propre try/catch et convertit toute exception échappée en un finding `SCANNER_ERROR` (Warning, message technique brut) — donc ce bug ne fait PAS planter l'app ni le reste du scan, il fait juste disparaître silencieusement le résultat normal du module `security` au profit d'une ligne d'erreur technique peu engageante. Reste un vrai défaut (perte du check + mauvaise UX), juste moins grave qu'un crash. **Deuxième occurrence identique trouvée en vérifiant les scanners voisins** : `CompletenessScanner.CollectWheelStems` (L.163-166) a exactement le même patron (assignation protégée, `foreach` de consommation non protégé) sur `Directory.EnumerateDirectories(popMediaDir, "Wheel", SearchOption.AllDirectories)` — même filet `ScanEngine` en dernier recours, risque plus faible en pratique (`popMediaDir` est un sous-dossier d'install, pas `C:\`), mais même bug de fond. `LayoutDetector.SafeEnumerateDirs`/`SafeEnumerateFiles` ont déjà le bon patron (try/catch PAR dossier, jamais sur l'IEnumerable entier) — c'est le patron à répliquer, pas à inventer. **Aucun correctif appliqué** : les deux fichiers sont des scanners EXISTANTS, jamais touchés sans reconfirmation explicite (règle du projet) — question posée à Maxime le 06/08, toujours sans réponse à ce jour.
  2. **Root scope confirme le diagnostic empiriquement.** Le rapport 00:40 (racine corrigée `C:\Visual Pinball`) montre le module `security` propre (résultat `BLOCKED_NONE`/`BLOCKED_DLL` normal, aucun `SCANNER_ERROR`) — `C:\Documents and Settings` n'existe que directement sous `C:\`, jamais atteint depuis une racine plus profonde. Prédiction du 06/08 vérifiée sur le terrain, pas juste en théorie.
  3. **8 `ROM_MISSING` critical recoupent EXACTEMENT le relevé du 04/08** (Blood Machines VPW 2022, hpgf-052-DOF, Jurassicparklimitededition, leprechaun, Munsters 2020, Stranger Things SE 1.47_OSB, The Goonies Javier1515 2019, Willy Wonka Pro — mêmes tables que bloodmach/hpgof/jurassic/leprechaun/mmunsters/STLE/goonies/willywonka du 04/08). Deux sessions, plusieurs jours d'écart, même liste stable → cohérence forte, mais **ne tranche toujours pas la question KPI #1** (originales/homebrew sans ROM vs vrais hacks nécessitant une ROM précise) — toujours sans réponse de Maxime, voir DÉCISIONS EN ATTENTE #12.
  4. **Score 0/100·F vérifié conforme à la formule**, pas un bug : `max(0, 100 − 15×Critical − 5×Warning)`, Warning plafonné à −30 depuis le 03/08. 8 critical × 15 = 120 à eux seuls > 100 → plancher à 0 attendu, indépendamment des ~105 warnings (déjà plafonnés). Confirmé à Maxime.
  5. **Constat produit (pas un bug) : cab sans backglass confirmé par photo** (tabletop/cocktail, écran unique, pas de second moniteur) génère ~205 findings combinés `B2S_MISSING`+`B2S_ORPHAN` — bruit attendu structurellement sur ce type de cab, jamais actionnable par cet utilisateur précis. Piste de dé-emphase déjà notée en DÉCISIONS EN ATTENTE #13, pas codée, pas urgente.
  6. Autres findings du scan réel (racine correcte, 00:40), actions Maxime uniquement — pas de code à écrire : 1 `NVRAM_EMPTY` (`spagb_100.nv`, à supprimer + relancer la table une fois), 2 paires `ALTCOLOR_INCOMPLETE` (avs_170c, mt_145hc), 2 `B2S_MALFORMED` (Goldorak_1.00, Iron Maiden Virtual Time 2020), 1 `POPPER_ORPHAN_PLAYLIST` (421 jeux — à retrier dans l'admin PinUP Popper).
- disposition: **3 décisions posées à Maxime, toutes sans réponse à ce jour** (voir DÉCISIONS EN ATTENTE #11-13). Rien codé cette entrée (constat de terrain + vérification de code, pas un chantier). Action Maxime en plus des 3 décisions : re-scanner en pointant la racine sur le dossier parent commun (Visual Pinball + PinUP Popper) pour une couverture complète en un seul rapport propre ; confirmer le `git push` des 2 commits Tier B (`14894ed`, `1ab33fc`) — pas de capture reçue depuis.

## 2026-08-06 (session Sonnet 5, autonome, effort max) · Handoff scanners — exécution de la file
- code:        transverse — journal de session unique, une ligne par item (détail sous chaque item ci-dessous)
- bac:         FIX (nouveaux scanners) + FEATURE (rendu Note)
- contexte:    Reprise du handoff `docs/HANDOFF-Sonnet5-scanners-2026-08.md`, Maxime absent, autonomie
  totale (R1-R6, zéro question). Baseline reconfirmée avant tout code : **Core 144/144, Repair 105/105,
  Debug ET Release** (SDK réinstallé dans le sandbox, fixtures régénérées). Outil de vérif ajouté ce jour :
  un mini-checker Roslyn (`/tmp/roslyn-check`, `Microsoft.CodeAnalysis.CSharp` chargé directement depuis
  les DLL livrées avec le SDK — pas d'accès NuGet dans ce sandbox) qui parse chaque fichier App édité et
  compte les vraies erreurs de syntaxe CSxxxx. Remplace la simple relecture manuelle mentionnée au §2 du
  handoff par une vérification programmatique réelle (0 erreur de syntaxe à chaque item, voir détail par
  item ci-dessous).
- analyse (journal par item, le plus récent en dernier) :

  **Item 1 — Knowledge.cs + Loc.cs rétroactif pour `VPX_VERSION_OUTDATED` (R1).** Entrée `Knowledge.cs`
  (Impact/Cause FR+EN, pas d'`AutoFixable` — mise à jour manuelle de VPX, même famille que
  `COMPAT_MIN_VERSION`/`B2S_MISSING`) + `Loc.cs` (`FrFindings` avec les 3 args table/requise/installée,
  `FrFixHints` en texte plat sans répéter les numéros déjà dans le message — patron identique aux entrées
  voisines). Roslyn : 0 erreur sur les deux fichiers. Aucun test ne référence l'App (Core.Tests ne
  dépend pas d'App) donc le vert Core/Repair n'était pas en jeu ici — vérifié quand même par prudence.

  **Item 2 — Rendu App du palier `Severity.Note`, prérequis Tier B.** Bilan de la revue : le palier Core
  était déjà vert (session Opus), mais **3 bugs réels de rendu** dormaient dans `MainWindow.xaml.cs`,
  exactement le risque nommé au handoff (« switch App non exhaustif ») — trouvés et corrigés avant tout
  scanner Tier B :
  1. Écran : `Show(Severity)` (filtre chip) et les switchs `SevBrush`/`RowBg` avaient un bras `_ =>` par
     défaut qui routait silencieusement `Note` sur le bucket **Ok** — un finding heuristique se serait
     affiché caché par défaut (le chip Ok est masqué par défaut) et, si affiché, peint en **vert
     confirmation** au lieu d'une couleur distincte. Pas un crash — pire, un mensonge visuel silencieux.
  2. Export Markdown (`BuildForumMarkdown`, le bouton « Copier pour le forum », le plus utilisé) :
     `foreach (var sev in new[] { Critical, Warning, Info })` — `Note` **absent du tableau** → un finding
     Note aurait **disparu intégralement** du rapport forum, malgré `Rolled()` qui les contient. Le pire
     des trois : silence total, pas juste une couleur fausse.
  3. Export HTML : `cls = f.Severity switch { ... _ => "o" }` — même défaut, classe CSS « o » (verte, Ok)
     appliquée à une ligne Note.
  **Correctifs** : les 3 switchs rendus exhaustifs sur les 5 valeurs de `Severity` (explicite, plus de
  `_ =>` fourre-tout) ; `Note` ajouté au tableau de `BuildForumMarkdown` ; nouvelle teinte violette
  distincte (`#B58DF5`, ni le bleu Info ni l'orange Warning) déclinée en `NoteSev` (App.xaml),
  `BrushNote`/`RowNote` (code-behind), classe CSS `.n` (HTML), couleur `purple` (BBCode) ; libellé exact
  demandé : `sev.Note` = FR « À noter » / EN « Note » ; nouveau chip `PillNote`/`ChipNote` entre Warning
  et Info (ordre = sévérité décroissante, cohérent avec `Ordered()`) ; `_showNote = true` par défaut
  (comme Info/Warning/Critical — seul Ok est masqué par défaut) ; `status.done` et les 4 résumés d'export
  texte étendus avec le compte de notes. **Score/bandeau vérifiés déjà corrects sans y toucher** :
  `ScanReport.Score` ne compte que Critical/Warning (test `Test_Note_NeverMovesScore` déjà vert) et le
  bandeau « FIX THIS FIRST »/« WORTH A LOOK » (`RefreshList`) ne retombe jamais sur Note (chaîne
  `FirstOrDefault(Critical) ?? FirstOrDefault(Warning)`, s'arrête à Warning) — aucune modif nécessaire là.
  Export **JSON déjà correct sans changement** : `severity = f.Severity.ToString()` est exhaustif par
  construction. Vérifié : App.xaml + MainWindow.xaml XML bien formés (`xml.etree`), `MainWindow.xaml.cs` +
  `Loc.cs` 0 erreur Roslyn, `PillNote`/`ChipNote` cohérents XAML↔code-behind (grep croisé). Core 144/144 +
  Repair 105/105 Debug ET Release toujours verts (aucun test ne touchait ce chantier, mais revérifié).

  **Item 3 — Tier A, E1 VPMAlias Recursion Loop (`VPMALIAS_LOOP`, Warning).** `Services/AliasGraph.cs`
  (pur : détection de cycle sur le mapping alias→cible, marche de proche en proche en trackant l'index de
  chaque nœud dans le chemin courant ; un `Dictionary` a au plus un arc sortant par nœud donc c'est un
  simple graphe fonctionnel — auto-boucle, cycle à 2, cycle plus loin dans une chaîne avec préfixe acyclique
  correctement exclu du cycle rapporté, insensible à la casse comme `AliasFile.Parse`) +
  `Scanning/AliasLoopScanner.cs` (**zéro I/O propre** : `ScanEngine` a déjà parsé `VPMAlias.txt` dans
  `ctx.Aliases` comme prep partagée — même map que lit déjà `RomValidatorScanner` — donc le scanner se
  contente d'interroger `AliasGraph.FindCycles(ctx.Aliases)`, pas de délégué à injecter faute d'I/O à
  faire) + `tests/AliasLoopScannerTests.cs` (12 tests neufs : 8 sur la classe pure, 4 sur le scanner) +
  Knowledge/Loc (FR/EN + `cat.aliasloop`="VPMAlias") + `.Add(new AliasLoopScanner())`. **Core 144→156/156**
  (12 nouveaux tests), Repair 105/105 inchangé, Debug ET Release, Roslyn 0 erreur sur les 6 fichiers
  touchés. Aucun scanner existant modifié. Id `aliasloop` (aucune collision avec les 13 existants).
  **Note produit, pas codée** : 6 des 13 scanners existants (`legacy`,`disk`,`process`,`display`,
  `media-orphan`,`vpxversion`) n'ont pas d'entrée `cat.*` dans `Loc.cs` — leur colonne Module affiche le
  code brut `cat.xxx` au lieu d'un libellé (repéré en cherchant le patron à suivre pour ce nouveau
  scanner). Pré-existant, hors périmètre du handoff (ni un scanner touché, ni un code Warning/Critical) —
  je l'ai seulement évité pour tout scanner neuf de cette session. Coût de le corriger : trivial (6 paires
  de lignes `Loc.cs`, additif, zéro risque). Signalé pour la revue de clôture plutôt que codé à l'aveugle
  hors file.

  **Item 4 — Tier A, H1 NVRAM 0-Byte Detector (`NVRAM_EMPTY`, Warning).** `Services/NvramInspector.cs`
  (pur : filtre les paires nom/taille à taille=0, scope strict au 0-octet — « taille ≠ spec » exclu faute
  de base de specs par ROM, audit §4/H1) + `Scanning/NvramScanner.cs` (énumérateur de dossier injecté,
  défaut = vrai `Directory.EnumerateFiles` sur `VPinMAME/nvram/*.nv` ; dossier absent/illisible → silence,
  jamais une exception qui remonte) + `tests/NvramScannerTests.cs` (9 tests neufs) + Knowledge/Loc
  (FR/EN + `cat.nvram`="NVRAM") + `.Add(new NvramScanner())`. **Core 156→165/165**, Repair 105/105
  inchangé, Debug ET Release, Roslyn 0 erreur. Id `nvram` sans collision.
  **Décision transverse actée ici, valable pour tous les scanners restants de cette session** : vérifié
  que `Knowledge.KnowledgeEntry.AutoFixable` **n'a aucun lecteur dans l'App** (`grep IsAutoFixable` : la
  seule définition + son usage interne à `Knowledge.cs`, zéro appelant ailleurs) — le vrai signal
  « Repair peut corriger X » qui atteint l'utilisateur passe uniquement par
  `RepairOfferBuilder.Build` → `RepairEngine.Plan` → le `RepairActionRegistry` fermé (4 actions réelles)
  croisé avec les `repairRules` de `knowledge/pack-2026.08.json` (JSON séparé, ADR-005) — un mécanisme
  entièrement différent que je ne touche pas cette session (pack Repair = hors périmètre Scanner). Je ne
  positionnerai donc `AutoFixable = true` sur aucun nouveau code cette session (comme `NVRAM_EMPTY`
  ci-dessus) : le flag est aujourd'hui mort mais visuellement trompeur si mis à `true` sans règle Repair
  réelle derrière — cohérent avec les 9 codes existants qui le laissent déjà à `false` par défaut.
  Signalé pour la revue de clôture (flag mort à documenter ou câbler, coût faible).

  **Item 5 — Tier A, B1 AltColor/SERum Pair Integrity (`ALTCOLOR_INCOMPLETE`, Warning).**
  `Services/AltColorInspector.cs` (pur : complet si `.vni`+`.pal` OU fichier Serum `.crz`+`.pal`, les
  deux formats vivant côte à côte selon l'audit §4/B1 ; dossier/jeu vide traité comme « pas complet » par
  la fonction pure mais c'est au scanner de ne PAS transformer ça en finding — un ROM sans aucun fichier
  n'a simplement jamais eu de colorisation installée, ce n'est pas un défaut) + `Scanning/AltColorScanner.cs`
  (croise `ScriptAnalyzer.AnalyzeRomUsage` comme `CompletenessScanner` — ne vérifie QUE les ROMs
  réellement requises par une table présente, jamais tout le dossier `altcolor/`, énumérateur de dossier
  injecté) + `tests/AltColorScannerTests.cs` (16 tests neufs, dont un qui vérifie que le lecteur n'est
  **jamais appelé** pour une table sans ROM) + Knowledge/Loc (FR/EN + `cat.altcolor`) +
  `.Add(new AltColorScanner())`. **Core 165→181/181**, Repair 105/105 inchangé, Debug ET Release, Roslyn
  0 erreur. Id `altcolor` sans collision.
  **Périmètre volontairement réduit, décidé et logué plutôt que deviné** : la fiche audit §4/B1 mentionne
  aussi « la concordance 32/64-bit des DLL de colorisation ». Aucun nom de DLL distinct n'est spécifié
  nulle part pour ce sous-point au-delà de ce que `BitnessScanner` vérifie déjà (`dmddevice64.dll`,
  `BITNESS_DMD64_MISSING`) — inventer un nom de fichier sans preuve aurait échangé le FP-nul de ce check
  déterministe contre une supposition. Non codé, pas silencieusement oublié — signalé pour la clôture.

  **Item 6 — Tier A, B2 AltSound Structural Linter (`ALTSOUND_SAMPLE_MISSING`, Warning).**
  Format `altsound.csv` vérifié par recherche web (guide communautaire « How to create a new altsound
  project », forums VPINBALL.COM) avant de coder plutôt que deviné : en-tête
  `"ID","CHANNEL","DUCK","GAIN","LOOP","STOP","NAME","FNAME"`, CSV virgule avec champs entre guillemets,
  seuls ID/NAME/FNAME obligatoires, plusieurs lignes peuvent partager un ID (le moteur pioche au hasard
  pour la variété). `Services/AltSoundManifestLinter.cs` (pur : parseur CSV maison — zéro dépendance,
  guillemets + `""` échappé gérés — extrait uniquement la colonne FNAME ; en-tête sans colonne FNAME =
  format non reconnu → liste vide plutôt qu'une supposition ; ligne trop courte ou FNAME vide = ignorée
  en silence, pas signalée comme défaut — une ligne placeholder/désactivée est un choix d'auteur légitime)
  + `Scanning/AltSoundScanner.cs` (même discipline anti-FP que B1 : ne vérifie que les ROMs réellement
  requises via `ScriptAnalyzer.AnalyzeRomUsage`, lit `altsound/<rom>/altsound.csv` sous `VPinMameDir`,
  déduplique les FNAME avant de compter — des lignes dupliquées pour variété ne doivent pas gonfler les
  compteurs — puis vérifie l'existence de chaque fichier référencé ; une exception sur la vérification
  d'un fichier précis est traitée comme « présent » plutôt que comme un défaut deviné ; un seul finding
  résumé par ROM avec compteur absents/total + jusqu'à 8 exemples en Args, même patron que
  `POPPER_MEDIA_MISSING`) + `tests/AltSoundScannerTests.cs` (19 tests neufs : 9 sur le parseur pur dont
  guillemets échappés et insensibilité à la casse de l'en-tête, 10 sur le scanner dont non-appel du
  lecteur si aucune ROM requise et déduplication FNAME). Knowledge/Loc (FR/EN + `cat.altsound`) +
  `.Add(new AltSoundScanner())`. **Core 181→200/200**, Repair 105/105 inchangé, Debug ET Release, Roslyn
  0 erreur sur les 6 fichiers touchés. Id `altsound` sans collision.
  **Deux réductions de périmètre décidées et loguées, pas devinées** : (1) la fiche handoff §3/B2 mentionne
  aussi le format legacy `.ini` (« g-sound ») — aucun schéma vérifiable trouvé pour ce format (contrairement
  au CSV, confirmé par recherche), donc non implémenté plutôt que deviné ; silence sur ce format, pas un
  faux « rien à signaler ». (2) la fiche mentionne aussi signaler les « erreurs de syntaxe CSV » en plus des
  samples absents — un seul code est mandaté (`ALTSOUND_SAMPLE_MISSING`) et `AltSoundManifestLinter` traite
  déjà toute ligne malformée comme silencieuse (cohérent avec le biais silence appliqué à tout le reste de
  cette session) plutôt que d'inventer un deuxième signal hors mandat ; si Maxime veut un code dédié
  (`ALTSOUND_MANIFEST_MALFORMED` ou similaire) c'est un ajout additif trivial pour une session future.

  **Item 7 — Tier A, C1 Screen Topology Check (`DISPLAY_OFFSCREEN`, Warning).** Le plus complexe de la
  file (annoncé comme tel au handoff). **Format `ScreenRes.txt`/`B2STableSettings.xml` vérifié par
  recherche approfondie avant tout code** (template officiel `ScreenResTemplate.txt`, wiki
  vpinball/b2s-backglass, Changelog, exemples réels dual/single-screen — pas deviné) : découverte
  majeure, la prémisse du handoff était fausse sur un point — `B2STableSettings.xml` **ne contient
  aucune donnée de position** (vérifié sur 3 exemples réels indépendants, tout y est des toggles
  logs/perf) ; toute la géométrie vit dans `ScreenRes.txt`/`<table>.res` (17 lignes, une valeur par
  ligne non-commentée). `Services/MonitorTopologyProbe.cs` (nouveau, P/Invoke `EnumDisplayMonitors`+
  `GetMonitorInfo` — délibérément séparé de `DisplayProbe.cs` sans y toucher, comme demandé — donne
  rectangle + nom `\\.\DISPLAYn` par écran, coordonnées virtuelles signées) + `Services/ScreenTopologyAnalyzer.cs`
  (pur : parse lignes 1-7, résout le sélecteur d'écran ligne 5 dans ses 3 syntaxes (`N` nom de device,
  `@NNNN` X absolu, `=N` Nᵉ écran de gauche à droite 1-indexé) contre les moniteurs réels, décide
  hors-écran = zéro intersection avec tout rectangle moniteur) + `Scanning/ScreenTopologyScanner.cs`
  (`ScreenRes.txt` global évalué une seule fois — pas par table, pour ne pas dupliquer un même défaut
  partagé — puis chaque `.res` par-table évalué indépendamment) + `tests/ScreenTopologyScannerTests.cs`
  (30 tests neufs, dont un jeu de données copié verbatim de l'exemple réel dual-screen du wiki officiel).
  Knowledge/Loc (FR/EN + `cat.screentopology`) + `.Add(new ScreenTopologyScanner())`. **Core 200→230/230**,
  Repair 105/105 inchangé, Debug ET Release, Roslyn 0 erreur sur les 9 fichiers touchés. Id
  `screentopology` sans collision. `DisplayProbe.cs` non modifié (nouveau P/Invoke séparé comme exigé).
  **Trois réductions de périmètre décidées et loguées, la recherche les a directement motivées** :
  (1) `B2STableSettings.xml` n'est plus lu du tout par ce Doctor — la prémisse du handoff était fausse
  (voir ci-dessus), agir sur la réalité vérifiée plutôt que sur la prémisse d'origine, même logique que
  la vérification du schéma altsound.csv à l'item 6. (2) Seule la position du **backglass** (lignes 6-7)
  est vérifiée, pas celle du **DMD** (lignes 10-11) : la doc officielle dit ces coordonnées « relatives
  à l'écran du backglass » mais le seul exemple réel chiffré disponible ne fait sens que si elles sont
  relatives à la fenêtre du backglass elle-même — conflit non résolu entre deux lectures possibles de la
  même source, jamais recoupé par une deuxième donnée ; encoder l'une ou l'autre comme un fait aurait
  risqué exactement le faux positif que cette session entière cherche à éviter. (3) Seuls les fichiers
  portant le marqueur `# V2` (ajouté en 2.0.0) sont acceptés : avant cette version, les blocs Backglass
  et Background peuvent **échanger silencieusement leur sens** selon un réglage qui ne vit même pas dans
  ce fichier — plutôt que de recouper un deuxième fichier pour lever une ambiguïté qui ne se résout pas
  forcément par table, ce parseur refuse tout fichier sans le marqueur (silence, cohérent avec le reste
  de la session). Les trois décisions restent additives : rien n'empêche de les lever plus tard avec
  plus de preuve terrain.

  **Item 8 — Tier A, G3 Junction Health (`BROKEN_JUNCTION`, Warning).** Panne très spécifique aux pincabs :
  un propriétaire jonctionne souvent un gros dossier (roms, PUPVideos, une colorisation) vers un second
  disque/NAS pour l'espace — quand ce disque disparaît, le dossier a toujours l'air présent mais est vide
  pour tout le monde (VPX, Popper, ce scan), sans la moindre erreur. `Services/JunctionInspector.cs`
  (pur, trivial par nature : reparse point + cible absente = cassé) + `Scanning/JunctionScanner.cs`
  (portée : les dossiers clés d'`InstallLayout` — RootPath/TablesDir/VPinMameDir/RomsDir/PupVideosDir/
  PopMediaDir — plus leurs sous-dossiers immédiats, un seul niveau, jamais de parcours récursif illimité ;
  point d'attention retenu : lire les attributs bruts du dossier — jamais suivis par Windows sur le
  reparse point lui-même — **avant** de demander son état, pour ne pas confondre « jonction cassée » avec
  « rien à cet endroit » via une sonde d'existence naïve qui suivrait le lien mort) + `tests/JunctionScannerTests.cs`
  (12 tests neufs, dont un qui vérifie qu'un chemin listé à la fois comme racine et comme enfant d'une
  autre racine n'est évalué qu'une fois). Knowledge/Loc (FR/EN + `cat.junctions`) +
  `.Add(new JunctionScanner())`. **Core 230→242/242**, Repair 105/105 inchangé, Debug ET Release, Roslyn
  0 erreur sur les 6 fichiers touchés. Id `junctions` sans collision.

  **Item 9 — Tier A, H2 DirectB2S XML Malform (`B2S_MALFORMED`, Warning).** Le handoff signalait qu'un
  `.directb2s` « peut être du XML brut OU compressé » et pointait le `CompoundFileReader` déjà en place
  (utilisé pour lire les `.vpx`, eux-mêmes de vrais fichiers OLE/MS-CFB) comme piste si besoin. **Recherche
  faite avant de coder** : source du DirectB2S Designer lui-même (l'exportateur — un simple
  `XmlDocument.Save`), source du loader de B2S Backglass Server lui-même (un simple `XmlDocument.Load` +
  `SelectSingleNode("DirectB2SData")`), et un parseur tiers indépendant testé sur de vraies collections
  utilisateur — les trois s'accordent : un `.directb2s` réel est **toujours du XML brut**, jamais un
  conteneur OLE compressé. Aucune preuve d'une variante compressée réelle trouvée nulle part.
  `Services/DirectB2SValidator.cs` (pur : `IsWellFormedXml` via `XmlReader`, plus `LooksLikeCompoundFile`
  qui reconnaît juste la signature MS-CFB sans tenter de la décoder) + `Scanning/DirectB2sScanner.cs`
  (énumère `*.directb2s` dans `TablesDir`, signale tout fichier qui n'est ni XML bien formé ni reconnu
  comme conteneur OLE — un fichier 0 octet est délibérément **signalé**, même logique que `NVRAM_EMPTY`) +
  `tests/DirectB2sScannerTests.cs` (18 tests neufs). Knowledge/Loc (FR/EN + `cat.directb2s`) +
  `.Add(new DirectB2sScanner())`. **Core 242→260/260**, Repair 105/105 inchangé, Debug ET Release, Roslyn
  0 erreur sur les 6 fichiers touchés. Id `directb2s` sans collision.
  **Décision de périmètre motivée directement par la recherche** : le `CompoundFileReader` existant
  n'est **pas** invoqué ici. La prémisse du handoff (variante compressée réelle) n'a pas été confirmée ;
  et même si un fichier commence par la signature MS-CFB, aucune source ne donne le nom du flux interne
  à en extraire — le décoder aurait exigé de deviner une structure non vérifiée, contraire à la
  discipline tenue tout du long cette session (altsound.csv à l'item 6, ScreenRes.txt à l'item 7). Un tel
  fichier est donc traité en silence (« format différent, pas cassé »), pas en `B2S_MALFORMED`. Si Maxime
  a un vrai fichier qui commence par cette signature, le décoder proprement est un ajout additif trivial
  une fois sa structure réelle connue.

  **Item 10 — Tier A, F1 PUPDatabase Orphan Playlist (`POPPER_ORPHAN_PLAYLIST`, Warning). File Tier A
  terminée.** Le handoff ne précisait que « jointure Games×Playlists via PlaylistID orpheline » sans
  schéma — **recherche faite avant d'écrire la moindre requête**, le schéma exact n'était documenté
  nulle part dans ce repo. Confirmé via le propre wiki de NailBuster (créateur de PinUP Popper, requêtes
  SQL postées par lui-même) et plusieurs fils de forum indépendants convergents : l'appartenance à une
  playlist n'est **pas** une colonne sur `Games`, c'est une table de jonction `PlayListDetails`
  (`GameID`, `PlayListID`, `isFav`), jointe à `Playlists` (`PlayListID`). Comportement confirmé en terrain
  (VPForums #50896) : supprimer une playlist depuis l'UI Popper ne retire que sa ligne dans `Playlists` —
  les lignes `PlayListDetails` restent, pointant dans le vide, et ça fige le menu du frontend à
  l'ouverture. `Services/PlaylistIntegrityInspector.cs` (pur : croise les `PlayListID` référencés contre
  les `PlayListID` réels ; exclut délibérément les lignes `isFav=2` — le pseudo-« favoris global »
  intégré à Popper, dont on n'a pas pu confirmer s'il pointe légitimement vers un `PlayListID`
  inexistant par construction — plutôt que risquer un FP massif sur cette convention non tranchée) +
  `Scanning/PopperPlaylistScanner.cs` (lecture seule via `SqliteReader.TryReadTable`, conforme ADR-007 ;
  résolution best-effort du nom du jeu via `Games` pour un message lisible, avec repli sur le GameID brut
  si cette lecture bonus échoue ; **un seul finding résumé** avec compteur + jusqu'à 8 exemples, même
  patron que `POPPER_MEDIA_MISSING`) + `tests/PopperPlaylistScannerTests.cs` (19 tests neufs).
  Knowledge/Loc (FR/EN + `cat.popperplaylist`) + `.Add(new PopperPlaylistScanner())`. **Core 260→279/279**,
  Repair 105/105 inchangé, Debug ET Release, Roslyn 0 erreur sur les 6 fichiers touchés. Id
  `popperplaylist` sans collision.
  **Lacune de recherche assumée** : le nom de la colonne « titre » sur `Playlists` elle-même n'a été
  confirmé nulle part — le scanner ne l'affiche donc jamais (seuls les jeux affectés sont nommés). Ajout
  additif trivial si Maxime confirme un jour ce nom de colonne.

  **Bilan file Tier A (handoff §3, items 1 à 10) : terminée intégralement.** E1, H1, B1, B2, C1, G3, H2,
  F1 tous livrés, testés, vérifiés Roslyn, écrits sur disque, commités localement. Core 144→279/279,
  Repair 105/105 stable sur toute la file, Debug ET Release à chaque étape. Aucun scanner existant
  modifié. Sur consigne explicite de Maxime reçue en cours de session (« termine »), la file Tier B
  (D1/C2/A1-détection/B3/G1/E2/A2/A3, item 11 du plan) n'est **pas** attaquée cette session — décision de
  cadrage loguée ici plutôt que silencieuse, cohérente avec « chaque item est indépendamment expédiable » :
  la Tier A déterministe est un point d'arrêt propre et complet, la Tier B (heuristique, doctrine Note)
  reste entièrement disponible pour une prochaine session sans aucune dette laissée par ce choix.

  **Clôture de session — dégel formalisé (ADR-010) + revue CTO/Product (consigne permanente Maxime).**
  Avant de considérer la file terminée : `docs/adr/ADR-010-degel-scanner-doctrine-note.md` écrit
  (formalise la décision déjà prise par Maxime le 05/08, distingue la porte 🟢 déterministe — ship
  direct, plus de gate deux-signaux — de la porte 🟡 heuristique — doctrine Note obligatoire) ;
  `PROJECT-BRAIN` §6 (279/105, 21 scanners listés par id) et §7 (dégel documenté, ancienne ligne de gel
  marquée supersédée plutôt que supprimée) mis à jour ; `TRANSMISSION.md` reçoit le bloc de clôture
  mandaté par le handoff (§10) avec sa section `DÉCISIONS EN ATTENTE`.

  Revue de skills faite avant clôture (`ListSkills`, consigne permanente de Maxime) : aucun skill
  spécialisé (.NET/C#, revue de code, sécurité, architecture) disponible dans cette session
  au-delà de l'outillage déjà mobilisé pendant la file (agents de recherche primaire-source avant
  code, vérification Roslyn syntaxique) — constaté explicitement plutôt que supposé, conforme à la
  consigne « si aucun skill pertinent n'est disponible, indique-le et poursuis ».

  **Revue CTO + Product (consigne permanente de Maxime, à chaque clôture de tâche) :**
  - *Le code est-il propre ?* Oui — gabarit du comparateur cloné à l'identique sur les 8 items (pur en
    `Services/` + `IScanner` mince à I/O injectée en `Scanning/`), zéro dépendance externe, aucune
    convention de nommage rompue. Une régression cosmétique (indentation perdue sur une ligne
    `Loc.cs`) trouvée et corrigée en cours de route par relecture, pas par accident découvert plus
    tard. Point non nettoyé, assumé : `AutoFixable` reste positionné `false`/absent sur les 8 entrées
    `Knowledge.cs` neuves, cohérent avec les 9 codes existants, mais le flag lui-même reste mort dans
    toute l'App (voir DÉCISIONS EN ATTENTE ci-dessous).
  - *L'architecture reste-t-elle cohérente ?* Oui, plutôt renforcée : point de composition toujours
    unique (`MainWindow.xaml.cs`, 21 lignes `.Add`), `ScanContext`/`IScanner` inchangés, aucun des 21
    scanners existants modifié. `MonitorTopologyProbe.cs` créé neuf plutôt que d'étendre
    `DisplayProbe.cs` (consigne du handoff respectée à la lettre) — la frontière « un fichier P/Invoke
    = une responsabilité » reste nette plutôt que de commencer à s'éroder.
  - *Les tests sont-ils suffisants ?* 135 tests neufs sur 8 scanners (144→279), chaque fichier couvrant
    au minimum : chemin nominal, silence sur donnée absente/illisible, silence sur exception, et au
    moins un cas limite anti-FP (dédoublonnage, casse, exclusion `isFav=2`, etc.). Limite structurelle
    assumée, pas nouvelle à cette session : le vrai chemin d'I/O Windows (P/Invoke réel de
    `MonitorTopologyProbe`, lecture réelle de reparse point par `JunctionScanner`) n'est testable que
    sur un vrai Windows — seul `build.cmd`/le test terrain de Maxime referme cette boucle, comme pour
    tout scanner Windows précédent du projet.
  - *Cette fonctionnalité apporte-t-elle une vraie valeur utilisateur ?* Oui, concrètement : les 8
    items ciblent des pannes « invisibles jusqu'à ce que ça morde » réelles et courantes en pincab —
    jonction cassée après débranchement d'un disque, samples altsound renommés/absents, playlist
    Popper orpheline qui fige le menu du frontend, backglass hors écran après reconfiguration moniteur,
    NVRAM vidée qui perd les high-scores en silence, colorisation incomplète. Exactement la promesse
    du moteur (symptôme → cause → correctif). Réserve honnête : aucun des 8 n'a encore tourné sur un
    vrai cab — la validation terrain reste à faire, pas contournée par le dégel (ADR-010 remplace la
    barrière d'entrée, pas la vérification a posteriori).
  - *Y a-t-il un risque technique ou commercial ?* Technique : faible et maîtrisé par construction
    (additif, biais silence partout, dégrade sans jamais lever d'exception visible). Commercial : le
    seul risque réel du projet — un faux positif public — n'est pas nul sur 3 des 8 items (C1, H2, F1)
    dont la prémisse a été *corrigée* par recherche plutôt que confirmée par un utilisateur réel ; le
    design biaise déjà vers le silence sur ces trois, mais c'est un vrai delta d'incertitude par
    rapport aux items purement mécaniques (E1, H1, G3). Le saut 12→21 scanners est le plus gros
    changement de surface du Scanner depuis sa clôture du 03/08 — §7 du Brain priorise déjà « tester
    sur le cab réel » avant tout le reste, et cette session le rend encore plus vrai qu'avant.
  - *Amélioration à faible coût, proposée sans être codée ?* Trois candidats, aucun codé : (1) les 6
    `cat.*` manquants sur des scanners pré-existants (`legacy`, `disk`, `process`, `display`,
    `media-orphan`, `vpxversion`) — additif, ~6 paires de lignes `Loc.cs`, risque nul ; (2) le flag
    `AutoFixable` mort — soit le câbler sur le vrai calcul `RepairOfferBuilder`/`RepairActionRegistry`,
    soit le documenter comme décoratif pour qu'un futur lecteur n'assume pas qu'il fait quelque chose ;
    (3) le plus rentable : demander spécifiquement à 1-2 utilisateurs terrain déjà identifiés (Gregg,
    itchigo) de faire tourner le prochain build sur les trois items à prémisse corrigée (C1, H2, F1) en
    premier — beaucoup moins cher que deviner davantage, et exactement la discipline FIELD-LOG déjà en
    place pour tout le reste du projet.

  **Item 11 — les 3 améliorations à coût faible, faites sur demande explicite de Maxime (post-clôture,
  même journée).** Maxime revenu actif : « fais les 3 amélioration a cout faible et c'est poussé sur
  git tu peux verifier ».
  - **Vérification git faite avant tout code** (sa deuxième demande) : `git fetch origin main` depuis
    le sandbox — la lecture réseau vers GitHub n'est **pas** bloquée par le proxy, seul le push
    l'était. `origin/main` est à `403f3d5`, auteur Maxime Chauvin, message identique à la commande
    fournie à la clôture précédente. `git diff --stat a57d414 origin/main` confirme les **34 fichiers
    exacts** de la session (mêmes noms, tailles cohérentes) déjà présents sur GitHub. **Push confirmé
    réussi**, rien à refaire côté Maxime pour la partie précédente.
  - **#1 fait** : 6 entrées `cat.*` ajoutées dans `Loc.cs` (En+Fr) — `cat.legacy`, `cat.disk`,
    `cat.process`, `cat.display`, `cat.media-orphan`, `cat.vpxversion`. Patron identique aux 8 entrées
    déjà ajoutées cette session. Roslyn 0 erreur, Core 279/279 + Repair 105/105 inchangés (fichier App,
    aucun test n'y touche, revérifié quand même).
  - **#2 fait, en choisissant délibérément « documenter » plutôt que « câbler »** (les deux options
    avaient été laissées ouvertes à la clôture précédente) : câbler `AutoFixable` sur un vrai signal
    aurait été **architecturalement faux**, pas juste plus cher — la fixabilité réelle dépend de l'état
    runtime (licence, préflight, bugs spécifiques à une action comme le mismatch GUID/chemin de
    `set_default_audio_device`) qu'un bool statique par code ne peut structurellement pas représenter
    correctement ; c'est précisément pour ça que `RepairOfferBuilder` a été construit en dehors de ce
    flag. Câbler aurait réintroduit l'imprécision que le moteur actuel évite déjà. Doc-comment XML
    clair ajouté sur `KnowledgeEntry.AutoFixable` et `Knowledge.IsAutoFixable` (vestigial, zéro lecteur
    vérifié par grep .cs **et** .xaml, pointeur vers `RepairOfferBuilder` comme vrai mécanisme).
    Aucune modification de comportement, risque nul par construction (commentaire seul). Roslyn 0
    erreur, Core/Repair inchangés.
  - **#3 pas codable** (action terrain, pas du code) : message de sollicitation rédigé en anglais
    (registre du forum VPForums) pour Gregg et itchigo, demandant de tester en priorité C1/H2/F1 sur
    le prochain build — donné à Maxime dans le chat, pas encore envoyé (aucun canal direct vers le
    forum depuis cette session).
  - **Core 279/279, Repair 105/105, Debug ET Release, revérifiés après les 2 changements de code.**

  **Item 12 — Tier B (5/5 livrables livrés), sur confirmation implicite de Maxime (« ok je vais
  tester le scaner si tu la finis »).** Repris après un aller-retour hors-mandat : un vrai build
  Windows (`build.cmd`) a échoué (CS0103 sur `Path`/`File` dans `RepairOfferBuilder.cs`, using
  manquants — bug préexistant du 04/08, jamais vu par le Roslyn syntax-only de ce sandbox), corrigé
  et confirmé vert par Maxime (2 captures d'écran) avant de reprendre Tier B (commit `83f9799`).
  - **D1 Audio Current-State (`AUDIO_DEFAULT_SUSPECT`, Note)** — nouveau lecteur COM read-only côté
    Core (`AudioEndpointReader`) : mirror volontairement appauvri de `RealAudioDeviceControl`
    (Repair) — seulement `GetDefaultAudioEndpoint`+`OpenPropertyStore`+`GetValue`, `IPolicyConfig`
    absent du fichier donc **structurellement incapable d'écrire** le périphérique, pas juste par
    convention. Périmètre volontairement réduit vs la fiche originale (audit §4-D1) : seul le nom du
    device par défaut est comparé à des marqueurs HDMI/écran — PAS "aucun endpoint activé" ni
    "volume à zéro" (surface COM plus large, hors scope de cette passe). Détection seule : PAS
    branché sur l'action Repair `set_default_audio_device` déjà codée — il manque un nom de
    périphérique CIBLE (ex. "Speakers") que je ne peux pas deviner pour un cab arbitraire ; c'est
    une décision Knowledge Pack `repairRules` pour Maxime.
  - **C2 DPI Scaling (`DPI_SCALING_NONSTANDARD`, Note)** — lecture registre `AppliedDPI`
    (`WindowMetrics`), même patron P/Invoke que `VpinmameRegistry`. 96 = 100 % (constante Windows
    connue) ; le pourcentage est un fait déterministe, la CONSÉQUENCE (fenêtre tronquée) ne l'est
    pas → Note, pas Warning.
  - **B3 dmddevice.ini COM-Probe (`DMD_COM_PORT_NOT_FOUND`, Note)** — parseur INI maison
    (`DmdDeviceIniParser`, zéro dépendance, même esprit que le parseur CSV d'AltSound) + nouveau
    lecteur registre `SerialPortRegistry` (énumération `RegEnumValue` sur
    `HKLM\HARDWARE\DEVICEMAP\SERIALCOMM` — première utilisation de `RegEnumValue` dans ce projet,
    plus complexe que les lectures à valeur unique déjà faites). ⚠️ **Incertitude non résolue** : le
    nom exact de la clé INI pour le port COM (`port`/`comport`/`com_port`/`serialport` tous
    acceptés, faute de fichier réel `dmddevice.ini` disponible pour vérifier) — voir DÉCISIONS EN
    ATTENTE #9. Biais silence total si l'énumération SERIALCOMM renvoie un ensemble vide (ne peut
    pas distinguer "échec de lecture" de "aucun port existant").
  - **G1 Séparateur décimal FR (`LOCALE_DECIMAL_SEPARATOR`, Note)** — déviation actée et formalisée
    ici : `CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator` plutôt que la lecture
    registre directe `HKCU\Control Panel\International\sDecimal` suggérée par la fiche — même fait
    effectif, BCL pur, zéro P/Invoke, plus simple et plus sûr. Rien ne change côté détection,
    seulement le mécanisme de lecture.
  - **E2 Registry/INI Phantom Conflict (`VPINMAME_CONFIG_PHANTOM`, Note)** — nouveau lecteur étroit
    `VpinmameKeyProbe` (existence de la clé `HKCU\...\Visual PinMame` seulement, casse alignée sur
    celle déjà validée empiriquement par `VpinmameRegistry` plutôt que celle — probablement fautive
    — de la prose de l'audit) ; `VpinmameRegistry.cs` existant **non modifié**. Ne tranche pas
    lequel (registre ou .ini) a effectivement la main — énoncé comme fait de coexistence, pas
    verdict de précédence (Doctrine Note règle 2).
  - **A1 Script Doctor — reporté, décision motivée (PAS codé)** : la fiche demande de comparer la
    version détectée à « un plancher connu (donnée de profil, PAS en dur) » — ce champ n'existe pas
    dans `Profile.cs`/`profiles/vpx-popper.json` aujourd'hui, et la valeur (quelle version compte
    comme « périmée » pour `core.vbs`/`controller.vbs`/`VPMKeys.vbs`/`nudge.vbs`) est un jugement
    métier que je ne peux pas deviner. Détection de présence seule, sans comparaison, produirait un
    Note sans delta actionnable ("core.vbs présent, version 4.5") — ne passe pas la barre "vraie
    valeur utilisateur" de la revue CTO+Product. Reporté plutôt que bâclé ; débloquable rapidement
    si Maxime fournit les planchers par script (nouveau champ profil) — voir DÉCISIONS EN ATTENTE #10.
  - **A2 (polices) / A3 (chemins en dur) — reportés** : sous-spécifiés dans la fiche et l'audit
    (quelle regex de police ? quel seuil "chemin suspect" ?), même discipline anti-supposition.
  - **Core 279→321/321, Repair 105/105, Debug ET Release, tout vert.** Roslyn 0 erreur sur les 3
    fichiers App touchés (`MainWindow.xaml.cs`, `Knowledge.cs`, `Loc.cs`) — outil `/tmp/roslyn-check`
    déjà utilisé pour le fix `RepairOfferBuilder.cs` de cette session ; `using
    PincabToolbox.Core.Scanning;`/`.Services;` déjà présents dans `MainWindow.xaml.cs` (même
    namespace que les scanners Tier A existants) → risque "using manquant" nul pour la ligne `.Add`
    elle-même (contrairement au bug qui a cassé le build ce matin).
  - Un seul commit consolidé (`14894ed`, pas 5 séparés comme Tier A) : les 3 fichiers App
    (Knowledge/Loc/MainWindow) touchent les 5 codes à la fois dans le même diff, les séparer après
    coup aurait été artificiel — message de commit détaillé par code en compensation.

## DÉCISIONS EN ATTENTE (pour Maxime)
Rien n'a bloqué la file cette session au sens R3 du handoff (aucune décision qui n'était pas déjà
prise n'a empêché un item d'être livré). Les points ci-dessous sont des améliorations à faible coût
repérées en cours de route et volontairement **non codées** hors mandat — consolidées ici pour la
revue de clôture plutôt que dispersées, cf. détail complet par item plus haut :
1. ✅ **FAIT (Item 11)** — ~~6 scanners pré-existants sans entrée `cat.*` dans `Loc.cs`~~
   (`legacy`,`disk`,`process`,`display`,`media-orphan`,`vpxversion`).
2. ✅ **FAIT (Item 11)** — ~~`Knowledge.KnowledgeEntry.AutoFixable` est un flag mort~~ — documenté
   comme vestigial plutôt que câblé (raisonnement complet dans l'entrée Item 11).
3. **Format legacy `.ini` (g-sound) pour AltSound non couvert** (B2) — aucun schéma vérifiable trouvé ;
   additif si Maxime a un exemple réel.
4. **DLL 32/64-bit de colorisation non vérifiées** (B1, sous-point audit §4) — aucun nom de fichier
   distinct confirmé au-delà de ce que `BitnessScanner` couvre déjà.
5. **Position DMD non vérifiée par `ScreenTopologyScanner`** (C1, seul le backglass l'est) — deux
   lectures possibles de la doc officielle, jamais recoupées par une deuxième source ; fichiers
   pré-2.0.0 (sans marqueur `# V2`) volontairement ignorés pour la même raison.
6. **`.directb2s` compressé (OLE/MS-CFB) traité en silence, jamais décodé** (H2) — aucune preuve
   qu'une telle variante existe réellement ; si Maxime a un fichier qui matche la signature MS-CFB,
   sa structure interne reste à documenter avant tout décodage.
7. **Sémantique `isFav=2` sur `PlayListDetails` et nom de colonne « titre » sur `Playlists` non
   confirmés** (F1) — les favoris globaux sont exclus du check par prudence plutôt que par certitude ;
   le nom de la playlist elle-même n'est donc jamais affiché, seuls les jeux affectés le sont.
8. ✅ **FAIT (Item 12)** — ~~File Tier B entièrement reportée~~ — D1/C2/B3/G1/E2 livrés (5/5, tous
   Note, commit `14894ed`). A1 reste reporté (voir #10 ci-dessous) ; A2/A3 aussi (sous-spécifiés,
   inchangé depuis la clôture Tier A).
9. **B3 — nom de clé INI du port COM non confirmé sur un vrai `dmddevice.ini`** — `DmdDeviceIniParser`
   accepte `port`/`comport`/`com_port`/`serialport` faute de fichier réel disponible pour vérifier
   lequel Freezy dmd-extensions utilise réellement. Sans impact sur la sûreté (biais silence si aucun
   ne matche), mais peut sous-détecter si le vrai nom est différent. **Action à faible coût pour
   Maxime** : coller le contenu d'un `dmddevice.ini` réel (le sien, ou un exemple communautaire) dans
   le chat, ou confirmer directement le nom de clé — une ligne de code à ajuster si besoin.
10. **A1 Script Doctor bloqué par l'absence d'un plancher de version en donnée de profil** — pour
    debloquer, il faudrait un nouveau champ dans `profiles/vpx-popper.json` (ex. un objet
    `sharedScriptFloors: { "core.vbs": "x.y", "controller.vbs": "x.y", ... }`) avec, pour chaque
    script partagé, la version en-dessous de laquelle le déclarer périmé. C'est un jugement métier
    (quelle version fait référence aujourd'hui dans la communauté vpinball) que je n'ai pas la
    légitimité de deviner — **décision Maxime**, débloquable en une session courte une fois les
    valeurs connues.
11. ✅ **FAIT (entrée 07/08 bis)** — ~~Bug confirmé (lecture de code) — énumération paresseuse non
    protégée dans 2 scanners existants~~ (`BlockedFileScanner.cs` module `security`,
    `CompletenessScanner.CollectWheelStems`) — corrigé en répliquant le patron
    `LayoutDetector.SafeEnumerateDirs` (try/catch PAR dossier). **Pas encore committé/pushé** (git
    lock résiduel côté machine Maxime) ni rebuild vérifié (pas de `dotnet` disponible cette session) —
    action Maxime : voir entrée FIELD-LOG 07/08 (bis) et bloc Git dans TRANSMISSION.
12. **KPI #1 toujours ouvert — les 8 `ROM_MISSING` critical (Blood Machines, hpgf-052-DOF, Jurassic
    Park, leprechaun, Munsters 2020, Stranger Things SE, The Goonies, Willy Wonka Pro) sont-ils de
    vrais hacks nécessitant une ROM précise, ou des originales/homebrew qui ne devraient rien
    réclamer ?** Liste identique sur 2 sessions distinctes (04/08 et 07/08, plusieurs jours d'écart) —
    stabilité qui argue plutôt pour « vrais hacks » (une originale mal classée aurait des chances de
    varier ou d'être reconnue par Maxime d'un coup d'œil), mais ne tranche rien sans vérification
    d'au moins un cas. Un seul cas confirmé homebrew suffirait à rouvrir le chantier FP.
13. **[Basse priorité, pas un bug] Dé-emphase `B2S_MISSING`/`B2S_ORPHAN` pour les cabs sans
    backglass** — le cab réel de Maxime (photo confirmée : tabletop/cocktail, écran unique) génère
    ~205 findings combinés structurellement inévitables sur ce type de cab. Piste produit seulement
    (regrouper/dé-prioriser ces codes quand `DisplaySetupScanner` ne détecte aucun second écran) —
    pas codée, pas demandée, à garder en tête pour une session UX future.

## 2026-08-05 (session Opus, 2 missions + dégel + palier Note) · Comparateur VPX livré + audit Scanner + handoff autonome Sonnet 5
- code:        VPX_VERSION_OUTDATED (nouveau, Warning) · Severity.Note (nouveau palier) · transverse (audit/produit)
- bac:         FIX (nouveau scanner + palier) + FEATURE (audit) + décision produit (dégel)
- contexte:    Maxime, 2 missions (carte blanche, ordre libre) : (1) coder le comparateur version VPX
  (chantier identifié MAJ 05/08 (4)) ; (2) audit fonctionnel complet du Scanner + vision produit + handoff
  Sonnet 5. En cours de session, décisions produit de Maxime : **dégel du gel Scanner**, « rendre les 🟡
  sûrs », « bouton de MAJ », et **« une nouvelle catégorie que Info, genre note »**.
- analyse:
  1. **Comparateur VPX livré, vert** (Core 140→144/144, Repair 105/105, Debug+Release). Le morceau que
     `CompatibilityScanner` laissait explicitement à faire. Sévérité = **Warning, pas Critical** (la
     déclaration « requires VPX 10.x » est une heuristique de commentaire ; un faux Critical plomberait le
     score et le bandeau — dégât asymétrique du 30/07, verrouillé par un test dédié). Silence total si
     version installée indétectable. Multi-exe → on prend la **plus haute** installée. 3 fichiers neufs +
     1 ligne MainWindow, aucun scanner existant touché. Loose end : pas encore d'entrée Knowledge.cs/Loc.cs
     pour `VPX_VERSION_OUTDATED` → délégué à Sonnet (handoff R1).
  2. **Audit Scanner** (`docs/AUDIT-Scanner-2026-08.md`) : 6 catégories non couvertes, ancrées code réel
     des 12 scanners + FIELD-LOG + corroboration terrain (pas web seul). Classement **FP-risk (🟢
     déterministe / 🟡 heuristique)** qui pilote la barre de preuve. Arbitrage des 2 salves Gemini
     (pépite/glaise, bonne pêche cette fois : core.vbs, ScreenRes+B2STableSettings, AltColor/AltSound,
     VPMAlias loop, NVRAM 0-octet, directb2s XML, PUPDatabase orphelin…). Monétisation par ligne
     (détection gratuite → fix/gestion payante) ; Table Companion confirmé meilleur 2ᵉ produit. §8.4 bouton
     de MAJ (canal Knowledge Pack = valeur ADR-002 ; canal binaire conditionné à la signature de code).
  3. **DÉGEL** (décision Maxime) : le Scanner rouvre. Le gel de *calendrier* tombe, la règle anti-FP reste.
  4. **Doctrine Note + nouveau palier `Severity.Note`** (demande Maxime) : entre `Info` et `Warning`,
     score-neutre, jamais « FIX THIS FIRST ». Rend les 🟡 shippables **en énonçant le fait en `Note`**
     (jamais le jugement en `Warning`), escalade `Warning` seulement sur du déterministe, résumé par-table
     (2ᵉ leçon du 30/07 : pas de bruit). **Partie Core livrée verte** (`Finding.cs` : Ok=0/Info=1/Note=2/
     Warning=3/Critical=4 ; score/rollup gèrent Note comme Info sans changement de formule ; 4 tests
     `SeverityNoteTests`). **Reste le rendu App** (libellé FR « À noter »/EN « Note », couleur, 6 exports)
     = prérequis Sonnet avant tout scanner Tier B (sinon `switch` App non exhaustif → crash runtime).
  5. **Handoff Sonnet 5 autonome** (`docs/HANDOFF-Sonnet5-scanners-2026-08.md`) : effort max, zéro question,
     never-block, décisions pré-tranchées (R1-R6), recette build sandbox, gabarit = le comparateur, file
     Tier A (🟢 → Warning) + Tier B (🟡 → Note). Objectif : la session de demain avance seule sans Maxime.
- disposition: comparateur + palier Note (Core) + 2 docs écrits sur le disque de Maxime, tests verts.
  **À formaliser par Maxime/ADR** : reporter le dégel dans `PROJECT-BRAIN` ; ADR core.vbs OSS ; acter
  Table Companion 2ᵉ produit ; ADR carve-out auto-update. **Git à pusher par Maxime** (le proxy bloque le
  repo depuis le sandbox — commande dans TRANSMISSION MAJ (5)). Sonnet exécute le handoff demain.

## 2026-08-05 (solo, carte blanche après l'heure) · Durcissement licence codé, limites tenues sur Scanner/nouvelles actions
- code:        transverse (sécurité) — pas un finding
- bac:         FIX (infra)
- contexte:    Maxime, en réponse au récap de l'heure solo : « pas le temps de discuter, si tu as
  trouvé c'est que ça vaut le coup, réalise tes hypothèses, corrige ce qui doit être corrigé, code
  ce qui doit être codé, carte blanche. »
- analyse:
  1. **Codé et livré** — les 2 durcissements mineurs de la revue sécurité (non urgents, mais sûrs
     et sans conflit ADR) : (a) borne de taille (`MaxLicenseKeyLength = 4096`) avant tout décodage
     dans `LicenseVerifier.Verify` — rejette vite un copier-coller massivement erroné au lieu de le
     décoder en mémoire pour rien ; (b) `key?.Dispose()` dans le `catch` du constructeur
     `LicenseVerifier(string)` — avant le fix, si `ImportSubjectPublicKeyInfo` levait après que
     `ECDsa.Create()` ait déjà pris un handle crypto natif, ce handle n'était jamais libéré ; ce
     chemin est emprunté à **chaque démarrage de l'app aujourd'hui** (la clé publique embarquée est
     encore le placeholder). 2 nouveaux tests de régression
     (`Test_OversizedLicenseKey_RejectedBeforeDecoding`,
     `Test_RepeatedFailedKeyImport_DoesNotThrowOrLeaveInconsistentState`). **128/128 Core, 105/105
     Repair (103 + 2 nouveaux), Debug ET Release, tout vert.**
  2. **Sciemment NON fait, malgré la carte blanche** — et pourquoi, pour que ce soit tranché plutôt
     que silencieux :
     - **Scanner (Black Knight / Rocky & Bullwinkle)** : mes deux hypothèses restent des hypothèses,
       pas des faits confirmés par Gregg. Coder une "correction" sur `ScriptAnalyzer.cs`/
       `RomValidatorScanner.cs` sans savoir laquelle des pistes (ou aucune) est la vraie cause,
       reviendrait à modifier le Scanner — gelé, jamais rouvert, règle non négociable rappelée en
       tout début de cette session — à l'aveugle. Le risque n'est pas symétrique : le Scanner
       gratuit est ce qui construit la confiance de tout le reste ; une régression introduite pour
       "peut-être" corriger 2 cas isolés serait pire que le statu quo. Les questions de diagnostic
       exactes sont prêtes (entrée précédente) — carte blanche utilisée pour les préparer
       immédiatement, pas pour deviner du code dessus.
     - **6 idées de fonctionnalités produit** : aucune n'a été codée. Deux raisons structurelles,
       pas juste de la prudence : (i) `PROJECT-BRAIN` §7 point 4 dit explicitement que les
       confiances (98/88) doivent être **calibrées sur du terrain réel** — aucune des 6 idées n'a
       encore ce terrain, coder une confiance devinée serait le même genre d'erreur que le gating
       licence qu'on a déjà dû corriger le 03/08 (survente) ; (ii) l'idée n°1 (scanner base Popper)
       touche de front ADR-007, qui est une décision produit explicitement réservée à Maxime, pas
       un feu vert technique. Carte blanche interprétée comme « agis sans me demander sur ce qui
       est vraiment prêt », pas comme « lève les garde-fous qu'on a posés justement pour les
       moments de rush ».
- disposition: durcissement licence livré sur le disque de Maxime, testé vert. Le reste reste en
  attente de : (a) réponse de Gregg aux 2 questions de diagnostic, (b) arbitrage de Maxime sur les
  6 idées produit (en particulier la conversation à ouvrir sur ADR-007).

## 2026-08-05 (solo, 1h) · Réponse Gregg (3 cas) + revue sécurité licence + recherche produit
- code:        ROM_MISSING (Black Knight SOR, Rocky & Bullwinkle) + transverse (sécurité) + FEATURE (x6)
- bac:         FN (à confirmer) · FN (à confirmer) · FIX (confirmé) · sécurité RAS · FEATURE x6
- contexte:    Maxime m'a laissé 1h en autonomie avec 3 mandats : traiter la réponse de Gregg sur
  les 3 cas ouverts (rapport HTML + 3 captures jointes), vérifier que la protection licence tient,
  chercher de nouvelles fonctionnalités vendables même sans signal direct de Maxime. Consigne
  rappelée et respectée : ne pas coder sur un signal isolé, vérifier PROJECT-BRAIN/ADR avant toute
  proposition, ne rien télécharger (ADR-004), ne pas toucher à l'Écran 2 Repair.

### a) Black Knight: Sword of Rage — toujours CRITICAL après l'ajout de la ROM par Gregg
- verbatim:    « Black Knight Sword of Rage - added the rom (bksor.zip) and run the PinCabToolbox
  again. Result: see screenshot & new PinCabToolbox report. »
- analyse:     Le rapport HTML rejoué (2026-08-04 15:20) confirme `bksor.zip` toujours signalé
  manquant (ligne 24, + variante PUP ligne 25), cohérent avec sa capture d'écran. **Code
  relu en entier** (`RomValidatorScanner.cs`, `ScanEngine.cs` lignes 1-60) : la logique de
  présence (HashSet insensible à la casse, peuplé depuis les `.zip` de premier niveau du dossier
  roms) est correcte, pas de bug trouvé côté scanner. Hypothèse la plus probable — **non
  confirmée, je ne code rien dessus** : piège classique Windows "extensions de fichiers masquées"
  (le fichier réellement enregistré serait `bksor.zip.zip`, invisible dans l'explorateur avec les
  extensions masquées). Autre possibilité : le zip a été déposé dans un sous-dossier plutôt qu'à
  la racine du dossier roms (le scan est `TopDirectoryOnly` par design, ADR à vérifier avant de
  changer ce comportement — pas fait ici).
- disposition: à répondre à Gregg via Maxime — question de diagnostic précise à poser : « Peux-tu
  faire un clic droit → Propriétés sur le fichier dans le dossier roms et coller le nom exact
  affiché, extensions comprises ? Et confirmer qu'il est directement dans le dossier roms, pas
  dans un sous-dossier ? » Pas de fix codé tant que la cause n'est pas confirmée.

### b) The Adventures of Rocky & Bullwinkle — mauvais nom de ROM attendu par le scan
- verbatim:    « Rocky & Bullwinkle: added screenshots. Seems that the scan is looking for another
  rom name than the actual rom used by the table. »
- analyse:     Capture d'écran de l'éditeur de script montre `Const cGameName = "rab_320"` actif
  ligne 126, `"rab_130"` commenté ligne 127 (table "...Bigus(MOD)1.0"). Mais le rapport HTML,
  pour une entrée "...0.96" (nom/version différents — table distincte ou version antérieure dans
  sa collection), attend `Rab.zip` : ne correspond ni à `rab_320` ni à `rab_130`. **Code relu en
  entier** (`ScriptAnalyzer.cs`) : `RomRequirement.Primary => Candidates[0]`, c'est-à-dire le
  **premier** nom trouvé dans l'ordre du fichier (Const, puis assignations non-const, puis
  `.GameName` en dernier recours) — jamais résolu sémantiquement comme "la déclaration
  effectivement active". Si un script a 3+ déclarations `cGameName` (modules inclus, code mort
  commenté ailleurs que ce qu'on voit sur la capture), `Primary` peut pointer sur la mauvaise.
  **Hypothèse plausible mais non confirmée** — je n'ai vu que les lignes 125-128 du script, pas le
  fichier complet. Point important : ceci n'affecte que le nom de fichier suggéré dans le
  fix-hint, PAS le verdict pass/fail lui-même (qui teste TOUS les candidats en OR) — donc si
  `rab_320.zip` est bien présent, le finding ne devrait normalement pas apparaître du tout ; le
  fait qu'il apparaisse renforce l'hypothèse d'une 3e déclaration non vue.
- disposition: à répondre à Gregg via Maxime — demander la liste complète de toute occurrence de
  `cGameName` dans le script complet de la table (recherche "cGameName" dans l'éditeur, toutes
  occurrences, pas seulement autour de la ligne 126). Pas de fix codé sans cette confirmation —
  toucher `ScriptAnalyzer.cs` sur un seul signal partiel serait exactement l'erreur déjà commise
  une fois sur `POPPER_NOT_REGISTERED` (ADR-007).

### c) Amazing Spiderman — résolu, pas un bug
- verbatim:    « Amaz. Spiderman - Solved: B2S had a different name as the actual table. »
- analyse:     Auto-résolu par Gregg (renommage du fichier B2S). Pas un faux positif : le scan
  avait raison de signaler le mismatch.
- disposition: classé résolu, rien à changer côté outil.

### d) Contexte persona (relayé par Maxime, thread public) — utile pour la recherche produit ci-dessous
- verbatim itchigo : « My setup isn't typical... I have a setup about the same size as Flying
  Dutchman, but usually I know what the issue is when something doesn't work. So this tool may not
  be for me, but I know others that haven't been around can definitely use it. »
- verbatim Gregg : « I agree .. but sometimes having so much (well quality) pinball tables in my
  setup, I am not able to oversee it all. A tool like this helps me in pinpointing to tables I
  don't touch that frequently. »
- analyse:     Confirme un persona déjà pressenti : pas le débutant total, mais le curateur de
  grosse collection qui ne peut plus tout superviser manuellement. Utile pour prioriser les idées
  de fonctionnalités ci-dessous (le pain point n°1 y correspond directement).

### e) Revue sécurité — module Licensing + gating Repair (`RepairModeResolver`)
- contexte:    Mandat Maxime : vérifier que la protection licence tient, corriger si problème réel.
  Agent `code-reviewer` indisponible dans cet environnement (type inexistant) → relancé en
  `general-purpose`, puis complété par une lecture directe de `RepairModeResolver.cs` (absent de
  l'extrait initialement donné à l'agent) pour trancher le seul point resté incertain.
- analyse:     **RAS globalement.** Signature ECDSA P-256 : pas de confusion d'algorithme possible
  (algo fixé en dur des deux côtés, pas de champ `alg` piloté par l'attaquant), jamais de fallback
  permissif (`signatureOk` doit être vrai avant tout retour valide), jamais d'exception non gérée
  (`Test_Garbage_NeverThrows` couvre déjà ça). Parsing base64url + JSON entièrement défensif, pas
  de désérialisation polymorphe dangereuse. Clé publique embarquée = placeholder non-base64
  valide, dégrade proprement en "tout refuser" (mode d'échec sûr), aucun risque avant le vrai
  `license-tool init`. **Gating** : `RepairModeResolver.Resolve` est une fonction pure lue en
  entier — `licensed=false` ne peut structurellement produire que `ManualOnly` ou `Locked`, jamais
  `Automatic`/`ConfirmationRequired` (gate sécurité avant gate commerciale, ADR déjà en place
  03/08). Aucun appel à `Apply`/`Preflight`/`Undo` nulle part dans l'App (grep confirmé) ; le seul
  appel à `Plan()` dans l'App (`RepairOfferBuilder.cs`) force `licensed: false` en dur. Aucun
  bypass identifié.
- disposition: 2 pistes de durcissement mineures notées, non codées (pas de bug réel, juste de la
  défense en profondeur, à ne faire que si Maxime le souhaite) : (1) borne de taille sur
  `licenseKey` avant décodage dans `LicenseVerifier.Verify` (DoS mémoire théorique, impact
  négligeable en usage local mono-utilisateur) ; (2) `ECDsa` non explicitement disposé si
  `ImportSubjectPublicKeyInfo` échoue au constructeur (fuite ressource native négligeable, un seul
  objet au démarrage). Rien d'urgent, rien codé.

### f) Recherche produit — 6 idées de fonctionnalités vendables (aucun chiffre de marché inventé)
- contexte:    Mandat Maxime : trouver ce que veulent les gens même sans signal direct de sa part.
  Recherche via agent dédié (WebSearch), sources réelles uniquement (VPForums, VPUniverse,
  GitHub vpinball/vpinball, Reddit r/virtualpinball), respect strict de la consigne
  PROJECT-BRAIN §7 contre les statistiques inventées.
  1. **Scanner d'intégrité base Popper** (doublons, entrées orphelines, liens cassés) — citation :
     « I added over 500 games... they are all so inter dependent that i cant erase them »
     ([VPUniverse](https://vpuniverse.com/forums/topic/13380-oops-i-added-500-games-and-need-to-erase-them/)).
     Correspond directement au persona Gregg/itchigo ci-dessus (curateur qui ne peut plus tout
     superviser). **⚠️ Touche directement ADR-007** ("écriture SQLite Popper hors v1, à décider
     quand le terrain le demandera" — PROJECT-BRAIN §7 point 3). Ce signal terrain + le persona
     confirmé sont exactement le déclencheur qu'ADR-007 attendait explicitement pour être
     rouverte — **mais rouvrir une ADR est une décision produit qui revient à Maxime, pas un feu
     vert pour coder**. À présenter comme "conversation ADR-007 à rouvrir", pas comme un chantier
     prêt à lancer.
  2. **Validateur de mapping ROM/VPinMAME** (nom de fichier/registre incohérent alors que la ROM
     est possédée) — plusieurs threads convergents (VPForums #33699, #39779, VPUniverse #3675).
     100% local, aucun conflit ADR identifié.
  3. **Vérificateur de compatibilité de version du moteur VPX** — preuve indirecte forte : un
     outil tiers gratuit existe déjà pour un sous-problème ([JockeJarre/VPinballX.starter](https://github.com/JockeJarre/VPinballX.starter)),
     signe d'une douleur réelle. Détection locale seulement, jamais de téléchargement de binaire
     tiers (ADR-004).
  4. **Correcteur de liens backglass B2S** (mismatch nom de fichier/résolution) — VPUniverse
     #13772, #9368, GitHub vpinball/vpinball #1476.
  5. **Coffre-fort de sauvegarde NVRAM/high-scores** — valeur "assurance" plus que "urgence",
     techniquement trivial (backup/restore local versionné).
  6. **Audit des références médias/wheel** — une seule source (VPUniverse #5559), pas de
     corroboration croisée ; à traiter comme module complémentaire de l'idée #1, pas en
     autonome.
- disposition: à présenter à Maxime pour arbitrage produit (aucune n'est codée). Idée #1 nécessite
  explicitement de rouvrir la discussion ADR-007 avant tout, pas juste un feu vert Repair normal.

## 2026-08-05 · Reprise session — sync GitHub (push bloqué, action Maxime), décisions (a)/(b) en cours de clarification
- code:        transverse (process/infra, pas un finding)
- bac:         FIX (infra) + décision produit en cours
- contexte:    Reprise du dossier local (`Desktop/Pincab suite/...`), reconnecté en début de session.
  TRANSMISSION/PROJECT-BRAIN/FIELD-LOG lus depuis le disque (plus à jour que GitHub). Objectif de
  session : présenter à Maxime les 2 décisions ouvertes de la revue qualité du 04/08 avant tout code,
  proposer `license-tool init`, ne pas câbler l'Écran 2 sans confirmation explicite du jour.
- analyse:
  1. **Vérifié plutôt que supposé** : la note de clôture du 04/08 disait les 5 corrections + la
     consigne PM « pas encore réécrites sur le disque de Maxime » (pont déconnecté en session). En
     rouvrant les fichiers réels (mtimes, contenu — `IsContained` par segments, `.gitignore` *.pem,
     `LicenseVerifier` dégradé, consigne PM dans `PROJECT-BRAIN` §9), **tout y était déjà** — le pont
     a dû se reconnecter juste avant la fin de cette session-là, après l'écriture de la note. Rien à
     refaire.
  2. **Sync GitHub** : le dépôt distant (`waylo1/pincab-toolbox`, HEAD `24e7e0f`) n'avait aucun des
     chantiers du 04/08 nuit. 24 fichiers rapatriés individuellement depuis le disque de Maxime
     (fichier par fichier, pas une archive complète — une première tentative par tar a introduit du
     bruit de fin de ligne Windows/Linux sur tout le dépôt, abandonnée et recommencée proprement).
     Diff vérifié identique au `git status` du disque local. **SDK .NET installé dans ce sandbox
     cloud** (`apt-get install dotnet-sdk-8.0`, après un `apt-get update` pour rafraîchir les
     paquets). **128/128 tests Core et 101/101 tests Repair verts, Debug ET Release**, re-vérifiés
     sur l'exact jeu de fichiers avant tout commit. Commit fait (`bb6076a`). **Push refusé** par le
     proxy git du sandbox (« waylo1/pincab-toolbox n'est pas dans l'ensemble de dépôts autorisés de
     cette session ») — restriction d'environnement, pas un problème de code ou de droits GitHub.
     Commande exacte laissée à Maxime dans TRANSMISSION.md pour pousser depuis sa machine (son
     disque local a déjà les mêmes fichiers en working tree non commité).
  3. **Décisions (a)/(b) présentées à Maxime, pas encore tranchées.** (b) [`set_default_audio_device`
     rejeté à vie par `IsContained` car son Target est un GUID, pas un chemin] a été jugée pas claire
     par Maxime — reformulation en langage simple nécessaire avant de pouvoir trancher. (a) [scénario
     partiellement automatisable compté "réparable" sans montrer les étapes manuelles obligatoires,
     ADR-006] — Maxime a répondu par un principe produit plutôt qu'un choix parmi les 3 options
     proposées : « il faut un produit que les gens achèteraient sans survendre, mais il faut qu'ils
     en aient besoin ». Juste, mais pas encore une décision codable — à retraduire en option concrète
     et reconfirmer avant tout câblage (catégorie UI Repair, jamais touchée sans accord explicite).
  4. **`license-tool init` proposé à Maxime** (commande exacte dans `tools/PincabToolbox.LicenseTool/README.md`)
     pour générer sa vraie paire de clés — pas encore lancé, décision qui lui revient entièrement (la
     clé privée ne doit jamais transiter par une session cloud).
  5. **(b) reformulée en langage simple** (première réponse : « je ne comprends pas ») — comparaison
     au vigile qui ne vérifie que les gens restés dans le bâtiment. Maxime tranche : créer une
     exemption ciblée pour ce type d'action, même patron que l'exemption déjà faite pour
     `kill_zombie_pinup_display`.
  6. **(a) reformulée en application concrète de son principe** (« vendre sans survendre, mais qu'ils
     en aient vraiment besoin ») : garder l'item dans "réparable" (vraie valeur) + afficher les étapes
     manuelles en plus (rien de caché). Maxime confirme : « Oui, montrer les étapes manuelles ».
  7. **Les deux codées et vertes dans la foulée**, catégorie UI Repair/archi mais avec accord explicite
     obtenu juste avant, conforme à la règle. (a) : `RepairOffer.NotAutomatable` (déjà calculé côté
     moteur depuis le 03/08, jamais affiché) câblé sous l'agrégat "Repair pourrait corriger X/Y" dans
     `MainWindow.xaml`/`.xaml.cs` + clé `Loc.cs` FR/EN. (b) : exemption `ChangeKind.AudioDeviceDefault`
     dans `RepairEngine.IsContained`. 2 nouveaux tests de régression
     (`Test_Offer_PartialScenario_CountsAsFixable_AndListsItsManualSteps`,
     `Test_Preflight_AudioDeviceTarget_IsExemptFromPathContainment`) — **128/128 Core, 103/103 Repair,
     Debug ET Release.** `MainWindow.xaml` revérifié XML bien formé après édition ; le reste de l'App
     (WPF) relu manuellement ligne à ligne, non compilable dans ce sandbox — `build.cmd` de Maxime
     reste la seule vérification qui compte pour cette partie. `set_default_audio_device` reste
     volontairement hors du registre App (toujours "pas reliée à un Finding") : cette session la rend
     exécutable le jour où elle sera câblée, ne l'active pas.
  8. **2 commits faits dans ce sandbox cloud** (`bb6076a`, `685ada8`) mais **push refusé aux deux**
     par le proxy git de session (même cause qu'au point 2 ci-dessus). Commande consolidée (un seul
     commit, tout le travail de la session) laissée à Maxime dans TRANSMISSION.md.
- disposition: **décisions (a)/(b) tranchées, codées, vertes.** Reste à Maxime : `git push` depuis sa
  machine (commande donnée), `license-tool init` quand il le souhaite, et — seulement après ça et sur
  nouvelle confirmation explicite — le câblage de l'Écran 2 (bouton Apply réel), non entamé cette
  session.

## 2026-08-04 (nuit, quater) · Revue qualité pré-v1.0 — 5 angles, 5 agents indépendants, 5 corrections appliquées
- code:        transverse (audit qualité, pas un finding)
- bac:         FEATURE (process)
- contexte:    Suite de la consigne PM. Cinq agents indépendants (lecture seule, aucune coordination entre eux) ont audité en parallèle : (1) cohérence architecture/ADR, (2) qualité de code, (3) sécurité/frontières de confiance, (4) couverture de tests, (5) cohérence produit/UX vs. les promesses (ADR-006 notamment). Cible : tout le dépôt, avec une attention particulière au code de cette session (Licensing/, LicenseTool, extension du harnais démo) jamais revu par personne d'autre.
- analyse:     **Trouvailles réelles, pas du remplissage :**
  1. **[CRITIQUE, corrigé]** `LicenseVerifier.EmbeddedPublicKeyBase64` était un placeholder invalide (mauvaise longueur DER) — `new LicenseVerifier()`, exactement ce que l'App appellera, plantait à la construction au lieu de renvoyer poliment "Invalid". Trouvé indépendamment par 3 des 5 agents. **Corrigé** : la clé cassée dégrade maintenant vers "vérification non configurée" (jamais de licence valide, mais plus jamais de crash) ; 2 tests ajoutés qui verrouillent ce comportement de dégradation, y compris pour le futur (une fois la vraie clé collée, ces tests testent toujours la dégradation, pas la valeur placeholder).
  2. **[HAUT, corrigé]** `IsContained` (le filet ADR-005 contre une écriture qui sortirait du pincab) faisait un simple `StartsWith` texte : un dossier voisin (`C:\vpx` acceptait `C:\vpxtra\...`) ou un chemin avec `..` pouvait passer le contrôle. Trouvé indépendamment par 2 agents. **Corrigé** : comparaison par segments de chemin normalisés, `..` recollé avant comparaison. 2 tests de régression ajoutés. Reste non traité, volontairement (hors scope d'une correction "sûre") : la résolution de lien symbolique/jonction, qui demanderait de toucher l'abstraction `IFileSystem` elle-même — signalé, pas deviné.
  3. **[HAUT, corrigé]** `tools/PincabToolbox.LicenseTool` écrit la clé privée par défaut dans un chemin relatif (`license-private-key.pem`), donc potentiellement DANS le dépôt de travail — et `.gitignore` ne l'excluait pas malgré ce que dit le README de l'outil. **Corrigé** : `.gitignore` couvre maintenant `*.pem` et `license-private-key*`.
  4. **[MOYEN, corrigé]** Le message de correctif de `ROM_MISSING` (le Critical le plus fréquent — 8 occurrences sur le scan réel de Maxime lui-même) n'avait pas d'entrée dans `FrFixHints` de `Loc.cs` et retombait silencieusement en anglais pour un utilisateur FR. **Corrigé**, une ligne ajoutée, cohérente avec le style des entrées voisines (générique, sans re-décliner le nom de fichier déjà donné par le texte du finding).
  5. **[BAS, corrigé]** `about.roadmap` (FR+EN) appelait encore le futur payant "Repair (Pro)" alors que toute autre source de vérité (ADR-002, DESIGN-Repair-v1.md, UX-COPY-Repair.md, le tag `repair.tag`) dit juste "Repair". **Corrigé**, "(Pro)" retiré des deux langues.
  6. **[HAUT, NON corrigé — décision produit, pas une correction sûre]** Un scénario de réparation multi-étapes partiellement automatisable (ex. `MIGRATION_32_TO_64_INCOMPLETE` : 1 étape sur 3 automatique, 2 `manualOnly`) est compté comme "réparable" dans le résumé gratuit (`FixableCount`) exactement comme un item 100 % automatique — les deux étapes manuelles obligatoires ne sont affichées NULLE PART dans l'App alors que `RepairOffer.NotAutomatable`/`RepairPlanItem.Missing` existent précisément pour ça (ADR-006). C'est le finding le plus sérieux de toute la revue : ça touche directement la promesse anti-survente et l'affichage Écran 1 déjà câblé. **Volontairement pas touché** — c'est de l'UI Repair, exactement la catégorie qu'on ne modifie jamais sans redemander à Maxime.
  7. **[HAUT, NON corrigé — bug latent, dormant tant qu'Écran 2 n'existe pas]** `SetDefaultAudioDeviceAction` produit un `Target` qui est un GUID de périphérique, pas un chemin de fichier — `IsContained` (même corrigé) le rejettera TOUJOURS puisqu'il compare des segments de chemin. Concrètement : cette action ne pourra jamais s'appliquer pour de vrai tant que ce n'est pas réglé, même une fois Écran 2 câblé et licence en place. Signalé, pas corrigé : implique une vraie décision d'archi (exempter `ChangeKind.AudioDeviceDefault` du contrôle de chemin, ou changer la forme de `Target`), pas une correction mécanique.
  8. **[MOYEN, non corrigé, nuance importante]** `VpsDatabase.cs` fait un vrai appel réseau (`raw.githubusercontent.com`) à chaque scan pour la Virtual Pinball Spreadsheet — un agent l'a d'abord lu comme une violation du discours "zéro télémétrie", mais **ADR-004 §3 nomme explicitement VPS comme source de données autorisée**, donc ce n'est PAS un bug caché. Le vrai point ouvert : est-ce assez visible pour l'utilisateur (aucun toggle de confirmation trouvé) ? Question produit à trancher avec Maxime, pas une anomalie.
  9. **Backlog, pas corrigé** : duplication de la logique "dernier segment de chemin" dans 5 fichiers différents (déjà documentée en commentaire comme "3ᵉ occurrence du piège" — c'est en fait la 5ᵉ) ; incohérence de gestion d'erreur try/catch entre les 5 `IRepairAction` ; 8 trous de couverture de tests identifiés (le plus notable : `LicenseVerifier`'s crypto-exception catch jamais réellement déclenché par un test, `ScanReport.Rolled()` jamais testé exactement à `count == 5`, `SCANNER_ERROR` jamais testé).
  10. **Non vérifiable dans ce sandbox** : ADR-001, ADR-003, ADR-008 absentes du mirror local (jamais mises en scène cette session — le pont machine s'est déconnecté avant d'avoir pu les charger).
- disposition: **Build+tests vérifiés verts après CHAQUE correction** : Debug ET Release, 101/101 Repair (97 + 4 nouveaux tests de régression), 128/128 Core (inchangé), harnais démo toujours vert. Fichiers livrés à Maxime via SendUserFile (voir liste dans TRANSMISSION) — **pas encore réécrits sur son disque**, le pont `mcp__remote-devices__*` s'est déconnecté en cours de session (`get_device_info` → "not connected to the bridge"), toujours down à la fin de cette entrée. Prochaine étape : dès reconnexion, `device_stage_files` frais puis `device_commit_files` de tous les fichiers listés, ET reporter la consigne PM dans `PROJECT-BRAIN` (toujours pas fait, cf. entrée précédente).

## 2026-08-04 (nuit, ter) · Consigne PM permanente ajoutée · revue qualité pré-v1.0 démarrée
- code:        transverse (process/gouvernance, pas un finding)
- bac:         FEATURE (process)
- contexte:    Maxime a discuté avec GPT de plusieurs consignes permanentes (revue CTO en fin de tâche, revue qualité pré-v1.0, casquette Product Manager sur Repair, vision « Mode Appliance »). Retour donné avant d'agir : d'accord sur l'esprit, réserves concrètes sur « corriger sans demander » (contredit la règle UI Repair/HANDOFF) et sur les notes /10 (peu de signal, on note son propre travail). Maxime tranche : adopte la consigne PM (texte donné mot pour mot) et lance la revue qualité pré-v1.0 avant d'aller plus loin.
- analyse:     Consigne PM ajoutée en tête de `TRANSMISSION.md` (section dédiée, lue à chaque session). **Pas encore reportée dans `PROJECT-BRAIN`** — le pont vers la machine de Maxime (`mcp__remote-devices__*`) s'est déconnecté en cours de session (`get_device_info` renvoie « not connected to the bridge »). `PROJECT-BRAIN.md` lui-même n'a jamais été chargé dans ce sandbox (pas trouvé dans les fichiers déjà mis en scène) — recherche à refaire une fois reconnecté.
- disposition: **revue qualité pré-v1.0 démarrée dans cette même session**, sur la copie locale déjà en mémoire (Core, Repair, App, tests, ADR, docs) — n'a pas besoin du pont machine, seule la restitution finale sur disque de Maxime en aura besoin. Prochaine étape après reconnexion : reporter la consigne PM dans `PROJECT-BRAIN` (canonique), pas seulement TRANSMISSION.

## 2026-08-04 (nuit, suite) · Phase 1 (licence) + Phase 2 (harnais démo) codées et vertes
- code:        transverse (infrastructure de vente)
- bac:         FEATURE
- contexte:    Maxime délègue le choix cryptographique (« je ne suis pas competent... à toi de voir ») et valide de démarrer les deux phases tout de suite.
- analyse:
  1. **Choix fait : ECDSA P-256**, pas HMAC. Raison : la clé embarquée dans l'exe (donnée à TOUT le monde) est la clé PUBLIQUE — elle peut vérifier une signature mais pas en fabriquer une. Quelqu'un qui extrait le binaire n'obtient rien qui lui permette de générer ses propres licences valides, contrairement à un secret HMAC partagé. Coût de complexité quasi nul : `System.Security.Cryptography.ECDsa` est dans le BCL .NET, zéro dépendance ajoutée.
  2. **Découverte en cours de route : le harnais de test démo (`tools/PincabToolbox.Repair.Demo`) existe DÉJÀ**, avec 5 scénarios contre le vrai moteur Repair (vrai `RealFileSystem`, vrai pack). C'est exactement la Phase 2 demandée par Maxime le 04/08 soir (« 2 tu le fais avec le mode demo deja présent ») — pas à recréer, juste à étendre. Il manquait `quarantine_orphaned_media` (ajouté, scénario 6, filesystem réel) et un test de fumée pour `set_default_audio_device` (ajouté, scénario 7, **lecture seule volontairement** — ne change jamais le périphérique audio réel de la machine qui l'exécute, seul un test manuel explicite testera le vrai `SetDefaultPlaybackDevice`). `kill_zombie_pinup_display` pas ajouté au harnais : ses risques réels (bug de séparateur de chemin) sont déjà couverts par les tests unitaires avec fake, contrairement à l'audio (COM non documenté) ou au Zone.Identifier (spécificité NTFS) qui ne peuvent être vérifiés que hors sandbox Linux.
  3. **Nouveau module `src/PincabToolbox.Repair/Licensing/`** : `LicensePayload` (email, émission, fin de fenêtre MAJ — **la licence elle-même ne périme jamais**, ADR-002), `LicenseCodec` (JSON + base64url, `System.Text.Json` déjà utilisé ailleurs dans Repair), `LicenseSigner` (offline uniquement), `LicenseVerifier` (clé publique embarquée en constante, zéro appel réseau). Format de clé façon JWT à un point (`base64url(payload).base64url(signature)`), pas une bibliothèque JWT — fait main, cohérent avec la règle zéro dépendance.
  4. **Nouvel outil `tools/PincabToolbox.LicenseTool`** (console, buildable dans CE sandbox car pas WPF) : `init` (génère la paire de clés, UNE SEULE FOIS, clé privée jamais dans le dépôt), `issue` (signe une licence pour un client après achat), `verify` (contrôle une clé sans lancer l'App). Testé de bout en bout dans le sandbox avec une paire de clés JETABLE (générée puis supprimée après test — **la vraie paire de production reste à générer par Maxime lui-même sur sa machine**, cette session n'en a jamais eu et n'en garde aucune trace).
  5. **9 nouveaux tests** (`LicenseTests.cs`) : aller-retour valide, tolérance aux espaces de copier-coller, payload trafiqué → invalide, signature trafiquée → invalide, mauvaise clé publique → invalide, chaînes n'importe quoi → jamais d'exception, null/vide → invalide, fenêtre de MAJ expirée → licence quand même valide (ADR-002 : ne pas confondre expiration des MAJ et expiration de la licence).
  6. **Tout vert, Debug ET Release, vérifié dans ce sandbox** : 128/128 Core (inchangé), 97/97 Repair (89 existants + 8 nouveaux `LicenseTests`, confirmé par la sortie du test runner), harnais démo (`repair-demo`) exécuté avec succès dans les deux configurations — scénario 1 (DLL) et 7 (audio) s'annoncent correctement non reproductibles hors Windows, scénario 6 (nouveau, quarantaine) passe en entier sur vrai système de fichiers Linux.
- disposition: **Écran 2 (bouton Apply réel dans l'App) reste NON câblé**, conforme HANDOFF — reconfirmation explicite à demander à Maxime avant de le faire, ce n'est pas fait cette entrée. Prochaine étape côté Maxime : lancer `license-tool init` sur sa machine pour générer sa vraie paire de clés, remplacer `EmbeddedPublicKeyBase64` (encore un `PLACEHOLDER` volontairement invalide dans le code livré) puis rebuilder ; lancer `dotnet run --project tools/PincabToolbox.Repair.Demo` sur son PC Windows pour valider pour de vrai les scénarios 1 et 7.

## 2026-08-04 (nuit) · Maxime demande un vrai bouton « Apply » — vérif ADR avant de coder, POPPER_NOT_REGISTERED tué net
- code:        transverse (monétisation Repair, pas un finding)
- bac:         FEATURE, décision produit
- contexte:    Suite directe de l'entrée précédente. Maxime : « le logiciel a un bouton qui deplace ou fait l'action, on a assez de choses gratuites faut vendre maintenant. » Avant de proposer quoi que ce soit, vérification des 4 ADR liées (002, 004, 007, 009) plutôt que de partir sur ma propre proposition non vérifiée de la session précédente (`POPPER_NOT_REGISTERED`).
- analyse:
  1. **`POPPER_NOT_REGISTERED` est mort, et c'était déjà tranché avant même que je le propose.** ADR-007 (25/07) : écrire dans `PUPDatabase.db` (SQLite) à la main sans bibliothèque = risque de corrompre toute la bibliothèque Popper de l'utilisateur (pages B-tree, journal, compteur de changement). Reste `ManualOnly` en v1, **verrouillé par un test** (`Test_ShippedPack_PopperRegistrationIsManualInV1`) qui casse la suite si quelqu'un ajoute une règle Popper sans re-trancher l'ADR. Bien fait de vérifier avant de coder : ça m'aurait fait perdre du temps et recréer un risque déjà écarté sciemment.
  2. **Le modèle de vente est déjà entièrement décidé sur le papier (ADR-002/009), rien n'est codé.** Licence perpétuelle qui débloque la colonne « Réparer » dans un seul exe (le Scanner reste gratuit et complet à côté) ; vérification 100 % locale (signature hors ligne liée à l'email, **aucun appel réseau obligatoire**) ; encaissement via Lemon Squeezy en Merchant of Record (gère la TVA mondiale, ne bloque pas le lancement du Scanner gratuit, Phase 3 assumée). Donc : le plan existe, **le code de vérification de licence n'existe pas encore** — c'est le vrai bloquant avant tout bouton Apply, pas un manque d'action Repair.
  3. **ADR-004 confirme que la règle « on vérifie, on ne fournit jamais » n'est PAS scopée au Scanner** — c'est un filtre projet entier, opposable à toute proposition quelle que soit sa valeur commerciale. Ça clôt définitivement l'idée du 04/08 (soir) de « jouer avec les limites du légal » côté Repair.
  4. **Relecture des 4 rapports HTML de Maxime avec un œil correctif plutôt que diagnostic** : le rapport 16:30 (191 médias orphelins, PinUP Popper réel) est un vrai match pour `quarantine_orphaned_media`, déjà codée et testée (89/89). Le rapport 16:33 contient bien un DLL bloqué (`version.dll`) mais dans un dossier de crack logiciel piraté sans rapport avec le pincab — **ne PAS s'en servir comme démo/preuve pour `unblock_file`**, mauvais exemple à montrer publiquement même si le mécanisme marcherait techniquement dessus.
- disposition: **répondu à Maxime avec un plan en 3 phases** : (1) module de vérification de licence locale (signature, .NET natif, zéro dépendance — conforme à la règle du projet), (2) harnais de test console contre `DemoData` pour valider les actions à effet d'écriture existantes hors UI (déjà autorisé le 04/08 soir : « 2 tu le fais avec le mode demo deja présent »), (3) câblage réel de l'Écran 2 (bouton Apply) une fois (1) et (2) faits — **reconfirmation explicite requise avant (3)**, conforme à la règle HANDOFF. Question posée à Maxime : choix de l'algorithme de signature pour (1) (ECDSA clé publique embarquée vs. secret partagé HMAC), décision structurante pour tous les futurs clients donc pas prise seule.

## 2026-08-04 (soir, suite) · Angle mort trouvé par Maxime lui-même : aucune des 5 actions Repair ne couvre son propre scan
- code:        transverse (stratégie produit Repair, pas un finding)
- bac:         FEATURE / question de fond, soulevée par Maxime
- contexte:    En recevant les instructions manuelles pour ses 8 ROM manquantes, Maxime pousse sur le fond : « je vais pas réparer les critical justement le but de repair c'est de réparer à la place de l'utilisateurs, sinon le scan fait le travail et je gagne pas d'argent ». Question légitime sur le modèle économique, pas une simple demande de fix.
- analyse:     **Vérifié plutôt que rassuré à vide.** Deux faits distincts, à ne pas mélanger :
  1. **`ROM_MISSING` (ses 8 criticals) ne sera JAMAIS traité par Repair, licence ou pas — décision déjà actée (ADR-004, interdiction permanente de fournir/télécharger des ROMs).** Ce n'est pas un manque à combler, c'est une limite volontaire et légale (ROMs protégées). Donc pour cette catégorie précise, il n'y a rien à « attendre » de Repair, ni maintenant ni plus tard.
  2. **`B2S_MISSING` (ses 100 warnings) n'a AUCUNE règle dans `knowledge/pack-2026.08.json` non plus** — vérifié : les 4 règles du pack couvrent `BLOCKED_DLL`, `ROM_UNZIPPED`, `PINUP_DISPLAY_ZOMBIE`, `ORPHANED_MEDIA_FILE`. Même famille de raison probable (fichiers backglass protégés par leurs créateurs) même si jamais tranché explicitement — à confirmer avec Maxime si le sujet revient, pas à décider seul ici.
  3. **Conséquence factuelle plus large, à dire clairement plutôt qu'à cacher** : sur SON scan réel, **aucun des 5 codes remontés (`ROM_MISSING`, `B2S_MISSING`, `B2S_ORPHAN`, `BITNESS_INVENTORY`, `COMPAT_*`) ne correspond à une des 5 actions Repair existantes** (`unblock_file`, `restore_rom_archive`, `kill_zombie_pinup_display`, `quarantine_orphaned_media`, `set_default_audio_device` — non relié). Sa cab, telle quelle, n'a par hasard aucun cas que Repair sait traiter aujourd'hui — pas un bug, juste une coïncidence de ce qui est cassé chez lui vs. ce que Repair couvre.
  4. **Le modèle n'est pas contredit, mais il ne se prouve pas sur ce scan-là.** ADR-006 dit déjà que Repair doit annoncer honnêtement ce qu'il NE fera jamais (`NotAutomatable`, jamais caché) — c'est exactement le cas ici : sur cette install, `RepairOffer` afficherait `FixableCount = 0`. Le produit se comporte comme prévu (rien à vendre s'il n'y a rien de vendable), mais ça veut dire que la propre cab de Maxime n'est pas un bon terrain de démo pour Repair en l'état.
- disposition: **répondu à Maxime avec les faits ci-dessus, sans habiller.** Deux pistes proposées, décision pas prise ici : (a) élargir le catalogue d'actions Repair vers des problèmes plus fréquents mais toujours sans redistribuer de fichier protégé (dans l'esprit d'`unblock_file`/`restore_rom_archive`, qui ne fournissent jamais de contenu, juste réparent une manipulation locale) ; (b) tester le mécanisme Repair avec un cas synthétique (ex. décompresser exprès un dossier ROM déjà présent pour déclencher `ROM_UNZIPPED`, ou bloquer volontairement un DLL) puisque sa cab actuelle n'a aucun des 5 cas. **À trancher avec Maxime, pas à deviner.**

## 2026-08-04 (soir) · Maxime a testé le scanner fraîchement buildé sur sa vraie cab — 4 rapports HTML
- code:        `ROM_MISSING` (8×, confirmé) · `B2S_MISSING` (100×, nouveau volume observé) · `B2S_ORPHAN` (105×) · `BLOCKED_DLL` (1×, hors périmètre pincab)
- bac:         FIX (vérification cab réel — le test demandé après le build du 04/08)
- contexte:    Maxime a copié `publish/` sur clé USB, lancé le scanner sur sa cab réelle, et ramené 4 exports HTML (16:29 à 16:33).
- analyse:     **Un seul des 4 rapports est le vrai scan de la cab** (16:29 — 225 tables, score 0/100·F, 8 critical · 100 warning · 276 info · 225 ok). Les 3 autres (16:30 score 92, 16:31 score 100, 16:33 score 87) ont chacun l'avertissement « aucun dossier de tables trouvé sous la racine choisie » — signe qu'un mauvais dossier a été sélectionné (test du bouton Parcourir, probablement), sans rapport avec le vrai résultat. Le rapport 16:33 a même remonté un `BLOCKED_DLL` sur un `version.dll` dans un dossier `[ Torrent911.com ] IObit Driver Booster Pro ... Crack` — clairement hors du périmètre pincab (pas un problème du scanner, juste un mauvais dossier scanné par erreur).
  **Sur le vrai scan (16:29) :**
  - **8 `ROM_MISSING` critical, tous avec un nom de ROM précis** (bloodmach.zip, hpgof.zip, jurassic.zip, leprechaun.zip, mmunsters.zip, STLE.zip, goonies.zip, willywonka.zip). **Recoupe exactement la liste que Maxime avait lui-même identifiée dans sa réponse à Gregg le 03/08** (Blood Machines, Jurassic Park, Leprechaun, hpgf…) — confirme sur le terrain que ce sont de vrais hacks ROM nécessitant une ROM spécifique, pas le mécanisme de FP des commentaires VBScript corrigé le 03/08. Cohérence croisée entre deux sessions, bon signal.
  - **100 `B2S_MISSING` (Avertissement)** — regroupés par `Rolled()` dans le HTML, liste complète non visible sans export texte/JSON. Premier volume aussi élevé observé sur ce code, sur SA PROPRE collection — pas encore vu remonté par un tiers avec ce volume. À garder à l'œil si ça revient dans un futur retour terrain (pourrait justifier un candidat §2 futur : suggestion de tri/filtre B2S manquant, mais **un seul signal pour l'instant, pas deux** — ne pas coder).
  - **105 `B2S_ORPHAN` (Info)** — fichiers backglass sans table correspondante, probablement des restes de tables supprimées/renommées (même famille que `ORPHANED_MEDIA_FILE`, dont l'action `quarantine_orphaned_media` existe déjà côté Repair mais ne couvre que POPMedia/PUPVideos, pas `.directb2s` — hors périmètre de cette action, noté pour référence future seulement).
  - Aucune DLL bloquée dans l'install pincab elle-même — `BlockedFileScanner`/`UnblockFileAction` n'ont rien à faire ici sur ce cab.
- disposition: instructions étape par étape données à Maxime (placer les 8 ROMs dans `VPinMAME\roms`, zippées, nom exact ; relancer un scan sur la vraie racine pour confirmer ; export texte/JSON si besoin de la liste complète des 100 B2S_MISSING — optionnel, pas bloquant). **Confirme que le build du 04/08 (avec le fix Knowledge.cs) tourne correctement sur une vraie cab, de bout en bout.** Aucune ROM fournie ni cherchée (ADR-004).

## 2026-08-04 (suite) · UI Repair câblée — Écran 1 seulement (offre gratuite), sur autorisation explicite
- code:        transverse (App WPF + moteur Repair, aucun nouveau finding)
- bac:         FIX (chantier de code)
- contexte:    Maxime : « 1 ok et 2 on attend, fais 3 et 4 » — le point 3 de la liste ordonnée proposée était « décider/câbler l'UI Repair ». C'est la reconfirmation explicite exigée par la règle de session (« ne jamais câbler Repair sans redemander ») et par la décision HANDOFF du 27/07 elle-même.
- analyse:     **Périmètre volontairement réduit à l'Écran 1** (UX-COPY-Repair.md : « Réparation disponible, avant achat »), pas les Écrans 2–4 (confirmation/préflight/récupération = le chemin d'ÉCRITURE). Deux raisons : (a) aucune infrastructure de licence n'existe encore dans l'App (ADR-009/Lemon Squeezy non câblé) — un bouton « Réparer » cliquable qui ne ferait rien violerait la règle de copie du produit lui-même (« jamais rassurer/promettre à vide ») ; (b) le Blocage #2 (valider les 3 actions à effet d'écriture sur une vraie cab) n'a pas encore eu lieu — Maxime part justement tester le scanner et ramènera Repair après.
  **Ce qui a été câblé, concrètement :**
  - `RepairOfferBuilder.cs` (nouveau, App) — point de composition qui construit un `RepairOffer` depuis un `ScanReport` en appelant `IRepairEngine.Plan(..., licensed:false)` **uniquement**. `Preflight`/`Apply`/`Undo` (le chemin d'écriture) ne sont appelés nulle part dans l'App — seul le côté pur/lecture-seule du moteur est branché. Toute exception (pack corrompu, sonde COM, etc.) est avalée et renvoie `null` : Repair est un bonus, une panne dedans ne doit jamais casser le scan gratuit.
  - Registre d'actions réelles construit avec 4 des 5 actions existantes (`UnblockFileAction`, `RestoreRomArchiveAction`, `QuarantineOrphanedMediaAction`, `KillZombiePinUpDisplayAction`) — `SetDefaultAudioDeviceAction` volontairement exclue, cohérent avec son propre commentaire d'en-tête (« pas encore relié à un Finding ») et confirmé : aucune règle du pack ne référence `set_default_audio_device`.
  - `PincabToolbox.App.csproj` — ajout de la référence de projet vers `PincabToolbox.Repair` (elle n'existait pas du tout avant : l'App ne connaissait même pas l'assembly Repair) + inclusion de `knowledge/pack-2026.08.json` en contenu copié (comme `profiles/vpx-popper.json`).
  - `MainWindow.xaml` / `.xaml.cs` — une ligne d'agrégat sous les puces de sévérité (« Repair pourrait corriger X problème(s) sur Y — bientôt disponible »), et le tag `DetailRepairTag` qui existait déjà dans le panneau de détail mais était **purement cosmétique** (`Knowledge.IsAutoFixable(code)`, une liste statique, jamais reliée au moteur réel) est maintenant piloté par le plan calculé : coche « réparable », coche « sauvegarde » et coche « réversible » seulement si c'est vrai pour CE code (une action non-réversible comme le kill de process ne revendique jamais la coche réversible), durée en bucket. Toutes les coches viennent de `RepairPlanItem.Summary`, jamais écrites en dur — même principe ADR-006 que le moteur applique déjà en interne.
  - `Loc.cs` — nouvelles clés FR/EN pour les coches et le libellé verrou, repris mot pour mot de UX-COPY-Repair.md (Écran 1, section « sans licence »).
  - **Limite connue, non bloquante** : `ScanReport.Rolled()` collapse les findings répétitifs (≥5 même code) en une ligne de groupe au code `GROUPED` — cette ligne collapsée ne matchera jamais une entrée de `RepairOffer.ByCode` (qui indexe par vrai code de finding), donc le tag détail ne s'affiche pas sur une ligne groupée même si le code sous-jacent est réparable. Dégrade proprement (n'affiche rien de faux, juste moins) — cohérent avec la règle ADR-005 « un pack plus récent annonce moins, jamais plus ».
- disposition: **livré, vérifié différemment de d'habitude.** Ce sandbox cloud n'avait jamais eu de SDK .NET jusqu'ici (mentionné comme absent dans l'entrée du 04/08 plus bas) — installé cette fois (`apt-get install dotnet-sdk-8.0`, réseau nuget.org bloqué mais `NuGet.Config` du dépôt clarifie déjà les sources donc la restauration locale marche). Résultat : **`PincabToolbox.Core` et `PincabToolbox.Repair` compilent réellement ici (0 erreur, 0 warning)**, **128/128 tests Core et 89/89 tests Repair verts en Debug ET Release** (aucune régression — ni l'un ni l'autre projet n'a été modifié, seule l'App les référence maintenant), et le fichier le plus risqué du changement (`RepairOfferBuilder.cs`, LINQ générique dont `GroupBy`/`Max` sur `DurationBucket`) a été compilé isolément dans un mini-projet jetable contre les deux mêmes DLL — 0 erreur. **Seule chose non vérifiable ici, comme d'habitude** : `PincabToolbox.App` (WPF, `net8.0-windows`) exige le pack `Microsoft.WindowsDesktop.App.Ref`, disponible seulement via NuGet — bloqué par le même pare-feu réseau que d'habitude dans ce sandbox. Relecture manuelle ligne à ligne faite (signatures d'API, nullabilité, XML bien formé du XAML vérifié par un parseur) mais **le build Windows réel (CI `build-windows` ou `build.cmd` de Maxime) reste la seule vérification qui compte pour cette partie**, cohérent avec §10 de DESIGN-Repair-v1.md.

## 2026-08-04 · Premier `build.cmd` réel sur la machine Windows de Maxime — App confirmée compilable, Core tests bloqués par Windows (pas par le code)
- code:        aucun (résultat de build, pas un finding)
- bac:         FIX (vérification cab réel / build)
- contexte:    Maxime a lancé `build.cmd` en entier pour la première fois depuis le 30/07. Résultat mitigé mais net.
- analyse:
  - ✅ **`PincabToolbox.App` (WPF) compile et se publie pour de vrai en Release** (`dotnet publish ... -r win-x64`, succès). C'est la première compilation réelle (pas un parse Roslyn) des fichiers modifiés le 03/08 (`MainWindow.xaml.cs`, `Knowledge.cs`, `Localization/Loc.cs`) **et** de l'entrée `VPT_LEGACY_PRESENT` ajoutée aujourd'hui dans `Knowledge.cs`. Le plus gros doute du blocage #1 (« ça compile vraiment ? ») est levé pour l'App.
  - ✅ **Repair : 89/89 tests verts** en Release, aucune régression.
  - 🔴 **Core tests n'ont pas pu s'exécuter** — pas un échec de test, un blocage avant même le lancement : `System.IO.FileLoadException` sur `PincabToolbox.Core.Tests.dll`, message « Une stratégie de contrôle d'application a bloqué ce fichier » (HRESULT `0x800711C7`). C'est Windows (Smart App Control / Windows Defender Application Control ou Mark-of-the-Web hérité du dossier `-src` extrait d'un zip téléchargé) qui refuse de charger l'assembly compilé, **pas un bug dans le code**. Ironie notée : c'est exactement la même famille de panne que `BlockedFileScanner`/`UnblockFileAction` détectent et réparent chez l'utilisateur — sauf que là c'est le propre outil de build de Maxime qui en est victime.
  - **Hypothèse à tester en premier** (la plus probable et la plus simple) : le dossier `pincab-toolbox-v0.1.1-alpha-src` porte dans son nom la trace d'une extraction depuis un zip téléchargé — si Windows a marqué l'arborescence source (Zone.Identifier / Mark-of-the-Web), certains binaires compilés dedans en héritent selon la configuration de sécurité. À vérifier avec PowerShell : `Get-ChildItem -Path "C:\Users\User\Desktop\Pincab suite\pincab-toolbox-v0.1.1-alpha-src" -Recurse | Unblock-File`, puis relancer `dotnet run --project tests\PincabToolbox.Core.Tests -c Release` seul. Si ça ne suffit pas : vérifier Windows Sécurité → Contrôle des applications et du navigateur → « Contrôle d'application intelligent » (Smart App Control), et l'historique de protection pour voir ce qui a été bloqué exactement.
- disposition: **build #1 pas encore vert dans son ensemble** — App et Repair confirmés, Core toujours à vérifier une fois le blocage Windows levé côté Maxime. Rien à republier tant que les 128 tests Core ne sont pas passés au moins une fois sur cette machine.

## 2026-08-04 (suite) · `Unblock-File` récursif testé — hypothèse Mark-of-the-Web INFIRMÉE, même blocage à l'identique
- code:        aucun
- bac:         FIX (dépannage build)
- contexte:    Maxime a lancé `Get-ChildItem ... -Recurse | Unblock-File` sur toute l'arborescence source, puis relancé `dotnet run --project tests\PincabToolbox.Core.Tests -c Release` seul (capture d'écran PowerShell).
- analyse:     **Même erreur, mot pour mot, même HRESULT (`0x800711C7`), sur le même fichier.** Hypothèse Mark-of-the-Web/Zone.Identifier hérité du zip source **infirmée** — encore un exemple où vérifier plutôt que supposer évite de s'accrocher à une fausse piste. Deux éléments resserrent le diagnostic : (1) `PincabToolbox.Repair.Tests.dll` s'est chargé et a tourné sans problème depuis le **même dossier**, donc ce n'est pas un blocage par emplacement (Desktop) ni par le dossier source en bloc ; (2) le blocage vise spécifiquement l'assembly `PincabToolbox.Core.Tests.dll`. Ça pointe vers **Windows Defender Application Control / Smart App Control** (fonctionnalité Windows 11, souvent activée par défaut sur une install récente) plutôt qu'une simple Mark-of-the-Web — ou, moins probable sur une machine perso, une politique AppLocker/EDR ciblée. Reste à confirmer avec l'historique de protection Windows, qui nomme précisément ce qui a été bloqué (signature/heuristique vs. politique de contrôle d'application).
- disposition: **à faire côté Maxime** : (1) Windows Sécurité → Contrôle des applications et du navigateur → statut du « Contrôle d'application intelligent » (On/Off/Évaluation) ; (2) Windows Sécurité → Protection antivirus et contre les menaces → Historique de protection → chercher l'entrée liée à `PincabToolbox.Core.Tests.dll` et lire le nom exact de la détection. Sans ce nom, deviner la cause reviendrait à re-coder à l'aveugle — même règle que pour Freezy plus haut.

## 2026-08-04 (suite) · Cause confirmée : Smart App Control (WDAC), pas un antivirus classique — et un trou trouvé en vérifiant la CI
- code:        aucun
- bac:         FIX (dépannage build) + garde-fou process
- contexte:    Journal `Microsoft-Windows-CodeIntegrity/Operational` lu directement (events 3033/3077/3118) plutôt que deviné.
- analyse:
  1. **Cause confirmée, pas supposée** : la politique Code Integrity `VerifiedAndReputableDesktop` (Policy ID `0283ac0f-fff1-49ae-ada1-8a933130cad6`) — c'est le nom interne du **Contrôle d'application intelligent (Smart App Control)**, une fonctionnalité Windows 11 — bloque `PincabToolbox.Core.Tests.exe/.dll` pour « non-conformité au niveau de signature Enterprise ». Ce n'est pas un antivirus qui détecte un virus : c'est une politique de réputation qui n'a jamais vu ce binaire précis (fraîchement compilé, non signé) et refuse de le charger, bloqué de façon stable sur 2 tentatives distinctes (10:32 et 10:37). `Repair.Tests.exe` n'a **pas** été bloqué le même run — signature comportementale ou hash différents, cause exacte de l'écart non élucidée, sans intérêt pratique pour la suite.
  2. **En cherchant une voie de vérification qui contourne Smart App Control (la CI GitHub tourne sous Linux, insensible à ce blocage), j'ai vérifié le fichier réel `.github/workflows/build.yml` plutôt que de croire la note du 03/08** (« la CI GitHub testait déjà les deux [Core et Repair] », TRANSMISSION MAJ 03/08 soir). **C'est faux** : le workflow ne lance que `dotnet run --project tests/PincabToolbox.Core.Tests`, aucune étape Repair. Encore un cas où une note affirmait un fait non vérifié — comme l'alerte KPI#1, sauf que celle-ci allait dans le sens inverse (sous-couverture non détectée plutôt que fix non détecté). Corrigé : étape `Run Repair tests` ajoutée au job `test-linux`, même commande que `build.cmd`.
- disposition: **Décision pour Maxime, pas pour moi** — désactiver Smart App Control est **irréversible sans réinstallation complète de Windows** (documenté par Microsoft : Off ne peut plus repasser à On sans reset). Alternative sans toucher à la sécurité de la machine : si ce dépôt a déjà un remote GitHub, un `git push` déclencherait la CI corrigée (Core **et** maintenant Repair) sur Linux, donnant un vrai résultat vert/rouge sans dépendre de Smart App Control. **Question posée à Maxime** : le dépôt est-il déjà sur GitHub avec un remote configuré ?

## 2026-08-04 (suite) · Dépôt git créé pour de vrai — le code source n'avait JAMAIS été poussé
- code:        aucun
- bac:         FIX (infra) + découverte
- contexte:    Maxime a donné l'URL `https://github.com/waylo1/pincab-toolbox`. `git status` dans le dossier de travail renvoyait `fatal: not a git repository` — confirmé : ce dossier n'a jamais été suivi par git.
- analyse:     `git fetch` a révélé que le dépôt GitHub existait mais ne contenait **qu'un commit initial avec un `README.md` placeholder** (3 objets, 862 octets) — les tags `v0.1.0-alpha` et `v0.1.1-alpha` pointent dessus. **Le code source n'a donc jamais été versionné nulle part avant aujourd'hui** : les deux releases publiées (65 téléchargements pour la v0.1.1-alpha) ont dû être fabriquées et distribuées sans dépôt git faisant foi. Ça explique en partie pourquoi des affirmations comme « la CI GitHub testait déjà les deux [Core et Repair] » (03/08) ont pu s'écrire sans jamais être vérifiées : il n'y avait tout simplement rien à tester côté CI, aucun push n'avait jamais eu lieu.
  `.gitignore` déjà correct (exclut `bin/`, `obj/`, `publish/`, `*.zip`) — vérifié dans `git status` avant le premier commit : aucun binaire, aucun `.exe` de 146 Mo, rien d'inattendu dans les 150 fichiers indexés.
  `git init` + `git remote add origin` + commit racine (150 fichiers, 19441 lignes) + `git pull --allow-unrelated-histories` (conflit `README.md` résolu en gardant la version du projet) + `git push -u origin main` : **poussé avec succès**, `main` sur GitHub passe de `964cc15` (le placeholder) à `70fc4e2`.
- disposition: **le dépôt GitHub a maintenant le vrai code, pour la première fois.** Le push déclenche la CI corrigée (Core + Repair, Linux) — résultat à confirmer par Maxime dans l'onglet Actions. À partir de maintenant, chaque session devrait pousser ses commits plutôt que de ne modifier que la copie locale sur le Bureau — ça aurait évité une partie de la confusion documentée plus haut sur l'état réel du dépôt.

## 2026-08-04 (clôture) · BLOCAGE #1 LEVÉ — premier build entièrement vert depuis le 30/07, via CI
- code:        aucun
- bac:         FIX (vérification build) — clôture de chantier
- contexte:    Après le premier `git push` (commit `70fc4e2`), la CI a tourné avec l'ancien `build.yml` (jamais modifié sur le disque de Maxime malgré l'instruction donnée plus tôt dans la session — vérifié via la date de modification du fichier, restée à sa valeur d'origine) : `test-linux` vert mais **sans l'étape Repair** (jamais ajoutée réellement), et `build-windows` en échec sur les mêmes erreurs NU1101 que le run local (le workflow CI n'avait pas non plus le fallback `RestoreSources`).
- analyse:     Comme `.github/workflows/*.yml` est protégé en écriture à distance (bonne pratique de sécurité — confirmé par un deuxième essai infructueux), le fichier corrigé (étape Repair + `-p:RestoreSources=https://api.nuget.org/v3/index.json`) a été envoyé à Maxime en pièce jointe pour remplacement manuel plutôt qu'en instructions d'édition ligne à ligne (plus fiable). Il a remplacé le fichier, commité (`68bee0a`, 3 lignes ajoutées — cohérent avec les 3 lignes manquantes identifiées), poussé. **Confusion en cours de route** : Maxime a d'abord regardé un réexecution de l'ancien run (même URL, workflow figé au commit d'origine) plutôt que le nouveau run déclenché par le push — clarifié.
  **Résultat du run `build #2` (commit `68bee0a`) : tout vert**, `test-linux` (128 tests Core + 89 tests Repair) et `build-windows` (publish self-contained win-x64), confirmé par Maxime après inspection du détail des deux jobs, pas seulement du résumé.
- disposition: **Blocage #1 (TRANSMISSION, 03/08) formellement levé — premier build.cmd complet, vérifié de bout en bout, depuis le 30/07.** Reste un point mineur non bloquant : Smart App Control empêche toujours l'exécution locale de `PincabToolbox.Core.Tests.exe` sur la machine de Maxime (voir entrées plus haut) — sans conséquence pratique puisque la CI GitHub fait foi désormais et tourne sous Linux. Le `publish/PincabToolbox.exe` produit localement plus tôt dans la session (avant la découverte du trou CI) reste valide : il inclut le fix `Knowledge.cs` et n'était pas concerné par le bug `RestoreSources`, qui ne touchait que le workflow CI, pas `build.cmd`.

## 2026-08-04 · Chantier code — trou Knowledge trouvé en vérifiant, Freezy laissé de côté malgré le feu vert
- code:        VPT_LEGACY_PRESENT (Knowledge) · FREEZY_ZEDMD_MISMATCH (non codé, volontairement)
- bac:         FIX (chantier de code) + garde-fou process
- contexte:    Maxime a demandé de reprendre le scanner et Repair. Il a explicitement autorisé à coder même sans les deux signaux terrain exigés depuis la clôture du 03/08 (« tout même si j'ai pas de signaux »), en réponse à une question de clarification.
- analyse:     **Vérification avant de coder** (leçon de la session du 03/08, appliquée ici) : audit des candidats §2 encore ouverts avant d'en coder un au hasard.
  1. **`.vpt` invisible dans PinUP** — en vérifiant dans le code (pas seulement dans le FIELD-LOG), le check existe déjà en entier depuis le 30/07 : `LegacyTableScanner` (code `VPT_LEGACY_PRESENT`), câblé dans `MainWindow.xaml.cs`, traduit FR/EN dans `Loc.cs`, couvert par un test dans `CoreTests.cs`. **Seul trou réel trouvé** : aucune entrée dans `Knowledge.cs`, alors que tous les autres codes (y compris Info : `ORPHANED_MEDIA_FILE`, `DISPLAY_SETUP_INCOMPLETE`…) en ont une. Corrigé — entrée ajoutée (Impact/Cause FR+EN), même style que les entrées voisines. Changement de donnée pure, zéro logique touchée.
  2. **Freezy/zedmd** — **pas codé, malgré le feu vert.** Différence de nature avec les autres candidats §2 : ce n'est pas qu'il manque un deuxième signal terrain, c'est que **la cause elle-même n'est pas confirmée** par l'utilisateur qui a remonté le cas (E0434352, 2026-07-28). Coder une détection (DLL Freezy 64-bit dans un setup qui exige le x86, zedmd.dll/zedmd64.dll résiduels) reviendrait à parier sur une hypothèse non vérifiée — exactement le mécanisme qui a produit la fausse alerte KPI#1 du 03/08, sauf que cette fois ce serait publié plutôt qu'interne. Je n'ai pas de deuxième round de questions à faire dans cette session pour reconfirmer avec Maxime lui-même ; laissé en l'état, signalé ici plutôt que codé au jugé.
  3. **Vérification technique impossible cette session** : ni ce sandbox cloud ni le bridge vers la machine de Maxime (VM Linux) n'ont de SDK .NET (`dotnet`/`csc` absents des deux). Contrairement à la session du 03/08 qui avait pu faire un parse Roslyn direct sur les fichiers WPF modifiés, **aucun contrôle de syntaxe n'a pu être fait ici, même pas ça.** L'édition de `Knowledge.cs` est un ajout de dictionnaire suivant exactement le patron des entrées existantes (risque faible), mais reste non vérifiée tant que `build.cmd` n'a pas tourné sur Windows.
- disposition: entrée Knowledge livrée (à valider par le prochain `build.cmd`). Freezy reste en attente d'une cause confirmée, pas d'un deuxième signal — à trancher avec Maxime, pas à deviner. **Blocage #1 (build Windows) toujours pas levé** : Maxime confirme ne pas encore avoir lancé `build.cmd` sur sa machine.

## 2026-08-03 · 🔴 DÉCOUVERTE INTERNE — le fix KPI#1 (B2S ≠ signal ROM) documenté comme livré N'EST PAS dans le code réel
- code:        KPI#1 / `ROM_MISSING`
- bac:         FIX (alerte interne)
- contexte:    Alerte levée en session : le fix « B2S ≠ signal ROM » est annoncé comme livré dans RELEASE-NOTES/TRANSMISSION, mais `ScriptAnalyzer.cs` aurait toujours une seule regex traitant `VPinMAME.Controller` et `B2S.Server` comme équivalents.
- ⚠️ **ENTRÉE RECONSTRUITE** (2026-08-03 nuit) — le fichier a été écrasé par une réécriture disque de la session de code ; texte d'origine perdu, titre conservé, contenu réécrit à partir de la vérification faite ensuite. À corriger si Maxime retrouve l'original.
- analyse:     **ALERTE INFIRMÉE, mais elle a fait remonter deux vrais défauts.** Vérification faite au lieu de croire le log :
  1. `ScriptAnalyzer.cs` a bien **deux regex séparées** (`VpinmameCreate()` / `B2SCreate()`) et deux propriétés distinctes (`UsesController` / `UsesB2S`). Le test `Test_B2S_Backglass_Is_Not_A_Rom_Signal` existait déjà.
  2. Le test demandé (« B2S-only + `Const cGameName` résolvable ») a été écrit **avant** toute modification et **passait déjà** : aucun faux critical sur ce cas.
  3. Vérification jusque dans le **binaire livré** (`PincabToolbox.zip`, build du 30/07 19:15 — celui des 65 téléchargements) : la chaîne `uses a B2S backglass but does not drive VPinMAME` y est présente. Le fix est réellement en circulation.
  **Donc : fausse alerte sur le fond.** Mais deux défauts réels trouvés en vérifiant :
  - **(a) La garde d'entrée de `RomValidatorScanner` faisait bien de B2S un signal équivalent.** Elle lisait `!UsesController && !UsesB2S` : une table B2S-only entrait dans la validation ROM et n'en ressortait que grâce à un `else if` en aval. Conséquence observable : une originale B2S dont le `cGameName` existe par hasard dans le dossier roms était étiquetée **`ROM_OK` (« ROM found »)**. Surtout, la protection dépendait d'une branche en aval, pas de la décision elle-même — toute retouche du bloc de lookup rouvrait le faux critical. Corrigé : `UsesController` est désormais le **seul** signal d'entrée, la décision est à un seul endroit. Test dédié qui échouait avant le fix.
  - **(b) Vrai faux positif KPI#1 encore présent, jamais identifié jusqu'ici : les commentaires VBScript.** Les regex `CreateObject` ne sont pas ancrées et `AnalyzeRomUsage` travaillait sur le script brut → une ligne **commentée** `' Set Controller = CreateObject("VPinMAME.Controller")` comptait comme un vrai signal ROM. Or les originales/homebrew sont massivement construites à partir d'un template de table à ROM dont la plomberie VPinMAME est **commentée plutôt que supprimée**. C'est très probablement le mécanisme derrière la liste de Gregg (entrée suivante). Corrigé par `ScriptAnalyzer.StripComments` (gestion `'` et `REM`, conscient des littéraux de chaîne pour ne pas casser sur une apostrophe dans un titre type « Rocky & Bullwinkle's »). 6 tests unitaires + 1 test scanner bout-en-bout.
- disposition: fix (a) et (b) livrés, 108 tests Core verts. **Leçon de process** : une entrée de FIELD-LOG qui affirme qu'un fix manque doit être vérifiée dans le code ET dans le binaire livré avant d'être crue — ici la fausse alerte a coûté zéro et rapporté deux vrais bugs, mais l'inverse (re-coder un fix déjà présent) aurait pu casser du code sain.

## 2026-08-03 · [FB « Virtual Pinball and VPin Cab Builders »] · Gregg — liste de « criticals » qu'il pense être des originaux sans ROM + 2 cas ouverts
- code:        `ROM_MISSING` (FP suspecté)
- bac:         FP
- contexte:    Gregg poste une liste de tables remontées en `critical` par le scan, en disant qu'il s'agit selon lui d'originaux ne nécessitant pas de ROM. 2 cas laissés ouverts : **Rocky & Bullwinkle** et un **B2S Bigus(MOD)**.
- ⚠️ **ENTRÉE RECONSTRUITE** (2026-08-03 nuit) — même écrasement disque que l'entrée précédente ; titre conservé, détail réécrit de mémoire de la consigne de Maxime. Verbatim et liste exacte des tables **perdus, à redemander à Gregg**.
- analyse:     Position de Maxime : **probablement pas des bugs pour la plupart des tables listées** — ce sont des hacks ROM connus, c'est-à-dire des tables qui exigent réellement une ROM (une ROM modifiée/alternative) que Gregg n'a pas. Dans ce cas le `critical` est légitime.
  **Mais l'analyse du code faite dans la foulée (entrée précédente, défaut (b)) donne un mécanisme de FP crédible qui colle exactement à sa description** : une originale bâtie sur un template de table à ROM, avec la plomberie VPinMAME **commentée**, était lue comme « pilote VPinMAME » → `ROM_MISSING` critique. C'est précisément « un original que l'outil déclare critique ». Ce mécanisme est corrigé depuis.
  Sur les 2 cas ouverts : **Rocky & Bullwinkle** (Data East 1993) est une vraie table à ROM (`rab_*`) — sauf s'il parle d'un re-thème original portant ce nom, le critical est très probablement correct, à confirmer avec lui. **B2S Bigus(MOD)** relève de l'autre chantier : les mods portent le nom+année de la table de base mais suivent leur propre versionnage (même racine que la demande de Chad le même jour) — traité côté UpdateWatcher, voir entrée « frictions d'achat ».
- disposition: **répondu ✔ (03/08, la veille de cette session — Maxime avait déjà répondu avant qu'on en reparle ici).** Réponse effectivement envoyée, différente de ce que cette entrée recommandait — **pas** de suggestion de relancer le scan avec le nouveau build ; à la place, Maxime a directement nommé les tables identifiables dans le message de Gregg (Blood Machines, Jurassic Park LE, The Leprechaun King, Harry Potter…) et expliqué que le scanner a trouvé un **nom de ROM précis** pour chacune (pas un « rom manquante » générique), ce qui indique des hacks ROM réels plutôt que le mécanisme de FP des commentaires VBScript — tout en demandant confirmation (le nom exact de ROM demandé sur une des tables). Sur les 2 cas ouverts : demande le screenshot/texte exact pour **Rocky & Bullwinkle** ; sur le B2S, la table est maintenant identifiée précisément — **`Amazing Spider-Man (Gottlieb 1980)_Bigus(MOD)`** — avec un début de piste concret : Maxime voit le fichier B2S présent avec un nom correspondant dans la capture de Gregg, donc si le finding dit « backglass manquant » ça sentirait un vrai FP (pas le sujet UpdateWatcher/mods comme supposé dans cette entrée — possiblement `B2S_ORPHAN` ou `B2S_MISSING`, à confirmer). Demandé : le texte exact du finding. **À suivre : en attente de la réponse de Gregg** (nom de ROM, capture Rocky & Bullwinkle, texte exact du warning B2S).

## 2026-08-03 · [Facebook, post FlipSync] · Chad Greenaway — 2 demandes de fonction (filtre mods + lien direct VPS)
- code:        candidats NOUVEAUX — `UPDATE_FILTER_MODS` + lien direct fiche VPS (renforce le backlog UpdateWatcher déjà consigné 2026-07-31)
- bac:         FEATURE
- verbatim:    « Pretty cool idead, wish the portion that told you if your table was outdated or not had filters like avoid biggus mods etc as well as linked directly to the table on the spreadsheet rather than having to search 🤷 otherwise great work and good idea »
- analyse:     Deux demandes distinctes sur l'UpdateWatcher. (1) **Lien direct vers la fiche VPS** de la table plutôt que de laisser l'utilisateur chercher — pure UX, faisable si on stocke déjà l'ID VPS matché. (2) **Filtrer les variantes/mods (ex. mods « Biggus »)** pour ne pas les flaguer « outdated » à tort — recoupe directement le souci déjà consigné le 2026-07-31 (FD) : la détection ne lit que le nom de fichier local vs la dernière version connue sur VPS, sans savoir qu'un mod n'est pas la table de base → même racine que le problème de renommage. Confirme que la fiabilité de l'UpdateWatcher (nom de fichier seul, pas de vraie correspondance VPS) est un point de friction récurrent, pas isolé.
- disposition: répondu (remercié, honnête sur la limite actuelle — lecture du nom de fichier seul —, pas de date promise). Backlog v0.2 « UpdateWatcher » renforcé avec ces 2 pistes concrètes.

## 2026-08-03 · Rapport perso de Maxime (son propre cab, 6 photos hardware + rapport complet) + clarification attentes Repair
- code:        aucun bug — score F confirmé correct (8 vrais criticals)
- bac:         INFO (partagé « pour info », pas de demande de fix) + clarification produit
- contexte:    Maxime a partagé 6 photos de son pincab physique (carte mère Gigabyte 970A-DS3P, CPU AMD FX-8320, GPU GTX 960 en 4K, Windows 10 Pro FR, dongle Bluetooth/Wifi « edenwood ») + `pincabtoolboxreport202608011755.html` (scan du 2026-08-01 17:55), en précisant explicitement « je veux rien améliorer c'est juste pour info ».
- verbatim:    « voilà mon pincab, je veux rien améliorer c'est juste pour info » ... « du coup si j'installe repair sur le pincab et que je te donne le rapport on va avancer? »
- analyse:     Rapport = 8 critical · 100 warnings · 276 info · 225 ok → score 0/100·F **légitime** (8 vrais `ROM_MISSING` : Blood Machines VPW, hpgf-052-DOF, Jurassic Park, leprechaun, etc. — pas le cas « collection saine » du chantier #1). Anonymisation KPI#2 propre. **CORRECTIF (2026-08-03, relecture HANDOFF.md) : Repair EXISTE déjà en tant que moteur** (`src/PincabToolbox.Repair`, 61 tests verts, 2 actions : DLL bloquée par Windows / archive ROM décompressée) — ce que j'avais dit à Maxime (« Repair n'existe pas encore ») était imprécis. Ce qui est vrai : l'UI Repair n'est PAS câblée dans l'app (décision HANDOFF du 27/07 : « le moteur attend des utilisateurs »), et aucune des deux actions existantes ne couvre le fetch de ROM manquante (RestoreRomArchiveAction ne fait que re-zipper un dossier ROM déjà présent mais décompressé — pas de fourniture de fichier). Donc la réponse донnée reste correcte sur le fond (Repair ne fournira jamais de ROM), mais inexacte sur « n'existe pas du tout ».
- disposition: consigné, correction à passer à Maxime si le sujet revient. Le backlog v0.2 (zombie PinUpDisplay, audio par défaut, nettoyage PinupSystem) reste à coder **en plus** des 2 actions existantes, pas à la place.

## 2026-08-03 (soir) · Chantier code — reste du backlog §2 (feu vert Maxime, hors fenêtre de demande)
- code:        PINUP_DISPLAY_ZOMBIE, DISPLAY_SETUP_INCOMPLETE, ORPHANED_MEDIA_FILE (nouveaux) + kill_zombie_pinup_display, set_default_audio_device, quarantine_orphaned_media (nouvelles IRepairAction)
- bac:         FIX (chantier de code, pas un retour terrain)
- contexte:    Feu vert explicite de Maxime pour coder tout le reste du backlog §2 (scanner + candidats Repair) même hors signal de demande. Moteur Repair étendu, pas refait (61 tests existants + 2 actions conservés intacts).
- analyse:
  **Scanner (Core), 3 nouveaux checks :**
  - `PinupDisplayZombieScanner` — live-process check (comme DiskSpaceScanner, pas un scan de fichiers) : PinUpDisplay.exe actif + aucun VPinballX*/VPinballX64/VPinballX_GL64 actif → Warning. `ProcessProbe`/`DisplayProbe` (Core.Services) ajoutés, P/Invoke direct (advapi32/user32-style, zéro dépendance externe, même pattern que VpinmameRegistry).
  - `DisplaySetupScanner` (code `DISPLAY_SETUP_INCOMPLETE`, Info) — **portée volontairement réduite** vs l'idée originale `DISPLAY_ORDER_MISMATCH` : détecte un composant b2s/DMD installé avec moins de 2 écrans connectés (`GetSystemMetrics(SM_CMONITORS)`), PAS l'ordre/l'assignation écran↔rôle (vivrait dans la config PinUP Popper, schéma non documenté — pas reconstruit pour éviter de deviner un format qu'on ne maîtrise pas). Toujours aucun correctif registre (ADR-005 respecté).
  - `OrphanedMediaScanner` (code `ORPHANED_MEDIA_FILE`, Info) — POPMedia/PUPVideos, un niveau de sous-dossiers. Logique de correspondance dans `Core.Services.OrphanMediaMatcher` (partagée scanner + action Repair, DRY), biaisée pour NE PAS signaler (suffixes `(SCREENx)` et index numériques strippés avant comparaison, fichiers `default*` jamais signalés) — test de non-régression dédié à l'incident du script communautaire (FIELD-LOG 2026-07-29).
  **Repair (3 nouvelles actions), toutes testées (fakes), toutes respectent ADR-005 (registre fermé, confinement par le moteur) et ADR-006 (résumé gratuit dérivé du plan réel) :**
  - `KillZombiePinUpDisplayAction` — `IsReversibleByNature=false` assumé (pas de "dé-tuer" un process) → jamais `Automatic`, toujours confirmation. **Ajustement moteur nécessaire** : `RealEnvironmentProbe.BlockingProcessNames` contenait déjà "PinUpDisplay" (gate générique "rien n'écrit tant que le cab tourne") — sans exemption ciblée, sa seule présence aurait bloqué l'action censée le tuer. `RepairEngine.Preflight` exempte maintenant le(s) process qu'un plan a l'intention de terminer (`ChangeKind.ProcessTermination`, nouveau), sans affaiblir le blocage pour tout le reste (VPinballX qui tourne vraiment continue de bloquer — 2 tests dédiés). Fail-closed si le chemin exact de l'exe n'est pas résolu (pas de confinement possible sur un simple nom de process).
  - `SetDefaultAudioDeviceAction` — décision Maxime du 29/07 respectée (à la demande, pas de script Startup). **Pas encore relié à un Finding** : aucun moyen fiable de détecter statiquement "le device va se réinitialiser", donc backlog UI (bouton Outils dédié) à trancher avec Maxime, pas câblé cette session. `RealAudioDeviceControl` passe par l'interface COM non documentée `IPolicyConfig` (la même que NirCMD utilise en interne — aucune API publique Windows n'existe pour ça) : implémentation Vista→Win10 la plus répandue, **non vérifiable en sandbox Linux, potentiellement pas fonctionnelle telle quelle sur Windows 11** (l'interface a changé au moins une fois). **À tester sur cab réel avant toute release**, conformément à la règle déjà actée en TRANSMISSION pour le code à effet d'écriture Windows.
  - `QuarantineOrphanedMediaAction` — dry-run + backup automatiques (déjà garantis par le moteur), déplacement en quarantaine locale (`_pctb-quarantine`, sibling du dossier scanné) **jamais suppression**, réversible. Recalcule la liste de candidats depuis `RepairContext.Layout` à chaque `Plan()`/`StillApplies()` plutôt que de faire confiance à un `Finding.FilePath` unique (ADR-006 : le plan est calculé, jamais déclaré). **Pas testé sur un vrai gros dossier PinupSystem** (perf du scan un-niveau en conditions réelles inconnue).
  **Bug trouvé et corrigé pendant le chantier** : `RepairEngine.Preflight` utilisait `Path.GetFileNameWithoutExtension` sur les cibles de process — `System.IO.Path` ne traite PAS `\` comme séparateur hors Windows, donc l'exemption ne matchait jamais en environnement de test/sandbox Linux (et ça aurait pu aussi mal se comporter avec des chemins mixtes sur Windows). Remplacé par un split manuel sur `/` ET `\`, même convention que `FileBackupService.LastSegment` déjà dans le code — capturé par le test `Test_Preflight_ProcessKillTarget_IsNotBlockedByItsOwnPresence` avant toute livraison.
  **Knowledge Pack (`knowledge/pack-2026.08.json`)** : entrées ajoutées pour PINUP_DISPLAY_ZOMBIE (règle `kill-zombie-pinup-display`, confiance 90, non réversible, pas de backup — rien à sauvegarder pour un kill de process) et ORPHANED_MEDIA_FILE (règle `quarantine-orphaned-media`, confiance 85, réversible, backup obligatoire). DISPLAY_SETUP_INCOMPLETE volontairement SANS entrée pack (pas de capacité de réparation, comme LOW_DISK_SPACE/VPT_LEGACY_PRESENT avant lui — dégrade proprement en ManualOnly). Catégories `process`/`media-orphan` ajoutées au schéma JSON. `knowledge/selftest.py` mis à jour (sa liste d'ActionId de référence, indépendante du vrai registre par design, ne connaissait pas les 2 nouvelles actions réparables → pack de référence "cassé" par erreur dans le garde-fou, corrigé).
  **`Knowledge.cs` et `Loc.cs` (App)** : entrées FR/EN ajoutées pour les 3 nouveaux codes (impact/cause + libellés localisés), cohérent avec le reste des findings déjà affichés. Les 3 scanners sont câblés dans la composition du `ScanEngine` (`MainWindow.xaml.cs`) — c'est un ajout de SCANNER, pas l'UI Repair (qui reste non câblée, décision HANDOFF du 27/07 à reconfirmer avant tout câblage).
  **Build/tests** : 61 tests Repair existants + 18 nouveaux = 79/79 verts (Debug ET Release) ; 71 tests Core existants + 16 nouveaux = 87/87 verts. `build.cmd` ne lançait QUE les tests Core (jamais Repair) — corrigé, `build.cmd` lance maintenant les deux avant le publish (la CI GitHub, elle, testait déjà les deux). Le projet `PincabToolbox.App` (WPF, `net8.0-windows`) n'a PAS pu être compilé dans ce sandbox Linux — HANDOFF/Maxime doivent valider le build complet (`build.cmd`) sur Windows avant toute release, en particulier `RealAudioDeviceControl`.
- disposition: chantier livré, FIELD-LOG + TRANSMISSION.md mis à jour. **Rien de nouveau publiquement annoncé** — ces 3 checks scanner sont désormais actifs dans l'app dès le prochain build, les 3 actions Repair existent dans le moteur mais restent invisibles (pas d'UI Repair). Décisions explicitement laissées à Maxime : (1) câblage UI Repair (HANDOFF), (2) surface de déclenchement pour l'action audio (bouton Outils ou autre), (3) validation cab réel avant d'annoncer quoi que ce soit sur l'audio/le kill process/le nettoyage média.

## 2026-08-03 (nuit) · Chantier — frictions qui empêchent Repair d'être achetable
- code:        transverse (moteur Repair + UpdateWatcher)
- bac:         FIX
- contexte:    Consigne de Maxime : « il faut que tu regardes les frictions des utilisateurs pour vraiment que Repair soit achetable ». Audit du chemin complet scan gratuit → décision d'achat.
- analyse:     Le blocage n'est pas la fonctionnalité — le moteur a 5 actions et 89 tests. Le blocage est la **crédibilité du scan gratuit**, qui est la seule vitrine de Repair. Trois défauts trouvés, tous du même type : le gratuit promet plus que ce que le payant délivre.
  **1. Faille commerciale dans `RepairModeResolver` (la plus grave).** Les portes s'évaluaient commercial → sécurité. Une règle de confiance < 70 donnait donc `Locked` **sans licence** (« un correctif existe, débloque Repair ») et `ManualOnly` **avec licence** (« voici la procédure, débrouille-toi »). Le scan gratuit vendait littéralement un correctif que l'achat ne délivrait pas — le pire scénario possible pour un produit dont l'argument est la confiance. Corrigé : la porte **sécurité passe avant la porte commerciale**, donc `Locked` ne peut plus signifier que « une licence débloque réellement ceci ». Test exhaustif qui balaie les 101 valeurs de confiance × réversible/non et vérifie que **tout** `Locked` devient `ConfirmationRequired` ou `Automatic` une fois licencié. ADR-006 inchangé.
  **2. Même faille un cran au-dessus, dans `RepairEngine.BuildFindingItem`.** Une action qui `Plan()` zéro changement (échec propre volontaire : `KillZombiePinUpDisplayAction` sans chemin d'exe résolu, `SetDefaultAudioDeviceAction` sans état précédent connu, `QuarantineOrphanedMediaAction` sans orphelin réel) gardait quand même `Mode = Locked`. Après achat : l'item ne faisait rien. Corrigé — zéro changement ⇒ `ManualOnly` + une entrée `Missing` qui **dit pourquoi**.
  **3. Pas de surface d'offre agrégée.** Chaque item portait déjà ses faits, mais le chiffre sur lequel un utilisateur décide (« combien de mes problèmes ça règle ? ») n'existait nulle part — l'UI aurait dû l'agréger elle-même, et une agrégation écrite dans l'UI est une agrégation que personne ne teste (c'est exactement comme ça qu'un tier gratuit se met à sur-promettre). Ajout de `RepairOffer` (`src/PincabToolbox.Repair/Engine/RepairOffer.cs`) : compte réparable / manuel, codes concernés, réversibilité et backup **unanimes ou faux** (une seule action irréversible fait tomber la promesse pour toute l'offre, et une offre vide ne revendique rien — un « tout est réversible » vide reste un mensonge pour un lecteur), durée en bucket, et la liste explicite de **ce que Repair ne fera pas**, affichée AVANT l'achat. `RepairOffer.From` **refuse un plan licencié** (`ArgumentException`) : ADR-006 devient une contrainte de type, plus une convention. 10 tests dédiés.
  **Note d'architecture :** rien de tout ça n'est de l'UI. L'UI Repair reste non câblée (décision HANDOFF du 27/07). Ce chantier construit la **surface moteur** sur laquelle l'UI se branchera — testable, compilable en sandbox Linux, et ça réduit le câblage UI à venir à une liaison de données. Écrire du WPF ici, sans pouvoir le compiler, aurait été le contraire d'un service.
  **UpdateWatcher — la friction de bruit.** Le rapport de FD comptait 2711 `info`, en écrasante majorité « une version plus récente existe sur VPS ». Chad (« wish it had filters like avoid biggus mods ») et Gregg (« B2S Bigus(MOD) ») décrivent la même racine : un mod porte le **nom+année de la table de base** mais suit son propre versionnage → comparaison de versions sans objet → faux « périmé ». Ajout de `TableVariantDetector` (Core.Services) : marqueurs `MOD`/`BIGUS`/`BIGGUS`, **tokens entiers uniquement** (« Modern Times », « Bigger Bang », « The Model Shop » ne matchent pas), groupe `(Fabricant Année)` jamais fouillé (« (MOD Industries 1999) » ne matche pas) mais `(MOD)` seul si. Volontairement **étroit** : seuls les marqueurs avec preuve terrain directe, et biaisé vers le NON-classement — rater un mod coûte une ligne de bruit, classer à tort une table de base **cache une vraie mise à jour**, ce qui est bien plus cher. FSS/VR/hybride délibérément exclus (versionnés par l'auteur d'origine). Les mods ne sont plus comparés et sont **comptés dans le résumé** plutôt que silencieusement ignorés — une omission inexpliquée est indistinguable d'un bug. 7 tests.
  **Lien direct VPS (demande #1 de Chad).** L'id VPS matché était déjà disponible mais jamais exposé. Ajouté en `Args` + `UpdateSource.GameUrlTemplate` (`{id}`) dans le profil. **Laissé vide exprès** : le front VPS a changé d'hôte (`virtual-pinball-spreadsheet.web.app` redirige désormais vers `virtualpinballspreadsheet.github.io`) et le format de route n'a pas pu être confirmé — un lien faux est pire que pas de lien. Dès que Maxime confirme le format, c'est une ligne de JSON, pas un rebuild. 4 tests.
- disposition: livré, tests verts. **Rien annoncé publiquement.** Décisions restant à Maxime : (1) câblage UI Repair sur `RepairOffer` (HANDOFF à reconfirmer), (2) remplir `gameUrlTemplate` après vérification du format VPS, (3) relancer Gregg avec le prochain build avant d'investiguer sa liste plus loin.

## 2026-08-03 (nuit) · Clôture du scanner — audit complet + 3 chantiers, périmètre gelé
- code:        transverse (scanner)
- bac:         FIX
- contexte:    Consigne de Maxime : « qu'est-ce qui fonctionne, ce qui ne fonctionne pas, ce qui reste — faut qu'on en finisse avec le scanner définitivement ». Audit exhaustif puis fermeture.
- analyse:     **État constaté :** 12 scanners câblés, 35 codes émis, **100 % traduits en FR** (zéro trou), les 3 faux positifs historiques les plus coûteux morts et testés (ROM/B2S, score trompeur, roms multi-lecteur). **3 trous trouvés, les 3 comblés :**
  **1. `BlockedFileScanner` : zéro test côté Core.** Le seul scanner non couvert — et c'est la détection derrière la **seule action Repair confirmée deux fois par le terrain** (déblocage DLL : VPForums + Pincab Passion). Le maillon le plus important commercialement était le seul non testé. La lecture du flux NTFS `Zone.Identifier` n'est pas exécutable hors Windows, donc les **deux décisions qu'elle alimente** ont été extraites en pur : `SeverityFor(fileName)` (plugin cœur ⇒ Critical, reste ⇒ Warning) et `IsBlockedZone(contenu)` (zones 3/4 bloquées, **0/1/2 non** — sinon un cab en domaine s'allumerait sur chaque fichier). 8 tests. **Même piège cross-platform retrouvé une 3ᵉ fois** : `Path.GetFileName` ne coupe pas sur `\` hors Windows → un chemin Windows revenait entier et ne matchait jamais. Split manuel, comme `FileBackupService.LastSegment` et `RepairEngine.ProcessNameFromPath`. Capturé par un test avant livraison.
  **2. Trois Warnings sans entrée Knowledge** : `COMPAT_SIGNATURE`, `LOW_DISK_SPACE`, `SCANNER_ERROR` s'affichaient sans impact, ni cause, ni méthode de vérification. Un avertissement qu'on ne peut pas comprendre est un avertissement sur lequel on ne peut pas agir. Rédigés en FR+EN. **Vérification automatisée ajoutée à l'audit : plus aucun code Warning/Critical sans entrée.** (Les codes restants sans entrée sont tous `Ok`/`Info` de confirmation — `ROM_OK`, `BLOCKED_NONE`… — normal, ils n'ont rien à expliquer.)
  **3. Le volume — le vrai problème restant, absent du backlog jusqu'ici.** Plusieurs scanners émettent **une ligne par table** par conception (`ROM_OK`, `ROM_NOT_REQUIRED`, `UPDATE_AVAILABLE`). Sur les 2090 tables de FD ça fait des milliers de lignes, et le peu qui compte s'y noie. **Plafonner le score le 30/07 a corrigé le chiffre, pas le rapport.** Ajout de `ScanReport.Rolled()` : regroupe par (sévérité, code) au-delà de 5 occurrences en **une ligne comptée**. Points de conception : **(a) les `Critical` ne sont JAMAIS regroupés**, quel qu'en soit le nombre — un critical est une table qui ne démarre pas, l'utilisateur a besoin de chaque nom, et masquer 300 tables cassées derrière une ligne propre serait la même malhonnêteté que l'ancien score, dans l'autre sens ; **(b) groupement par (sévérité, code) et non par code seul** — `BLOCKED_DLL` est Critical pour un plugin cœur et Warning pour le reste, les fusionner effacerait la distinction ; **(c) rien n'est perdu** : `Ordered()` garde tout et reste utilisé par le **rapport texte complet** (archive) et le payload JSON, ce que le message de regroupement dit explicitement à l'utilisateur. 8 tests dont « le score est inchangé par le regroupement » (le regroupement est une vue, jamais un diagnostic).
  **Câblage app** : `Rolled()` branché sur la **liste à l'écran**, le **rapport HTML**, le **markdown** et le **BBCode** (les formats qu'on partage sur un forum — c'est là que des milliers de lignes sont catastrophiques). `Ordered()` conservé pour le rapport texte et le JSON. La bannière « FIX THIS FIRST » lit toujours `Ordered()` — elle doit pointer un vrai finding, jamais une ligne de regroupement. **`PincabToolbox.App` ne compile pas dans le sandbox (WPF)**, donc les 4 fichiers App modifiés ont été **vérifiés à la syntaxe par un parse Roslyn direct** (`csc.dll` du SDK) : zéro erreur de famille CS1xxx. Ça ne remplace pas un vrai build Windows, mais ça élimine la faute de frappe.
- disposition: livré. **Périmètre scanner gelé** (encadré en tête de §2) : plus aucun nouveau check sans deux signaux terrain indépendants. L'effort passe sur Repair.

## 2026-07-30 · [Facebook, post FlipSync] · Harley Kirkegard — ".json error" immédiat, EN ATTENTE DE PRÉCISIONS
- code:        — (pas assez d'info pour un code, à rouvrir dès réponse)
- bac:         FN (probable, non confirmé)
- contexte:    Commentaire très bref, aucun détail : « I'm getting a .json error immediately........ »
- verbatim:    « I'm getting a .json error immediately........ »
- analyse:     Pas assez d'info pour diagnostiquer — plusieurs hypothèses possibles, à ne PAS trancher sans le message d'erreur exact : (1) répétition déguisée du bug packaging du 29/07 — s'il a extrait seulement l'exe du zip sans les dossiers `profiles/`/`DemoData/` à côté, il retombera sur une erreur liée à `vpx-popper.json` introuvable/invalide, erreur classique d'utilisateur pressé qui glisse juste l'exe hors de l'archive ; (2) un vrai bug JSON distinct (ex. souci de culture/locale Windows — classique en .NET quand la locale système utilise la virgule comme séparateur décimal et casse un parsing numérique) ; (3) un souci réseau/pare-feu sur le Knowledge Pack téléchargé (JSON aussi). Impossible de savoir laquelle sans le texte exact de l'erreur ou une capture.
- disposition: à répondre en demandant précision (voir message proposé) — remercier, demander le message d'erreur complet/capture, et vérifier en priorité s'il a bien extrait tout le contenu du zip (pas juste l'exe) dans le même dossier. **Ne pas consigner de code de finding tant que la cause n'est pas confirmée.**

## 2026-07-30 · [Facebook, post FlipSync] · Donald Parker — demande de périmètre (Future Pinball)
- code:        — (question de périmètre, pas un finding)
- bac:         FEATURE
- contexte:    Commentaire public sur le post FlipSync (20 likes, 8 commentaires à ce stade).
- verbatim:    « Will it work on Future pinball tables 🤔 asking for a friend 🥸 »
- analyse:     Réponse honnête : **non**, l'app est actuellement scopée VPX (+ PinUP Popper) — lecteur `.vpx` (CompoundFileReader/VpxReader), profils `vpx-popper.json`, rien côté Future Pinball (`.fpt`, moteur totalement différent, pas juste un autre format de table). Pas un bug, une limite de périmètre assumée (cohérent avec le nom du profil et l'architecture actuelle). Bon signal de demande à consigner (Future Pinball a sa propre grosse communauté, cf. Pinsimdb.org exploré le 29/07) mais aucun engagement à prendre publiquement dessus pour l'instant.
- disposition: à répondre honnêtement (voir message proposé), sans sur-promettre de date. Backlog FEATURE "support Future Pinball" ajouté en §2 — clairement hors scope court terme (changement de moteur de scan, pas une évolution mineure).

## 2026-07-30 · Rapport HTML complet reçu (FD, commentaire FB/forum) — 2 découvertes
- code:        NOUVEAU (candidat A : `ROM_FOLDER_NOT_FOUND_MULTIDRIVE`) + candidat B (scoring, pas de code existant)
- bac:         FN (candidat A) + FP (candidat B, sur le score agrégé)
- contexte:    FD a envoyé son rapport de scan complet (grosse collection, plusieurs centaines de tables). Sa question : « It skipped the Rom part .. because VPX is installed on another drive? »
- verbatim:    Ligne du rapport : « VPinMAME roms folder not found — ROM checks skipped. » (Warning, module `rom`, un seul warning global, pas par table). En-tête du rapport : **score 0/100, note F**, résumé « 0 critical · 71 warnings · 2711 info · 1 ok ».
- analyse:     **(A) Confirme l'hypothèse de FD** : le module `rom` n'a pas trouvé le dossier `roms` de VPinMAME et a skippé TOUT le contrôle ROM (pas un FP par table cette fois, un vrai trou de détection). Cause probable : la détection du dossier roms ne cherche que sur le lecteur où est installé VPX/VPinMAME, alors que sa configuration a manifestement les tables (ou VPinMAME) sur un lecteur différent. À confirmer avec lui : quel lecteur pour VPX/Tables, quel lecteur pour VPinMAME/roms exactement. **(B) Découverte plus large et plus grave** : le rapport n'a AUCUN critical, et pourtant le score global affiche **0/100, grade F** — juste à cause du volume (71 warnings de compatibilité version-VPX + 2711 info, dont l'immense majorité = « une version plus récente existe sur le Virtual Pinball Spreadsheet », un item par table). Sur une grosse collection bien entretenue, ça affiche un F alarmant et trompeur alors que rien de grave n'est trouvé — probable bug de formule de score (les infos comptent comme des points négatifs, ou un plafond mal calibré). Risque réputationnel réel : un utilisateur avec une collection saine et à jour peut voir "0/100 F" et perdre confiance, ou pire, le montrer en public comme preuve que l'outil est cassé. Bonne nouvelle en creux : le path-scrubbing fonctionne bien (tous les chemins du rapport sont relatifs, `.\NomTable.vpx`, rien de personnel qui fuite — KPI #2 anonymisation toujours à 0).
- disposition: à répondre à FD (voir message ci-dessous), pas encore codé. **Candidat B (score trompeur) à prioriser très haut demain** — potentiellement plus impactant que KPI #1 puisqu'il touche systématiquement toute grosse collection, pas un cas de table isolée.

## 2026-07-30 · FD précise sa config (capture d'écran de l'app + messages complémentaires) — confirme et aggrave les 2 découvertes
- code:        `ROM_FOLDER_NOT_FOUND_MULTIDRIVE` (candidat A, confirmé) + candidat B (scoring/wording, confirmé plus grave)
- bac:         FN + FP
- contexte:    FD précise : « Pincab root folder » = `E:\...\Tables` (SSD externe, QUE les tables). VPX lui-même est installé sur le lecteur **D**. PinballY! est sur **E**. 2090 tables VPX scannées. Question : « Did it scan all? ... What about the b2s's and media / roms etc. »
- verbatim:    Capture d'écran de l'app : sous le score, le texte affiche littéralement **« Install in bad shape »**, et un encart orange **« FIX THIS FIRST »** met en avant en premier `'AceOfSpeed-2019' declares it requires VPX 10.5+ — check your installed version before launching.` — une simple note de compatibilité (pas une panne) présentée comme LA chose à corriger en priorité.
- analyse:     **(A) Confirmé** : root folder tables = E, VPX/VPinMAME = D → le dossier roms n'est cherché que relativement à un chemin lié à D (ou à un autre défaut), jamais sur E où sont les tables, ni détecté correctement même sur D. Cause quasi certaine maintenant : la détection du dossier roms devrait passer par le registre VPinMAME (`HKEY_CURRENT_USER\Software\Freeware\Visual PinMame\globals`, valeur `rompath`) plutôt que par un chemin relatif fixe — à vérifier côté code demain. B2S/médias, eux, ont bien été scannés (cohérent : ils vivent à côté des tables dans le même dossier E — le rapport contient bien des findings `completeness` sur des `.directb2s`). Seul le module `rom` est en cause. **(B) Aggravé** : la capture confirme que ce n'est pas qu'un chiffre bizarre — le texte **« Install in bad shape »** et l'encart **« FIX THIS FIRST »** sur une simple note de compat (pas un vrai problème) sont des formulations actives et alarmantes, pas juste un artefact de calcul. Ça renforce fortement la priorité #1 pour demain : num & wording du diagnostic global à revoir ensemble (score ET les libellés "bad shape"/"fix this first").
- disposition: à répondre à FD. Bonus marketing : sa phrase spontanée (« like many of you, I've spent more evenings fixing my cab than playing it... ») est un excellent verbatim de positionnement produit, à garder de côté (pas pour le FIELD-LOG métier, plutôt pour marketing/témoignages si FD est d'accord qu'on la cite).

## 2026-07-29 · [Pincab Passion — section Logiciels divers](https://www.pincabpassion.net/f63-logiciels-divers) · MINE de terrain (recommandée par l'admin)
- code:        — (source d'idées, pas un finding isolé)
- bac:         FEATURE
- contexte:    Post FlipSync publié dans cette section (recommandation de l'admin). 33 sujets actifs (tags [ASTUCE]/[SOLUTION]/[INFO]), classés par engagement le plus fort en premier : Changer l'ordre des écrans Windows (52 réponses), Jouer en 4K sur écran Full HD (24), Nettoyer le dossier PinupSystem (11), Mise à jour du flipper (16), Définir le périphérique audio principal (7), Démarrage rapide de Windows (7), Pause VPX & F12 Restart (8), Affichage DMD couleur sous PinballX (SOLUTION), Plus de vidéos sous PinballX (SOLUTION, 5), PinAffinity by MJR (9).
- analyse:     Recoupe le FN Freezy/DMD déjà consigné le 2026-07-28 → le DMD est bien un point de friction récurrent, pas un cas isolé. Trois autres candidats forts pour le scanner/Repair, jamais couverts aujourd'hui : (1) ordre des écrans (52 réponses = la plus grosse douleur connue de la communauté, potentiel finding "mapping écran ≠ profil PinUP") ; (2) périphérique audio par défaut qui se réinitialise après reboot (panne classique multi-écrans/multi-DAC) ; (3) dossier PinupSystem qui grossit avec des fichiers orphelins (media cache). Le fil "Débloquer les fichiers bloqués" confirme au contraire qu'on est déjà bons : BlockedFileScanner/UnblockFileAction couvrent déjà ce cas connu de la communauté.
- disposition: consigné, PAS codé (règle de lancement 48 h). Candidats détaillés en §2. À relire à froid : les fils 52 et 24 réponses en détail (bien plus riches que le résumé) pour extraire les messages d'erreur exacts avant d'écrire un check.

## 2026-07-29 · [VPForums.org](https://www.vpforums.org) — recherche croisée sur les forums (post FlipSync déjà en ligne)
- code:        — (source d'idées, plusieurs candidats à isoler)
- bac:         FEATURE
- contexte:    Maxime a posté sur VPForums/PinballNirvana, pas encore de retour communauté dessus. Recherche proactive de patterns similaires à ceux de Pincab Passion, sur les forums eux-mêmes (pas sur le post FlipSync).
- analyse:     Fils croisés : [guide dépannage PinUp Player](https://www.vpforums.org/index.php?showtopic=39100) · ["Pinup not recognizing some tables"](https://www.vpforums.org/index.php?showtopic=51601) · ["pinup popper issues"](https://www.vpforums.org/index.php?showtopic=44268). Points nouveaux, pas encore couverts par le scanner :
  1. **`PinUpDisplay.exe` reste actif après fermeture d'une table** ("processus non fermé", il faut le tuer manuellement au Gestionnaire des tâches avant de relancer). Bon candidat Repair : action simple et sûre (terminer un processus zombie), bien plus safe que les pistes registre déjà écartées.
  2. **Erreur "fichier .b2sserver introuvable"** au lancement via PinballX — le nom de table se perd dans les paramètres passés à B2S Server. Symptomatique, mais correctif pas évident (dépend du lanceur tiers) — à garder en donnée FAQ plutôt qu'action pour l'instant.
  3. **Tables `.vpt` (legacy VP9) invisibles dans la recherche PinUP** alors qu'elles se lancent très bien en direct — cause : l'extension `.vpt` n'est pas déclarée dans "Games File Extension" de l'émulateur VPX côté Popper. ⚠️ Le développeur officiel NailBuster **déconseille** d'ajouter bêtement `vpt` à l'émulateur VPX existant (ça casse le lancement des .vpt) et recommande un émulateur dédié legacy. Bon candidat de **finding informatif** ("tables .vpt présentes mais non indexées") + lien vers la bonne procédure, PAS un correctif auto qui suivrait le raccourci déconseillé par l'éditeur.
  4. Confirmation croisée : le déblocage DLL Windows revient aussi ici (guide dépannage PinUp) → nouvelle validation que `BlockedFileScanner`/`UnblockFileAction` couvre un vrai point de douleur, cross-site.
  5. Le reste ("pinup popper issues") est un fourre-tout de config individuelle (touches dupliquées, chemin incomplet, playlist vide, case SQL cochée) — spécifique à chaque install, peu généralisable en check produit.
- disposition: consigné, rien codé. Le plus solide des trois nouveaux : nettoyage `PinUpDisplay.exe` zombie (simple, sûr, à ajouter en backlog §2).

## 2026-07-29 · [Pinball Nirvana](https://pinballnirvana.com/forums/) — recherche croisée sur les forums
- code:        — (corrobore un candidat déjà identifié)
- bac:         FEATURE
- contexte:    Fils croisés : ["Wrong screens going on wrong display"](https://pinballnirvana.com/forums/threads/wrong-screens-going-on-wrong-display.22713/) · [guide de dépannage Visual Pinball général](https://pinballnirvana.com/forums/threads/trouble-shooting-visual-pinball.12109/).
- analyse:     Le premier fil **recoupe directement** le candidat "ordre/mapping des écrans" déjà noté depuis Pincab Passion (52 réponses) — la même douleur existe sur un site totalement différent, ce qui renforce sa priorité. Nuance nouvelle et utile : ici la cause n'est pas un souci de driver/registre mais la **veille des moniteurs** ("monitors on standby") qui les fait se reconnecter dans le désordre après le réveil — corrigé en désactivant la veille écran indépendamment de la veille PC. Bonne précision à ajouter à la future FAQ. Le second fil (guide général) est surtout de l'archéologie VP9/DirectX 8/Internet Explorer — peu pertinent pour VPX moderne, sauf un point générique réutilisable : un **espace disque insuffisant** provoque des erreurs (« Unable to Create Offscreen Texture »/textures manquantes) — check simple, générique, pas cher à ajouter (`DiskSpaceScanner`).
- disposition: consigné. Renforce la priorité de "ordre des écrans" (détection + FAQ, toujours pas de Repair registre) et ajoute deux petits candidats : nuance veille moniteurs (FAQ), et un check espace disque générique.

## 2026-07-29 · [Pincab Passion — "Changer l'ordre des écrans dans Windows"](https://www.pincabpassion.net/t2788-astuce-changer-l-ordre-des-ecrans-dans-windows) · lecture détaillée (52 réponses)
- code:        — (recherche, pas encore un finding)
- bac:         FEATURE
- contexte:    Ordre des écrans Windows qui ne correspond pas à l'assignation attendue (playfield/backglass/DMD) sur un pincab multi-écrans.
- verbatim:    Fix de base posté par gech : « débrancher ses écrans 2 et 3, supprimer ces 2 clés registre [`HKLM\...\GraphicsDrivers\Configuration` et `\Connectivity`] et ajouter ses écrans dans le bon ordre ». Autres pistes citées : RivaTuner/Powerstrip, réinstall pilotes GPU via DDU, assignation manuelle des numéros d'écran dans PinballX. Cause consensuelle : Windows numérote selon le **type de sortie vidéo** (VGA prioritaire sur numérique) et le timing de détection ; s'aggrave avec des connexions mixtes (VGA/HDMI/DP) ou un mix de GPU AMD+NVIDIA.
- analyse:     Douleur réelle et la plus commentée du forum, MAIS mauvais candidat pour une action Repair automatisée : (1) le fix touche des clés de registre **système**, hors de toute racine d'installation détectée par `InstallLayout` → sort du modèle de confinement d'ADR-005 (« registre actions fermé » = le moteur ne valide que des cibles à l'intérieur des racines détectées, pas HKLM système) ; (2) la manip nécessite un geste physique (débrancher/rebrancher les écrans) qu'aucune action ne peut automatiser ; (3) trop de variantes matérielles (VGA/HDMI/DP, AMD+NVIDIA) pour un correctif générique fiable. Piste plus sûre et cohérente avec "lecture seule" : un **check qui détecte un nombre d'écrans/résolutions incohérent avec le profil PinUP** (signal, pas correction) + renvoyer vers cet article en FAQ/knowledge pack (donnée, pas capacité).
- disposition: consigné pour décision demain. Recommandation : **détection (finding informatif) + lien FAQ, pas de Repair action registre**.

## 2026-07-29 · [Pincab Passion — "Jouer en 4K sur un écran Full HD"](https://www.pincabpassion.net/t8544-info-jouer-en-4k-sur-un-ecran-full-hd-c-est-possible) · lecture détaillée (24 réponses)
- code:        — (écarté du backlog scanner/Repair après lecture)
- bac:         FEATURE
- contexte:    Technique de supersampling (DSR nVidia / VSR AMD, facteur 4.00 : 1920×1080 → 3840×2160) + réglages VPX (« Force exclusive Full Screen Mode », anti-aliasing désactivé) pour améliorer le rendu sur un écran Full HD.
- verbatim:    L'auteur (Shadow_SHD) le présente lui-même comme une « astuce un peu gadget » servant surtout à « vérifier la compatibilité de son matériel en prévision d'un passage à un écran 4K » — pas à corriger une panne. Effets de bord rapportés : lag/ralentissements en fenêtré, configuration minimum recommandée (i5, 8 Go RAM, GTX 1060 6 Go, SSD — une 1050 Ti est insuffisante), crashs au démarrage sans message pour un utilisateur, et un fix de dernier recours en cas de crash (registre `ddraw` de 1 à 0 par ROM).
- analyse:     Confirmé après lecture : **ce n'est pas une panne**, c'est un réglage de confort optionnel et risqué (le fix de crash touche aussi le registre, par ROM). Aucun signal de finding fiable à en tirer (pas de message d'erreur générique, dépend du choix de l'utilisateur). Écarté du backlog scanner/Repair.
- disposition: retiré des candidats de check (voir §2, corrigé). Éventuellement un contenu FAQ/astuce marketing si utile, hors scope produit actuel.

## 2026-07-29 · Repérage des sites communautaires pinball virtuel (Orbitalpin, VPForums, PinballNirvana, VPDB, Pinsimdb, RoguePinball)
- code:        — (repérage stratégique, pas un finding)
- bac:         FEATURE
- contexte:    Maxime demande si ça vaut le coup de s'inscrire et poster partout sur : Orbitalpin.com, VPForums.org, pinballnirvana.com, VPDB.io, Pinsimdb.org, RoguePinball.com.
- analyse:     Les 6 sites ne sont PAS équivalents — trois sont des forums où poster une annonce a du sens, trois sont des catalogues/bases de données en lecture, pas des lieux d'annonce :
  - **VPForums.org** — forum généraliste majeur, très actif (membres à >12 000 messages, discussions quotidiennes en juillet 2026), inscription libre + section "Upload Content" et catégorie "Frontends and Addons" pour les outils. **Poster ici.**
  - **pinballnirvana.com** — forum actif depuis 2003, énorme (39 389 membres, 113 165 messages), couvre VPX/Future Pinball + machines réelles, compte + post possibles. **Poster ici.**
  - **RoguePinball.com** — plus petit mais réel et vivant (1 840 membres, 4 922 messages, 46 sous-forums), inscription ouverte, upload libre pour tous les membres, accueille cab et desktop. Bon rapport effort/retour, **à faire en 3ᵉ si le temps le permet.**
  - **VPDB.io** — PAS un forum : une base de données/API de tables VPX (repo GitHub `vpdb/server`, `vpdb/website`). Rien à poster ici en communauté ; intérêt = source de données technique (voir plus bas).
  - **Pinsimdb.org** — catalogue/archive de référence (Future Pinball surtout, >1 900 tables listées), dimension communautaire secondaire via un forum externe. Peu d'intérêt pour une annonce, à laisser de côté.
  - **Orbitalpin.com** — PAS un forum non plus : une plateforme de distribution de tables VPX **originales/homebrew** (pas de compte, pas de post). **Piste concrète et directe pour corriger le FP KPI #1 (ROM_MISSING)** : leur catalogue référence des tables originales sans ROM, et cite justement une règle **Harry Potter** (homebrew, voir docs.orbitalpin.com) — l'exact exemple donné par le commentateur Facebook du FP. Source potentielle pour une liste de référence "tables originales sans ROM" à croiser dans RomValidatorScanner/ScriptAnalyzer, en complément/alternative à la base VPS déjà utilisée (VpsDatabase.cs).
- disposition: consigné, pas d'inscription faite. Recommandation : **ne pas s'inscrire partout** — prioriser VPForums.org et pinballnirvana.com (grosse audience, sections adaptées), RoguePinball.com en bonus si le temps le permet. VPDB/Pinsimdb/Orbitalpin ne sont pas des cibles de post ; Orbitalpin à recreuser côté produit (donnée) pour KPI #1, pas côté communauté.

## 2026-07-29 · [Visual Pinball Addicts (FB)] · commentaire communauté — PREMIER FP CRITIQUE CONFIRMÉ (KPI #1)
- code:        ROM_MISSING
- bac:         FP
- contexte:    VPX / tables ORIGINALES (natives, sans ROM requise) — ex. cités : Guardians of the Galaxy, Harry Potter (homebrew). À distinguer des recréations Stern (qui, elles, ont bien une ROM).
- verbatim:    « il existe des faux positifs dans les recherches qui indique des problèmes critiques, la plupart sont des roms manquantes alors que ce sont des tables originales sans rom (comme la guardian of the galaxy, Harry Potter, etc...) » (anonymisé)
- analyse:     Premier FP CRITIQUE confirmé publiquement depuis le lancement — le plus précieux à ce jour (KPI #1). RomValidatorScanner/ScriptAnalyzer ne distinguent pas aujourd'hui « table qui doit avoir une ROM » de « table native/homebrew sans ROM » → flague ces dernières en « ROM manquante » sévérité CRITIQUE, alors que le produit promet justement de reconnaître les originales. À confirmer avec l'auteur pour caler le fix : bien des versions originales/homebrew (vs recréation Stern) ? + récupérer la ligne exacte du rapport ou le nom de fichier d'une table concernée.
- disposition: répondu ✔ (assumé immédiatement, remercié, question de clarification posée pour obtenir la donnée exacte). **À corriger avant le prochain pack — priorité haute.**

## 2026-07-29 · CORRECTIF packaging DÉPLOYÉ
- Fix livré le matin même : re-package en `PincabToolbox.zip` (exe + profiles/ + DemoData/), uploadé sur la release GitHub v0.1.0-alpha (ancien exe nu supprimé). Landing redéployée (bouton → .zip, vérifié live). Posts (landing/forum/FB) basculés .zip + note « dézippe & lance ».
- Bonus : téléchargement ~62 Mo (zip) au lieu de ~145 Mo (exe).
- Dette technique OUVERTE pour v0.1.1 : revenir à un **exe unique** en embarquant le profil (EmbeddedResource, loader côté Core — cloud-testable) + les données démo. Évite le format zip et la contrainte « garder les fichiers ensemble ». À faire à froid, pas dans l'urgence.
- Leçon actée : toujours tester un **téléchargement propre** (machine sans le dossier de dev) avant publication. Trou trouvé par la communauté en heures.

## 2026-07-29 · Visual Pinball Addicts (FB) — PREMIER BUG TERRAIN (P0 packaging)
- code:        — (bug de packaging, pas un finding)
- bac:         FIX / packaging
- contexte:    2 users (Lee Davey EN, Franck Lemarinel FR — +1 like) lancent l'exe téléchargé → « app is looking for profiles/vpx-popper.json ».
- verbatim:    « Not working for me… it looking for profiles/vpx-popper.json » · « l application me demande un profiles vpx-popper.json ?????? »
- analyse:     La release ne contenait QUE PincabToolbox.exe. Or l'app charge 2 fichiers LOOSE via AppContext.BaseDirectory : profiles/vpx-popper.json (MainWindow.xaml.cs:297, requis pour scanner) ET DemoData/install (BtnDemo_Click:272, requis pour le mode démo). Single-file .NET n'embarque PAS les Content → ils restaient à côté de l'exe dans publish/, non uploadés. Marchait chez Maxime (lancé depuis publish/). Cause : confusion « self-contained runtime » ≠ « self-contained content ». Le mode démo était cassé pareil (2ᵉ mur non encore remonté).
- disposition: FIX immédiat = re-package en PincabToolbox.zip (exe + profiles/ + DemoData/), liens landing+posts basculés .exe→.zip + note « dézippe & lance ». Répondu à Lee & Franck (assumé, correction annoncée). À FAIRE proprement en v0.1.1 : embarquer le profil (resource) + démo pour revenir à un exe unique (cloud-testable côté Core). Leçon : tester un TÉLÉCHARGEMENT PROPRE sur machine sans le dossier de dev — le seul cas jamais couvert, trouvé par la communauté en heures.

## 2026-07-28 · [Visual Pinball Addicts (FB, 14,8K)] · post FlipSync
- code:        — (interaction de confiance, pas un finding)
- bac:         WORDING / confiance
- contexte:    Post de lancement EN. Un membre challenge publiquement la promesse no-tracking.
- verbatim:    « you promote no tracking/no telemetry for the app but the link here has a FB tracker in the URL » (public)
- analyse:     Vérifié : landing = AUCUN tracker (pas de pixel FB, pas d'analytics, pas de cookie ; vercel.json = en-têtes sécu uniquement). Le « tracker » = fbclid ajouté par Facebook lui-même à tout lien sortant → pas de notre fait. ⚠️ Honnêteté : ne PAS clamer « app 100% offline / zéro réseau » — l'Update Watcher lit la base PUBLIQUE VPS en ligne (tourne aussi hors-ligne). Formule juste = « rien de TOI n'est envoyé ».
- disposition: répondu ✔ (précis, non défensif, invite à vérifier soi-même). KPI #2 (incidents d'anonymisation) reste **0** — c'est une interrogation, pas une fuite. A servi de base à marketing/FAQ-objections.md.

## 2026-07-28 · Visual Pinball Addicts — accueil global
- code:        — (signal d'accueil)
- bac:         FEATURE/retour positif
- verbatim:    « What a cool tool man… one of the few things I dislike about this hobby is how complicated it can be to manage everything. This will really help people. » · « bravo pour l'initiative » · « Yeah baby! »
- analyse:     Verbatims de valeur (proposition de valeur validée en public). À réutiliser comme témoignages.
- disposition: répondu ✔. Note : ~15 téléchargements à J+1 (baseline KPI #4, avant posts forum).

---

## 2026-07-29 · [Pincab Passion — "Définir le périphérique audio principal en ligne de commande"](https://www.pincabpassion.net/t6432-astuce-definir-le-peripherique-audio-principal-en-ligne-de-commande) · lecture détaillée (7 réponses)
- code:        — (recherche, pas encore un finding)
- bac:         FEATURE
- contexte:    « Aléatoirement quand je démarre mon pincab, le périphérique audio par défaut se met sur mon écran playfield branché en HDMI au lieu de la sortie jack » — oblige à quitter PinballX et reconfigurer l'audio à chaque occurrence.
- verbatim:    Solution postée : outil **NirCMD** (utilitaire CLI tiers) + script Startup : `nircmd.exe setdefaultsounddevice "Haut-parleurs" 1`. Alternative proposée (désactiver les autres sorties dans le panneau de config) rejetée par l'auteur : « nécessité de préserver les sorties HDMI pour les effets sonores mécaniques ».
- analyse:     Présenté comme une **panne récurrente et automatique** (pas un réglage ponctuel) — bon candidat Repair, et contrairement au cas "ordre des écrans" : (1) l'action (définir le périphérique audio par défaut) est réversible et sans risque de casse, (2) elle peut s'implémenter en code natif (API Windows) sans dépendre d'un binaire tiers téléchargé (NirCMD) ni d'un script Startup permanent — cohérent avec ADR-005 (capacité = code chez nous, pas un outil externe invoqué par le pack). Reste une question produit à trancher : action **à la demande** dans l'app (l'utilisateur relance Repair quand ça recasse) vs script Startup persistant (sort du modèle "action ponctuelle" des autres `IRepairAction`, plus proche d'une modification système permanente) — poser la question à Maxime.
- disposition: consigné pour décision demain. Recommandation : **bon candidat Repair**, plus sûr que "ordre des écrans", à condition de rester une action ponctuelle déclenchée par l'utilisateur (pas un script Startup silencieux qu'on installerait pour lui).

## 2026-07-29 · [Pincab Passion — "Nettoyer automatiquement votre dossier PinupSystem"](https://www.pincabpassion.net/t16087-astuce-nettoyer-automatiquement-votre-dossier-pinupsystem) · lecture détaillée (11 réponses)
- code:        — (recherche, pas encore un finding)
- bac:         FEATURE
- contexte:    Accumulation progressive de médias orphelins dans le dossier `PinupSystem` (14 sous-dossiers : Audio, BackGlass, DMD, Loading, Wheel, etc.) — fichiers dont le nom ne correspond plus à aucune table `.vpx` installée. Pas une panne, une accumulation qui bouffe l'espace disque.
- verbatim:    Script PowerShell communautaire : scanne les `.vpx` présents, compare aux fichiers média des 14 sous-dossiers, supprime les orphelins, préserve les fichiers "default" et ceux en "(SCREENx)". **Incident réel dans le fil** : Draken06 signale que la première version du script a supprimé par erreur ses vidéos Loading "fullscreen" du type `F-14 Tomcat (Williams 1987)01(SCREEN3).mp4` — corrigé après coup pour exclure les "(SCREENx)".
- analyse:     Bon candidat Repair car la cible reste dans les racines détectées par `InstallLayout` (cohérent avec ADR-005, contrairement au cas écrans). Mais l'incident Draken06 est un avertissement direct : une heuristique de correspondance nom-fichier ↔ table mal conçue **supprime des fichiers encore utilisés** — exactement le risque qu'ADR-006 (dry-run gratuit) et le Journal/BackupService existants sont censés couvrir. Présenté comme nettoyage ponctuel (pas systématique après chaque install), donc pas urgent, mais à ne coder qu'avec dry-run obligatoire + sauvegarde avant suppression, jamais en suppression directe.
- disposition: consigné pour décision demain. Recommandation : **candidat Repair valide**, mais complexité/risque plus élevés que l'audio (14 sous-dossiers, heuristiques de nommage) — prévoir dry-run + backup dès la conception, pas une réécriture du script communautaire tel quel.

## 2026-07-28 · [groupe FB Pincab Passion](https://facebook.com/groups/201831033775882)
- code:        NOUVEAU (candidat : FREEZY_ZEDMD_UPGRADE_RESIDUE / PINUP_BITNESS)
- bac:         FN
- contexte:    Freezy dmd-extensions 2.5.0 + zedmd (serum 2.6.0) + PinUP Popper. « External exception E0434352 » (exception .NET/CLR) + DMD noir dans les menus PinUP. OK en lançant depuis VPX, KO depuis PinUP.
- verbatim:    « je n'arrive pas à mettre à jour freezy en 2.5.0 [...] External exception E0434352 + DMD noir dans les menus » (anonymisé)
- analyse:     E0434352 = exception CLR .NET. Causes classiques (issue GitHub #482 + wiki officiel PinUP) : (1) DLL Freezy 64-bit copiées alors que PinUP veut le x86 32-bit ; (2) anciens zedmd.dll/zedmd64.dll de la 2.4.0 non supprimés ; (3) dmddevice.ini du zip écrasant les réglages. Le scanner v0.1 vérifie le 32/64-bit de dmddevice (pourrait mordre sur la cause 1) mais NE détecte PAS les zedmd.dll résiduels ni le mismatch PinUP→x86 → faux négatif probable sur ce cas.
- disposition: répondu ✔ (compte officiel, pistes en questions). Check à créer en v0.2 UNE FOIS la cause confirmée par l'utilisateur — pas avant (règle de lancement). Sources : github.com/freezy/dmd-extensions/issues/482 · nailbuster.com/wikipinup upgrade_freezy.

---

## 2. Demandes de réparation / de fonctions (backlog, PAS pendant les 48 h)

*Sert les KPI #9 (demandes explicites de réparation) et #8 (codes fréquents) de SUCCESS-METRICS.*

> ### 🔒 SCANNER GELÉ (décision Maxime, 2026-08-03)
> Le scanner est considéré **clos**. 12 scanners câblés, 35 codes, 100 % traduits FR, tous les
> codes Warning/Critical documentés (impact + cause), tous les scanners testés.
>
> **Règle d'entrée à partir de maintenant : aucun nouveau check n'entre sans DEUX signaux terrain
> indépendants** (deux utilisateurs, ou deux sources/forums distincts). C'est ce qui a fait la
> qualité des derniers ajouts — le filtre mods vient de Chad ET de Gregg le même jour, le nettoyage
> média de Pincab Passion ET de VPForums. Un signal unique se consigne en §2 et attend son
> deuxième. Ça vaut aussi pour les idées internes : la fausse alerte KPI#1 du 03/08 rappelle
> qu'une intuition non recoupée coûte du temps et peut casser du code sain.
>
> L'effort passe désormais sur **Repair** (surface d'achat, validation cab réel), pas sur la détection.

- **Support Future Pinball** — origine : commentaire FB de Donald Parker (2026-07-30, voir §1). Hors scope actuel (app scopée VPX/PinUP), demanderait un moteur de lecture de tables `.fpt` entièrement différent — gros chantier, pas une évolution mineure de v0.2. À garder en veille de demande (si ça revient souvent, ça devient un signal de marché à part entière), sans engagement de date.
- ✅ **CODÉ — vérifié le 2026-08-03** — ~~🔴 PRIORITÉ HAUTE~~ **Score global trompeur (0/100 · F malgré 0 critical)**. Les deux volets sont livrés et testés : (1) `ScanReport.Score` — les `Info` et `Ok` ne bougent plus le score du tout, les `Warning` passent par des rendements décroissants **plafonnés à −30** (`WarningPenaltyCap`), donc le volume seul ne peut plus descendre en dessous du grade B ; seuls les `Critical` (−15 chacun, sans plafond) emmènent plus bas. 5 tests dont `Test_Score_LargeHealthyCollection_NeverGradesF`. (2) Le **wording**, qui était la moitié la plus toxique du problème : « FIX THIS FIRST » (rouge) est désormais réservé aux `Critical` ; un simple `Warning` obtient un libellé plus doux (`priority.watch`) et l'accent orange (`MainWindow.xaml.cs`). Le cas exact de FD — une note de compat présentée comme LA chose à corriger — ne peut plus se produire. *(Bullet laissé dans le backlog par erreur alors que le code était déjà livré ; corrigé en relisant §2.)*
- ~~**🔴 PRIORITÉ HAUTE — Score global trompeur (0/100 · F malgré 0 critical)**~~ *(entrée d'origine ci-dessus, conservée pour l'historique)* — origine : rapport complet de FD (2026-07-30, voir §1). Sur une grosse collection sans aucun problème critique, le score affiche quand même 0/100 et un F, uniquement à cause du volume de warnings (compat version-VPX) et d'info (mises à jour VPS disponibles, une ligne par table). Impact potentiellement plus large que KPI #1 : touche systématiquement tout utilisateur avec une grosse bibliothèque bien tenue → confiance/crédibilité de l'outil en jeu. À revoir demain en priorité : soit la formule de score ne doit pas compter les "info" comme des points négatifs (ou très marginalement, avec plancher), soit il faut un second indicateur distinct de "problèmes réels" (warnings+critical) vs "informations" (mises à jour dispo), pour ne pas mélanger les deux dans une seule note anxiogène.
- ✅ **CODÉ — vérifié le 2026-08-03** — **Détection dossier roms VPinMAME sur un lecteur différent** (`ROM_FOLDER_NOT_FOUND_MULTIDRIVE`) — origine : rapport de FD (voir §1), tout le module ROM était skippé (« roms folder not found ») quand VPX/VPinMAME sont sur des lecteurs différents. Livré : `LayoutDetector.Detect` retombe sur le **rompath du registre VPinMAME** (`VpinmameRegistry.TryGetRomPath()`) quand aucun dossier roms n'existe sous la racine scannée, avec un paramètre `vpinmameRomPathHint` injectable pour rendre le cas testable hors Windows. Classe `LayoutDetectorTests` dédiée. *(Bullet resté ouvert alors que le code était livré ; corrigé en relisant §2.)* **Reste à faire : confirmer avec FD** que son cas concret est bien résolu — c'est lui qui a remonté le FN, il mérite le retour.
- ✅ **CODÉ (2026-08-03, nuit)** — **Filtre mods/variantes dans l'UpdateWatcher** — origine : Chad Greenaway (« avoid biggus mods ») + Gregg (« B2S Bigus(MOD) »), même journée, plus le rapport de FD sur le renommage. Racine commune : un mod porte le nom+année de la table de base mais suit son propre versionnage, donc la comparaison de versions fabrique un « périmé » fantôme. `TableVariantDetector` (Core.Services) : marqueurs `MOD`/`BIGUS`/`BIGGUS` en **tokens entiers**, groupe `(Fabricant Année)` jamais fouillé. Volontairement étroit et biaisé vers le non-classement — classer à tort une table de base **cacherait une vraie mise à jour**, ce qui coûte plus cher qu'une ligne de bruit. Les mods sont **comptés dans le résumé**, jamais silencieusement ignorés. 7 + 4 tests. **FSS/VR/hybride exclus exprès** (versionnés par l'auteur d'origine).
- 🟡 **CODÉ À MOITIÉ (2026-08-03, nuit)** — **Lien direct vers la fiche VPS** (demande #1 de Chad) — l'id VPS matché est désormais exposé dans les `Args` du finding `UPDATE_AVAILABLE`, et `UpdateSource.GameUrlTemplate` (`{id}`) permet de construire le lien. **Laissé vide dans le profil exprès** : le front VPS a changé d'hôte (`virtual-pinball-spreadsheet.web.app` redirige vers `virtualpinballspreadsheet.github.io`) et le format de route n'a pas pu être confirmé — un lien faux est pire que pas de lien. **Action Maxime : ouvrir une fiche table sur le site, copier le format d'URL, le coller dans `profiles/vpx-popper.json`.** Une ligne de JSON, pas un rebuild.
- **Détection résidus/mismatch upgrade Freezy (PinUP)** — après un upgrade Freezy, détecter (a) DLL Freezy 64-bit dans un setup PinUP qui exige le x86, (b) anciens zedmd.dll/zedmd64.dll résiduels. Origine : cas 2026-07-28 (voir §1). Fort potentiel : E0434352 est une panne très fréquente. **Toujours BLOQUÉ (2026-08-03)** : cause pas encore confirmée par l'utilisateur — règle de lancement respectée, non codé.
- **Visualiser le rapport de scan dans l'appli (sans export)** — demande liée au FP ROM_MISSING du 2026-07-29 (voir §1). L'utilisateur doit actuellement exporter pour relire le log. **Non codé (2026-08-03)** : feature d'UI d'affichage, hors du périmètre « scanner + candidats Repair » de ce chantier. Reste backlog v0.2/v0.3.
- ✅ **CODÉ (2026-08-03)** — Détection (pas correction) d'un ordre d'écrans/résolutions incohérent avec le profil PinUP (candidat NOUVEAU, ex. `DISPLAY_ORDER_MISMATCH`, finding **informatif seulement**) — origine : fil "Changer l'ordre des écrans dans Windows" (52 réponses, lu en détail — voir §1), le sujet le plus commenté du forum. **Lu en détail : la correction elle-même (clés de registre HKLM GraphicsDrivers + débranchement physique des écrans) sort du scope Repair** — hors des racines confinées par ADR-005, geste physique non automatisable, trop de variantes matérielles. **Livré sous une forme plus étroite que prévu** : `DisplaySetupScanner` (code `DISPLAY_SETUP_INCOMPLETE`) compare le nombre d'écrans réellement connectés (`GetSystemMetrics(SM_CMONITORS)`) au fait qu'un composant backglass/DMD soit installé — signal de COMPTE, pas d'ORDRE (le mapping écran↔rôle vit dans la config PinUP Popper elle-même, schéma non documenté, pas re-créé pour ne pas deviner). Aucun correctif registre, comme prévu. Voir entrée technique du 2026-08-03 ci-dessous pour le détail.
- ✅ **CODÉ (2026-08-03), pas encore relié à un Finding** — Périphérique audio par défaut réinitialisé (candidat NOUVEAU, ex. `AUDIO_DEVICE_MISMATCH`) — lu en détail (voir §1) : panne récurrente confirmée (« aléatoirement au démarrage »), fix communautaire = NirCMD + script Startup. **Meilleur candidat Repair des quatre pistes** : action réversible, sans risque, implémentable en code natif sans dépendance externe. **DÉCISION (2026-07-29, Maxime) : action ponctuelle à la demande**, cohérente avec le modèle `IRepairAction` existant (pas de script Startup persistant/silencieux). `SetDefaultAudioDeviceAction` (`set_default_audio_device`) codée et testée contre l'abstraction — mais aucun scan ne peut détecter statiquement « le device va se réinitialiser », donc pas de Finding/règle de pack pour l'instant : déclenchement futur via un bouton Outils dédié, décision UI à reprendre avec Maxime. **COM interop non vérifiable en sandbox, à tester sur cab réel avant toute release** (voir entrée technique ci-dessous).
- ✅ **CODÉ (2026-08-03)** — Nettoyage dossier PinupSystem (fichiers média orphelins) — lu en détail (voir §1) : cible bien confinée dans InstallLayout (cohérent ADR-005), mais un **incident réel** dans le fil (le script communautaire a supprimé par erreur des vidéos encore utilisées, mal exclues par son heuristique de nommage) confirme le risque. `OrphanedMediaScanner` (code `ORPHANED_MEDIA_FILE`) + `QuarantineOrphanedMediaAction` (`quarantine_orphaned_media`) : dry-run + backup + **déplacement en quarantaine locale, jamais suppression** ; l'heuristique de nom exclut explicitement les suffixes `(SCREENx)` et biaise vers « ne pas signaler » (test de non-régression dédié à l'incident communautaire). **Reste à tester sur un vrai dossier PinupSystem volumineux** avant release (perf du scan un-niveau, jamais mesurée en conditions réelles).
- ~~4K sur écran Full HD~~ — **écarté** après lecture détaillée (voir §1) : réglage de confort optionnel assumé comme « gadget » par son propre auteur, pas une panne, pas de signal de finding exploitable.
- ✅ **CODÉ (2026-08-03)** — Nettoyage `PinUpDisplay.exe` zombie (candidat NOUVEAU) — origine : guide dépannage PinUp Player sur VPForums (voir §1). Le processus reste actif après fermeture d'une table et bloque le lancement suivant tant qu'il n'est pas tué manuellement. Excellent candidat Repair : action simple (terminer un processus), réversible... **en fait NON réversible** (aucun moyen de "dé-tuer" un processus) — `IsReversibleByNature=false` assumé, donc jamais `Automatic` (règle d'or du moteur), toujours confirmation. `PinupDisplayZombieScanner` (code `PINUP_DISPLAY_ZOMBIE`) + `KillZombiePinUpDisplayAction` (`kill_zombie_pinup_display`). **Détail moteur notable** : la liste des process bloquants (`RealEnvironmentProbe.BlockingProcessNames`) contenait déjà "PinUpDisplay" — sans ajustement, sa seule présence aurait bloqué l'action censée le tuer. `RepairEngine.Preflight` exempte maintenant, au cas par cas, le process qu'un plan a l'intention de terminer (`ChangeKind.ProcessTermination`), sans toucher au blocage pour tout le reste (VPinballX qui tourne vraiment continue de bloquer). Testé (2 tests dédiés dans RepairTests).
- **Tables `.vpt` (legacy) invisibles dans la recherche PinUP** (candidat NOUVEAU, finding **informatif**) — origine : VPForums "Pinup not recognizing some tables" (voir §1). Cause : extension `.vpt` absente de la config "Games File Extension" de l'émulateur VPX. ⚠️ NailBuster (éditeur PinUP) déconseille le correctif rapide (l'ajouter à l'émulateur VPX existant casse les .vpt) — recommande un émulateur legacy dédié. Donc : détecter et pointer vers la bonne procédure, pas d'auto-fix qui suivrait le raccourci déconseillé.
- ✅ **CODÉ — vérifié le 2026-08-03** — **Check espace disque générique** (`LOW_DISK_SPACE`) — origine : guide dépannage VP général sur Pinball Nirvana (voir §1). Livré : `DiskSpaceScanner` avec une méthode `Evaluate` pure (testable) + un wrapper qui lit le disque réel. Pas de règle de réparation associée, et c'est volontaire — on ne libère pas de l'espace à la place de l'utilisateur. *(Bullet resté ouvert alors que le code était livré ; corrigé en relisant §2.)*
- **FAQ : veille moniteurs indépendante de la veille PC** — précision à ajouter à la future FAQ "ordre/mapping des écrans" (pas un nouveau code) — origine : Pinball Nirvana "Wrong screens going on wrong display" (voir §1), qui corrobore le candidat déjà identifié chez Pincab Passion.
- **Fichier `.b2sserver` introuvable via PinballX** — origine : VPForums (voir §1). Le nom de table se perd dans les paramètres passés à B2S Server par le lanceur tiers. Symptomatique mais correctif pas maîtrisé par nous (dépend de PinballX) — à garder en note FAQ, pas en check pour l'instant.

*(sinon vide)*

---

## 3. Compteurs vivants (report vers SUCCESS-METRICS)

| Semaine | Téléchargements | Rapports postés | FP confirmés | FN découverts | Nouveaux cas pack | Demandes Repair |
|---|---|---|---|---|---|---|
| S0 (lancement) | 40 (zip repackagé, pas de relevé fait le matin même → sous-estimé) · ancien exe nu : 20+ avant retrait | 1 (FD, rapport complet reçu 2026-07-30) | 1 | 2 (roms multi-lecteur + score trompeur) | — | — |

*Incidents d'anonymisation (doit rester 0) :* aucun.

## 2026-07-29 · Point téléchargements (KPI #4)
- 40 téléchargements sur `PincabToolbox.zip` (le build repackagé, exe+profiles+DemoData). L'ancien exe nu (avant le fix packaging du matin) avait déjà dépassé 20 téléchargements. Relevé fait en fin de journée, pas de comptage le matin même → le total réel de la journée est probablement plus haut. Bon signal de traction J+1.
- **Décision Maxime : on améliore le scanner demain** (pas ce soir) — cohérent avec la règle de lancement (on consigne les 48h critiques, on ne code pas dans le money time). Chantiers prêts pour demain, par ordre de priorité déjà dégagé dans ce log : (1) FP `ROM_MISSING` sur tables originales/homebrew (KPI #1, priorité haute), (2) nettoyage `PinUpDisplay.exe` zombie, (3) action Repair audio par défaut (à la demande, décidé), (4) détection informative ordre des écrans + FAQ, (5) reste du backlog §2.
