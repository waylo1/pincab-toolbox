# ADR-007 — L'écriture dans la base PinUP Popper sort du périmètre de Repair v1

**Statut** : Accepté · **Date** : 25/07/2026 · **Décideur** : décision d'ingénierie prise pendant l'implémentation

---

## Contexte

`Knowledge.cs` marque **trois** codes comme `AutoFixable` : `BLOCKED_DLL`, `ROM_UNZIPPED` et `POPPER_NOT_REGISTERED`. Le design de Repair v1 prévoyait donc trois actions.

À l'implémentation, la troisième s'est révélée d'une nature différente des deux autres.

`POPPER_NOT_REGISTERED` se corrige en **insérant une ligne dans `PUPDatabase.db`**, une base **SQLite**. Or :

- Le Core possède un `SqliteReader.cs` écrit à la main, **en lecture seule**. Lire un fichier SQLite est faisable sans bibliothèque ; **y écrire ne l'est pas** raisonnablement. Il faudrait gérer les pages B-tree, les listes de pages libres, le journal, les index et le compteur de changement — une erreur sur l'un de ces points **corrompt la base du frontend de l'utilisateur**.
- Ajouter une bibliothèque SQLite casse la règle « zéro dépendance » qui tient depuis le début du projet, dans les deux assemblys.
- C'est aussi, des trois, le fichier **le plus douloureux à perdre** : la base Popper contient toute la bibliothèque de tables, les médias associés et les réglages. Un utilisateur qui perd sa `PUPDatabase.db` perd des heures de configuration.

Autrement dit : le correctif le plus risqué du lot serait aussi le premier à écrire dans un format qu'on ne maîtrise pas en écriture.

## Décision

**`POPPER_NOT_REGISTERED` reste `ManualOnly` en v1.** Le pack `2026.08` ne porte aucune règle de réparation pour ce code ; il porte une procédure manuelle claire, en FR et en EN.

`AutoFixable = true` reste vrai dans `Knowledge.cs` — c'est une **frontière commerciale** (« ce problème appartient au domaine de Repair »), pas une promesse d'implémentation. Les deux portes du gating font exactement leur travail : la porte commerciale dit oui, l'absence de règle fait retomber en manuel. **Aucune ligne de code n'a été nécessaire pour gérer ce cas**, ce qui est le bon signe que la séparation donnée/code d'ADR-005 tient.

## Alternatives, à trancher pour la v1.1

1. **Accepter une dépendance SQLite vérifiée** (`Microsoft.Data.Sqlite`). Fiable et éprouvé, mais rompt la règle zéro-dépendance et alourdit le binaire auto-contenu. Si on la prend, la prendre **uniquement dans `PincabToolbox.Repair`**, jamais dans le Core — le scanner gratuit doit rester sans dépendance.
2. **Déléguer à `sqlite3.exe` s'il est présent.** Zéro dépendance embarquée, mais résultat non déterministe selon la machine — mauvais pour un outil dont l'argument est la fiabilité.
3. **Écrire un writer SQLite minimal maison.** Séduisant intellectuellement, disproportionné en risque pour un `INSERT`.

**Penchant actuel : l'option 1, cantonnée à Repair.** À décider quand la fonctionnalité sera réellement demandée par les retours terrain, pas avant.

## Conséquences

- Repair v1 livre **deux** actions au lieu de trois : `unblock_file` et `restore_rom_archive`. Le périmètre est plus petit et entièrement testé.
- L'utilisateur voit une procédure manuelle propre plutôt qu'un bouton qui pourrait corrompre sa bibliothèque.
- La règle « on ne fait rien qu'on ne sait pas défaire » (ADR-004 §5) est respectée jusqu'au bout : on ne sait pas encore défaire proprement une écriture SQLite, donc on ne l'écrit pas.
- Le test `Test_ShippedPack_PopperRegistrationIsManualInV1` **verrouille cette décision** : si quelqu'un ajoute une règle Popper au pack sans avoir tranché cette ADR, la suite de tests échoue.
