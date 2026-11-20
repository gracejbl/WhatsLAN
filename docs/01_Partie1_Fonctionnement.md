# PROMPT — Clone de WhatsApp Desktop en C# (réseau local, 100 % P2P)

## 0. CONSIGNES POUR L'IA QUI LIT CE PROMPT

Tu es un développeur C# senior. Tu dois m'aider à construire, étape par étape, une application de bureau type **WhatsApp Desktop** qui fonctionne **uniquement en réseau local (LAN)**, **sans serveur central** (pair-à-pair).

Règles de réponse :
1. Je te demanderai de travailler **phase par phase** (voir section 14). Ne génère pas tout d'un coup. Attends que je dise « Phase N ».
2. Chaque réponse contient du **code complet et compilable** (pas de « ... », pas de pseudo-code), avec les `using`, les namespaces et les chemins de fichiers.
3. Respecte **exactement** les comportements décrits ci-dessous (boutons, états, messages d'erreur, textes affichés). Si un détail manque, choisis le comportement de WhatsApp Desktop et signale-le en une ligne.
4. Après chaque phase, donne : (a) la liste des fichiers créés/modifiés, (b) comment tester, (c) les pièges connus.
5. Commente le code en français. Noms de classes et de méthodes en anglais ou français, mais de façon cohérente.

---

## 1. CONTEXTE ET CONTRAINTES TECHNIQUES

- Langage : **C# 12**, **.NET 8**.
- UI : **WPF** avec le pattern **MVVM** (CommunityToolkit.Mvvm).
- Réseau : `System.Net.Sockets` uniquement (UDP + TCP). **Aucun serveur, aucune API cloud, aucun SignalR.**
- Persistance locale : **SQLite** (`Microsoft.Data.Sqlite`) dans `%AppData%\WhatsLAN\`.
- Audio : **NAudio** (enregistrement micro, lecture, appels).
- Vidéo : **OpenCvSharp4** (capture webcam) ou **AForge.Video.DirectShow** ; encodage JPEG image par image.
- Chiffrement : `System.Security.Cryptography` (**ECDH P-256** pour l'échange de clé, **AES-256-GCM** pour les messages).
- JSON : `System.Text.Json`.
- Notifications : `Microsoft.Toolkit.Uitoolkit` / `Microsoft.Toolkit.Win32.UI.Notifications` (toasts Windows) ou `NotifyIcon` WinForms.
- Un seul exécutable à lancer sur chaque PC ; on peut lancer **plusieurs instances sur le même PC** en passant `--port 50011 --data C:\temp\userB` en argument pour tester.
- Fonctionne sur Windows 10/11, sur un même sous-réseau (Wi-Fi ou câble).

Ports par défaut :
- UDP **50000** : découverte (broadcast).
- TCP **50001** (ou premier port libre à partir de 50001) : messages, fichiers.
- UDP **50100–50199** : flux audio/vidéo des appels (port choisi dynamiquement et annoncé dans la signalisation).

---

## 2. ARCHITECTURE DU PROJET

```
WhatsLAN/
  WhatsLAN.sln
  WhatsLAN.App/                (WPF)
    Views/                     MainWindow, ChatView, ContactListView, ProfileView,
                               NewGroupDialog, GroupInfoPanel, CallWindow, ImageViewer,
                               SettingsView, SearchPanel, EmojiPicker
    ViewModels/                MainViewModel, ChatViewModel, ContactViewModel,
                               MessageViewModel, GroupViewModel, CallViewModel, ...
    Controls/                  MessageBubble, VoiceMessagePlayer, AvatarControl,
                               TypingIndicator, ReplyPreview
    Converters/ Resources/ Themes/
  WhatsLAN.Core/               (bibliothèque, sans UI)
    Models/                    Peer, Message, Conversation, Group, Attachment, CallSession
    Network/                   DiscoveryService, TcpServer, PeerConnection, ConnectionManager,
                               PacketSerializer, FileTransferService, CallService
    Security/                  KeyManager, CryptoService
    Storage/                   Database, MessageRepository, ConversationRepository, SettingsRepository
    Services/                  MessageService, PresenceService, ReceiptService, NotificationService,
                               AudioRecorder, AudioPlayer, VideoCapture
```

Règle : `Core` ne référence jamais WPF. Les ViewModels écoutent des événements de `Core` et mettent à jour l'UI via `Dispatcher`.

---

## 3. IDENTITÉ ET PROFIL

### 3.1 Premier lancement (écran de bienvenue)
- Si aucune base n'existe : afficher une fenêtre « Bienvenue sur WhatsLAN » avec :
  - Un cercle d'avatar cliquable (gris avec icône personne). Clic → ouvre un sélecteur de fichier (png, jpg), recadre en carré, redimensionne à 256×256.
  - Champ **Pseudo** (obligatoire, 1 à 25 caractères, trim, pas uniquement des espaces).
  - Champ **Statut/À propos** (facultatif, 139 caractères max, défaut : « Disponible »).
  - Bouton **Commencer** : désactivé tant que le pseudo est vide. Au clic : génère un **GUID utilisateur** permanent (`UserId`), génère la paire de clés ECDH, enregistre le profil, ouvre la fenêtre principale.
  - Erreur sous le champ si pseudo invalide : « Entrez un pseudo (25 caractères max). »

### 3.2 Panneau Profil (accessible en cliquant sur son propre avatar en haut à gauche)
- Panneau qui glisse depuis la gauche (animation 200 ms) avec : grand avatar (clic → changer la photo, survol → overlay « Changer la photo de profil » + icône caméra), pseudo (icône crayon → devient éditable, **Entrée** valide, **Échap** annule), statut (idem).
- Toute modification est **diffusée immédiatement** à tous les contacts en ligne par un paquet `ProfileUpdate`.
- Flèche retour en haut à gauche ferme le panneau.

---

## 4. PROTOCOLE RÉSEAU (SPÉCIFICATION COMPLÈTE)

### 4.1 Découverte des pairs (UDP broadcast)
- Toutes les **3 secondes**, chaque instance envoie sur `255.255.255.255:50000` (et sur l'adresse de broadcast du sous-réseau) un datagramme JSON :
  `{"t":"HELLO","userId":"GUID","name":"Pseudo","tcpPort":50001,"avatarHash":"sha256","status":"Disponible","pubKey":"base64"}`
- Une instance ignore ses propres paquets (même `userId`).
- À réception d'un `HELLO` : si le `userId` est inconnu → ajouter le contact en ligne ; sinon mettre à jour `lastSeen`, IP, port.
- Un contact est **hors ligne** si aucun `HELLO` ni aucun paquet TCP n'est reçu depuis **10 secondes**. À ce moment : passer son statut à « hors ligne », enregistrer `lastSeen`, fermer la connexion TCP.
- Au démarrage : envoyer 3 `HELLO` rapides (200 ms d'intervalle). À la fermeture propre : envoyer `{"t":"BYE","userId":"..."}`.
- Si l'avatarHash du contact diffère de celui en cache : demander l'avatar par TCP (`AvatarRequest`).

### 4.2 Connexion TCP
- Chaque instance a un `TcpListener` sur son port.
- Une connexion **persistante** par contact. Pour éviter les doublons (A se connecte à B en même temps que B à A) : c'est le pair dont le `userId` est le plus petit (comparaison de chaînes) qui initie la connexion ; l'autre attend.
- Reconnexion automatique avec back-off : 1 s, 2 s, 4 s, 8 s, max 15 s.
- **Handshake** à l'ouverture : chaque côté envoie `Handshake{userId, name, pubKey}` ; on calcule le secret partagé ECDH, on dérive la clé AES-256 avec HKDF-SHA256. Tous les paquets suivants sont chiffrés.

### 4.3 Trame (framing)
`[4 octets longueur big-endian][1 octet type][1 octet flags][nonce 12 octets][payload chiffré AES-GCM][tag 16 octets]`
- flags : bit0 = compressé, bit1 = chiffré.
- Taille max d'une trame : 1 Mo. Les fichiers sont découpés en morceaux de 64 Ko.

### 4.4 Types de paquets
| Type | Direction | Contenu principal |
|---|---|---|
| `MSG` | A→B | id (GUID), conversationId, from, to, texte, replyToId?, timestamp UTC, kind (text/image/file/voice/system) |
| `ACK_DELIVERED` | B→A | messageId, timestamp |
| `ACK_READ` | B→A | liste de messageId, timestamp |
| `TYPING` | A→B | conversationId, state (start/stop/recording) |
| `EDIT` | A→B | messageId, nouveau texte, editedAt |
| `DELETE` | A→B | messageId, mode (forEveryone) |
| `REACT` | A→B | messageId, emoji (ou vide pour retirer) |
| `FILE_OFFER` | A→B | fileId, nom, taille, mime, sha256, messageId, miniature (base64 pour image) |
| `FILE_ACCEPT` / `FILE_REJECT` | B→A | fileId |
| `FILE_CHUNK` | A→B | fileId, index, données |
| `FILE_DONE` | les deux | fileId, ok/erreur |
| `PROFILE_UPDATE` | A→tous | name, status, avatarHash |
| `AVATAR_REQUEST/RESPONSE` | | hash, données |
| `GROUP_CREATE/UPDATE/LEAVE` | | groupId, nom, avatar, membres, admins, version |
| `CALL_INVITE/ACCEPT/REJECT/END` | | callId, type (audio/video), port UDP média, clé de session |
| `PING/PONG` | | pour détecter une connexion morte (toutes les 5 s) |

### 4.5 Livraison hors-ligne
- Si le destinataire est hors ligne : le message est stocké avec `status = Pending` (icône horloge ⏱). Dès que le contact revient en ligne, tous les messages `Pending` sont renvoyés **dans l'ordre**, avec dé-doublonnage côté récepteur (par `messageId`).

---

## 5. BASE DE DONNÉES (SQLite)

```
Profile(userId, name, status, avatarPath, privateKey, publicKey)
Contact(userId PK, name, status, avatarPath, avatarHash, lastIp, lastPort, lastSeenUtc, pubKey, isBlocked)
Conversation(id PK, type [direct|group], peerUserId?, groupId?, pinned, muted, archived,
             lastMessageId, unreadCount, draftText, updatedUtc)
GroupInfo(groupId PK, name, avatarPath, createdBy, createdUtc, version)
GroupMember(groupId, userId, isAdmin, joinedUtc)
Message(id PK, conversationId, senderId, kind, body, replyToId, status
        [Pending|Sent|Delivered|Read|Failed], createdUtc, editedUtc, isDeleted,
        isStarred, attachmentId?, durationMs?)
Reaction(messageId, userId, emoji, PRIMARY KEY(messageId,userId))
Attachment(id PK, messageId, fileName, localPath, size, mime, sha256, thumbPath, width, height, transferState)
MessageReceipt(messageId, userId, deliveredUtc, readUtc)   -- utile pour les groupes
Setting(key PK, value)
```
Index sur `Message(conversationId, createdUtc)` et recherche plein texte via **FTS5** sur `Message.body`.

---

## 6. FENÊTRE PRINCIPALE — DISPOSITION

Fenêtre 1100×700 (minimum 800×500), thème **clair/sombre**, deux colonnes comme WhatsApp Desktop :

```
┌───────────────┬────────────────────────────────────────────┐
│ [Avatar] ⋮ ✎  │ [Avatar] Nom du contact      🔍  📞  🎥  ⋮ │
│ [🔍 Rechercher]│  statut : en ligne / écrit… / vu à 14:32   │
│ [Tous|Non lus|│───────────────────────────────────────────│
│  Groupes]     │                                            │
│ ─ Conversation│     zone des messages (scroll)             │
│ ─ Conversation│                                            │
│ ─ ...         │                                            │
│               │────────────────────────────────────────────│
│               │ [😊] [📎]  [ Écrire un message ]  [🎤/➤]  │
└───────────────┴────────────────────────────────────────────┘
```
Largeur colonne gauche : 360 px (redimensionnable de 280 à 480 px par glissement de la séparation). Si aucune conversation n'est sélectionnée, la zone droite affiche l'écran d'accueil : logo, « WhatsLAN pour Windows », « Envoyez et recevez des messages sur votre réseau local », et la mention « 🔒 Chiffré de bout en bout ».

---

## 7. COLONNE GAUCHE — DÉTAIL DE CHAQUE ÉLÉMENT

### 7.1 En-tête
- **Avatar** (clic → panneau Profil, voir 3.2). Pastille verte si connecté au réseau.
- **Bouton « Nouvelle discussion »** (icône ✎) : ouvre un panneau « Nouvelle discussion » avec : champ de recherche, entrées fixes « Nouveau groupe » (icône groupe), puis la liste alphabétique de **tous les contacts détectés** (en ligne en premier avec point vert). Clic sur un contact → ouvre ou crée la conversation et revient à l'écran principal.
- **Menu ⋮** : « Nouveau groupe », « Messages importants (étoilés) », « Archivées », « Paramètres », « Se déconnecter / Quitter ». Chaque entrée ouvre le panneau correspondant.

### 7.2 Barre de recherche
- Champ « Rechercher ou démarrer une discussion ». Icône loupe ; devient une flèche retour quand le champ a le focus ; bouton ✕ pour effacer.
- La saisie filtre en direct (debounce 200 ms) : section « Discussions » (nom de contact/groupe qui correspond) puis section « Messages » (résultats FTS avec l'extrait, mot recherché en **gras/surligné**, date). Clic sur un résultat message → ouvre la conversation et **fait défiler jusqu'au message** en le faisant clignoter en jaune 2 s.
- **Échap** vide le champ et ferme la recherche.

### 7.3 Filtres (puces)
- « Toutes », « Non lues », « Favoris », « Groupes ». Une seule active à la fois ; la puce active est verte.

### 7.4 Ligne de conversation
Contenu : avatar 49 px (pastille verte « en ligne »), nom (gras, tronqué avec …), dernier message (une ligne, gris, tronqué ; préfixé de l'icône d'accusé ✓/✓✓ si c'est moi l'expéditeur ; préfixé de « Vous : » dans les groupes si c'est moi, ou « Nom : » sinon ; « 🎤 0:23 » pour un vocal, « 📷 Photo » pour une image, « 📄 nomfichier.pdf » pour un fichier, « 🚫 Ce message a été supprimé » si supprimé), heure à droite (HH:mm aujourd'hui, « Hier », jour de semaine dans la semaine, sinon jj/MM/aaaa), **badge vert** du nombre de non-lus (affiche « 99+ » au-delà), icône 📌 si épinglée, 🔇 si en sourdine.
- Si un brouillon existe : « Brouillon : … » en vert à la place du dernier message.
- Si le contact est en train d'écrire : « écrit… » en vert ; en train d'enregistrer : « enregistre un message vocal… ».
- Tri : épinglées d'abord, puis par `updatedUtc` décroissant.
- **Clic gauche** : ouvre la conversation (surbrillance grise) et met le compteur de non-lus à 0 (envoie les `ACK_READ`).
- **Clic droit / survol → flèche ⌄** : menu contextuel : « Archiver », « Mettre en sourdine » (sous-menu : 8 heures / 1 semaine / Toujours) ou « Réactiver le son », « Épingler » (max 3 ; sinon message « Vous ne pouvez épingler que 3 discussions »), « Marquer comme lu / non lu », « Ajouter aux favoris », « Vider la discussion » (confirmation), « Supprimer la discussion » (confirmation : « Supprimer cette discussion ? » [Annuler] [Supprimer]), « Bloquer » (contacts directs).

---

## 8. ZONE DE CONVERSATION — EN-TÊTE

- **Avatar + nom** (clic → panneau « Infos du contact » ou « Infos du groupe » à droite, 360 px).
- **Sous-titre** (dynamique, ordre de priorité) : « écrit… » > « enregistre un message vocal… » > « en ligne » > « vu aujourd'hui à HH:mm » / « vu hier à HH:mm » / « vu le jj/MM/aaaa ». Pour un groupe : liste des membres séparés par des virgules (tronquée avec …), ou « X écrit… ».
- **🔍 Rechercher** : ouvre la barre de recherche dans la conversation (en haut de la zone de messages) avec champ, compteur « 3 sur 12 », flèches ▲ ▼ (Entrée = suivant, Maj+Entrée = précédent), ✕ pour fermer (Échap aussi). Les occurrences sont surlignées en jaune, l'occurrence courante en orange.
- **📞 Appel audio** et **🎥 Appel vidéo** : désactivés (grisés, infobulle « Contact hors ligne ») si le contact est hors ligne ou si c'est un groupe. Voir section 12.
- **Menu ⋮** : « Infos du contact/groupe », « Sélectionner les messages », « Mettre en sourdine », « Messages temporaires » (désactivé / 24 h / 7 jours / 90 jours — suppression automatique), « Fermer la discussion » (Échap), « Vider la discussion », « Supprimer la discussion », « Bloquer / Débloquer ».

---

## 9. ZONE DES MESSAGES

### 9.1 Mise en page
- Fond avec motif discret. Messages envoyés : bulles **vertes**, alignées à droite ; reçus : bulles **blanches**, à gauche (en mode sombre : vert foncé / gris foncé). Petite « queue » sur la première bulle d'une série.
- Largeur maximale d'une bulle : 65 % de la zone. Coins arrondis 8 px. Le texte revient à la ligne, conserve les sauts de ligne et rend les liens `http(s)://` cliquables (ouvre le navigateur).
- En bas à droite de chaque bulle : **heure HH:mm** (gris, petit), mention « modifié » si édité, et, pour mes messages, **l'icône d'accusé**.
- **Séparateurs de date** centrés (« AUJOURD'HUI », « HIER », « lundi », « 12 novembre 2026 »), collants en haut pendant le scroll.
- Dans les groupes : le **nom de l'expéditeur** s'affiche en couleur (une couleur stable par membre) au-dessus de la première bulle de sa série, avec petit avatar à gauche.
- **Messages système** centrés dans une pastille grise : « 🔒 Les messages sont chiffrés de bout en bout… », « Alice a créé le groupe », « Bob a quitté le groupe », « Vous avez bloqué ce contact », etc.

### 9.2 Comportement du défilement
- À l'ouverture : positionné sur le premier message non lu (une pastille « 3 MESSAGES NON LUS » est insérée avant), sinon tout en bas.
- Chargement paresseux : 50 derniers messages, puis 50 de plus quand on remonte jusqu'en haut (virtualisation du `ItemsControl`).
- Si l'utilisateur est en bas : un nouveau message fait défiler automatiquement. S'il a remonté : un **bouton rond ⌄** apparaît en bas à droite avec un badge du nombre de nouveaux messages ; clic → retour en bas animé.

### 9.3 Accusés de réception (icônes sous la bulle)
| État | Icône | Quand |
|---|---|---|
| Pending | ⏱ horloge grise | message en attente (destinataire hors ligne / pas encore envoyé) |
| Sent | ✓ gris | écrit sur le socket TCP |
| Delivered | ✓✓ gris | `ACK_DELIVERED` reçu |
| Read | ✓✓ **bleu** | `ACK_READ` reçu |
| Failed | ⚠ rouge | échec ; clic → « Renvoyer » |
- `ACK_DELIVERED` est envoyé dès réception et enregistrement du message.
- `ACK_READ` est envoyé quand : la conversation est ouverte **et** la fenêtre est au premier plan **et** le message est visible à l'écran. Si la fenêtre est en arrière-plan, l'accusé part au retour du focus.
- **Groupes** : ✓✓ gris quand **tous** les membres l'ont reçu ; ✓✓ bleu quand **tous** l'ont lu. Le panneau « Infos du message » (menu contextuel) liste, membre par membre, « Lu à » / « Remis à » / « En attente ».
- Option Paramètres « Confirmations de lecture » : si désactivée, on n'envoie pas `ACK_READ` et on ne reçoit pas ceux des autres (comme WhatsApp).

### 9.4 Menu contextuel d'un message (clic droit ou flèche ⌄ au survol)
- **Répondre**, **Réagir** (barre de 6 émojis 👍 ❤️ 😂 😮 😢 🙏 + bouton « + » vers le sélecteur complet), **Copier** (texte), **Transférer** (ouvre un sélecteur de contacts multiple, bouton ➤ ; le message transféré affiche « ↪ Transféré »), **Étoiler / Retirer des favoris** (⭐), **Infos** (heures de remise/lecture), **Modifier** (mes messages texte, dans les **15 minutes** suivant l'envoi), **Supprimer** (voir 9.6), **Sélectionner**.

### 9.5 Réponse à un message
- Choisir « Répondre » (ou glisser/double-cliquer le message, ou raccourci **Ctrl+R**) : au-dessus du champ de saisie apparaît un **bandeau de citation** : barre verticale colorée à gauche, nom de l'auteur (en couleur), extrait du message (1–2 lignes ; « 📷 Photo », « 🎤 Message vocal », etc.), miniature à droite si image, bouton **✕** pour annuler. Le focus passe dans le champ de saisie.
- Après envoi : la bulle contient un bloc de citation grisé en haut. **Clic sur la citation → défilement jusqu'au message d'origine** et clignotement jaune 2 s. Si le message d'origine est supprimé : « Ce message a été supprimé ».

### 9.6 Suppression
- Modal « Supprimer le message ? » avec boutons : **« Supprimer pour moi »** (efface localement), **« Supprimer pour tous »** (visible seulement pour mes messages de moins de **60 minutes** ; envoie `DELETE`), **« Annuler »**.
- Après suppression pour tous : la bulle devient « 🚫 Vous avez supprimé ce message » (ou « 🚫 Ce message a été supprimé » chez le destinataire), en italique grisé ; les pièces jointes locales sont effacées chez le destinataire.
- Si le destinataire est hors ligne : l'ordre `DELETE` est mis en file et appliqué à sa reconnexion.

### 9.7 Modification
- « Modifier » met le texte dans le champ de saisie avec un bandeau « Modifier le message » (✕ pour annuler) ; le bouton envoyer devient ✓. Entrée valide → paquet `EDIT`. La bulle affiche « modifié ». Impossible après 15 minutes (l'entrée du menu est alors grisée).

### 9.8 Réactions
- Les réactions apparaissent en pastille ronde sous la bulle, en bas à gauche/droite (« 👍 2 »). Clic sur la pastille → fenêtre listant qui a réagi avec quoi ; clic sur sa propre réaction la retire. Une seule réaction par utilisateur et par message (la nouvelle remplace l'ancienne).

### 9.9 Sélection multiple
- Mode « Sélectionner » : cases à cocher rondes à gauche des messages ; barre d'actions en haut « N sélectionné(s) » + boutons **Supprimer**, **Transférer**, **Copier**, **Étoiler**, **✕** (quitter). Les boutons non applicables sont grisés.

---

## 10. BARRE DE SAISIE

De gauche à droite :
1. **Bouton 😊 Émojis** : ouvre un panneau (320×350) au-dessus du champ avec onglets (Récents, Smileys, Personnes, Animaux, Nourriture, Activités, Voyages, Objets, Symboles, Drapeaux), un champ de recherche (par nom : « coeur », « rire »…), et une grille cliquable. Clic sur un émoji → l'insère **à la position du curseur** sans fermer le panneau. Le panneau se ferme par Échap, par clic en dehors, ou en recliquant sur 😊. Les 30 derniers émojis utilisés sont mémorisés.
2. **Bouton 📎 Joindre** : menu vertical : « 📄 Document », « 🖼 Photos et vidéos », « 📷 Appareil photo » (capture via webcam avec aperçu et bouton « Capturer » puis « Envoyer »), « 👤 Contact » (partage de carte de contact). Voir 11.
3. **Champ de saisie** : multi-ligne, hauteur auto de 1 à 6 lignes puis scroll. Placeholder « Écrivez un message ». **Entrée** = envoyer ; **Maj+Entrée** = saut de ligne ; **Ctrl+V** colle du texte **ou une image du presse-papiers** (ouvre l'aperçu d'envoi d'image). Glisser-déposer de fichiers sur la fenêtre → aperçu d'envoi. Emoji-texte (`:)` → 🙂) optionnel.
   - À chaque frappe : envoie `TYPING start` (au maximum 1 fois / 3 s) ; `TYPING stop` après 4 s d'inactivité, à l'envoi, ou si le champ est vidé.
   - Le brouillon est **sauvegardé automatiquement** à chaque changement de conversation et à la fermeture.
   - Limite : 4096 caractères ; au-delà, le compteur rouge apparaît et l'envoi est bloqué.
4. **Bouton principal** (à droite), selon l'état :
   - champ **vide** → icône **🎤 micro** (message vocal) ;
   - champ **non vide** (ou pièce jointe prête) → icône **➤ envoyer**.

Envoi d'un message texte (déroulé exact) : trim → si vide ne rien faire → créer `Message` avec `status=Pending` et `id=GUID` → l'ajouter à l'UI immédiatement (bulle verte, ⏱) → persister en base → si contact en ligne, chiffrer et envoyer (→ `Sent` ✓) ; sinon rester `Pending` → vider le champ, supprimer le brouillon, retirer le bandeau de réponse → défiler en bas → son d'envoi discret.

---

## 11. FICHIERS, IMAGES, VIDÉOS, DOCUMENTS

### 11.1 Aperçu avant envoi (fenêtre modale)
- Grande prévisualisation (image/vidéo : lecture ; document : icône + nom + taille). Si plusieurs fichiers : bandelette de miniatures en bas, clic = changer de fichier, ✕ sur la miniature = retirer, bouton « + » = ajouter.
- Champ **Légende** (facultatif). Boutons : ✕ Fermer (Échap), **➤ Envoyer**. Pour les images : outils **Rogner** et **Pivoter**.
- Limites : **100 Mo** par fichier ; au-delà : message « Ce fichier dépasse 100 Mo ». Extensions exécutables (.exe, .bat…) : avertissement « Ce fichier peut être dangereux. Envoyer quand même ? ».

### 11.2 Transfert (déroulé)
1. L'expéditeur crée un message `kind=file/image` et envoie `FILE_OFFER` (avec miniature 200 px pour les images, sha256).
2. **Images ≤ 10 Mo : acceptées automatiquement** ; autres fichiers : acceptation automatique aussi si l'option Paramètres « Téléchargement automatique » est cochée, sinon la bulle affiche un bouton **⬇ Télécharger (2,3 Mo)**.
3. Le récepteur répond `FILE_ACCEPT`. L'expéditeur envoie les `FILE_CHUNK` de 64 Ko (asynchrone, avec `CancellationToken`).
4. **Progression** : cercle de progression dans la bulle (à l'envoi et à la réception) avec bouton **✕ annuler** au centre ; texte « 1,2 Mo / 4,5 Mo ». Annulation → `FILE_REJECT`/`FILE_DONE(error)`, bulle « Envoi annulé » avec bouton ↻ réessayer.
5. À la fin : le récepteur vérifie le **SHA-256**. Si OK → fichier dans `%AppData%\WhatsLAN\Media\<conversation>\`, bulle devient complète. Sinon « Échec du téléchargement » + ↻.
6. Si la connexion se coupe en cours de route : la reprise se fait à l'index du dernier chunk reçu (transfert **reprenable**).

### 11.3 Affichage dans la bulle
- **Image** : miniature (max 330×330, coins arrondis), légende dessous, heure + accusé incrustés en bas à droite sur l'image. **Clic** → visionneuse plein écran (fond noir) : flèches ◄ ► (et touches ← →) pour naviguer entre toutes les images de la conversation, molette = zoom, glisser = déplacer, boutons ⟲ rotation, ⬇ enregistrer sous…, ➤ transférer, 🗑 supprimer, ✕ fermer (Échap).
- **Vidéo** : miniature avec bouton ▶ et durée ; clic → lecteur intégré (lecture/pause, barre de progression, volume, plein écran).
- **Document** : carte avec icône du type (PDF, Word, Excel, ZIP…), nom (tronqué), « PDF • 1,2 Mo », bouton ⬇ / ouvrir. Clic → ouvre avec l'application par défaut ; clic droit → « Enregistrer sous… », « Afficher dans le dossier ».
- **Carte de contact** : nom + avatar + boutons « Message » / « Ajouter ».

---

## 12. MESSAGES VOCAUX

### 12.1 Enregistrement
- **Clic sur 🎤** : démarre l'enregistrement (NAudio `WaveInEvent`, 16 kHz mono, encodage Opus ou MP3/WAV compressé). L'interface de saisie est **remplacée** par : bouton **🗑 Annuler** (à gauche), point rouge clignotant + **chrono 0:07**, **onde sonore animée** en direct, bouton **⏸ Pause/Reprendre**, bouton **➤ Envoyer** (à droite).
- `TYPING recording` est envoyé toutes les 3 s pendant l'enregistrement (→ « enregistre un message vocal… » chez l'autre).
- Durée minimale 1 s (sinon annulé silencieusement) ; maximale 15 min (envoi automatique avec avertissement à 14 min).
- **Échap** ou 🗑 annule (confirmation si > 5 s). Si aucun micro n'est détecté : message « Aucun microphone détecté ».
- Option : maintenir le bouton 🎤 enfoncé (≥ 500 ms) = enregistrement « push-to-talk » ; relâcher = envoyer ; glisser vers la gauche = annuler.

### 12.2 Lecture (dans la bulle)
- Contrôles : bouton **▶/⏸**, **onde sonore statique** (barres) avec curseur de lecture cliquable (clic/glisse = se placer), **durée** (compte à rebours), **vitesse** (bouton 1× → 1,5× → 2× → 1×), avatar de l'expéditeur avec petite icône micro.
- Pastille/micro **bleu** quand le message a été écouté (le point vert « non écouté » disparaît). L'expéditeur voit le micro bleu quand le destinataire l'écoute (via `ACK_READ` spécifique `PLAYED`).
- Un seul vocal à la fois : lancer un nouveau met l'ancien en pause. Lecture continue du vocal suivant si non écouté.

---

## 13. GROUPES (P2P SANS SERVEUR)

Principe : il n'y a pas de serveur ; **chaque message de groupe est envoyé individuellement à chaque membre** (fan-out). L'état du groupe (nom, membres, admins) est répliqué chez tous avec un numéro de `version` ; la version la plus élevée l'emporte.

### 13.1 Création
- Menu ⋮ → « Nouveau groupe » : **étape 1** : liste de contacts avec cases à cocher ; les membres choisis apparaissent en puces en haut (✕ pour retirer) ; bouton flèche ➜ (désactivé si 0 membre). **Étape 2** : avatar de groupe (clic pour choisir), champ **Nom du groupe** (obligatoire, 25 car. max), bouton ✓ vert « Créer ».
- À la création : `GROUP_CREATE` envoyé à tous les membres en ligne (et mis en file pour les hors-ligne) ; le message système « Vous avez créé le groupe “X” » apparaît.

### 13.2 Panneau « Infos du groupe »
- Avatar, nom (modifiable par les admins : icône crayon), description, nombre de membres.
- Liste des membres (admin marqué « Admin du groupe »). Clic sur un membre (si je suis admin) → menu : « Nommer admin », « Retirer l'admin », « Retirer du groupe ».
- Bouton « Ajouter des participants » (admins), « Quitter le groupe » (rouge ; si dernier admin, le plus ancien membre devient admin), « Signaler/Bloquer » non applicable.
- Chaque changement incrémente `version`, est diffusé (`GROUP_UPDATE`) et génère un message système (« Alice a ajouté Bob », « Alice a changé le nom du groupe en “X” »).
- Un membre retiré ne reçoit plus les messages et voit « Vous n'êtes plus membre de ce groupe » ; le champ de saisie est remplacé par ce texte grisé.

### 13.3 Messages de groupe
- Mêmes fonctions que les discussions directes (réponses, réactions, fichiers, vocaux, édition, suppression).
- Accusés agrégés (voir 9.3). Indicateur « écrit… » : « Alice écrit… » ou « Alice et Bob écrivent… ».
- Les mentions **@nom** : en tapant `@`, une liste de membres apparaît (flèches + Entrée pour sélectionner) ; le nom est inséré en vert et le membre cité reçoit une notification même si le groupe est en sourdine.

---

## 14. APPELS AUDIO ET VIDÉO (LAN, UDP)

### 14.1 Signalisation (TCP existant)
1. A clique 📞 ou 🎥 → crée `callId`, ouvre un port UDP local, envoie `CALL_INVITE{callId, type, udpPort, mediaKey}`.
2. **Chez A** : fenêtre « Appel en cours… » (avatar, nom, « Appel de X… », bouton rouge **Raccrocher**). Sonnerie sortante (bip toutes les 3 s). Délai max 45 s → « Pas de réponse » puis fermeture.
3. **Chez B** : **fenêtre d'appel entrant** toujours au premier plan : avatar, nom, « Appel audio/vidéo WhatsLAN entrant », boutons **🟢 Accepter** et **🔴 Refuser**, sonnerie en boucle. Si B est déjà en appel : refus automatique avec « Occupé » côté A.
4. Accepter → `CALL_ACCEPT{udpPort}` ; Refuser → `CALL_REJECT`. Un message système est ajouté dans la conversation : « Appel audio manqué à 14:32 », « Appel audio 3:21 », « Appel refusé ».

### 14.2 Fenêtre d'appel
- **Audio** : avatar centré, nom, **chrono** (mm:ss), indicateur de qualité (barres), boutons ronds : **🎤 Muet** (bascule, barré en rouge), **🔊 Haut-parleur/choix de sortie**, **📹 Activer la vidéo** (convertit en appel vidéo avec nouvelle invitation), **🔴 Raccrocher**.
- **Vidéo** : vidéo distante plein cadre ; **ma vidéo en miniature** (160×120, coin bas droit, déplaçable) ; mêmes boutons plus **📹 Caméra on/off**, **⛶ Plein écran** (Échap pour quitter), choix de la caméra.
- **Média** : audio PCM 16 kHz → Opus (ou PCM brut si pas de codec) en paquets de 20 ms dans des datagrammes UDP, avec **numéro de séquence** et **timestamp**, **jitter buffer** de 60–100 ms ; vidéo : images JPEG qualité 60 à 15 fps, 640×480, découpées en fragments < 1400 octets, réassemblées par numéro d'image (une image incomplète est abandonnée). Chiffrement AES-GCM avec `mediaKey` dérivée de la session.
- **Fin d'appel** : bouton rouge, ou `CALL_END` de l'autre, ou perte de média > 10 s (« Connexion perdue »). Libération des périphériques micro/caméra, fermeture de la fenêtre, message système avec la durée.
- Un seul appel à la fois. Les appels de groupe ne sont pas pris en charge (bouton grisé dans les groupes).

---

## 15. NOTIFICATIONS ET SONS

- **Nouveau message** (fenêtre non active ou conversation non ouverte) : toast Windows avec avatar, nom (ou « Groupe : Nom »), extrait (« 📷 Photo », « 🎤 Message vocal »…), son « pop ». Clic sur la notification → ouvre l'application et la conversation. Option « Afficher l'aperçu » (Paramètres) pour masquer le contenu.
- **Icône dans la zone de notification** (system tray) : clic = restaurer/réduire ; clic droit = « Ouvrir », « Quitter ». Fermer la fenêtre (✕) **réduit dans le tray** si l'option « Continuer en arrière-plan » est activée (défaut), sinon quitte.
- **Badge** : le nombre total de non-lus s'affiche dans le titre de la fenêtre « (3) WhatsLAN » et en overlay sur l'icône de la barre des tâches.
- **Sons** : message reçu, message envoyé, appel entrant (boucle), appel sortant, fin d'appel. Désactivables. Pas de notification pour les conversations en sourdine (sauf @mention) ni pour les contacts bloqués.

---

## 16. CONTACTS, BLOCAGE, ÉTAT DE LA CONNEXION

- **Panneau « Infos du contact »** : grand avatar (clic = agrandir), nom, statut/à propos, « vu à… », IP:port (en petit, pour debug), médias partagés (grille des dernières images/docs avec « Voir tout »), bouton « Messages favoris », interrupteur « Sourdine », « Messages temporaires », « Bloquer X », « Supprimer la discussion ».
- **Blocage** : les paquets `MSG` / appels / fichiers du contact bloqué sont **ignorés** silencieusement ; il ne voit plus mon statut/avatar. Bandeau dans la conversation : « Vous avez bloqué ce contact. [Débloquer] ». Le champ de saisie est remplacé par ce bandeau.
- **Bandeau de connexion** en haut de la fenêtre (jaune) si aucune interface réseau active : « Pas de connexion réseau. Les messages seront envoyés dès le retour du réseau. ». Bandeau rouge si le port est occupé / pare-feu bloquant : « Impossible d'écouter sur le port X ».
- **Messages temporaires** : si activés, chaque message porte `expiresUtc` et est supprimé localement (tâche de fond chaque minute) à l'expiration ; message système « Les messages temporaires sont activés ».

---

## 17. CHIFFREMENT DE BOUT EN BOUT

- À la première exécution : générer une paire **ECDH P-256** ; la clé privée est stockée chiffrée avec **DPAPI** (`ProtectedData`) ; la clé publique est annoncée dans `HELLO`/`Handshake`.
- Secret partagé = `ECDH(privA, pubB)` → clé = `HKDF-SHA256(secret, info="WhatsLAN-v1")` (256 bits) ; chaque message : **nonce aléatoire de 12 octets**, AES-256-GCM, l'en-tête (type + messageId) est passé en *associated data* pour l'authentification.
- **Écran « Vérifier le chiffrement »** (dans Infos du contact) : affiche l'**empreinte** (60 chiffres ou 12 groupes de 5 chiffres) calculée = SHA-256(pubA ‖ pubB) pour comparaison à l'oral ; case « Marquer comme vérifié ». Si la clé publique d'un contact change : message système « Le code de sécurité de X a changé. [En savoir plus] ».
- Messages au repos : la base SQLite n'est pas chiffrée par défaut ; option Paramètres « Chiffrer l'historique » (clé DPAPI + SQLCipher ou chiffrement champ par champ du `body`).

---

## 18. PARAMÈTRES (fenêtre ou panneau)

Sections et options :
- **Général** : Démarrer avec Windows, Continuer en arrière-plan à la fermeture, Langue (Français / English).
- **Compte** : pseudo, statut, photo (identique au profil), « Réinitialiser l'identité » (efface clés et historique après confirmation).
- **Discussions** : Thème (Clair / Sombre / Système), Taille de police (Petite/Moyenne/Grande), Entrée envoie le message (oui/non ; si non, Ctrl+Entrée envoie), Téléchargement automatique (images/documents/vidéos), Dossier de téléchargement (bouton « Changer »), Confirmations de lecture, Indicateur « écrit… ».
- **Notifications** : Sons, Aperçus des messages, Notifications de groupes, Notifications d'appels.
- **Réseau** : port TCP (champ numérique), port de découverte UDP, intervalle de découverte, « Interface réseau » (liste déroulante), bouton **« Tester le réseau »** (affiche : IP locale, ports ouverts ?, contacts détectés), bouton « Ajouter un contact par IP » (secours si le broadcast est bloqué : champ IP + port → tentative de connexion TCP directe).
- **Stockage** : espace utilisé par les médias, bouton « Vider le cache média ».
- **À propos** : version, licences.

---

## 19. RACCOURCIS CLAVIER

| Raccourci | Action |
|---|---|
| Ctrl+N | Nouvelle discussion |
| Ctrl+Maj+N | Nouveau groupe |
| Ctrl+F | Rechercher dans la conversation |
| Ctrl+Maj+F | Rechercher dans toutes les discussions |
| Ctrl+K / Ctrl+Alt+/ | Aller à la recherche de la liste |
| Ctrl+Tab / Ctrl+Maj+Tab | Conversation suivante / précédente |
| Ctrl+E | Archiver la discussion |
| Ctrl+Maj+M | Mettre en sourdine |
| Ctrl+Maj+P | Épingler |
| Ctrl+Maj+U | Marquer comme non lue |
| Ctrl+Retour arrière | Supprimer la discussion |
| Ctrl+R | Répondre au message sélectionné |
| Ctrl+Maj+E | Ouvrir le sélecteur d'émojis |
| Ctrl+O | Joindre un fichier |
| Ctrl+, | Paramètres |
| Entrée / Maj+Entrée | Envoyer / nouvelle ligne |
| Échap | Fermer panneau/aperçu/recherche/réponse, annuler enregistrement |
| ↑ (champ vide) | Modifier mon dernier message |
| F11 | Plein écran |
| Ctrl+Q | Quitter |

---

## 20. CAS LIMITES ET GESTION D'ERREURS (À IMPLÉMENTER)

1. **Même utilisateur lancé 2 fois** avec la même base : détecter via `Mutex` nommé et afficher « WhatsLAN est déjà ouvert » puis activer la fenêtre existante.
2. **Messages en double** (reconnexion) : ignorés grâce à `messageId` unique.
3. **Horloges désynchronisées** : l'ordre d'affichage utilise l'heure de **réception** quand `|t_reçu − t_envoyé| > 5 min`.
4. **Changement d'IP** (DHCP) : l'IP est mise à jour à chaque `HELLO` ; les connexions TCP échouées sont rouvertes avec la nouvelle IP.
5. **Paquet malformé / trop gros / déchiffrement impossible** : journalisé, connexion fermée, aucune exception non gérée (pas de crash).
6. **Disque plein / fichier verrouillé** pendant la réception : transfert marqué en erreur, message clair.
7. **Pare-feu Windows** : au premier lancement, si l'écoute échoue → boîte de dialogue expliquant comment autoriser l'app (et bouton qui ouvre les paramètres du pare-feu).
8. **Fermeture pendant un transfert** : confirmation « Un transfert est en cours. Quitter ? ».
9. **Aucun contact détecté** : la liste affiche « Aucun contact trouvé sur le réseau. Vérifiez que les autres sont connectés au même Wi-Fi. » + bouton « Réessayer » et « Ajouter par IP ».
10. **Journal** (`%AppData%\WhatsLAN\logs\app-AAAAMMJJ.log`) avec niveaux Info/Warn/Error pour chaque paquet envoyé/reçu (sans contenu).
11. Toutes les opérations réseau/disque sont **asynchrones** ; l'UI ne doit **jamais** se figer.

---

## 21. ORDRE DE LIVRAISON (PHASES)

Quand je dis « Phase N », génère uniquement cette phase, testable seule :

1. **Phase 1 — Fondations** : solution, projets, SQLite, profil (écran de bienvenue), modèles.
2. **Phase 2 — Réseau de base** : découverte UDP, TCP, handshake, chiffrement, présence en ligne/hors ligne, console de test.
3. **Phase 3 — UI principale** : fenêtre, liste des conversations, zone de messages, envoi/réception de texte, heure, séparateurs de date, historique.
4. **Phase 4 — Accusés et état** : ⏱ ✓ ✓✓ bleu, « écrit… », vu à, non-lus, messages en attente hors ligne.
5. **Phase 5 — Actions sur messages** : répondre, modifier, supprimer, réactions, copier, transférer, étoiler, sélection multiple, recherche.
6. **Phase 6 — Émojis et notifications** : sélecteur, toasts, tray, sons, badge, sourdine, épingle, archive, brouillons.
7. **Phase 7 — Fichiers** : aperçu, transfert par chunks, progression, images/vidéos/documents, visionneuse.
8. **Phase 8 — Vocaux** : enregistrement, onde sonore, lecture, vitesse.
9. **Phase 9 — Groupes** : création, infos, admins, fan-out, accusés agrégés, mentions.
10. **Phase 10 — Appels audio** puis **Phase 11 — Appels vidéo**.
11. **Phase 12 — Finitions** : paramètres, blocage, messages temporaires, vérification du chiffrement, thèmes, raccourcis, gestion d'erreurs, tests.

---

## 22. CRITÈRES D'ACCEPTATION (TESTS À FAIRE)

- Deux instances (même PC, ports différents) se voient en < 5 s et échangent du texte.
- Un message envoyé à un contact hors ligne arrive dans l'ordre dès son retour.
- Les accusés passent ⏱ → ✓ → ✓✓ → ✓✓ bleu aux bons moments.
- Un fichier de 50 Mo est transféré, SHA-256 vérifié, et reprend après coupure.
- Un vocal de 30 s s'enregistre, s'envoie, se lit avec vitesse 2×.
- Un groupe de 3 instances : tout message arrive aux 2 autres, les accusés se cumulent.
- Un appel audio puis vidéo se connecte, le chrono tourne, raccrocher libère micro et caméra.
- Sniffer Wireshark : aucun texte clair lisible dans les paquets TCP.
- Aucune exception non gérée pendant 10 minutes d'utilisation intensive.

---

**Commence par me confirmer que tu as bien compris, puis résume en 10 lignes l'architecture que tu vas suivre, et attends que je te dise « Phase 1 ».**
