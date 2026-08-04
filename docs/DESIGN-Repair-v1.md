# Design — Repair v1

**Statut** : ✅ **implémenté et testé** — 55 tests verts · **Date** : 25/07/2026
**Remplace** : `architecture-repair-phase2.md` (notes v1)
**À lire avant** : `ARCHITECTURE-KnowledgeEngine.md` §5, §6, §9 · `adr/ADR-001`, `ADR-002`, `ADR-004`

> Ce document décrit **le moteur**, pas les correctifs. Les correctifs sont de la donnée
> (Knowledge Pack) et s'ajoutent sans toucher au moteur — c'est tout l'intérêt.

---

## 1. Le principe qui commande tout le reste

Le Scanner a passé six mois à construire une réputation de « lecture seule, zéro risque ». Repair est le moment où on écrit sur l'installation de quelqu'un d'autre. **Une seule mauvaise écriture coûte plus cher que dix fonctionnalités manquantes.**

Tout ce qui suit découle de ça :

> **Repair n'agit jamais sur un problème qu'il n'a pas revérifié au moment d'agir, jamais sans avoir montré ce qu'il va faire, jamais sans pouvoir revenir en arrière, et jamais sans le dire dans un journal.**

Un scan est un **instantané**. Entre le scan et le clic sur « Réparer », l'utilisateur a pu corriger le problème à la main, déplacer un dossier, lancer VPX. Le moteur doit supposer que le monde a bougé.

---

## 2. Ce qui change par rapport aux notes v1

Les notes de phase 2 proposaient une interface unique `IFixer` portant à la fois la connaissance (*quoi corriger*) et l'exécution (*comment*). C'est incompatible avec deux décisions prises depuis :

- `ARCHITECTURE-KnowledgeEngine.md` §4 : **la connaissance est de la donnée**, mise à jour indépendamment de l'app.
- `ARCHITECTURE-KnowledgeEngine.md` §8 : le Knowledge Pack a vocation à devenir **contribuable par la communauté**.

Mises bout à bout, ces deux décisions signifient qu'un fichier de données, potentiellement écrit par un tiers, va décrire des opérations d'écriture sur le disque de l'utilisateur. **Sans séparation stricte, le Knowledge Pack devient un vecteur d'exécution de code.**

D'où la scission, qui est la décision structurante de ce design :

| | Nature | Qui l'écrit | Ce que ça peut faire |
|---|---|---|---|
| **`RepairRule`** | **Donnée** (Knowledge Pack) | Nous, puis la communauté | *Composer* des capacités existantes |
| **`IRepairAction`** | **Code** (registre fermé) | Nous seuls, dans le dépôt | *Définir* une capacité |

Une règle du pack ne peut nommer qu'un `ActionId` **présent dans le registre compilé**, avec des paramètres typés et validés. Un pack ne peut jamais introduire une capacité nouvelle — seulement de nouvelles combinaisons de capacités déjà auditées. Voir **ADR-005**.

Autre changement, issu d'ADR-001 : le `FocusWatchdog` et le `DisplayLayout/ScreenPicker` **ne sont plus dans Repair**. Ce sont des résidents actifs pendant le jeu ou des réglages matériel → ligne **Play Optimizer**.

---

## 3. Les objets

### 3.1 `RepairRule` — donnée, dans le pack

```
RepairRule {
  Id                    // "unblock-dll-v1"
  TargetCode            // code de finding, OU scenarioId
  ActionId              // doit exister dans le registre — sinon la règle est ignorée
  Parameters            // typés, validés par l'action
  RepairConfidence      // 0–100
  Reversible            // déclaré, mais l'action a le dernier mot
  BackupRequired        // true par défaut
  ManualProcedureFr/En  // affiché quand aucune automatisation n'est possible
}
```

### 3.2 `IRepairAction` — code, dans le registre

```csharp
public interface IRepairAction
{
    string ActionId { get; }
    ChangeKind Kind { get; }
    bool IsReversibleByNature { get; }

    ValidationResult ValidateParameters(RepairParameters p);

    // Pur. Aucun effet de bord. C'est le dry-run.
    IReadOnlyList<PlannedChange> Plan(RepairContext ctx, RepairParameters p);

    ExecutionResult Execute(PlannedChange change);
    ExecutionResult Revert(PlannedChange change);
}
```

**`Plan()` est pur et c'est non négociable.** Le dry-run et l'application consomment le **même objet** `PlannedChange`. Si le dry-run calculait une chose et l'apply une autre, l'aperçu deviendrait un mensonge — et l'aperçu est précisément ce qui justifie la confiance.

### 3.3 `PlannedChange` — l'unité d'écriture

```
PlannedChange {
  ActionId
  Kind        // FileAttribute | FileMove | IniWrite | RegistryWrite | SqliteWrite
  Target      // chemin, clé de registre, table+ligne
  Before      // valeur constatée, courte
  After       // valeur proposée
  Reversible
}
```

> `Before` a exactement la forme d'un `EvidenceItem` (ADR-003). Ce n'est pas une coïncidence :
> **l'aperçu est une preuve portant sur un état futur.** Même structure, même rendu UI,
> même règle d'anonymisation à l'export.

### 3.4 `RepairPlanItem` — l'unité de transaction

```
RepairPlanItem {
  ItemId
  TargetCode
  Mode              // Automatic | ConfirmationRequired | ManualOnly | Locked
  Changes[]         // ORDONNÉ — un playbook de scénario est UN item
  Completeness      // Full | Partial
  Missing[]         // ce qui ne peut pas être automatisé, et pourquoi
  Blockers[]        // ce qui empêche de lancer
  Selected          // opt-in, false par défaut
}
```

**Le playbook d'un scénario est un seul item.** C'est ce qui donne la bonne granularité transactionnelle (§6) : une migration 32→64 à moitié appliquée est pire que pas de migration du tout, alors que deux correctifs indépendants n'ont aucune raison de s'annuler l'un l'autre.

### 3.5 Trois abstractions découvertes en écrivant les tests

Elles n'étaient pas dans la conception initiale. Sans elles, le préflight n'est **pas testable** :
il faudrait vraiment lancer VPX et vraiment remplir un disque.

- **`IEnvironmentProbe`** — processus bloquants, espace libre, droit d'écriture.
- **`ISystemClock`** — horodatage déterministe du journal.
- **`IFileSystem`** — surface fichier minimale. Permet aussi d'exercer tout le moteur sur n'importe
  quel OS, et cantonne `System.IO` à une seule classe (`RealFileSystem`).

C'est le genre de trou que seule l'écriture des tests fait apparaître.

---

## 4. Le gating — deux portes indépendantes

Une confusion à ne jamais faire : `AutoFixable` est une frontière **commerciale**, `RepairConfidence` est une mesure de **sûreté**. Elles ne se remplacent pas.

```
Porte 1 — commerciale : existe-t-il une règle ? la licence est-elle valide ?
Porte 2 — sûreté      : la confiance et la réversibilité autorisent-elles quel mode ?
```

La porte de sûreté ne peut que **dégrader** le mode, jamais l'améliorer.

| Confiance | Réversible | Mode |
|---|---|---|
| ≥ 95 | oui | **Automatic** — applicable en lot, toujours avec sauvegarde + journal + opt-in |
| ≥ 95 | non | **ConfirmationRequired** + case « je comprends que c'est irréversible » |
| 70 – 94 | — | **ConfirmationRequired** — confirmation par correctif |
| < 70 | — | **ManualOnly** — procédure affichée, aucun bouton |
| pas de licence | — | **Locked** — résumé visible, détail retenu |

*Vérifié : 8 cas de gating, dont « non réversible + confiance 100 → jamais Automatic ».*

### La coupe gratuit / payant (ADR-006)

La porte ne passe pas entre « voir » et « ne pas voir », mais entre le **quoi** et le **comment**.

| | Gratuit (Scanner) | Payant (Repair) |
|---|---|---|
| Le problème, son impact, sa cause | ✅ | ✅ |
| Qu'une réparation existe | ✅ | ✅ |
| Réversible · sauvegarde prévue · durée estimée · nombre d'écritures | ✅ `Summary` | ✅ |
| Ce qu'on **ne sait pas** faire (`Missing[]`, procédures manuelles) | ✅ | ✅ |
| Chemins exacts, valeurs avant → après, **ordre** des opérations | ❌ | ✅ |
| Exécuter, sauvegarder, annuler | ❌ | ✅ |

Deux règles qui tiennent l'ensemble :

1. **Le résumé est calculé, jamais déclaré.** `RepairSummary.From()` le dérive du plan réel. Si une règle du pack prétend une réversibilité que l'action ne sait pas fournir, **l'action gagne**. Sinon « réversible » deviendrait un argument marketing.
2. **Une limitation n'est jamais cachée.** La partialité et les procédures manuelles restent gratuites : ce sont des choses que Repair n'automatisera jamais (ADR-004), et on ne fait pas payer ce qu'on refuse de faire.

**La coupe est faite dans le moteur** (`Plan(..., licensed: false)` renvoie `Changes` vide), pas dans l'UI : aucun bug d'interface ne peut faire fuiter un détail qui n'a jamais quitté le moteur.

---

## 5. Le flux

```
        ┌─ SCAN (gratuit) ────────────────────────────────────┐
        │  Findings + Scénarios                                │
        └───────────────────────┬──────────────────────────────┘
                                ▼
        ┌─ PLAN / DRY-RUN ─────────────────────────────────────┐
        │  Pour chaque finding : règle ? → action ? → Plan()    │
        │  → PlannedChange[] + Summary + Mode + Completeness   │
        │  AUCUN effet de bord.                                │
        │  sans licence → Changes VIDE, Summary rempli (ADR-006)│
        └───────────────────────┬──────────────────────────────┘
                                ▼  l'utilisateur coche (opt-in)
        ┌─ PRÉFLIGHT ──────────────────────────────────────────┐
        │  1. VPX / Popper / VPinMAME arrêtés ?     → sinon REFUS│
        │  2. Espace disque pour la sauvegarde ?    → sinon REFUS│
        │  3. Droits d'écriture sur chaque cible ?               │
        │  4. Chaque cible est-elle DANS l'install ? → sinon rejet│
        │  5. Le finding est-il TOUJOURS vrai ?      → sinon drop │
        └───────────────────────┬──────────────────────────────┘
                                ▼
        ┌─ SAUVEGARDE ─────────────────────────────────────────┐
        │  Uniquement les cibles concernées, hors de l'install   │
        └───────────────────────┬──────────────────────────────┘
                                ▼
        ┌─ APPLY (par item, ordonné) ──────────────────────────┐
        │  succès → ChangeApplied dans le journal               │
        │  échec  → compensation inverse, puis ItemRolledBack   │
        └───────────────────────┬──────────────────────────────┘
                                ▼
        ┌─ VÉRIFICATION ───────────────────────────────────────┐
        │  Re-scanner les codes concernés : ont-ils disparu ?    │
        │  → alimente la calibration de RepairConfidence (§8)   │
        └───────────────────────┬──────────────────────────────┘
                                ▼
        ┌─ UNDO (à tout moment) ───────────────────────────────┐
        │  Rejoue le journal à l'envers. Même préflight.        │
        └──────────────────────────────────────────────────────┘
```

### Le préflight est la pièce qui manquait aux notes v1

Les cinq contrôles ne sont pas du confort :

1. **VPX en cours d'exécution** → écrire dans une install vivante corrompt des fichiers ouverts. On **refuse**, on n'avertit pas.
2. **Espace disque** → une sauvegarde tronquée est pire que pas de sauvegarde : elle donne un faux sentiment de sécurité.
3. **Droits d'écriture** → détecté avant d'écrire, pas au milieu du playbook.
4. **Confinement** → toute cible doit résoudre à l'intérieur des racines détectées (`InstallLayout`). C'est le filet sous ADR-005 : même si une règle malveillante passait la validation, le moteur refuse la cible.
5. **Re-vérification** → le finding est-il encore vrai ? Sinon on l'abandonne (`StaleDropped`) au lieu de « corriger » quelque chose qui va déjà bien.

---

## 6. Sémantique transactionnelle

**Atomicité par compensation, à l'échelle de l'item.**

- Un item échoue à l'étape N → les étapes N-1 … 1 sont annulées **en ordre inverse**, l'item est marqué `ItemRolledBack`.
- Les autres items du plan **ne sont pas touchés**. Deux correctifs indépendants ne s'annulent pas mutuellement.

### Le pire cas : l'annulation elle-même échoue

C'est le scénario qu'on n'a pas le droit de traiter à l'improviste.

> **On arrête immédiatement de compenser.** Continuer à annuler alors qu'une annulation vient d'échouer
> ne peut qu'aggraver l'état. On écrit `RecoveryRequired` dans le journal, avec le chemin exact
> de la sauvegarde et la liste précise des fichiers à restaurer à la main, et on affiche
> un écran de récupération.

L'utilisateur ne doit jamais se retrouver sans chemin de retour, même quand tout a échoué. Ce cas est celui qui décide si les gens nous font confiance après un incident.

*Vérifié par le harnais : compensation en ordre inverse, isolation entre items, arrêt de la compensation sur échec d'annulation.*

---

## 7. Sauvegarde, journal, undo

### Sauvegarde
- **Portée** : exactement les cibles touchées. Pas l'install entière — un pincab fait des centaines de Go, et la sauvegarde complète est une *fonctionnalité* de Repair (ADR-001), pas son filet de sécurité.
- **Emplacement** : `%LOCALAPPDATA%\PincabToolbox\backups\<planId>\`, **hors de l'installation**. Une install cassée ne doit pas emporter sa sauvegarde.
- **Contenu** : les fichiers + un manifeste JSON (chemin d'origine, hash, taille, horodatage). Pour le registre et SQLite : export ciblé de la clé / de la ligne avant écriture.
- **Rétention** : les 10 derniers plans. **Jamais de suppression automatique** du plan le plus récent, ni d'un plan marqué `RecoveryRequired`.

### Journal
Fichier **JSONL append-only**, une ligne par événement :

`PlanCreated` · `PreflightPassed` / `PreflightFailed` · `RuleRejected` · `StaleDropped` · `ItemSkipped` · `BackupCreated` · `ChangeApplied` · `ChangeFailed` · `ChangeReverted` · `ItemCompleted` · `ItemRolledBack` · `ItemUndone` · `RecoveryRequired` · `PlanCompleted`

Chaque `ChangeApplied` porte son `Before` et son `After` : **le journal est l'information d'annulation**, la sauvegarde n'est que le recours quand l'annulation échoue.

L'export du journal suit la **même règle d'anonymisation que le rapport de scan** (ADR-003) : il finira collé sur un forum.

### Undo
- Undo de session (« annule tout ce que je viens de faire ») et undo par item.
- Rejoue le journal à l'envers.
- **Passe le même préflight** — on n'annule pas non plus pendant que VPX tourne.
- **Idempotent** : annuler un item déjà annulé est un no-op journalisé, pas une erreur.
- Un undo est lui-même journalisé. Il n'y a pas d'action invisible.

---

## 8. Boucle de calibration

L'étape de vérification post-apply re-scanne les codes concernés. Le résultat est un signal local :

- Le code a disparu → le correctif a marché.
- Le code est toujours là → il n'a pas marché.

`ARCHITECTURE-KnowledgeEngine.md` §6 dit que la confiance « se calibre avec le réel ». Voilà par où elle entre. **En v1, ce signal reste local et n'est jamais envoyé** (zéro télémétrie, ADR-004) : il sert à afficher « ce correctif a échoué chez toi » et à alimenter un rapport que l'utilisateur peut **choisir** de coller sur le forum. Toute remontée automatique serait une rupture du contrat de confiance et exigerait un ADR à part.

---

## 9. Périmètre réel de la v1 *(mis à jour après implémentation)*

### Livré et testé

Le moteur complet : registre d'actions fermé, plan/dry-run pur, préflight à cinq contrôles,
sauvegarde ciblée, journal anonymisé, apply avec compensation, undo idempotent, vérification.
Plus le **chargeur de Knowledge Pack JSON**, tolérant aux paquets malformés.

**Deux actions**, pas trois :

| Code | Action | Réversible | Confiance | Mode résultant |
|---|---|---|---|---|
| `BLOCKED_DLL` | `unblock_file` — retire le flux `Zone.Identifier` | oui | **98** | Automatic |
| `ROM_UNZIPPED` | `restore_rom_archive` — recompresse et **met le dossier de côté** | oui | **88** | Confirmation |
| `POPPER_NOT_REGISTERED` | *(aucune en v1)* | — | — | **ManualOnly — voir ADR-007** |

> **Pourquoi seulement deux.** `POPPER_NOT_REGISTERED` demande d'écrire dans une base **SQLite**.
> Le Core sait la lire (writer maison en lecture seule) ; y écrire exigerait soit une dépendance
> — contraire à la règle zéro-dépendance —, soit un writer B-tree maison qui risquerait de
> **corrompre la bibliothèque de tables de l'utilisateur**. Décision et alternatives dans **ADR-007**.
> Aucune ligne de code n'a été nécessaire pour gérer ce cas : l'absence de règle dans le pack
> suffit à faire retomber le finding en manuel. C'est la preuve que la séparation d'ADR-005 tient.

> ⚠️ Les confiances **98** et **88** sont des points de départ. Elles doivent être **calibrées sur
> cab réel** après le lancement du Scanner, via la boucle de vérification (§8).

### Le playbook Migration 32→64 — vérifié partiel

Il se déclenche quand `BITNESS_MISMATCH_VPM` **et** `BITNESS_DMD64_MISSING` coexistent, applique
automatiquement le déblocage préalable, et déclare **deux étapes manuelles sur trois** — la
réinstallation de VPinMAME 64-bit et `dmddevice64.dll`, que nous ne fournirons jamais (ADR-004).

C'est structurellement honnête et c'est testé : `Test_EndToEnd_MigrationScenarioIsPartialAndHonest`
vérifie que l'utilisateur voit « 2 étapes ne peuvent pas être automatisées » **avant** de cliquer.

### Dehors, toujours
Tout téléchargement (ADR-004) · Focus Guardian et assistant écrans (Play Optimizer, ADR-001) ·
sauvegarde/migration complète d'install (v1.1, même moteur) · remontée automatique de calibration.

## 10. Ce qui reste à faire

1. **Brancher l'UI WPF** sur `IRepairEngine`. La copie des 4 écrans est prête (`UX-COPY-Repair.md`).
   Seul morceau non testable dans le cloud.
2. **Trancher ADR-006** (dry-run gratuit) — décision de revenu, elle appartient à Maxime.
3. **Trancher ADR-007** (écriture SQLite Popper) quand le terrain le demandera.
4. **Calibrer les confiances** sur cab réel, après le lancement du Scanner.

**Vérification en place** : `dotnet run --project tests/PincabToolbox.Repair.Tests -c Release`
→ 55 tests. Et `python3 knowledge/validate_pack.py … --registry src/PincabToolbox.Repair`
→ fait respecter ADR-005 mécaniquement, en lisant les `ActionId` **dans le code**.
