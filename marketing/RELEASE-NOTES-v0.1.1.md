## 🇫🇷 Français

Merci pour vos premiers retours — cette version corrige les points que vous avez remontés en quelques jours.

**Corrigé**
- **Score global plus juste.** Une grosse collection saine et à jour affichait « 0/100 · F » et « Installation en mauvais état » à cause du seul volume de notes mineures. Le score ne compte plus les simples informations (mises à jour dispo) comme des défauts, et un gros volume d'avertissements ne peut plus, à lui seul, faire chuter la note. « À corriger en priorité » est désormais réservé aux vrais problèmes bloquants.
- **Plus de fausse « ROM manquante » sur les tables originales.** Les tables originales/homebrew qui utilisent un backglass B2S (ex. Guardians of the Galaxy, Harry Potter homebrew) étaient signalées à tort en critique. Le signal « ROM requise » ne dépend plus que de VPinMAME. Les vraies tables à ROM restent correctement détectées.
- **Dossier roms sur un autre lecteur.** Quand VPinMAME/roms est sur un lecteur différent des tables, toute la vérification ROM était sautée. On lit maintenant le chemin roms configuré par VPinMAME pour retrouver le dossier, même en multi-lecteurs.

**Nouveau**
- **Alerte espace disque faible** sur le lecteur des tables (un disque presque plein provoque des erreurs de texture/chargement dans Visual Pinball).
- **Repérage des tables `.vpt` (ancien format VP9)** présentes mais non indexées par PinUP, avec la bonne marche à suivre (émulateur legacy dédié — sans le raccourci déconseillé par l'auteur de PinUP).

Toujours **100 % local, en lecture seule, zéro télémétrie, gratuit**. Chaque correctif ci-dessus est verrouillé par des tests de non-régression.

**Installation :** télécharge `PincabToolbox.zip`, dézippe le dossier et lance `PincabToolbox.exe` depuis l'intérieur (garde les fichiers ensemble). Exe non signé → alerte SmartScreen normale : « Informations complémentaires » puis « Exécuter quand même ».

---

## 🇬🇧 English

Thanks for the early feedback — this release fixes what you reported within days.

**Fixed**
- **Fairer health score.** A large, healthy, up-to-date collection could show "0/100 · F / Install in bad shape" purely because of the volume of minor notes. The score no longer counts plain information (available updates) as defects, and warning volume alone can no longer sink the grade. "Fix this first" is now reserved for genuinely blocking issues.
- **No more false "missing ROM" on original tables.** Original/homebrew tables that use a B2S backglass (e.g. Guardians of the Galaxy, Harry Potter homebrew) were wrongly flagged as missing a ROM. The "ROM required" signal now depends on VPinMAME only. Real ROM-based tables are still detected correctly.
- **Roms folder on another drive.** When VPinMAME/roms lives on a different drive than the tables, ROM checks were skipped entirely. The tool now reads VPinMAME's configured roms path to find the folder across drives.

**New**
- **Low-disk-space warning** on the tables' drive (a nearly-full drive causes texture/loading failures in Visual Pinball).
- **Legacy `.vpt` (VP9) detection** for tables present but not indexed by PinUP, pointing to the correct procedure (a dedicated legacy emulator — not the shortcut PinUP's author advises against).

Still **100% local, read-only, zero telemetry, free**. Every fix above is locked by regression tests.

**Install:** download `PincabToolbox.zip`, unzip the folder and run `PincabToolbox.exe` from inside it (keep the files together). Unsigned exe → the usual SmartScreen "More info → Run anyway".
