# ADR-010 — Dégel du Scanner et doctrine `Severity.Note`

**Statut** : ✅ **Accepté** · **Décidé le** : 05/08/2026 (Maxime Chauvin, « je sonne le dégel du gel ») · **Formalisé le** : 06/08/2026 (session autonome Sonnet 5, sur consigne R2 du handoff)
**Supersède** : la fermeture « 🔒 SCANNER CLOS » du 03/08/2026 — décision réelle mais jamais montée en ADR à l'époque, documentée seulement dans `TRANSMISSION.md`. `PROJECT-BRAIN §7` (« Ne pas ajouter de nouveaux checks avant le lancement ») est mis à jour en conséquence.

---

## Contexte

Le 03/08/2026, le Scanner a été volontairement clos : 12 scanners câblés, 128 tests verts, et surtout une règle d'entrée stricte — **aucun nouveau check sans deux signaux terrain indépendants** (deux utilisateurs, ou deux forums distincts). Cette règle existait pour une raison concrète, pas par prudence gratuite : la fausse alerte KPI#1 du 03/08 avait montré qu'un check codé sur une hypothèse non confirmée peut produire un faux positif, et **un seul faux positif public tue la conversion** (`PROJECT-BRAIN §8`). Le gel a ensuite tenu pendant que l'effort portait sur Repair (licence, moteur, UI Écran 1).

Le 05/08, un audit fonctionnel complet du Scanner (`docs/AUDIT-Scanner-2026-08.md`, ancré code réel des 12 scanners + FIELD-LOG + corroboration terrain, pas une recherche web seule) a identifié **6 catégories de pannes réelles non détectées** : scripts partagés (core.vbs), topologie d'affichage réelle, colorisation/altsound, état audio, résidus Freezy, hygiène système FR. Maxime a alors tranché : « je sonne le dégel du gel ». La question que cet ADR formalise est *comment* rouvrir sans reproduire le mécanisme exact de KPI#1.

## Décision

**Le dégel lève le gel de calendrier. Il ne lève PAS la règle anti-faux-positif — il lui donne un deuxième chemin.**

Chaque nouveau check entre désormais par une des deux portes suivantes, jamais par une troisième :

1. **🟢 Déterministe (FP nul par construction, démontrable par le test)** → construit et **activé directement**, sévérité `Warning`, sans attendre de signal terrain. La preuve de FP nul (biais silence sur tout cas ambigu/illisible, discipline anti-devinette) remplace le signal terrain comme barrière d'entrée. C'est le mécanisme qui a produit la file Tier A du 06/08 (8 scanners, voir « Conséquences »).
2. **🟡 Heuristique (jugement possible, FP non démontrable à zéro)** → passe obligatoirement par le nouveau palier de sévérité **`Severity.Note`** (ajouté entre `Info` et `Warning`, Core livré et vert le 05/08) avant tout ship, selon 5 règles fixes (« Doctrine Note », `docs/HANDOFF-Sonnet5-scanners-2026-08.md` §Doctrine) :
   - Sévérité `Note` uniquement, jamais `Warning` directement — `Note` ne bouge **jamais** le score (`ScanReport.Score` reste défini sur Critical/Warning seuls, invariant verrouillé par `Test_Note_NeverMovesScore`) et ne déclenche jamais la bannière « FIX THIS FIRST ». C'est le levier structurel : même si l'heuristique se trompe, elle ne peut pas reproduire le désastre du 30/07 (un Warning qui plombe la note d'un scan sain).
   - Énoncer le **fait observé**, jamais le **verdict** (« sortie audio par défaut = HDMI ‹X› », pas « ton audio est cassé »).
   - Escalade en `Warning` uniquement sur une **sous-condition déterministe** explicite (ex. comparaison de versions).
   - Un finding résumé par table/lot, jamais une ligne par occurrence (patron `POPPER_MEDIA_MISSING`).
   - Biais silence sur tout échec de lecture/parse — inchangé du reste du Scanner.

   Cette doctrine **remplace** la barre « deux signaux terrain indépendants » pour les checks heuristiques : la preuve n'est plus « le terrain a confirmé le problème », c'est « la sévérité ne peut structurellement pas nuire même si l'heuristique se trompe ».

3. **Irréductibles, hors des deux portes, hors périmètre même sous cette doctrine** : **F3 quote-safety** (trop de formes valides en VBScript, FP même en `Note`) et le ***fix*** Repair pour `core.vbs` (question ADR distincte sur le statut OSS du script partagé — seule sa *détection* passe en `Note`, jamais la correction automatique).

**Ce qui ne change pas** : les stops nets R3 du handoff — aucun des scanners existants n'est jamais modifié (uniquement des fichiers neufs + une ligne `.Add` de composition) ; l'Écran 2 / bouton Apply de Repair reste hors périmètre Scanner ; le fix `B2S_ORPHAN` et la fonctionnalité auto-update restent des chantiers séparés, non débloqués par cet ADR.

## Alternatives écartées

- **Garder le gel jusqu'à un vrai signal terrain sur chaque nouvelle catégorie** — écarté : l'audit du 05/08 montre que plusieurs trous (ex. topologie d'affichage) sont des pannes structurellement certaines dès qu'on a les bonnes données (coordonnées hors de l'union des écrans = fait, pas une supposition) ; attendre un signal terrain pour un fait déjà démontrable retarde de la valeur sans réduire le risque.
- **Rouvrir sans distinction 🟢/🟡** — écarté : c'est exactement le chemin qui a produit KPI#1. La distinction déterministe/heuristique est ce qui rend le dégel sûr.
- **Un palier `Note` qui compte dans le score mais faiblement** — écarté (choix de Maxime, 05/08) : dès qu'un palier bouge le score, même peu, la pression à le traiter comme un Warning déguisé revient ; `Note` reste strictement score-neutre pour rester un fait, pas un jugement dilué.

## Conséquences

- **File Tier A livrée le 06/08** (session autonome Sonnet 5, seule, sur `docs/HANDOFF-Sonnet5-scanners-2026-08.md`) : 8 checks 🟢 déterministes construits et activés — `VPMALIAS_LOOP`, `NVRAM_EMPTY`, `ALTCOLOR_INCOMPLETE`, `ALTSOUND_SAMPLE_MISSING`, `DISPLAY_OFFSCREEN`, `BROKEN_JUNCTION`, `B2S_MALFORMED`, `POPPER_ORPHAN_PLAYLIST`. Scanner passe de 13 à **21 scanners actifs**. Core 144→279/279, Repair 105/105 stable, Debug ET Release à chaque étape, aucun scanner existant modifié. Détail complet : `knowledge/FIELD-LOG.md`, entrée du 06/08.
- **File Tier B (checks 🟡, doctrine Note) reste ouverte, non attaquée le 06/08** (arrêt de cadrage sur consigne Maxime en cours de session, pas un blocage technique) — le prérequis (rendu App du palier `Note` : libellé FR/EN, couleur, 6 exports) est déjà livré, donc le premier check Tier B d'une session future n'a plus de prérequis à lever.
- `PROJECT-BRAIN §7` est mis à jour : la ligne « Ne pas ajouter de nouveaux checks avant le lancement » est marquée supersédée pour les checks 🟢, avec renvoi vers cet ADR.
- La règle « deux signaux terrain indépendants » (`FIELD-LOG §1`) **reste pleinement en vigueur** pour tout ce qui n'entre dans aucune des deux portes ci-dessus — cet ADR ouvre deux chemins d'entrée supplémentaires, il n'abolit pas la règle de base.

## À surveiller

- **Dérive de la doctrine Note** : si un futur check `Note` énonce un jugement plutôt qu'un fait (règle 2), le risque KPI#1 réapparaît sous un autre nom malgré la neutralité de score — la revue de code d'un nouveau `Note` doit relire le libellé exact (`EnglishText`/`FrFindings`) avant merge, pas seulement vérifier la sévérité.
- **Escalade Note→Warning non déterministe** : toute escalade doit rester sur une comparaison de fait vérifiable (règle 3) — une escalade fondée sur un seuil « qui semble raisonnable » romprait la garantie de cet ADR.
