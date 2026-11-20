# AGENTS.md — Projet WhatsLAN

Ce fichier est lu en premier par l'agent (Codex, Antigravity ou autre). Suis-le à la lettre.

## Projet
**WhatsLAN** : clone de WhatsApp Desktop en **C# 12 / .NET 8 / WPF (MVVM)**, qui fonctionne en **réseau local, 100 % pair-à-pair, sans serveur**. Le comportement doit être le plus fidèle possible à WhatsApp Desktop, de l'installation à la désinstallation.

## Où sont les spécifications (à relire avant CHAQUE phase)
- `docs/01_Partie1_Fonctionnement.md` : réseau, protocole, base de données, messages, fichiers, vocaux, groupes, appels, chiffrement, paramètres, phases 1 à 12.
- `docs/02_Partie2_Design_Statuts_Verrouillage.md` : design (couleurs, dimensions, XAML), statuts, verrouillage de discussion, confidentialité, extras, phases 0 et 13 à 16.
- `docs/03_Partie3_Installation_Compte_Checklist.md` : installation, compte/connexion/déconnexion, Windows, contacts, chaînes, communautés, sauvegarde, paramètres complets, checklist maîtresse, phases 17 à 26.
- `docs/logo_whatslan.svg` : logo officiel du projet. Ne jamais recréer ni utiliser un autre logo.
- `docs/reference/` : captures d'écran de WhatsApp Desktop fournies par l'utilisateur (référence visuelle). Si le dossier est vide, suis les valeurs de la Partie 2.
- `docs/reference/NOTES_CAPTURES_REFERENCE.md` : **observations visuelles tirées de vraies captures** de WhatsApp Desktop (rail, panneaux, bulles, barre de saisie pilule, paramètres, palette sombre estimée). **Pour la disposition et le style visuel, ce fichier PRIME sur la Partie 2.** Pour les comportements, l'ordre reste Partie 3 > Partie 2 > Partie 1. Il ne contient aucune donnée personnelle et n'en autorise aucune dans le code.

## Règles de priorité en cas de conflit
Partie 3 > Partie 2 > Partie 1. Le nom de l'application est **WhatsLAN** partout (remplacer toute occurrence de « WhatsLan »). Si un nom de package NuGet cité dans les docs n'existe pas, choisis le vrai package équivalent et note-le dans `PROGRESS.md`.

## Méthode de travail (obligatoire)
1. Travaille **une seule phase à la fois**, dans l'ordre du tableau de `PROGRESS.md`. Ne commence jamais une phase sans que l'utilisateur ait écrit « Phase N » ou « phase suivante ».
2. Avant de coder : relis les sections des docs concernées par la phase et liste en 5 lignes max ce que tu vas faire.
3. Code **complet et compilable** : pas de `TODO`, pas de `...`, pas de méthodes vides. Pas de données fictives dans le code final, sauf en Phase 0 (design).
4. Après le code : `dotnet build` doit réussir **sans erreur**. Corrige avant de continuer. Ajoute des tests (`WhatsLAN.Tests`, xUnit) pour la logique réseau/protocole/crypto/base de données quand c'est possible, et lance `dotnet test`.
5. Mets à jour `PROGRESS.md` : coche les éléments faits, note les limites connues et les choix faits.
6. Fais un **commit git** (`git add -A && git commit -m "Phase N: <résumé>"`).
7. Termine par un message court : (a) fichiers créés/modifiés, (b) **comment lancer et tester à la main** (commandes exactes, ex. `dotnet run --project src/WhatsLAN.App -- --port 50011 --data C:\temp\userB`), (c) pièges connus. Puis **arrête-toi** et attends l'utilisateur.
8. Si une spécification est ambiguë : choisis le comportement de WhatsApp Desktop, note-le en une ligne dans `PROGRESS.md`, continue. Ne pose pas de question sauf si une décision est irréversible.
9. Ne supprime jamais de fonctionnalité déjà livrée. Ne refactore pas en masse sans le dire.

## RÈGLE ABSOLUE : AUCUNE DONNÉE EN DUR, CAPTURES = RÉFÉRENCE VISUELLE UNIQUEMENT
- Les captures d'écran de `docs/reference/` servent **uniquement** à imiter l'apparence : couleurs, espacements, tailles, positions, formes, icônes.
- Il est **interdit** de recopier dans le code, les fichiers de ressources, les tests ou les données de démonstration le moindre contenu visible sur une capture : noms de personnes, pseudos, messages, numéros, heures, photos, noms de groupes, statuts. Si une capture contient des informations personnelles, ignore-les totalement.
- Toute donnée affichée par l'application doit venir de la **base SQLite**, du **réseau** (autres instances) ou de la **saisie de l'utilisateur**. Aucun contact, message, statut ou appel écrit « en dur » dans le code final.
- Les textes d'interface fixes (libellés de boutons, menus, messages d'erreur) viennent des fichiers de ressources `.resx` (FR/EN) et des specs de `docs/`, jamais d'une capture.
- **Exception unique, Phase 0 (design)** : des données factices **génériques et inventées** (ex. « Contact 1 », « Message de test ») sont autorisées pour valider le visuel, mais uniquement dans un seul fichier clairement nommé `DesignTimeData.cs`, utilisé seulement par le mode design. Ce fichier est **supprimé ou désactivé** dès la Phase 1 (le design-time ne doit plus alimenter l'application réelle).
- Avant chaque commit, vérifie par recherche dans le code (`grep`) qu'aucune donnée factice ne reste utilisée en exécution normale. Signale dans `PROGRESS.md` tout écart.

## Structure du dépôt à créer en Phase 1
```
WhatsLAN/
  WhatsLAN.sln
  src/
    WhatsLAN.App/      (WPF, MVVM)
    WhatsLAN.Core/     (logique sans UI : réseau, crypto, stockage)
    WhatsLAN.Tests/    (xUnit)
  installer/           (script Inno Setup, Phase 17)
  docs/                (specs, ne pas modifier sauf PROGRESS)
  AGENTS.md
  PROGRESS.md
```
`Core` ne référence jamais WPF. Toutes les opérations réseau et disque sont `async` ; l'interface ne doit jamais se figer.

## Qualité exigée
- Aucune exception non gérée ; journalisation dans `%AppData%\WhatsLAN\logs\`.
- Interface fidèle aux valeurs de la Partie 2 (couleurs, tailles, icônes, animations).
- Thème clair/sombre commutable à chaud ; textes en FR/EN via `.resx` (au minimum FR dès le début, prévoir EN).
- Sécurité : E2E (ECDH P-256 + AES-256-GCM), pas de secret en clair dans les logs.
- Tests manuels : deux instances sur la même machine (ports et dossiers de données différents).

## Environnement
Windows 10/11, .NET 8 SDK, Git, Visual Studio 2022 (pour l'utilisateur). WPF ne compile que sous Windows. Si tu travailles dans un environnement sans Windows, écris le code et les tests logiques (Core), indique clairement ce qui n'a pas pu être compilé/lancé, et n'affirme jamais qu'une interface « fonctionne » sans l'avoir exécutée.

## Interdits
Numéro de téléphone/SMS, serveur central, API cloud, télémétrie, publicités, logos ou marques tierces, fonctionnalités listées en section 16 de la Partie 3.
