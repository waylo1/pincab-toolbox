> # ⚫ DOCUMENT MORT — NE PAS UTILISER
>
> Archivé le 25/07/2026. Remplacé intégralement par **ADR-002**. Ses fourchettes de prix et sa position anti-abonnement ne font plus foi.
>
> La source de vérité est `docs/PROJECT-BRAIN.md`. Ce fichier est conservé
> uniquement pour retrouver le raisonnement d'origine.

---

# Stratégie de prix — Pincab Toolbox (version Repair)

> Document de travail — MC Automation. À affiner ensemble une fois les retours du scanner gratuit obtenus.

---

## 1. Rappel du positionnement

- **Scanner (gratuit, pour toujours)** : diagnostique, ne modifie rien. C'est la porte d'entrée et l'outil de confiance communautaire.
- **Repair (payant, petit prix)** : corrige ce que le scanner trouve. Focus watchdog, réglage écrans à la souris, renommage médias, fix 64-bit, etc.

Le gratuit doit rester utile seul — jamais bridé artificiellement pour forcer l'achat. La confiance construite avec le scanner gratuit est l'actif principal ; la payante doit la mériter, pas l'exploiter.

## 2. Repères de prix sur le marché pincab

- **PinUP Popper** : gratuit / license "Pro" autour de 20 USD.
- **Outils communautaires** (scripts, utilitaires de config) : très majoritairement gratuits ou "pay what you want".
- **Logiciels indés grand public** (utilitaires Windows) : 5–15 € en one-shot, rarement plus sans fonctionnalités récurrentes.
- Le public pincab est passionné mais **allergique aux abonnements** pour un outil de maintenance — culture "j'achète une fois, c'est à moi".

## 3. Recommandation : one-shot, petit prix, pas d'abonnement

**Fourchette suggérée : 9 € – 19 € en achat unique (licence à vie, mises à jour mineures incluses).**

Pourquoi pas d'abonnement :
- Un scanner de maintenance n'apporte pas de valeur continue mensuelle (pas de contenu qui se renouvelle) → un abonnement serait perçu comme abusif par cette communauté.
- Le coût d'acquisition sur un forum passionné est faible (bouche-à-oreille), mais la tolérance au prix l'est aussi. Un abonnement tuerait la conversion.

Pourquoi pas trop bas (< 5 €) :
- Sous-évalue le travail et donne un signal de qualité faible.
- Les frais de plateforme de paiement (Stripe ~2,9 % + 0,25 €, ou Gumroad/Lemon Squeezy ~5-10 %) rognent vite une marge trop fine.

## 4. Structure suggérée (à valider après tes retours cab)

| Palier | Prix | Contenu |
|---|---|---|
| **Scanner** | Gratuit | Diagnostic complet, illimité, aucune limite de temps |
| **Repair — Early bird** (premiers utilisateurs forum) | 9 € | Toutes les fonctions repair, tarif de lancement limité dans le temps ou en nombre |
| **Repair — Prix normal** | 14–19 € | Une fois l'early bird terminé |

Option complémentaire : **"pay what you want" avec un minimum suggéré** pour la toute première vague (premiers 20-50 acheteurs sur le forum) — génère de la bonne volonté et des retours qualitatifs, quitte à passer à prix fixe ensuite.

## 5. Ce qu'il faut valider avant de figer un prix

1. **Zéro faux positif confirmé** sur ton cab réel (sinon aucune conversion, peu importe le prix).
2. **Sondage informel dans le post de lancement** : la question "tu paierais 15-20 € pour la version qui répare ça ?" peut être posée directement en fin de thread une fois la confiance installée (pas dans le post d'annonce initial, ça ferait vendeur).
3. **Coût de la solution de paiement** : Stripe (le plus simple à intégrer, pas de compte marchand séparé nécessaire) vs Gumroad/Lemon Squeezy (gèrent la TVA UE automatiquement — pertinent pour une micro-entreprise qui vend à des particuliers dans toute l'UE, à vérifier avec ton comptable si le seuil de franchise TVA de la micro-entreprise s'applique).

## 6. Prochaine étape concrète

Une fois le scanner validé sur ton cab : on regarde ensemble Stripe Payment Links (aucun code, juste un lien de paiement à coller dans les posts forum) — solution la plus rapide pour tester le marché sans développer un vrai système de licence dans un premier temps.
