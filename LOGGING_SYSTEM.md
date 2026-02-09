# Système de Logs - Ploco

## Vue d'ensemble

Le système de logs de Ploco enregistre toutes les opérations importantes de l'application pour faciliter le diagnostic des bugs et comprendre le comportement de l'application.

## Emplacement des logs

Les fichiers de logs sont stockés dans :
```
%AppData%\Ploco\Logs\
```

Chemin complet typique :
```
C:\Users\[VotreNom]\AppData\Roaming\Ploco\Logs\
```

## Nom des fichiers

Chaque session de l'application crée un nouveau fichier de log avec un timestamp :
```
Ploco_YYYY-MM-DD_HH-mm-ss.log
```

Exemple : `Ploco_2026-02-09_15-30-45.log`

## Accès aux logs

### Via le menu de l'application
1. Ouvrir l'application Ploco
2. Menu **Option** → **Logs**
3. Le dossier des logs s'ouvre dans l'Explorateur Windows

### Accès manuel
Appuyez sur `Windows + R`, tapez :
```
%AppData%\Ploco\Logs
```

## Niveaux de logs

Le système utilise 4 niveaux de logs :

### DEBUG
- Messages de débogage détaillés
- Utilisé pour comprendre le flux d'exécution
- Exemple : "Opening status dialog for loco 1234"

### INFO
- Informations sur les opérations normales
- Succès d'opérations
- Exemple : "Successfully moved loco 1234 to Track 5"

### WARNING
- Situations anormales mais gérées
- Actions potentiellement problématiques
- Exemple : "Cannot move loco to occupied rolling line"

### ERROR
- Erreurs avec traces d'exception
- Problèmes qui empêchent une opération
- Inclut la stack trace complète

## Ce qui est enregistré

### 1. Cycle de vie de l'application
- Démarrage de l'application
- Chargement de la fenêtre principale
- Nombre de locomotives et tuiles chargées
- Fermeture de l'application (avec ou sans annulation)

### 2. Mouvements de locomotives
- Déplacement d'une locomotive vers une voie
- Track source et destination
- Index de position
- Tentatives de déplacement bloquées

### 3. Changements de statut
- Statut avant et après
- Numéro de locomotive
- Annulation de changement

### 4. Placement prévisionnel
- Activation du mode prévisionnel
- Ligne de roulement cible
- Création du ghost
- Annulation du placement
- Validation du placement
- Remplacement de locomotives existantes

### 5. Opérations de réinitialisation
- Réinitialisation des locomotives
- Réinitialisation des tuiles
- Nombre d'éléments affectés

### 6. Erreurs
- Exceptions avec messages
- Stack traces complètes
- Inner exceptions

## Format des logs

Chaque ligne de log contient :
```
[Timestamp] [Level] [Context] Message
```

Exemple :
```
[2026-02-09 15:30:45.123] [INFO   ] [Movement] Successfully moved loco 1234 to Track 5
[2026-02-09 15:30:50.456] [WARNING] [Forecast] Ghost not found for loco 1234 in track Line 1101
[2026-02-09 15:31:00.789] [ERROR  ] [Menu] Failed to open logs folder
Exception: IOException
Message: Access denied
StackTrace: ...
```

## Gestion automatique

### Rotation des logs
- Les logs de plus de 30 jours sont automatiquement supprimés
- Nettoyage effectué au démarrage de chaque session
- Permet de garder l'historique récent sans consommer trop d'espace

### Thread-safe
- Le système de logs est thread-safe
- Plusieurs opérations peuvent logger en même temps
- Pas de corruption de fichier

## Utilisation pour le débogage

### Trouver un bug
1. Reproduire le bug dans l'application
2. Ouvrir le dossier des logs (Menu Option → Logs)
3. Ouvrir le dernier fichier de log
4. Rechercher les messages ERROR ou WARNING
5. Examiner le contexte autour de l'erreur

### Comprendre une séquence d'opérations
1. Effectuer les opérations dans l'application
2. Ouvrir le log correspondant
3. Chercher par numéro de locomotive ou nom de track
4. Suivre la séquence temporelle des opérations

### Exemples de recherche

**Trouver tous les mouvements d'une locomotive :**
```
Rechercher : "loco 1234"
```

**Trouver toutes les erreurs :**
```
Rechercher : "[ERROR"
```

**Trouver les opérations de prévision :**
```
Rechercher : "[Forecast]"
```

## Contextes disponibles

Les logs utilisent différents contextes pour catégoriser les messages :

- `Application` : Démarrage, fermeture, chargement
- `Movement` : Mouvements de locomotives
- `Status` : Changements de statut
- `Forecast` : Placement prévisionnel
- `Reset` : Opérations de réinitialisation
- `Menu` : Actions de menu
- `Logger` : Opérations du système de logs lui-même

## Confidentialité

- Les logs sont stockés localement sur votre machine
- Aucune donnée n'est envoyée en ligne
- Les logs peuvent être supprimés manuellement à tout moment
- Supprimer le dossier `%AppData%\Ploco\Logs` supprime tous les logs

## Dépannage

### Le dossier ne s'ouvre pas
- Vérifier que Windows Explorer fonctionne
- Ouvrir manuellement : `%AppData%\Ploco\Logs`
- Vérifier les permissions du dossier

### Pas de fichiers de logs
- Vérifier que l'application a les droits d'écriture
- Vérifier l'espace disque disponible
- Le dossier est créé automatiquement au premier lancement

### Fichiers de logs trop volumineux
- Le système garde seulement 30 jours d'historique
- Vous pouvez supprimer manuellement les anciens logs
- Chaque session crée un nouveau fichier

## Support

Si vous rencontrez un bug :
1. Reproduire le bug
2. Récupérer le fichier de log correspondant
3. Rechercher les messages ERROR
4. Partager le contexte avec l'équipe de support
