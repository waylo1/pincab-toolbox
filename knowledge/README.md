# Knowledge Pack

La connaissance est de la **donnée**, pas du code (`ARCHITECTURE-KnowledgeEngine.md` §4).
Ce dossier contient le format, un pack de départ, et le validateur qui fait respecter **ADR-005**.

```
knowledge/
  knowledge-pack.schema.json   schéma JSON (draft 2020-12)
  pack-2026.08.json            pack de départ — périmètre Repair v1
  validate_pack.py             validateur : ce que le schéma ne peut pas voir
  selftest.py                  12 packs volontairement cassés, pour prouver que le validateur mord
```

## Lancer la validation

```bash
python3 validate_pack.py pack-2026.08.json --registry ../src/PincabToolbox.Repair
python3 selftest.py
```

Le validateur lit les `ActionId` **directement dans le code C#**. C'est le point important :
un pack ne peut pas déclarer une capacité, seulement en composer. Si quelqu'un ajoute
`"actionId": "delete_everything"` dans un pack, la CI refuse le commit.

État vérifié : **pack valide, 1 avertissement** (le playbook de migration est partiel — c'est
voulu et attendu). **12/12 garde-fous confirmés** par le self-test.

## Provenance des textes

Les champs **`impact*` et `cause*` sont extraits verbatim de `Knowledge.cs`** — migration réelle,
faite par script pour éviter toute erreur de recopie.

Les champs `title*`, `player*`, `explanation*` et `verification*` sont **nouveaux** : ils n'existaient
nulle part (`Knowledge.cs` ne portait qu'Impact/Cause/Refs/AutoFixable, les libellés venant de
`Loc.FrFindings`). À relire, ce sont les seuls textes que tu n'as pas écrits.

## Ce qui reste à migrer

Cinq codes sur les dix-neuf sont couverts — exactement le périmètre de Repair v1.
Les quatorze autres sont à porter depuis `Knowledge.cs`, **sans règle de réparation**
(diagnostic seul) tant que leur confiance n'a pas été calibrée sur cab réel :

| Catégorie | Codes restants |
|---|---|
| `rom` | `ROM_MISSING` · `ROM_OK` · `ROM_NOT_REQUIRED` |
| `bitness` | `BITNESS_MISMATCH_VPM32` · `BITNESS_HYBRID_INSTALL` |
| `completeness` | `B2S_MISSING` · `B2S_ORPHAN` · `POPPER_MEDIA_MISSING` |
| `dependencies` | `B2S_SERVER_MISSING` · `FLEXDMD_MISSING` |
| `compat` | `COMPAT_MIN_VERSION` · `COMPAT_SIGNATURE` |
| `updates` | `UPDATE_AVAILABLE` |
| `security` | `SCRIPT_UNREADABLE` |

Le validateur refusera tout `TODO` résiduel : impossible de publier un pack à moitié migré.

## Deux champs à ne pas confondre

- **`verificationFr/En`** (ici, dans le pack) : *comment le check vérifie, en général*. Texte statique, identique pour tout le monde.
- **`Evidence`** (ADR-003, sur le `Finding`) : *ce qui a été observé chez cet utilisateur-là*. Par occurrence.

Le premier est de la documentation. Le second est une preuve. Ils ne se remplacent pas.

## Le lien avec le modèle économique

ADR-002 fait reposer le renouvellement annuel sur l'enrichissement de ce pack.
Concrètement, ce qui justifie les 9 €/an, c'est le **millésime** : `packVersion` passe de
`2026.08` à `2026.09` avec de nouveaux codes, de nouveaux correctifs, et des confiances
recalibrées par le terrain.

Si ce dossier cesse de bouger, le renouvellement devient indéfendable et il faut arrêter
de le vendre. C'est écrit dans ADR-002 et ce n'est pas une clause de style.
