# Audit de cohérence — landing Pincab Toolbox (19/08/2026)

**Objectif** : vérifier que `flipsync-site/landing/index.html` (et `cgu.html` à côté) dit vrai par
rapport à ce que le logiciel fait réellement aujourd'hui, avant de la montrer à des testeurs.

**Méthode** : chaque promesse de la landing a été confrontée au code (`src/PincabToolbox.Core/Scanning/*.cs`,
`src/PincabToolbox.App/MainWindow.xaml.cs`, `RepairSession.cs`, `knowledge/pack-2026.08.json`,
`Knowledge.cs`, `Loc.cs`), pas au discours de la landing elle-même ni au FIELD-LOG.

Je ne modifie rien sur la landing. Ce document propose, tu trancheras.

> ✅ **MAJ 19/08, plus tard le même jour — les deux corrections ci-dessous ont été appliquées, sur
> ta confirmation.** Fichiers réécrits directement sur ta machine (`flipsync-site/landing/index.html`
> et `cgu.html`), pas dans git (ils n'y sont pas). Le reste de cette section garde le détail du
> "pourquoi", pour mémoire.

---

## 1. Ce qui a été corrigé — survendu / incohérent

### 1.1 SURVENDU (corrigé) — « System decimal separator that breaks table physics »

- **Où** : section « What the free scanner checks » → carte « System & processes », dernière puce.
- **Promesse landing (EN)** : *"System decimal separator that breaks table physics"* (FR : *"Séparateur
  décimal système qui casse la physique des tables"*).
- **Réalité code** : `Knowledge.cs`, code `LOCALE_DECIMAL_SEPARATOR`, `ImpactEn` = *"Some VPX table
  scripts and physics/config parsing assume a dot as the decimal separator, and **can misbehave**
  under a comma-decimal locale."* — la doctrine interne (`Severity.Note`, ADR-010) est volontairement
  prudente : c'est un facteur de risque possible, jamais une certitude de casse.
- **Verdict** : **survendu**. La landing affirme un fait ("casse"), le code ne constate qu'un risque
  ("peut perturber"). Sur un point technique que peu de testeurs sauront vérifier eux-mêmes, l'écart
  de certitude est le genre de détail qui, découvert après coup, entame la crédibilité — exactement le
  scénario du 30/07.
- **Texte appliqué** :
  - EN : *"System decimal separator that can affect physics on some tables"*
  - FR : *"Séparateur décimal système qui peut perturber la physique de certaines tables"*
  - ES : *"Separador decimal del sistema que puede afectar a la física de algunas mesas"*

### 1.2 INCOHÉRENCE (corrigée) — durée des mises à jour, `cgu.html`, encart anglophone

- **Où** : `flipsync-site/legal/cgu.html`, encart résumé "English speakers" (~lignes 83-93).
- **Texte actuel (EN)** : *"The paid Repair module is currently in closed beta (not on general sale) —
  when it opens, it's a one-time purchase (perpetual license) with **12 months of updates included**,
  not a subscription to use the software."*
- **Réalité** : `ADR-013` (19/08) supprime toute limite de durée sur les mises à jour — *"mises à jour
  incluses sans limite de durée"*. La section française §4 « Prix », **sur la même page**, dit
  d'ailleurs le contraire de l'encart anglais : *"les mises à jour du module et de sa base de
  connaissance sont incluses sans limite de durée, et il n'y a aucun renouvellement à payer."*
- **Verdict** : **incohérence interne au document légal** — pas vraiment un cas de survente (le texte
  anglais promet *moins* que la réalité, donc plutôt sous-vendu), mais une page légale qui se
  contredit elle-même selon la langue lue est un problème de crédibilité et potentiellement un
  problème juridique (le CGU/CGV fait foi, il ne peut pas dire deux choses différentes). L'encart
  anglais n'a manifestement pas été mis à jour quand le reste de la page a été aligné sur ADR-013.
- **Texte appliqué (EN)** : *"The paid Repair module is currently in closed beta (not on general
  sale) — when it opens, it's a one-time purchase (perpetual license) with updates included with no
  time limit, not a subscription to use the software, and no renewal to ever pay."*

*(Aucune autre incohérence de ce type détectée dans `cgu.html` sur la portion lue — le reste de
l'encart anglophone et le §6 médiateur sont cohérents avec le texte français correspondant.)*

---

## 2. Vérifications ciblées demandées

### 2.1 Prix

- **Résultat** : `index.html` (la landing publique) **n'affiche aucun prix** — normal et voulu, Repair
  est en "closed beta", explicitement marqué *"it isn't on sale yet"*. Aucun tunnel d'achat, conforme
  à la règle non-négociable de cette session.
- Le prix 3,99 (EUR/USD/GBP, ADR-013) n'apparaît que dans `cgu.html` §4, et y est correct.
- **Verdict** : **conforme**. Rien à corriger.

### 2.2 Liens

| Lien | Cible | Statut |
|---|---|---|
| Conditions d'utilisation | `/cgu.html` | Fichier présent à côté de la landing, conforme. |
| Confidentialité | `/cgu.html#confidentialite` | Ancre présente dans `cgu.html`, conforme. |
| Contact | `mailto:flipsync.contact@gmail.com` | Adresse cohérente avec le reste du projet — je ne peux pas vérifier depuis le sandbox que la boîte est active, à confirmer de ton côté. |
| Téléchargement (x2, hero + CTA finale) | `https://github.com/waylo1/pincab-toolbox/releases/latest/download/PincabToolbox.zip` | Techniquement correct : ce format d'URL suit toujours la release marquée « Latest » sur GitHub, pas une version figée. Pas un défaut de la landing. |

**✅ Confirmé par toi (19/08) : `v0.1.2-alpha` est bien la release GitHub "Latest".** En testant ce
lien de téléchargement depuis le sandbox, la redirection HTTP réelle de GitHub pointait vers l'asset
de `v0.1.2-alpha` (07/08), pas `v0.1.1-alpha` (30/07) — je ne pouvais pas le confirmer par une
deuxième méthode depuis ce sandbox (accès GitHub restreint sur ce dépôt), donc je l'avais signalé
sans trancher. Ce n'est pas un bug de la landing : le lien suit automatiquement la release "Latest",
il pointait déjà vers la bonne version. Voir `docs/RELEASE-NOTES-depuis-v0.1.1-alpha.md` pour ce que
ça change dans la base de comparaison des nouveautés.

### 2.3 Mentions légales (footer landing)

- *"© 2026 MC Automation — Pincab Toolbox · Beta 0.1 · Windows 10/11"* — conforme, cohérent avec le
  statut réel du projet.
- *"Not affiliated with Visual Pinball, VPinMAME or PinUP Popper."* — conforme, cohérent avec les 5
  règles légales inviolables d'ADR-004.
- Pas de mention du médiateur de la consommation sur la landing elle-même — normal, cette mention vit
  dans `cgu.html` §6, où elle reste `[à compléter — souscription en cours]`. Ce n'est pas un défaut de
  la landing, c'est le point déjà identifié et toujours ouvert (souscription non faite).

### 2.4 Cohérence avec `cgu.html`

- Traité en §1.2 ci-dessus (le seul écart trouvé).
- Le reste des deux pages (langues EN/FR/ES, structure, ton) est cohérent.

---

## 3. Autres promesses vérifiées face au code — conformes

| Promesse landing | Vérification code | Verdict |
|---|---|---|
| "36 Checks per scan" | `MainWindow.xaml.cs` : 36 appels `.Add(new XScanner())` confirmés (le total, pas la répartition détaillée par module — je n'ai pas recompté module par module). | Conforme |
| "Nine modules, 36 checks" | 9 cartes de modules listées sur la landing, cohérentes avec la structure des sous-onglets du Scanner (`StabCauses/Results/Components/Tables/System/Updates` + regroupements). Répartition fine non recomptée exhaustivement. | Conforme (probable) |
| "100% Local & read-only" / "0 Telemetry/accounts" | Aucun code de télémétrie trouvé ; les 5 règles inviolables d'ADR-004 ("on vérifie, on ne fournit jamais") s'appliquent. **Nuance à connaître** : le bouton "Check for updates" (`UpdateChecker.cs`, commit du 07/08) est le tout premier appel réseau du projet — mais il est manuel, opt-in, et ne concerne que la vérification de version, pas de la télémétrie sur l'utilisateur. La formulation de la landing ("0 Telemetry") reste juste, elle ne dit pas "0 appel réseau". | Conforme, nuance à connaître |
| "Closed beta: Repair" — 4 correctifs listés (Unblock file, Restore ROM, Quarantine orphaned media, Kill zombie PinUpDisplay) | `knowledge/pack-2026.08.json` : exactement 4 `actionId` distincts référencés par une règle (`unblock_file`, `restore_rom_archive`, `quarantine_orphaned_media`, `kill_zombie_pinup_display`). `register_com_component` est codé mais n'a aucune règle qui le déclenche (inerte, choix assumé ADR-012) — la landing ne le liste donc pas, c'est cohérent. | Conforme |
| FAQ (5 questions : VPX 10.8, PinUP Popper, modification de fichiers, droits admin, Future Pinball) | Recoupé avec ADR-004 et le comportement réel (lecture seule, pas de modification hors Repair). | Conforme |
| "What's new" (4 nouveaux checks, motifs d'échec Repair visibles, bouton Rescore) | Correspond exactement au lot de livraison du 08/18 (commits `561087a`, `b94d175`, `767b7d6`). | Conforme. **Mise à jour appliquée le 19/08** : date passée au 19 août, 4e carte ajoutée pour la réparation COM sans droits admin (commit `bc76baf`), cohérente avec `RELEASE-NOTES-depuis-v0.1.1-alpha.md`. |

---

## 4. Verdict global

La landing est **prête pour des testeurs**. Les deux corrections concrètes identifiées ont été
appliquées le 19/08 (§1.1 puce "decimal separator" assouplie, §1.2 encart anglophone de `cgu.html`
aligné sur ADR-013 et sur le texte français), et la section "What's new" a été mise à jour pour
inclure la réparation COM sans droits admin.

Le reste (prix absent, liens, mentions légales, FAQ, périmètre Repair) est cohérent avec le code réel.
Le point qui restait à trancher par toi (§2.2, release GitHub réellement "Latest") est confirmé :
`v0.1.2-alpha`.
