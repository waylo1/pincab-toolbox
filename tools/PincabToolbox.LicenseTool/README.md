# Outil de licence — OFFLINE UNIQUEMENT

Génère et signe les licences Repair (ADR-002 : vérification 100 % locale, aucun appel réseau ;
ADR-013 : achat unique à prix fixe, licence perpétuelle, mises à jour incluses **sans limite de
durée**, encaissement Stripe en direct). C'est la SEULE partie du projet qui touche la clé privée —
elle ne voyage jamais dans l'App elle-même.

## Une seule fois : générer ta paire de clés

```
dotnet run --project tools/PincabToolbox.LicenseTool -- init --out ~/pincab-license-key.pem
```

- Le fichier `.pem` (clé **privée**) part sur ta machine, **jamais dans le dépôt git** (déjà dans
  `.gitignore` si tu le mets dans le repo par erreur — mais range-le ailleurs, ex. gestionnaire de
  mots de passe ou disque chiffré). Si tu la perds, les licences déjà vendues continuent de
  fonctionner (vérification locale), mais tu ne peux plus en signer de nouvelles avec cette identité.
- La commande affiche aussi la clé **publique** (une seule ligne base64) — colle-la dans
  `src/PincabToolbox.Repair/Licensing/LicenseVerifier.cs`, constante `EmbeddedPublicKeyBase64`,
  puis recompile. C'est ce qui verrouille l'App sur CETTE paire de clés précise.

## À chaque vente : émettre une clé pour le client

```
dotnet run --project tools/PincabToolbox.LicenseTool -- issue \
  --key ~/pincab-license-key.pem --email client@exemple.com --updates-months 12
```

Affiche la clé de licence à copier-coller dans l'email envoyé au client après son paiement Stripe
(ADR-013 — encaissement en direct, pas de Merchant of Record ; pour l'instant l'envoi reste manuel,
aucun webhook n'est câblé).

⚠️ **`--updates-months` (défaut 12) est un reliquat d'ADR-002, périmé depuis ADR-013 (19/08/2026) :
la licence Repair n'a plus de fenêtre de mise à jour bornée, les mises à jour sont incluses **sans
limite de durée** et il n'y a plus de renouvellement à vendre.** Le paramètre lui-même n'a pas été
retiré ici — le corriger (nouveau défaut, ou suppression) est une décision produit à trancher par
Maxime, pas faite unilatéralement dans ce commit. En attendant cette décision, passer une valeur
très large (ex. `--updates-months 1200`, soit 100 ans) obtient le même résultat pratique pour les
clés émises dès maintenant : la licence elle-même n'a jamais expiré, seule la fenêtre de mise à jour
l'était.

## Vérifier une clé sans lancer l'App

```
dotnet run --project tools/PincabToolbox.LicenseTool -- verify \
  --public-key <la-clé-publique> --license <la-clé-de-licence>
```

Utile pour confirmer qu'une clé fonctionne avant de l'envoyer à un client.
