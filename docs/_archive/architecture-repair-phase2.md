# Architecture — Pincab Toolbox Repair (Phase 2)

> Document de conception. Objectif : réutiliser au maximum le Core existant (zéro dépendance), garder le scanner gratuit intact et indépendant, ajouter une couche "Repair" séparée et payante.

---

> ## ⚠️ Statut : NOTES v1 — à remplacer par le vrai design Repair
>
> Écrit le 19/07/2026, avant que les fondations soient verrouillées. Conservé pour ses **idées de fixers**
> et son **contrat de sûreté**, qui restent valables.
>
> **Périmé dans ce fichier :**
> - **§4 (UI, option A vs B)** — tranché : un seul exécutable, modules déverrouillés par licence. Voir **ADR-002**.
> - **§5 (licence)** — remplacé par **ADR-002** : perpétuel 19 € + renouvellement optionnel 9 €/an, vérification locale.
> - **§6 (ordre de développement)** — le `FocusWatchdog` n'appartient plus à Repair mais à **Play Optimizer** (ADR-001).
> - **§7 (prochaine étape)** — obsolète, voir `HANDOFF.md`.
>
> Le `repairConfidence` du Knowledge Pack (`ARCHITECTURE-KnowledgeEngine.md` §5.3 et §6) doit servir de base
> au design — il n'existait pas encore quand ces notes ont été écrites.

---


## 1. Principe directeur

Le **Scanner reste 100 % gratuit et autonome** — il ne doit jamais dépendre du module Repair pour fonctionner. Le module Repair, lui, **dépend du Scanner** : chaque action de réparation part d'un `Finding` (un problème déjà détecté et affiché à l'utilisateur), jamais d'une action à l'aveugle.

Règle de confiance (héritée du scanner) : **chaque réparation doit être réversible ou confirmée explicitement** avant écriture. On ne perd pas la confiance construite avec le "lecture seule" du scanner en devenant intrusif sans prévenir.

## 2. Nouveau projet : `PincabToolbox.Repair` (Core, zéro dépendance)

Même philosophie que `PincabToolbox.Core` : pas de packages NuGet externes, C# pur + API Windows natives via P/Invoke si besoin (ex. gestion fenêtres pour le focus watchdog).

```
src/PincabToolbox.Repair/
  Fixers/
    IFixer.cs                 → interface commune : CanFix(Finding), Preview(), Apply(), Undo()
    RomFolderFixer.cs         → propose où placer une ROM manquante (ouvre l'explorateur au bon dossier, pas de téléchargement)
    BitnessFixer.cs           → détecte la présence locale d'un binaire 64-bit alternatif et propose de le lier/copier si déjà présent chez l'utilisateur
    BackglassLinkFixer.cs     → renomme/relie un .directb2s existant au bon nom de table
    PopperRegistrationFixer.cs→ ajoute l'entrée manquante dans la base PinUP Popper (SQLite, écriture ciblée avec backup préalable)
  Services/
    BackupService.cs          → copie de sécurité systématique avant toute écriture (horodatée, restaurable en 1 clic)
    UndoLog.cs                → journal des actions effectuées, permet un "tout annuler" de session
  FocusWatchdog/
    WatchdogService.cs        → process léger qui remet VPX au premier plan si un autre process (Popper, overlay) lui vole le focus au lancement d'une table
  DisplayLayout/
    ScreenPickerOverlay.cs    → assistant "clique sur cet écran pour dire à quoi il sert" (DMD / backglass / playfield / topper) au lieu d'éditer un .ini à la main
    Cfg/RegistryOrIniWriter.cs→ écrit la config résultante dans VPinMAME/B2S selon ce qui est détecté
  MediaRename/
    MediaMatcher.cs           → propose un renommage des fichiers médias (images/vidéos wheel, backglass...) pour matcher le nom exact de la table
```

## 3. Ce qui NE change PAS dans le Core / Scanner existant

- `Scanning/*Scanner.cs` restent inchangés — ils produisent des `Finding` (Models/Finding.cs), c'est le contrat entre gratuit et payant.
- Idée clé : un `Finding` doit porter un identifiant de catégorie stable (`rom`, `bitness`, `completeness`, etc. — déjà le cas) pour que `Repair` sache quel `IFixer` proposer en face de chaque ligne du rapport.
- Suggestion mineure (non bloquante) : ajouter un champ optionnel `FixerHint` sur `Finding` pour que l'UI gratuite puisse afficher un bouton grisé "🔒 Réparable avec Repair" — teasing léger, jamais intrusif, sans bridage du scanner.

## 4. UI — App WPF

Deux approches possibles, à trancher ensemble :

- **Option A — Même exécutable, fonctionnalités déverrouillées par licence.** Plus simple à distribuer (un seul .exe), le module Repair est présent mais verrouillé tant qu'aucune clé de licence valide n'est saisie. Risque : plus facile à cracker, mais public pincab pas hostile — beaucoup paieront par soutien au projet.
- **Option B — Exécutable séparé (Repair.exe) téléchargé après achat.** Plus propre côté séparation gratuit/payant, mais duplique une partie de l'UI et complique la distribution.

**Recommandation : Option A** — un seul installeur, colonne "Réparer" dans le tableau de résultats qui s'active avec la licence. Plus simple à maintenir pour une micro-entreprise solo, et cohérent avec le principe "le scanner reste toujours complet, le repair est un bonus".

## 5. Licence — approche minimale viable

Pas de serveur de licence complexe au départ (coût de dev disproportionné pour un lancement à petit prix) :
- Génération d'une clé simple liée à l'email (ex. signature HMAC locale vérifiable hors-ligne) au moment du paiement Stripe.
- Vérification 100 % locale dans l'app (pas d'appel réseau obligatoire pour activer — cohérent avec le discours "zéro télémétrie").
- Anti-piratage volontairement léger : le public visé achète par soutien à un outil qui résout un vrai problème, pas par contrainte technique.

## 6. Ordre de développement suggéré (une fois le scanner validé sur cab réel)

1. `BackupService` + `UndoLog` (fondation de sécurité, sert à tous les fixers)
2. `RomFolderFixer` + `BackglassLinkFixer` — les plus simples, forte valeur perçue immédiate
3. `PopperRegistrationFixer` — un peu plus sensible (écriture SQLite), bien tester avec des sauvegardes
4. `FocusWatchdog` — fonctionnalité "confort" très demandée sur les forums, bon argument marketing
5. `DisplayLayout` — le plus complexe (interaction utilisateur multi-écrans), à faire en dernier
6. Génération de licence + Stripe Payment Link

## 7. Prochaine étape

*(Section périmée — voir le bandeau en tête de fichier. Le modèle de prix est figé dans **ADR-002** ; la prochaine étape est dans `HANDOFF.md`.)*
