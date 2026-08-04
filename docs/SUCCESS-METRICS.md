# SUCCESS-METRICS — Tableau de bord produit Pincab Toolbox

**Document vivant.** On pilote avec des données réelles, pas des impressions. **Se lit en moins de 2 minutes avant chaque session** pour savoir où concentrer l'effort. En cas de contradiction avec `PROJECT-BRAIN.md`, le Brain gagne. Règle sous-jacente : **ADR-008**.

MC Automation — Maxime Chauvin · Créé le 27/07/2026 · 🟢 Vivant

---

## ⏱️ À lire en 30 secondes

- **Phase en cours :** Pré-lancement → **Phase 1 (Validation du Scanner)**.
- **Focus maintenant :** sortir le scanner sur les forums et **établir la baseline**. Tant qu'il n'y a pas d'utilisateurs, tout le reste est une supposition.
- **Décision en attente :** héberger le `.exe` (URL du bouton de téléchargement).
- **Les 3 chiffres qui comptent cette phase :** rapports postés · faux positifs confirmés · % de pannes correctement expliquées.
- **Valeurs actuelles :** — (pré-lancement, à remplir dès J+0).

> **Étoile polaire** (au-dessus de tous les KPI, `ARCHITECTURE §2`) : **% des pannes réelles qu'un scan explique correctement.** Pas le nombre de checks, pas le nombre de téléchargements.

---

## Les KPI (11) — chacun déclenche une décision

Un indicateur qui ne déclenche aucune action n'a pas sa place ici. Aucun KPI de vanité (visites, vues, étoiles). Catégories prioritaires : **confiance · adoption · qualité des diagnostics · conversion Scanner → Repair.**

| # | KPI | Cat. | Source | **Décision qu'il déclenche** | Valeur |
|---|---|---|---|---|---|
| 1 | Faux positifs confirmés | Confiance | 📋 | Tout FP → corrigé **avant** le pack suivant. Si le taux monte → **on gèle l'ajout de checks**. | — |
| 2 | Incidents d'anonymisation (fuite d'identité) | Confiance | 📋/interne | Doit rester **0**. Tout incident → correctif immédiat + communication. | — |
| 3 | Rapports réellement **postés** sur les forums | Confiance + Adoption | 📋 | Téléchargements OK mais peu de rapports postés → le rapport n'inspire pas assez confiance / pas assez partageable → **retravailler la sortie forum**. | — |
| 4 | Téléchargements | Adoption | 🌐 | Stagnant après le post → problème de **message/visibilité**, pas de produit → ajuster post/landing, pas le code. | — |
| 5 | **% de pannes correctement expliquées** (étoile polaire) | Qualité | 📋 dérivé | Bas → **prioriser la justesse** (textes, nouveaux cas) avant toute nouvelle fonctionnalité. | — |
| 6 | Faux négatifs découverts | Qualité | 📋 | Chaque FN validé → nouveau code au **backlog v0.2** ; concentrer les futurs checks sur les FN fréquents. | — |
| 7 | Nouveaux cas ajoutés au Knowledge Pack (/ millésime) | Qualité | interne | Le pack stagne → le volant ne tourne pas et le renouvellement (ADR-002) devient indéfendable → **agir ou arrêter de le vendre**. | — |
| 8 | Top-5 des **codes les plus fréquents** | Conversion | 📋 dérivé | Détermine **quelles règles Repair coder en premier**. | — |
| 9 | Demandes explicites de réparation | Conversion | 📋 | Franchit le seuil d'appétit → **go pour coder l'UI Repair** (sinon elle attend). | — |
| 10 | Manifestations d'intérêt **opt-in** (« préviens-moi quand Repair existe ») | Conversion | 🙋 opt-in | Taille de la liste early-buyers → **go/no-go commercial** + calibrage de l'early-bird. | — |
| 11 | Premiers achats + taux download → achat | Commercial | 🌐 | Valide/invalide le modèle. Les **raisons des non-achats** (📋) orientent le pivot prix/offre. | — |

*Volontairement hors tableau :* scans réalisés, rapports exportés, clics/ouvertures Repair → **non mesurables sans télémétrie**. On ne les suit pas ; leur meilleur proxy est « rapports postés » (#3) et « intérêt opt-in » (#10). Bugs remontés, coût de support, satisfaction → suivis dans le **Field Log** comme opérationnel, pas comme KPI de pilotage (ils ne déclenchent pas de décision stratégique distincte).

---

## Comment on mesure (sans trahir la confiance)

Le système de mesure obéit aux **mêmes règles que le produit** : règle absolue n°2 (zéro télémétrie) + ADR-004 s'appliquent aussi ici. Trois sources autorisées, une interdite :

- 🌐 **Serveur** — compteurs d'actifs publics (téléchargements, ventes, inscriptions opt-in). N'observe **jamais** la machine de l'utilisateur.
- 📋 **Field Log** (`knowledge/FIELD-LOG.md`) — ce que la communauté publie **volontairement** sur les forums. **Saisie manuelle** au début, et c'est suffisant.
- 🙋 **Opt-in** — le rapport anonymisé que l'utilisateur **choisit** de coller (déjà scrubbé, ADR-003) ; toute future collecte au même standard : opt-in, anonymisé, visible.
- 🔒 **Interdit** — instrumentation silencieuse d'événements in-app. On préfère un proxy imparfait mesuré proprement à une vraie métrique obtenue en trahissant la confiance.

**Cibles chiffrées :** on ne les invente pas (`Brain §9`). La Phase 1 établit la baseline ; les cibles se fixent ensuite, phase par phase. Seul repère de réalité documenté : plafond **10-30 K€/an** pour la gamme complète — une borne, pas un objectif de lancement.

---

## La règle de roadmap (ADR-008)

**Tout chantier important doit nommer le KPI qu'il améliore OU le risque (`Brain §8`) qu'il réduit.** À défaut, il est remis en question avant d'entrer dans la roadmap.

```
Chantier : … | KPI visé : … — OU — Risque réduit : … | Preuve de succès : … | Coût : …
```

---

## Cadence

- **48 h de lancement :** relevé quotidien de #4 (téléchargements) + saisie de chaque retour au Field Log.
- **Hebdo :** tri du Field Log → mise à jour de #1, #3, #5, #6, #8, #9 + décisions de pack.
- **Par millésime de pack :** mettre à jour #7.
- **Changement de phase :** figer la baseline, écrire les vraies cibles de la phase suivante ici même.
