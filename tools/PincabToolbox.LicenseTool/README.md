# Outil de licence — OFFLINE UNIQUEMENT

Génère et signe les licences Repair (ADR-002 : perpétuelle, vérification 100 % locale, aucun appel
réseau). C'est la SEULE partie du projet qui touche la clé privée — elle ne voyage jamais dans
l'App elle-même.

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

Affiche la clé de licence à copier-coller dans l'email/la livraison au client (ex. déclenché
automatiquement par un webhook Lemon Squeezy plus tard — pour l'instant, à la main).

`--updates-months` ne fixe QUE la fenêtre de mise à jour du Knowledge Pack (ADR-002) — la licence
elle-même ne périme jamais, Repair reste débloqué indéfiniment même après cette date.

## Vérifier une clé sans lancer l'App

```
dotnet run --project tools/PincabToolbox.LicenseTool -- verify \
  --public-key <la-clé-publique> --license <la-clé-de-licence>
```

Utile pour confirmer qu'une clé fonctionne avant de l'envoyer à un client.
