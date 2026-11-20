# NOTES_CAPTURES_REFERENCE.md — Observations visuelles de WhatsApp Desktop (thème sombre)

> Source : 7 captures de la version récente de WhatsApp Desktop (écran 1920×1200, échelle Windows 150 % : **px WPF ≈ px de la capture ÷ 1,5**).
> Ce document décrit **uniquement l'apparence et la structure**. Aucun contenu personnel des captures (noms, numéros, messages, photos) ne doit être repris dans le code, les tests ou les données.
> **Priorité** : pour la disposition et le style visuel, ce document **prime sur la Partie 2**. Pour les comportements, l'ordre reste Partie 3 > 2 > 1.
> Les couleurs ci-dessous sont des **estimations à l'œil** : à confirmer avec une pipette (ex. PowerToys « Color Picker ») sur une vraie capture.

---

## 1. Fenêtre
- **Barre de titre personnalisée sombre** (≈ 38 px WPF) : logo + nom de l'app à gauche (chez nous : logo WhatsLAN + « WhatsLAN »), boutons réduire / agrandir / fermer à droite, style Windows 11.
- Trois zones sous la barre : **rail** (≈ 64 px), **panneau liste** (≈ 380 px), **zone centrale** (reste). Séparateurs fins de 1 px (gris très sombre).
- Le filigrane « Activer Windows » et la barre des tâches visibles sur les captures **ne font pas partie de l'application**.

## 2. Rail de gauche (de haut en bas)
1. **Discussions** (icône boîte de dialogue) + **badge vert** avec le nombre de non-lus.
2. **Appels** (téléphone).
3. **Statuts** (anneau).
4. **Chaînes** (bulle avec point) + **petit point vert** quand il y a du nouveau.
5. **Communautés** (trois personnes).
6. *Séparateur fin.*
7. **Archivées** (icône boîte avec flèche vers le bas) + badge vert du nombre de non-lus archivés. ⇒ **Archivées est une entrée du rail avec son propre panneau**, pas une ligne dans la liste.
8. *(Meta AI : ne pas reproduire.)*
- **En bas** : une icône galerie (médias / messages importants, optionnelle) puis **l'avatar de l'utilisateur = ouvre les Paramètres**.
- **État sélectionné** : un **disque rempli gris** derrière l'icône (≈ 40 px), pas de barre verte.
- **Survol/focus** : anneau vert fin autour de l'icône + **info-bulle** blanche à droite avec texte sombre (« Chaînes », « Communautés »).
- Icônes **outline** blanches/grises de 24 px.

## 3. Panneau « Discussions »
- En-tête : titre **« Chats / Discussions »** en grande taille (≈ 22 px WPF, police semi-grasse), à droite **menu ⋮** puis **gros bouton rond vert** (≈ 40 px) « Nouvelle discussion » avec une icône noire.
- **Champ de recherche en pilule** (rayon maximal, hauteur ≈ 40 px), icône loupe, texte gris « Rechercher ou démarrer une discussion ».
- **Puces de filtre** (pilules, hauteur ≈ 34 px) : **Toutes** (sélectionnée : fond vert foncé, texte vert), **Non lues** + compteur, **Favoris**, puis une **puce à flèche ▾** qui ouvre les autres filtres (Groupes, listes personnalisées).
- Liste verticale avec **barre de défilement fine** à droite.

## 4. Ligne de conversation
- Hauteur ≈ 76 px WPF ; **avatar rond ≈ 48 px** ; marges intérieures ≈ 16 px.
- Ligne 1 : **nom** (≈ 16 px, blanc, tronqué avec …) ; **heure à droite** (≈ 13 px, gris ; **verte si non lu**).
- Ligne 2 : aperçu du dernier message (≈ 14,5 px, gris) ; à droite : **badge vert rond avec nombre** si non lu, **icône épingle** si épinglée, **icône cloche barrée** si en sourdine.
- Préfixes d'aperçu : **✓✓ bleu / ✓ gris** pour mes messages ; « Nom : texte » dans les groupes ; icône autocollant, icône appel (« Appel vocal »), « Message supprimé » en italique avec icône ⊘ ; aperçu de réaction « A réagi 🙏 à : « … » » ; mention « (Vous) » après le nom de la discussion avec soi-même.
- **Ligne sélectionnée** : rectangle **arrondi (≈ 12 px) légèrement plus clair**, inséré avec une marge de ≈ 12 px par rapport aux bords du panneau.
- Dates : « Hier », jour de la semaine, puis date selon la langue (FR : jj/MM/aaaa).
- Avatar par défaut d'un groupe : disque sombre teinté avec **icône « groupe »** colorée ; d'une personne : disque avec initiale ou icône personne.

## 5. État vide de la zone centrale (aucune discussion ouverte)
Fond sombre uni, **quatre boutons ronds** (≈ 60 px, fond gris foncé, icône blanche) avec légende dessous :
**Envoyer un document**, **Ajouter un contact**, **Inviter un ami**, *(Meta AI : remplacer par* **Créer un groupe***)*. Pas de logo ni de grand texte.

## 6. Panneau « Archivées »
Titre **« Archivées »** (sans flèche retour puisqu'on y accède par le rail), sous le titre un **texte d'information gris** (les discussions restent archivées à la réception de nouveaux messages ; réglable dans Paramètres → Discussions), un **trait séparateur**, puis les lignes de conversation habituelles.

## 7. Discussion ouverte
- **En-tête** (≈ 64 px) : avatar rond 40 px (**anneau vert** s'il a un statut non vu), **nom en gras**, **sous-titre gris** (liste des membres tronquée pour un groupe ; « en ligne » / « écrit… » pour une personne). À droite :
  - discussion **individuelle** : **vidéo, appel, recherche, ⋮** (dans cet ordre) ;
  - **groupe** : **recherche, ⋮** seulement (pas d'appel).
- **Zone des messages** : fond noir très sombre (ou **fond d'écran choisi**, ex. motif sombre) ; **pastilles de date** centrées (« Hier », « Aujourd'hui », heure) en gris foncé arrondi ; **pastille « 32 messages non lus »** plus grande, centrée.
- **Bulles** : coins arrondis ≈ 8 px, fond gris très sombre ; dans un groupe : à gauche un **petit avatar rond avec initiale**, en haut de la bulle le **nom coloré** (préfixé de **« ~ »** quand la personne n'est pas dans mes contacts) ; **citation** : bloc plus sombre avec **barre verticale colorée** à gauche, nom coloré, extrait ; **heure en bas à droite** (gris ≈ 12 px) + **✓✓** pour mes messages. **Autocollants** : affichés sans bulle, avec heure en dessous.
- **Bouton de réaction** : petit disque avec smiley qui apparaît **à côté de la bulle au survol**.
- **Bouton « aller en bas »** : disque ≈ 44 px avec chevron ⌄, **badge vert** du nombre de nouveaux messages au-dessus.
- **Barre de saisie flottante en pilule** (pas de bande de fond, le fond d'écran reste visible derrière) : marges latérales ≈ 16 px, hauteur ≈ 50 px, rayon maximal ; de gauche à droite : **trombone (joindre)**, **icône autocollants/émojis**, champ « Écrivez un message », **micro** à droite (devient « envoyer » quand il y a du texte).
- Barre de défilement verticale fine à l'extrême droite.

## 8. « Discussions verrouillées »
En-tête : **flèche retour** + titre **« Discussions verrouillées »** ; champ de recherche pilule **« Rechercher dans les discussions verrouillées »** ; lignes identiques à la liste normale, avec ici beaucoup de **cloches barrées** (sourdine). La discussion sélectionnée s'ouvre à droite normalement.

## 9. Onglet Statuts
Panneau gauche : liste (avec **spinner de chargement** pendant la récupération). Zone centrale vide : **grande icône anneau**, titre **« Partager des statuts »**, sous-titre « Partagez des photos, vidéos et textes qui disparaissent après 24 heures. », deux boutons ronds : **Statut texte** (crayon) et **Photo et vidéo** (appareil photo).

## 10. Onglet Chaînes
Zone centrale vide : grande icône chaîne, titre **« Découvrir les chaînes »**, sous-titre, deux boutons ronds : **Créer une chaîne** (+) et **Explorer les chaînes** (grille 2×2). Panneau gauche avec spinner pendant le chargement.

## 11. Onglet Communautés
Panneau gauche : **bouton ✕** en haut. Zone centrale : grande icône « personnes », titre **« Créer des communautés »**, sous-titre (regrouper des membres en groupes thématiques et leur envoyer des annonces d'admin), et en bas la mention **🔒 « Vos messages personnels dans les communautés sont chiffrés de bout en bout »**.

## 12. Paramètres (s'ouvrent dans le **panneau liste**, pas dans une fenêtre séparée)
- En-tête = **nom d'affichage de l'utilisateur** ; **champ de recherche** (bordure verte de 2 px quand il a le focus).
- Liste d'entrées : **icône outline à gauche + titre + sous-titre gris** ; l'entrée survolée/sélectionnée a un fond arrondi plus clair.
  1. **Général** — Démarrage et fermeture
  2. **Profil** — Nom, photo de profil, nom d'utilisateur
  3. **Compte** — Notifications de sécurité, infos du compte
  4. **Confidentialité** — Contacts bloqués, messages temporaires
  5. **Discussions** — Thème, fond d'écran, paramètres de discussion
  6. **Vidéo et voix** — Caméra, microphone et haut-parleurs
  7. **Notifications** — Messages, groupes, sons
  8. **Raccourcis clavier** — Actions rapides
  9. **Aide et commentaires**
  10. *(en dessous : Se déconnecter)*
- Le détail de l'entrée choisie s'affiche dans la **zone centrale**.

## 13. Palette estimée (thème sombre)
| Élément | Valeur estimée |
|---|---|
| Fond fenêtre / panneaux | #161717 – #1A1B1B (gris neutre très sombre, **plus de bleu-vert marine**) |
| Zone de messages sans fond d'écran | #0B0B0B – #0E0F0F |
| Ligne sélectionnée / survol | #2A2C2C |
| Bulles (reçues et envoyées sur ces captures) | gris très sombre #242626 (la teinte des bulles envoyées est à vérifier) |
| Vert d'accent (badges, bouton « nouvelle discussion », heure non lue) | ≈ #21C063 |
| Puce sélectionnée | fond ≈ #0F3B2A, texte vert d'accent |
| Texte principal | #E9EDEF – #FFFFFF |
| Texte secondaire | ≈ #8D9696 |
| Coche lue | bleu ≈ #53BDEB |
→ Mettre ces valeurs dans `Themes/Dark.xaml` (clés existantes de la Partie 2) ; pour le thème clair, **reprendre les valeurs de la Partie 2** et ajuster après comparaison avec la version claire de WhatsApp Desktop.

## 14. Écarts avec les Parties 1, 2 et 3 → décisions
1. **Archivées** = entrée du rail avec panneau dédié (et pas une ligne en haut de la liste).
2. **Paramètres** = panneau dans la colonne liste, ouverts par l'**avatar en bas du rail** ; **ajouter la section « Vidéo et voix »** (choix caméra, micro, haut-parleurs, test micro, aperçu caméra) ; garder les autres sections de la Partie 3 §10 comme sous-pages.
3. **Barre de saisie** = pilule flottante, sans bande de fond ; icônes : trombone + autocollants/émojis, pas de bouton séparé « 📎 ➕ ».
4. **Sélection du rail** = disque plein ; **survol** = anneau vert + info-bulle.
5. **Puces de filtre** : Toutes / Non lues (compteur) / Favoris / ▾ (Groupes, listes).
6. **Bouton « Nouvelle discussion »** = gros rond vert dans l'en-tête de la liste.
7. **Groupes** : en-tête sans icônes d'appel ; pas de numéro de téléphone à côté des noms (WhatsLAN n'en a pas) → afficher le **pseudo seul**, préfixé de **« ~ »** si la personne n'est pas dans mes contacts.
8. **État vide central** : quatre boutons ronds (Envoyer un document, Ajouter un contact, Inviter un ami, Créer un groupe) à la place du logo + texte de la Partie 2 §6.11 ; le message « 🔒 Chiffré de bout en bout » reste en bas.
9. **Titre de fenêtre** : barre personnalisée sombre avec logo et « WhatsLAN ».
10. **Éléments à ne pas reproduire** : Meta AI, numéros de téléphone, « Inviter un ami » par SMS (→ remplacé par « partager le lien de l'installeur via le réseau »).

## 15. Rappel
Ces captures proviennent d'un compte réel : **ne jamais** en copier les noms, numéros, messages, photos, ni les montrer comme données de l'application.
