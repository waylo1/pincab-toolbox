> # ⚫ DOCUMENT MORT — NE PAS UTILISER
>
> Archivé le 25/07/2026. Fusionné dans `PROJECT-BRAIN.md` §7 (Backlog).
>
> La source de vérité est `docs/PROJECT-BRAIN.md`. Ce fichier est conservé
> uniquement pour retrouver le raisonnement d'origine.

---

# Améliorations du scanner gratuit avant publication forum

> Petites choses à peaufiner avant de poster sur Pincab Passion / VPUniverse. Classées par effort, pas par obligation — le scanner fonctionne déjà tel quel (testé via le mode démo).

## Vite fait, fort impact

- **Bouton "Copier le rapport"** à côté d'"Exporter le rapport" — beaucoup de gens sur les forums collent directement le texte dans un post plutôt que joindre un fichier.
- **Filtre par module** dans le tableau (ROM / Bitness / Compatibilité / Install / Mises à jour) — utile dès qu'un cab a beaucoup de tables et que le rapport devient long.
- **Compteur de tables scannées** affiché quelque part ("42 tables analysées en 3,2s") — donne confiance sur l'exhaustivité, bon indicateur de perf à montrer dans le post.
- **Lien cliquable** dans les lignes "Mises à jour" vers virtual-pinball-spreadsheet.web.app (actuellement probablement juste du texte dans le tableau — à vérifier si le contrôle WPF le permet nativement).

## Effort moyen

- **Tri des colonnes** du tableau de résultats (cliquer sur "Gravité" trie par sévérité, etc.) — attente basique pour ce genre d'outil.
- **Bouton "Ignorer" par ligne** (masquer un finding jugé non pertinent par l'utilisateur, ex. un faux positif sur une table exotique) — désamorce les critiques "il me signale un truc qui n'est pas un problème" avant même qu'elles arrivent, et te donne un signal indirect des faux positifs remontés.
- **Persistance de la dernière config scannée** (mémorise le dernier dossier racine utilisé) pour ne pas re-parcourir à chaque lancement.

## Optionnel / à ne pas bloquer le lancement

- Icône/traybar pour lancer un scan rapide sans ouvrir toute la fenêtre.
- Thème clair en option (le dark theme est cohérent avec l'univers pincab, mais certains préfèrent).
- Export JSON en plus du .txt, pour les power users qui veulent scripter dessus.

## Ce qui ne doit PAS être fait avant le lancement

- Ne pas ajouter de nouveaux scanners/checks maintenant — le set actuel (ROM, Bitness, Compatibilité, Install, Update Watcher) est déjà cohérent et testé. Ajouter des checks non testés sur cab réel risque d'introduire des faux positifs juste avant le lancement.
- Ne pas toucher à l'architecture Core avant validation terrain — priorité au test sur ton vrai cab.

## Priorité recommandée

1. Teste d'abord sur ton cab réel (ça peut faire remonter des vrais bugs plus importants que cette liste).
2. Si tu as du temps pendant l'attente : "Copier le rapport" + lien cliquable Update Watcher sont les deux qui rendent le meilleur service au moment où les gens du forum liront le post.
3. Le reste peut attendre une v0.2 post-lancement, alimentée par les vrais retours du forum plutôt que des suppositions.
