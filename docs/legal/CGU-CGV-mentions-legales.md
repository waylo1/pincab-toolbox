# Mentions légales, CGU, CGV et politique de confidentialité — Pincab Toolbox

*Document source, rédigé 19/08/2026, corrigé le même jour après découverte de travaux antérieurs
(20-25/07) restés hors de ce repo : `flipsync-site/legal/CGU-FR.md`, `CGV-FR-brouillon.md`,
`TERMS-EN.md`, `TERMS-OF-SALE-EN-draft.md`, et `flipsync-site/landing/cgu.html` (déjà en ligne).
Cette version reprend leurs informations réelles (SIRET, traitement de l'adresse) et la grille de
prix de `docs/adr/ADR-002-modele-economique-packaging-licence.md` — ma première passe du 19/08
avait inventé des CGV avec un prix et une durée de licence non conformes à la décision de Maxime du
25/07. Version publiée en ligne : `cgu.html` sur le site FlipSync (landing), lien en pied de page.*

> **⚠️ Avant publication, 2 actions restent à faire, aucune ne peut être devinée à sa place :**
> 1. **Médiateur de la consommation** (§6) — obligatoire par la loi pour toute vente en ligne à un
>    particulier depuis 2016 (amende de 3 000 € sinon). Le moins cher trouvé (19/08) : CNPM Médiation
>    de la consommation via l'association de micro-entrepreneurs UPSME, à partir de 10 €/an. Aucun
>    médiateur n'est souscrit à ce jour, déjà signalé dans `CGV-FR-brouillon.md` depuis le 20/07 sans
>    être résolu depuis — ce document ne peut pas en inventer un.
> 2. **Case à cocher au moment du paiement** (§4.5) — le contenu numérique (clé de licence) doit être
>    marqué comme livré immédiatement avec renoncement exprès au délai de rétractation de 14 jours,
>    sinon la loi impose ce délai même après livraison. Texte exact fourni au §4.5, à ajouter dans le
>    tunnel de paiement choisi (voir §4.4 — plateforme encore à trancher).
>
> SIRET et adresse (§1) sont déjà connus et repris ci-dessous depuis `CGU-FR.md` — rien à compléter
> ici. L'adresse du siège est volontairement non publiée (statut « non-diffusible » INSEE), pas un
> oubli : c'est une option légale que les micro-entrepreneurs peuvent activer pour ne pas exposer
> leur domicile dans un registre public.

---

## Sommaire

1. Éditeur du site et de l'application
2. Hébergement
3. Conditions générales d'utilisation (CGU) — le site et le Scanner gratuit
4. Conditions générales de vente (CGV) — la licence Repair
5. Politique de confidentialité (RGPD)
6. Médiation de la consommation
7. Propriété intellectuelle
8. Droit applicable et litiges

---

## 1. Éditeur du site et de l'application

Le site `pincab-toolbox.vercel.app` et le logiciel Pincab Toolbox (y compris son module Repair)
sont édités par :

- **Nom commercial** : MC Automation
- **Statut juridique** : entreprise individuelle (micro-entrepreneur / EI)
- **Exploitant** : Maxime Chauvin
- **SIRET** : 10599502100018
- **Siège social** : en France — adresse non diffusée (statut « non-diffusible » INSEE), communiquée
  sur demande légitime
- **TVA** : non applicable, article 293 B du Code général des impôts (franchise en base de TVA)
- **Contact** : flipsync.contact@gmail.com
- **Directeur de la publication** : Maxime Chauvin

## 2. Hébergement

Le site est hébergé par :

- **Vercel Inc.**, 440 N Barranca Avenue #4133, Covina, CA 91723, États-Unis —
  [vercel.com](https://vercel.com)

Le code source de l'application et ses releases sont distribués via :

- **GitHub Inc.**, 88 Colin P Kelly Jr St, San Francisco, CA 94107, États-Unis —
  [github.com](https://github.com)

Les paiements pour la licence Repair sont traités par **Stripe**, prestataire de services de paiement
(`ADR-013`) — voir §4.4 et §5.

## 3. Conditions générales d'utilisation (CGU) — le site et le Scanner gratuit

### 3.1 Objet

Les présentes CGU régissent l'utilisation du site vitrine FlipSync et du logiciel **Pincab
Toolbox**, un utilitaire Windows gratuit qui analyse une installation Visual Pinball X / PinUP
Popper et signale ce qui est cassé, manquant ou mal configuré ("le Scanner"). L'utilisation du
Scanner vaut acceptation pleine et entière des présentes CGU.

### 3.2 Ce que fait le Scanner — et ce qu'il ne fait pas

- Le Scanner est fourni **gratuitement**, sans création de compte.
- Il fonctionne **100 % en local** : il lit des fichiers et des clés de registre sur la machine de
  l'utilisateur, n'envoie jamais ces informations ailleurs, et ne collecte aucune donnée d'usage
  (zéro télémétrie — voir §5).
- Il est **strictement en lecture seule** : le Scanner ne modifie, ne supprime et ne déplace aucun
  fichier. Seul le module payant Repair (§4) écrit sur le disque, et uniquement sur les éléments
  explicitement sélectionnés par l'utilisateur après confirmation.
- Une fonctionnalité optionnelle et manuelle — le bouton « Vérifier les mises à jour » — contacte
  GitHub, à la demande de l'utilisateur uniquement, pour comparer la version installée à la
  dernière version publiée. Aucune autre information n'est transmise à cette occasion, et cette
  vérification ne se déclenche jamais automatiquement.

### 3.3 Licence d'utilisation

L'éditeur concède à l'utilisateur un droit d'usage personnel, non exclusif et non transférable du
Scanner, à des fins d'analyse de sa propre installation. Toute décompilation, revente, ou
redistribution du logiciel sous un autre nom est interdite (voir §7).

### 3.4 Compatibilité et absence de garantie de résultat

Pincab Toolbox n'est ni édité ni affilié à Visual Pinball, VPinMAME ou PinUP Popper — il est
compatible avec ces projets communautaires gratuits, sans lien officiel avec leurs auteurs. Le
Scanner est fourni **« en l'état »** : compte tenu de l'extrême diversité des configurations de
cabinets pinball (matériel, versions logicielles, tables et mods installés), l'éditeur ne peut pas
garantir qu'il détectera l'intégralité des problèmes possibles sur une installation donnée, ni
qu'une installation signalée comme saine fonctionnera nécessairement sans erreur.

### 3.5 Limitation de responsabilité

Le Scanner étant strictement en lecture seule (§3.2), il ne peut techniquement pas être à l'origine
d'un dommage aux fichiers de l'utilisateur. L'éditeur ne saurait être tenu responsable des
conséquences d'une interprétation erronée d'un résultat de scan par l'utilisateur, ni des actions
que celui-ci entreprendrait de sa propre initiative en dehors de l'application suite à un résultat
de scan.

## 4. Conditions générales de vente (CGV) — la licence Repair

### 4.1 Objet et champ d'application

Le module **Repair** est une fonctionnalité payante de Pincab Toolbox qui, avec l'accord explicite
de l'utilisateur pour chaque action, corrige automatiquement certains problèmes détectés par le
Scanner (déblocage de fichier, restauration d'archive ROM, arrêt d'un processus fantôme, mise en
quarantaine de fichiers orphelins — liste à jour dans l'application elle-même). Les présentes CGV
s'appliquent à toute vente d'une clé de licence Repair par MC Automation à un client, particulier ou
professionnel, à distance depuis le site ou tout canal de vente indiqué par l'éditeur.

> **Statut au 19/08/2026 : bêta fermée.** Repair est proposé à un nombre restreint de testeurs, à
> titre gracieux ou à prix préférentiel selon accord individuel, avant une commercialisation plus
> large. Les présentes CGV s'appliquent dès la première vente réelle et doivent être ajustées si les
> conditions commerciales (prix, durée de licence) changent au moment du lancement public.

### 4.2 Description du produit — licence perpétuelle, mises à jour incluses sans limite de durée

Conformément à `docs/adr/ADR-013-prix-unique-3-99-et-encaissement-stripe.md` (décision de Maxime,
19/08/2026, qui supersede `ADR-002` sur ce point), une licence Repair est un **achat unique et
perpétuel**, pas un abonnement :
- l'achat déverrouille durablement les actions de réparation automatiques du module Repair, dans
  l'exécutable unique qui contient aussi le Scanner gratuit ;
- il inclut les mises à jour du module Repair et de sa base de connaissance (Knowledge Pack :
  nouveaux correctifs, compatibilité avec les nouvelles versions de VPX) **sans limite de durée** ;
- il n'y a **aucun renouvellement à payer**, ni annuel ni autre. Rien ne s'éteint, rien ne se bride,
  aucune fonctionnalité acquise n'est reprise.

Chaque action de réparation :
- effectue une sauvegarde avant toute modification (quand la nature de l'action le permet — certaines
  actions, par nature, ne sont pas réversibles ; l'application l'indique explicitement avant
  confirmation) ;
- montre un aperçu de ce qui va changer avant toute exécution ;
- reste annulable après coup via le journal de l'application, sauf mention contraire affichée au
  moment de l'action ;
- refuse de s'exécuter tant que le logiciel de simulation pinball (Visual Pinball) est en cours
  d'utilisation, pour éviter d'écrire sur des fichiers ouverts.

### 4.3 Prix

Prix unique au 19/08/2026 (`ADR-013`) — achat unique, aucun abonnement, aucun renouvellement :

| Devise | Prix |
|---|---|
| Euro | 3,99 € |
| Dollar américain | 3.99 $ |
| Livre sterling | 3.99 £ |

**Le prix est le même nombre quelle que soit la devise** ; il ne s'agit pas d'une conversion au taux
du jour. La devise appliquée est celle proposée à l'acheteur au moment de la commande.

TVA non applicable en France, article 293 B du CGI (voir §1 et §4.4bis). Le prix définitif est celui
indiqué sur la page de paiement au moment de la transaction ; MC Automation se réserve le droit de
faire évoluer ce prix pour les achats futurs, sans effet rétroactif sur une licence déjà vendue.

> **Rappel `ADR-013` :** les mises à jour étant incluses à vie, la valeur promise repose entièrement
> sur un Knowledge Pack qui continue de s'enrichir. Si le pack cesse d'évoluer, c'est la promesse de
> vente elle-même qui doit être revue — ce n'est pas une simple clause de style.

### 4.4 Paiement et livraison

> **Plateforme de paiement — tranchée le 19/08/2026 par `ADR-013`.** Le choix est **Stripe, en
> direct**. `ADR-009` (Lemon Squeezy en Merchant of Record) est abandonné : c'est `ADR-013` qui fait
> foi. Conséquence fiscale assumée et documentée en §4.4bis.

Le paiement est traité par **Stripe**, qui agit en qualité de **prestataire de services de paiement**
et non de revendeur officiel (*Merchant of Record*) : **MC Automation demeure le vendeur** au sens
juridique vis-à-vis de l'acheteur, et émet la facture.

MC Automation ne stocke ni ne voit jamais le numéro de carte bancaire du client — voir la politique
de confidentialité de Stripe pour le traitement de cette donnée.

### 4.4bis TVA sur les ventes internationales

MC Automation bénéficie de la **franchise en base de TVA** (article 293 B du CGI) : aucune TVA n'est
facturée sur les ventes réalisées en France.

L'encaissement se faisant par un simple prestataire technique (§4.4) et non par un revendeur
officiel, MC Automation reste redevable de la TVA du pays de l'acheteur pour les ventes B2C
transfrontalières dans l'Union européenne, au-delà du **seuil européen unique de 10 000 €** — seuil
qui s'applique **même sous franchise en base**. En dessous : aucune formalité. Au-delà : facturation
au taux du pays de l'acheteur et déclaration via le **guichet unique OSS**. Les ventes hors Union
européenne relèvent des règles propres à chaque juridiction.

> **ACTION MAXIME (§4.4bis)** — à cadrer avec un comptable avant la première vente publique. À 3,99,
> le seuil de 10 000 € représente environ 2 500 ventes : pas un problème du jour 1, mais un seuil à
> surveiller et non à découvrir.

La livraison consiste en l'envoi, par email, d'une **clé de licence numérique** générée
individuellement pour l'adresse email du client, vérifiée localement par l'application (signature
hors-ligne, aucun appel réseau obligatoire pour l'activer — cohérent avec le zéro télémétrie de
`ADR-002`). Cet envoi intervient dans les meilleurs délais suivant la confirmation du paiement, en
pratique sous 48 heures compte tenu du fonctionnement actuel, manuel, de la génération de clés
(activité solo).

### 4.5 Droit de rétractation — contenu numérique

Conformément à l'article L.221-28 13° du Code de la consommation, le droit de rétractation de 14
jours ne s'applique pas à la fourniture d'un contenu numérique non fourni sur un support matériel
(ici : une clé de licence envoyée par email) **dont l'exécution a commencé après accord préalable
exprès du consommateur, qui a renoncé expressément à son droit de rétractation**.

> **ACTION MAXIME (§4.5) — texte à faire cocher explicitement avant paiement**, dans le checkout
> Stripe (§4.4), juste avant le bouton de paiement :
> « Je reconnais que la licence Repair est un contenu numérique livré immédiatement après paiement,
> et je renonce expressément à mon droit de rétractation de 14 jours. »
>
> Sans cette case cochée et enregistrée, le délai de rétractation légal de 14 jours s'applique
> malgré la livraison immédiate — un client pourrait légalement demander un remboursement dans les
> 14 jours même après avoir reçu et utilisé sa clé.

### 4.6 Garantie légale de conformité

L'exclusion du droit de rétractation (§4.5) ne prive pas le client des garanties légales de
conformité et des vices cachés (articles L.217-3 et suivants, et 1641 et suivants du Code civil),
qui restent applicables : si la licence livrée ne donne pas accès aux fonctionnalités décrites au
§4.2, ou si le module Repair présente un défaut qui le rend impropre à son usage normal, le client
peut demander la mise en conformité, le remboursement, ou l'échange, selon les règles de droit
commun.

### 4.7 Responsabilité

Le module Repair modifie des fichiers réels sur la machine du client, avec son accord explicite,
action par action (§4.2). MC Automation met en œuvre les moyens décrits au §4.2 (sauvegarde,
aperçu, confinement aux dossiers de l'installation détectée) pour limiter ce risque, sans pouvoir
garantir un résultat dans tous les environnements possibles (diversité des configurations, voir
§3.4). La responsabilité de MC Automation, en cas de dommage directement imputable à une action
Repair, est limitée au montant effectivement payé par le client pour la licence concernée.

### 4.8 Durée

La licence Repair est **perpétuelle** (§4.2) : elle ne s'éteint jamais, n'est jamais résiliée et
n'est limitée par aucune échéance. Les mises à jour du module Repair et de sa base de connaissance
sont incluses **sans limite de durée** et sans renouvellement à payer (§4.3). Le Scanner gratuit continue lui aussi de
fonctionner sans limitation (§3), avec ou sans licence Repair.

## 5. Politique de confidentialité (RGPD)

### 5.1 Responsable de traitement

MC Automation (Maxime Chauvin), coordonnées au §1, est responsable du traitement des données
décrites ci-dessous.

### 5.2 Le site

Le site vitrine ne dépose aucun cookie de mesure d'audience ou de publicité, et n'intègre aucun
outil d'analytics ou de tracking tiers. Aucune donnée personnelle n'est collectée par la simple
navigation sur le site.

### 5.3 Le logiciel Pincab Toolbox — Scanner

Comme indiqué en application (écran « À propos ») et au §3.2 : le Scanner fonctionne 100 % en
local, ne transmet aucune information sur l'installation, les tables ou les résultats de scan de
l'utilisateur, et ne nécessite aucun compte. La seule connexion réseau possible est manuelle et
volontaire (bouton « Vérifier les mises à jour », requête à GitHub).

### 5.4 Achat d'une licence Repair

Lors de l'achat d'une licence Repair, les données suivantes sont traitées :

- **Adresse email**, pour l'envoi de la clé de licence et le support — base légale : exécution du
  contrat de vente (article 6.1.b du RGPD). Conservée pendant la durée de la relation commerciale
  puis archivée selon les délais légaux de conservation comptable (10 ans, obligation légale).
- **Données de paiement** (numéro de carte, etc.) : jamais reçues ni stockées par MC Automation —
  traitées directement par Stripe (§4.4, `ADR-013`), sous sa propre politique de confidentialité.

Aucune donnée n'est vendue, louée ou partagée à des fins publicitaires. Aucun profilage n'est
effectué.

### 5.5 Sous-traitants

- **Stripe** (prestataire de services de paiement, §4.4) — voir §5.4.
- **Vercel** (hébergement du site) — voir §2.
- **GitHub** (distribution du logiciel, vérification manuelle de version) — voir §2.

Ces prestataires peuvent être situés hors de l'Union européenne (États-Unis) ; ils s'appuient sur
leurs propres garanties de transfert (clauses contractuelles types ou équivalent — voir leurs
politiques respectives).

### 5.6 Droits de la personne concernée

Conformément aux articles 15 à 21 du RGPD, toute personne dispose d'un droit d'accès, de
rectification, d'effacement, de limitation, d'opposition et de portabilité sur ses données, exerçable
par email à flipsync.contact@gmail.com. En cas de réponse insatisfaisante, une réclamation peut être
introduite auprès de la CNIL ([cnil.fr](https://www.cnil.fr)).

## 6. Médiation de la consommation

Conformément à l'article L.616-1 du Code de la consommation, tout client consommateur (particulier)
en litige avec MC Automation, après une réclamation écrite préalable restée insatisfaite, peut
recourir gratuitement à un médiateur de la consommation.

> **ACTION MAXIME (§6)** — aucun médiateur n'est souscrit à ce jour (19/08/2026). C'est une
> obligation légale pour toute vente à distance à un particulier, sanctionnée par une amende de
> 3 000 € en cas de manquement (source : upsme.fr, voir recherche du 19/08). Option la moins chère
> trouvée : CNPM Médiation de la consommation via l'association UPSME, à partir de 10 €/an pour un
> micro-entrepreneur. Une fois souscrit, remplacer ce paragraphe par : nom du médiateur, adresse
> postale, site web.
> **Médiateur : [À COMPLÉTER une fois souscrit]**

## 7. Propriété intellectuelle

Le nom « Pincab Toolbox », le nom « FlipSync », leur code source, leur interface et leur contenu
(hors composants tiers cités ci-dessous) sont la propriété de MC Automation / Maxime Chauvin, protégés
par le droit d'auteur dès leur création (article L.111-1 du Code de la propriété intellectuelle —
aucun dépôt n'est nécessaire pour cette protection). Toute reproduction, modification ou
redistribution sans autorisation écrite est interdite.

Pincab Toolbox n'est ni édité ni affilié à Visual Pinball, VPinMAME ou PinUP Popper, dont les noms et
marques appartiennent à leurs auteurs respectifs (voir §3.4). L'Update Watcher s'appuie sur la base
de données ouverte Virtual Pinball Spreadsheet, dont les données restent la propriété de ses
contributeurs.

## 8. Droit applicable et litiges

Les présentes CGU/CGV sont soumises au droit français.

En cas de litige, et avant toute action judiciaire, le client est invité à contacter MC Automation
à l'amiable (flipsync.contact@gmail.com), puis, si besoin, le médiateur de la consommation (§6).

Pour un client **consommateur** (particulier), les règles impératives du Code de la consommation et
du Code de procédure civile s'appliquent : le consommateur peut saisir soit l'une des juridictions
territorialement compétentes selon le Code de procédure civile, soit la juridiction du lieu où il
demeurait au moment de la conclusion du contrat ou de la survenance du fait dommageable — aucune
clause contraire ne peut restreindre ce choix.

Pour un litige entre professionnels, les tribunaux compétents sont ceux du ressort du siège social
de MC Automation — adresse non publiée (§1), communiquée sur demande légitime dans ce contexte.

---

*Dernière mise à jour : 19/08/2026. Toute évolution du produit (fin de la bêta fermée Repair, ajout
d'un nouveau moyen de paiement, changement de prestataire d'hébergement…) doit se refléter ici avant
d'entrer en vigueur.*
