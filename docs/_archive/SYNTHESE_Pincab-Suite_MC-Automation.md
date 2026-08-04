> # ⚫ DOCUMENT MORT — NE PAS UTILISER
>
> Archivé le 25/07/2026. Sa carte à 3 produits et son usage de « FlipSync » comme nom de produit sont remplacés par **ADR-001**. Ses données de marché de juillet 2026 restent intéressantes à titre historique.
>
> La source de vérité est `docs/PROJECT-BRAIN.md`. Ce fichier est conservé
> uniquement pour retrouver le raisonnement d'origine.

---

# Pincab Suite — Synthèse stratégique & Roadmap

**MC Automation — Maxime Chauvin** · Version 1.1 — 18 juillet 2026
*Phase : fin de brainstorming, validé par recherche marché + arbitrage des revues externes (Gemini/GPT). Prochaine étape : exécution.*

---

## 1. Thèse

Suite d'utilitaires **payants, locaux, sans cloud obligatoire** pour la maintenance, la réparation et la migration des pincabs (Visual Pinball X / PinUp Popper). Modèle inspiré de LaunchBox Premium (75 $ lifetime, prospère depuis 10 ans dans la niche voisine des frontends arcade) : **gratuit utile → payant confortable**, polish professionnel, maintenance garantie.

Le territoire : tout ce qui se passe **après** l'installation initiale (chasse gardée de Baller Installer, gratuit et populaire — ne pas l'attaquer). Diagnostic, réparation, sauvegarde, migration, tuning.

Architecture pensée **Compound** dès le jour 1 : le moteur ne connaît pas VPX, il lit des profils. Changer de niche (sim racing, flight sim, arcade) = écrire un profil, pas réécrire le produit.

**Positionnement (l'axe de toute la communication) : on ne vend pas des modules, on vend de la tranquillité — « le mécanicien de votre pincab ».** Votre pincab fonctionne. S'il casse, on le détecte. S'il manque un fichier, on le trouve. Avant toute modification, on sauvegarde. Vous cliquez, c'est réparé.

**Règle de communication : en public, UN seul produit à la fois.** La suite est une stratégie interne, pas un argument marketing. FlipSync et la Tuning Suite restent masqués jusqu'à ~100 clients Toolbox — leur annonce créera alors de l'attente auprès d'une base existante.

---

## 2. Le marché (données du 18/07/2026)

| Donnée | Valeur | Source |
|---|---|---|
| Communauté française (Pincab Passion) | **18 777 inscrits**, pic 1 478 connectés simultanés (03/2026) | pincabpassion.net |
| Pratiquants actifs monde (estimation) | 30 000 – 80 000 | VPForums, VPUniverse, Reddit, FB |
| Acheteurs potentiels d'utilitaires | quelques milliers | estimation prudente |
| Coût d'un pincab (matériel) | 2 000 – 10 000 € | acheteurs démontrés |

**Preuves de volonté de payer :**

- VPForums vend un abonnement téléchargement illimité (~10 $) — la niche paie déjà pour la commodité.
- LaunchBox Premium : 75 $ lifetime, référence du modèle économique.
- Dons réguliers aux développeurs (Popper/NailBuster).

**Contre-preuve à respecter :** les outils gratuits sur VPUniverse plafonnent à quelques centaines de téléchargements ; culture du gratuit réelle. Conséquence : on ne vend pas « un petit exe », on vend **l'outil maintenu et poli qui répare ton pincab**.

---

## 3. État de la concurrence (recherche du 18/07/2026)

| Concurrent | État | Notre ouverture |
|---|---|---|
| VPBM v2.1 (backup, gratuit) | 238 dl, 0 avis, MAJ 03/2024, backups **verrouillés à la machine** | Anti-migration *par design* → FlipSync attaque là |
| PinCab.Configurator (all-in-one, open source) | 16 étoiles, abandonné | L'idée ne suffit pas : UX + maintenance + distribution font tout |
| VP Media Manager / ClrVpin | fichiers manquants basique (971/343 dl) | Notre scan doit être clairement supérieur (bitness, linter versions) |
| B2S ScreenRes Editor / Identifier | vieux, rudimentaires | Angle : drag visuel à la souris |
| Focus (perte de premier plan) | **aucun outil** — threads de détresse seulement | Voie totalement libre sur la douleur n°1 |
| USB Guard, profiler acoustique, nudge | rien (workaround SSF = REW + micro UMIK-1 ~100 €) | Voie libre |
| vpxtool / VPX-VBS-Extractor (diff scripts) | gratuits, open source | Diff = lead magnet, pas produit |
| Baller Installer | gratuit, populaire, install initiale | Ne pas concurrencer — se positionner *après* |

---

## 4. La gamme — 3 produits, 1 funnel

### Produit 1 : **Pincab Toolbox** — licence unique 29-39 €

*Le funnel : le scan gratuit révèle, la licence répare.*

**Le héros marketing est le Scanner, pas un module payant.** Personne ne cherche « Focus Guardian » sur Google ; les gens cherchent leurs symptômes : *VPX crash, écran noir, table ne démarre pas, rom missing, backglass absent, DLL 64-bit, erreur B2S*. Tout le contenu (posts forums, SEO, page produit) part de ces symptômes et mène au scan gratuit. Le scanner affiche « 15 problèmes détectés » → bouton « Réparer maintenant » → licence. Modèle CCleaner/Malwarebytes : le scanner est la publicité permanente. Focus Guardian reste le module payant le plus fort, mais c'est un argument de conversion, pas la vitrine.

**Tier gratuit (acquisition) :**
- ROM Validator — extraction `cGameName` des scripts, vérification `vpinmame/roms`, gestion des alias. 100 % local, **jamais** de lien de téléchargement de ROM.
- Linter de compatibilité — détection VPX 10.7/10.8, signatures nFozzy/Roth, alerte avant crash.
- Bitness Doctor (lecture seule) — scan des en-têtes PE de chaque DLL/plugin, détection des installs hybrides 32/64-bit.
- Update Watcher — comparaison avec la base VPS (JSON open source sur GitHub = source légale) ; lien vers la page officielle de la table, jamais de téléchargement auto.
- Diff-Master — comparaison visuelle de scripts entre deux versions d'une table.
- Audit de complétude — B2S présent ? ROM ? PUP-Pack ? nommage cohérent ?

**Tier payant (réparation) :**
- **Focus Guardian** (module vedette) — watchdog qui surveille le process VPX et restaure le premier plan instantanément. Douleur n°1, zéro concurrence.
- Bitness Doctor (réparation) — téléchargement/installation automatique des dépendances **open source uniquement** (Freezy, VLC, B2S, DOF) dans la bonne version/bitness.
- ScreenRes Visual Builder — fenêtres redimensionnables à la souris sur les écrans réels → écrit `ScreenRes.txt`.
- Popper Media Enforcer — drag-drop d'un média brut → renommage strict + placement `POPMedia` via lecture de `PUPDatabase.db` (SQLite).
- Freezy INI Sandbox — GUI cases/sliders → syntaxe parfaite dans `dmddevice.ini` ; inclut le vérificateur de compatibilité AltColor (VNI/PAC/Serum ↔ version dmddevice). Validation seulement, pas de conversion (des coloriseurs vendent leur travail — zone sensible).
- USB Guard — snapshot des VID/PID/GUID des contrôleurs (KL25Z/Pinscape), détection de dérive, réparation un-clic. Export des clés + point de restauration avant toute écriture registre.
- Performance (stuttering) — presets connus via NVAPI + mesure objective avant/après (PresentMon). Promesse : « diagnostique et améliore », jamais « corrige ».
- Validateur/déployeur DOF — vérifie et installe le fichier du DOF Configtool. (Ne **jamais** refaire le Configtool : base communautaire, guerre inutile.)

### Produit 2 : **FlipSync** — licence unique ~35 € (+ option cloud plus tard)

*Gate de lancement : ne se construit qu'après ~100 clients Toolbox. La réparation est une douleur quotidienne, la migration une douleur exceptionnelle — le Toolbox passe devant. Chaque réparation du Toolbox fait déjà un backup avant écriture : ce mini-backup sème l'argument FlipSync dans l'usage quotidien.*

- Sauvegarde silencieuse « Install & Forget » : configs, scripts, réglages DMD, nvram, bases PinUp (quelques Go critiques — pas les 500 Go de médias retéléchargeables).
- Versioning + destinations au choix : dossier local, NAS, dossier OneDrive/Drive déjà synchronisé, bucket S3/B2 du client (Bring Your Own Storage → zéro backend, zéro RGPD).
- **Migration Assistant (killer feature)** : remap des chemins absolus dans `PUPDatabase.db` (backup → find/replace validé → vérification d'existence de chaque fichier). Vendu au moment où l'utilisateur a le plus peur : le changement de PC. Le concurrent gratuit verrouille ses backups à la machine — nous, on fait de la migration l'argument central.
- V2 si traction : stockage cloud managé en abonnement pour les non-bricoleurs.

### Produit 3 : **Tuning Suite** — licence unique ~35 €

- Routage SSF un-clic : clés registre VPinMAME (4.0/7.1), sauvegarde/restauration des clés = argument de sécurité.
- Profiler acoustique : smartphone posé sur la vitre — **accéléromètre** pour la bande tactile 10-100 Hz (les micros coupent sous ~50 Hz), micro en mode measurement pour l'audible ; sweep joué depuis le PC, sync par détection de chirp (faisabilité prouvée par HouseCurve sur iOS) ; export courbes EQ → Equalizer APO / PinVol.
- Nudge Wizard : essais structurés → gain/deadzone/seuil de tilt écrits proprement.
- **Android-first** (écosystème de MC Automation, pas de licence Apple à 99 $/an). La variance des micros Android est neutralisée par la conception : l'accéléromètre (mesure principale, bande tactile) est homogène entre appareils, et l'EQ travaille en *relatif* (détection des pics de résonance), pas en réponse absolue calibrée.
- Alternative desktop + micro UMIK-1 examinée et **rejetée** : un outil exigeant un micro à 100 € affronte REW (gratuit, établi) sans différenciation. L'innovation est précisément le téléphone-capteur.
- Décision finale de plateforme à retrancher en début de Phase 3 — pas avant.

### Règles absolues (périmètre légal)

1. **Jamais** de téléchargement automatique de contenu (tables, ROMs, médias, colorisations) — seules les dépendances open source s'installent automatiquement.
2. Jamais de scraping des forums communautaires (VPForums, VPUniverse) — ce sont nos canaux de distribution, pas nos sources de données.
3. Marques tierces : usage nominatif descriptif uniquement (« compatible avec VPX/PinUp Popper »), jamais dans un nom de produit.
4. Toute écriture registre/base = sauvegarde + restauration un-clic préalables.
5. La communauté est l'actif n°1 : en cas de doute, demander avant de lancer.

---

## 5. Business model

| Levier | Détail |
|---|---|
| Vente | Lemon Squeezy (Merchant of Record : TVA internationale gérée, clés de licence incluses) → zéro backend |
| Prix | Toolbox 29-39 € · FlipSync ~35 € · Tuning ~35 € · **Bundle ~79 €** |
| B2B silencieux | Licence pro multi-machines 99-199 € pour les artisans qui vendent des pincabs configurés (10-50 machines/an ; 1 builder = 5 clients particuliers) |
| Coûts fixes | ~0 € (tout local, pas de serveur) ; budget certificat de signature de code à prévoir (SmartScreen/AV) |

**Projections mois 1 (Toolbox seul, lancement FR+EN) :**

| Scénario | Ventes | Revenu |
|---|---|---|
| Plancher (lancement ignoré) | 1-5 | **30-150 €** |
| Médian (bon accueil forums) | 20-40 | 600-1 200 € |
| Plafond (hit + hook 64-bit) | 100-150 | 3 000-4 500 € |

Croisière ensuite : 5-25 ventes/mois (150-750 €/mois), pics à chaque release VPX. **Plafond annuel réaliste des 3 produits : 10-30 k€/an.** Objectif assumé : complément solide de micro-entreprise, pas une fusée.

---

## 6. Architecture technique (décisions structurantes)

1. **Moteur agnostique + profils JSON.** Le scanner, le watcher, le backup ne contiennent aucune logique VPX en dur — ils exécutent des profils (chemins, formats, règles, signatures). Le profil « VPX/Popper » est le premier ; « SimHub », « MSFS », « LaunchBox » seront des fichiers, pas des forks.
2. **Local-first, zéro backend.** Aucune donnée ne quitte la machine. Licences validées via l'API Lemon Squeezy.
3. **Briques réutilisables** : scan engine (fichiers/PE/SQLite/registre) · watchdog (process/focus/USB) · backup-migrate engine (snapshot, versioning, remap de chemins) · DSP profiler (sweep, FFT, fitting EQ) · writers sûrs (INI/registre avec rollback).
4. Stack pressentie : app Windows en .NET (WPF) ou Tauri ; app mobile Tuning en Flutter/React Native ; à trancher en phase de plan technique.
5. Chaque écriture système = transaction réversible (backup automatique + bouton restaurer).

---

## 7. Roadmap

**Phase 0 — Scanner gratuit (2-3 semaines)**
MVP : ROM Validator + linter compatibilité + Bitness Doctor lecture seule + Update Watcher. Test sur le setup de Maxime. Post de lancement sur Pincab Passion (FR) puis VPUniverse/VPForums (EN), avec le hook du moment : le chaos 64-bit.
→ *Objectif : 200+ téléchargements et des retours qualitatifs en 30 jours.*

**Phase 1 — Toolbox payant, lancement itératif (3-4 semaines pour la v1)**
V1 avec 3 modules payants seulement : **Focus Guardian + Bitness réparation + ScreenRes Builder**. Lancement à 29 € (early bird) puis 39 €. Ensuite **un module par mois** (Media Enforcer → INI Sandbox → USB Guard → Performance → DOF) : chaque drop = un post de forum = la preuve vivante de maintenance que la niche exige. Les acheteurs early bird reçoivent tout — argument « le produit grandit ».
→ *Objectif : franchir 40 ventes cumulées.*

**Phase 2 — FlipSync (3-4 semaines) — gate : ~100 clients Toolbox**
Backup silencieux + Migration Assistant. Annoncé (« le Backup arrive ») puis vendu à la liste du Toolbox. Bundle activé.

**Phase 3 — Tuning Suite (4-6 semaines)**
Registre SSF + Nudge Wizard (Windows), puis app mobile profiler (Android-first, accéléromètre au centre).

**Phase 4 — Transplantation Compound (quand la niche est rentable)**
Par ordre d'attractivité : sim racing (backup configs + tuning buttkickers via SimHub) → MSFS (Community folder doctor) → arcade/LaunchBox (Kiosk Guardian). Chaque port = un profil JSON + un habillage.

**Parking (revoir plus tard, ne pas construire maintenant) :**
- Leaderboards/tournois (effet de réseau, backend, anti-triche — contraire aux filtres actuels)
- VPX Standalone Linux/Batocera (segment émergent)
- Frictions VR spécifiques
- Stockage cloud managé FlipSync v2

---

## 8. Risques & parades

| Risque | Parade |
|---|---|
| Culture du gratuit | Polish niveau LaunchBox, maintenance visible, scan gratuit réellement utile |
| SmartScreen / antivirus (watchdog, registre) | Certificat de signature de code dès la Phase 1 ; comportements documentés publiquement |
| VPX évolue (10.9, 64-bit, Standalone) | Architecture par profils = mise à jour de données, pas de code |
| Niche petite | B2B builders + bundle + transplantation Compound (Phase 4) |
| Backlash communautaire (« encore un truc payant ») | Tier gratuit généreux, respect absolu des règles de périmètre, présence humaine sur les forums |
| Solo-dépendance (bus factor) | Code simple, documenté, généré/maintenu avec Claude ; pas de backend à surveiller |

---

## 9. Critères d'arrêt (kill criteria — à s'imposer)

- Phase 0 : **< 100 téléchargements et zéro retour enthousiaste en 30 jours** → pivoter le positionnement ou réévaluer avant d'écrire la moindre ligne du Toolbox payant.
- Phase 1 : **< 15 ventes en 60 jours malgré 500+ scans gratuits** → problème de conversion : revoir prix/módules avant de construire FlipSync.
- Ne jamais engager la Phase 4 tant que la niche pincab ne génère pas ≥ 300 €/mois stables.

---

## 10. Décisions — état après arbitrage des revues externes (18/07)

| Décision | Statut | Résolution |
|---|---|---|
| **Stack Windows** | ✅ **Close** | .NET 8 / C# + WPF (UI modernisée via lib type WPF-UI). Accès natif registre/processus/USB/PE/PresentMon ; C# = fiabilité maximale en génération assistée. Tauri rejeté (friction Rust ↔ API Windows bas niveau). Avalonia notée pour un éventuel port Linux/Standalone futur. |
| **Certificat de signature** | ✅ **Close** | Obligatoire, ~150-200 €/an (OV). Pas pour le scanner Phase 0 (alerte SmartScreen assumée et expliquée sur les forums) ; achat déclenché par la validation de la Phase 0, pour signer la V1 payante. |
| **Ce qu'on vend** | ✅ **Close** | De la tranquillité — « le mécanicien de votre pincab ». Voir Positionnement (§1). |
| **Nom de marque** | ⏳ Ouverte — budget 30 min max | Pattern retenu : **ombrelle neutre + noms produits descriptifs** (« Pincab Toolbox by X » → demain « Sim Racing Toolbox by X »). Le nom cherchable/SEO est « Pincab Toolbox » ; l'ombrelle peut attendre le produit n°2. Shortlist à vérifier (domaine + marques) : RigKit, RigSync, ForgeKit, Maintiq. Pré-revenu, y passer plus de 30 minutes = procrastination déguisée. |

## 11. Arbitrage des revues externes — traçabilité

Intégré : scanner = héros marketing (SEO par symptômes, modèle CCleaner) · positionnement « tranquillité » · suite masquée en public jusqu'à ~100 clients · Toolbox itératif (3 modules v1 puis un/mois) · FlipSync repoussé derrière le gate · stack .NET tranchée · pattern de marque neutre.

Rejeté : pivot desktop + micro UMIK-1 pour le Tuning (tue la différenciation face à REW gratuit ; l'innovation EST le téléphone-capteur) · avancer FlipSync (Gemini) — la réparation est quotidienne, la migration exceptionnelle (GPT arbitré gagnant).

*Document généré le 18/07/2026 sur la base de la recherche marché du même jour. Sources : vpuniverse.com, vpforums.org, pincabpassion.net, github.com (vpxtool, PinCab.Configurator, dmd-extensions), launchbox-app.com, nailbuster.com, superfromnd.gitlab.io.*
