# Croisement de la liste ChatGPT (11/08/2026) avec le code réel

**But de ce document** : Maxime a demandé à ChatGPT une liste "meilleur des logiciels et projets
communautaires du monde du pincab" à intégrer à Pincab Toolbox. Avant de coder quoi que ce soit
dessus, ce document vérifie, ligne par ligne, ce qui existe déjà dans le code (32 scanners réels
dans `src/PincabToolbox.Core/Scanning/`, 6 actions Repair dans `src/PincabToolbox.Repair/Actions/`)
plutôt que de faire confiance à la liste telle quelle. Même discipline que pour la recherche
GPT+Gemini du 10/08 (`docs/SPEC-lot-communaute-2026-08-10.md`) : filtrer avant de coder, jamais
l'inverse.

**Conclusion en une phrase** : la moitié de cette liste est déjà faite, souvent mieux détaillée que
ce que ChatGPT décrit. Le reste se répartit entre décisions produit déjà en attente (DOF), vrais
manques (matériel USB, DMD physiques, autres frontends), et hors périmètre par choix (Future
Pinball).

## Déjà couvert, rien à recoder

| Domaine (liste ChatGPT) | Ce qui existe réellement |
|---|---|
| VPX version/bitness | `VpxVersionScanner`, `BitnessScanner`, `ChainBitnessScanner` |
| VPinMAME/B2S/FlexDMD — COM, chemins, bitness | `ComHealthScanner` (LOT A, cette session) : `COM_NOT_REGISTERED`, `COM_STALE_PATH`, `COM_PATH_OUTSIDE_INSTALL`, `COM_BITNESS_GAP`, `VPINMAME_NOT_REGISTERED` |
| FlexDMD/B2S dépendance manquante | `DependencyScanner` (déjà câblé, `FLEXDMD_MISSING` en `Warning`) |
| DMDext/Freezy config | `DmdConfigScanner` (LOT C, format `[VirtualDMD]` confirmé sur le vrai `DmdDevice.ini` de freezy) + `DmdComPortScanner` |
| Fichiers bloqués (.dll/.exe/.ocx) | `BlockedFileScanner` (étendu LOT E), Repair : `UnblockFileAction` |
| Réenregistrement COM (VPinMAME/B2S/FlexDMD) | `RegisterComComponentAction` — **codé et testé, PAS câblé** (LOT I, voir ADR-012 : deux inconnues à valider sur machine réelle avant activation) |
| PinUP Player zombie | `PinupDisplayZombieScanner`, Repair : `KillZombiePinUpDisplayAction` |
| PinUP Popper — playlists, médias orphelins | `PopperPlaylistScanner`, `OrphanedMediaScanner`, Repair : `QuarantineOrphanedMediaAction` |
| Table individuelle — backglass, entrée Popper, dossier PuP-Pack | `CompletenessScanner` (vérifie déjà les trois par table, en lecture seule sur la base SQLite) |
| Toutes les tables en lot | `CompletenessScanner`/`CompatibilityScanner`/`LegacyTableScanner` tournent sur l'ensemble du parc à chaque scan, pas table par table à la demande |
| Backglass (.directb2s) | `DirectB2sScanner` |
| ROMs manquantes/doublons | `RomValidatorScanner`, Repair : `RestoreRomArchiveAction` |
| Écrans — position, résolution, rotation | `DisplaySetupScanner`, `ScreenTopologyScanner`, `ScreenResUnparsedScanner` (LOT F), `DpiScalingScanner` — détection complète, **aucune action de correction automatique** (juste détection aujourd'hui) |
| NVRAM/cfg | `NvramScanner`, `NvramWritabilityScanner` (LOT G, sonde d'écriture réelle) |
| Registre VPX/B2S/PinMAME | Couvert pour COM (`ComHealthScanner`) et pour altsound/altcolor (`FeatureEnabledScanner`, LOT D) — pas un scanner de registre généraliste |
| Installations multiples / conflits | `VpxVersionScanner` gère déjà plusieurs exécutables ; scan multi-racines/disque entier existant (ADR-011) |
| Sauvegarde avant réparation | `FileBackupService`, systématique avant toute écriture Repair |
| Rollback en un clic | `Undo`, journal persistant (LOT H.1) |
| Réparations par lot | `Apply` accepte une sélection multiple d'items en un seul appel (jamais "tout réparer" silencieux, par choix — H.2 règle 3) |
| Mode Dry Run | Deux niveaux : l'aperçu gratuit sans licence (ADR-006) et le nouveau `PINCAB_REPAIR_FORCE_DRYRUN` (ce jour) |

## Décision déjà en attente, pas un manque

**DOF / DirectOutput.** Repéré dans la spec du 10/08 (§6.6) : le fichier `GlobalConfig.xml` de DOF
n'est pas propre à B2S-Server comme on le pensait au départ (vérifié via la doc officielle
DirectOutput), et la spec elle-même dit que ce sujet "mérite son propre cadrage produit avec
Maxime avant toute spec". Cette question t'a déjà été posée plus tôt dans cette session sans
réponse encore. Rien de nouveau apporté par la liste ChatGPT ici, sinon la confirmation que DOF est
un vrai sujet à traiter, pas un oubli.

## Vrais manques (à cadrer avant de coder, comme d'habitude)

- **DMD physiques (ZeDMD, Pin2DMD...)** — détection USB/firmware. Rien dans le code aujourd'hui.
  Nécessite de définir comment détecter du matériel USB depuis .NET sans dépendance tierce
  (contrainte du projet : zéro dépendance externe), ce qui n'est pas trivial.
- **USB Cabinet (Pinscape, KL25Z, LedWiz)** — même remarque, sujet matériel/USB, à cadrer.
- **UltraDMD** — cité par ChatGPT mais aucune trace dans le code ; à vérifier si c'est encore
  utilisé dans la communauté avant d'y investir (DMDext/FlexDMD ont largement pris le relais).
- **SSF Audio (mapping surround pinball-spécifique)** — différent du simple périphérique de sortie
  par défaut déjà couvert par `AudioStateScanner`/`SetDefaultAudioDeviceAction`. Pas couvert.
- **Autres frontends (PinballY, PinballX)** — le projet cible PinUP Popper aujourd'hui. Ouvrir
  d'autres frontends est un choix de périmètre, pas un bug à corriger.
- **Variables d'environnement** — aucun scanner générique dessus aujourd'hui.
- **Score de santé du pincab, historique des scans, comparaison entre deux scans, rapport
  HTML/PDF/JSON** — fonctionnalités transverses citées par ChatGPT, aucune n'existe. Ce sont des
  chantiers UI/produit à part entière, pas des scanners individuels.

## Hors périmètre par choix, pas par oubli

**Future Pinball + BAM.** Le projet est VPX uniquement depuis le début. L'ouvrir à Future Pinball
serait un changement de périmètre produit, pas un ajout incrémental — à traiter comme tel si tu le
souhaites un jour, pas glissé dans un lot de scanners.

## Ce que je propose, sans le coder maintenant

Ne pas partir sur cette liste telle quelle. Elle mélange du déjà-fait, une décision déjà en
attente (DOF) et des vrais chantiers de tailles très différentes (un scanner de plus vs. détection
matérielle USB vs. un système de scoring transverse). Si tu veux avancer, la prochaine étape utile
serait une session dédiée à écrire une spec scopée sur UNE SEULE des zones "vrai manque"
ci-dessus, exactement comme le 10/08, plutôt que de tout lancer en parallèle. Le plus proche
niveau d'effort de ce qui vient d'être fait serait probablement DOF (la question est déjà posée,
il ne manque que ta réponse) ou les DMD physiques (matériel mais bien délimité).
