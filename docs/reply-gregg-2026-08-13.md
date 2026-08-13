# Réponse à Gregg (forum) — 13/08/2026

Contexte : Gregg répond à notre message du 12/08 avec trois points — Full House (Williams 1966) est
une vraie table électromécanique, sans puce ROM digitale ; des tables homebrew (ex. un "Happy
Potter") mentionnent un nom de ROM dans leur script mais tournent très bien sans elle, alors que le
scanner les marque "will not start" ; et il n'a pas trouvé comment ouvrir le "rapport détaillé" après
avoir exporté un rapport.

Vérifié dans le code avant de répondre.

**Sur les deux cas ROM (Full House + homebrew) : pas de correctif aujourd'hui, une question de
clarification d'abord.** `ScriptAnalyzer.AnalyzeRomUsage` (`src/PincabToolbox.Core/Services/`)
n'active `UsesController` que sur un vrai `CreateObject("VPinMAME.Controller")` non commenté — les
commentaires sont retirés avant l'analyse (`StripComments`), donc un nom de ROM cité dans un
commentaire ou une ligne morte ne déclenche rien. Si ces tables sortent en Critical, leur script
contient donc un appel réel au contrôleur VPinMAME, ce n'est pas un artefact de mot-clé. L'hypothèse
la plus probable : ces scripts créent le contrôleur pour une fonctionnalité optionnelle (son ou
affichage DMD additionnel) et protègent l'appel qui charge la ROM, si bien que la table tourne quand
même quand le fichier manque — un cas que le scanner ne distingue pas aujourd'hui de "la table ne
démarre pas sans ROM". On ne code pas cette distinction sans avoir vu un vrai script qui fait ça : la
dernière fois qu'on a détendu cette détection sur une hypothèse pas vérifiée, ça a rouvert un vrai
faux positif ailleurs. D'où la question posée à Gregg ci-dessous avant tout changement.

**Sur le rapport complet : notre réponse du 12/08 avait tort, corrigé ici.** On avait écrit que
"each format includes the full detail […] not just what's shown in the table" — c'est faux pour
HTML/Markdown/BBCode : ces trois formats regroupent les résultats répétitifs (ex. 273×`B2S_ORPHAN`)
sous une seule ligne de résumé, avec un message qui dit juste "the full text report has every one of
them" sans préciser qu'il s'agit du même bouton "Export report", avec le format .txt (ou .json)
choisi dans le menu déroulant du dialogue de sauvegarde. Gregg a très probablement exporté en HTML
(le choix par défaut) et n'a logiquement rien trouvé de plus détaillé que ce qu'il voyait déjà.
Corrigé dans la réponse ci-dessous ; la clarté de ce message de regroupement est un candidat de
correction à faible coût, prévu quand on touchera l'export (PDF, prochain chantier), pas dans cette
réponse.

---

## Texte prêt à poster (anglais)

Hi Gregg, thanks for pushing back on this, and sorry for the confusion on both counts.

On the report first, since that one's on us: I gave you wrong info last time. HTML, Markdown and
BBCode all collapse repetitive findings into one summary line to stay readable (that's what your
"273 similar findings" line is) — only the .txt and .json formats show every single one
individually. Same "Export report" button, you just need to pick ".txt" (or ".json") in the save
dialog's format dropdown instead of the default ".html". That's on you having no way to guess it from
that message, we'll make it clearer.

On the ROM findings, I don't want to give you another confident answer I'm not sure of. The scanner
only flags a table when its script genuinely calls the VPinMAME controller (CreateObject), so
something in Full House's and that homebrew table's script really does reach for VPinMAME, it's not
picking up the ROM name as plain text. But you're right that "will not start" doesn't match what
you're seeing. My best guess is those scripts create the controller for something optional (extra
sound or DMD output) and guard the actual ROM load so the table still runs fine without it, which
would make our wording wrong without the finding itself being made up.

Before I change anything: for either table, when you remove/rename the ROM zip, does anything
actually look or sound different in-game (missing sound, dead DMD/score display), or does it run
completely identically to when the ROM is present? That tells me whether "will not start" should
become something softer like "some sound/display may not work" instead of just going quiet for these
cases — I'd rather get this right than fast.

---

## Suite (13/08, plus tard) — Gregg répond, redirige vers JPSalas

Contexte : Gregg confirme n'avoir aucun fichier ROM (.zip) pour ces deux tables, mais ne peut pas
expliquer pourquoi le script les mentionne quand même, il ne fait pas de scripting VPX lui-même, et
suggère de contacter JPSalas (auteur de nombreuses tables originales) pour la réponse technique.

Toujours pas de script réel entre les mains, toujours pas assez pour toucher au code (voir l'analyse
du 12/08 ci-dessus, elle tient telle quelle). On ferme le fil poliment plutôt que d'insister, il a dit
clairement ne pas pouvoir aider davantage.

### Texte prêt à poster (anglais)

Thanks Gregg, that actually confirms what I needed, no ROM file at all and the table still runs
fine. I don't want to guess at what that script call is doing without seeing it, so I won't ask you
to dig further since you've said scripting isn't your thing, thanks for trying. Reaching out to
JPSalas is a good idea, I might do that myself down the line. For now I'm leaving the wording as it
is until I actually understand the mechanism, I'd rather stay accurate than guess again.
