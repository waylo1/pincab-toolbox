# Réponse à Gregg (forum) — 12/08/2026

Contexte : Gregg a posté 3 captures d'écran d'un scan PincabToolbox (avertissements FlexDMD et B2S
manquants, un Critical ROM manquant sur 'Full House (Williams 1966)', écran À propos v0.1.2), avec
deux questions : où trouver le rapport texte complet mentionné dans les "détails", et s'il existe un
moyen d'éviter les alertes ROM manquant pour les tables qui n'ont pas besoin de ROM.

Vérifié dans le code avant de répondre : `src/PincabToolbox.Core/Scanning/RomValidatorScanner.cs`.
Le scanner ne signale un ROM manquant que si le script de la table pilote réellement le contrôleur
VPinMAME (`UsesController`). Une table originale/homebrew qui se contente d'un backglass B2S sans
piloter VPinMAME reçoit le code `ROM_NOT_REQUIRED`, en `Ok`, jamais en Critical. C'est déjà exactement
ce que Gregg demande, et ça tourne déjà sur son install. 'Full House (Williams 1966)' est une vraie
table Williams qui pilote VPinMAME, donc son script demande bien un ROM : le Critical est correct, pas
un faux positif, il lui manque juste le fichier `Full House.zip` dans son dossier roms VPinMAME.

Pour le rapport complet : bouton "Export report" dans la barre du haut (HTML/TXT/MD/BBCode/JSON), et
bouton "Copy for forum" qui copie directement un texte Markdown prêt à coller, avec chaque résultat et
son détail complet (pas seulement ce qui est visible dans le tableau à l'écran).

---

## Texte prêt à poster (anglais)

Hi Gregg, thanks for the detailed screenshots, that's exactly what I needed.

For the full report, look for the "Export report" button in the top toolbar, it lets you save the
scan as HTML, TXT, Markdown or BBCode, and each format includes the full detail and fix hint for every
finding, not just what's shown in the table. There's also a "Copy for forum" button right next to it,
it copies a ready to paste Markdown version straight to your clipboard, that's probably the fastest way
for you to share a full report next time.

On the ROM question, the scanner already does what you're asking. It reads each table's script and
only checks for a ROM if the script actually drives the VPinMAME controller. Original or homebrew
tables that just show a B2S backglass without touching VPinMAME are never flagged, you'd see them
listed as fine in the full report. Full House (Williams 1966) is a real Williams table though, its
script does call the VPinMAME controller, so the Critical is accurate, it genuinely needs a ROM to
run. You just need to drop Full House.zip into your VPinMAME roms folder and that one will clear.

Same logic applies to the FlexDMD and B2S Backglass Server warnings in your screenshots, those are
real too, they're telling you those components aren't installed yet, not false positives.
