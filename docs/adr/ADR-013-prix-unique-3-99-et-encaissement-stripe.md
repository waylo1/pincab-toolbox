# ADR-013 — Prix unique 3,99 et encaissement Stripe en direct

**Statut** : ✅ **Accepté** · **Date** : 19/08/2026 · **Décideur** : Maxime Chauvin
**Supersede** : `ADR-002` (partie prix et durée de licence uniquement) et `ADR-009` (intégralement).

---

## Contexte

`ADR-002` (25/07/2026) fixait Repair à 19 € (12 € early bird), licence perpétuelle incluant 12 mois de mises à jour, plus un renouvellement optionnel à 9 €/an. `ADR-009` (27/07/2026) choisissait Lemon Squeezy en Merchant of Record pour déléguer la conformité TVA mondiale.

Deux éléments ont changé depuis :

1. **Le signal de prix venu de la communauté.** Observation de Maxime en suivant les échanges publics du milieu pincab : « la communauté n'a pas l'air d'acheter bien cher ». Un tarif à deux chiffres pour un utilitaire de diagnostic, dans une communauté d'amateurs qui assemblent eux-mêmes leur cabinet et partagent gratuitement l'essentiel de leurs outils, se heurte à un plafond psychologique bien plus bas que ce que `ADR-002` supposait.
2. **La complexité du modèle lui-même.** Perpétuel + 12 mois de mises à jour + renouvellement annuel demandait trois paragraphes de CGV pour être expliqué honnêtement. À un prix bas, ce coût d'explication dépasse la valeur qu'il capture.

## Décision

**1. Prix unique : 3,99 — même nombre quelle que soit la monnaie.**

| Devise | Prix |
|---|---|
| EUR | 3,99 € |
| USD | 3.99 $ |
| GBP | 3.99 £ |

Pas de conversion au taux du jour : le prix affiché est le même nombre partout, la devise s'adapte au pays de l'acheteur. C'est un choix de lisibilité assumé, pas une parité économique.

**2. Achat unique, licence perpétuelle, mises à jour incluses sans limite de durée.** Le renouvellement optionnel à 9 €/an de `ADR-002` est **supprimé** : à 3,99, un renouvellement annuel coûterait plus du double du produit lui-même. Il n'y a plus qu'une seule ligne de prix et une seule chose à comprendre — vous payez une fois, vous avez le logiciel et ses mises à jour.

**3. Encaissement : Stripe, en direct.** `ADR-009` (Lemon Squeezy en Merchant of Record) est abandonné. MC Automation reste le vendeur légal vis-à-vis de l'acheteur.

**4. Le tunnel d'achat n'est pas construit et ne doit pas l'être** tant que Maxime ne le demande pas explicitement (décision du 19/08). Aucune vente n'est ouverte avant le retour des testeurs.

## Alternatives écartées

- **Rester à 12 €/19 €** (`ADR-002`) — écarté sur le signal communautaire ci-dessus. Un prix qu'on peut baisser plus tard sans casse est préférable à un prix qui ne se vend pas.
- **Garder Lemon Squeezy en MoR** (`ADR-009`) — le MoR reste le seul montage qui délègue entièrement la conformité fiscale mondiale, et cet argument reste techniquement valable. Il est écarté au profit de Stripe en direct : commission plus basse, ce qui pèse beaucoup plus lourd à 3,99 qu'à 19 €, et infrastructure déjà en place pour les clés de test. La conséquence fiscale est réelle et assumée, voir ci-dessous.
- **Renouvellement à un tarif réduit** — écarté explicitement le 19/08 au profit de la simplicité.

## Conséquences

**Fiscales — le point qui mord, et il est réel.** En vendant via Stripe en direct, MC Automation redevient responsable de la TVA due dans le pays de l'acheteur, ce que le MoR prenait en charge :

- La **franchise en base française** (art. 293 B du CGI) couvre les ventes en France. Elle ne couvre PAS le mécanisme européen.
- Le **seuil UE de 10 000 €** de ventes B2C transfrontalières s'applique même à une micro-entreprise en franchise en base. En dessous : rien à faire. Au-dessus : immatriculation au guichet unique **OSS** et facturation de la TVA au taux du pays de l'acheteur.
- **Hors UE** (US, UK notamment) : chaque juridiction a ses propres règles pour les services numériques vendus à des consommateurs. À faire confirmer par un comptable avant d'ouvrir la vente à ces pays, ou à restreindre géographiquement au départ.
- **Ordre de grandeur** : à 3,99, le seuil de 10 000 € représente environ 2 500 ventes. Ce n'est pas un problème du jour 1, mais c'est un seuil qu'il faut surveiller et non découvrir.

> **ACTION MAXIME** — point à cadrer avec un comptable avant la première vente publique, pas avant les tests. Ce n'est pas un blocage du lancement du Scanner gratuit, qui ne met en jeu aucun paiement.

**Sur les documents juridiques.** Les CGV FR et EN, la page `cgu.html` du landing et `docs/legal/CGU-CGV-mentions-legales.md` sont à réaligner : grille de prix, absence de renouvellement, Stripe nommé comme prestataire (et non plus vendeur légal), section TVA réécrite. Fait le 19/08 dans le même lot que cet ADR.

**Sur l'architecture : rien.** La vérification de licence reste 100 % locale (signature hors-ligne, zéro télémétrie, `ADR-002`). Le prestataire de paiement ne fait qu'encaisser et déclencher la génération d'une clé. Changer d'avis à nouveau coûte un après-midi de paramétrage, pas une refonte — ce garde-fou posé par `ADR-002` puis `ADR-009` reste vrai et vient de servir deux fois.

**Sur la marge.** À 3,99, la commission fixe par transaction (part fixe de Stripe, de l'ordre de 0,25 € en zone euro, à confirmer sur la grille en vigueur) pèse proportionnellement bien plus qu'à 19 €. C'est une donnée à intégrer au plafond de réalité de 10-30 K€/an (`Brain §9`), pas un motif de revenir sur le prix.

## À surveiller

- Le **cumul des ventes B2C intra-UE** par rapport au seuil de 10 000 €.
- Le **taux de conversion** à 3,99 après retour des testeurs : c'est ce chiffre, pas une intuition, qui dira si le prix est juste. Un prix bas mal converti signale un problème de valeur perçue, pas de tarif.
- La **grille de commission Stripe** en vigueur au moment d'ouvrir la vente.
