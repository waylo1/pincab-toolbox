# Plateformes de paiement — veille, décision reportée

**Statut : 🅿️ PARQUÉ.** Aucune décision à prendre maintenant, aucune conséquence sur l'architecture.
Sujet à rouvrir **quand Repair approchera de la commercialisation**.

*Dernière vérification des faits : 27/07/2026.*

> ⚠️ **Ce document n'est pas un conseil fiscal.** Je ne suis pas comptable. Ce sont des faits
> collectés pour préparer ta décision, à faire confirmer par un professionnel avant la première vente.
> Les frais et surtout les règles de TVA changent — revérifier au moment de décider.

---

## 1. Ce qui compte vraiment dans ce choix

Pas le pourcentage de commission. **Qui est le vendeur légal.**

- **Processeur de paiement** (Stripe seul) — *tu* es le vendeur. Tu encaisses, et **la TVA de chaque pays de tes acheteurs est ton problème**.
- **Merchant of Record** (Lemon Squeezy, Paddle) — *la plateforme* est le vendeur. Elle achète chez toi et revend au client final. Elle collecte et reverse la TVA à ta place. Tu ne vois plus qu'un virement.

Pour une micro-entreprise solo qui vend un logiciel téléchargeable à des particuliers dans toute l'UE, cette différence pèse **beaucoup plus lourd** que 2 points de commission.

---

## 2. Les deux seuils français qui te concernent

| Seuil | Montant | Ce qui se passe au-delà |
|---|---|---|
| **Franchise en base de TVA** (prestations de services, 2026) | **37 500 €** de CA annuel (tolérance jusqu'à 41 250 €) | Tu dois facturer la TVA française |
| **Ventes à distance B2C dans l'UE** | **10 000 €** cumulés, tous pays UE confondus | La TVA devient due **dans le pays de l'acheteur**, et l'inscription au **guichet unique OSS** devient obligatoire — *et c'est à toi de la faire, ce n'est pas automatique* |

### L'observation qui compte pour ton projet

Le plafond réaliste documenté dans le Brain est de **10-30 K€/an**. Tu resterais donc sous la franchise de 37 500 €.

**Mais le seuil de 10 000 € est bien plus bas — et tes acheteurs seront internationaux dès le premier jour** (VPUniverse et VPForums sont anglophones). C'est donc **ce seuil-là qui mordra en premier**, pas la franchise en base.

Autrement dit : la question n'est pas « vais-je dépasser 37 500 € ? », mais **« vais-je vendre pour plus de 10 000 € à des particuliers hors de France ? »**. Sur une bonne année, c'est plausible.

C'est exactement le problème qu'un Merchant of Record fait disparaître : s'il est le vendeur légal, ces ventes ne sont plus les tiennes au sens de la TVA. **À faire confirmer** — c'est le point précis à poser à un comptable, et le seul qui justifie de payer plus cher.

---

## 3. Les plateformes

### Lemon Squeezy — *le plus adapté à ton cas aujourd'hui*

Merchant of Record. **Racheté par Stripe en 2024**, toujours en service en 2026, en cours de convergence vers « Stripe Managed Payments ».

- **~5 % + 0,50 $**, plus ~1,5 % sur les transactions internationales
- **Clés de licence intégrées** — c'est exactement ce que Repair vend, pas d'abonnement à simuler
- Aucun minimum de chiffre d'affaires, inscription rapide
- TVA UE gérée à leur charge

**Réserve, et elle est sérieuse** : Stripe pousse ses utilisateurs vers Stripe Managed Payments et annonce une migration. Le produit n'est plus tout à fait autonome. **À revérifier au moment de décider** — c'est précisément pour ça que ce document est daté.

### Paddle — l'alternative MoR indépendante

Merchant of Record également, positionné SaaS.

- **~5 % + 0,50 $**, plus ~2 % à l'international
- **Validation manuelle du dossier avant activation** : compter du délai, à anticiper avant un lancement forum
- Orienté abonnement ; la licence unique est possible mais moins centrale
- Avantage sur Lemon Squeezy : **indépendant de Stripe**, donc pas exposé au même risque de migration

### Stripe seul — le moins cher, le plus exigeant

**~2,9 % + 0,30 $**, soit environ moitié moins. Mais **tu restes le vendeur légal** : au-delà de 10 000 € de ventes B2C dans l'UE, l'inscription OSS et les déclarations sont à ta charge. Stripe Tax facilite le calcul, il ne fait pas les démarches à ta place.

**Stripe Managed Payments** est leur offre MoR, encore en accès restreint en 2026 (35+ pays). C'est probablement la destination naturelle du sujet — raison de plus pour ne pas figer maintenant.

### Écarté d'emblée

- **Gumroad** — commission élevée, image « creator » plutôt que logiciel professionnel.
- **Paiement direct / virement** — ingérable dès la première dizaine de ventes, et aucune preuve de transaction propre.

---

## 4. Récapitulatif

| | Lemon Squeezy | Paddle | Stripe seul |
|---|---|---|---|
| Vendeur légal | **Eux** | **Eux** | **Toi** |
| TVA UE | à leur charge | à leur charge | **à la tienne (OSS)** |
| Commission | ~5 % + 1,5 % intl | ~5 % + 2 % intl | **~2,9 %** |
| Clés de licence | **intégrées** | possible | à construire |
| Délai d'ouverture | immédiat | validation préalable | immédiat |
| Risque propre | migration vers Stripe | — | charge administrative |

**Penchant actuel, à revérifier le moment venu : Lemon Squeezy**, pour le MoR et les clés de licence intégrées. Paddle est le plan B si la migration Stripe se précise mal. Sur 30 K€ de CA, l'écart de commission avec Stripe représente environ 600 €/an — le prix de ne pas gérer la TVA de dix-sept pays, ce qui est bon marché pour un solo.

---

## 5. Ce qu'il faudra vérifier au moment de décider

1. **Statut réel de Lemon Squeezy** — toujours ouvert aux nouveaux vendeurs ? migration imposée ? frais changés ?
2. **Confirmation par un comptable** : un MoR te dispense-t-il effectivement du seuil de 10 000 € et de l'OSS ? *(c'est le point qui fait ou défait le choix)*
3. **CGV à mettre à jour** — elles nomment Stripe aujourd'hui, et ne décrivent pas encore le renouvellement annuel d'ADR-002.
4. **Frais à jour** — les grilles bougent, ne pas se fier aux chiffres ci-dessus le jour venu.

---

## 6. Ce que ça ne change pas — et c'est le point important

**Rien dans l'architecture.** ADR-002 pose une vérification de licence **100 % locale** (signature hors-ligne, aucun appel réseau obligatoire). La plateforme ne fait que **générer une clé et encaisser** ; l'application n'en sait rien et n'a pas à en savoir quoi que ce soit.

Ce découplage est délibéré : il permet exactement ce qu'on fait ici — **repousser la décision commerciale sans bloquer une seule ligne de code**. Changer de plateforme plus tard coûtera un après-midi de paramétrage, pas une refonte.

Le produit passe avant l'infrastructure commerciale.
