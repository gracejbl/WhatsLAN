# PROGRESS.md — Suivi de WhatsLAN

Légende : ⬜ à faire · 🟨 en cours · ✅ terminé et testé à la main

## Ordre de travail (suivre cet ordre exact)

| # | Phase | Source | État |
|---|---|---|---|
| 0 | Design : thèmes clair/sombre, rail, 3 zones, bulles, données factices | Partie 2 §1–2 | ✅ |
| 1 | Fondations : solution, projets, SQLite, profil, modèles | Partie 1 §21 | ✅ |
| 2 | Réseau de base : UDP, TCP, handshake, chiffrement, présence | Partie 1 §4, §17 | ✅ |
| 3 | UI principale : liste, messages texte, historique | Partie 1 §6–10 | ⬜ |
| 4 | Accusés, « écrit… », non-lus, file hors-ligne | Partie 1 §9.3 | ⬜ |
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

## Résumé de la dernière phase
(l'agent écrit ici 10 lignes d'explication simple de ce qui a été fait et où se trouve chaque partie, utile pour la présentation orale)

- Correction du masquage du placeholder de saisie pour empêcher la superposition avec le texte tapé par l'utilisateur.
- Implémentation du service cryptographique `CryptoService.cs` avec AES-256-GCM et dérivation de clé HKDF-SHA256.
- Réalisation du cadrage binaire de paquets `PacketFraming.cs` selon la structure exacte imposée par les spécifications.
- Développement du service de découverte UDP broadcast `DiscoveryService.cs` (port 50000, paquets HELLO et BYE).
- Implémentation de la détection de présence automatique avec passage au statut hors-ligne après 10 secondes sans annonce.
- Création du serveur d'écoute TCP `TcpServer.cs` et du gestionnaire de connexions chiffrées `ConnectionManager.cs`.
- Intégration de la poignée de main `Handshake` avec dérivation réciproque de la clé de session AES-256.
- Orchestration complète via `NetworkService.cs` avec persistance SQLite et accusés de réception automatiques.
- Raccordement temps réel à l'UI : mise à jour instantanée de la présence et réception en direct des messages sans rafraîchissement.
- Exécution avec succès de 16 tests unitaires xUnit validant l'ensemble de la pile réseau et cryptographique.
