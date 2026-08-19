# Prompt pour la prochaine session Cowork — landing, récap de release, clés testeurs (19/08/2026)

> À copier-coller tel quel dans une nouvelle session Cowork.
> Structure K.E.R.N.E.L : **K**adrage, **E**nvironnement, **R**éférences, **N**on-négociables,
> **É**tapes, **L**ivrables.
>
> **Recommandation de modèle/effort : effort ÉLEVÉ.** Aucune des trois missions n'est du code
> mécanique : la première est un audit de cohérence entre deux artefacts qui ont dérivé l'un de
> l'autre, la deuxième demande de trier 95 commits en distinguant ce qui compte pour un utilisateur
> de ce qui ne compte que pour un développeur, la troisième touche à la clé privée de licence. Un
> effort bas produira ici un résultat qui a l'air fini et qui a inventé.

---

## K — KADRAGE (l'objectif, en une phrase)

Tu reprends **Pincab Toolbox / FlipSync** (MC Automation, Maxime Chauvin, micro-entreprise solo, dev
indé français). **Trois missions indépendantes**, à livrer dans cet ordre, aucune ne devant bloquer
les suivantes :

1. **AUDIT LANDING** — vérifier que la nouvelle landing est cohérente avec ce que le logiciel fait
   réellement aujourd'hui, et prête à être vue par des testeurs.
2. **RÉCAP DE RELEASE** — un résumé de toutes les nouveautés depuis la dernière release publique.
3. **3 CLÉS DE LICENCE TESTEURS** — produire les commandes exactes pour que Maxime génère lui-même
   trois clés Repair pour ses testeurs.

## E — ENVIRONNEMENT

- **Dépôt** : `/home/claude/pincab-suite` (clone si absent : `https://github.com/waylo1/pincab-toolbox`), branche `main`.
- **Le poste de Maxime** (pont `mcp__remote-devices__*`, actif seulement si son app desktop est
  ouverte) : dépôt réel dans `C:\Users\User\Desktop\Pincab suite\pincab-toolbox-v0.1.1-alpha-src\pincab-suite`,
  landing dans `C:\Users\User\Desktop\Pincab suite\flipsync-site\landing\`, documents juridiques dans
  `C:\Users\User\Desktop\Pincab suite\flipsync-site\legal\`. **Le dossier `flipsync-site` n'est PAS
  dans le dépôt git** — il ne se voit que par le pont.
- **`git push` est bloqué depuis le sandbox** (403 du proxy, `waylo1/pincab-toolbox` hors du set
  autorisé). N'essaie pas d'insister : livre les fichiers par `SendUserFile` +
  `mcp__remote-devices__device_commit_files`, et donne à Maxime les commandes git à passer lui-même.
- **`PincabToolbox.App` NE COMPILE PAS dans le sandbox Linux** (`net8.0-windows`, NU1100 sur
  `Microsoft.WindowsDesktop.App.Ref`). Fait connu, pas une régression. Vérifie tes changements App
  par XML bien formé + passe de syntaxe Roslyn, jamais par compilation réelle.
- **Les tests, eux, tournent ici.** Baseline au 19/08/2026, à ne jamais dégrader :
  ```bash
  dotnet run --project tests/PincabToolbox.Core.Tests -c Release     # 540/540
  dotnet run --project tests/PincabToolbox.Repair.Tests -c Release   # 163/163
  ```

## R — RÉFÉRENCES (à lire AVANT de produire quoi que ce soit)

Ce projet a déjà payé deux fois le prix de sessions qui ont écrit avant de lire. **Lis ces fichiers
en premier, dans cet ordre :**

1. **`TRANSMISSION.md`** — le journal de reprise. En particulier la section « ⚠️ À LIRE AVANT DE
   RE-DIAGNOSTIQUER UN BLOCAGE « Contrôle intelligent des applications » », et la MAJ 19/08 (soir).
2. **`docs/adr/`** — les décisions structurantes. Ne rédige **jamais** une phrase sur le prix, le
   paiement, le périmètre légal ou le modèle de licence sans avoir lu l'ADR correspondant. Les plus
   récents priment sur les plus anciens, et les en-têtes indiquent ce qui est superseded :
   - **`ADR-013`** (19/08) — prix unique **3,99**, même nombre en EUR/USD/GBP, achat unique, licence
     perpétuelle, mises à jour incluses **sans limite de durée**, **aucun renouvellement**,
     encaissement **Stripe en direct**. Supersede `ADR-002` (prix) et `ADR-009` (en entier).
   - **`ADR-004`** — périmètre légal, les cinq règles inviolables.
   - **`ADR-002`** — reste en vigueur pour tout SAUF le prix et la durée de licence.
3. **`docs/legal/CGU-CGV-mentions-legales.md`** — l'état juridique de référence, à jour au 19/08.
4. **`knowledge/FIELD-LOG.md`** — les retours terrain réels.

## N — NON-NÉGOCIABLES

- 🚫 **Ne propose JAMAIS l'achat d'un certificat de signature de code**, sous aucune forme (OV, EV,
  Azure Trusted Signing, « juste pour la réputation SmartScreen »). Écarté le 18/08, re-confirmé
  explicitement le 19/08 : « ça n'arrivera jamais, il faut toujours que tu trouves une solution ».
  C'est une décision de fond, pas un arbitrage à re-tenter. Toute réponse à un blocage doit chercher
  une solution à coût nul.
- 🚫 **Ne construis pas le tunnel d'achat.** Décision du 19/08 : il attend une demande explicite de
  Maxime. Aucune page de paiement, aucun webhook, aucune intégration Stripe.
- 🚫 **Aucune communication publique, aucun contenu marketing à publier.** « la communication pour
  l'instant on attend je te dirai ». Tu peux auditer la landing, tu ne publies rien.
- 🚫 **Ne touche jamais à `license-private-key.pem`**, ne le lis pas, ne le copie pas, ne le fais pas
  transiter par le sandbox. Il vit sur la machine de Maxime et nulle part ailleurs.
- ⚠️ **N'invente aucun fait.** Si une info n'est pas dans le dépôt ou vérifiable, dis « je ne sais
  pas » plutôt que de produire une phrase plausible. Le projet a déjà perdu de la crédibilité
  publique une fois sur un faux positif (incident du 30/07).
- ✍️ **Style d'écriture pour tout texte destiné à Maxime ou au public** : pas de tirets en
  séparateur, des virgules ; pas de « ai slop » ; Maxime parle **toujours en solo** dans ses posts
  publics, jamais en « nous ».

## É — ÉTAPES

### Mission 1 — Audit de cohérence de la landing

**Objectif** : est-ce que la landing promet exactement ce que le logiciel fait, ni plus ni moins, et
est-elle prête à être montrée à des testeurs ?

1. Récupère la landing réelle via le pont (`device_list_dir` puis `device_stage_files` sur
   `C:\Users\User\Desktop\Pincab suite\flipsync-site\landing\`). Elle n'est pas dans git, tu ne peux
   pas la deviner depuis le dépôt.
2. Établis la **liste réelle** de ce que le logiciel fait aujourd'hui, depuis le code et non depuis
   la landing : les détecteurs du Scanner (`src/PincabToolbox.Core/`), les actions de réparation
   enregistrées ET réellement câblées à une règle de `knowledge/pack-2026.08.json` (une action
   enregistrée sans règle correspondante est **inerte** : elle ne produira jamais rien pour un
   utilisateur, ne la compte pas comme une fonctionnalité livrée), les onglets de l'UI
   (`MainWindow.xaml`), les langues effectivement traduites (`Localization/Loc.cs`).
3. Compare, et produis un tableau **promesse landing → réalité code → verdict**. Trois verdicts
   possibles : conforme, survendu (la landing promet plus que le code ne fait), sous-vendu (le code
   fait plus que la landing ne dit).
4. Vérifie séparément : le prix affiché (doit être **3,99**, `ADR-013`), les liens (CGU, contact,
   téléchargement), la présence des mentions légales obligatoires, et la cohérence avec `cgu.html`
   qui est dans le même dossier.
5. **Le survendu est le seul défaut bloquant.** Signale-le en premier, avec la formulation de
   remplacement exacte à mettre à la place.

### Mission 2 — Récap des nouveautés depuis la dernière release

- **La dernière release publique GitHub est `v0.1.1-alpha` (30/07/2026).** Un tag local
  `v0.1.2-alpha` (07/08) existe mais n'a pas été publié — vérifie l'état réel des releases GitHub
  avant de trancher, et dis clairement laquelle tu as prise comme point de départ.
- Il y a environ **95 commits** depuis `v0.1.1-alpha`. Ne les recopie pas : **trie**.
- Produis **deux niveaux de lecture** dans un même fichier :
  - une section **« ce que ça change pour toi »**, en français simple, sans jargon, lisible par un
    testeur qui ne code pas. C'est celle qui compte ;
  - une section **technique**, pour Maxime, avec les décisions structurantes (les ADR pris depuis),
    ce qui a été retiré ou abandonné, et les points restés ouverts.
- **Sépare explicitement** ce qui est visible par un utilisateur de ce qui est interne (refactos,
  tests, docs). Un utilisateur ne verra jamais un test qui passe de 501 à 540.
- Signale ce qui est **codé mais volontairement éteint** (par exemple les règles du pack non
  activées) : c'est présent dans le code mais ce n'est pas une nouveauté livrée.

### Mission 3 — Trois clés de licence pour les testeurs

- L'outil est `tools/PincabToolbox.LicenseTool` (voir son `README.md`). Il tourne **hors ligne, sur
  la machine de Maxime uniquement**, parce que c'est le seul endroit qui touche la clé privée.
- **Tu ne peux pas et ne dois pas générer les clés toi-même.** Produis les **commandes PowerShell
  exactes** que Maxime lancera depuis son dépôt, une par testeur, avec les emails en placeholders
  clairs qu'il n'aura qu'à remplacer.
- ⚠️ **Incohérence à traiter avant de donner les commandes** : le paramètre `--updates-months` vaut
  12 par défaut, hérité d'`ADR-002`. Or `ADR-013` supprime toute limite de durée sur les mises à
  jour. Deux options à présenter à Maxime, sans trancher à sa place : passer une valeur très large
  (`--updates-months 1200`) pour ces trois clés, ou corriger le défaut de l'outil. Dans les deux cas,
  explique que la licence elle-même n'a jamais expiré, seule la fenêtre de mise à jour était bornée.
- ⚠️ **Deuxième incohérence** : le message affiché par `issue` dit « à envoyer au client, ex. via
  Lemon Squeezy ». C'est faux depuis `ADR-013` (Stripe). Corrige cette chaîne, et le `README.md` de
  l'outil, dans le même lot.
- **Rappelle à Maxime, une seule fois et sans insister**, que `license-private-key.pem` n'a
  toujours pas de sauvegarde. S'il le perd, les clés déjà émises continuent de fonctionner mais il
  ne pourra plus jamais en signer de nouvelles avec cette identité.

## L — LIVRABLES

1. **`docs/AUDIT-landing-2026-08-19.md`** — le tableau promesse → réalité → verdict, le survendu en
   tête, chaque correction proposée avec sa formulation de remplacement exacte. Ne modifie **pas** la
   landing toi-même dans cette session : propose, Maxime tranche.
2. **`docs/RELEASE-NOTES-depuis-v0.1.1-alpha.md`** — les deux niveaux de lecture décrits plus haut.
3. **Les trois commandes de génération de clés**, dans le message de session (pas seulement dans un
   fichier), plus les deux corrections de l'outil (`Program.cs` et `README.md`) commitées.
4. **`TRANSMISSION.md` mis à jour** — une section datée pour cette session, dans le même style que
   les précédentes : ce qui a été fait, ce qui a été trouvé, ce qui reste ouvert.
5. **Livraison** : commits séparés et annulables un par un, fichiers écrits sur la machine de Maxime
   via le pont, et les commandes git exactes à passer de son côté (le push depuis le sandbox est
   bloqué).
6. **Revue CTO + Produit de clôture**, obligatoire avant de dire que c'est fini : le code est-il
   propre, l'architecture reste-t-elle cohérente, les tests suffisants, la fonctionnalité apporte-t-elle
   une vraie valeur utilisateur, y a-t-il un risque technique ou commercial, vois-tu une amélioration
   à faible coût qui mérite d'être proposée sans la coder ?

---

### Points déjà ouverts que cette session doit connaître sans forcément les traiter

- **Médiateur de la consommation** : obligation légale pour vendre à des consommateurs en France,
  toujours pas souscrit. Coût de l'ordre de 10 à 40 €/an. Bloquant avant la première vente, pas
  avant les tests.
- **Case de renoncement au droit de rétractation** : obligatoire dans le tunnel d'achat, qui n'existe
  pas encore et ne doit pas être construit.
- **TVA au-delà du seuil UE de 10 000 €** : depuis l'abandon du Merchant of Record (`ADR-013`),
  Maxime redevient responsable de la TVA du pays de l'acheteur au-delà de ce seuil, qui s'applique
  même en franchise en base. À cadrer avec un comptable avant la vente publique.
- **Portail Microsoft Security Intelligence** : soumission développeur gratuite, identifiée le 18/08
  et jamais mise en œuvre, pour faire rendre le verdict de réputation Windows **avant** de diffuser
  un binaire aux testeurs. C'est la seule piste à coût nul contre les blocages « Contrôle intelligent
  des applications ». Pertinent maintenant que des testeurs vont télécharger l'exe.
- **Fichiers résiduels** dans le dossier de Maxime : `cleanup-docs.bundle`, `gitattributes-fix.bundle`,
  `test-sac.cmd`, tous supprimables.
