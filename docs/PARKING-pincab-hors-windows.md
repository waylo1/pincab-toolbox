# Pincab hors Windows — état des lieux, décision reportée

**Statut : 🅿️ PARQUÉ.** Aucune action maintenant. Cohérent avec la décision figée n°1 : on reste
sur le pincab Windows jusqu'au premier euro encaissé.

*Faits vérifiés le 27/07/2026. À revérifier avant toute décision — ce segment bouge vite.*

---

## 1. La question

« Et si les pincabs ne tournaient pas sous Windows ? »

Elle est légitime : notre application est en WPF, donc Windows uniquement. Si le marché bascule,
on construit sur du sable.

## 2. Ce qui est vrai aujourd'hui

**VPX tourne sur Linux.** Le portage *VPX Standalone* est réel et fonctionne : plateau, backglass,
DMD, configuration multi-écrans. Batocera intègre un système Visual Pinball. Ce n'est pas une rumeur.

**Mais l'écosystème autour ne suit pas.** Sur un cab Linux en 2026 :

| Brique | État sous Linux |
|---|---|
| VPX (le simulateur) | ✅ fonctionne |
| Backglass, DMD, écrans | ✅ fonctionne |
| **DOF** (retour physique) | ⚠️ « fonctionne, mais pas de façon constante » |
| **PUP-Packs** | ⚠️ certains oui, d'autres non |
| **Tables** | ⚠️ toutes ne marchent pas sans patch |
| **PinUP Popper** | ❌ **Windows uniquement** — l'alternative multiplateforme est VpinFE |

Les développeurs VPX travaillent à combler l'écart en ajoutant des greffons pour les
fonctionnalités Windows (VBScript, .NET).

**Conclusion factuelle** : viable pour un passionné prêt à dépanner, **pas encore clé en main**.
Un cab complet — retour physique, frontend, médias — reste très majoritairement Windows.

## 3. Ce que notre architecture encaisse déjà

C'est la bonne surprise, et ce n'était pas prémédité.

| Bloc | Cible | Dépendance Windows |
|---|---|---|
| `PincabToolbox.Core` (7 scanners) | `net8.0` | **aucune** — vérifié |
| `PincabToolbox.Repair` (moteur) | `net8.0` | uniquement le « Mark of the Web », derrière `OperatingSystem.IsWindows()` |
| `PincabToolbox.App` (interface) | `net8.0-windows` | **totale** — WPF |

**Le moteur tourne déjà sous Linux.** Ce n'est pas une hypothèse : les 56 tests du Core et les 61
de Repair s'exécutent sous Linux à chaque session de développement. Seule l'interface est bloquée.

## 4. Pourquoi ce n'est quand même pas « juste un portage d'UI »

Le vrai obstacle n'est pas technique, il est **dans la connaissance**.

Sur un cab Linux, la moitié de nos findings n'existent tout simplement pas :

- `BLOCKED_DLL` — le « Mark of the Web » est une particularité NTFS. **Aucun sens sous Linux.**
- `POPPER_NOT_REGISTERED` — pas de PinUP Popper sous Linux.
- `BITNESS_*` — les scanners lisent des en-têtes PE de `.dll`. Sous Linux ce sont des `.so` au
  format ELF : autre format, autres règles, autres pannes.

Autrement dit : **un pincab Linux n'est pas une plateforme à porter, c'est un domaine de connaissance
différent**. Le moteur se transporte ; le Knowledge Pack, non.

Et c'est précisément ce que notre architecture sépare déjà (ADR-005 : le moteur est du code, la
connaissance est de la donnée). Le jour où ce segment compte, le travail sera **un nouveau pack et
une nouvelle interface**, pas une réécriture.

## 5. Décision

**On ne fait rien maintenant.** Construire pour un segment expérimental avant d'avoir encaissé un
euro sur le segment principal, c'est exactement la dispersion que la décision n°1 interdit.

**Deux garde-fous à coût nul, à tenir :**

1. **Ne jamais introduire de dépendance Windows dans `Core` ni dans `Repair`.** C'est déjà le cas,
   et les tests tournant sous Linux le vérifient mécaniquement à chaque exécution. Si quelqu'un
   ajoute un accès registre non gardé dans le Core, la CI le verra.
2. **Ne pas promettre le support Linux.** Ni sur la landing, ni sur les forums. Le jour venu, ce
   sera une bonne nouvelle ; annoncé trop tôt, ce serait une dette.

## 6. Le signal qui rouvrirait le sujet

Un seul : **un frontend multiplateforme crédible qui prend une part réelle du marché**
(VpinFE ou un autre), ou l'apparition de posts « mon cab Linux ne marche pas » en nombre sur
Pincab Passion et VPUniverse.

Tant que la douleur ne se voit pas sur les forums, elle n'existe pas pour nous — c'est le même
critère que pour tout le reste du projet.

---

### Sources
- [Batocera — système Visual Pinball](https://wiki.batocera.org/systems:vpinball)
- [Major Frenchy — Linux Cab Project (02/2026)](https://www.majorfrenchy.com/blog/2026/02/20/Linux-Cab/)
- [VPForums — VpinFE, frontend Linux/Windows/Mac](https://www.vpforums.org/index.php?showtopic=56634)
