# PROMPT — PARTIE 2 : DESIGN, STATUTS, VERROUILLAGE ET INTERACTIONS (WhatsLAN)

> À coller **à la suite** de la Partie 1 (même conversation). Cette partie **complète et remplace** en cas de conflit.
> **Nom de l'application : `WhatsLAN` partout** (titre de fenêtre, écran d'accueil, notifications, installateur, dossier `%AppData%\WhatsLAN\`). Remplace toute occurrence de « WhatsLan ».
> **Logo** : utiliser le fichier `Assets/logo.svg` / `logo.png` fourni (voir section 1.4). Ne jamais dessiner ni recréer d'autre logo.

---

## 1. DESIGN SYSTEM (rendu fidèle à WhatsApp Desktop)

### 1.1 Palette (à mettre dans `Themes/Light.xaml` et `Themes/Dark.xaml` sous forme de `SolidColorBrush` clés)

| Clé | Clair | Sombre |
|---|---|---|
| `AppBackground` | #F0F2F5 | #111B21 |
| `PanelBackground` (listes, panneaux) | #FFFFFF | #111B21 |
| `PanelHeader` | #F0F2F5 | #202C33 |
| `ChatBackground` | #EFEAE2 (avec motif doodle à 6 % d'opacité) | #0B141A (motif à 4 %) |
| `BubbleOut` (envoyé) | #D9FDD3 | #005C4B |
| `BubbleIn` (reçu) | #FFFFFF | #202C33 |
| `TextPrimary` | #111B21 | #E9EDEF |
| `TextSecondary` | #667781 | #8696A0 |
| `Accent` (vert) | #00A884 | #00A884 |
| `AccentLight` (badges non lus, point en ligne) | #25D366 | #00A884 |
| `TickBlue` | #53BDEB | #53BDEB |
| `Divider` | #E9EDEF | #222D34 |
| `RowHover` | #F5F6F6 | #202C33 |
| `RowSelected` | #F0F2F5 | #2A3942 |
| `InputBar` | #F0F2F5 | #202C33 |
| `InputField` | #FFFFFF | #2A3942 |
| `SearchField` | #F0F2F5 | #202C33 |
| `Danger` | #EA0038 | #F15C6D |
| `LinkBlue` | #027EB5 | #53BDEB |
| `SystemPill` (messages système) | #FFFFFFE6 | #182229E6 |
| `StatusRingUnseen` | #25D366 | #00A884 |
| `StatusRingSeen` | #B1B8BC | #3B4A54 |

Couleurs des noms d'expéditeurs dans les groupes (une par membre, stable via hash du `userId`) : #1F7AA3, #B33F7B, #A5671E, #06A19B, #7C4DFF, #E0653D, #5E8A00, #C73E4A.

### 1.2 Typographie et dimensions (en pixels WPF)
- Police : **Segoe UI** (fallback Helvetica Neue). Mode sombre/clair identiques.
- Titre de conversation (liste) 17 ; dernier message 14 ; heure (liste) 12 ; texte de bulle 14,2 ; heure de bulle 11 ; nom d'expéditeur dans groupe 13 (semi-gras) ; en-tête de section 14 gras vert ; placeholder 15.
- Barre de navigation gauche (rail) : largeur **64**, icônes 24, fond `PanelHeader`.
- Colonne liste : largeur **400** par défaut (min 340, max 540, `GridSplitter` 1 px invisible, curseur ↔).
- En-têtes (liste et conversation) : hauteur **59**. Ligne de conversation : hauteur **72**, avatar **49** (cercle). Avatar d'en-tête 40. Avatar de statut 49 (anneau 3 px + espace 3 px).
- Barre de saisie : hauteur min **62** ; champ : hauteur min 42, coins **8**, padding 9×12.
- Bulles : coins **7,5** ; padding 6×7×8×9 ; espace entre deux bulles de même expéditeur **2** ; entre expéditeurs différents **12** ; largeur max **65 %** de la zone (min 240) ; ombre très légère (0 1 0.5 rgba(11,20,26,.13)).
- « Queue » de bulle : triangle 8×13 à l'angle supérieur (gauche pour reçu, droit pour envoyé), seulement sur la **première bulle** d'une série.
- Scrollbars fines (6 px), visibles au survol seulement, thumb #B2B5B8 (clair) / #374248 (sombre), coins arrondis.
- Animations : ouverture de panneau latéral 250 ms (cubic ease-out) ; apparition d'un nouveau message : fondu + translation 8 px, 150 ms ; surbrillance de saut vers un message : fond jaune #FFF3B0 → transparent en 2 s.

### 1.3 Icônes
Utiliser le package **MaterialDesignThemes** (`PackIcon`) ou **Segoe Fluent Icons**. Table d'icônes imposée : Discussions `MessageText`, Appels `Phone`, Statuts `CircleSlice8`/anneau (dessiné), Nouvelle discussion `SquareEditOutline`, Menu `DotsVertical`, Recherche `Magnify`, Joindre `Plus`/`Paperclip`, Émoji `EmoticonHappyOutline`, Micro `Microphone`, Envoyer `Send`, Verrou `Lock`, Étoile `Star`, Archive `ArchiveArrowDownOutline`, Épingle `Pin`, Sourdine `VolumeOff`, Coche simple `Check`, Double coche `CheckAll`, Horloge `ClockOutline`, Caméra `Video`, Appareil `Camera`, Document `FileDocument`, Galerie `Image`, Sondage `Poll`, Contact `Account`, Paramètres `Cog`, Retour `ArrowLeft`, Fermer `Close`.

### 1.4 Logo et nom
- L'application s'appelle **WhatsLAN**. Fichiers : `Assets/logo.svg` et `Assets/logo.png` (512×512) fournis séparément ; icône `.ico` multi-tailles (16, 32, 48, 256) pour l'exécutable, la barre des tâches et le tray.
- Utilisation : icône de la fenêtre, écran de bienvenue (logo 96 px), écran d'accueil de la zone de conversation (logo 80 px grisé à 50 %), toasts, « À propos ».
- Le texte à côté du logo est « **WhatsLAN** ». Aucune autre marque, aucun autre logo.

### 1.5 Disposition générale (4 zones)

```
┌────┬──────────────────────┬──────────────────────────────────────┐
│Rail│  Panneau liste       │  Zone de conversation                │
│ 💬 │  (Discussions /      │  (en-tête 59 / messages / saisie)    │
│ 📞 │   Appels / Statuts)  │                                      │
│ ◯  │                      │                                      │
│    │                      │                                      │
│ ⚙  │                      │                                      │
│ 👤 │                      │                                      │
└────┴──────────────────────┴──────────────────────────────────────┘
```
- **Rail (64 px)** : en haut, **Discussions** (badge vert = total non lus), **Appels**, **Statuts** (point vert si statuts non vus). En bas : **Paramètres** ⚙ et **Mon avatar** (ouvre le profil). L'onglet actif a un fond arrondi `RowSelected` et une barre verte de 3 px à gauche. Info-bulle sur chaque icône au survol (500 ms).
- Un panneau latéral droit (infos contact/groupe, recherche, messages étoilés, infos de message) **s'ouvre à droite** en largeur 400 et réduit la zone de messages ; si la fenêtre est < 1000 px, il la recouvre.

### 1.5 bis Squelette XAML de `MainWindow.xaml` (à respecter)

```xml
<Grid>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="64"/>                       <!-- Rail -->
    <ColumnDefinition Width="400" MinWidth="340" MaxWidth="540"/> <!-- Liste -->
    <ColumnDefinition Width="1"/>                        <!-- GridSplitter -->
    <ColumnDefinition Width="*"/>                        <!-- Conversation -->
    <ColumnDefinition Width="Auto"/>                     <!-- Panneau droit (0 ou 400) -->
  </Grid.ColumnDefinitions>
  <views:NavRail Grid.Column="0"/>
  <ContentControl Grid.Column="1" Content="{Binding CurrentListViewModel}"/> <!-- ChatList / CallsList / StatusList -->
  <GridSplitter Grid.Column="2" Width="1" HorizontalAlignment="Stretch"/>
  <ContentControl Grid.Column="3" Content="{Binding CurrentCenterViewModel}"/> <!-- ChatView / Accueil / StatusViewer -->
  <ContentControl Grid.Column="4" Content="{Binding SidePanelViewModel}"/>
</Grid>
```
`ChatView` = `Grid` à 3 lignes : en-tête (59), `ListBox` virtualisé des messages (`*`, `VirtualizingStackPanel`, `ScrollViewer` avec `CanContentScroll=True`), barre de saisie (`Auto`). Les bulles utilisent un `DataTemplateSelector` (texte, image, vidéo, document, vocal, sondage, système, supprimé, contact).

---

## 2. LE RAIL ET LES TROIS ONGLETS PRINCIPAUX

### 2.1 Onglet « Discussions »
Comme décrit en Partie 1 (sections 7 et 8), avec ces ajouts :
- **Ligne « Archivées »** en haut de la liste (icône archive + « Archivées » + nombre à droite) si au moins une discussion est archivée ; clic → ouvre la vue « Archivées » (flèche retour, liste des discussions archivées).
- **Ligne « Discussions verrouillées »** (voir section 4) juste dessous.
- Une discussion archivée qui reçoit un message **reste archivée** (si l'option « Garder les discussions archivées » est activée, défaut oui).
- **Discussion « Vous »** (messages à soi-même) : toujours disponible via « Nouvelle discussion » → première entrée « Vous (Moi) ». Elle n'a pas d'accusés, pas de statut, pas d'appel ; ses messages restent en local.

### 2.2 Onglet « Appels »
- En-tête : titre « Appels », boutons ✎ « Nouvel appel » (ouvre un sélecteur de contacts avec 📞 et 🎥 sur chaque ligne) et ⋮.
- Champ de recherche. Liste de l'**historique** : avatar, nom, ligne secondaire avec flèche ↙ rouge (manqué), ↗ vert (sortant), ↙ vert (entrant) + type (📞/🎥), heure à droite, et bouton 📞/🎥 pour rappeler. Les appels manqués ont le nom en **rouge**.
- Clic sur une ligne → panneau droit « Infos de l'appel » (liste des appels de ce contact avec durée, heure) + boutons Message / Rappeler. Clic droit → « Supprimer l'appel », « Bloquer ». Menu ⋮ → « Effacer l'historique des appels » (confirmation).
- Table `CallLog(id, peerUserId, direction, type, startUtc, durationSec, result[answered|missed|rejected|busy])`.

### 2.3 Onglet « Statuts » → voir section 3.

---

## 3. STATUTS (STORIES 24 h)

### 3.1 Principe P2P
Un statut est un contenu éphémère (texte, image, courte vidéo ≤ 30 s) visible **24 h** par les contacts autorisés. Sans serveur : à la publication, WhatsLAN envoie un paquet `STATUS_POST` (+ transfert du média via `FILE_*`) à chaque contact autorisé en ligne ; pour les hors-ligne, la publication est mise en file et livrée à leur reconnexion **tant que le statut n'a pas expiré**. Chaque récepteur stocke le statut localement avec `expiresUtc = postedUtc + 24h`. Une tâche de fond (chaque minute) supprime les statuts expirés (média compris).

Paquets : `STATUS_POST{statusId, kind[text|image|video], text, bgColor, fontIndex, caption, fileId?, postedUtc, expiresUtc}`, `STATUS_VIEWED{statusId, viewerId, viewedUtc}`, `STATUS_REACT{statusId, emoji}`, `STATUS_DELETE{statusId}`.
Tables : `Status(id PK, authorId, kind, body, bgColor, fontIndex, caption, attachmentId, postedUtc, expiresUtc, isMine, privacyMode)`, `StatusView(statusId, viewerId, viewedUtc)`, `StatusMute(authorId)`.

### 3.2 Liste des statuts (panneau liste, onglet Statuts)
1. En-tête : titre « Statuts », bouton **+** (menu : « Photo ou vidéo », « Texte »), bouton ⋮ (« Confidentialité du statut », « Statuts masqués »).
2. **Ligne « Mon statut »** : mon avatar avec **pastille + verte** si je n'ai rien publié ; sinon avatar entouré d'un **anneau segmenté** (un segment par statut actif, vert). Sous-titre : « Appuyer pour ajouter un statut » ou « il y a 12 minutes ». À droite : bouton 📷 (nouveau statut) et ⋮.
   - Clic sur l'avatar/ligne si vide → ouvre le **menu +** ; si j'ai des statuts → ouvre la **visionneuse** sur mes statuts.
   - ⋮ → « Supprimer le statut » n'existe pas ici : ouvrir mon statut → icône 🗑.
3. Section **« Récents »** : contacts avec statuts **non vus** ; anneau vert segmenté (segments pleins = non vus, gris = déjà vus) ; sous-titre = heure du dernier statut (« il y a 3 heures », « Aujourd'hui, 10:42 »). Tri : plus récent d'abord.
4. Section **« Vus »** : contacts dont tous les statuts sont vus ; anneau gris `StatusRingSeen`.
5. Section **« Statuts masqués »** (repliable) : contacts mis en sourdine.
6. Clic droit sur un contact : « Mettre en sourdine le statut / Réactiver », « Ouvrir la discussion ». Aucun statut → écran vide « Partagez des photos, vidéos et textes qui disparaissent après 24 heures. ».
7. Le rail affiche un **point vert** sur l'icône Statuts tant qu'il existe un statut non vu.

### 3.3 Créer un statut
**A. Texte** (menu + → « Texte ») : fenêtre plein panneau avec fond coloré (palette de 12 couleurs unies), texte centré modifiable en direct (placeholder « Saisissez un statut »), barre du haut : ✕ (fermer, confirmation « Abandonner ce statut ? » si texte non vide), 😊 (émojis), **T** (change la police parmi 5 : Sans, Serif, Norwester, Bryndan, Oswald — cycle à chaque clic), 🎨 (couleur de fond suivante). Bouton rond vert ➤ en bas à droite (désactivé si vide) → publie.
**B. Photo/vidéo** (« Photo ou vidéo ») : sélecteur de fichiers (jpg, png, mp4 ≤ 30 s ≤ 64 Mo ; sinon proposer de rogner la vidéo avec une frise de découpage). Écran d'édition : image plein cadre, outils en haut : ✂ rogner, ✏ dessiner (3 épaisseurs, 8 couleurs, gomme), **T** ajouter du texte déplaçable, 😊 autocollant émoji redimensionnable. En bas : champ « Ajouter une légende… » et bouton vert ➤ « Envoyer ».
**C. Confidentialité (au moment de publier)** : en bas de l'éditeur, un lien « Visible par : Mes contacts ▾ » ouvre un choix : **Mes contacts** (défaut) / **Mes contacts, sauf…** (cases à cocher, liste d'exclusion) / **Partager uniquement avec…** (cases à cocher). Case « Mémoriser ce choix ». Même écran dans Paramètres → Confidentialité → Statuts.
Après publication : retour à la liste, ma ligne « Mon statut » affiche l'anneau ; toast « Statut publié ».

### 3.4 Visionneuse de statuts (plein panneau central, fond #0B141A)
- **Haut** : barres de progression fines (une par statut du contact, la courante se remplit en **5 s** pour texte/image, durée réelle pour vidéo) ; sous elles : avatar 40, nom, heure (« Aujourd'hui, 10:42 »), boutons ⏸/▶, 🔇/🔊 (vidéo), ⋮, ✕.
- **Centre** : contenu centré (image/vidéo adaptées en hauteur, texte sur son fond coloré). Légende en bas du contenu sur dégradé noir.
- **Navigation** : clic sur le **tiers gauche** (ou ←) = statut précédent ; **tiers droit** (ou →) = suivant ; à la fin des statuts d'un contact → **enchaîne sur le contact suivant** de la liste « Récents » ; à la fin de tous → ferme. **Maintenir** le bouton de la souris appuyé = pause ; relâcher = reprise. **Espace** = pause/lecture. **Échap** ou ✕ = fermer. ◄► flèches visibles aux bords de l'écran au survol.
- Dès l'affichage d'un statut d'autrui : envoi de `STATUS_VIEWED` (respecte l'option « Confirmations de lecture » : si désactivée, pas d'envoi).
- **Bas (statut d'autrui)** : champ « Répondre… » (clic = met en pause) + 😊 ; Entrée envoie un **message dans la discussion directe** avec le statut cité (bandeau de citation : miniature + « Statut » + texte) ; petit cœur ❤ à droite → réaction rapide (`STATUS_REACT` ❤️ avec animation) ; flèche ⌃ « Réagir » ouvre 6 émojis.
- **⋮ (statut d'autrui)** : « Mettre en sourdine », « Message », « Signaler » (absent en LAN : ne pas l'afficher), « Bloquer ».
- **Bas (mon statut)** : zone « 👁 12 » (nombre de vues) ; clic → **tiroir « Vu par »** qui monte du bas : liste des contacts avec avatar, nom, heure de vue (et l'émoji de réaction à droite) ; « Aucune vue pour l'instant » si vide. Boutons : 🗑 **Supprimer** (confirmation « Supprimer ce statut ? » → envoie `STATUS_DELETE`), ➡ **Transférer**, ✏ **Ajouter un statut**.
- Si le contact est hors ligne ou que le média n'est pas encore arrivé : spinner et miniature floue.

### 3.5 Règles
- Un statut expiré disparaît partout (local) à `expiresUtc` ; la visionneuse se ferme proprement s'il expire pendant la lecture.
- Je ne vois pas les statuts de quelqu'un que j'ai bloqué ou qui m'exclut de son audience.
- Mes propres statuts supprimés/expirés disparaissent de « Vu par ».
- Option Paramètres « Statuts » → téléchargement automatique des statuts (oui/non).

---

## 4. VERROUILLAGE DE DISCUSSION ET VERROUILLAGE DE L'APPLICATION

### 4.1 Verrouiller une discussion
Entrée : menu contextuel d'une discussion → **« Verrouiller la discussion »** ; ou ⋮ de l'en-tête de conversation → « Verrouiller la discussion » ; ou panneau Infos du contact → ligne « 🔒 Verrouillage de discussion » avec interrupteur.
1. **Première utilisation** (aucun code créé) : dialogue **« Créer un code secret »** : champ **Code** (min 4 caractères, chiffres ou lettres, masqué, œil 👁 pour afficher), champ **Confirmer le code**, texte d'aide « Ce code protège vos discussions verrouillées. Si vous l'oubliez, vous devrez les supprimer. », case « Masquer les discussions verrouillées » (décochée par défaut) ; boutons « Annuler » / « Verrouiller ». Erreurs en rouge : « Le code doit contenir au moins 4 caractères », « Les codes ne correspondent pas ».
2. **Ensuite** : un simple dialogue de confirmation « Verrouiller la discussion avec X ? Les nouveaux messages de cette discussion seront cachés. » [Annuler] [Verrouiller].
3. Résultat : la discussion **disparaît de la liste principale** et rejoint le dossier **« Discussions verrouillées »**. Toast « Discussion verrouillée ». Si elle était ouverte, la zone centrale revient à l'écran d'accueil.
4. Le code est stocké **haché** (PBKDF2-SHA256, sel aléatoire 16 octets, 200 000 itérations) dans `Setting`. Le contenu des discussions verrouillées est **chiffré au repos** avec une clé dérivée du code (AES-256-GCM) — la base ne les révèle pas sans le code.

### 4.2 Le dossier « Discussions verrouillées »
- Ligne fixe en haut de la liste (sous « Archivées ») : icône 🔒 dans un cercle vert clair, texte « Discussions verrouillées », badge vert du **nombre total de non-lus**, sous-titre générique (jamais de nom/aperçu).
- **Clic** → dialogue **« Entrez votre code secret »** (champ masqué, œil, bouton « Déverrouiller », « Annuler », lien « Code oublié ? »). Entrée = valider.
  - Code correct → affiche la liste des discussions verrouillées (flèche retour, titre « Discussions verrouillées », mêmes lignes/menus qu'une liste normale + option « Retirer le verrouillage »).
  - Code incorrect → secousse du champ + « Code incorrect. 4 essais restants ». Après **5 échecs** : blocage 30 s (compte à rebours affiché « Réessayez dans 28 s »), puis 60 s, 5 min…
- **Verrouillage automatique** : le dossier se referme et exige à nouveau le code lorsque : on quitte le dossier, la fenêtre est réduite/mise en arrière-plan > **1 minute**, l'application est minimisée dans le tray, ou après 5 min d'inactivité.
- **Code oublié ?** → écran d'avertissement : « Vous devez supprimer toutes les discussions verrouillées pour réinitialiser le code. Cette action est irréversible. » [Annuler] [Supprimer les discussions verrouillées] (double confirmation en tapant « SUPPRIMER »). Puis le code est effacé et peut être recréé.
- **Option « Masquer les discussions verrouillées »** : la ligne du dossier disparaît de la liste ; on y accède en tapant **le code secret dans la barre de recherche** puis Entrée (le dossier s'ouvre alors directement). Réglable dans Paramètres → Confidentialité → Verrouillage de discussion.

### 4.3 Comportement réseau/notifications pour une discussion verrouillée
- Les messages continuent d'être reçus, accusés (✓✓), stockés (chiffrés).
- **Notifications** : toast générique « WhatsLAN — Nouveau message » (sans nom, sans aperçu, sans avatar), son normal ; ne s'affichent pas dans le tray/titre en détail ; le badge non-lus du rail inclut ces messages mais sans révéler la conversation.
- **Recherche globale** : ne retourne **jamais** de résultat provenant d'une discussion verrouillée (sauf si le dossier est déverrouillé et qu'on cherche à l'intérieur).
- Les statuts, appels et infos de l'appelant restent visibles normalement dans l'onglet Appels (option : masquer aussi les appels des contacts verrouillés).
- Les médias de ces discussions n'apparaissent pas dans la galerie globale et sont chiffrés sur disque.
- Les accusés « lu » ne sont envoyés qu'à l'ouverture effective de la discussion déverrouillée.

### 4.4 Déverrouiller une discussion (la retirer du dossier)
Dans le dossier déverrouillé → menu de la discussion → « Retirer le verrouillage » → confirmation → elle retourne dans la liste principale (re-déchiffrement des médias en tâche de fond avec barre de progression).

### 4.5 Verrouillage de l'application (écran de verrouillage global)
Paramètres → Confidentialité → **« Verrouillage de l'application »** : interrupteur ; si activé : demander un code (même dialogue de création) ou **Windows Hello** (`UserConsentVerifier`) si disponible ; délai : « Immédiatement / après 1 min / 15 min / 1 heure » d'inactivité ou de mise en veille.
- Quand verrouillée : fenêtre plein écran avec logo WhatsLAN, « WhatsLAN est verrouillé », champ code + bouton **Déverrouiller** (ou « Utiliser Windows Hello »), **Ctrl+L** verrouille manuellement. Les notifications deviennent génériques (comme 4.3). Les messages sont quand même reçus en arrière-plan.

---

## 5. PARAMÈTRES DE CONFIDENTIALITÉ (comme WhatsApp)

Paramètres → **Confidentialité** :
- **Dernière connexion et en ligne** : Tout le monde / Mes contacts / Personne ; « En ligne » : Tout le monde / Comme dernière connexion. Réciprocité : si je masque, je ne vois pas celle des autres.
- **Photo de profil**, **Info (à propos)** : Tout le monde / Mes contacts / Personne.
- **Statuts** : Mes contacts / sauf… / uniquement avec… (voir 3.3.C).
- **Confirmations de lecture** (interrupteur) : désactivé → pas de ✓✓ bleu (ni envoi ni réception), les vocaux et statuts ne sont plus signalés « vus ».
- **Messages temporaires par défaut** : Désactivé / 24 h / 7 jours / 90 jours (appliqué aux nouvelles discussions).
- **Groupes : qui peut m'ajouter** : Tout le monde / Mes contacts / Mes contacts sauf… (si refusé, l'invitation arrive comme demande à accepter).
- **Contacts bloqués** : liste avec bouton « Débloquer » à droite de chaque ligne, bouton « Ajouter » en haut.
- **Verrouillage de discussion** et **Verrouillage de l'application** (section 4).
- Les valeurs sont **diffusées** dans `HELLO`/`PROFILE_UPDATE` sous forme de règles que les autres pairs respectent (côté récepteur : on n'affiche pas ce que le contact n'autorise pas).

---

## 6. FONCTIONNALITÉS WHATSAPP SUPPLÉMENTAIRES À IMPLÉMENTER

### 6.1 Mise en forme du texte
Dans les messages : `*gras*`, `_italique_`, `~barré~`, `` `code en ligne` ``, ```` ```bloc de code``` ````, listes `- item` / `1. item`, citation `> texte`. Le rendu se fait à l'affichage (les symboles disparaissent). Dans le champ de saisie : sélection de texte → mini-barre flottante **G / I / S / <>** ; raccourcis **Ctrl+B / Ctrl+I / Ctrl+Maj+X (barré) / Ctrl+Maj+M (monospace)**.

### 6.2 « Lire la suite »
Un message de plus de 1 200 caractères ou 20 lignes est tronqué avec « **Lire la suite** » (bleu) ; clic = déplie sur place.

### 6.3 Aperçu de lien
Si le message contient une URL, une carte d'aperçu (titre, description, miniature) s'affiche dans la bulle ; clic = ouvre le navigateur. Dans le champ de saisie, l'aperçu apparaît **avant l'envoi** avec un ✕ pour le retirer. En LAN sans Internet : pas d'aperçu (aucune erreur).

### 6.4 Photo/vidéo « Vue unique »
Dans l'aperçu d'envoi de médias : icône **①** « Vue unique ». Le destinataire voit « 📷 Photo • Ouvrir » ; une seule ouverture, puis « Ouvert » (le média est supprimé du disque). Impossible de capturer/transférer/copier/enregistrer ; l'expéditeur voit « Ouvert » avec heure.

### 6.5 Sondages
Menu 📎 → **Sondage** : fenêtre « Créer un sondage » : **Question** (255 max), **Options** (2 à 12 ; ligne vide ajoutée automatiquement ; ✕ pour retirer), interrupteur « Autoriser plusieurs réponses ». Bouton « Envoyer ». Dans la bulle : question en gras, options avec **cercles/cases** et barre de progression avec nombre de votes, total « 5 votes ». Clic sur une option = vote (`POLL_VOTE{messageId, optionIds[]}`), re-clic = retire ; clic sur « Voir les votes » → liste par option. Les votes sont diffusés à tous les membres.

### 6.6 Partage de contact et de position
- **Contact** : sélecteur multiple de contacts WhatsLAN → carte « Nom + avatar », boutons « Message » et « Ajouter ».
- **Position** : non pris en charge (omettre l'entrée de menu).

### 6.7 Épingler un message
Menu d'un message → **Épingler** (durée : 24 h / 7 jours / 30 jours). Un bandeau sous l'en-tête affiche le message épinglé (barre verte à gauche, extrait, 📌) ; clic = saute au message ; si plusieurs : indicateur à points, clic alterne. Menu du bandeau : « Désépingler ». Message système : « Alice a épinglé un message ».

### 6.8 Listes de diffusion
Menu ⋮ → **Nouvelle liste de diffusion** : sélection de contacts (≥ 2) + nom. Le message est envoyé **individuellement** à chaque destinataire (leurs discussions directes) ; chez la liste, les réponses n'apparaissent que dans les discussions privées. Icône 📢 sur l'avatar. Infos : « Modifier la liste », « Supprimer ».

### 6.9 Messages étoilés
Menu ⋮ → **Messages importants** (ou panneau droit) : liste groupée par discussion avec date, clic = ouvre la conversation et saute au message ; menu ⋮ → « Tout désétoiler ».

### 6.10 Archives
Écran « Archivées » avec flèche retour, bouton ⋮ → « Désarchiver tout ». Menu d'une discussion → « Désarchiver ».

### 6.11 Messages de bienvenue et états vides
- Zone de conversation sans sélection : logo, « WhatsLAN pour Windows », « Envoyez et recevez des messages en réseau local sans serveur. », ligne basse « 🔒 Chiffré de bout en bout ».
- Nouvelle discussion vide : pastille système « 🔒 Les messages sont chiffrés de bout en bout. Seuls vous et X pouvez les lire. ».
- Liste vide : « Aucune discussion. Cliquez sur ✎ pour commencer. ».

---

## 7. TABLE « QUE SE PASSE-T-IL QUAND ON CLIQUE SUR… »

| Élément | Clic gauche | Clic droit / long | Double-clic / autre |
|---|---|---|---|
| Mon avatar (rail) | Ouvre panneau Profil | — | — |
| Icône Discussions/Appels/Statuts | Change d'onglet, mémorise la sélection | — | Re-clic sur Discussions : remonte en haut de la liste |
| Avatar d'un contact (liste) | Ouvre la discussion | Menu de la discussion | — |
| Avatar d'un contact (en-tête) | Ouvre « Infos du contact » à droite | — | — |
| Avatar de contact (grande taille, dans infos) | Visionneuse de la photo de profil | « Enregistrer sous… » | — |
| Nom du contact (en-tête) | Infos du contact | — | — |
| Bulle de message | Sélectionne (surbrillance) | Menu contextuel du message | Double-clic : répondre |
| Citation dans une bulle | Saute au message cité + clignotement | — | — |
| Lien dans une bulle | Ouvre le navigateur (confirmation si domaine inconnu : « Ouvrir ce lien ? ») | « Copier le lien », « Ouvrir » | — |
| Image dans une bulle | Visionneuse | « Enregistrer sous… », « Transférer » | — |
| Nom d'expéditeur (groupe) | Insère @nom dans le champ | — | — |
| Pastille de réaction | Liste des réactions | — | — |
| Coche ✓✓ | Aucun (info-bulle « Lu à 14:32 ») | — | — |
| Date collante | Ouvre le calendrier « Aller à la date » | — | — |
| Bouton ⌄ (retour en bas) | Défile en bas | — | — |
| Icône 📎 | Menu des pièces jointes | — | — |
| Icône 🎤 | Démarre l'enregistrement | — | Maintenu = push-to-talk |
| Icône ➤ | Envoie | — | — |
| Champ de saisie | Focus | Menu : Couper/Copier/Coller/Tout sélectionner/Mise en forme | — |
| Ligne « Archivées » | Ouvre l'écran des archives | — | — |
| Ligne « Discussions verrouillées » | Demande le code | — | — |
| Ligne de statut d'un contact | Ouvre la visionneuse | Mettre en sourdine | — |
| Ligne d'appel | Panneau infos d'appel | Supprimer l'appel | — |
| Icône ⋮ (liste) | Menu principal | — | — |
| Notification toast | Ouvre l'app + la discussion | — | Bouton « Répondre » dans le toast : champ de réponse rapide |
| Icône tray | Restaure/réduit | Menu : Ouvrir / Verrouiller / Quitter | — |
| ✕ de la fenêtre | Réduit dans le tray (ou quitte selon option) | — | — |
| Titre de fenêtre | — | — | Double-clic : maximise |

Les **info-bulles** (délai 500 ms) sont obligatoires sur toutes les icônes. Tous les menus se ferment par Échap ou clic extérieur. Tous les boutons ont les états : normal, survol (`RowHover`), pressé (assombri 8 %), désactivé (opacité 40 %), focus clavier (anneau vert 2 px).

---

## 8. PAQUETS ET TABLES AJOUTÉS (en plus de la Partie 1)

Paquets : `STATUS_POST`, `STATUS_VIEWED`, `STATUS_REACT`, `STATUS_DELETE`, `POLL_CREATE` (porté par `MSG kind=poll`), `POLL_VOTE`, `PIN`, `UNPIN`, `VIEW_ONCE_OPENED`, `PLAYED` (vocal écouté), `PRIVACY_UPDATE`, `BROADCAST` (traité comme N `MSG`).
Tables : `Status`, `StatusView`, `StatusMute`, `CallLog`, `LockedChat(conversationId PK)`, `Poll(messageId, question, multi)`, `PollOption(id, messageId, text)`, `PollVote(optionId, userId)`, `PinnedMessage(conversationId, messageId, untilUtc)`, `BroadcastList(id, name)`, `BroadcastMember(listId, userId)`, `PrivacySetting(key, value, exceptJson)`.
Settings ajoutés : `lock.hash`, `lock.salt`, `lock.hideFolder`, `applock.enabled`, `applock.delay`, `theme`.

---

## 9. PHASES SUPPLÉMENTAIRES (à ajouter à la liste de la Partie 1)

- **Phase 0 — Design** (à faire en tout premier) : `Themes/Light.xaml` et `Dark.xaml`, styles de boutons/listes/scrollbars, `MainWindow.xaml` avec rail + 3 zones, `MessageBubble`, `ChatListItem`, commutateur de thème, avec **données factices** pour valider le visuel sans réseau.
- **Phase 13 — Appels (historique)** : onglet Appels, `CallLog`.
- **Phase 14 — Statuts** : liste, création texte/image/vidéo, visionneuse, vues, réactions, expiration 24 h.
- **Phase 15 — Verrouillage** : code secret, dossier verrouillé, chiffrement au repos, notifications génériques, verrouillage de l'app.
- **Phase 16 — Extras** : mise en forme, aperçu de lien, vue unique, sondages, message épinglé, listes de diffusion, confidentialité, messages étoilés/archives.

---

## 10. CRITÈRES D'ACCEPTATION ADDITIONNELS

- Comparaison visuelle côte à côte avec une capture de WhatsApp Desktop : proportions, couleurs, espacements indiscernables à l'œil.
- Basculer clair/sombre met à jour **tout** instantanément sans redémarrage.
- Un statut publié apparaît chez 2 autres instances en < 5 s, disparaît à 24 h (tester en forçant l'horloge à +24 h), et « Vu par » se met à jour.
- Une discussion verrouillée disparaît de la liste, ne sort dans aucune recherche, la notification est générique, le dossier exige le code, 5 mauvais codes → blocage 30 s.
- Après redémarrage, la base ne contient aucun texte lisible pour une discussion verrouillée (vérifier avec un éditeur SQLite).
- Chaque ligne du tableau de la section 7 produit exactement le comportement indiqué.

---

**Confirme que tu as intégré cette Partie 2 en 8 lignes max, puis attends « Phase 0 ».**
