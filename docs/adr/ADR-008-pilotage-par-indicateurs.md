# ADR-008 — Pilotage par indicateurs (roadmap sous condition de KPI)

**Statut** : ✅ **Accepté** · **Date** : 27/07/2026 · **Décideur** : Maxime Chauvin

---

## Contexte

Le projet a atteint une bonne maturité produit, et la tentation à ce stade est de piloter la suite « à l'impression » : ajouter une fonction parce qu'elle semble bonne, coder l'UI Repair parce qu'elle est prête, empiler des checks parce qu'on sait les faire. `ARCHITECTURE-KnowledgeEngine.md §2` posait déjà une métrique de fond (le % de pannes réellement expliquées), et `PROJECT-BRAIN §8` liste des risques, mais rien n'obligeait un chantier à **se justifier par un indicateur ou un risque** avant d'entrer dans la roadmap.

Sans cette discipline, deux dérives : on construit dans le vide (le « 186 checks avant d'avoir des utilisateurs » que l'architecture met déjà en garde de ne pas faire), et on ne sait jamais, après coup, si un effort a servi.

## Décision

**Tout chantier important doit répondre explicitement à une question avant d'entrer dans la roadmap :**

> *Quel KPI cette évolution est-elle censée améliorer, ou quel risque identifié réduit-elle ?*

**Si un chantier n'améliore aucun indicateur mesurable ET ne réduit aucun risque documenté (`PROJECT-BRAIN §8`), il est remis en question** — il retourne au parking jusqu'à ce qu'une des deux lignes puisse être remplie.

Le tableau de bord des indicateurs vit dans **`SUCCESS-METRICS.md`** (document vivant), organisé par phase (Validation du Scanner → de l'intérêt Repair → commerciale). Gabarit de justification d'un chantier :

```
Chantier :        …
KPI visé :        … (lequel, dans quel sens)  — OU —
Risque réduit :   … (lequel, dans Brain §8)
Preuve de succès: comment on saura, chiffres à l'appui
Coût estimé :     …
```

**Deux précisions qui font que la règle ne se retourne pas contre la confiance :**

1. **La réduction de risque est une justification valide à part entière.** Certains chantiers essentiels n'améliorent aucun compteur — signer le code (réduit la friction SmartScreen / la défiance), mettre à jour les CGV avant la première vente (risque légal). La règle les autorise via la ligne « Risque réduit », pas via un KPI forcé.

2. **La mesure des KPI obéit elle-même aux règles absolues.** Aucun indicateur ne justifie d'ajouter de la télémétrie ou de brider le gratuit (règle n°2, ADR-004). Un KPI non mesurable sans trahir la confiance reste non mesuré, ou passe par un proxy / un opt-in explicite — jamais par une instrumentation silencieuse. `SUCCESS-METRICS.md §2` détaille les sources autorisées.

## Alternatives écartées

- **Ne rien formaliser, garder la métrique de l'architecture comme simple intention** — insuffisant : une intention non opposable ne bloque aucun chantier faible.
- **Exiger un KPI chiffré pour *tout*** — casserait les chantiers de sûreté/légaux qui réduisent un risque sans bouger un compteur. D'où la double porte KPI **ou** risque.
- **Des cibles chiffrées dès maintenant** — reviendrait à inventer des nombres (le piège récurrent du `Brain §9`). Les cibles se fixent sur la baseline réelle, phase par phase.

## Conséquences

- Toute proposition de chantier passe le gabarit ci-dessus. La question « on le fait ? » se tranche sur données ou sur risque, plus sur impression.
- `SUCCESS-METRICS.md` devient un document de référence au même titre que les ADR et le Brain, et doit être tenu à jour (sa propre règle de maintenance s'applique).
- Le lancement du Scanner **est** la Phase 1 de ce dispositif : sa fonction première est d'établir la baseline qui rendra les phases suivantes pilotables.
- Cohérent avec ADR-002 : la « santé du renouvellement » (le pack continue-t-il de s'enrichir ?) devient un KPI explicite, pas une clause de style.

## À surveiller

Le risque inverse d'un pilotage par KPI est le **court-termisme** : privilégier ce qui bouge un compteur vite (vanity metrics) au détriment de l'étoile polaire (pannes correctement expliquées) et de la confiance. Garde-fou : l'étoile polaire prime sur tous les compteurs, et aucun KPI ne peut justifier de franchir une règle absolue. À réexaminer si un jour un indicateur pousse à une décision qui abîmerait la confiance — dans ce cas, c'est l'indicateur qu'on corrige, pas la confiance.
