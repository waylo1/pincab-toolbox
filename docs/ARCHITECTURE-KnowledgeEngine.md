# Pincab Toolbox — Architecture « Knowledge Engine »

> Document de référence produit & technique.
> Objectif : faire de Pincab Toolbox **le moteur de diagnostic de référence** de l'écosystème Virtual Pinball — pas « un scanner de plus ».


> **Ce document décrit le moteur.** Les décisions produit, économiques et de périmètre vivent dans
> `PROJECT-BRAIN.md` et `adr/` — en cas de contradiction, le Brain gagne.

---

## 1. Vision en une phrase

Quand un pincab a un problème, le premier réflexe de la communauté doit être :
**Lance Pincab Toolbox → Health Check → Poste le rapport.**

Le scanner gratuit (lecture seule) **bâtit la confiance** et alimente la connaissance.
Le module Repair (payant) **applique** cette connaissance, sans jamais trahir la confiance.

---

## 2. Les 4 principes non négociables

1. **La justesse avant le nombre.** Mieux vaut 30 checks qui couvrent 90 % des pannes fréquentes que 200 checks artificiels. Le nombre viendra des cas réels. Métrique cible : *% des pannes réelles qu'un scan explique correctement*, pas le compte de checks.
2. **Lecture seule = sanctuaire.** Le scanner ne modifie jamais rien, ne télécharge rien, zéro télémétrie. C'est *pour ça* qu'on lui fera confiance au point d'en faire un réflexe. On ne franchit jamais cette ligne dans le produit gratuit.
3. **Repair = système critique.** Toute réparation : sauvegarde automatique → aperçu (dry-run) → opt-in par correctif → annulation possible → journal détaillé. Le scanner construit la confiance ; Repair ne doit jamais la détruire.
4. **Le moat, c'est la connaissance, pas l'UI.** L'avantage défendable est la base de cas vérifiés, pas le code. On l'architecture pour qu'elle grandisse *avec la communauté*, pas seulement avec nos commits.

---

## 3. La pipeline

```
Checks  ──►  Findings  ──►  Scénarios  ──►  Repair
(code)       (données)      (corrélation)   (actions sûres)
```

- **Check** : un détecteur (code) qui repère une condition et émet un ou plusieurs `Code`.
- **Finding** : une occurrence détectée, identifiée par son `Code` (déjà présent dans le modèle actuel).
- **Scénario** : une corrélation de plusieurs Findings → une *cause racine* nommée.
- **Repair Rule** : une action de correction, rattachée à un Finding **ou** à un Scénario.

`Code` est la **clé de jointure** entre les quatre étages. Un même `Code` relie : le détecteur qui l'émet, son entrée de connaissance, ses règles de réparation, et les scénarios qui l'utilisent.

---

## 4. Séparation Moteur / Connaissance

**Le Moteur reste dans le code** (C#) : lecture des INI/registre, parsers, inspection PE, logique complexe, exécution des réparations. Ce qui exige du *vrai code* reste du code — on ne transforme pas le moteur en interpréteur générique.

**La Connaissance devient de la donnée** (Knowledge Pack, JSON/YAML) : descriptions, gravités, causes, impacts, procédures, règles de réparation, niveaux de confiance, liens, compatibilité. Mise à jour **indépendamment de l'app**, par-dessus le réseau — comme la base VPS déjà utilisée, ou les définitions d'un antivirus.

Bénéfices : passer de 10 à 200 checks **sans sortir un nouveau .exe** ; permettre à des non-développeurs (et à la communauté) d'enrichir la base ; corriger un texte ou une confiance en quelques minutes.

---

## 5. Modèle de données

### 5.1 Finding (émis par le moteur)

```
Finding {
  code            // ex. "BLOCKED_DLL" — clé vers la connaissance
  severity        // Critical | Warning | Info | Ok
  category        // rom | bitness | completeness | compat | updates | security | display | dof | ...
  subject         // table ou fichier concerné
  filePath        // chemin absolu (usage interne / bouton "ouvrir")
  args[]          // arguments pour le template localisé
  detectionConfidence   // 0–100 : suis-je sûr que la condition est VRAIE ?
  evidence[]            // ce qui a été RÉELLEMENT observé sur cette machine — voir ADR-003
}
```

> `evidence[]` (par occurrence, propre à la machine) et `verification` (§5.2, texte statique par code)
> répondent à deux questions différentes : *ce que j'ai vu chez toi* vs *comment je vérifie, en général*.
> Ne jamais les fusionner. Détail et règles d'anonymisation à l'export : **ADR-003**.

### 5.2 Knowledge Entry (dans le pack, clé = `code`)

```
KnowledgeEntry {
  code
  title_fr / title_en
  explanation_fr / explanation_en   // ce qui se passe, en clair
  probableCause                     // pourquoi ça arrive
  impact                            // conséquence concrète pour l'utilisateur
  verification                      // comment le check le vérifie (transparence)
  references[]                      // liens forum / wiki / repo officiel
  repairRules[]                     // voir 5.3 (peut être vide → diagnostic seul)
  appliesTo                         // compat : versions VPX / VPinMAME / Popper concernées
}
```

### 5.3 Repair Rule (dans le pack)

```
RepairRule {
  id
  code                    // finding qu'elle corrige (ou scenarioId)
  repairConfidence        // 0–100 : ce correctif est-il SÛR et PERTINENT ici ?
  reversible              // true/false — sinon on n'auto-répare pas
  action                  // identifiant d'action exécutée par le moteur (ex. "unblock_file")
  manualProcedure_fr/en   // procédure pas-à-pas si non automatisable
  backupRequired          // true par défaut
}
```

### 5.4 Scénario (dans le pack)

```
Scenario {
  id                      // ex. "MIGRATION_32_TO_64_INCOMPLETE"
  title_fr / title_en
  when                    // motif : combinaison de codes requis / optionnels
     requires[]           //   codes obligatoires
     supports[]           //   codes qui renforcent la confiance
     excludes[]           //   codes qui l'invalident
  scenarioConfidence      // 0–100, calculé selon les codes présents
  explanation_fr / en     // le diagnostic racine, en clair
  triggeredBy[]           // codes réellement présents → AFFICHÉ à l'utilisateur (transparence)
  repairPlaybook[]        // suite ORDONNÉE d'actions (l'ordre compte)
}
```

---

## 6. Le modèle de confiance (le garde-fou réputation)

**Deux confiances distinctes — ne jamais les confondre :**

- `detectionConfidence` : sûreté que la condition est vraie.
- `repairConfidence` : sûreté que le correctif est sûr *et* pertinent pour ce cas.

**Le gating de Repair porte sur `repairConfidence` :**

| repairConfidence | Comportement |
|---|---|
| 95–100 % | Réparation automatique possible (toujours avec backup + journal) |
| 70–95 %  | Réparation **proposée**, confirmation explicite requise |
| < 70 %   | **Diagnostic seul** — procédure manuelle affichée, aucune action auto |

Règle d'or : une réparation **non réversible** n'est jamais automatique, quelle que soit la confiance.

**La confiance est vivante.** Elle est stockée dans le pack et se **calibre avec le réel** : les retours (« ce correctif a marché / échoué ») font monter ou descendre le score. On ne fige pas un chiffre au doigt mouillé dans le code.

---

## 7. Scénarios : règles d'or

- **Confiance propre** à chaque scénario, affichée (« Migration 32→64 incomplète — 96 % »).
- **Transparence obligatoire** : toujours montrer les Findings qui l'ont déclenché (`triggeredBy`). Jamais de boîte noire — un scénario faux mais « intelligent » détruit la confiance plus vite qu'un finding faux.
- **Conservateur** : n'émettre que si le motif est fort. Des findings coïncidents ne font pas une panne. En cas de doute → pas de scénario, on reste sur les findings.
- **Playbook ordonné** : un scénario peut porter une suite de réparations dans le bon ordre (le séquencement compte). C'est plus fort que la somme des correctifs isolés.
- **Effet UX** : le rapport passe de « 15 problèmes à plat » à « 1 diagnostic principal qui en explique 6, + les indépendants ». Bien plus lisible et bien plus *partageable*.

---

## 8. Le flywheel (pourquoi la base grandit toute seule)

```
Utilisateurs ──► Scans ──► Rapports postés ──► Données de frictions réelles
      ▲                                                      │
      └───────── Base de connaissance meilleure ◄────────────┘
```

Chaque rapport posté, chaque « ce check s'est trompé / ce fix a marché » est un signal. **La base ne grandit pas parce qu'on code plus — elle grandit parce que la communauté rencontre des pannes et qu'on les capture.** D'où l'intérêt de rendre le Knowledge Pack **ouvert / contribuable** (modèle Virtual Pinball Spreadsheet) : ça allège le solo *et* ça approfondit le moat (la communauté a un intérêt à ce que Pincab Toolbox reste la référence).

---

## 9. Contrat de sûreté de Repair (rappel, à ne jamais assouplir)

Avant toute modification :
1. **Sauvegarde automatique** du/des fichier(s) concerné(s) (avec horodatage).
2. **Dry-run** : montrer exactement ce qui va changer, avant.
3. **Opt-in par réparation** : jamais d'application en masse aveugle.
4. **Annulation** : chaque action doit pouvoir être défaite (restauration du backup).
5. **Journal détaillé** : quoi, quand, quel fichier, ancienne → nouvelle valeur.
6. Une action **non réversible** ⇒ jamais automatique.

---

## 10. Séquencement (tempo pour un solo)

**Maintenant** (cheap tant que c'est petit) :
- Poser l'architecture data-driven : `Code` comme clé, champ `detectionConfidence` sur Finding, structure du Knowledge Pack, table `Scenario` (même vide).
- Ajouter les checks lecture-seule à haute fréquence (voir §11).
- Garder le format d'affichage « Scanning N health checks… » — mais N réel et utile.

**Ensuite** (avec les vrais rapports) :
- Remplir scénarios et calibrer les confiances à partir des cas réellement rencontrés.
- Ouvrir le pack à la contribution communautaire.
- Développer Repair sur les seuls correctifs à `repairConfidence` élevée et réversibles.

**Ne pas faire** : construire 186 checks ou 50 scénarios dans le vide avant d'avoir des utilisateurs.

---

## 11. Roadmap des prochains checks (tous lecture seule, haute fréquence)

Priorisés par fréquence réelle observée sur les communautés (VPForums, VPUniverse, Pincab Passion, nailbuster wiki) :

1. **DLL bloquées par Windows** (`Zone.Identifier`) — *fait* (module `BlockedFileScanner`).
2. **Serveur B2S / FlexDMD requis mais absent** — *fait* (module `DependencyScanner` : `B2S_SERVER_MISSING`, `FLEXDMD_MISSING`).
3. **Backglass orphelin, ROM dézippée, média Popper (wheel) manquant, mismatch bitness inversé** — *fait* (dans Install Auditor / ROM Validator / Bitness Doctor).
4. **Cohérence `dmddevice.ini`** — présence + « Use External » cohérent (complète Bitness Doctor).
5. **Sanity des écrans via `VPinballX.ini`** — fenêtres hors écran / écrans mal assignés (cause n°1 des « playfield sur le backglass »).
6. **Validation de la config DOF** (`directoutputconfig` / `GlobalConfig`) — parse + syntaxe.
7. **Structure complète des PUP-Pack** — arborescence, dossiers imbriqués, `screens.pup`.

**Hors périmètre tant que ça exige de lancer les tables** : mesure des FPS, détection matérielle en temps réel. On ne casse pas la philosophie « lecture seule, zéro risque » pour ça.

---

## 12. La formule finale

> **Checks → Findings → Scénarios → Repair Rules**, chacun porté par un **niveau de confiance mesuré** et une **base de connaissance vivante et partagée**.
>
> On ne construit pas un scanner. On construit un **moteur d'expertise** qui explique la cause des pannes et guide la réparation — au point que la communauté le considère comme *la* référence à lancer avant toute tentative de réparation.
