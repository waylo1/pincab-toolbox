# HANDOFF — Pincab Toolbox / FlipSync

*Point d'entrée d'une session Cowork neuve. Dernière MAJ : **27/07/2026 (soir — pivot lancement : Maxime en vacances, bêta ouverte sans validation cab, exécution PC assistée par Cowork).***

---

## 🎯 GOAL DE CETTE SESSION

**Le Scanner gratuit est prêt à lancer.** Toute la préparation est faite. Pivot du 27/07 soir : Maxime en vacances ne teste pas sur cab avant de publier → **Cowork pilote le build/la capture démo via le PC connecté**, Maxime garde la main sur le feu vert de déploiement/publication.

Le **prochain gros chantier Cowork est l'audit juridique de pré-commercialisation** — à lancer **avant toute vente** (Repair), pas bloquant pour ce lancement gratuit. Si Maxime dit « on lance l'audit juridique », **c'est le goal de la session** (scope plus bas). Sinon, aider à boucler les points de lancement ci-dessous.

> Ne propose pas de coder l'UI Repair : le lancement passe toujours devant, et cet arbitrage est acté (le moteur Repair attend des utilisateurs pour calibrer ses confiances). La couche indicateurs (`SUCCESS-METRICS.md`) est **figée** — ne pas la rouvrir sans raison nouvelle.

### État au 27/07 (ce que la session précédente a fait)

- **3 retouches de code appliquées** — *à recompiler chez Maxime + retester* :
  - `Knowledge.cs` : `POPPER_NOT_REGISTERED` → `AutoFixable=false` (Repair v1 ne réécrit pas la base Popper, ADR-007). Le badge « Repair · bientôt » ne s'affiche plus que sur `BLOCKED_DLL` et `ROM_UNZIPPED` (les 2 vraies actions).
  - `Loc.cs` : `about.roadmap` FR+EN ne promet plus de fonctions hors périmètre (fini « gardien de focus / réglage écrans / renommage médias » = lignes parquées).
  - `MainWindow.xaml.cs` : « Copier les détails » passe par `Public()` (scrub, ADR-003).
- **Landing corrigée, NON déployée** (`flipsync-site/landing/index.html`) : tunnel basculé en **téléchargement direct** (formulaires bêta → boutons « Télécharger »), `og:image` ajoutée, 7ᵉ carte module (DLL bloquées + dépendances), durées de scan assouplies.
- **Posts forum remplacés** par les v2 (sur-promesse Repair retirée, durée alignée).
- **Couche indicateurs ajoutée et FIGÉE** : `docs/SUCCESS-METRICS.md` (11 KPI, chacun déclenche une décision), `docs/adr/ADR-008` (pilotage par KPI), `knowledge/FIELD-LOG.md` (journal de terrain).
- **Paiement décidé** : **Lemon Squeezy (Merchant of Record)** — `docs/adr/ADR-009`, Brain §5 dé-parqué.

### Pivot décidé le 27/07 au soir (Maxime, en vacances)

Maxime ne peut pas tester sur son cab physique cette semaine. Décision prise avec lui :

- **Pas de test sur cab avant publication.** À la place : lancement en **bêta ouverte**, la communauté devient le testeur. Les 2 posts forum (v2) ont été retouchés en conséquence — cadrés explicitement comme un appel à bêta-testeurs (« pas encore de scan validé sur cab réel »), pas comme un résultat validé. Risque assumé, pas ignoré : cf. règle absolue « un faux positif juste avant le post tue la crédibilité » — d'où l'ajustement de ton plutôt qu'un silence sur le sujet.
- **Capture d'écran = mode démo intégré**, pas un scan réel (l'app a un mode démo prévu à cet effet). Les 2 posts référencent maintenant explicitement que la capture vient du mode démo.
- **Audit juridique laissé de côté pour ce lancement** : cohérent avec les docs existantes — le Scanner est gratuit (pas de vente, zéro télémétrie), l'audit juridique du Brain cible « avant toute vente » (CGV/EULA). Pas de CGV/EULA nécessaires pour publier un outil gratuit en lecture seule. L'audit reste programmé pour **avant Repair/toute commercialisation**.
- **Exécution du reste (build, capture démo, remplissage des URLs) pilotée par Cowork ce soir**, via contrôle à distance du PC connecté de Maxime — feu vert explicite requis avant tout déploiement de landing ou publication forum (règle inchangée).

### Ce qui reste, dans l'ordre

**Fait le 27/07 au soir :** build (`publish\PincabToolbox.exe`, 56/56 tests), captures mode démo FR+EN dans `marketing/`, release GitHub v0.1.0-alpha, URL de téléchargement posée dans la landing (2 boutons) et les 2 posts.

1. **Déployer la landing** (`flipsync-site/landing/`, projet Vercel déjà lié `prj_xiFyfyn…`). Aucun identifiant Vercel côté Cowork → Maxime seul.
2. **Poster** Pincab Passion (FR), puis VPUniverse (EN) quelques jours après. Comptes forum → Maxime seul. Joindre la capture correspondante depuis `marketing/`.
3. **Tester le bouton de téléchargement** une fois la landing en ligne (l'URL `releases/latest/download/` est un redirect GitHub — vérifier qu'il déclenche bien le téléchargement).
4. **Répondre à chaque retour sous 48 h** et consigner dans `knowledge/FIELD-LOG.md`. Priorité absolue au premier faux positif critique signalé : corriger + remercier publiquement.
5. **Test sur cab réel** dès que possible — vérifier en priorité l'anonymisation sur un export réel (nom de compte Windows + `C:\Users\` dans les 5 exports + le presse-papiers).

### Décisions du 27/07 au soir (Maxime) — ne pas rouvrir

- **Landing = canal hors forum, pas cible du lancement.** Les 2 posts pointent directement sur GitHub (lien brut = plus de confiance sur un forum, et pas de redirection vers un site commercial). La landing sert en réponse ciblée à quelqu'un qui galère dans un fil, et pour les audiences hors forum (Facebook, bouche-à-oreille, recherche). Ne pas la lier dans les posts d'annonce.
- **Signature du code : reportée, assumée.** Un OV (~219 $/an) ne supprimerait pas l'alerte SmartScreen (réputation à construire organiquement) ; seul un EV (~325 $/an) le fait, avec validation d'entreprise + token matériel obligatoire depuis juin 2023. Public VPX tolérant aux exe non signés, et le post explique l'alerte. **Le bon moment = quand on vend** (un client payant qui voit l'avertissement = problème de conversion), donc même fenêtre que l'audit juridique et Lemon Squeezy. Justifié par ADR-008 : pas de dépense avant baseline des KPI #4 et #11.

### ⚠️ Trou ouvert — captation de l'appétit Repair (KPI #10)

La bascule de la landing en téléchargement direct a supprimé les formulaires d'opt-in. **Plus rien ne capte « préviens-moi quand Repair existe ».** Or `SUCCESS-METRICS` #10 déclenche le **go/no-go commercial** de Repair. Seul signal restant : les commentaires forum (#9), c'est mince pour une décision de cette taille. À trancher avant que le flux de curieux du lancement ne soit passé.

### Le prochain chantier Cowork — audit juridique (quand le Scanner est prêt)

Audit **complet** avant commercialisation, tous risques identifiés puis **classés par priorité**, avec un **plan d'action jusqu'au lancement** :
marque · propriété intellectuelle · licences open source (Freezy dmd-extensions, VLC, B2S, DOF…) · conformité RGPD · CGV & EULA (à jour du renouvellement annuel, ADR-002) · responsabilité · protection de la base de connaissances (Knowledge Pack) · concurrence · risques **juridiques et fiscaux** d'une micro-entreprise française vendant un logiciel dans le monde entier (TVA/OSS, seuil B2C UE 10 000 €, franchise en base, rôle du Merchant of Record Lemon Squeezy).
Au sens d'ADR-008, ce chantier se justifie par **réduction de risque** (pas par KPI) — il est légitime dans la roadmap. Rappel : Claude n'est ni juriste ni fiscaliste → livrer une analyse de risques et un plan, en pointant ce qui doit être validé par un avocat / comptable.

### Ce qu'il ne faut PAS faire

- **Ne pas coder l'UI Repair** (elle est conçue, copie écrite dans `docs/UX-COPY-Repair.md` ; elle attend des utilisateurs).
- **Ne pas ajouter de checks au Scanner** avant le lancement (un faux positif juste avant le post tue la crédibilité).
- **Ne rien déployer ni publier** sans feu vert explicite.
- **Ne pas rouvrir les décisions figées** : ADR-001→009 (dont 008 pilotage KPI, 009 Lemon Squeezy), la carte produit, et les sujets parqués (pincab hors Windows). La couche indicateurs est figée.

---

## Lis ceci, dans cet ordre

1. **`docs/PROJECT-BRAIN.md`** — la source de vérité. **En cas de contradiction, le Brain gagne.**
2. **`docs/SUCCESS-METRICS.md`** — le tableau de bord (se lit en < 2 min : dit où concentrer l'effort).
3. **`docs/adr/`** — les 9 décisions figées.
4. Le reste **uniquement si la tâche le demande** : `ARCHITECTURE-KnowledgeEngine.md`, `DESIGN-Repair-v1.md`, `UX-COPY-Repair.md`, `PARKING-*`.

Ne relis jamais toute la documentation « pour se mettre en contexte ». Le Brain + SUCCESS-METRICS suffisent.

---

## Où sont les fichiers

**Source de vérité :** `C:\Users\User\Desktop\Pincab suite\pincab-toolbox-v0.1.1-alpha-src\pincab-suite\`
Landing + légal (hors dépôt principal) : `C:\Users\User\Desktop\Pincab suite\flipsync-site\`

Depuis Cowork, via le device bridge (`mcp__remote-devices__*`) : `device_list_dir` / `device_stage_files` pour lire, `device_bash` pour éditer en place, `SendUserFile` → `device_commit_files` pour réécrire. Demander l'accès aux deux dossiers en début de session.

---

## Où on en est

| Bloc | État |
|---|---|
| **Scanner** (`Core`, 7 scanners) | ✅ alpha 0.1.0 — 56 tests verts, rapports anonymisés. **Retouches app à recompiler.** Jamais publié, jamais testé sur cab réel. |
| **Repair** (moteur v1) | ✅ 61 tests verts. 2 actions. **UI non codée — attend le lancement.** |
| **Knowledge Pack** | ✅ format + pack 2026.08 + validateur CI. Field Log prêt à alimenter les millésimes. |
| **Landing** | ⚠️ corrigée, **URL de téléchargement en place**, **non déployée** — reste le déploiement Vercel (projet déjà lié). |
| **Posts forum** | ✅ **prêts à publier** — cadrage bêta ouverte, URL + captures démo en place. Reste la publication (comptes forum de Maxime). |
| **Paiement** | ✅ décidé : Lemon Squeezy (ADR-009). Intégration = Phase 3, ne bloque pas le lancement. |
| **CGV / EULA** | ⚠️ brouillon — pas requis pour ce lancement (Scanner gratuit, pas de vente) ; à traiter dans l'audit juridique avant toute vente (Repair). |
| **Indicateurs** | ✅ SUCCESS-METRICS + ADR-008 + FIELD-LOG. **Couche figée.** |
| **Release GitHub** | ✅ `waylo1/pincab-toolbox` v0.1.0-alpha publiée le 27/07. Repo **volontairement vide** (README seul) pour ne pas exposer le source ; l'exe est en asset. URL stable utilisée : `releases/latest/download/PincabToolbox.exe` (pas besoin de re-modifier la landing aux versions suivantes). |
| **Validation cab réel** | ❌ **reportée après lancement** (Maxime en vacances) — remplacée par bêta ouverte communautaire. Décision du 27/07 soir, pas une omission. |

**Aucune décision produit en attente.** Reste : capture démo + URLs + feu vert déploiement/post, puis l'audit juridique (avant Repair).

### Build — fait le 27/07 au soir, `publish\PincabToolbox.exe` généré (56/56 tests verts)

`build.cmd` avait 2 trous qui l'empêchaient de tourner tel quel sur un poste neuf — corrigés dans le script :
1. Il ne générait pas les fixtures de test (`tests/fixtures/out`, gitignorées) avant de lancer les tests → ajout de l'étape `[1/4] Generating test fixtures` (`python`/`py tests\fixtures\make_fixtures.py`).
2. `NuGet.Config` vide les sources de paquets (`<clear />`, voulu — l'app n'a zéro dépendance tierce), mais `dotnet publish --self-contained` a besoin de télécharger les **runtimes officiels .NET/WPF** (pas des dépendances tierces) au moins une fois → ajout de `-p:RestoreSources=https://api.nuget.org/v3/index.json` **sur cette seule commande**, `NuGet.Config` lui-même n'est pas touché. (Le flag court `-s` a d'abord été essayé et rejeté par ce SDK — `-p:RestoreSources=` fonctionne.)

---

## Comment vérifier que rien n'est cassé

**Du code a changé cette session — recompiler et retester est important.** Le SDK .NET n'est pas préinstallé dans le conteneur Cowork ; une fois posé (`sudo apt-get install -y dotnet-sdk-8.0`), tout se compile et se teste dans le cloud (sauf le WPF) :

```
python3 tests/fixtures/make_fixtures.py
dotnet run --project tests/PincabToolbox.Core.Tests   -c Release   # 56 passed
dotnet run --project tests/PincabToolbox.Repair.Tests -c Release   # 61 passed
python3 knowledge/validate_pack.py knowledge/pack-2026.08.json --registry src/PincabToolbox.Repair
cd knowledge && python3 selftest.py                                # 12/12
```

**WPF non compilable dans le cloud** → éditer prudemment, vérifier structurellement (XAML valide, `Click`/`x:Name` résolus, clés Loc EN **et** FR), Maxime recompile. **Chez Maxime :** `cd ...\src\PincabToolbox.App && dotnet run` · `build.cmd` → `publish\PincabToolbox.exe`.

---

## Les cinq règles absolues

1. **On vérifie, on ne fournit jamais** (aucun téléchargement de tables/ROMs/médias ; exception : dépendances open source).
2. **Lecture seule par défaut** — zéro télémétrie, gratuit jamais bridé. *(S'applique aussi à la mesure des KPI : `SUCCESS-METRICS §Comment on mesure`.)*
3. **Repair est un système critique** — sauvegarde → dry-run → opt-in → annulation → journal.
4. **Pas de scraping** des forums communautaires : ce sont nos canaux de distribution.
5. **Toute écriture est réversible.**

**La confiance est le seul actif qui ne se rachète pas.**
