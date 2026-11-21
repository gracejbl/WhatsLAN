# PROGRESS.md — Suivi de WhatsLAN

Légende : ⬜ à faire · 🟨 en cours · ✅ terminé et testé à la main

## Ordre de travail (suivre cet ordre exact)

| # | Phase | Source | État |
|---|---|---|---|
| 0 | Design : thèmes clair/sombre, rail, 3 zones, bulles, données factices | Partie 2 §1–2 | ✅ |
| 1 | Fondations : solution, projets, SQLite, profil, modèles | Partie 1 §21 | ✅ |
| 2 | Réseau de base : UDP, TCP, handshake, chiffrement, présence | Partie 1 §4, §17 | ✅ |
| 3 | UI principale : liste, messages texte, historique | Partie 1 §6–10 | ✅ |
| 4 | Accusés, « écrit… », non-lus, file hors-ligne | Partie 1 §9.3 | ✅ |
| 5 | Actions sur messages : répondre, modifier, supprimer, réactions, recherche | Partie 1 §9 | ⬜ |
| 6 | Émojis, notifications, tray, sons, sourdine, épingle, archive, brouillons | Partie 1 §7, §10, §15 | ⬜ |
| 7 | Fichiers : aperçu, transfert par morceaux, visionneuse | Partie 1 §11 | ⬜ |
| 8 | Messages vocaux | Partie 1 §12 | ⬜ |
| 9 | Groupes | Partie 1 §13 + Partie 3 §6 | ⬜ |
| 10 | Appels audio | Partie 1 §14 | ⬜ |
| 11 | Appels vidéo | Partie 1 §14 | ⬜ |
| 12 | Finitions de base : paramètres, blocage, temporaires, vérification du chiffrement | Partie 1 §16–18 | ⬜ |
| 13 | Historique des appels | Partie 2 §2.2 | ⬜ |
| 14 | Statuts | Partie 2 §3 | ⬜ |
| 15 | Verrouillage de discussion et d'application | Partie 2 §4 | ⬜ |
| 16 | Extras : mise en forme, sondages, vue unique, épingle de message, diffusion, confidentialité | Partie 2 §5–6 | ⬜ |
| 17 | Installeur, mise à jour par pair, désinstallation | Partie 3 §1 | ⬜ |
| 18 | Compte et connexion : création, login, mot de passe oublié, multi-comptes, déconnexion, suppression, transfert | Partie 3 §2 | ⬜ |
| 19 | Intégration Windows : tray, toasts interactifs, jump list, DPI, zoom, veille | Partie 3 §3, §12 | ⬜ |
| 20 | Contacts : QR, IP, inconnus, fiche contact | Partie 3 §4 | ⬜ |
| 21 | Discussions avancées : listes, favoris, fonds, export, médias/liens/docs | Partie 3 §5 | ⬜ |
| 22 | Groupes complets, Chaînes, Communautés | Partie 3 §6–7 | ⬜ |
| 23 | Messages restants : stickers, GIF, audio, événements, éditeur d'images/vidéos | Partie 3 §8–9 | ⬜ |
| 24 | Sauvegarde, restauration, export, stockage | Partie 3 §11 | ⬜ |
| 25 | Paramètres complets, langues, accessibilité, aide, diagnostic réseau | Partie 3 §10, §13–15 | ⬜ |
| 26 | Durcissement final : checklist, charge, coupures réseau, sécurité, performance | Partie 3 §17–18 | ⬜ |

## Cœur à valider d'abord (si le temps manque)
Phases 0 à 5, puis 7 et 9. Une démonstration solide de ces phases vaut mieux qu'un projet entier à moitié fonctionnel.

## Journal de décisions et limites connues
(l'agent complète ici après chaque phase : choix de packages, écarts par rapport aux specs, bugs connus)

- **Phase 0** :
  - Choix d'implémentation MVVM native (ObservableObject, RelayCommand) dans `src/WhatsLAN.App/MVVM/` pour un découplage total et une fiabilité de compilation instantanée sans dépendance réseau.
  - Palette sombre issue de `NOTES_CAPTURES_REFERENCE.md` (#161717, #202C33, #0E0F0F, #21C063) et palette claire dans `Themes/Light.xaml` et `Themes/Dark.xaml`.
  - Intégration d'un bouton de bascule de thème clair/sombre en direct sur le rail de navigation (icône soleil/lune) pour vérifier immédiatement les deux thèmes.
  - Données factices génériques strictement isolées dans `DesignTimeData.cs` conformément à la règle d'AGENTS.md.

- **Correction ergonomie** :
  - Résolution du bogue de superposition visuelle entre le texte saisi et le filigrane (« Écrivez un message » / « Rechercher… ») via le convertisseur dédié `StringEmptyToVisibilityConverter`.
- **Phase 2** :
  - Implémentation du chiffrement E2E AES-256-GCM et dérivation de clé HKDF-SHA256 (`info="WhatsLAN-v1"`) dans `WhatsLAN.Core.Security.CryptoService`.
  - Format de trame binaire conforme au §4.3 : `[4 octets longueur big-endian][1 octet type][1 octet flags][nonce 12 octets][payload chiffré AES-GCM][tag 16 octets]`.
  - Découverte automatique des pairs en UDP broadcast sur le port 50000 avec paquets `HELLO` (période 3 s) et `BYE`, et bascule hors-ligne après 10 s d'inactivité.
  - Écoute TCP et gestion des connexions P2P directes avec `TcpServer`, arbitrage anti-collision par comparaison ordinale des GUIDs et poignée de main `Handshake` sécurisée.
  - Couche d'orchestration `NetworkService` connectée à l'UI WPF, mettant à jour la présence (points verts) et relayant les messages et accusés réels chiffrés.
  - Suite de 16 tests unitaires xUnit validant le chiffrement, les trames, la poignée de main réseau et l'échange chiffré.

- **Phase 3** :
  - Formatage dynamique des horodatages selon les spécifications françaises de WhatsApp Desktop (HH:mm, Hier, jour de semaine, dd/MM/yyyy) et pastilles de séparateurs de dates ("AUJOURD'HUI", "HIER", date longue) dans `DateTimeFormatter.cs`.
  - Intégration du sous-titre de contact temps réel ("en ligne", "vu aujourd’hui à HH:mm", "vu hier à HH:mm", "hors ligne").
  - Tri dynamique des conversations : épinglées en tête, puis tri par date de dernier message décroissante.
  - Fenêtre modale "Nouvelle discussion" (`NewChatWindow.xaml`) pour démarrer immédiatement une conversation avec un pair découvert sur le réseau local ou saisir une IP/Port.
  - Défilement automatique vers le bas lors de l'arrivée ou de l'envoi de nouveaux messages (`ScrollToBottom()`).
  - Ajout de 10 tests unitaires supplémentaires dans `DateTimeFormatterTests.cs` (26 tests xUnit au total, tous au vert).

- **Phase 4** :
  - Cycle de vie complet des accusés de réception sous les bulles et dans la liste des discussions : ⏱ horloge grise (`Pending`), ✓ simple coche grise (`Sent`), ✓✓ double coche grise (`Delivered`), ✓✓ double coche bleue (`Read`), ⚠ rouge (`Failed`) via `StatusToGeometryConverter` et `TicksColorConverter`.
  - Implémentation du protocole `ACK_READ` (§4.4, §9.3) chiffré en AES-GCM et mis à jour en base SQLite (`MarkMessagesAsRead`) à l'ouverture de la discussion ou à l'arrivée d'un message lorsque la discussion est active.
  - Détection temps réel de la frappe au clavier avec debounce et timer d'inactivité (envoi de `TYPING start` et `TYPING stop`).
  - Affichage réactif de « écrit… » en vert et italique dans le sous-titre de l'en-tête de discussion et dans l'aperçu de la liste des conversations avec réinitialisation automatique après 4 s sans signal.
  - File d'attente hors-ligne (Offline Queue, §4.5) : stockage automatique des messages en `Pending` lorsque le contact est inaccessible, et vidage/dépilement automatique ordonné dès sa reconnexion (`FlushPendingMessagesAsync`).
  - Ajout de 4 nouveaux tests unitaires xUnit dédiés (`ReceiptsAndTypingTests.cs`) portant le total de la suite de tests à 30 tests au vert.

## Résumé de la dernière phase
La Phase 4 apporte le moteur complet d'accusés de réception, d'indicateur de frappe, de gestion des non-lus et de file d'attente hors-ligne de WhatsApp Desktop. Les messages passent fidèlement par les 4 étapes visuelles sous la bulle : horloge ⏱ en attente, coche simple ✓ envoyée sur le réseau, double coche grise ✓✓ remise au destinataire et double coche bleue ✓✓ dès que la discussion est ouverte et lue par le contact. Lorsque l'interlocuteur tape au clavier, un paquet chiffré `TYPING` est émis et fait apparaître la mention « écrit… » en vert dans le sous-titre et la liste des discussions. Les messages entrants non lus incrémentent le badge vert de la conversation et sont automatiquement acquittés en lecture (`ACK_READ`) dès l'ouverture du fil. Si un contact est hors-ligne ou injoignable, les messages restent stockés en `Pending` dans SQLite et sont automatiquement et séquentiellement dépilés et remis dès sa réapparition sur le réseau local.
