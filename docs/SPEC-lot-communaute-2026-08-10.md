# SPEC — Lot « retours communauté » (recherche GPT+Gemini du 10/08/2026)

**Auteur de la spec** : session Opus du 10/08, à partir du document de recherche externe fourni par Maxime.
**Destinataire** : session Sonnet (effort élevé) qui code et câble.
**Statut** : prêt à coder pour le §5. Le §4 contient des décisions Maxime encore ouvertes — ne pas les deviner.

---

## §0 — Comment lire ce document

Le document de recherche source contient ~90 « besoins » numérotés, produits par GPT+Gemini à partir de
VPForums, VPUniverse, Reddit, Pincab Passion et GitHub. **Il n'a pas été repris tel quel.** Chaque item a
été passé au filtre suivant, dans cet ordre :

1. **Est-ce déjà codé ?** → voir §2 (carte anti-doublon). ~40 % du document décrit des choses qui existent déjà.
2. **La détection est-elle déterministe ?** (un fait vérifiable, pas une inférence) → sinon, palier `Note` ou rejet.
3. **Le signal terrain est-il réel ?** → nombre de discussions indépendantes, dates, communautés distinctes.
4. **Est-ce dans le périmètre ?** (pas de téléchargement de contenu protégé, pas de patch du moteur VPX)

Ce qui a survécu est en §5 (à coder maintenant) et §6 (backlog specifié). Ce qui a été rejeté est en §7,
**avec la raison** — pour ne pas avoir à re-débattre dans six semaines.

> ⚠️ **Le document source est une recherche assistée par IA, pas une vérification terrain.** Il admet
> lui-même ne pas avoir pu valider tous ses liens (« je ne peux pas garantir ce niveau de validation »).
> Les citations et identifiants techniques repris ci-dessous ont donc été **recoupés avec le code réel du
> projet** quand c'était possible — c'est indiqué à chaque fois. Aucun identifiant non recoupé n'ouvre un
> finding de sévérité supérieure à `Note`.

---

## §1 — Corrections factuelles au handoff précédent

**À corriger dans `TRANSMISSION.md` : `FLEXDMD_MISSING` n'est PAS une chaîne morte.**

Le prompt de reprise du 10/08 affirmait, « vérifié par lecture du code », que `FLEXDMD_MISSING` existait
dans `Loc.cs` sans être câblé à aucun scanner. **C'est faux** : `DependencyScanner.cs` ligne 80 l'émet en
`Severity.Warning`, à partir d'un signal composite déjà correct (un script de table fait
`CreateObject("FlexDMD…")` ET aucun binaire de rôle `flexdmd` n'existe sous l'install).

Conséquences directes :

- **Le « chantier FlexDMD » de Gregg est déjà fait à moitié**, et la moitié faite est la bonne moitié
  (détection de l'absence pure). Ne pas la re-coder.
- **Ce qui manque réellement sur FlexDMD n'est pas l'absence du fichier, c'est l'état de son
  enregistrement COM et la cohérence de sa version/architecture** — ce que le document de recherche
  confirme massivement (items 71, 72, 152, 153, 39, et le P0 de la consolidation finale). C'est l'objet
  du **LOT A** ci-dessous.
- La « spec complète du 08/08 » introuvable sur disque n'a plus besoin d'être retrouvée : ce document la
  remplace, avec en plus la recherche primaire-source faite le 10/08 (`Set FlexDMD =
  CreateObject("FlexDMD.FlexDMD")` confirmé par le tutoriel VPForums « Add a flexDMD to EM tables » et
  par `flexdmd/docs/JPSalas.md` du dépôt officiel `vbousquet/flexdmd`).

---

## §2 — Carte anti-doublon (à lire AVANT de coder quoi que ce soit)

26 scanners sont déjà câblés. Voici ce que le document de recherche redemande **et qui existe déjà** :

| Besoin du document | Déjà couvert par | Reste-t-il un trou ? |
|---|---|---|
| DLL bloquée Windows / Zone.Identifier (#28, #50) | `BlockedFileScanner` → `BLOCKED_DLL` | **Oui** : ne regarde que `*.dll`. Les sources citent `VPinballX.exe` bloqué. → LOT E |
| ScreenRes.txt hors écran (#4, #69) | `ScreenTopologyScanner` → `DISPLAY_OFFSCREEN` | **Oui** : exige le marqueur `# V2`, sinon silence total. → LOT F |
| x86/x64 incohérent (#2, #17) | `BitnessScanner` → `BITNESS_*` | **Oui, énorme** : le code dit lui-même qu'il « liste ce que vous avez » sans vérifier l'appairage B2S/FlexDMD. → LOT B |
| AltColor incomplet (#9) | `AltColorScanner` → `ALTCOLOR_INCOMPLETE` | **Oui** : ne vérifie jamais si la colorisation est *activée*. → LOT D |
| AltSound échantillons manquants (#8) | `AltSoundScanner` → `ALTSOUND_SAMPLE_MISSING` | **Oui** : ne vérifie ni l'emplacement ni le mode d'activation. → LOT D |
| FlexDMD absent (#152 partiel) | `DependencyScanner` → `FLEXDMD_MISSING` | **Oui** : « Does not check COM registration itself ». → LOT A |
| B2S absent (#151 partiel) | `DependencyScanner` → `B2S_SERVER_MISSING` | idem → LOT A |
| .directb2s orphelin / mauvais nom (#11, #70) | `CompletenessScanner` → `B2S_ORPHAN` | **Non, couvert.** Ne pas re-coder. |
| .directb2s malformé | `DirectB2sScanner` → `B2S_MALFORMED` | **Non, couvert.** |
| NVRAM corrompue (#120) | `NvramScanner` → `NVRAM_EMPTY` | **Oui** : 0 octet seulement, pas les permissions. → LOT G |
| Playlists Popper orphelines | `PopperPlaylistScanner` → `POPPER_ORPHAN_PLAYLIST` | **Non, couvert.** |
| Port COM DMD introuvable | `DmdComPortScanner` → `DMD_COM_PORT_NOT_FOUND` | Couvert pour le port. Le reste de `dmddevice.ini` non → LOT C |
| Tables .vpt legacy | `LegacyTableScanner` → `VPT_LEGACY_PRESENT` | **Non, couvert.** |
| Espace disque | `DiskSpaceScanner` → `LOW_DISK_SPACE` | **Non, couvert.** |
| VPinMAME.ini vs registre | `ConfigPhantomScanner` → `VPINMAME_CONFIG_PHANTOM` | Couvert pour la coexistence. « VPinMAME jamais enregistré » non → LOT A |
| Médias Popper orphelins | `OrphanedMediaScanner` | **Non, couvert.** |
| Version VPX vs table | `VpxVersionScanner` → `VPX_VERSION_OUTDATED` | **Non, couvert.** |

**Infrastructure réutilisable qui existe déjà** (ne rien réécrire) :
`PeInspector.GetBitness` (architecture d'un PE) · `DmdDeviceIniParser` (parseur INI DMD) ·
`SqliteReader` (lecture seule PUPDatabase) · `VpinmameRegistry` / `SerialPortRegistry` / `DpiRegistry`
(précédents de lecture registre) · `ScriptAnalyzer.AnalyzeRomUsage` (ROM requise + `UsesController` +
`UsesB2S`, commentaires déjà strippés) · `LayoutDetector.FindFilesByPattern` (recherche bornée sans
exception) · `MonitorTopologyProbe.TryGetMonitorRects` (géométrie écrans) · `Profile.BinaryRoles`
(patterns de fichiers pilotés par le profil, jamais en dur).

---

## §3 — Doctrine applicable à ce lot

### §3.0 — Principe fondateur (posé par Maxime le 10/08, prioritaire sur tout le reste de ce §3)

> « Les gens ont de vrais problèmes, faut pas les ignorer parce qu'ils sont tout seuls. »

**La règle des deux signaux indépendants ne doit JAMAIS servir à écarter quelqu'un.** Elle a été écrite
pour éviter de transformer une anecdote en check universel qui se déclenche chez 500 personnes — pas pour
décider qui mérite d'être écouté. Ce sont deux moments différents, et les confondre est une faute.

Sur un produit comme celui-ci, la règle brute est même contre-productive : la communauté pincab est
petite, chaque cab est une construction unique, et **une grande partie des vrais problèmes ne seront
jamais signalés deux fois.** Attendre un deuxième témoin, c'est garantir que la première personne à
rencontrer un problème n'obtient rien. L'utilisateur terrain du 10/08 l'a dit lui-même : *« these types
of tools benefit from real world one off situations »*.

**Doctrine corrigée : le nombre de signaux ne décide pas SI on construit, il décide de la SÉVÉRITÉ.**

| Ce qu'on a | Ce qu'on fait |
|---|---|
| Une personne, un problème réel, un mécanisme qu'on sait mesurer | **On code le check**, il entre en `Note` |
| Deux signaux indépendants, ou un mécanisme certain et vérifiable | `Warning` |
| Mécanisme certain **et** impact total (rien ne fonctionne) | `Critical`, conditions conjointes obligatoires |
| Problème réel mais mécanisme non déterminable | **On ne rejette pas la personne**, on consigne en FIELD-LOG et on lui répond ; le check attend d'être cadrable |
| Hors périmètre (illégal, moteur VPX) ou déjà corrigé en amont | Rejet, avec la raison |

**Un seul motif de rejet est interdit : « une seule personne l'a signalé ».** Si le mécanisme est
mesurable, on construit, en `Note`. Le palier `Note` (ADR-010) existe exactement pour ça : il ne bouge
jamais le score et ne déclenche jamais « FIX THIS FIRST », donc un check à un seul signal ne coûte rien
s'il se trompe, et aide la personne concernée s'il a raison. C'est le meilleur des deux mondes, et c'est
la raison d'être de ce palier.

**Ce qui ne se relâche pas, et pourquoi.** La règle « jamais de faux positif accepté sciemment » reste
entière — mais pas par purisme. Un faux positif, c'est **aussi** ignorer le vrai problème de quelqu'un :
on envoie une personne réparer ce qui n'est pas cassé, ou pire, supprimer un fichier qui marchait
(l'incident Draken06 du FIELD-LOG : un script communautaire a effacé des vidéos encore utilisées à cause
d'une heuristique de nommage trop confiante). Écouter tout le monde et ne pas deviner ne s'opposent pas,
c'est la même exigence appliquée à deux moments différents : large à l'entrée, rigoureux à la sortie.

---

### §3.1 — Règles d'implémentation

**Le Scanner reste gelé (03/08) sauf sur ce lot précis.** Maxime a donné le feu vert le 10/08 (« chaque
signal utilisateur compte », « les gens demandent, on fait, si c'est possible et légal »). Ce feu vert
lève la règle des deux signaux **pour l'entrée** — il ne lève **pas** la règle sur les faux positifs.
Concrètement, pour ce lot :

1. **Aucun nouveau `Critical` sans quatre conditions déterministes conjointes.** Un `Critical` est
   non-groupable (`ScanReport.Rolled`) et coûte −15 au score sans plafond. Un faux `Critical` est le seul
   bug de ce projet qui a déjà coûté de la crédibilité en public (incident du 30/07).
2. **Le palier `Note` (ADR-010) est le défaut pour tout ce qui est heuristique.** `Note` ne bouge jamais
   le score et ne déclenche jamais « FIX THIS FIRST ». C'est exactement le véhicule qui permet d'honorer
   « chaque signal compte » sans risquer un faux positif coûteux : on dit ce qu'on observe, l'utilisateur
   tranche.
3. **Illisible = silence, jamais « cassé ».** Un `try/catch` qui échoue ne produit aucun finding. Cette
   règle est déjà appliquée à l'identique dans les 26 scanners existants ; la respecter mot pour mot.
4. **Aucun identifiant inventé.** Si une clé de registre, un nom de fichier ou un nom de section n'est pas
   confirmé par une source primaire ou par le code existant, on ne le teste pas — ou on le teste de façon
   tolérante et on reste silencieux si rien ne matche (précédent : les 4 variantes de clé port COM).
5. **Ne pas modifier les scanners existants** sauf mention explicite ici. Les nouveaux checks arrivent
   dans de **nouveaux** scanners, qui consomment les mêmes services.

---

## §4 — Décisions Maxime — TRANCHÉES le 10/08

**D-1 — Chemin d'écriture de Repair : ✅ À CÂBLER, dans cette même session.**
`Preflight` / `Apply` / `Undo` n'avaient jamais été appelés depuis l'App (décision suspendue depuis le
HANDOFF du 27/07). Maxime tranche : on les câble. **C'est le changement le plus important et le plus
risqué de l'histoire du projet — la première fois que Pincab Toolbox écrit réellement sur la machine d'un
utilisateur.** Il a donc sa propre spec complète et ses propres garde-fous : **§5 LOT H**, à lire
intégralement avant d'écrire une ligne. Ne pas traiter ce point comme une simple case à cocher.

**D-2 — Ré-enregistrement COM : ✅ via les outils du composant, jamais par écriture registre directe.**
On n'écrit jamais nous-mêmes dans `HKCR`. On exécute l'outil d'enregistrement officiel **déjà présent dans
l'install scannée** (`FlexDMDUI.exe`, `B2SBackglassServerRegisterApp.exe`, `Setup.exe` de VPinMAME). Ça
reste dans la racine confinée d'ADR-005, ça n'invente aucun CLSID, et c'est la procédure que la communauté
applique déjà. **Attention : exécuter un processus externe est une classe de capacité entièrement nouvelle
pour ce produit** — règles de confinement spécifiques en **§5 LOT I**.

**D-3 — `VPINMAME_NOT_REGISTERED` : ✅ `Critical`.**
Premier `Critical` ajouté depuis le gel du 03/08. Justifié : les quatre conditions sont déterministes et
l'impact est total (aucune table ROM ne démarre). **Contrepartie non négociable** : si l'une des quatre
conditions n'est pas mesurable, le finding n'est pas émis du tout. Un `Critical` n'est jamais émis « dans
le doute » — voir LOT A.3.

**D-4 — Périmètre : ✅ tout le sprint 1 (LOT A → G), plus H et I.**

> **Avis d'ingénierie, à lire par la session Sonnet.** Le périmètre validé est large : 7 lots de détection
> + le câblage du chemin d'écriture + une nouvelle classe de capacité. **Si le temps manque, l'ordre
> d'abandon est G, F, E, D, C — dans cet ordre.** Ce qui ne doit JAMAIS être livré à moitié, c'est le
> LOT H : un `Apply` sans journal persistant, sans `Preflight` bloquant ou sans `Undo` accessible depuis
> l'interface est pire que pas d'`Apply` du tout. **Livrer H entièrement, ou ne pas le livrer.**

---

## §5 — SPRINT 1 (à coder)

### LOT A — Santé des enregistrements COM  ⭐ pièce maîtresse

**Pourquoi c'est le lot #1.** C'est le thème dominant de toute la recherche : P0 dans les cinq tableaux de
synthèse du document, présent sur VPForums *et* VPUniverse *et* Reddit, avec des occurrences continues de
2021 à janvier 2026. Symptômes utilisateur : « ActiveX component can't create object », « Library not
registered. (Exception from HRESULT: 0x8002801D) », « Registered FlexDMD does not match your install
path », « I had multiple instances from old installs ». **Et rien dans les 26 scanners actuels ne lit un
seul enregistrement COM.** Détection 100 % déterministe, zéro heuristique.

#### A.1 — Nouveau service `ComRegistrationProbe` (Core/Services)

Lecture seule du registre. Aucune écriture, jamais.

```
TryResolve(progId, RegistryView view) -> ComRegistration?
```

Chaîne de résolution, strictement celle de Windows :
1. `HKEY_CLASSES_ROOT\<progId>\CLSID` → valeur par défaut = `{GUID}`
2. `HKEY_CLASSES_ROOT\CLSID\{GUID}\InprocServer32` → valeur par défaut = chemin du serveur
   (si absent, essayer `LocalServer32` — les deux existent selon le composant)
3. Retourner `{ ProgId, Clsid, ServerPath, View }`, ou `null` si un maillon manque.

**Point technique critique — les deux vues du registre.** Sur un Windows 64 bits, un composant COM 32 bits
s'enregistre dans une arborescence séparée (`Wow6432Node`). Un composant peut donc être parfaitement
enregistré en 64 bits et totalement absent en 32 bits — **et c'est précisément la cause racine du P0 n°2
de la recherche** (« 64 bit and 32 bit are different "ecosystems" », ≥5 discussions indépendantes). Il
faut donc lire **les deux vues séparément** :

```csharp
RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32)
RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64)
```

Ne jamais utiliser `Registry.ClassesRoot` tout court : la vue obtenue dépendrait de l'architecture du
processus appelant, ce qui rendrait le résultat du scan dépendant de comment l'app a été compilée. C'est
le piège n°1 de ce lot.

Toute exception (accès refusé, ruche absente, non-Windows) → retourner `null`. `null` signifie
**« je n'ai pas pu savoir »**, et ne doit jamais être rendu comme « non enregistré » (voir A.2).

#### A.2 — Nouveau scanner `ComHealthScanner` (Id = `"com"`)

Composants examinés — **ProgIDs, tous recoupés** :

| ProgID | Rôle profil | Niveau de preuve |
|---|---|---|
| `VPinMAME.Controller` | `vpinmame` | **Confirmé par le code du projet** (regex de `ScriptAnalyzer`) |
| `B2S.Server` | `b2s` | **Confirmé par le code du projet** (regex de `ScriptAnalyzer` + `DependencyScanner`) |
| `FlexDMD.FlexDMD` | `flexdmd` | **Confirmé source primaire** (dépôt officiel `vbousquet/flexdmd`, doc `JPSalas.md` + tutoriel VPForums) |
| `PinUpPlayer.PinDisplay` | — | Source unique (recherche externe, item 154) → **`Note` uniquement, jamais plus** |

Pour chaque composant, le scanner croise **trois** dimensions déjà disponibles ailleurs dans le projet :

- **Requis ?** — un script de table le demande. Réutiliser `ScriptAnalyzer.AnalyzeRomUsage` (`UsesController`,
  `UsesB2S`) et, pour FlexDMD, la signature `flexdmd` du profil. Ne pas ré-écrire de regex.
- **Présent ?** — le binaire existe sous la racine scannée. Réutiliser `Profile.BinaryRoles` +
  `LayoutDetector.FindFilesByPattern` (exactement comme `DependencyScanner`).
- **Enregistré ?** — `ComRegistrationProbe`, dans les deux vues.

**Findings, avec leur logique exacte :**

| Code | Sévérité | Condition (toutes les clauses obligatoires) |
|---|---|---|
| `COM_NOT_REGISTERED` | `Warning` | lecture registre réussie ET ProgID absent des **deux** vues ET binaire **présent** sous la racine scannée ET au moins une table le requiert |
| `COM_STALE_PATH` | `Warning` | enregistrement trouvé ET `File.Exists(serverPath) == false` |
| `COM_PATH_OUTSIDE_INSTALL` | **`Note`** | enregistré vers un chemin qui existe, hors racine scannée, ALORS QUE le même binaire existe aussi dans la racine scannée |
| `COM_BITNESS_GAP` | `Warning` | un VPX de bitness B est installé ET le ProgID est absent de la vue correspondant à B ET présent dans l'autre vue |
| `COM_OK` | `Ok` | enregistré, chemin existant, dans la racine scannée |

Justification des sévérités, à respecter :

- `COM_PATH_OUTSIDE_INSTALL` est **`Note` et pas `Warning`** parce qu'avoir plusieurs installs VPX est
  parfaitement légitime, et rien ne nous dit laquelle l'utilisateur lance réellement. C'est le cas d'école
  d'ADR-010 : on énonce le fait (« le FlexDMD enregistré n'est pas celui de ce dossier »), on ne rend pas
  de verdict. Le mettre en `Warning` produirait du bruit chez tous les utilisateurs multi-install.
- `COM_BITNESS_GAP` est le finding qui répond au P0 n°2. Il n'est émis que si on a **mesuré** la bitness
  d'un VPX réellement installé (via `PeInspector`, jamais deviné) — si `Bitness.Unknown`, silence.
- Aucun `Critical` dans ce tableau. Le seul candidat `Critical` est isolé en A.3 parce qu'il demande un
  arbitrage.

**Règles de silence, non négociables :** registre illisible → aucun finding pour ce composant (jamais
« non enregistré »). Binaire absent ET non requis → silence total. Binaire absent MAIS requis → **ne rien
émettre ici**, `DependencyScanner` le dit déjà (`FLEXDMD_MISSING` / `B2S_SERVER_MISSING`) — ne pas doubler
le rapport.

#### A.3 — `VPINMAME_NOT_REGISTERED` (décision D-3)

Cas le plus fréquent de tout le document (« Setup.exe was never executed », signalé depuis le début des
années 2010, encore en 2025) : l'utilisateur copie une install VPX à la main, n'exécute jamais le
`Setup.exe` de VPinMAME, et **aucune table ROM ne démarre** alors que les ROMs sont là.

Quatre conditions conjointes, toutes déterministes :
1. `VPinMAME.dll` (ou `VPinMAME64.dll`) présent sous la racine scannée
2. `ComRegistrationProbe` a **réussi** sa lecture (pas d'exception)
3. `VPinMAME.Controller` absent des deux vues
4. Au moins une table scannée a `UsesController == true`

Si les quatre sont vraies, l'install est objectivement cassée pour toutes les tables ROM.
**Sévérité : `Critical` — tranché par Maxime le 10/08 (D-3).**

**Contrepartie obligatoire de ce `Critical`** (c'est le premier depuis le gel du 03/08, il doit être
irréprochable) : chacune des quatre conditions doit être **mesurée**, jamais supposée. En particulier, la
condition 2 est la plus importante : si `ComRegistrationProbe` lève une exception, retourne `null` par
échec d'accès, ou tourne hors Windows, **le finding n'est pas émis du tout** — on ne dégrade pas en
`Warning`, on se tait. Un `Critical` non groupable qui apparaîtrait sur une simple erreur de lecture de
registre serait exactement le scénario du 30/07, en pire. Écrire le test unitaire de ce cas **en premier**,
avant l'implémentation.

Note d'implémentation : `ConfigPhantomScanner` utilise déjà `VpinmameKeyProbe.KeyExists` sur
`HKCU\Software\Freeware\Visual PinMame`. **Ce n'est pas la même chose** qu'un enregistrement COM — cette
clé-là stocke la config, pas la registration. Ne pas confondre les deux ; ce sont deux signaux distincts.

#### A.4 — Tests attendus

`ComRegistrationProbe` n'est pas testable hors Windows → **extraire la décision en pur**, exactement comme
`BlockedFileScanner.SeverityFor` / `DisplaySetupScanner.Evaluate` l'ont fait :

```csharp
public static Finding? Evaluate(ComRegistration? view32, ComRegistration? view64,
                                bool probeSucceeded, bool binaryPresentUnderRoot,
                                string? binaryPathUnderRoot, bool requiredByATable,
                                IReadOnlyList<Bitness> installedVpxBitnesses, string category)
```

Cas de test obligatoires (minimum) : sonde en échec → `null` quel que soit le reste (le test qui protège
contre le faux positif le plus coûteux) · non enregistré + requis + présent → finding · non enregistré +
**non** requis → `null` · enregistré vers chemin inexistant → `COM_STALE_PATH` · enregistré hors racine
avec copie locale → `Note` · enregistré hors racine **sans** copie locale → `null` (multi-install
légitime, rien à dire) · VPX 64 + registration 32 seule → `COM_BITNESS_GAP` · VPX de bitness inconnue →
`null`.

---

### LOT B — Cohérence x86/x64 de la chaîne complète

**Signal** : P0, ≥5 discussions indépendantes Reddit+VPUniverse, 2023→avril 2025. Citation :
« 64 bit and 32 bit are different "ecosystems" », « there's about 10 steps you could have missed ».

**Le trou exact.** `BitnessScanner` croise déjà `main-exe` × `vpinmame` × `vpinmame64` × `dmddevice` ×
`dmddevice64`. Son propre texte `BITNESS_HYBRID_INSTALL` énonce la limite : *« every plugin (dmddevice,
B2S, FlexDMD) must exist in BOTH bitnesses — this scan lists what you have »*. Il **liste** sans
**vérifier**. B2S, FlexDMD et PinUP Player ne sont jamais appairés.

**Nouveau scanner `ChainBitnessScanner`** (Id = `"chain-bitness"`) — ne pas modifier `BitnessScanner`.

Pour chaque bitness de VPX réellement installée (mesurée par `PeInspector`, jamais déduite d'un nom de
fichier), vérifier que chaque composant **effectivement utilisé par au moins une table** existe dans cette
bitness :

| Code | Sévérité | Condition |
|---|---|---|
| `CHAIN_BITNESS_GAP` | `Warning` | VPX de bitness B installé ET composant requis par ≥1 table ET aucun binaire de ce rôle en bitness B trouvé sous la racine |
| `CHAIN_BITNESS_UNKNOWN` | — | **ne pas émettre** — si la bitness d'un fichier n'est pas lisible, silence |

Un seul finding agrégé par bitness manquante (pas un par table) : le rapport doit orienter, pas inonder.

**Piège à éviter** : ne pas conclure « manquant » à partir d'un nom de fichier. `dmddevice64.dll` *devrait*
être 64 bits, mais c'est `PeInspector` qui doit le confirmer. Le projet a déjà ce réflexe partout, le
garder.

---

### LOT C — Sémantique de `dmddevice.ini`

**Signal** : deux findings distincts, chacun ≥3 discussions indépendantes, jusqu'à janvier 2026.

`DmdDeviceIniParser` existe déjà (utilisé par `DmdComPortScanner` pour les ports COM). L'étendre —
**c'est un service, pas un scanner**, donc l'étendre ne viole pas la règle « ne pas toucher aux scanners
existants ».

**C.1 — `DMD_VIRTUAL_DISABLED` (`Note`)**
Citations : « VirtualDMD was set to false in my ini », « IT WAS SET TO FALSE BY DEFAULT! ».
Une mise à jour de Freezy réécrit `dmddevice.ini` et repasse le DMD virtuel à `false` ; le DMD disparaît
alors que tout le reste marche, ce qui envoie l'utilisateur chercher au mauvais endroit pendant des heures.

Condition : `dmddevice.ini` lisible ET section `[virtualdmd]` présente ET `enabled = false` ET **aucun**
périphérique DMD matériel activé par ailleurs dans le même fichier (sinon c'est une désactivation
parfaitement volontaire — cab avec vrai DMD).

**`Note`, jamais `Warning`** : désactiver le DMD virtuel est un choix légitime sur un cab à DMD physique.
La clause « aucun DMD matériel activé » réduit énormément le risque, mais ne l'annule pas — d'où `Note`.
C'est la sévérité que le document de recherche recommande lui aussi (« il faut éviter de modifier
automatiquement une configuration volontairement désactivée »).

**C.2 — `DMD_POSITION_OFFSCREEN` (`Warning`)**
Citation : « DMDdevice.ini had off screen positions set ».
Condition : positions lisibles dans `dmddevice.ini` ET géométrie des écrans lisible
(`MonitorTopologyProbe.TryGetMonitorRects`) ET le rectangle DMD n'intersecte **aucun** moniteur.

⚠️ **Piège explicitement signalé par la recherche, à respecter** : *des coordonnées négatives ne sont pas
une erreur*. Windows autorise un écran à gauche ou au-dessus du moniteur principal, ce qui donne des
coordonnées négatives parfaitement valides. Le test doit être « intersecte-t-il un moniteur réel »,
**jamais** « x < 0 ou y < 0 ». Ce seul contresens produirait un faux positif chez tous les cabs
multi-écrans correctement configurés.

`ScreenTopologyScanner` fait déjà ce raisonnement pour `ScreenRes.txt` et déclare `dmddevice.ini`
explicitement hors périmètre — donc réutiliser sa logique `IsOffScreen`, pas la réécrire.

Si la géométrie des écrans n'est pas lisible → silence (comme `ScreenTopologyScanner`).

---

### LOT D — « Présent mais pas activé » (AltSound / AltColor)

**Signal** : P1, plusieurs discussions VPUniverse, 2021→2025. Citation : « change the Alt Sound Mode (0-3)
from 0 to 1 ».

**Le trou.** `AltSoundScanner` et `AltColorScanner` vérifient tous deux que les *fichiers* sont corrects.
Ni l'un ni l'autre ne regarde si la fonctionnalité est **activée** dans VPinMAME. Résultat : un
utilisateur qui a correctement installé son AltSound obtient un rapport entièrement vert et n'entend
toujours rien. C'est un faux négatif frustrant, sur une install *bien faite*.

**Nouveau scanner `FeatureEnabledScanner`** (Id = `"feature-enabled"`).

| Code | Sévérité | Condition |
|---|---|---|
| `ALTSOUND_PRESENT_NOT_ENABLED` | `Note` | dossier `VPinMAME\altsound\<rom>` présent et non vide ET le réglage d'activation AltSound est lisible ET vaut « désactivé » |
| `ALTCOLOR_PRESENT_NOT_ENABLED` | `Note` | dossier `VPinMAME\altcolor\<rom>` présent et complet (réutiliser `AltColorInspector.IsComplete`) ET le réglage de colorisation est lisible ET vaut « désactivé » |

⚠️ **Blocage d'implémentation à traiter honnêtement.** Le document de recherche parle du « paramètre
Alt Sound Mode (0-3) » et de l'option « Use external DMD colors » **sans jamais donner le nom exact de la
valeur de registre**. Le projet lit déjà la ruche VPinMAME (`VpinmameRegistry.TryGetRomPath` sur
`HKCU\Software\Freeware\Visual PinMame\globals`), donc l'emplacement général est connu, mais **le nom
exact de la valeur ne l'est pas**.

Conduite à tenir, dans l'ordre :
1. **Chercher la source primaire** (dépôt `vpinball/pinmame`, dossier de la GUI VPinMAME, ou un
   `.reg`/capture réel) avant d'écrire la moindre clé.
2. Si le nom exact n'est pas confirmé : appliquer le **précédent du port COM DMD** — accepter plusieurs
   noms candidats, et **rester totalement silencieux si aucun ne matche**. Ne jamais supposer
   « absent = désactivé » : une valeur absente signifie « je ne sais pas », donc silence.
3. Si même ça n'est pas faisable proprement : **livrer le LOT D sans ces deux findings** et consigner le
   trou dans FIELD-LOG, comme `AltSoundScanner` l'a déjà fait pour le format `g-sound`. Un lot amputé et
   honnête vaut mieux qu'un finding deviné.

Les deux findings sont en `Note` : l'utilisateur peut légitimement avoir installé un pack de colorisation
sans vouloir l'activer tout de suite.

---

### LOT E — Fichiers bloqués par Windows au-delà des DLL  *(petit lot, gros rapport)*

**Signal** : P0/P1, plusieurs communautés, 2020→avril 2025. Citation : « I have to go in and unblock a
bunch of the files one by one ».

`BlockedFileScanner` ne lit `Zone.Identifier` que sur `*.dll`. Les sources citent explicitement des
**exécutables** bloqués (`VPinballX.exe`). Un `.exe` bloqué produit exactement le même symptôme opaque.

**C'est le seul lot qui touche un scanner EXISTANT.** Sous la règle du 10/08, c'est autorisé, mais à faire
avec un minimum de risque :

- Étendre la collecte de `*.dll` à `*.dll` + `*.exe` + `*.ocx`.
- **Ne PAS étendre la liste `CriticalNames`** : un `.exe` bloqué reste `Warning`. Élargir la surface *et*
  la sévérité en même temps est exactement comme ça qu'on fabrique un pic de faux `Critical`.
- Ne rien changer d'autre : la marche dossier-par-dossier, les zones 0–2 non bloquées, le split manuel
  `/`/`\`, le silence sur flux illisible restent identiques.
- Vérifier le coût : la marche est déjà en `int.MaxValue` de profondeur sur toute la racine ; on triple le
  nombre de fichiers ouverts. **Mesurer sur une vraie install** avant de considérer le lot fini
  (et a fortiori maintenant que le scan peut porter sur un disque entier, cf. ADR-011).

**Distinction à préserver** (la recherche insiste, à raison) : « fichier bloqué par Mark-of-the-Web » et
« fichier supprimé par Windows Defender » sont **deux mécanismes différents**. Le premier est détectable
et réparable ; le second n'est ni l'un ni l'autre de façon fiable. Ne pas les fusionner dans un même
finding.

---

### LOT F — `ScreenRes.txt` sans marqueur `# V2`  *(petit lot)*

`ScreenTopologyScanner` exige le marqueur `# V2` et reste **totalement silencieux** sans lui. Or la
recherche montre ≥5 discussions sur des `ScreenRes.txt` cassés, jusqu'à juillet 2026 — dont beaucoup sur
des installs anciennes, précisément celles qui n'ont pas le marqueur.

Ajouter un finding **`Note`** unique : `SCREENRES_UNPARSED` — « un `ScreenRes.txt` est présent mais n'a pas
le format que je sais vérifier, je ne me prononce pas sur son contenu ». Aucune analyse de son contenu,
aucun verdict. Ça transforme un silence total (l'utilisateur croit que c'est vérifié) en une information
honnête sur les limites de l'outil.

Ne **pas** tenter de parser l'ancien format sans source primaire décrivant sa grammaire.

---

### LOT G — Dossiers NVRAM / cfg non inscriptibles  *(petit lot)*

**Signal** : P2, discussions historiques, une seule citation directe (« the nvram folder was set to read
only ») — signal réel mais faible, la recherche elle-même ne le classe pas P0.

`NvramScanner` ne détecte que les `.nv` de 0 octet. Un dossier en lecture seule produit un symptôme
différent : les scores et réglages ne se sauvegardent jamais, silencieusement.

`NVRAM_FOLDER_NOT_WRITABLE` (`Warning`) — test d'écriture réel (créer puis supprimer un fichier temporaire
dans le dossier), **pas** une lecture d'ACL : les ACL Windows sont trop subtiles pour en déduire un verdict
fiable, alors qu'un test d'écriture est un fait.

⚠️ **Rejet explicite** : la recherche mentionne une réparation consistant à donner « Full Control au groupe
Users » sur le dossier. **Ne pas coder ça**, et le document source le rejette lui-même. Modifier des ACL
Windows est hors périmètre, difficilement réversible, et potentiellement un problème de sécurité.
Détection seulement.

---

### LOT H — Câblage du chemin d'écriture Repair (Preflight / Apply / Undo)  🔴 lot critique

> **Lire cette section en entier avant d'écrire une ligne.** C'est la première fois que ce produit
> modifiera réellement la machine d'un utilisateur. Jusqu'ici, tout — absolument tout — était en lecture
> seule, et c'est la promesse affichée sur la landing et dans l'onglet « à propos ». Le jour où `Apply`
> existe, la moindre erreur ne produit plus un faux positif dans un rapport : elle produit un fichier
> perdu chez quelqu'un.
>
> **Règle d'or : livrer ce lot entièrement, ou pas du tout.** Un `Apply` sans journal persistant, sans
> `Preflight` bloquant ou sans `Undo` atteignable depuis l'interface est plus dangereux que l'absence
> d'`Apply`.

#### H.0 — À lire dans le code avant de coder

Cette spec **ne suppose pas** les signatures exactes de `Preflight` / `Apply` / `Undo`. Elles n'ont pas
été relues en écrivant ce document. Ouvrir `PincabToolbox.Repair/RepairEngine.cs` et les lire d'abord, puis
câbler ce qui existe réellement. Ne pas inventer une API qui « devrait » exister.

Contexte déjà en place, à réutiliser tel quel : `RepairActionRegistry` (5 actions codées, une non
enregistrée) · `FileBackupService` · `RealEnvironmentProbe` (processus bloquants) · `RealFileSystem` ·
la liste de racines de confinement — **qui prend désormais un paramètre explicite** depuis le 10/08
(`RepairOfferBuilder.Build(report, confinementRoots)`, ADR-011) : en scan disque entier, ce sont les
vraies racines par install, jamais `C:\`.

#### H.1 — Bloqueur n°1 : le journal doit devenir persistant

`RepairOfferBuilder` construit aujourd'hui le moteur avec `new InMemoryRepairJournal()`. C'était sans
conséquence tant que seul `Plan()` tournait. **Avec `Apply`, un journal en mémoire signifie qu'`Undo`
devient impossible dès que l'application est fermée** — l'utilisateur applique une réparation, ferme
l'app, et n'a plus aucun moyen de revenir en arrière. C'est inacceptable et c'est le premier travail du
lot.

Exigences :

- Journal **écrit sur disque au fur et à mesure**, pas en fin de plan. Si l'app est tuée au milieu d'un
  `Apply`, ce qui a déjà été fait doit être retrouvable au prochain démarrage.
- Emplacement cohérent avec l'existant : à côté de `%APPDATA%\PincabToolbox\repair-backups`.
- Chaque entrée doit permettre de reconstituer l'annulation : quelle action, quelle cible, quel backup,
  quand, résultat. Le journal est ce qui rend `Undo` possible — pas un log de confort.
- Format lisible (JSON), pour qu'un utilisateur bloqué puisse être dépanné à distance en lisant le
  fichier. `System.Text.Json` est déjà utilisé, pas de dépendance nouvelle.
- Un journal illisible ou corrompu ne doit jamais faire planter l'app ni bloquer un scan : il dégrade en
  « pas d'historique disponible », comme `KnowledgePack.Empty` le fait déjà pour le pack.

#### H.2 — Séquence obligatoire

`Plan` → `Preflight` → **confirmation utilisateur explicite** → `Apply` → journal → `Undo` disponible.

Aucune étape n'est optionnelle, aucune n'est court-circuitable :

1. **`Preflight` est bloquant.** Si le preflight échoue ou signale un environnement non sûr (processus
   bloquant en cours, cible disparue, sauvegarde impossible), `Apply` **ne s'exécute pas**. Pas de
   « continuer quand même ».
2. **Re-vérifier que le finding tient toujours.** Le scan peut dater de vingt minutes ; l'utilisateur a pu
   corriger, déplacer ou supprimer le fichier entre-temps. Appliquer une réparation sur la base d'un
   rapport périmé est un des rares moyens de casser une install *saine*. Si l'état a changé depuis le scan,
   annuler l'action proprement et le dire, plutôt que d'appliquer sur des données mortes.
3. **Confirmation action par action**, jamais un « tout réparer » silencieux. L'écran doit dire, dans le
   texte, exactement quoi : quel fichier, quel chemin, réversible ou non, sauvegarde faite ou non.
4. **Sauvegarde avant toute écriture réversible**, via `FileBackupService`. Si la sauvegarde échoue,
   l'action n'est pas appliquée.
5. **`Undo` doit être accessible depuis l'interface.** Un `Undo` qui n'existe que dans le moteur n'existe
   pas pour l'utilisateur. C'est une exigence d'UI, pas seulement de moteur.

#### H.3 — Actions non réversibles

`KillZombiePinUpDisplayAction` a `IsReversibleByNature = false` (on ne « dé-tue » pas un processus) et le
moteur interdit déjà le mode `Automatic` dans ce cas. **Cette règle doit rester vraie après le câblage.**
Toute action non réversible : confirmation explicite obligatoire, formulation qui dit clairement que
l'opération ne pourra pas être annulée, jamais de mode automatique.

#### H.4 — Licence

`Apply` est la surface payante (ADR-002 / ADR-009), et `RepairOffer.From` lève déjà une exception si on lui
passe un plan sous licence (ADR-006) — l'écran 1 gratuit doit continuer à être construit avec
`licensed: false`. Le durcissement licence ECDSA est codé depuis le 05/08 : le brancher, ne pas le
réécrire. **Un échec de vérification de licence ne doit jamais dégrader en « on applique quand même ».**

#### H.5 — Ce qui doit rester vrai après ce lot

- Le **scan** reste 100 % en lecture seule. Aucune écriture ne doit pouvoir être déclenchée par un scan,
  seulement par un geste explicite de l'utilisateur.
- Le texte de l'onglet « à propos » et la landing promettent un outil de diagnostic en lecture seule.
  **Ces textes devront être mis à jour** — même discipline que le 07/08, quand le premier appel réseau du
  projet a été accompagné d'une correction du texte qui promettait « 100 % local ». Un produit qui se met
  à écrire sans le dire perd exactement la confiance sur laquelle il est construit.
- Aucune action Repair n'écrit hors des racines de confinement. À re-vérifier explicitement après le
  changement d'ADR-011.

#### H.6 — Tests

Le moteur Repair a déjà 105 tests. Ce lot doit ajouter au minimum : preflight en échec → aucune écriture ·
sauvegarde en échec → aucune écriture · cible modifiée depuis le scan → action annulée, pas appliquée ·
`Apply` puis `Undo` → état initial restauré · `Apply` interrompu en cours → le journal permet de retrouver
ce qui a été fait · cible hors racine de confinement → refus · action non réversible → jamais `Automatic`.

**Le test le plus important est celui qui vérifie qu'on n'écrit PAS.** Ce sont ceux-là qui protègent les
utilisateurs.

---

### LOT I — Action Repair : ré-enregistrer un composant COM  (nouvelle classe de capacité)

Décision D-2 : on n'écrit jamais dans le registre nous-mêmes ; on exécute l'outil d'enregistrement fourni
par le composant, déjà présent dans l'install scannée. **Mais exécuter un processus externe est une classe
de capacité que ce produit n'a jamais eue.** Elle mérite ses propres règles, aussi strictes que le
confinement fichier d'ADR-005.

**Déclencheurs** : les findings du LOT A — `COM_NOT_REGISTERED`, `VPINMAME_NOT_REGISTERED`,
`COM_BITNESS_GAP`. Jamais `COM_PATH_OUTSIDE_INSTALL` (multi-install légitime : ré-enregistrer d'autorité
casserait le choix de l'utilisateur).

**Règles de confinement de l'exécution — toutes obligatoires :**

1. **Liste blanche stricte de noms d'exécutables**, en dur dans le code, jamais construite à partir de
   données de scan : `FlexDMDUI.exe`, `B2SBackglassServerRegisterApp.exe`, `Setup.exe` (VPinMAME).
2. L'exécutable doit se trouver **à l'intérieur d'une racine de confinement**, résolu en chemin absolu
   canonique **avant** vérification (sinon un `..\..\` dans un chemin contourne le test).
3. **Aucun argument dérivé de données de scan.** Aucun nom de fichier, aucun chemin issu du rapport ne doit
   se retrouver sur une ligne de commande. Pas de shell, pas de `cmd /c`, pas d'interpolation de chaîne :
   lancement direct du processus.
4. Vérifier que c'est bien un PE et lire sa bitness avec `PeInspector` (déjà présent) avant de le lancer.
5. **Timeout** obligatoire, et un processus qui ne rend pas la main ne doit pas figer l'application.
6. **Élévation** : enregistrer un composant COM machine demande des droits administrateur. Si l'app ne les
   a pas, le comportement attendu est de **le dire clairement** (« cette réparation demande de lancer
   l'outil en tant qu'administrateur »), pas d'échouer silencieusement ni de forcer une élévation
   surprise. Vérifier ce que déclare `app.manifest` avant de décider.
7. `IsReversibleByNature = **false**`. On ne peut pas garantir la restauration de l'enregistrement
   précédent (le chemin d'origine peut ne plus exister — c'est même souvent le cas de départ). Donc :
   confirmation explicite systématique, jamais `Automatic`, et le journal enregistre l'enregistrement
   observé avant l'opération, à titre de trace.

**Si l'un de ces points ne peut pas être tenu proprement, ne pas livrer le LOT I.** La détection du LOT A
a déjà une grande valeur seule : elle transforme un « mon backglass ne marche plus » incompréhensible en
une cause précise et une procédure connue. C'est déjà l'essentiel du bénéfice.

---

## §6 — Backlog spécifié (pas dans ce sprint)

Retenus, signal réel, mais valeur/effort moins bons — à prendre dans cet ordre plus tard :

1. **`GlobalConfig_B2SServer.xml` absent ou mal nommé** (P1, ≥3 discussions VPU, →oct 2025). Citation :
   « Global config file ... does not exist; no global config loaded ». Test de présence trivial, nom de
   fichier exact confirmé. Très bon rapport effort/valeur — **candidat n°1 du prochain lot.**
2. **Dossier PuP-Pack au mauvais nom** (P0 dans la recherche). `CompletenessScanner` calcule déjà
   `rom.Primary` et teste `PupVideosDir/<rom>` ; il émet `PUPPACK_PRESENT` si trouvé et **rien** sinon.
   Ajouter : « un dossier proche existe sous un autre nom » (comparaison stricte, sans fuzzy). Cadrage
   noté « faible » par la recherche elle-même → à préciser avant de coder.
3. **Copies multiples d'un même composant** (`dmddevice.dll`, `FlexDMD.dll`, B2S) à des chemins
   différents et versions différentes — le motif « install hybride » que la recherche identifie comme
   *« l'une des idées les plus prometteuses »*. `BitnessScanner` a **déjà toutes les données** (il
   énumère et dédoublonne par chemin) mais n'en tire qu'un inventaire. Bon candidat, mais suppose de
   toucher un scanner existant ou d'en dupliquer l'énumération.
4. **Chemins configurés de PinUP Popper invalides** (P1). Citation : « Pinup Popper opens "This PC\Documents"
   when it can't find the folder you specified ». Nécessite de confirmer dans quelle table/colonne de
   `PUPDatabase.db` vivent ces chemins — `SqliteReader` sait lire, mais le schéma exact n'est pas confirmé
   (même trou que la colonne de nom des playlists, déjà documenté). **Bloqué sur confirmation de schéma.**
5. **`B2STableSettings.xml` local vs global** (clé `ArePluginsOn`). Rejoint la demande forum du 07/08 sur
   « un réglage global plugins qui marche vraiment ». `ScreenTopologyScanner` déclare ce fichier hors
   périmètre. Détection partielle seulement — on ne peut pas savoir si un réglage local est volontaire.
6. **DOF / DOFLinx** (items 201-209, plusieurs P0/P1 : `directoutputconfig.ini` introuvable, chemins morts
   dans `doflinx.ini`, DOFLinx qui prend VPX en charge alors que DOF le fait déjà). **Famille entière non
   couverte par le projet aujourd'hui** — aucun scanner ne regarde DOF. Signal réel et récent (→mars 2026),
   mais c'est un nouveau domaine complet, pas une extension : mérite son propre cadrage produit avec
   Maxime avant toute spec (est-ce qu'on veut entrer sur le terrain DOF ?).

---

## §7 — Rejeté, et pourquoi (ne pas re-débattre sans élément nouveau)

- **« PuP Pack ne marche pas » générique** (item 19) — le document le dit lui-même : causes hétérogènes,
  aucune cause racine unique. Un finding générique « ton PuP pack ne marche pas » sans cause identifiée
  est du bruit anxiogène. Rejeté en tant que finding ; les sous-cas déterministes (nom de dossier, LOT §6.2)
  restent recevables séparément.
- **« Pinup Popper n'aime pas les espaces dans les noms de dossiers »** — le document de recherche
  *avertit lui-même* de ne pas en faire une règle générale. Des millions d'installs ont des espaces.
  Rejeté.
- **« Tous les `.vbs` à la racine sont obsolètes »** (règle proposée par Gemini) — explicitement corrigée
  comme *trop forte* dans le document lui-même : certains `.vbs` externes sont des overrides volontaires.
  Rejeté.
- **Donner « Full Control » au groupe Users sur les dossiers VPinMAME** — modification de sécurité
  Windows, hors périmètre, rejeté aussi par le document source (LOT G).
- **Réparer un `VPinballX.ini` en le supprimant** (item 49) — supprimer la config de l'utilisateur pour
  voir si ça va mieux n'est pas une réparation, c'est une perte de données. Éventuellement un jour avec
  sauvegarde + confirmation explicite, mais **pas** dans un lot de détection.
- **Restaurer un fichier mis en quarantaine par Windows Defender** — techniquement impossible de façon
  fiable, et jouer avec la quarantaine antivirus est une très mauvaise idée dans un outil grand public.
  Détection du symptôme (fichier attendu absent) uniquement.
- **`hidapi.dll` manquant** (item 48) — lié à une révision précise de VPX 10.8.x, corrigée depuis en
  amont. Coder un check pour un bug déjà réparé par l'éditeur, c'est de la dette immédiate.
- **Runtimes Microsoft Visual C++** (item 47) — détection possible mais les versions exactes requises
  changent avec VPX ; un check faux sur ce sujet enverrait des gens installer des runtimes au hasard.
  À reconsidérer seulement si la doc VPX officielle fixe une version.

---

## §8 — Rappels de forme pour la session de code

- Nouveaux scanners à ajouter dans la chaîne `.Add(...)` de `MainWindow.xaml.cs` (~ligne 390), **après**
  les scanners existants, groupés avec un commentaire « Lot communauté 10/08 ».
- Chaque nouveau code de finding a besoin d'une entrée `Loc.cs` **FR + EN** (le projet est à 100 % de
  couverture de traduction, ne pas casser ça) et d'une entrée Knowledge (impact + cause + méthode de
  vérification) pour tout `Warning`/`Critical` — l'audit vérifie automatiquement qu'aucun
  `Warning`/`Critical` n'est sans entrée.
- `build.cmd` doit rester vert : Core + Repair + App. Aucun `dotnet` n'était disponible dans les sessions
  cloud jusqu'ici — si c'est toujours le cas, le dire clairement plutôt que d'annoncer des tests verts
  non exécutés.
- Mettre à jour `TRANSMISSION.md` (bloc du haut) et `knowledge/FIELD-LOG.md` en fin de session, avec la
  correction du §1 de ce document.
- **Un ADR est attendu pour le LOT H** (première écriture réelle sur la machine de l'utilisateur). Même
  raisonnement que l'ADR réclamé le 07/08 pour le premier appel réseau : une décision de cette nature ne
  doit pas rester du savoir tribal dans un commentaire de code. Numéro libre suivant : **ADR-012**.
- Ordre de travail recommandé : **H.1 (journal persistant) en premier** — c'est un prérequis dur de tout
  le reste du LOT H — puis LOT A (détection COM), puis H.2→H.6, puis I, puis B, C, D, E, F, G.
  Raison : le LOT A produit les findings dont le LOT I a besoin, et le journal conditionne tout `Apply`.

---

## §8bis — LOT J (ajouté le 10/08 après retour terrain réel) — `B2S_MISSING` sur une table à PUP-Pack

**Source : cas terrain direct** (utilisateur Messenger, install Baller refaite en décembre, 3 écrans, 2
rapports de scan fournis le 10/08). Verbatim : *« pup packs generally had you skip the b2s file being in
with the tables, and popper just handled it in the pup packs »*.

**Le faux positif.** Sur son install, 7 `B2S_MISSING` en `Warning` alors que l'absence de `.directb2s` est
**la configuration correcte** : son backglass vient du PUP-Pack, pas de B2S. La dé-emphase codée le 10/08
(#13) ne le couvre pas, car elle ne se déclenche que si **aucun** composant B2S n'est installé — or il en
a un. Deuxième signal terrain indépendant sur le même thème (le premier étant le cab sans backglass de
Maxime), donc recevable même sous la règle stricte du scanner gelé.

**Correctif, déterministe** (`CompletenessScanner`, qui calcule déjà tout ce qu'il faut) :

| Situation | Sévérité `B2S_MISSING` |
|---|---|
| table a un PUP-Pack (`PupVideosDir/<rom>` non vide, déjà calculé pour `PUPPACK_PRESENT`) ET son script ne fait **pas** `CreateObject("B2S.Server")` | **`Note`** — le backglass vient du pack, c'est normal |
| table a un PUP-Pack **et** son script demande B2S | `Warning` inchangé — la table réclame elle-même le fichier |
| pas de PUP-Pack | logique actuelle inchangée (dont la dé-emphase #13) |

`UsesB2S` est déjà fourni par `ScriptAnalyzer.AnalyzeRomUsage`. Aucune nouvelle lecture disque.

**Second constat, à traiter aussi (petit mais réel).** Entre ses deux rapports, tous les findings Popper
(`POPPER_NOT_REGISTERED`, `POPPER_ORPHAN_PLAYLIST`, `POPPER_MEDIA_MISSING`) ont disparu d'un coup.
Deux explications possibles : il a corrigé sa config Popper, **ou** `PUPDatabase.db` était verrouillé
(Popper ouvert pendant le scan) et `SqliteReader` a rendu `null` partout. Dans ce second cas, le scanner
devient **totalement silencieux sur toute la dimension Popper sans le dire** : `POPPER_DB_NOT_FOUND` n'est
émis que si le *chemin* est introuvable, jamais si le fichier existe mais est illisible.

→ Ajouter `POPPER_DB_UNREADABLE` (`Info`) : chemin de la base trouvé, mais lecture impossible. Le rapport
doit distinguer « vérifié, tout va bien » de « je n'ai pas pu vérifier ». C'est la même honnêteté que le
`SCREENRES_UNPARSED` du LOT F.

---

## §9 — Prompt de passation pour la session Sonnet

> Copier-coller tel quel :
>
> « Tu reprends Pincab Toolbox / FlipSync (MC Automation, Maxime Chauvin). Effort élevé.
>
> Lis dans cet ordre : `docs/SPEC-lot-communaute-2026-08-10.md` (intégralement — c'est ton ordre de
> travail), puis `TRANSMISSION.md` (bloc du haut), puis `docs/adr/ADR-005`, `ADR-006`, `ADR-010` et
> `ADR-011`.
>
> Mission : coder et câbler les lots A à I de la spec. Les 4 décisions produit sont déjà tranchées par
> Maxime le 10/08 et notées en §4 — ne pas les redemander, ne pas les réinterpréter.
>
> Trois points sur lesquels je te demande d'être intransigeant :
> 1. Le **LOT H** câble le premier chemin d'écriture réel du produit sur la machine d'un utilisateur.
>    Commence par H.1 (journal persistant) : sans lui, `Undo` ne survit pas à la fermeture de l'app.
>    Livre H entièrement ou pas du tout.
> 2. Le `Critical` du LOT A.3 est le premier ajouté depuis le gel du scanner. Ses 4 conditions doivent
>    être **mesurées**, jamais supposées : si la lecture du registre échoue, silence total, jamais un
>    `Critical` de repli.
> 3. Ne re-code pas ce qui existe : la carte anti-doublon du §2 liste 26 scanners déjà câblés et
>    l'infrastructure réutilisable. `FLEXDMD_MISSING` **est** déjà câblé (`DependencyScanner.cs:80`),
>    contrairement à ce que disaient les handoffs précédents.
>
> Si le temps manque, abandonne dans cet ordre : G, F, E, D, C. Jamais H à moitié.
>
> `build.cmd` doit rester vert (Core + Repair + App). Si aucun `dotnet` n'est disponible dans ton
> environnement, dis-le explicitement plutôt que d'annoncer des tests verts non exécutés — c'est la
> règle depuis le début du projet.
>
> Écris un ADR-012 pour le chemin d'écriture, mets à jour `TRANSMISSION.md` et `knowledge/FIELD-LOG.md`
> en fin de session, et fais une revue CTO + Product avant de clôturer. »
