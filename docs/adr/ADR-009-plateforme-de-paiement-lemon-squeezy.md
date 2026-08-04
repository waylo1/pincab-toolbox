# ADR-009 — Plateforme de paiement : Lemon Squeezy (Merchant of Record)

**Statut** : ✅ **Accepté** · **Date** : 27/07/2026 · **Décideur** : Maxime Chauvin
**Dé-parque** : `PROJECT-BRAIN §5` et `docs/PARKING-plateformes-paiement.md` (la décision y était volontairement reportée).

---

## Contexte

La vente de Repair approche assez pour qu'on tranche l'infrastructure d'encaissement (le Brain gardait la décision parquée jusqu'à ce moment). Le profil est particulier : **micro-entreprise française, solo, qui vend un logiciel dans le monde entier dès le premier jour.**

Le point qui mord en premier n'est pas la franchise en base de TVA française, mais la **TVA/taxes sur les ventes B2C à l'international** : l'UE impose la collecte de TVA au taux du pays de l'acheteur dès le **seuil de 10 000 €** de ventes B2C intra-UE (guichet OSS), et d'autres juridictions (US sales tax, UK, etc.) ont leurs propres règles. Gérer ça seul, en micro-entreprise, est intenable.

## Décision

**Encaisser via Lemon Squeezy, en tant que Merchant of Record (MoR).**

Un MoR devient le **vendeur légal** vis-à-vis de l'acheteur : il collecte et reverse la TVA / sales tax dans le monde entier à ta place, gère les factures conformes, la fraude et les remboursements. Toi, tu reçois des **versements** (payouts) et tu génères la clé de licence. C'est la seule option qui permet à un solo de vendre mondialement sans monter une machine de conformité fiscale.

**Ce dont la décision ne dépend PAS :** la vérification de licence reste **100 % locale** (ADR-002, signature hors-ligne, cohérent zéro-télémétrie). La plateforme ne fait que **générer une clé et encaisser**. Rien dans l'architecture n'en dépend — changer d'avis plus tard coûte un après-midi de paramétrage, pas une refonte (déjà acté `Brain §5`).

## Alternatives écartées

- **Stripe / PayPal en direct** — les frais bruts sont plus bas, mais **tu deviens responsable de la TVA mondiale** : déclarations OSS, sales tax US, factures par pays. Rédhibitoire en solo. (Note : Lemon Squeezy a été racheté par Stripe en 2024 et opère toujours comme MoR — on garde donc l'écosystème Stripe *sans* en porter la charge fiscale.)
- **Paddle** — MoR équivalent, également valable. Choix de Lemon Squeezy assumé (produit orienté indie/logiciel, intégration simple). Paddle reste le plan B naturel si Lemon Squeezy ne convient plus.
- **Gumroad** — MoR aussi, mais moins souple sur les licences et l'image « pro ».

## Conséquences

- `PROJECT-BRAIN §5` passe de « décision reportée » à « **décidé : Lemon Squeezy (ADR-009)** ». Le document `PARKING-plateformes-paiement.md` reste comme trace de la veille, marqué résolu.
- Le MoR prélève une **commission par vente** (supérieure à un PSP brut type Stripe direct) : c'est le prix de la conformité déléguée. Le montant exact est **à confirmer à la signature** (les grilles évoluent) et à intégrer au calcul de marge — mais ne change pas la décision.
- **Ne bloque pas le lancement du Scanner gratuit** : aucun paiement n'est requis tant que Repair n'est pas en vente. C'est une brique Phase 3.
- **Points à cadrer avec ton comptable / dans l'audit juridique** (hors périmètre de cet ADR, je ne suis pas juriste ni fiscaliste) : traitement des payouts MoR dans le chiffre d'affaires micro-entreprise et les plafonds, mentions de facturation (« TVA non applicable, art. 293 B du CGI » côté micro vs TVA gérée par le MoR côté acheteur), CGV/EULA à jour du modèle de renouvellement, rétractation sur produit numérique.

## À surveiller

- **Frais et conditions** de Lemon Squeezy après le rachat Stripe — vérifier qu'ils restent alignés avec le plafond de réalité de **10-30 K€/an** (`Brain §9`).
- **Dépendance à un tiers** : la clé de licence étant vérifiée localement, une défaillance ou un changement de politique du MoR n'empêche jamais le logiciel déjà vendu de fonctionner — garde-fou déjà posé par ADR-002.
