# Bac à sable Repair

L'équivalent du **mode démo du Scanner**, pour Repair.

```
dotnet run --project tools/PincabToolbox.Repair.Demo
dotnet run --project tools/PincabToolbox.Repair.Demo -- --keep    # garde le bac à sable pour l'inspecter
```

## Ce que ça fait

Fabrique une fausse installation sous `%TEMP%\PincabToolbox-RepairDemo\`, y reproduit de vraies
pannes, puis lance le **vrai moteur Repair** dessus — le vrai `RealFileSystem`, le vrai
`knowledge/pack-2026.08.json`, les vraies actions. Rien n'est simulé côté moteur.

**Aucune installation réelle n'est touchée.** Tout se passe dans le dossier temporaire, supprimé
en fin d'exécution sauf avec `--keep`.

## Les cinq scénarios

1. **DLL bloquée par Windows** — plan, application, vérification, annulation. *Ne s'exécute
   réellement que sous Windows* : le marqueur « Mark of the Web » est une particularité NTFS.
   C'est le seul chemin de code que les tests automatisés ne peuvent pas couvrir sous Linux —
   d'où l'intérêt de lancer cette démo sur ton PC.
2. **ROM décompressée** — vérifie que l'archive produite est une vraie archive lisible, et que le
   dossier d'origine est *mis de côté*, jamais supprimé.
3. **Gratuit vs sous licence** — affiche côte à côte ce que voit le Scanner et ce qu'ajoute Repair (ADR-006).
4. **Refus au préflight** — VPX qui tourne, puis espace disque insuffisant. Vérifie que rien n'est écrit.
5. **Tout annuler** — deux réparations, une seule annulation, et l'annulation en double qui ne casse rien.

Chaque scénario vérifie son propre résultat. **Code de sortie non nul si un seul contrôle échoue** :
la démo est aussi un test de fumée, utilisable en CI.

## Ce qu'elle a déjà attrapé

- Un chemin de sauvegarde contenant `..`, affiché tel quel sur l'écran de récupération — l'endroit
  où la clarté compte le plus.
- « 1 files » au lieu de « 1 file » dans un texte vu par l'utilisateur.

Les deux sont corrigés et verrouillés par des tests.
