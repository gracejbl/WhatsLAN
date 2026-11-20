# PROMPT — PARTIE 3 : INSTALLATION, COMPTE/CONNEXION, FONCTIONS RESTANTES ET CHECKLIST TOTALE (WhatsLAN)

> À coller **après les Parties 1 et 2** (même conversation). En cas de conflit, la Partie 3 prime.
> Objectif : WhatsLAN se comporte comme **WhatsApp Desktop, de l'installation à la désinstallation**, en réseau local, sans serveur.
> Nom partout : **WhatsLAN**. Logo : `Assets/logo.svg` fourni.

---

## 0. ADAPTATIONS OBLIGATOIRES (ce qui ne peut pas être identique)

WhatsApp repose sur un serveur, un numéro de téléphone et Internet. WhatsLAN n'en a pas. Voici l'équivalent retenu pour chaque notion — **à respecter partout** :

| WhatsApp | Équivalent WhatsLAN |
|---|---|
| Numéro de téléphone + code SMS | **Compte local** : pseudo + photo + mot de passe (facultatif) + clé cryptographique |
| Liaison par code QR avec le téléphone | **Pas de téléphone** : l'application est autonome. Le code QR sert à **ajouter un contact** et à **transférer son compte** vers un autre PC |
| Carnet d'adresses du téléphone | **Personnes sur le réseau** (détectées automatiquement) + **Mes contacts** (ceux que j'ai ajoutés) |
| Serveur de messages | **Connexion directe pair-à-pair** (TCP) + file d'attente locale pour les hors-ligne |
| Sauvegarde Google Drive/iCloud | **Sauvegarde locale chiffrée** (`.wlbak`) vers un dossier choisi + transfert PC→PC sur le LAN |
| Mise à jour via Microsoft Store | **Mise à jour par un pair** du réseau qui a une version plus récente + installeur |
| Aperçu de lien, GIF (Tenor), Meta AI, paiements, WhatsApp Business, position en direct | **Non pris en charge** (aucun accès Internet requis). Ne pas afficher ces entrées de menu |
| Plusieurs appareils liés | **Un seul appareil actif par compte** (voir 2.9) |

---

## 1. INSTALLATION (de l'installeur au premier clic)

### 1.1 Livrables de build
- `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false` (inclure le runtime .NET : aucun prérequis à installer).
- **Installeur** : `WhatsLAN-Setup-1.0.0.exe` créé avec **Inno Setup** (ou WiX/MSIX), signé si possible. Version dans le nom et dans les métadonnées (FileVersion, ProductVersion, Company, Copyright).
- Icône `.ico` (16/24/32/48/64/128/256 px) pour : exécutable, installeur, raccourcis, tray, barre des tâches.

### 1.2 Assistant d'installation (écrans dans l'ordre exact)
1. **Choix de la langue** (Français / English) — liste déroulante, bouton **OK**.
2. **Bienvenue** : logo, « Bienvenue dans l'assistant d'installation de WhatsLAN », version, boutons **Suivant** / **Annuler** (confirmation « Quitter l'installation ? »).
3. **Contrat de licence** : zone de texte défilante (CLUF), boutons radio « J'accepte / Je n'accepte pas » ; **Suivant** désactivé tant que « J'accepte » n'est pas coché.
4. **Dossier d'installation** : par défaut `%LocalAppData%\Programs\WhatsLAN` (installation **par utilisateur, sans droits administrateur**) ; bouton **Parcourir…** ; espace requis et disponible affichés ; erreur si le disque est insuffisant ou le chemin invalide.
5. **Tâches supplémentaires** (cases) : ☑ Créer un raccourci sur le bureau ; ☑ Créer un raccourci dans le menu Démarrer ; ☐ Épingler à la barre des tâches ; ☑ **Démarrer WhatsLAN avec Windows** ; ☑ **Autoriser WhatsLAN dans le pare-feu Windows** (demande l'élévation UAC uniquement pour cette étape) ; ☑ Associer les fichiers `.wlbak` et le protocole `whatslan://`.
6. **Prêt à installer** : récapitulatif (dossier, tâches) + bouton **Installer**.
7. **Installation en cours** : barre de progression, texte du fichier en cours, bouton **Annuler** (annulation = rollback complet).
8. **Fin** : « Installation terminée », case ☑ **Lancer WhatsLAN**, bouton **Terminer**.
- Si WhatsLAN est **déjà installé** : proposer « Réparer / Mettre à jour / Désinstaller » ; la mise à jour **conserve** `%AppData%\WhatsLAN\` (données). Si l'application tourne : demander de la fermer (ou la fermer automatiquement avec confirmation).
- **Actions techniques de l'installeur** : copie des fichiers ; clé de registre `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (démarrage auto, argument `--minimized`) ; entrée « Programmes et fonctionnalités » ; **règles de pare-feu** (`netsh advfirewall firewall add rule name="WhatsLAN TCP" dir=in action=allow protocol=TCP localport=50001-50010 program="…\WhatsLAN.exe"` et la même pour UDP 50000 et 50100-50199) ; enregistrement du protocole `whatslan://` et de l'extension `.wlbak` ; AppUserModelID pour les toasts.

### 1.3 Désinstallation
Via Paramètres Windows ou raccourci « Désinstaller WhatsLAN » :
1. Fermer l'app si ouverte. 2. Supprimer fichiers, raccourcis, clés de registre, règles de pare-feu, protocole. 3. Boîte finale : « Voulez-vous aussi supprimer vos discussions et données personnelles ? » [**Conserver mes données**] [**Tout supprimer** — double confirmation]. 4. Écran de fin « WhatsLAN a été désinstallé ».

### 1.4 Mise à jour (sans serveur)
- Chaque `HELLO` contient `appVersion`. Si un pair annonce une version **supérieure** (semver), afficher une **bannière verte** en haut de la liste : « Une nouvelle version de WhatsLAN (1.1.0) est disponible depuis Alice. [**Mettre à jour**] [Plus tard] ».
- **Mettre à jour** : demande l'installeur par TCP (`UPDATE_REQUEST`) ; le pair le transmet par morceaux (barre de progression) ; vérification SHA-256 **et signature** ; lancement de l'installeur en mode silencieux ; l'app se ferme, se met à jour, **se relance** sur la même discussion.
- Une version **expirée** (incompatibilité de protocole `protocolVersion`) : écran bloquant « Cette version de WhatsLAN n'est plus compatible. Mettez à jour pour continuer. [Mettre à jour] ».

---

## 2. PREMIER LANCEMENT, COMPTE, CONNEXION, DÉCONNEXION

### 2.1 Écran de bienvenue (premier lancement uniquement)
Fenêtre 800×560, fond blanc, logo 96 px, titre « **Bienvenue sur WhatsLAN** », sous-titre « Discutez avec les personnes de votre réseau, sans serveur ni Internet ». Texte légal : « En continuant, vous acceptez les Conditions d'utilisation et la Politique de confidentialité » (liens ouvrant des pages locales). Boutons : **Commencer** (vert) ; lien « **Restaurer depuis une sauvegarde** » ; lien « **Récupérer mon compte depuis un autre PC** ».
- Choix de la **langue** (coin supérieur droit, liste : Français, English).

### 2.2 Création du compte — étapes avec « Suivant/Retour » et indicateur 1/4…4/4
**Étape 1 — Profil**
- Avatar cliquable (pastille caméra) : « Importer une photo », « Prendre une photo » (webcam), « Supprimer ». Recadrage rond (zoom molette, déplacement), bouton « Enregistrer ».
- **Pseudo** (1–25 car., obligatoire), **À propos** (139 car., défaut « Disponible »).
- Validation en direct, messages rouges sous les champs, **Suivant** désactivé si pseudo invalide.

**Étape 2 — Sécurité**
- **Mot de passe du compte** (facultatif mais recommandé) : champ + confirmation, jauge de robustesse (Faible/Moyen/Fort), œil 👁, min. 6 caractères si renseigné. Case « Me demander le mot de passe à chaque démarrage » (par défaut cochée si mot de passe défini).
- Texte : « Le mot de passe chiffre vos données sur cet ordinateur. »

**Étape 3 — Phrase de récupération**
- Génération de **12 mots** (liste BIP-39) dérivant la clé maîtresse. Affichage en grille 3×4 numérotée, boutons « Copier » et « Imprimer/Enregistrer en PDF ». Avertissement : « Sans cette phrase, un compte perdu est irrécupérable. »
- **Vérification** : demander les mots n° 3, 7 et 11 (champs) ; **Suivant** actif seulement si corrects. Lien « Je le ferai plus tard » (rappel dans Paramètres → Compte).

**Étape 4 — Autorisations réseau**
- Test automatique : port TCP/UDP libre, pare-feu, interface réseau. Liste de contrôles avec ✔ / ✖ : « Écoute réseau », « Découverte des contacts », « Microphone », « Caméra », « Notifications ». Boutons « Autoriser » (ouvre l'invite Windows / paramètres) pour chaque ✖.
- Bouton final **Terminer** → crée `UserId` (GUID), paire de clés **ECDH P-256**, identité de signature **Ed25519**, base SQLite, dossier `%AppData%\WhatsLAN\<UserId>\`, puis écran de **synchronisation** (« Préparation de vos discussions… » 2 s, barre) et ouverture de la fenêtre principale avec message système de bienvenue.

### 2.3 Écran de connexion (démarrages suivants)
- Si un mot de passe est défini et « demander au démarrage » est actif : fenêtre 420×560 centrée : avatar, pseudo, champ **Mot de passe** (focus automatique, œil), bouton **Se connecter** (Entrée), lien « **Mot de passe oublié ?** », lien « **Changer de compte** », sélecteur de langue.
- Mot de passe faux : secousse + « Mot de passe incorrect » ; **5 échecs → attente 30 s**, puis 1 min, 5 min, 15 min (compte à rebours visible).
- **Windows Hello** : si activé dans les paramètres, bouton « Utiliser Windows Hello » (empreinte/visage/PIN Windows).
- Pas de mot de passe : ouverture directe.
- Le mot de passe déverrouille la **clé de chiffrement de la base** (Argon2id/PBKDF2 → AES-256) ; sans lui, les fichiers sont illisibles.

### 2.4 Mot de passe oublié
Écran « Récupérer mon compte » : champ **phrase de récupération** (12 mots, collage accepté, vérification de l'orthographe des mots) → si correcte : « Définir un nouveau mot de passe » (2 champs) → connexion. Si phrase perdue : bouton rouge « **Réinitialiser WhatsLAN** » (efface toutes les données locales après avoir tapé « SUPPRIMER »), qui revient à l'écran de bienvenue.

### 2.5 Plusieurs comptes (profils)
- « Changer de compte » (écran de connexion ou menu ⋮ → « Changer de compte ») : liste des profils locaux (avatar + pseudo), bouton « **Ajouter un compte** » (relance 2.2), icône ⋮ par profil → « Supprimer ce compte ».
- Chaque compte a sa base, ses clés, son port TCP (auto-incrémenté) ; **un seul compte actif par instance**. Une 2ᵉ instance peut ouvrir un autre compte (utile aux tests).

### 2.6 Déconnexion
Paramètres → Compte (ou ⋮ → « Se déconnecter ») :
- Dialogue « **Se déconnecter ?** » : « Vos discussions restent chiffrées sur cet ordinateur. Vous ne recevrez plus de messages tant que vous n'êtes pas reconnecté. » Boutons [Annuler] [**Se déconnecter**].
- Actions : envoi de `BYE`, fermeture des connexions TCP/UDP, arrêt des tâches (statuts, transferts → message « Transferts annulés »), effacement des clés de la mémoire, retour à l'écran de connexion (2.3) ou de choix de compte.
- **Se déconnecter** ≠ **Quitter** : Quitter (Ctrl+Q) ferme simplement l'app, la prochaine ouverture redemande le mot de passe selon l'option.

### 2.7 Suppression du compte
Paramètres → Compte → « **Supprimer mon compte** » (rouge) : écran d'avertissement (liste de ce qui sera effacé : discussions, médias, statuts, clés), champ « Tapez votre pseudo pour confirmer », mot de passe si défini, bouton rouge **Supprimer mon compte**. Actions : envoi de `ACCOUNT_DELETED` aux contacts en ligne (ils voient la pastille système « X a supprimé son compte ») ; suppression des fichiers (écrasement sécurisé de la clé) ; retour à l'écran de bienvenue.

### 2.8 Changer de PC (récupération d'un compte)
- **A) Par phrase de récupération + sauvegarde** : « Récupérer mon compte depuis un autre PC » → saisir la phrase → importer un `.wlbak` (facultatif) → l'identité (`UserId`, clés) est restaurée ; les contacts reconnaissent la même clé.
- **B) Transfert direct par le réseau** : sur l'ancien PC : Paramètres → Compte → « Transférer vers un nouveau PC » → affiche un **code QR + code à 8 chiffres** valable 5 min ; sur le nouveau : « Récupérer… » → « Scanner le code QR » (webcam) ou saisir le code → transfert chiffré de l'identité et de l'historique (barre de progression) ; à la fin, l'ancien PC se déconnecte (« Compte transféré »).

### 2.9 Un seul appareil actif
Si un `HELLO` signé avec **ma propre clé** arrive depuis une autre IP : bandeau rouge « WhatsLAN est ouvert sur un autre appareil. [**Utiliser ici**] [Quitter] ». « Utiliser ici » envoie `TAKEOVER` ; l'autre appareil affiche le même bandeau et se met en pause. Aucun doublon de messages.

---

## 3. FENÊTRE ET INTÉGRATION WINDOWS

- Taille/position/état (maximisé) **mémorisés** ; taille min 800×500 ; restauration correcte si l'écran a changé. **Haute résolution (DPI)** : `PerMonitorV2`, rendu net.
- **Zoom** de l'interface : Ctrl + / Ctrl − / Ctrl 0 (100 %, de 80 % à 200 %), mémorisé.
- **Plein écran** F11. Barre de titre native (boutons réduire/agrandir/fermer, double-clic = agrandir).
- **Instance unique** (Mutex par compte) ; relancer l'exe active la fenêtre existante (et passe les arguments : `whatslan://chat/<userId>` ouvre la discussion).
- **Démarrage avec Windows** (`--minimized` → directement dans le tray, sans fenêtre).
- **Tray** : icône avec badge (rouge si non lus), info-bulle « WhatsLAN — 3 messages non lus » ; clic gauche = afficher/masquer ; menu : Ouvrir, Statut du réseau (lecture seule), Mettre en sourdine les notifications (1 h / 8 h / jusqu'à demain), Verrouiller, Quitter.
- **Barre des tâches** : badge numérique (overlay icon), clignotement orange à la réception d'un message si fenêtre inactive (option), **Jump List** (« Nouvelle discussion », « Nouveau groupe », « Statuts »), barre de progression pendant un transfert de fichier.
- **Toasts Windows** avec boutons : **Répondre** (champ en ligne), **Marquer comme lu**, **Ignorer** ; pour un appel : **Accepter** / **Refuser**. Respecte l'**assistant de concentration** de Windows et l'option « Ne pas déranger ».
- **Glisser-déposer** : fichiers/images/dossiers (dossier proposé en .zip) sur la fenêtre → aperçu d'envoi ; glisser un message hors de l'app → exporte le fichier.
- **Presse-papiers** : coller image/texte/fichier ; copier un message conserve la mise en forme (RTF) et le texte brut.
- **Menu contextuel système** du champ de saisie : correction orthographique (soulignement rouge, suggestions, « Ajouter au dictionnaire »), Couper/Copier/Coller/Coller sans mise en forme/Tout sélectionner.
- **Veille/réveil Windows** : à la reprise → renvoi de `HELLO`, reconnexion automatique, re-synchronisation des messages en attente. **Changement de réseau** (Wi-Fi ↔ câble) détecté (`NetworkChange`) → reconnexion automatique.
- Journal d'erreurs fatales : `%AppData%\WhatsLAN\crash\` ; au redémarrage après plantage : « WhatsLAN s'est arrêté de façon inattendue. [Ouvrir le dossier des journaux] [Fermer] ».

---

## 4. CONTACTS ET PERSONNES SUR LE RÉSEAU

### 4.1 Modèle
- **Personnes détectées** (automatique via `HELLO`) : listées dans « Nouvelle discussion » sous « **Sur votre réseau** ».
- **Mes contacts** : personnes ajoutées avec un nom personnalisé. Seuls les contacts voient mon statut/photo selon mes règles de confidentialité (« Mes contacts »).

### 4.2 Ajouter un contact (écran « Nouveau contact », via Nouvelle discussion)
Trois méthodes (onglets) :
1. **Sur le réseau** : liste des personnes détectées, bouton **+ Ajouter** sur chaque ligne → dialogue « Nom du contact » (prérempli avec son pseudo) + **Enregistrer**.
2. **Scanner un code QR** : aperçu webcam, cadre de visée ; le QR contient `whatslan://add?id=<userId>&key=<pubKey>&ip=<ip>&port=<port>&name=<pseudo>` ; à la détection → écran « Ajouter X ? » [Ajouter] [Annuler].
3. **Par adresse IP** : champs IP + port, bouton **Se connecter** (secours si le broadcast est bloqué) ; en cas d'échec : « Impossible de joindre cette adresse. Vérifiez que l'application est ouverte et que le pare-feu l'autorise. »
- **Mon code QR** (Paramètres → Profil → icône QR) : affiche mon QR (QRCoder), boutons « Copier le lien », « Enregistrer l'image », « Réinitialiser le code ».

### 4.3 Message d'un inconnu (non contact)
La discussion apparaît avec un **bandeau en haut** : « X n'est pas dans vos contacts. [**Ajouter aux contacts**] [**Bloquer**] [Signaler] ». Les images/fichiers d'un inconnu ne sont **pas téléchargés automatiquement**. Option Confidentialité « Filtrer les messages d'inconnus » : leurs messages vont dans une section « Demandes de message » (accepter/refuser).

### 4.4 Fiche contact (panneau « Infos du contact »)
Grand avatar, nom (modifiable ✎ = renommer localement), pseudo d'origine, à propos + date, « Vu à… », **Médias, liens et documents** (aperçu + « Voir tout »), **Messages importants**, **Sourdine**, **Messages temporaires**, **Verrouillage de discussion**, **Confidentialité avancée de la discussion**, **Chiffrement** (« Les messages sont chiffrés… » → écran empreinte), **Ajouter à une liste**, **Ajouter aux favoris**, **Groupes en commun**, **Exporter la discussion**, **Vider**, **Supprimer**, **Bloquer**, **Signaler** (journalise localement), **Supprimer le contact**.

---

## 5. DISCUSSIONS — FONCTIONS COMPLÉMENTAIRES

### 5.1 Filtres de la liste
Puces : **Toutes**, **Non lues**, **Favoris**, **Groupes**, + **listes personnalisées** (voir 5.2). Le filtre actif est mémorisé.

### 5.2 Listes personnalisées
Menu d'une discussion → « Ajouter à une liste » → choisir/créer (nom + icône). Les listes apparaissent comme puces (maximum 10). Gestion : ⋮ sur la puce → « Renommer », « Modifier les discussions », « Supprimer la liste ».

### 5.3 Favoris
Discussions favorites (⭐ menu) → puce « Favoris », triées par ordre manuel (glisser-déposer pour réordonner).

### 5.4 Non lu
« Marquer comme non lue » → pastille verte sans nombre ; ouvrir la discussion retire la marque. « **Tout marquer comme lu** » dans le menu ⋮ de la liste.

### 5.5 Aller à la date
Clic sur la date collante → calendrier (mois/année naviguables, jours avec messages en gras) → saute au premier message du jour.

### 5.6 Médias, liens et documents (panneau droit, 3 onglets)
- **Médias** : grille de miniatures (photos/vidéos) groupée par mois ; clic = visionneuse (navigation dans l'ensemble) ; sélection multiple (cases) → Transférer / Supprimer / Enregistrer.
- **Liens** : liste (domaine, URL, date) ; clic = ouvrir ; clic droit = copier.
- **Documents** : liste (icône, nom, taille, date) ; clic = ouvrir ; clic droit = Enregistrer sous / Afficher dans le dossier.
- (**Audio** : vocaux et fichiers audio avec lecteur.)

### 5.7 Fond d'écran et couleur de discussion
Paramètres → Discussions → **Fond d'écran** : 24 motifs/couleurs prédéfinis (clair/sombre), « Choisir une image… », case « Atténuer en mode sombre » ; applicable **par discussion** (menu ⋮ → « Fond d'écran »). Couleur de bulle sortante personnalisable (8 teintes).

### 5.8 Confidentialité avancée de la discussion
Interrupteur par discussion : bloque l'**export** de la discussion, **désactive** le téléchargement automatique des médias, interdit de **transférer** ses messages, interdit l'utilisation du contenu dans l'IA (sans objet) ; bandeau d'information dans la discussion.

### 5.9 Exporter une discussion
⋮ → « **Exporter la discussion** » → choix « Sans médias » / « Avec médias » → fichier `.zip` contenant `_chat.txt` (format `[19/11/2026, 14:32:10] Alice: Salut`), médias nommés ; sélecteur de dossier ; toast « Exportation terminée [Ouvrir le dossier] ». Bloqué si Confidentialité avancée active.

### 5.10 Vider / supprimer / archiver
- **Vider** : dialogue avec case « Garder les messages importants » ; message système « Vous avez vidé cette discussion ».
- **Supprimer** : retire la discussion de la liste (les messages sont effacés) ; si un nouveau message arrive, elle réapparaît vide.
- **Archiver** : voir Partie 2.

### 5.11 Recherche (filtres)
Recherche globale (panneau liste) : puces de filtre **Photos, Vidéos, Liens, GIFs, Audio, Documents, Sondages** ; recherche dans la discussion (en-tête) avec filtre par date ; **résultats classés** par pertinence puis date ; accents/majuscules ignorés (FTS5 `remove_diacritics 2`).

### 5.12 Messages temporaires (compléments)
Sélecteur 24 h/7 j/90 j/Désactivé ; décompte à partir de l'**envoi** ; icône ⏱ dans l'en-tête ; messages système « Vous avez activé/désactivé les messages temporaires » ; option « Appliquer à toutes les nouvelles discussions ».

### 5.13 Brouillons, position de lecture, état
Brouillon par discussion (restauré au réaffichage, visible dans la liste en vert) ; **position de défilement mémorisée** par discussion ; la dernière discussion ouverte est **restaurée au redémarrage**.

---

## 6. GROUPES — FONCTIONNALITÉS COMPLÈTES

### 6.1 Paramètres du groupe (panneau « Infos du groupe »)
- **Description** (modifiable, 2 048 car.), **Modifier les infos du groupe** : « Tous les participants / Seulement les admins ».
- **Envoyer des messages** : « Tous / Seulement les admins » (si restreint : champ de saisie remplacé par « Seuls les admins peuvent envoyer des messages »).
- **Ajouter des participants** : « Tous / Seulement les admins ».
- **Approuver les nouveaux participants** (interrupteur) : les demandes arrivent dans « Demandes de participation » (Accepter/Refuser).
- **Messages temporaires**, **Sourdine**, **Verrouillage**, **Médias, liens, documents**, **Messages importants**, **Exporter**.
- **Lien d'invitation** : « Inviter via un lien » → `whatslan://join?g=<groupId>&t=<token>&ip=<ip>` ; boutons « Copier le lien », « Partager » (vers une discussion), « Code QR », « Réinitialiser le lien ». Ouvrir le lien (Ctrl+V dans « Rejoindre un groupe » ou clic via protocole) → fiche « Rejoindre le groupe X ? (N membres) » [Rejoindre] [Annuler].
- **Participants** : recherche ; chaque ligne : avatar, nom, « Admin du groupe », à propos ; clic → menu : « Message », « Infos », « Nommer admin / Retirer l'admin », « Retirer du groupe » ; ordre : admins, puis alphabétique.
- **Quitter le groupe** (confirmation ; case « Supprimer aussi la discussion ») ; **Supprimer le groupe** (réservé au créateur/admin unique, retire tous les membres) ; **Anciens participants** (liste).
- Limite : **64 membres** (message « Ce groupe est complet »).

### 6.2 Messages système de groupe (exhaustif)
« X a créé le groupe », « X vous a ajouté », « X a ajouté Y », « X a retiré Y », « X a quitté », « Y a rejoint via le lien », « X a changé l'icône / le nom / la description », « X a changé les paramètres du groupe pour que seuls les admins puissent… », « X est maintenant admin », « X n'est plus admin », « X a activé les messages temporaires », « X a épinglé un message », « Nouveau numéro de sécurité… » (clé changée).

### 6.3 Mentions
`@` ouvre la liste filtrée au fil de la frappe (flèches, Entrée/Tab, Échap) ; « @tous » (admins) notifie tout le monde ; la mention est surlignée en vert dans la bulle, clic = infos du membre ; l'élément mentionné reçoit une pastille « @ » verte dans la liste.

### 6.4 Réplication d'état (P2P)
Version incrémentale du groupe ; en cas de conflit : version la plus haute gagne, à égalité l'`userId` le plus petit ; un membre hors-ligne reçoit les `GROUP_UPDATE` manqués à sa reconnexion ; seuls les **admins signés** (Ed25519) peuvent modifier (vérification de signature côté récepteur).

---

## 7. CHAÎNES ET COMMUNAUTÉS (version LAN)

Le rail contient aussi **Chaînes** et **Communautés** (comme WhatsApp Desktop).

### 7.1 Chaînes (diffusion à sens unique)
- Créer une chaîne : nom, description, icône ; **seul le créateur publie** ; les abonnés **reçoivent, réagissent (émojis), ne répondent pas**.
- Découvrir : onglet « Sur votre réseau » liste les chaînes annoncées dans `HELLO`/`CHANNEL_ANNOUNCE` ; bouton **S'abonner / Abonné ✓** ; ⋮ → « Se désabonner », « Mettre en sourdine », « Partager le lien ».
- Publier : mêmes types de messages (texte, image, vidéo, sondage, document, vocal) ; compteur de **vues** et de **réactions** sous chaque publication ; les messages restent **30 jours**.
- Vue : liste des chaînes suivies (avatar, nom, dernier post, heure, badge non-lus) à gauche ; fil de publications à droite (pas de champ de saisie pour les abonnés).

### 7.2 Communautés
- Une communauté = un **groupe d'annonces** (seuls les admins publient) + des **groupes rattachés**. Créer : nom, description, icône ; ajouter/créer des groupes (max 10).
- Vue : en-tête communauté, ligne « Annonces », puis liste des groupes avec bouton **Rejoindre / Ouvert**. Infos : membres, groupes, lien d'invitation, quitter. Les admins peuvent diffuser un message à **tous** les groupes via « Annonces ».

---

## 8. TYPES DE MESSAGES RESTANTS

### 8.1 Autocollants (stickers)
Onglet **Autocollants** dans le panneau 😊 : Récents, Favoris (⭐), packs installés. **Créer un autocollant** : image → recadrage 512×512, fond supprimé (outil gomme/baguette), texte/émoji ajoutable → enregistre en WebP. Envoi en un clic ; clic droit sur un sticker reçu → « Ajouter aux favoris », « Enregistrer le pack ». Affichage : grande taille (190 px) sans bulle.
### 8.2 GIF
Onglet **GIF** : bibliothèque **locale** (importer des `.gif`/`.mp4`) avec recherche par nom ; envoi comme vidéo bouclée muette avec étiquette « GIF ». (Pas de recherche en ligne.)
### 8.3 Émojis (complet)
Version Unicode récente ; **tons de peau** (appui long/clic droit → palette de 6) mémorisés ; recherche par mot-clé (FR/EN) ; **saisie semi-automatique** `:coeur` → liste de suggestions (flèches/Tab) ; conversion `:)` → 🙂 (option) ; émojis agrandis (×3) quand le message ne contient que 1 à 3 émojis.
### 8.4 Fichier audio
Fichiers `.mp3/.m4a/.wav/.ogg` : carte avec icône casque, nom, durée, bouton ▶ lecteur intégré (progression, vitesse), distinct des messages vocaux.
### 8.5 Événements
Menu 📎 → **Événement** : titre, description, début/fin (sélecteur date/heure), lieu (texte), option « Autoriser les invités ». Carte dans la bulle avec boutons **J'y vais / Peut-être / Je n'y vais pas** et compteurs ; clic → panneau des réponses ; bouton « Ajouter à mon calendrier » (génère un `.ics`).
### 8.6 Messages transférés / fréquemment transférés
Étiquette « ↪ Transféré » ; après 5 transferts successifs : « ↪↪ Transféré plusieurs fois » ; limite de transfert à 5 discussions à la fois (message « Vous ne pouvez transférer qu'à 5 discussions »).
### 8.7 Messages vocaux (compléments)
Si l'enregistrement est verrouillé (glisser vers le haut / clic sur 🔒 pendant l'enregistrement) → mains libres ; **prévisualisation** avant envoi (▶ écouter, 🗑, ➤) ; vitesse 1×/1,5×/2× mémorisée ; **transcription** non prise en charge.

---

## 9. ÉDITEUR D'IMAGES ET DE VIDÉOS (avant envoi)

- **Plusieurs fichiers** (jusqu'à 30) : bandelette de miniatures, réordonnable par glisser-déposer, légende **par fichier**, bouton « + » pour ajouter.
- **Outils image** : **Rogner** (ratios libre/1:1/4:3/16:9 + rotation 90° + miroir), **Dessiner** (pinceau 4 tailles, 12 couleurs, surligneur, gomme, annuler/rétablir), **Texte** (5 polices, couleur, fond, alignement, déplaçable/redimensionnable), **Autocollants/Émojis**, **Flou** (zone/pinceau), **Filtres** (6 : Normal, N&B, Chaud, Froid, Sépia, Vif), **Luminosité/Contraste**.
- **Qualité** : bascule **SD / HD** (SD = compression JPEG 70 %, 1600 px ; HD = qualité maximale) ; défaut selon Paramètres.
- **Vidéo** : lecture, **découpage** par frise (poignées début/fin), silence (🔇), durée max 3 min (sinon proposer de découper ou d'envoyer en document).
- Options : **Vue unique ①**, **Envoyer comme document** (sans compression), **Légende** avec émojis, **Mentions** dans la légende (groupes).
- Boutons : ✕ (confirmation si modifié), « Annuler les modifications », **➤ Envoyer** (compteur du nombre de fichiers).

---

## 10. PARAMÈTRES — ARBORESCENCE COMPLÈTE

Fenêtre Paramètres : liste à gauche, détail à droite ; recherche en haut. Sections :

1. **Général** : Démarrer avec Windows ; Réduire dans le tray à la fermeture ; Langue ; **Zoom** ; Thème (Système/Clair/Sombre) ; Police (taille Petite/Moyenne/Grande) ; Afficher l'icône dans le tray ; Vérifier la présence de mises à jour sur le réseau.
2. **Compte** : Photo, pseudo, à propos ; **Mon code QR** ; **Mot de passe** (créer/modifier/supprimer) ; **Windows Hello** ; **Phrase de récupération** (afficher après mot de passe) ; **Notifications de sécurité** (alerte si la clé d'un contact change) ; **Transférer vers un autre PC** ; **Sauvegarde** (voir 11) ; **Demander mes données** ; **Se déconnecter** ; **Supprimer mon compte**.
3. **Confidentialité** : Dernière connexion/En ligne ; Photo ; À propos ; Statuts ; **Confirmations de lecture** ; Messages temporaires par défaut ; Groupes (qui peut m'ajouter) ; **Appels** (« Désactiver le son des appels d'inconnus ») ; **Filtrer les messages d'inconnus** ; Contacts bloqués ; **Verrouillage de discussion** ; **Verrouillage de l'application** ; **Confidentialité avancée**.
4. **Discussions** : Thème ; **Fond d'écran** ; Taille de police des messages ; **Entrée pour envoyer** ; **Correction orthographique** (langue FR/EN) ; **Remplacement de texte** (`:)`→🙂) ; **Remplacement des émojis** ; Archivage (garder archivées) ; **Historique des discussions** (Exporter toutes les discussions, Archiver toutes, Vider toutes, Supprimer toutes — chacune avec confirmation).
5. **Vidéos et photos** : Qualité d'envoi (Auto / Meilleure qualité / Économie de données) ; Téléchargement automatique (Photos / Audio / Vidéos / Documents : « Jamais / Contacts / Toujours »).
6. **Notifications** : Notifications de messages (sons, **aperçu**, **réactions**) ; Groupes (sons, aperçu) ; Appels (sonnerie) ; Statuts ; **Rappels** ; **Son de message** (liste de 6 sons + « Choisir un fichier ») ; **Ne pas déranger** (plage horaire) ; Notifications d'arrière-plan.
7. **Personnalisation** : Couleur d'accentuation ; **Animations** (réduire) ; Densité (Compacte/Normale) ; Disposition du rail.
8. **Stockage et données** : Espace utilisé (Discussions, Médias, Statuts, Total) en graphique ; **Gérer le stockage** (liste des discussions triées par taille, sélection → « Supprimer les éléments ») ; **Dossier de téléchargement** ; **Vider le cache** ; **Réseau** (voir 13).
9. **Raccourcis** : liste complète éditable (reconfigurer un raccourci, conflit détecté, « Réinitialiser »).
10. **Aide** : Centre d'aide (page locale HTML), **Raccourcis clavier** (Ctrl+/), Conditions d'utilisation, Politique de confidentialité, **Exporter les journaux de diagnostic** (.zip), **À propos** (version, build, licences open-source).

---

## 11. SAUVEGARDE, RESTAURATION, EXPORT

- **Sauvegarde manuelle** : Paramètres → Compte → Sauvegarde → « **Sauvegarder maintenant** » : choix du dossier, **mot de passe de sauvegarde** (ou phrase de récupération), option « Inclure les médias » ; fichier `WhatsLAN-AAAAMMJJ-HHmm.wlbak` (SQLite + médias compressés, chiffré AES-256-GCM) ; barre de progression ; « Dernière sauvegarde : aujourd'hui 10:12 ».
- **Sauvegarde automatique** : Jamais / Quotidienne / Hebdomadaire / Mensuelle, dossier configurable, conserve les **5** dernières.
- **Restauration** : depuis l'écran de bienvenue (2.1) ou Paramètres → « Restaurer » (remplace les données actuelles après confirmation) : choisir `.wlbak` → mot de passe → barre de progression → redémarrage.
- **Demander mes données** : génère un `.zip` (profil JSON, contacts, discussions en JSON/TXT, médias).
- **Intégrité** : au démarrage, `PRAGMA integrity_check` ; en cas de corruption → « Base de données endommagée. [Restaurer une sauvegarde] [Réparer] [Quitter] ».

---

## 12. NOTIFICATIONS ET SONS (détail)

- Toast : avatar rond, **nom** (gras), extrait ; groupes : « Groupe — Nom : texte » ; messages vocaux « 🎤 Message vocal (0:23) » ; pièces jointes « 📷 Photo », « 🎥 Vidéo », « 📄 Document », « 📊 Sondage », « 😀 Autocollant », « 📅 Événement », « 👤 Contact », « GIF ».
- **Regroupement** : plus de 3 notifications simultanées d'une même discussion → « 5 nouveaux messages ».
- **Sourdine** : aucun son ni toast (badge visible) ; les **mentions** et **réponses à mes messages** passent (option).
- **Appel entrant** : fenêtre toujours au premier plan + toast + sonnerie en boucle ; s'arrête dès qu'un autre appareil/instance répond.
- **Sons** (fichiers `.wav` inclus) : `message_in`, `message_out`, `call_ring_in`, `call_ring_out`, `call_end`, `ptt_start`, `ptt_stop`, `notification`. Volume global réglable, bouton « Tester le son ».
- Tous les toasts respectent « Ne pas déranger », l'assistant de concentration et le verrouillage (4.3 de la Partie 2).

---

## 13. RÉSEAU : ROBUSTESSE ET DIAGNOSTIC

- **Interfaces multiples** : liste déroulante de la carte réseau utilisée ; option « Toutes les interfaces ». Ignorer VPN/virtuelles sauf choix explicite.
- **Découverte de secours** si le broadcast est filtré : (1) **multicast** `239.255.50.50`, (2) **balayage du sous-réseau /24** sur le port TCP (option), (3) **ajout par IP** (4.2), (4) liste de **pairs mémorisés** reconnectés au démarrage.
- **Isolation Wi-Fi (AP isolation)** détectée : bandeau « Votre réseau semble empêcher les appareils de communiquer entre eux. »
- **Outil de diagnostic** (Paramètres → Stockage et données → Réseau → **Tester le réseau**) : IP locale, masque, interface, ports TCP/UDP libres, pare-feu (état), nombre de pairs détectés, latence (ping applicatif) par pair, débit de test (envoi de 5 Mo vers un pair), résultat ✔/✖ par test, bouton « Copier le rapport ».
- **Qualité** : indicateur de connexion par contact dans l'info (Excellente / Bonne / Faible) ; reprise automatique des transferts ; **limiteur de débit** optionnel (Mo/s) ; **horodatage** corrigé selon dérive d'horloge.
- **Sécurité réseau** : rejet des paquets non authentifiés, protection anti-rejeu (compteurs de séquence + fenêtre), limite de débit par pair (anti-flood), validation stricte de la taille des champs, aucun code exécuté depuis un message.

---

## 14. ACCESSIBILITÉ, LANGUES, CLAVIER

- **Langues** : FR et EN complètes via fichiers de ressources `.resx` ; changement **à chaud** sans redémarrage ; formats de date/heure selon la culture (24 h en FR).
- **Accessibilité** : `AutomationProperties.Name` sur tous les contrôles ; compatibilité **Narrateur/NVDA** (lecture « Message de Alice, 14:32, lu : Salut ») ; navigation **entièrement au clavier** (Tab, Maj+Tab, flèches, Entrée, Échap, Espace), ordre logique, **anneau de focus** visible ; mode **contraste élevé** Windows respecté ; contraste texte ≥ 4,5:1 ; option **réduire les animations** ; tailles de police ajustables.
- **Raccourcis** (en plus de la Partie 1) : Ctrl+/ (liste des raccourcis), Ctrl+L (verrouiller l'app), Ctrl+1/2/3/4 (Discussions/Appels/Statuts/Chaînes), Ctrl+P (Profil), Ctrl+Maj+A (Archivées), Ctrl+D (étoile), Ctrl+Alt+Maj+P (Épingler un message), Alt+← / Alt+→ (historique de navigation), Ctrl+W (fermer la discussion), Ctrl+G (aller à la date), PgUp/PgDn (défilement), Début/Fin (haut/bas de la discussion), F2 (renommer), Suppr (supprimer le message/la discussion sélectionné·e).

---

## 15. AIDE, À PROPOS, LÉGAL

- **Centre d'aide** (page HTML locale dans l'app, WebView2) : démarrage, ajout de contacts, statuts, verrouillage, sauvegarde, dépannage réseau, FAQ, raccourcis.
- **À propos** : logo, « WhatsLAN version 1.0.0 (build 1234) », date, « Fait avec .NET 8 », licences open-source (liste), bouton « Vérifier les mises à jour » (cherche sur le réseau).
- **Conditions d'utilisation** et **Politique de confidentialité** : documents locaux courts (aucune donnée ne quitte le réseau local ; pas de collecte, pas de télémétrie).
- **Premier message d'onboarding** : dans la zone vide, 3 cartes « Ajoutez un contact », « Publiez un statut », « Créez un groupe » avec boutons d'action ; visite guidée (« Passer / Suivant ») de 5 bulles : Rail → Liste → Recherche → Saisie → Paramètres.

---

## 16. CE QUI NE DOIT PAS EXISTER (ne pas l'afficher, ne pas le coder)

Numéro de téléphone/SMS, paiements, catalogue/Business, Meta AI, aperçu de liens en ligne, GIF en ligne, position en direct, rapport à un serveur, télémétrie, publicités, achats intégrés, synchronisation multi-appareils simultanée, appels de groupe. Si un élément de la Partie 1 ou 2 y fait référence, le supprimer.

---

## 17. CHECKLIST MAÎTRESSE (cocher chaque ligne avant de dire « terminé »)

**Installation**
- [ ] Installeur 8 écrans, sans droits admin ; raccourcis ; démarrage auto ; pare-feu ; protocole `whatslan://` ; `.wlbak`.
- [ ] Mise à jour par un pair ; réparation ; désinstallation avec choix de conserver les données.

**Compte**
- [ ] Bienvenue → 4 étapes (profil, sécurité, phrase 12 mots avec vérification, autorisations) → fenêtre principale.
- [ ] Connexion par mot de passe/Windows Hello ; anti-force-brute ; mot de passe oublié par phrase ; réinitialisation totale.
- [ ] Multi-comptes ; déconnexion ; suppression ; transfert vers un autre PC (QR/code) ; un seul appareil actif (« Utiliser ici »).

**Fenêtre/Windows**
- [ ] Taille mémorisée ; DPI ; zoom ; plein écran ; instance unique ; tray ; badge ; jump list ; toasts avec Répondre/Lu/Accepter/Refuser ; glisser-déposer ; presse-papiers ; veille/réveil ; changement de réseau ; rapport de plantage.

**Contacts**
- [ ] Détection automatique ; ajout réseau/QR/IP ; mon QR ; inconnus (bandeau, demandes) ; fiche contact complète ; blocage/déblocage ; renommer ; supprimer.

**Messages**
- [ ] Texte ; mise en forme ; émojis (tons, recherche, auto-complétion) ; liens ; « Lire la suite » ; citation/réponse ; modifier ; supprimer (moi/tous) ; réactions ; transférer ; étoile ; épingler ; infos ; copier ; sélection multiple ; mentions.
- [ ] Accusés ⏱ ✓ ✓✓ bleu (direct et groupe) ; « écrit… » ; « enregistre… » ; vu à ; en ligne ; hors-ligne file d'attente.
- [ ] Images/vidéos/documents/audio/vocaux/GIF/stickers/contact/sondage/événement/vue unique ; éditeur d'images ; multi-fichiers ; HD/SD ; progression, annulation, reprise, SHA-256.
- [ ] Recherche globale/ conversation / filtres ; aller à la date ; position mémorisée ; brouillons.

**Discussions**
- [ ] Épingler (3) ; sourdine ; archiver ; non lu ; favoris ; listes ; verrouiller ; fond d'écran ; messages temporaires ; confidentialité avancée ; exporter ; vider ; supprimer ; médias/liens/docs.

**Groupes/Chaînes/Communautés**
- [ ] Création, icône, description, admins, permissions, approbation, lien/QR d'invitation, anciens membres, quitter/supprimer, messages système, accusés agrégés, réplication signée.
- [ ] Chaînes (créer, s'abonner, publier, réactions, vues) ; Communautés (annonces, groupes).

**Statuts/Appels**
- [ ] Statuts texte/photo/vidéo, confidentialité, visionneuse, vues, réactions, réponse, expiration 24 h, sourdine.
- [ ] Appels audio/vidéo, historique, appel manqué/refusé/occupé, muet, caméra, plein écran, qualité.

**Sécurité**
- [ ] E2E (ECDH+AES-GCM), empreinte vérifiable, alerte changement de clé, verrouillage de discussion/app, chiffrement au repos, anti-rejeu, anti-flood, DPAPI.

**Paramètres/Divers**
- [ ] 10 sections complètes ; raccourcis éditables ; sauvegarde/restauration/auto ; demander mes données ; langues FR/EN à chaud ; accessibilité ; diagnostic réseau ; aide ; à propos ; onboarding guidé.

---

## 18. PHASES SUPPLÉMENTAIRES

- **Phase 17 — Installeur et mise à jour** (Inno Setup, pare-feu, protocole, mise à jour par pair, désinstallation).
- **Phase 18 — Compte et connexion** (bienvenue, 4 étapes, login, mot de passe oublié, multi-comptes, déconnexion, suppression, transfert, appareil unique).
- **Phase 19 — Intégration Windows** (tray, toasts interactifs, jump list, badge, DPI, zoom, veille/réseau, plantage).
- **Phase 20 — Contacts et inconnus** (QR, IP, demandes, fiche contact).
- **Phase 21 — Discussions avancées** (listes, favoris, fonds d'écran, export, médias/liens/docs, confidentialité avancée, aller à la date).
- **Phase 22 — Groupes complets, Chaînes, Communautés.**
- **Phase 23 — Types de messages restants** (stickers, GIF, audio, événements, éditeur d'images/vidéos).
- **Phase 24 — Sauvegarde/restauration/export/stockage.**
- **Phase 25 — Paramètres complets, langues, accessibilité, aide, diagnostic.**
- **Phase 26 — Durcissement final** : revue de la checklist, tests de charge (50 000 messages, 5 pairs), tests de coupure réseau, tests de sécurité (Wireshark), correction des fuites mémoire, performance (< 300 Mo RAM, démarrage < 3 s).

**Tests d'acceptation finaux** : installer sur un PC vierge, créer 3 comptes sur 3 PC, faire passer **chaque ligne de la checklist 17** ; aucune exception non gérée ; aucune interface qui se fige ; installation → connexion → utilisation → déconnexion → désinstallation sans erreur.

---

**Confirme la réception de cette Partie 3 en 8 lignes max, signale toute contradiction que tu détectes avec les Parties 1 et 2 (et propose la correction), puis attends « Phase 0 ».**
