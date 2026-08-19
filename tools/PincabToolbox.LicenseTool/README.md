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

## À chaque vente (ou à chaque testeur) : émettre une clé

```
dotnet run --project tools/PincabToolbox.LicenseTool -- issue \
  --key ~/pincab-license-key.pem --email client@exemple.com
```

Affiche la clé de licence à copier-coller dans l'email envoyé au client après son paiement Stripe
(ADR-013 — encaissement en direct, pas de Merchant of Record ; pour l'instant l'envoi reste manuel,
aucun webhook n'est câblé).

`--email` n'est qu'une étiquette libre stockée dans la licence (jamais validée comme une vraie
adresse, l'outil n'envoie lui-même aucun mail) — pour un testeur qui n'a pas d'email à donner, une
étiquette du genre `--email testeur-1` fonctionne tout aussi bien.

✅ **`--updates-months` — décidé le 19/08/2026 par Maxime : défaut passé de 12 (reliquat d'ADR-002)
à `1200` (100 ans).** ADR-013 supprime toute fenêtre de mise à jour bornée sur la licence Repair —
`1200` est la façon pratique de représenter « sans limite » sans réécrire le modèle de licence
(qui reste, en interne, une date de fin de fenêtre). Peut toujours être surchargé avec
`--updates-months <n>` si un jour un besoin différent apparaît.

## Vérifier une clé sans lancer l'App

```
dotnet run --project tools/PincabToolbox.LicenseTool -- verify \
  --public-key <la-clé-publique> --license <la-clé-de-licence>
```

Utile pour confirmer qu'une clé fonctionne avant de l'envoyer à un client.
