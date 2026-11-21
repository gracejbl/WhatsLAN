# PROGRESS.md — Suivi de WhatsLAN

Légende : ⬜ à faire · 🟨 en cours · ✅ terminé et testé à la main

## Ordre de travail (suivre cet ordre exact)

| # | Phase | Source | État |
|---|---|---|---|
| 0 | Design : thèmes clair/sombre, rail, 3 zones, bulles, données factices | Partie 2 §1–2 | ✅ |
| 1 | Fondations : solution, projets, SQLite, profil, modèles | Partie 1 §21 | ✅ |
| 2 | Réseau de base : UDP, TCP, handshake, chiffrement, présence | Partie 1 §4, §17 | ⬜ |
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

- **Phase 1** :
  - Création de la bibliothèque `src/WhatsLAN.Core` (.NET 8) strictement découplée de toute dépendance WPF.
  - Implémentation du stockage SQLite avec `Microsoft.Data.Sqlite` (`Database`, `ProfileRepository`, `ConversationRepository`, `MessageRepository`, `SettingsRepository`) en mode WAL.
  - Cryptographie asymétrique ECDH NIST P-256 avec dérivation de secret partagé via `System.Security.Cryptography` (`KeyManager`).
  - Écran de bienvenue `WelcomeWindow` pour la création d'identité au premier lancement (pseudo 1-25 car., statut, génération GUID et clés).
  - Gestion des arguments `--data` et `--port` dans `App.xaml.cs` pour exécuter plusieurs instances indépendantes sur une même machine.
  - Déconnexion complète de `DesignTimeData.cs` de l'exécution normale : l'application est alimentée exclusivement par SQLite.
  - Projet de tests `src/WhatsLAN.Tests` créé avec 9 tests unitaires validant la crypto et les opérations SQLite, tous passés avec succès.

## Résumé de la dernière phase
(l'agent écrit ici 10 lignes d'explication simple de ce qui a été fait et où se trouve chaque partie, utile pour la présentation orale)

- Création du projet de logique métier `src/WhatsLAN.Core` et du projet de tests xUnit `src/WhatsLAN.Tests`.
- Modélisation des entités clés dans `WhatsLAN.Core.Models` : `Profile`, `Contact`, `Conversation`, `Message`, `Attachment`, `Setting`.
- Mise en place du moteur SQLite `Database.cs` avec pragmas WAL et création automatique de toutes les tables nécessaires.
- Développement des dépôts SQLite pour profils, conversations, messages et paramètres système dans `WhatsLAN.Core.Storage`.
- Développement du gestionnaire cryptographique `KeyManager.cs` générant les paires de clés ECDH P-256 et calculant le secret partagé.
- Implémentation de `ProfileService.cs` pour la validation du pseudo, la persistance du compte et la gestion de l'identité locale.
- Création de l'écran de premier lancement `WelcomeWindow.xaml` invitant l'utilisateur à créer son compte avant d'ouvrir la fenêtre principale.
- Prise en charge des arguments CLI `--data` et `--port` dans `App.xaml.cs` permettant de tester plusieurs instances sur le même poste.
- Raccordement des ViewModels à la base SQLite réelle et arrêt définitif de l'alimentation par données factices.
- Écriture et exécution réussie de 9 tests unitaires xUnit couvrant les échanges de clés ECDH, l'accès base et la validation.
