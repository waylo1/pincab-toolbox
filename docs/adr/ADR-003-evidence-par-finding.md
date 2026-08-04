# ADR-003 — `Evidence` par Finding

**Statut** : Accepté · **Date** : 25/07/2026 · **Décideur** : Maxime Chauvin

---

## Contexte

La priorité qualité n°1 du projet est **zéro faux positif**. Mais un faux positif finit toujours par arriver, et la vraie question n'est pas de l'éviter à 100 % — c'est de faire en sorte qu'un utilisateur puisse **vérifier notre affirmation lui-même** et nous corriger.

Aujourd'hui, un `Finding` affirme (« VPinMAME est en 32-bit alors que VPX est en 64-bit ») sans jamais montrer sur quoi il s'appuie. Le premier post forum se heurtera à « pourquoi je devrais te croire ? ».

**Ce qui existe déjà et ne doit pas être dupliqué** : `KnowledgeEntry.verification` (`ARCHITECTURE-KnowledgeEngine.md` §5.2) décrit **comment un check vérifie, en général** — c'est une description statique, la même pour toutes les machines. Ce n'est pas ce dont on parle ici.

## Décision

Ajouter à `Finding` une liste **`Evidence`** : ce qui a été **réellement observé sur cette machine-là**, pour cette occurrence-là.

```
Finding {
  ... champs existants ...
  Evidence[]              // vide par défaut
}

EvidenceItem {
  Kind        // File | Registry | Ini | PeHeader | Sqlite | Process
  Locator     // chemin, clé de registre, section/clé d'ini
  Observed    // valeur constatée, courte et tronquée
}
```

Rendu attendu dans l'UI, replié par défaut (niveau expert) :

```
BITNESS_MISMATCH_VPM
  ▸ Preuves
    PeHeader   VPinMAME.dll        x86
    PeHeader   VPinballX.exe       x64
    Registry   HKCU\...\VPinMAME   présent
```

### Règles

1. **`verification` (Knowledge) répond à « comment je vérifie » ; `Evidence` (Finding) répond à « ce que j'ai vu chez toi ».** Deux choses distinctes, aucune fusion.
2. **`Evidence` est optionnelle**, valeur par défaut liste vide. Les scanners existants continuent de fonctionner sans modification — les 42 tests verts ne bougent pas.
3. **Anonymisation à l'export.** Le rapport est destiné à être **posté sur un forum public**. Les chemins utilisateur doivent être tronqués dans l'export texte (`C:\Users\<user>\...`). Une preuve qui fuite un nom d'utilisateur est un incident de confiance, pas un détail.
4. **`Observed` reste court.** Pas de dump de fichier, pas de contenu de script. Une valeur, un état, une version.
5. Ne remplit pas `Evidence` qui veut : un scanner qui n'a rien de solide à montrer n'invente pas de preuve. Une liste vide est un signal honnête.

## Alternatives écartées

- **Étendre `verification` dans le Knowledge Pack** : impossible, c'est du texte statique par code, il ne peut pas contenir les valeurs constatées chez un utilisateur donné.
- **Tout mettre dans `Args`** : `Args` sert au template localisé, il est ordonné et destiné à l'affichage. Y glisser des preuves casserait la localisation et rendrait les deux illisibles.
- **Un log de debug séparé** : personne ne lit un log. La preuve doit être à côté de l'affirmation, sinon elle ne sert pas la confiance.

## Conséquences

**Positives**
- Un faux positif devient **auditable** : l'utilisateur voit notre raisonnement, nous signale l'erreur, et le check s'améliore. C'est le flywheel de la base de connaissance qui se met en marche sur le terrain.
- Brique de **moteur**, pas de Scanner : elle se transporte telle quelle vers Repair (montrer les preuves avant d'écrire), vers Table Companion, et vers le futur assistant physique — où « la preuve » devient « le symptôme que tu m'as décrit ».

**Coût** — faible côté Core (l'information est déjà en mémoire au moment du check, on ne fait que la conserver), moyen côté UI (bloc repliable + anonymisation à l'export).

**Séquencement** — v0.2, **après** le lancement forum. Ajouter un champ maintenant est bon marché ; le rendu UI ne doit pas retarder le test terrain, qui reste prioritaire sur tout.
