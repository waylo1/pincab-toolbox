# ADR-002 — Modèle économique, packaging et licence

**Statut** : ⚠️ **Partiellement superseded par `ADR-013` (19/08/2026)** · **Date** : 25/07/2026 · **Décideur** : Maxime Chauvin
**Remplace** : `../_archive/strategie-prix.md` (archivé)

> ⚠️ **Le prix et la durée de licence décrits ici ne sont plus en vigueur.** `ADR-013` fixe un prix
> unique de **3,99** (même nombre en EUR/USD/GBP), achat unique, licence perpétuelle, mises à jour
> incluses sans limite de durée. Le renouvellement optionnel à 9 €/an décrit plus bas est
> **supprimé**. Tout le reste de cet ADR (Scanner gratuit à vie, vérification de licence 100 %
> locale, zéro télémétrie, Knowledge Pack) **reste en vigueur**.

---

## Contexte

Contradiction frontale entre deux documents :

- `../_archive/strategie-prix.md` (18/07) : « one-shot 9–19 €, **pas d'abonnement**, mises à jour mineures incluses », justifié par l'allergie de la communauté pincab aux abonnements.
- Décision figée du 22/07 (n°4) : « privilégier le **récurrent** — usage perpétuel + mises à jour annuelles payantes, adossées à la base de connaissance ».

Les deux ont raison sur un point : la communauté refuse d'être prise en otage, mais un outil de maintenance dont la valeur vient d'une base de connaissance vivante ne peut pas être vendu une fois pour toutes.

## Décision

### Prix

| Palier | Prix | Contenu |
|---|---|---|
| Scanner | **Gratuit à vie** | Diagnostic complet, illimité, jamais bridé |
| Repair — early bird | **12 €** | Premiers acheteurs du forum. Perpétuel + 12 mois de MAJ |
| Repair — prix normal | **19 €** | Perpétuel + 12 mois de MAJ |
| Renouvellement | **9 € / an, optionnel** | Prolonge l'accès aux mises à jour de 12 mois |

### La mécanique

L'achat donne une **licence perpétuelle**. L'abonnement ne porte **que sur les mises à jour**. Sans renouvellement, l'application continue de fonctionner indéfiniment avec le dernier Knowledge Pack reçu — elle cesse simplement d'en recevoir de nouveaux.

C'est le modèle JetBrains / Sublime Text. Il réconcilie les deux documents : récurrent pour nous, jamais coercitif pour l'utilisateur.

### La condition de validité

**Le récurrent n'est justifié que par un Knowledge Pack qui s'enrichit réellement** : nouveaux codes de finding, nouveaux correctifs, compatibilité avec les nouvelles versions de VPX. Si le pack cesse de s'enrichir, le renouvellement devient indéfendable et **il faut arrêter de le vendre**. Ce n'est pas une clause de style : c'est ce qui distingue ce modèle d'une rente.

### Packaging et licence

- **Un seul exécutable, un seul installeur.** La licence déverrouille la colonne « Réparer » dans le tableau de résultats.
- Le Scanner reste **complet et gratuit** dans ce même exe — jamais bridé pour pousser à l'achat.
- **Vérification de licence 100 % locale** (signature hors-ligne liée à l'e-mail), aucun appel réseau obligatoire pour activer. Cohérent avec le discours zéro télémétrie.
- **Anti-piratage volontairement léger.** Le public visé achète par soutien à un outil qui résout un vrai problème ; investir dans la protection coûterait plus cher que le piratage.

## Alternatives écartées

- **One-shot pur à vie** : contredit la décision figée du 22/07, et ne finance pas l'entretien de la base de connaissance.
- **Abonnement classique** (~3-4 €/mois, l'app s'arrête si on ne paie plus) : maximise le revenu théorique, tuerait la conversion sur cette niche.
- **Exécutables séparés par ligne produit** : séparation gratuit/payant plus nette, mais duplique l'UI et triple la charge de distribution pour une micro-entreprise solo.

## Conséquences

- `../_archive/strategie-prix.md` est archivé ; ses fourchettes de prix ne font plus foi.
- **Les CGV doivent être mises à jour avant la première vente** : §4 (prix) et §10 (licence) ne prévoient pas l'abonnement de mises à jour optionnel. À traiter avec les quatre points déjà signalés dans le brouillon (TVA/OSS, rétractation, médiateur, mentions légales).
- **La plateforme de paiement redevient une décision ouverte** : gérer des renouvellements annuels avec TVA UE change l'arbitrage. Recommandation : Lemon Squeezy (Merchant of Record — TVA UE à leur charge, licences et renouvellements natifs) plutôt que Stripe seul. **À valider avec le comptable** ; conséquences fiscales, donc non tranché ici.
