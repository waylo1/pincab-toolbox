# FAQ / Réponses aux objections — Pincab Toolbox (lancement)

*Réponses prêtes à coller pendant les 48h. Règle d'or : honnête, précis, jamais défensif, et **on ne sur-promet JAMAIS**. Un sceptique qui te prend en défaut sur une affirmation ronde = crédibilité perdue. Mieux vaut une nuance vraie.*

## Les faits VRAIS du produit (base de toute réponse)
- **Lecture seule** : ne modifie jamais fichier / registre / base. ✅
- **Rien de TOI n'est envoyé** : pas de télémétrie, pas de compte, pas d'analytics. ✅
- ⚠️ **PAS "zéro réseau"** : l'Update Watcher lit la base PUBLIQUE du Virtual Pinball Spreadsheet (check "tables périmées"). Tourne aussi hors-ligne. → dire « rien de toi n'est envoyé », **jamais** « aucun appel réseau ».
- **Exe non signé** (signature EV = coût + token matériel, prévu plus tard) → alerte SmartScreen normale.
- **Pas open source** (le repo ne contient que l'exe). Ne pas laisser croire l'inverse.
- **Périmètre** : VPX + PinUP Popper, Windows. Pas Future Pinball / Pinball FX / Linux / Batocera.
- **Gratuit pour toujours** (scanner). Version qui RÉPARE = peut-être plus tard, payante — assumé, pas caché.

---

## 1. « No tracking mais le lien a un tracker FB »
EN: Good catch — that `fbclid` is added by Facebook itself to every outbound link on the platform, on anyone's post. Not from the app or the landing page, and nothing comes back to me. The app never uploads anything about you (no telemetry); the only network it uses is reading the *public* Virtual Pinball Spreadsheet DB for the "outdated tables" check, and it runs fully offline too. The landing has no pixel/analytics/cookies — view-source confirms it. Appreciate you keeping me honest 👊

## 2. « C'est safe ? Non signé / SmartScreen / malware ? »
EN: Totally fair. It's unsigned for now (code-signing needs a paid cert + hardware token, it's on the roadmap), so Windows shows the usual SmartScreen warning. It's **read-only** by design — never writes to your files, registry or DB, only reads and reports. If you want to be sure before running it: drop it in a VM/sandbox and watch it. (Heads-up if you VirusTotal it: a single-file unsigned .NET app can trip one or two heuristic flags from the bundler — that's the packer, not malware.)
FR: Compréhensible. Pas encore signé (certificat payant + token matériel, c'est prévu), d'où l'alerte SmartScreen normale. Il est en **lecture seule** — ne modifie jamais rien. Pour être sûr : lance-le dans une VM. (Si tu le passes sur VirusTotal : un exe .NET mono-fichier non signé peut déclencher 1-2 alertes heuristiques du packer — c'est l'empaquetage, pas un virus.)

## 3. « C'est open source ? Je peux voir le code ? »
EN: Not open-source right now — the scanner is closed for the moment. I get that's a big ask for an unsigned tool; what I can offer today: it's read-only, you can run it in a sandbox and watch exactly what it touches, and I'm right here for any question. Open-sourcing the scanner is something I'm considering.
⚠️ Perso : NE PAS promettre l'open-source si pas décidé. « considering » seulement.

## 4. « Ça phone home ? Ça envoie mes données ? »
EN: Nothing about you or your machine ever leaves it — no telemetry, no account. The only network call is a one-way read of the *public* Virtual Pinball Spreadsheet database for the "outdated tables" check, and it works fully offline if you prefer.

## 5. « Ça m'a flag X mais c'est normal » (FAUX POSITIF — priorité absolue)
EN: Thanks for flagging — could you paste the exact line from the report? It's a v0.1 and killing false positives is my #1 priority, I fix fast.
FR: Merci — tu peux coller la ligne exacte du rapport ? C'est une v0.1, les faux positifs sont ma priorité n°1, je corrige vite.
→ consigner FIELD-LOG (bac FP). Puis remercier publiquement quand corrigé.

## 6. « Ça n'a pas vu mon problème » (faux négatif, ex. E0434352)
EN: Yeah, v0.1 has a limited set of checks — tell me the case and it goes straight onto the list for the next version. That's exactly how it gets better.
FR: Oui, la v0.1 a un jeu de checks limité — dis-moi le cas, il file direct dans la liste de la prochaine version.
→ FIELD-LOG (bac FN).

## 7. « T'es qui ? Pourquoi te faire confiance ? »
EN: Fair question — I'm a solo dev and cab owner, got tired of losing evenings fixing mine instead of playing, so I built this for myself and figured I'd share it. Free, read-only, and I'm right here for feedback.

## 8. « C'est gratuit ? C'est quoi le piège ? Tu vas faire payer ? »
EN: The scanner is free and read-only, forever — no catch. I do run a small one-man business, and down the line there may be an optional paid version that actually *repairs* some of what the scanner finds (always with a backup + preview + undo). But the scanner itself stays free. Not hiding the ball 🙂
FR: Le scanner est gratuit et en lecture seule, pour toujours — pas de piège. Je tiens une micro-entreprise solo, et plus tard il pourrait y avoir une version optionnelle payante qui RÉPARE une partie de ce que le scanner trouve (toujours sauvegarde + aperçu + annulation). Le scanner reste gratuit. Je ne cache rien 🙂

## 9. « Ça marche avec Future Pinball / Pinball FX / Linux / Batocera ? »
EN: For now it's built for VPX + PinUP Popper on Windows — those other setups are out of scope at the moment. Noted though, tell me what you run.

## 10. « Tu scrapes les forums / nos données ? »
EN: No scraping at all — it only uses the open-source Virtual Pinball Spreadsheet database and links you to official pages. It never downloads tables, ROMs or media. The community sites stay the source.

---
*Ton général : remercier, être précis, inviter à vérifier soi-même, ne jamais se braquer. Chaque échange technique ou FP/FN → FIELD-LOG.*
