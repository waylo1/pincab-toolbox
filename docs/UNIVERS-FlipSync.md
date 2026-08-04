# FlipSync — L'univers du flipper : carte stratégique

**MC Automation — Maxime Chauvin** · Brouillon v0.1 — 22 juillet 2026
*But du document : cartographier tout l'univers du flipper (virtuel + physique), les problèmes réels par segment, et les produits gratuits/payants que FlipSync peut lancer. À relire à froid et affiner ensemble. Rien n'est figé — c'est une base de discussion, pas une décision.*

---

> ## ⚠️ Statut : document de RECHERCHE — ne porte plus aucune décision
>
> Depuis le **25/07/2026**, ce fichier est conservé pour sa **cartographie de l'univers du flipper**
> (segments, acteurs, couches logicielles, ce qui existe déjà). C'est sa seule valeur.
>
> **Les décisions qu'il contenait ont été extraites** vers `PROJECT-BRAIN.md` et `adr/`.
> En cas de contradiction, **le Brain gagne**. Ne cite jamais ce document comme référence de décision.
>
> Deux points en particulier ont changé depuis sa rédaction : la **carte produit** (ADR-001)
> et le **modèle économique** (ADR-002).

---

## 0. Comment lire ce document

FlipSync est pensé comme une **marque-parapluie** : plusieurs produits sous un même toit, pour le pincab virtuel **et** les flippers physiques. Pincab Toolbox (le scanner gratuit) est le premier produit, la tête de pont.

Le fil rouge de toute la stratégie est un principe déjà inscrit dans ton architecture : **le moteur ne connaît pas VPX, il lit des profils.** Autrement dit, ce qu'on construit n'est pas « un scanner de pincab », c'est un **moteur d'expertise** (symptôme → cause → correctif, avec base de connaissance vivante). Ce moteur se transporte d'un segment à l'autre — même quand le *scan automatique* ne se transporte pas.

Trois questions traversent chaque segment :
1. **Où est la « couche logicielle » ?** (fichiers, ROMs, firmware, config) — c'est là qu'un outil peut agir.
2. **Quels problèmes réels** vivent les gens, et comment ils les résolvent aujourd'hui (souvent : forums + tâtonnement).
3. **Quel produit** — gratuit (aimant/confiance) ou payant (revenu) — et **le modèle "scanner" s'applique-t-il** ou faut-il un autre format ?

---

## 1. Vue d'ensemble : 5 segments, un pont

| # | Segment | Couche logicielle | Le "scanner PC" s'applique ? |
|---|---|---|---|
| 1 | **Pincab virtuel** (VPX, Future Pinball, Pinball FX…) | Énorme : tables, ROMs, DLL, backglass, DOF, médias, frontend | ✅ Oui — c'est le terrain de Pincab Toolbox |
| 2 | **Tables non-officielles / homebrew / téléchargements** | Fichiers `.vpx`/`.fp` + scripts VBScript, provenance incertaine | ✅ Oui — transversal au virtuel, angle sécurité/intégrité |
| 3 | **Flippers physiques modernes** (Stern, JJP, Chicago Gaming…) | Firmware/code mis à jour par USB ; réglages ; logs | ⚠️ Partiel — machine embarquée, pas un PC Windows |
| 4 | **Flippers physiques classiques/EM** (Williams, Bally, Gottlieb…) | ROMs (PinMAME), cartes MPU, pile, faisceaux | ⚠️ Partiel — surtout de la connaissance + du matériel |
| 5 | **Le pont virtuel ↔ physique** : colorisation (Pin2DMD/Serum), son (PinSound), ROMs/PinMAME | Fichiers de colorisation, packs son, ROMs — **communs aux deux mondes** | ✅ Oui — c'est le vrai point de recouvrement technique |

**L'idée-force** : le segment 5 (le pont) est ce qui légitime la marque FlipSync au-delà du pincab. Pin2DMD, Serum, PinSound, PinMAME servent **à la fois** les pincabs et les vrais flippers. C'est le tissu qui relie « produits pincab » et « produits flipper physique » — et donc la preuve que FlipSync n'est pas juste « des outils Windows pour geeks du virtuel ».

---

## 2. Segment 1 — Pincab virtuel *(terrain actuel)*

**Ce que c'est** : un PC Windows qui émule des flippers (Visual Pinball X surtout, plus Future Pinball, Pinball FX, Zaccaria), piloté par un frontend (PinUP Popper, PinballX, PinballY), avec retour physique (DOF), backglass (B2S), écrans multiples, médias.

**Couche logicielle** : la plus riche de tout l'univers. Tables `.vpx`, ROMs VPinMAME, DLL 32/64-bit, `.directb2s`, PUP-Packs, VPMAlias, bases Popper, colorisation, altsound. C'est un empilement fragile où **une seule pièce manquante casse une table** — sans message clair.

**Problèmes réels** : ROM manquante/mal nommée, migration 32→64-bit incomplète, DLL bloquée par Windows, backglass absent ou mal nommé, table pas enregistrée dans le frontend, média manquant, versions périmées. *(Exactement ce que Pincab Toolbox détecte déjà.)*

**Ce qui existe déjà** : des outils qui **gèrent et installent** — VPin Studio (open-source, gestion tables/joueurs/compétitions), Baller Installer (installe tout l'écosystème Popper), ClrVpin (nettoyage de contenu). **Aucun ne diagnostique** une install existante et n'explique les pannes. C'est le trou où Pincab Toolbox s'installe.

**Angle produit FlipSync** :
- **Gratuit** : Pincab Toolbox (le scanner) — l'aimant qui crée la confiance et le réflexe « lance-le avant de bidouiller ».
- **Payant** : Repair (réparation automatique, sûre, réversible), + potentiellement sauvegarde/migration/tuning (déplacer une install sur un nouveau PC sans tout casser, c'est un cauchemar récurrent).

---

## 3. Segment 2 — Tables non-officielles, homebrew & téléchargements *(transversal au virtuel)*

**Ce que c'est** : l'écosystème vit du **téléchargement massif** de tables sur VPUniverse, VPForums, Pinball Nirvana — recréations de vraies machines, tables originales (homebrew), remixes. Culture du gratuit très forte.

**Le point technique sensible** : une table `.vpx` **contient du VBScript exécuté** au lancement. Télécharger une table = exécuter du code d'un inconnu. Windows attache une « Mark of the Web » qui bloque les DLL (d'où le check que tu as déjà). Deux risques croissants :
1. **Sécurité/confiance** : provenance incertaine, scripts opaques, DLL tierces. Personne ne vérifie ce qu'il lance.
2. **Obsolescence de `vbscript.dll`** : Microsoft a entamé la dépréciation de VBScript dans Windows — un sujet qui, à terme, menace tout l'écosystème VPX et créera une vague de « ça ne marche plus ». *(À surveiller de près : c'est un futur pic de douleur communautaire — donc une future opportunité.)*

**Problèmes réels** : « j'ai téléchargé une table, elle plante / n'a pas de son / DMD noir » ; fichiers mal nommés ; dépendances non installées ; doute sur la fiabilité d'une source.

**Ce qui existe déjà** : rien de sérieux côté **intégrité/provenance**. Les gens se fient à la réputation du posteur.

**Angle produit FlipSync** :
- **Gratuit** : un volet « intégrité » dans le scanner (déjà amorcé : DLL bloquées, script illisible) — « cette table est saine / voici ce qui cloche ».
- **Payant/plus tard** : vérification de provenance, base de « bons hashes » communautaire, préparation automatique d'une table fraîchement téléchargée (débloquer, placer, enregistrer). **Prudence licences** : ne jamais héberger/redistribuer des tables ou ROMs — rester sur l'outil qui *vérifie et prépare*, jamais qui *fournit*.

---

## 4. Segment 3 — Flippers physiques modernes

**Ce que c'est** : les machines neuves. Panorama 2024-2026 : **Stern** (leader, ~3 jeux/an), **Jersey Jack** (haut de gamme, écrans LCD, Bluetooth), **Chicago Gaming** (remakes officiels Bally/Williams + 1er jeu original en 2023), **Spooky** (boutique, horreur, petites séries), **Multimorphic** (plateforme P3 **modulaire** : plusieurs jeux sur une machine via modules), **Dutch Pinball**, **Pinball Brothers** (Alien, fiabilité difficile), **Barrels of Fun** (nouveau venu, 2023). *American Pinball : statut incertain (peut-être inactif). Haggis : fermé (2024). Homepin : réputation faible.*

**Couche logicielle** : le **code se met à jour** (surtout Stern, système Spike/Spike 2 : on télécharge une image, on la met sur **clé USB**, la machine flashe). Ça **peut échouer** (clé mal formatée, coupure, version incompatible) et il y a une vraie communauté « comment mettre à jour / downgrader mon code ». Réglages, logs d'erreurs, cartes « node » en réseau.

**Le mur** : une machine physique **n'est pas un PC Windows** qu'on scanne. Pas de système de fichiers accessible, pas d'agent à lancer dessus. Le modèle « scanner PC » **ne se transporte pas directement**. Ce qui se transporte, c'est **la connaissance** (symptôme → cause → correctif) et **les artefacts logiciels autour** (préparer la bonne clé USB de MAJ, expliquer un code d'erreur, guider un réglage).

**Angle produit FlipSync** :
- **Gratuit** : un **assistant de diagnostic guidé** (base de connaissance des pannes/codes d'erreur par modèle) — le même moteur « Knowledge Engine » que le pincab, mais alimenté par des symptômes décrits, pas par un scan.
- **Payant/plus tard** : suivi des versions de code (« ton jeu a une MAJ »), préparateur de clé USB de mise à jour, carnet d'entretien numérique. **À valider** : le public physique paie surtout pour du **matériel** et de la **réparation** — la volonté de payer pour du *logiciel pur* y est moins prouvée que côté pincab.

---

## 5. Segment 4 — Flippers physiques classiques & électromécaniques

**Ce que c'est** : les machines d'époque — Williams, Bally, Gottlieb, Data East, Sega, Stern (ancien). Deux familles : **à ROM** (systèmes à microprocesseur, années 80-90, cœur émulé par **PinMAME** — le même que le virtuel !) et **électromécaniques** (EM, relais, pas de code).

**Couche logicielle / matérielle** :
- **ROMs & cartes** : les cartes MPU/CPU tombent en panne ; on les **remplace** (Rottendog, Alltek — ~150-300 $) et/ou on **reflashe des ROMs** (patchs communautaires, versions « home rom »). PinMAME sert de référence de comportement.
- **La corrosion de pile** : la panne emblématique — une pile qui fuit **détruit la carte MPU**. Prévention + diagnostic = sujet brûlant et universel.
- Faisceaux, connecteurs, switches, bobines, flippers faibles, billes coincées : mécanique/électrique, hors logiciel.

**Ce qui existe déjà** : une **culture de réparation manuelle** riche mais dispersée — PinWiki, « 10 commandements de la réparation », forums, multimètre. Zéro assistant logiciel unifié.

**Angle produit FlipSync** :
- **Gratuit** : assistant de diagnostic guidé (arbre symptôme → cause → test → correctif), base de codes d'erreur/pannes par système (WPC, Data East…), rappels d'entretien (la pile !).
- **Payant/plus tard** : bibliothèque de procédures détaillées, base de ROMs de référence (versions, checksums — **vérifier**, pas héberger), carnet d'entretien par machine. **Réalisme** : ici FlipSync serait surtout un **produit de connaissance/guidage**, pas un scanner. Marché de passionnés-réparateurs, plus étroit mais fidèle.

---

## 6. Segment 5 — Le pont : colorisation, son, ROMs *(le vrai recouvrement)*

C'est le segment le plus stratégique parce qu'il est **commun au virtuel et au physique** :

- **Colorisation DMD** — **Pin2DMD** et **Serum** produisent des fichiers de colorisation qui marchent **sur pincab ET sur vrai flipper**. **ColorDMD** est l'équivalent commercial (matériel). Les fichiers vivent sur VPUniverse. Problèmes : quel fichier pour quelle ROM, installation, compatibilité, versions.
- **Son** — **PinSound** (carte son remplaçable sur vrai flipper + packs) et **altsound** (virtuel) : mêmes packs, mêmes logiques.
- **ROMs / PinMAME** — le **même cœur d'émulation** est utilisé côté pincab et pour comprendre/patcher les vraies machines à ROM.

**Angle produit FlipSync** : c'est ici qu'un outil peut **légitimement s'adresser aux deux publics** avec la même brique — « quelle colorisation/quel pack son pour ma machine (réelle ou virtuelle), est-il bien installé, est-il à jour ? ». Gratuit pour vérifier/diagnostiquer ; payant pour automatiser install/mise à jour/organisation. **C'est probablement le meilleur deuxième produit après Pincab Toolbox**, parce qu'il étend la marque au physique **sans** exiger de scanner une machine embarquée.

---

## 7. Matrice produits FlipSync (vue synthétique)

| Produit | Segment(s) | Gratuit / Payant | Modèle | Faisabilité | Réutilise |
|---|---|---|---|---|---|
| **Pincab Toolbox — Scanner** | 1, 2 | Gratuit (aimant) | Scan PC | ✅ Existe | — |
| **Repair (pincab)** | 1, 2 | Payant | Automatisation sûre + backup | 🟢 Prochaine étape | Findings + Knowledge |
| **Backup / Migration pincab** | 1 | Payant | Sauvegarde & déménagement d'install | 🟢 Fort besoin | Layout detector, diff |
| **Colorisation & Son (pont)** | 5 | Freemium | Vérif gratuite, gestion payante | 🟡 À cadrer | Lecteur fichiers, Knowledge |
| **Assistant diagnostic — flipper physique** | 3, 4 | Gratuit → Pro | Guidage symptôme→cause→fix | 🟡 Connaissance, pas scan | Knowledge Engine (moteur) |
| **Carnet d'entretien / suivi versions** | 3, 4 | Payant | Appli de suivi | 🟠 À valider (volonté de payer) | UI, données |
| **Vérif intégrité/provenance tables** | 2 | Freemium | Hash/réputation communautaire | 🟠 Sensible (licences) | Scanner, base communautaire |
| **Digital Twin + Health Timeline** | 1 | Gratuit → Pro | Modèle complet de l'install + comparaison de scans | 🟢 Extension du moteur | Knowledge, diff Myers |
| **Switch Matrix Solver** | 4 | Gratuit (aimant physique) | Calcul matriciel de la panne, sans matériel | 🟢 Autonome | Algo pur |
| **Parseur d'audits Stern** | 3 | Payant (exploitants) | Parse les logs USB → anomalies d'usure | 🟡 Faisable | Lecteur de fichiers |

Légende faisabilité : 🟢 solide · 🟡 crédible à cadrer · 🟠 incertain / à valider.

---

## 8. Ce qui se réutilise d'un produit à l'autre (le moat technique)

Le vrai actif de FlipSync n'est aucun produit isolé, c'est **la base réutilisable** :

1. **Le Knowledge Engine** (symptôme/finding → impact → cause → correctif, avec niveau de fiabilité). Il porte du pincab au flipper physique **tel quel** — seule la source d'entrée change (scan automatique vs symptôme décrit).
2. **Les lecteurs de fichiers zéro-dépendance** (`.vpx`/compound file, SQLite, PE/bitness, diff Myers). Réutilisables pour colorisation, son, intégrité.
3. **L'architecture par profils** (data-driven). Nouveau segment = nouvelles données, pas nouveau produit.
4. **La marque et la confiance** : « lance FlipSync avant de bidouiller », d'abord sur pincab, extensible ensuite.

---

## 9. Lecture stratégique (mon avis, à débattre)

- **Le pincab est le bon point de départ** et le doit rester un moment : c'est le seul segment où (a) le scan auto marche, (b) le trou concurrentiel est net, (c) la volonté de payer pour du logiciel est la mieux prouvée.
- **Le meilleur 2ᵉ pas est le "pont" (segment 5)**, pas le flipper physique complet : colorisation/son touchent les deux mondes avec la brique la plus proche de l'existant, et posent la marque sur le physique **sans** buter sur « on ne scanne pas une machine embarquée ».
- **Le flipper physique pur (3, 4) est un produit de connaissance/guidage**, pas un scanner. Séduisant pour la marque et la communauté, mais modèle économique à valider : ce public paie surtout matériel + réparation. À traiter comme **extension de notoriété** avant extension de revenu.
- **Deux gardes-fous non négociables** : (1) **licences** — on vérifie/prépare/organise, on n'héberge **jamais** tables ni ROMs ; (2) **lecture seule par défaut** partout — la confiance est le seul actif qui ne se rachète pas.
- **Signal à surveiller** : la **dépréciation de VBScript** par Windows. Si elle mord, elle déclenchera une vague « mon pincab ne marche plus » — FlipSync doit être l'outil qui *explique et guide* le premier.

---

## 10. Apport des revues IA — synthèse filtrée (ce que je garde / ce que j'écarte)

Tu as fait passer 4 IA sur « les frictions du flipper physique transposables au numérique ». Beaucoup se recoupe ou relève du *chatbot*, pas du produit. Voici mon tri.

### A. Pour le pincab — renforce Toolbox / Repair

1. **Le cadrage en « Doctors » modulaires** *(idée GPT)* — au lieu d'« un scanner », présenter des modules nommés : **ROM Doctor, Bitness Doctor, Display Doctor, Audio Doctor, Plugin Doctor, Dependency Doctor, Script Doctor, Migration Doctor, Folder Doctor, Duplicate Doctor, Security Doctor**. Fort en marketing (chaque « Doctor » est une raison de parler du produit) et ça structure la roadmap. On a déjà 7 des briques — ça leur donne un nom vendeur.
2. **Digital Twin du pincab** *(idée GPT, la meilleure)* — après un scan, l'outil connaît toute la config logicielle (VPX, ROMs, DOF, DMD, plugins, écrans, dossiers, versions, dépendances) et **raisonne sur l'ensemble**, pas fichier par fichier. C'est exactement la direction Knowledge Engine + Scenarios : notre « diagnostic principal » est déjà un embryon de Digital Twin. À **assumer comme concept produit** — c'est ce qui, demain, permet à Repair de dire « migration 32→64 incomplète » au lieu de lister 12 erreurs.
3. **Health Timeline** — comparer deux scans (« depuis hier : 3 nouveaux problèmes »). Simple, très utile, réutilise le diff Myers qu'on a déjà. Bon candidat **gratuit** qui crée l'habitude de relancer l'outil.
4. **Nouveaux checks concrets, tous lisibles en statique** (notre point fort) :
   - *Audio Doctor* : périphérique audio invalide/désactivé, volume Windows à zéro, DLL audio absentes.
   - *Display Doctor* : ordre/résolution/DPI, écran débranché, coordonnées d'écran impossibles.
   - *Folder Doctor* : chemins trop longs, caractères interdits, doublons de dossiers.
   - *Script Doctor* : API VBScript obsolètes, références inexistantes — **sans lancer la table**.
   - *(via Kimi)* au niveau du script `.vpx` : force de flipper hors plage, `elasticity` anormale, `Balls` ≠ 3, `PlaySound()` pointant vers un fichier absent. À **prioriser par fréquence réelle**, pas tout faire.

### B. Pour le physique — les vrais produits logiciels (sans accès matériel)

Bonne surprise : il existe des produits **purement logiciels** côté physique, qui n'exigent pas de brancher la machine. Les sérieux (surtout idées Gemini) :

1. **Switch Matrix Solver** — l'utilisateur entre les 2-3 contacts qui se déclenchent seuls ; l'algo croise la matrice 8×8 et pointe la **diode / le fil fautif**. Faisable, précis, très demandé côté rétro. Le meilleur « petit produit » physique pour poser la marque.
2. **Parseur d'audits** — les Stern récents crachent des logs d'usage sur clé USB ; un parseur qui flag les anomalies (« bobine gauche 20 000 activations vs droite 5 000 → problème de géométrie », « switch activé 0 fois sur 500 parties → panne silencieuse »). Cible exploitants/collectionneurs.
3. **Recherche sur manuels (RAG / OCR)** — indexer les manuels d'époque : taper « J120 pin 3 » ressort la couleur du fil et sa destination. Réutilise la logique Knowledge Engine.
4. **Validateur d'intégrité carte SD / firmware** — lire la carte SD d'un flipper moderne, comparer les checksums aux manifestes constructeurs. Prolonge directement notre check d'intégrité de fichiers.

Logique commune (bien dite par GPT) : **symptôme → cause probable → procédure**, jamais de la reproduction de diagnostic matériel. C'est ce qui fait un *moteur de diagnostic*, pas un simple vérificateur.

### C. Ce que j'écarte (pour rester net)

- Tout ce qui exige d'**accéder au matériel** (mesurer des tensions, tester des puces à l'oscilloscope) — hors périmètre logiciel, et ça casse la promesse « zéro risque ».
- Les usages **« pose la question à une IA »** (identifier une puce par photo, traduire un manuel, glossaire) : utiles, mais ce sont des features de chatbot, **pas des produits FlipSync défendables** (aucun moat).
- Le **parsing de mesh 3D** pour la pente des rampes *(Kimi)* : coûteux, fragile, faible valeur.
- Les **valeurs de réglage matériel précises** balancées par les IA (puissance bobine « 15 », condensateur « C12 »…) : souvent génériques voire inventées — **à ne jamais reprendre sans source constructeur.** C'est un piège à crédibilité.

---

## 11. Décisions — déplacées

Les décisions prises le 22/07/2026 à partir de ce document ont été **formalisées et, pour deux d'entre elles, révisées** le 25/07 :

| Sujet d'origine | Où il vit maintenant |
|---|---|
| Périmètre de la marque (rester sur le flipper) | `PROJECT-BRAIN.md` §3 |
| Ordre des produits (pincab → pont → physique) | `PROJECT-BRAIN.md` §3 + ADR-001 |
| Physique : classique/EM d'abord, beachhead Switch Matrix Solver | ADR-001 — hors carte jusqu'au premier euro |
| Modèle de revenu (récurrent) | **ADR-002** — mécanique et prix figés |
| Où placer le pont colorisation/son | ADR-001 — c'est la ligne **Table Companion** |

**Ne pas rediscuter ces points ici.** Toute évolution passe par un nouvel ADR.

---

### Sources principales
- Fabricants actuels : [Kineticist — Who Makes Pinball Machines](https://www.kineticist.com/news/who-makes-pinball-machines), [Wikipedia — List of pinball manufacturers](https://en.wikipedia.org/wiki/List_of_pinball_manufacturers)
- Mise à jour code Stern Spike 2 : [Flippers.be — Spike2 upgrade](https://www.flippers.be/basics/101_spike2_upgrade.html), [PinWiki — Stern SPIKE System Repair](https://pinwiki.com/wiki/index.php/Stern_SPIKE%E2%84%A2_System_Repair)
- Cartes de remplacement classiques : [Rottendog Amusements](https://rottendog.us/), [Alltek Systems](https://allteksystems.com/)
- Colorisation & son : [Pin2DMD.com](https://pin2dmd.com/), [VPUniverse — Serum/Pin2DMD colorizations](https://vpuniverse.com/files/category/173-serum-dmd-colorizations/), [Pavlov Pinball — PinSound review](https://pavlovpinball.com/pinsound-review-a-sound-upgrade-that-transforms-your-pin/)
- Outils virtuels existants : [VPin Studio (GitHub)](https://github.com/syd711/vpin-studio), [Baller Installer (nailbuster wiki)](https://www.nailbuster.com/wikipinup/doku.php?id=ballerinstallerv2501)
- Réparation/entretien : [PinballHelp — 10 Commandments of Pinball Repair](http://pinballhelp.com/the-10-commandments-of-pinball-repair-and-maintenance/)
