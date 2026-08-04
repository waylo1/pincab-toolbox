# Post de lancement — Pincab Passion (FR) — v2 (27/07/2026)

> **Lancement en bêta ouverte, sans validation préalable sur cab réel (Maxime en déplacement) — le post est cadré en conséquence : appel à testeurs, pas résultat validé.** Sections à personnaliser entre [crochets].
> Forum cible : pincabpassion.net, section « Logiciels / Utilitaires » (vérifier la bonne section ET les règles d'annonce du forum avant de poster).
>
> *Repris le 27/07 (soir) : le teaser de fin promettait des fonctions qui ne sont pas dans Repair v1 (gardien de focus, réglage écrans, renommage médias = lignes Play Optimizer / Table Companion, parquées). Corrigé pour ne promettre que le tenable. Durée de scan alignée sur le réel. Ajout d'un appel explicite à bêta-testeurs (pas encore de scan validé sur cab réel) + capture issue du mode démo, pas d'un scan réel. Le reste du post était déjà bon.*

---

**Titre du sujet :** [OUTIL GRATUIT] Pincab Toolbox — scanne ton install VPX/Popper et trouve ce qui est cassé (ROMs manquantes, conflits 64-bit, backglass…)

---

Salut à tous,

Comme beaucoup ici, j'ai passé plus de soirées à **réparer** mon pincab qu'à jouer avec. Une table qui plante avec une erreur VPinMAME cryptique, un DMD qui meurt après le passage au 64-bit, un backglass qui ne s'affiche plus… et à chaque fois, une heure de fouille dans les dossiers pour trouver la cause.

Alors j'ai développé l'outil que j'aurais voulu avoir : **Pincab Toolbox**, un scanner **gratuit** qui diagnostique ton installation en quelques secondes (un peu plus sur une grosse collection).

**Petite précision honnête :** je suis en déplacement cette semaine et je n'ai pas encore pu le faire tourner sur un cab réel — 117 tests automatisés passent au vert, mais rien ne remplace un vrai scan sur une vraie collection bien crade. Je le sors quand même maintenant en **bêta ouverte** : si vous le lancez chez vous, dites-moi ce qu'il rate ou ce qu'il raconte n'importe quoi. C'est exactement ce que je cherche avec ce post.

**Ce qu'il vérifie :**

- **ROMs manquantes** — il lit le script de chaque table (`cGameName`), gère les alias `VPMAlias.txt`, et te dit exactement quel `.zip` manque dans `VPinMAME\roms`. Les tables originales/EM sont reconnues, pour éviter les faux positifs.
- **ROMs décompressées** — une ROM dézippée en dossier ne sera pas chargée par VPinMAME ; il te le signale.
- **Conflits 32/64-bit** — LE piège de la transition VPX 10.8 : il inventorie le bitness de chaque exe/DLL (VPinMAME, dmddevice, B2S, FlexDMD) et signale les installs hybrides bancales.
- **DLL bloquées par Windows** — un fichier extrait d'un ZIP téléchargé peut être mis en quarantaine (« Mark of the Web ») et ne pas se charger ; il le repère.
- **Dépendances manquantes** — B2S Backglass Server ou FlexDMD requis par tes tables mais absents.
- **Backglass et Popper** — `.directb2s` manquants, tables absentes de la base PinUP Popper, PUP-Packs présents.
- **Compatibilité** — signatures nFozzy/Roth et versions VPX minimales déclarées dans les scripts.
- **Tables périmées** *(bêta)* — comparaison avec la base open source du Virtual Pinball Spreadsheet, avec **lien vers la page officielle uniquement**.
- **Diff de scripts** — compare deux versions d'une table côte à côte avant d'écraser la tienne.

**Les règles que je me suis fixées** (et je sais qu'ici on y est attachés, à juste titre) :

- **100 % local** : rien n'est envoyé nulle part, zéro télémétrie, zéro compte.
- **Lecture seule** : le scanner ne modifie JAMAIS un fichier, une clé registre ou une base.
- **Zéro téléchargement de contenu** : ni tables, ni ROMs, ni médias — jamais. Ce n'est pas un outil de téléchargement, c'est un outil de diagnostic. Les sites de la communauté restent LA source.

**Capture d'écran (mode démo intégré — pas encore de scan réel disponible, cf. plus haut) :** voir `marketing/screenshot-scanner-demo-FR.png`

**Télécharger :** https://github.com/waylo1/pincab-toolbox/releases/latest/download/PincabToolbox.zip
(Dézippe le dossier et lance **PincabToolbox.exe** depuis l'intérieur — garde les fichiers ensemble.)
(Exe non signé pour l'instant → alerte SmartScreen normale : « Informations complémentaires » puis « Exécuter quand même ». Le code sera signé dès la prochaine version.)

Il y a aussi un **mode démo** intégré si tu veux voir ce que ça donne sans le lancer sur ta vraie config.

C'est une v0.1 en bêta ouverte : je cherche des retours francs — faux positifs, tables exotiques, messages pas clairs. Je corrige vite, y compris ce soir/demain si besoin. Et si l'outil vous plaît, une version qui **répare** une partie de ce que le scanner trouve est en préparation — toujours avec sauvegarde avant, aperçu de ce qui va changer, et annulation possible. Le scanner, lui, restera **gratuit et en lecture seule, pour toujours.**

**Et si ça vous intéresse, dites-le simplement en commentaire** (« intéressé par la version qui répare »). Je ne demande ni mail ni inscription — c'est juste pour savoir combien de personnes ça concerne vraiment avant d'y passer des semaines, et je saurai qui prévenir le jour où ça sort.

Merci, et bons flips !

[Ton pseudo]

---

*Notes perso (ne pas publier) :*
- *Répondre à CHAQUE commentaire les 48 premières heures — encore plus important que d'habitude vu qu'aucun scan réel n'a été fait avant publication.*
- *Surveiller en priorité absolue les 1ers retours de faux positifs critiques : premier signalé → corriger et remercier publiquement avant tout autre sujet (crédibilité).*
- *Si un modérateur tique sur quoi que ce soit → répondre en privé, proposer d'adapter. La relation avec le forum vaut plus que le post.*
- *Métriques à noter : téléchargements J+7 / J+30, nb de retours, demandes de features.*
- *Le lien de téléchargement doit mener à un vrai téléchargement, pas à la page « bêta » d'inscription email (voir la revue de la landing).*
- *Compter chaque commentaire « intéressé » → c'est le KPI #10 (go/no-go sur le codage de l'UI Repair). Consigner chacun.*
- *Chaque retour posté → à consigner dans le field-log (voir PROCESS-capture-retours.md).*
