# Ce qui a changé depuis v0.1.1-alpha (30/07/2026)

**Base de départ retenue : `v0.1.1-alpha`**, la dernière release GitHub publiée au moment où le
prompt de cette session a été écrit. 102 commits séparent cette release de l'état actuel (`64801e9`,
19/08/2026 soir).

**✅ Confirmé par toi (19/08) : `v0.1.2-alpha` (commit `763c2af`, 07/08/2026) est bien la release
GitHub "Latest" réelle**, pas `v0.1.1-alpha` comme le prompt de session le supposait. Je l'avais
repéré en testant le lien de téléchargement de la landing (la redirection GitHub pointait vers
l'asset de `v0.1.2-alpha`) mais je ne pouvais pas le confirmer par une deuxième méthode depuis ce
sandbox, donc je l'avais signalé au lieu de trancher. Ça ne change rien au contenu ci-dessous, qui
couvre tout depuis `v0.1.1-alpha` et inclut donc déjà tout ce qu'apporte `v0.1.2-alpha` — ça précise
seulement quelle release GitHub est réellement "la dernière publiée" au sens strict.

---

## Pour les testeurs (sans jargon technique)

Voici, en clair, ce qui a changé dans l'outil depuis la dernière fois que tu l'as ouvert.

**L'interface a changé de couleur et d'agencement.** Le orange est devenu vert (nouvelle identité
visuelle), le panneau de détail est maintenant sur le côté droit et peut se replier, les tableaux sont
plus lisibles (survol, en-têtes, densité revus), et il y a un nouvel onglet Tutoriel qui explique ce
que fait vraiment le Scanner. Le français et l'espagnol vouvoient maintenant ("vous" / "usted") au
lieu de tutoyer. L'espagnol est une langue entièrement nouvelle : l'app est maintenant disponible en
anglais, français et espagnol (l'anglais est la langue par défaut au premier lancement, plus l'ancien
comportement qui suivait la langue de Windows).

**Le Scanner détecte plus de choses.** Une douzaine de nouveaux contrôles sont apparus par vagues
successives début août puis le 18 août (fichiers de config B2S manquants, trois nouveaux contrôles
liés à des soucis remontés par des testeurs, scan qui couvre maintenant plusieurs disques ou un disque
entier au lieu d'un seul dossier). Il y a aussi un nouveau bouton "Check for updates" (manuel, tu dois
cliquer dessus, rien n'est vérifié automatiquement) qui compare ta version installée à la dernière
version publiée sur GitHub.

**Repair (le module payant, toujours en accès fermé, pas encore en vente) est plus clair.** Quand une
réparation échoue, tu vois maintenant pourquoi au lieu d'un simple échec silencieux. Les éléments
qu'on ne peut pas réparer automatiquement (marqués "à faire à la main" ou verrouillés) sont enfin
visibles au lieu d'être cachés. Après une réparation, un bouton "Revoir mon score" relance le même
scan pour te montrer l'avant/après. Il y a une case "tout sélectionner" pour cocher tous les
correctifs d'un coup. La réparation d'un composant COM ne demande plus les droits administrateur dans
la majorité des cas (seulement quand c'est vraiment nécessaire, au cas par cas, plus jamais pour toute
l'appli).

**Un export PDF du rapport** existe maintenant (généré par l'outil lui-même, sans dépendance externe).

**L'écran de démarrage (logo au lancement)** a été retiré à un moment (le 18 août) parce qu'il
déclenchait un blocage dur du Contrôle intelligent des applications de Windows, puis remis le 19 août
dans une version différente et plus prudente (sans transparence, sans appel bas niveau à Windows) pour
éviter de reproduire le problème.

**Rien de tout ça ne change le prix ou la manière d'acheter** : Repair reste en accès fermé, non en
vente. Les conditions d'utilisation (CGU/CGV) ont été mises à jour le 19 août pour refléter la
décision sur le prix (3,99, sans distinction de devise, achat unique, licence à vie, mises à jour
incluses sans limite de durée) — voir `docs/AUDIT-landing-2026-08-19.md` pour un point sur la
cohérence de ces textes avec la landing.

---

## Pour toi (technique)

### ADR décidés depuis v0.1.1-alpha

- **ADR-010** (06/08) — dégel de la doctrine `Severity.Note` : un niveau de sévérité entre Info et
  Warning, neutre pour le score, qui constate un fait sans jamais porter de verdict.
- **ADR-011** (11/08) — scan multi-racines / disque entier, au lieu d'un seul dossier d'installation.
- **ADR-012** (11-13/08) — architecture du chemin d'écriture de Repair (formalise les lots communauté
  A-H et I), avec un addendum "Suite — 11/08" qui documente la fin du filet de sécurité no-op : la
  vraie clé de licence est en production, Repair peut réellement écrire sur le disque d'un testeur
  depuis ce moment-là.
- **ADR-013** (19/08) — prix unique 3,99 (EUR/USD/GBP), achat unique, licence perpétuelle, mises à
  jour incluses sans limite de durée, encaissement Stripe en direct (MC Automation reste vendeur légal,
  pas de Merchant of Record). **Supersede ADR-009 en entier** et la partie prix/durée d'ADR-002.
- **ADR-009** (27/07, dans le lot précédent) — Lemon Squeezy comme Merchant of Record — **abandonné**
  le 19/08, remplacé par la décision ADR-013 ci-dessus. Conséquence assumée : MC Automation redevient
  responsable de la TVA du pays de l'acheteur au-delà du seuil UE de 10 000 €, ce que le MoR aurait
  pris en charge. Point à cadrer avec un comptable avant toute vente publique (voir "points ouverts"
  plus bas).

### Ce qui a été retiré ou abandonné

- **Écran de démarrage v1** (`be0a1ce`, 18/08) : retiré parce qu'il déclenchait un blocage dur du
  Contrôle intelligent des applications de Windows. Remplacé le 19/08 (`64801e9`) par une version
  reconstruite délibérément sans `AllowsTransparency` ni P/Invoke, pour ne pas reproduire la même
  cause.
- **Lemon Squeezy / Merchant of Record** (ADR-009) : abandonné au profit de Stripe en direct
  (ADR-013). Voir ci-dessus pour la conséquence TVA.

### Coté mais volontairement désactivé (à connaître, pas forcément à activer)

- `RegisterComComponentAction` est enregistré dans `RepairActionRegistry` mais **aucune règle du pack
  de connaissance** (`knowledge/pack-2026.08.json`) ne le référence — il est donc inerte en pratique,
  choix assumé dans ADR-012.
- `SetDefaultAudioDeviceAction` n'est même pas construit dans `RepairSession` — capacité non câblée du
  tout, pas seulement inactive par choix de règle.

### Explicitement pas visible pour un utilisateur (interne, ne pas mettre dans une annonce publique)

Une bonne partie des 102 commits est invisible pour un testeur : refactor de `Scenarios.cs` /
`RowPlanning.cs` vers `PincabToolbox.Core.Diagnostics` (points 3 à 6/6 du 13/08), correctifs de build
CI (`build-windows` fallback nuget, `.gitattributes`, qualification `typeof(Button)`), housekeeping de
merges/bundles (sync du sandbox, bundles `refonte-ui` / `gitattributes-fix` / `cleanup-docs`), et une
dizaine de commits `docs:` qui ne sont que des entrées de `FIELD-LOG.md` ou des réponses à des
testeurs (Gregg, Joey Mahon) consignées pour mémoire. La suite de tests est passée de 501 (état
antérieur à `v0.1.1-alpha`, mentionné pour référence) à **540/540** sur Core et **163/163** sur Repair
aujourd'hui — un testeur ne voit jamais ce chiffre, ce n'est pas un argument produit.

### Baseline de tests — vérifiée dans cette session (19/08)

- `dotnet run --project tests/PincabToolbox.Core.Tests -c Release` → **540 passed, 0 failed, 540
  total** (après génération des fixtures via `tests/fixtures/make_fixtures.py`, absentes par défaut
  dans un clone neuf — sans elles, 24 tests échouent avec `DirectoryNotFoundException: fixtures/out
  not found`, ce n'est pas une régression).
- `dotnet run --project tests/PincabToolbox.Repair.Tests -c Release` → **163 passed, 0 failed, 163
  total**.
- Ces deux résultats confirment que les deux corrections apportées à `PincabToolbox.LicenseTool` dans
  cette session (voir la livraison Mission 3) n'ont rien cassé — ce projet n'est référencé par aucune
  des deux suites de tests, le risque était de toute façon nul, mais la baseline est vérifiée et
  propre.

### Points déjà ouverts, non traités par cette session (pour information)

- Médiateur de la consommation : toujours pas souscrit, ~10-40€/an, bloquant avant la première vente
  réelle, pas avant des tests.
- TVA au-delà du seuil UE de 10 000€ : à cadrer avec un comptable maintenant que le MoR est abandonné
  (ADR-013).
- Soumission Microsoft Security Intelligence (gratuite) : identifiée le 18/08, jamais faite, seul
  levier gratuit connu contre le blocage "Smart App Control" — pertinent maintenant que des testeurs
  vont télécharger l'exe.
- Fichiers résiduels chez toi (`cleanup-docs.bundle`, `gitattributes-fix.bundle`, `test-sac.cmd`) :
  supprimables, sans lien avec le code.
