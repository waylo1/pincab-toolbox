# Annonce — nouvelle version + Repair

**À faire AVANT de poster :** `build.cmd` sur Windows, republier le zip, vérifier le numéro de version.
Les posts ci-dessous disent « nouvelle version » sans numéro — remplace par le tien.

**À vérifier :** je cite Chad et Gregg par leur prénom (ils ont posté publiquement). Coupe si tu préfères.

**Le point le plus important de tout ça** — la demande de re-scan. On n'annonce pas « les faux
criticals sont corrigés », on annonce « une cause de plus est corrigée, re-scannez et dites-moi ».
Si quelqu'un re-scanne et voit encore un faux critical, la première version te fait passer pour un
menteur, la seconde te fait passer pour quelqu'un de sérieux. Et surtout : ça te ramène la donnée
dont tu as besoin.

---
---

## 1 · Facebook (groupes VPin) — ANGLAIS

**Pincab Toolbox / FlipSync — new build**

Almost everything in this one came straight from your reports.

**Original and homebrew tables flagged as "ROM missing".** I found another cause. A lot of originals
are built from a ROM-table template where the VPinMAME lines are commented out rather than deleted —
and the scanner was reading those comments as real code. So a table that drives no ROM at all looked
like a table with a missing ROM. Fixed.

**If you had originals flagged critical, please re-scan and tell me whether any are left.** That is
by far the most useful thing you can send me right now. Gregg, this probably covers a good part of
your list — worth a re-run before we dig further.

**Mods and variants are no longer reported as outdated.** Anything tagged MOD, Bigus mods and the
like. They version independently of the base table, so comparing the two never made sense. Thanks
Chad for pushing on that one.

**Big collections finally produce a readable report.** It used to be thousands of lines. Repetitive
results are now grouped into a single counted line. Criticals are never grouped — if 12 tables are
broken you get 12 lines, not "12 problems". A broken cab should look broken.

**Three new checks:** PinUpDisplay left running after you close a table (the one you have to kill in
Task Manager before the next launch), an incomplete screen setup, and orphaned media files in
PinupSystem.

Still free, still read-only, still no telemetry, still nothing leaves your machine.

---

**What I'm working on next: Repair**

The scanner tells you what is wrong. Repair is the part that fixes it, locally, on your machine.

These are constraints already built into the engine, not promises:

- Nothing happens unless you ask. Every fix is shown to you in full before anything is written.
- A backup is taken before the change, and every fix can be undone.
- Everything is logged, so you can see exactly what was touched.
- **It will never download a ROM or a table.** That is not a technical limitation, it is a line I
  will not cross.

The scan stays free — including a summary of what Repair could fix on your install: how many things,
whether they are reversible, roughly how long. Repair itself will be paid. I would rather say that
now, before it exists, than surprise anyone later.

What already works in the engine (not in the interface yet): unblocking DLLs blocked by Windows,
re-zipping an extracted ROM folder, killing a zombie PinUpDisplay, quarantining orphaned Popper
media (moved, never deleted), and setting the default audio device.

**So — what would you actually want in it?** What is the thing you fix by hand every single time,
that you would pay to never do again? That is what I want to build. No date promised.

---
---

## 2 · Facebook (groupes VPin) — FRANÇAIS

**Pincab Toolbox / FlipSync — nouvelle version**

Presque tout vient directement de vos retours.

**Tables originales et homebrew signalées « ROM manquante ».** J'ai trouvé une cause de plus.
Beaucoup d'originales sont construites à partir d'un template de table à ROM où les lignes VPinMAME
sont commentées au lieu d'être supprimées — et le scanner lisait ces commentaires comme du vrai code.
Résultat : une table qui ne pilote aucune ROM ressemblait à une table à qui il manque sa ROM. Corrigé.

**Si vous aviez des originales en critique, relancez un scan et dites-moi s'il en reste.** C'est de
loin ce que vous pouvez m'envoyer de plus utile en ce moment.

**Les mods et variantes ne sont plus signalés comme périmés.** Tout ce qui est tagué MOD, les mods
Bigus et compagnie. Ils suivent leur propre versionnage, donc les comparer à la table de base n'avait
aucun sens.

**Les grosses collections donnent enfin un rapport lisible.** C'était des milliers de lignes. Les
résultats répétitifs sont maintenant regroupés en une seule ligne comptée. Les criticals ne sont
jamais regroupés : si 12 tables sont cassées, vous voyez 12 lignes, pas « 12 problèmes ». Un cab
cassé doit avoir l'air cassé.

**Trois nouveaux contrôles :** PinUpDisplay resté actif après la fermeture d'une table (celui qu'il
faut tuer au Gestionnaire des tâches avant de relancer), une configuration d'écrans incomplète, et
les fichiers média orphelins dans PinupSystem.

Toujours gratuit, toujours en lecture seule, toujours zéro télémétrie, rien ne sort de votre machine.

---

**Ce sur quoi je travaille maintenant : Repair**

Le scanner dit ce qui ne va pas. Repair, c'est la partie qui le répare — en local, sur votre machine.

Ce sont des contraintes déjà codées dans le moteur, pas des promesses :

- Rien ne se fait sans que vous le demandiez. Chaque correctif vous est montré en entier avant
  la moindre écriture.
- Une sauvegarde est prise avant la modification, et chaque correctif est annulable.
- Tout est journalisé, vous voyez exactement ce qui a été touché.
- **Il ne téléchargera jamais une ROM ni une table.** Ce n'est pas une limite technique, c'est une
  ligne que je ne franchirai pas.

Le scan reste gratuit — y compris le résumé de ce que Repair pourrait corriger chez vous : combien de
choses, si c'est réversible, à peu près combien de temps. Repair lui-même sera payant. Je préfère le
dire maintenant, avant même que ça existe, plutôt que de surprendre quelqu'un plus tard.

Ce qui marche déjà dans le moteur (pas encore dans l'interface) : débloquer les DLL bloquées par
Windows, re-zipper un dossier ROM décompressé, tuer un PinUpDisplay zombie, mettre en quarantaine les
médias Popper orphelins (déplacés, jamais supprimés), et définir le périphérique audio par défaut.

**Du coup : vous voudriez quoi dedans ?** C'est quoi le truc que vous réparez à la main à chaque
fois, et que vous paieriez pour ne plus jamais refaire ? C'est ça que je veux construire.
Aucune date promise.

---
---

## 3 · Pincab Passion (forum FR) — version longue

**[MAJ] Pincab Toolbox / FlipSync — nouvelle version : correctifs issus de vos retours + où en est Repair**

Bonjour à tous,

Petit point sur l'outil, deux semaines après le lancement ici. Cette version ne contient quasiment
que des correctifs venus de retours d'utilisateurs — c'est exactement comme ça que je voulais que ça
se passe, alors merci à ceux qui ont pris le temps de m'écrire, y compris pour me dire que ça ne
marchait pas.

### Ce qui est corrigé

**Faux « ROM manquante » sur les tables originales et homebrew.** C'était le faux positif le plus
gênant, parce qu'il touchait précisément ce que l'outil prétend savoir reconnaître. Une première
cause avait déjà été corrigée fin juillet. J'en ai trouvé une deuxième : beaucoup de tables
originales sont construites à partir d'un template de table à ROM, et les lignes VPinMAME y sont
**commentées** plutôt que supprimées. Le scanner lisait ces commentaires comme du code actif, donc
une table qui ne pilote aucune ROM ressemblait à une table à qui il manque sa ROM.

**Si vous aviez des originales remontées en critique, relancez un scan et dites-moi ce qu'il reste.**
Je préfère demander une vérification que d'annoncer que c'est réglé — tant que personne ne l'a
reconfirmé sur sa propre install, je ne considère pas le sujet clos.

**Les mods et variantes ne sont plus signalés « une version plus récente existe ».** Un mod porte le
nom et l'année de la table de base mais suit son propre versionnage : la comparaison ne voulait rien
dire et produisait une mise à jour fantôme.

**Rapport lisible sur les grosses collections.** Sur une bibliothèque de plusieurs milliers de
tables, le rapport faisait des milliers de lignes et le peu qui comptait s'y noyait. Les résultats
répétitifs sont maintenant regroupés en une ligne comptée. En revanche **les criticals ne sont jamais
regroupés** : si douze tables ne démarrent pas, vous voyez les douze noms. Regrouper ça derrière une
ligne propre reviendrait à cacher le problème, et je me suis assez battu contre le score trompeur de
la version précédente pour ne pas refaire la même erreur dans l'autre sens.

**Trois nouveaux contrôles**, tous issus de sujets lus ici ou sur VPForums :

- **PinUpDisplay resté actif** après la fermeture d'une table — celui qu'il faut aller tuer au
  Gestionnaire des tâches avant de pouvoir relancer.
- **Configuration d'écrans incomplète** — un composant backglass ou DMD installé alors que moins de
  deux écrans sont connectés. C'est volontairement un signal de *nombre*, pas d'*ordre* : le sujet
  « changer l'ordre des écrans » est le plus commenté de cette section, je l'ai lu en entier, et la
  correction elle-même passe par des clés de registre et du débranchement physique. Ça sort de ce que
  je m'autorise à automatiser sur la machine de quelqu'un d'autre.
- **Fichiers média orphelins dans PinupSystem.** Là aussi j'ai lu le sujet, y compris le message de
  celui à qui un script communautaire a supprimé des vidéos encore utilisées. L'outil ne fait que
  **signaler**, et le futur correctif déplacera en quarantaine — jamais de suppression.

Trois avertissements qui s'affichaient sans aucune explication ont maintenant leur impact et leur
cause détaillés.

Toujours gratuit, toujours en lecture seule, aucune télémétrie, rien qui sort de votre machine.

### Où en est Repair

Le scanner dit ce qui ne va pas. Repair est la partie qui le répare, en local.

Le moteur existe et est testé. Ce qui suit n'est pas une liste d'intentions, c'est ce qui est
déjà codé dedans :

- **Rien ne part sans votre accord.** Chaque correctif vous est montré en entier — quel fichier,
  quelle valeur avant, quelle valeur après — avant la moindre écriture.
- **Sauvegarde avant modification, et annulation possible.** Une action qui ne peut pas être annulée
  n'est jamais proposée en automatique, quelle que soit la confiance qu'on a dedans.
- **Journal complet** de ce qui a été touché.
- **Aucun téléchargement de ROM ni de table. Jamais.** Ce n'est pas une limite technique, c'est une
  ligne que je ne franchirai pas, et ça veut dire que Repair ne résoudra jamais une ROM réellement
  manquante. Autant que ce soit clair tout de suite.

**Sur le prix, pour ne pas laisser de flou :** le scan reste gratuit, y compris un résumé de ce que
Repair pourrait corriger chez vous — combien de choses, si c'est réversible, si c'est sauvegardé,
à peu près combien de temps. Repair lui-même sera payant. Je préfère le dire maintenant, avant même
que ça soit disponible, plutôt que de le sortir au dernier moment.

Ce qui fonctionne déjà dans le moteur, pas encore branché dans l'interface : débloquer les DLL
bloquées par Windows (le fameux « Débloquer » dans les propriétés du fichier), re-zipper un dossier
ROM décompressé, tuer un PinUpDisplay zombie, mettre en quarantaine les médias Popper orphelins, et
définir le périphérique audio par défaut.

### Et c'est là que j'ai besoin de vous

**Qu'est-ce que vous voudriez y trouver ?**

Concrètement : quel est le truc que vous réparez à la main à chaque fois, que vous connaissez par
cœur, et dont vous en avez assez ? C'est ça que je veux mettre dedans en priorité, plutôt que ce que
moi je trouve intéressant à coder.

Deux choses m'aident particulièrement : le message d'erreur exact quand il y en a un, et si c'est
arrivé plus d'une fois. Je ne code un contrôle que quand deux personnes ou deux sources différentes
décrivent le même problème — c'est ce qui m'évite d'ajouter un faux positif de plus.

Aucune date promise. Merci à tous pour l'accueil.

---
---

## 4 · VPForums / Pinball Nirvana — version longue, ANGLAIS

**[Update] Pincab Toolbox / FlipSync — fixes from your reports, and where Repair stands**

Short version: new build out, almost entirely fixes that came from user reports. And I want to
describe what I am building next, because I would rather get told now that I am building the wrong
thing.

### Fixed

**False "ROM missing / critical" on original and homebrew tables.** This was the worst false positive
the tool had, because it hit exactly what the tool claims to recognise. One cause was fixed in late
July. I found a second one: a lot of original tables are built from a ROM-table template where the
VPinMAME lines are **commented out** rather than deleted, and the scanner was reading those comments
as live code. A table driving no ROM at all therefore looked like a table with a missing ROM.

**If you had originals flagged critical, please re-scan and tell me what is left.** I would rather
ask for verification than announce it is solved — until somebody confirms it on their own install,
I do not consider this closed.

**Mods and variants are no longer reported as outdated.** A mod carries the base table's name and
year but versions independently, so the comparison was meaningless and produced a phantom update.

**Readable reports on large collections.** On a several-thousand-table library the report ran to
thousands of lines and the few that mattered drowned. Repetitive results are now grouped into one
counted line. **Criticals are never grouped** — if twelve tables will not start, you see twelve
names. Collapsing that into one tidy line would hide the problem, and I spent enough effort killing
the previous version's misleading score to avoid making the same mistake in the other direction.

**Three new checks**, all from threads on here and on the French pincab forums:

- **PinUpDisplay still running** after a table closes — the one you have to kill in Task Manager
  before the next launch works.
- **Incomplete screen setup** — a backglass or DMD component installed with fewer than two displays
  connected. Deliberately a *count* signal, not an *order* one: fixing screen order means registry
  keys and physically unplugging monitors, which is well outside what I am willing to automate on
  somebody else's machine.
- **Orphaned media files in PinupSystem** — detection only, and the future fix will move files to
  quarantine, never delete them. That is a direct consequence of reading about a community script
  that deleted videos which were still in use.

Three warnings that previously displayed with no explanation now carry their impact and cause.

Still free, still read-only, no telemetry, nothing leaves your machine.

### Where Repair stands

The scanner tells you what is wrong. Repair is the part that fixes it, locally.

The engine exists and is tested. What follows is not a list of intentions — it is what is already
built into it:

- **Nothing runs without your say-so.** Every fix is shown in full — which file, value before, value
  after — before anything is written.
- **Backup before the change, and undo afterwards.** An action that cannot be undone is never
  offered as automatic, no matter how confident the rule behind it is.
- **Full journal** of what was touched.
- **No ROM or table downloads. Ever.** Not a technical limitation, a line I will not cross — which
  also means Repair will never solve a genuinely missing ROM. Better said up front.

**On pricing, so there is no ambiguity:** the scan stays free, including a summary of what Repair
could fix on your install — how many items, whether they are reversible, whether they are backed up,
roughly how long. Repair itself will be paid. I would rather say that now, before it ships, than
spring it on anyone later.

Already working in the engine, not yet wired into the interface: unblocking DLLs blocked by Windows,
re-zipping an extracted ROM folder, killing a zombie PinUpDisplay, quarantining orphaned Popper
media, and setting the default audio device.

### What I actually want from this thread

**What would you want in it?**

Specifically: what is the thing you fix by hand every single time, that you know by heart, and are
tired of? That is what I want to prioritise, rather than whatever I happen to find interesting to
write.

Two things help most: the exact error message when there is one, and whether it has happened more
than once. I only build a check when two people or two separate sources describe the same problem —
that is what keeps me from shipping yet another false positive.

No dates promised. Thanks for the reception so far.
