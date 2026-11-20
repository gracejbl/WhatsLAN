# Démarrage rapide — WhatsLAN avec Codex ou Antigravity

## Contenu du dossier
```
WhatsLAN/
  AGENTS.md                  Consignes lues automatiquement par l'agent
  PROGRESS.md                Suivi : l'agent le met à jour à chaque phase
  README_DEMARRAGE.md        Ce fichier
  docs/
    01_Partie1_Fonctionnement.md
    02_Partie2_Design_Statuts_Verrouillage.md
    03_Partie3_Installation_Compte_Checklist.md
    logo_whatslan.svg
    reference/               Mets ici tes captures d'écran de WhatsApp Desktop
```

## Ce qu'il faut sur ton PC (Windows 10/11)
1. **.NET 8 SDK** : https://dotnet.microsoft.com/download/dotnet/8.0 (vérifie avec `dotnet --version`)
2. **Git** : https://git-scm.com/download/win
3. **Visual Studio 2022 Community** (gratuit), cocher « Développement .NET Desktop »
4. (Plus tard, Phase 17) **Inno Setup** : https://jrsoftware.org/isinfo.php

## Mise en place (5 minutes)
1. Décompresse le zip dans un dossier simple, par ex. `C:\Projets\WhatsLAN`.
2. (Optionnel) Mets 2 à 4 **captures d'écran de WhatsApp Desktop**, sans informations personnelles (floutées ou recadrées), dans `docs/reference/` (liste des discussions, une conversation, les statuts, les paramètres).
3. Ouvre un terminal dans le dossier et initialise Git :
   ```
   git init
   git add -A
   git commit -m "Specs initiales"
   ```
4. Ouvre le dossier dans ton agent :
   - **Codex** : ouvre le dossier du projet (il lit `AGENTS.md` tout seul).
   - **Antigravity** : ouvre le dossier comme espace de travail (il lit `AGENTS.md`). S'il ne le prend pas en compte, colle le texte de `AGENTS.md` dans ses règles/instructions de projet.

## Premier message à envoyer à l'agent (copier-coller)
> Lis `AGENTS.md`, `PROGRESS.md` et les trois fichiers de `docs/`. Confirme en 10 lignes que tu as compris le projet et l'ordre des phases. N'écris pas encore de code. Puis attends que je te dise « Phase 0 ».

## Ensuite, pour chaque phase
1. Tu écris : `Phase 0` (puis `Phase 1`, etc.).
2. L'agent code, compile (`dotnet build`), teste, met à jour `PROGRESS.md` et fait un commit.
3. **Toi, tu lances l'application** (Visual Studio → F5, ou la commande donnée par l'agent) et tu vérifies à la main ce que la phase promet.
4. Si ça marche : « phase suivante ». Sinon : copie le message d'erreur ou joins une capture d'écran et dis « corrige ».

## Tester le réseau avec 2 instances sur le même PC
```
dotnet run --project src/WhatsLAN.App -- --port 50011 --data C:\temp\userA
dotnet run --project src/WhatsLAN.App -- --port 50012 --data C:\temp\userB
```
Si les deux ne se voient pas : autorise l'application dans le pare-feu Windows (réseau privé).

## Si l'agent s'arrête (limite d'usage, session perdue)
Ouvre une nouvelle session et envoie : « Lis `AGENTS.md` et `PROGRESS.md`, puis reprends à la première phase non terminée. » Tout l'état est dans les fichiers et les commits Git.

## Conseils importants
- Ne demande jamais « fais tout d'un coup » : le résultat serait incomplet. Une phase à la fois.
- Garde la **Phase 0 à 5** comme priorité, puis 7 (fichiers) et 9 (groupes).
- Après chaque phase, demande : « Explique-moi en 10 lignes ce que tu as fait et où est chaque partie ». Garde ces notes pour expliquer ton projet au professeur.
- Fais un commit après chaque phase réussie : tu peux toujours revenir en arrière (`git log`, `git checkout`).
- Relis l'énoncé du TP : si certaines fonctionnalités n'y figurent pas, tu peux sauter les phases correspondantes.
