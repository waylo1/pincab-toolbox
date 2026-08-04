# ADR-006 — Le Scanner annonce la réparation, Repair vend le plan

**Statut** : ✅ **Accepté** · **Date** : 25/07/2026 · **Décideur** : Maxime Chauvin
**Historique** : une première version proposait le dry-run complet en gratuit. **Rejetée par Maxime**, à raison. Le compromis ci-dessous est le sien.

---

## Contexte

Repair doit être gated par la licence. Reste à choisir **où** poser la porte.

Le calcul du plan (`IRepairAction.Plan()`) est pur et sans effet de bord par construction. Techniquement, on peut donc le montrer ou le cacher sans rien changer au moteur. La question est entièrement commerciale.

Deux positions extrêmes, toutes deux mauvaises :

- **Tout cacher** — « 3 problèmes réparables 🔒 » et rien d'autre. Bride artificiellement le Scanner, contredit la règle absolue n°2, et demande à l'utilisateur de croire sur parole que Repair est sûr.
- **Tout montrer** — chemin, valeur avant → après, ordre exact des opérations. C'était ma première proposition. **Elle transforme le produit payant en tutoriel gratuit** : un utilisateur avancé lit le plan et l'applique à la main. On ne vendrait plus une réparation, on publierait un mode d'emploi.

## Décision

**La porte passe entre le *quoi* et le *comment*.**

### Gratuit — ce que le Scanner montre

Assez pour comprendre le problème, mesurer le bénéfice et juger du risque :

```
DLL bloquée par Windows
  Ce fichier ne peut pas se charger. La table ne démarrera pas.
  ✓ Réparable automatiquement
  ✓ Sauvegarde avant modification
  ✓ Réversible
  ⏱ Quelques secondes
  [ Réparer ]  🔒 Repair
```

Portés par `RepairPlanItem.Summary` : nombre d'écritures, natures d'écriture, réversibilité, sauvegarde prévue, durée estimée.

### Payant — ce que Repair ajoute

Les **chemins exacts**, les **valeurs avant → après**, l'**ordre des opérations**, le **playbook détaillé**. C'est-à-dire tout ce qui permettrait de reproduire la réparation à la main.

Visible **à l'achat, juste avant l'exécution** : l'acheteur valide en connaissance de cause, avec le dry-run complet sous les yeux.

### Deux garde-fous non négociables

**1. Le résumé est *calculé*, jamais *déclaré*.** `RepairSummary.From()` le dérive du plan réellement calculé. « Réversible » est vrai parce que chaque changement l'est, pas parce qu'une règle du pack le prétend — et si une règle prétend une réversibilité que l'action ne sait pas fournir, **l'action gagne**. Sans cette contrainte, « réversible » et « sauvegarde préalable » deviendraient des arguments marketing, et l'argument de confiance s'effondrerait.

**2. Ce qu'on ne sait PAS faire reste gratuit et visible.** `Completeness.Partial`, la liste `Missing[]` et les **procédures manuelles** ne sont jamais masquées. Cacher une limitation n'est pas protéger de la valeur, c'est survendre — et ces étapes-là, Repair ne les automatisera jamais (ADR-004). On ne peut pas faire payer ce qu'on refuse de faire.

### Où la coupe est implémentée

**Dans le moteur, pas dans l'UI.** `RepairEngine.Plan(..., licensed: false)` renvoie des items avec `Changes` vide et `Summary` rempli. Aucun bug d'interface ne peut faire fuiter le détail, parce qu'il n'a jamais quitté le moteur.

## Alternatives écartées

- **Dry-run complet gratuit** (ma proposition initiale) — meilleur argument de confiance possible, mais le risque commercial est réel et Maxime a eu raison de le refuser : sur une niche technique, une part non négligeable des utilisateurs sait lire un plan et l'appliquer.
- **Tout cacher derrière un cadenas** — fabrique une frustration artificielle sur un calcul déjà fait, et contredit « le gratuit n'est jamais bridé pour forcer l'achat ».
- **Résumé limité aux N premiers findings** — le pire des deux mondes : bridage visible *et* protection faible.

## Conséquences

- La porte de licence porte sur le **détail du plan**, `Apply`, `Backup` et `Undo`. Jamais sur le diagnostic, jamais sur le résumé.
- L'argumentaire de vente devient : *« Tu sais ce qui ne va pas et qu'on sait le réparer. Repair fait le travail, avec sauvegarde, journal et annulation. »* On vend l'exécution fiable, pas le savoir.
- Le parcours d'achat doit afficher le **plan complet avant l'exécution**, sinon l'acheteur valide à l'aveugle — ce qui violerait le contrat de sûreté.
- **Verrouillé par cinq tests** : `Test_Plan_WithoutLicence_DetailIsWithheld`, `…_SummaryIsStillShown`, `…_PartialityIsStillDisclosed`, `Test_Summary_IsDerivedFromTheRealPlanNotDeclared`, `Test_Plan_WithLicence_DetailIsRestored`. Un futur contributeur qui rouvrirait la vanne casserait la suite.

## À surveiller

Si le catalogue de correctifs reste dominé par des actions triviales (« supprimer un marqueur »), le résumé seul suffira à deviner le geste, et la protection sera faible en pratique. Ce serait le signal que la valeur doit se déplacer vers les **playbooks ordonnés multi-étapes**, qu'on ne refait pas à la main sans se tromper. À réexaminer après les premiers retours terrain.
