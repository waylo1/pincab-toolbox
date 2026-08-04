# Guide de test sur ton pincab (session 1)

## 0. Prérequis (5 min)

1. Sur le PC du pincab (ou ton PC dev), installe le SDK .NET 8 si absent :
   `winget install Microsoft.DotNet.SDK.8`
2. Copie le dossier du projet, puis double-clique `build.cmd`.
   → Il lance les tests puis produit `publish\PincabToolbox.exe`.
3. Lance `PincabToolbox.exe`. SmartScreen râlera (exe non signé — normal en Phase 0) : « Informations complémentaires » → « Exécuter quand même ».

## 1. Scan de vérité (10 min)

1. Onglet **Scanner** → « Parcourir » → choisis la racine de ton install (le dossier qui contient `Tables`, `VPinMAME`, `PinUPSystem`).
2. Lance le scan. Note le nombre de vérifications affiché.
3. **Vérifie chaque CRITIQUE annoncé** : le scanner dit qu'une ROM manque → va voir dans `VPinMAME\roms` si c'est vrai. C'est LE test qui compte : zéro faux positif critique toléré.
4. Compare le compte de tables détectées avec la réalité.

## 2. Cas limites à tester (10 min)

- [ ] Une table **sans ROM** (originale/EM) → doit être « ne nécessite pas de ROM », PAS « ROM manquante ».
- [ ] Une table avec **alias** dans `VPMAlias.txt` → doit résoudre.
- [ ] Table `.vpx` corrompue ou exotique → « script illisible », pas de crash.
- [ ] Dossier racine sans rien (ex. `C:\Windows`) → message propre, pas de crash.
- [ ] Débranche le réseau puis scanne → l'Update Watcher doit dire « base indisponible » et continuer.
- [ ] **Backglass mal nommé** (renomme un `.directb2s` pour qu'il ne corresponde à aucune table) → « orphelin », pas juste « backglass manquant ».
- [ ] **Serveur B2S absent** (`.directb2s` présents mais pas de `B2SBackglassServer.dll`) → avertissement « Plugins · B2S Backglass Server ».
- [ ] **ROM dézippée** (décompresse un `.zip` de ROM en dossier) → « ROM présente mais dézippée », pas « ROM manquante ».
- [ ] Bouton **FR / EN** → tous les textes basculent (y compris le libellé « Plugins »).
- [ ] **Exporter le rapport** (HTML) et **Copier pour le forum** → vérifie le rendu (le bouton passe à « ✓ Copié »).

## 3. Diff (5 min)

1. Onglet **Diff de scripts** : prends une table dont tu as deux versions (ou copie une table, modifie 2 lignes de script dans l'éditeur VPX, sauvegarde).
2. Compare → les lignes modifiées doivent être surlignées, le résumé cohérent.

## 4. Ce que je veux comme retour

1. Temps de scan + nombre de tables.
2. Chaque faux positif / faux négatif (copie la ligne du rapport).
3. Le message le plus confus à tes yeux (on le réécrira).
4. Une capture d'écran de l'onglet Scanner rempli — elle servira pour le post forum.
5. Verdict subjectif : est-ce que tu paierais 19 € pour la version qui RÉPARE ce qu'il liste ? (prix figé dans `docs/adr/ADR-002`)

## Bugs connus / limites v0.1.0-alpha

- Update Watcher : matching par « Nom (Fabricant Année) » du nom de fichier — les fichiers nommés autrement ne matchent pas (c'est prévu, bêta).
- La version installée de VPX n'est pas encore lue depuis l'exe (linter basé sur les signatures de script uniquement).
- Exe non signé : alertes SmartScreen normales jusqu'au certificat (Phase 1).
