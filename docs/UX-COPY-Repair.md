# Copie UX — les 4 écrans critiques de Repair

FR + EN · 25/07/2026 · à relire avant implémentation de l'UI

> Repair est le moment où on écrit sur l'installation de quelqu'un qui a peur qu'on la casse.
> **La copie n'est pas de la décoration ici : c'est le principal véhicule de la confiance.**
> Un écran mal formulé annule le travail du moteur.

---

## Principes de ton, propres à ce produit

**Ne jamais rassurer à vide.** « Pas d'inquiétude ! » augmente l'inquiétude. On ne rassure pas
avec un adjectif, on rassure avec un fait : *ce qui a été sauvegardé, où, et comment revenir en arrière.*

**Ne jamais blâmer l'utilisateur.** Ni « vous avez mal installé », ni « fichier corrompu par
l'utilisateur ». La cause est décrite comme un événement, pas comme une faute.

**Pas d'humour, pas d'emoji, pas de « Oups ».** Le registre est celui d'un outil de diagnostic
sérieux. Un « Oups ! » devant quelqu'un dont l'installation vient de casser est insultant.

**Le mot « sauvegarde » est un engagement.** On ne l'écrit que quand elle existe réellement,
et on donne toujours son chemin.

**Deux niveaux partout** : la phrase joueur d'abord, le détail technique derrière
`Afficher les détails ▾`. Jamais l'inverse.

**Vocabulaire figé** — même mot pour la même chose, partout :

| Concept | FR | EN | À bannir |
|---|---|---|---|
| L'aperçu | **aperçu** | **preview** | simulation, dry-run (en UI) |
| L'écriture | **appliquer** | **apply** | exécuter, lancer, forcer |
| Le retour arrière volontaire | **annuler** | **undo** | restaurer, rollback |
| Le retour arrière automatique après échec | **remis en état** | **rolled back** | annulé |
| La copie de sécurité | **sauvegarde** | **backup** | copie, archive |

---

## Écran 1 — Réparation disponible (avant achat)

**État utilisateur** : curieux et méfiant. Il veut savoir si l'outil peut régler son problème,
et à quel risque. C'est l'écran qui vend Repair.

**Ce qu'il montre, et pourquoi** (ADR-006) : assez pour comprendre le problème et juger du risque —
**jamais** le chemin, la valeur avant/après, ni l'ordre des opérations. On vend l'exécution fiable,
pas le mode d'emploi.

### Sans licence

> **DLL bloquée par Windows**
> Ce fichier ne peut pas se charger. La table ne démarrera pas tant qu'il est bloqué.
>
> ✓ Réparable automatiquement
> ✓ Sauvegarde avant modification
> ✓ Réversible — annulable en un clic
> ⏱ Quelques secondes
>
> `[ Réparer ]` 🔒 **Repair**
> Repair applique la réparation, la sauvegarde et permet de l'annuler.

### Avec licence — le détail apparaît, juste avant d'exécuter

> **Ce qui va changer — 1 fichier**
> `…\VPinMAME\VPinMAME.dll`
> avant : bloqué par Windows → après : débloqué
>
> `[ Appliquer ]`  `[ Annuler ]`

**EN**

> **DLL blocked by Windows** — This file cannot load. The table will not start while it stays blocked.
> ✓ Fixable automatically · ✓ Backed up before changing · ✓ Reversible — one click to undo · ⏱ A few seconds
> `[ Fix ]` 🔒 **Repair** — Repair applies the fix, backs it up, and lets you undo it.

### Trois règles pour cet écran

**La phrase de verrouillage dit ce que Repair *ajoute*, jamais ce qu'il *cache*.** « Débloquez Repair
pour voir le problème » est interdit : le diagnostic est gratuit et le restera.

**Les quatre propriétés sont des faits calculés, pas des promesses.** Elles viennent de
`RepairPlanItem.Summary`, dérivé du plan réel. Ne jamais les écrire en dur dans l'UI.

**Ce qu'on ne sait pas faire s'affiche gratuitement.** Un playbook partiel annonce
« 1 étape sur 3 est automatique » **sans licence**. Cacher une limitation, c'est survendre.

## Écran 2 — Confirmation avant application

**État utilisateur** : sur le point de laisser un programme modifier son installation. C'est
l'instant de l'hésitation.

> **Appliquer 3 correctifs ?**
>
> Une sauvegarde des 4 fichiers concernés sera créée avant toute modification.
> Tu pourras tout annuler d'un clic.
>
> · Débloquer VPinMAME.dll
> · Remettre la ROM `mm_109c` sous forme d'archive
> · Ajouter « Attack From Mars » au menu
>
> `[ Appliquer les 3 correctifs ]`   `[ Revoir l'aperçu ]`

### Variante — au moins un correctif est irréversible

> ⚠ **Un de ces correctifs ne peut pas être annulé**
>
> « *Nom du correctif* » modifie un fichier qui ne pourra pas être remis dans son état
> d'origine automatiquement. La sauvegarde reste disponible et tu pourras restaurer à la main.
>
> ☐ J'ai compris que ce correctif est irréversible
>
> `[ Appliquer ]` *(désactivé tant que la case n'est pas cochée)*

**EN**

> **Apply 3 fixes?** — A backup of the 4 files involved will be created before anything changes.
> You can undo all of it in one click.
>
> ⚠ **One of these fixes cannot be undone** — "*fix name*" changes a file that cannot be restored
> automatically. The backup stays available and you can restore it by hand.
> ☐ I understand this fix is irreversible

> **Règle** : le bouton porte l'action et son nombre — « Appliquer les 3 correctifs », jamais « OK ».
> Le bouton d'annulation dit ce qu'il ramène — « Revoir l'aperçu », jamais « Annuler », qui
> entrerait en collision avec l'annulation d'un correctif.

---

## Écran 3 — Blocage au préflight

**État utilisateur** : il a cliqué, il s'attend à ce que ça se fasse. On lui dit non. Il faut
que le refus se transforme en action en une seule lecture.

### VPX en cours d'exécution

> **Ferme Visual Pinball avant de continuer**
>
> Modifier des fichiers pendant que le jeu tourne peut les corrompre. Rien n'a été touché.
>
> Encore ouvert : **VPinballX**, **PinUP Player**
>
> `[ Revérifier ]`

### Espace disque insuffisant

> **Pas assez de place pour la sauvegarde**
>
> Il faut 240 Mo pour sauvegarder les fichiers concernés ; il en reste 61 sur `C:`.
> Repair ne modifie rien tant qu'il ne peut pas sauvegarder d'abord.
>
> `[ Revérifier ]`

### Un problème a disparu depuis le scan

> **1 correctif retiré**
> « Débloquer dmddevice64.dll » n'est plus nécessaire : le fichier n'est plus bloqué.
> Les 2 autres correctifs sont prêts.

**EN**

> **Close Visual Pinball before continuing** — Changing files while the game is running can corrupt
> them. Nothing has been touched. Still open: **VPinballX**, **PinUP Player**. `[ Check again ]`
>
> **Not enough space for the backup** — 240 MB are needed to back up the files involved; 61 MB are
> free on `C:`. Repair changes nothing until it can back up first.
>
> **1 fix removed** — "Unblock dmddevice64.dll" is no longer needed: the file is not blocked anymore.

> **Règle** : « Rien n'a été touché » est la phrase la plus importante de cet écran. Elle passe
> avant l'explication, parce que c'est la première question que se pose l'utilisateur.

---

## Écran 4 — Récupération *(le plus important du produit)*

**État utilisateur** : quelque chose a échoué, **et la remise en état a échoué aussi**. Il est
inquiet, peut-être en colère. C'est l'écran qui décide s'il nous fait encore confiance demain.

**Ce qu'il ne faut surtout pas faire** : s'excuser longuement, employer un ton léger, dire
« contacte le support » comme seule issue — une micro-entreprise solo n'a pas de support à 22 h.
La première issue proposée doit être **autonome**.

> ### Ton installation est dans un état intermédiaire
>
> Un correctif a échoué, et la remise en état automatique n'a pas pu aller au bout.
> Repair s'est arrêté immédiatement plutôt que de continuer à modifier des fichiers.
>
> **Ce qui est certain**
> Une sauvegarde complète des fichiers concernés a été créée avant toute modification.
> Rien n'est perdu.
>
> **Ce qui a été modifié**
> ✓ `VPinMAME.dll` — débloqué, puis remis dans son état d'origine
> ✗ `VPinMAME.dll` — remplacé par la version 64-bit, **n'a pas pu être remis en état**
> `[le fichier est verrouillé par un autre programme]`
>
> **Ta sauvegarde**
> `C:\Users\<toi>\AppData\Local\PincabToolbox\backups\2026-07-25_14-32\`
> `[ Ouvrir le dossier ]`
>
> ---
>
> ### Pour revenir à l'état d'avant
>
> **1.** Ferme tous les programmes de flipper — Visual Pinball, PinUP Popper, PinUP Player.
> **2.** `[ Réessayer la restauration ]` ← à tenter en premier, c'est automatique
>
> Si la restauration échoue encore :
> **3.** Ouvre le dossier de sauvegarde ci-dessus.
> **4.** Recopie `VPinMAME.dll` par-dessus `…\VPinMAME\VPinMAME.dll`.
> C'est le seul fichier à remettre.
>
> ---
>
> `[ Copier le rapport ]` — journal complet, chemins anonymisés, prêt à coller sur le forum
> Aide de la communauté : *Pincab Passion* · *VPUniverse*

**EN**

> ### Your installation is in an in-between state
>
> A fix failed, and the automatic rollback could not finish. Repair stopped immediately rather
> than keep changing files.
>
> **What is certain** — A full backup of the files involved was created before anything changed.
> Nothing is lost.
>
> **What changed**
> ✓ `VPinMAME.dll` — unblocked, then restored
> ✗ `VPinMAME.dll` — replaced with the 64-bit version, **could not be restored**
> `[file is locked by another program]`
>
> **Your backup** — `C:\Users\<you>\AppData\Local\PincabToolbox\backups\2026-07-25_14-32\`
>
> ### To get back to where you were
> **1.** Close every pinball program — Visual Pinball, PinUP Popper, PinUP Player.
> **2.** `[ Retry restore ]` ← try this first, it is automatic.
>
> If the restore fails again:
> **3.** Open the backup folder above.
> **4.** Copy `VPinMAME.dll` back over `…\VPinMAME\VPinMAME.dll`. That is the only file to put back.
>
> `[ Copy report ]` — full log, paths anonymised, ready to paste on the forum

### Pourquoi cette formulation

**« état intermédiaire »**, pas « erreur critique » ni « échec ». C'est descriptif et exact.
« Critique » déclenche la panique ; « erreur » suggère un bug de notre part plutôt qu'une situation
à résoudre.

**« Repair s'est arrêté immédiatement plutôt que de continuer »** — on transforme le pire moment
en preuve de sérieux. C'est vrai, c'est une décision de design (§6 du design Repair), et ça se dit.

**« Ce qui est certain » avant « ce qui a été modifié »** — la sauvegarde est la seule information
qui fait baisser le rythme cardiaque. Elle passe en premier. Toujours.

**Un seul fichier nommé dans les étapes manuelles.** Face à une liste de huit fichiers, personne
n'agit. On ne liste que ce qui reste réellement à faire.

**« Copier le rapport » plutôt que « Contacter le support »** — l'utilisateur repart avec quelque
chose d'utile, et le forum est le vrai canal d'aide de cette communauté. Cohérent avec le réflexe
qu'on cherche à installer : *lance l'outil, poste le rapport*.

---

## Micro-copie transverse

| Contexte | FR | EN |
|---|---|---|
| Succès total | **3 correctifs appliqués.** Sauvegarde conservée pendant 30 jours. `[ Annuler ]` | **3 fixes applied.** Backup kept for 30 days. `[ Undo ]` |
| Succès partiel | **2 correctifs sur 3 appliqués.** Le troisième a échoué et a été remis en état. | **2 of 3 fixes applied.** The third failed and was rolled back. |
| Annulation faite | **Tout a été remis comme avant.** | **Everything is back the way it was.** |
| Playbook partiel | **3 étapes sur 4 sont automatiques.** La dernière demande un fichier que nous ne pouvons pas fournir — la procédure est indiquée. | **3 of 4 steps are automatic.** The last one needs a file we cannot supply — the steps are shown. |
| Aucun correctif disponible | **Rien à réparer automatiquement ici.** Ce problème se corrige à la main : voici comment. | **Nothing to fix automatically here.** This one is a manual fix: here's how. |
| Pendant l'application | **Sauvegarde en cours…** puis **Application du correctif 2 sur 3…** | **Backing up…** then **Applying fix 2 of 3…** |

> **Sur le playbook partiel** : cette phrase doit apparaître **sur l'écran de confirmation**, pas
> après. Découvrir à l'étape 4 qu'on ne va pas au bout détruit plus de confiance que l'annoncer d'emblée.

---

## Trois formulations à ne jamais employer

**« Une erreur inattendue s'est produite. »** — ne dit rien, n'aide à rien, et signale qu'on n'a
pas prévu le cas. Toujours nommer ce qui a échoué et sur quel fichier.

**« Êtes-vous sûr ? »** — question sans information. Le dialogue doit dire *ce qui va se passer* :
« Appliquer 3 correctifs ? », « Annuler les 3 correctifs appliqués ? ».

**« Votre installation est corrompue. »** — même quand c'est techniquement défendable, c'est une
condamnation. On décrit l'état constaté et le chemin de sortie, jamais un verdict.

---

## Notes de localisation

- **L'anglais raccourcit d'environ 15 %.** Caler les largeurs de boutons sur le **français**,
  sinon les libellés FR débordent.
- **Tutoiement en français**, cohérent avec le ton des forums pincab francophones. Ne pas
  alterner tu/vous d'un écran à l'autre — c'est le défaut le plus visible d'une localisation bâclée.
- **Ne pas traduire les noms de fichiers, de processus ni de clés de registre.** `VPinballX` reste
  `VPinballX` dans les deux langues.
- **Les chemins sont toujours anonymisés à l'affichage comme à l'export** (ADR-003) : `<toi>` en FR,
  `<you>` en EN.
- **Rappel technique** : les textes qui atterrissent dans `FrFixHints` sont rendus tels quels,
  sans `string.Format`. Aucun placeholder numérique dans ces chaînes — il s'afficherait littéralement.
  Le validateur du Knowledge Pack refuse déjà ce cas.
