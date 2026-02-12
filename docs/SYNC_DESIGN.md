# 🔄 Conception de la Synchronisation Multi-Utilisateurs - PlocoManager

**Date** : 12 février 2026  
**Version** : 1.0  
**Statut** : Document de Conception

---

## 📋 Table des Matières

1. [Contexte et Besoin](#-contexte-et-besoin)
2. [Analyse du Code Existant](#-analyse-du-code-existant)
3. [Exigences Fonctionnelles](#-exigences-fonctionnelles)
4. [Solutions Proposées](#-solutions-proposées)
5. [Comparaison des Approches](#-comparaison-des-approches)
6. [Architecture Recommandée](#-architecture-recommandée)
7. [Plan d'Implémentation](#-plan-dimplémentation)
8. [Considérations Techniques](#-considérations-techniques)

---

## 🎯 Contexte et Besoin

### Situation Actuelle

PlocoManager est actuellement une **application desktop standalone** qui fonctionne avec :
- Une base de données SQLite locale (`ploco.db`)
- Des fichiers de configuration JSON locaux
- Aucune synchronisation entre instances

### Besoin Exprimé

**Scénario** : Deux utilisateurs travaillent simultanément sur des PC différents connectés en réseau

**Exigences** :
1. **Rôle Master** : Un utilisateur peut modifier les données
2. **Rôle Consultant** : Les autres utilisateurs visualisent les modifications en temps réel
3. **Transfert de Master** : Quand le master termine sa journée, quelqu'un d'autre peut prendre le relais
4. **Synchronisation temps réel** : Les modifications du master sont immédiatement visibles chez les consultants

---

## 🔍 Analyse du Code Existant

### Architecture Actuelle

#### Modèle de Données
```
AppState
├── Series (List<RollingStockSeries>)
├── Locomotives (List<LocomotiveModel>)
└── Tiles (List<TileModel>)
    └── Tracks (ObservableCollection<TrackModel>)
        └── Locomotives (ObservableCollection<LocomotiveModel>)
```

#### Persistance
- **PlocoRepository** : Classe centralisée pour l'accès aux données
- **SQLite** : Base de données relationnelle locale
- **Méthodes clés** :
  - `LoadState()` : Charge toutes les données au démarrage
  - `SaveState(AppState)` : Sauvegarde complète de l'état
  - Operations CRUD spécifiques (locomotives, tiles, tracks)

#### Interface Utilisateur
- **WPF** avec **ObservableCollection** pour le binding MVVM
- **INotifyPropertyChanged** sur tous les modèles
- Mise à jour automatique de l'UI via les bindings
- Canvas interactif pour le drag & drop

### Points Forts pour la Synchronisation

✅ **Architecture MVVM** : Séparation claire entre données et présentation  
✅ **ObservableCollection** : Mise à jour automatique de l'UI  
✅ **Repository Pattern** : Couche d'abstraction des données  
✅ **INotifyPropertyChanged** : Notifications de changements  

### Points à Adapter

⚠️ **SQLite local** : Non conçu pour l'accès concurrent réseau  
⚠️ **Pas de versioning** : Aucun système de gestion de conflits  
⚠️ **Sauvegarde complète** : `SaveState()` écrase tout (pas d'updates incrémentaux)  
⚠️ **Pas d'authentification** : Aucun concept d'utilisateur  

---

## 📝 Exigences Fonctionnelles

### Exigences Principales

#### 1. Gestion des Rôles
- **Master** :
  - Peut modifier toutes les données (locomotives, voies, tiles)
  - Peut transférer le rôle Master à un autre utilisateur
  - Un seul Master actif à la fois
  
- **Consultant** :
  - Visualisation en lecture seule
  - Reçoit les mises à jour en temps réel
  - Peut demander le rôle Master (avec accord de l'actuel Master)

#### 2. Synchronisation
- **Temps Réel** : Latence < 2 secondes
- **Opérations à synchroniser** :
  - Déplacement de locomotives (drag & drop)
  - Changement de statut (OK, HS, ManqueTraction, DefautMineur)
  - Ajout/suppression de tiles
  - Modification des voies (tracks)
  - Placement prévisionnel (forecast)
  - Import par lot

#### 3. Gestion des Sessions
- Détection de la connexion/déconnexion des utilisateurs
- Transfert automatique du Master si déconnexion
- Reconnexion automatique après perte réseau

#### 4. Intégrité des Données
- Pas de perte de données en cas de panne
- Gestion des conflits (rare avec système Master/Consultant)
- Validation des opérations avant synchronisation

---

## 💡 Solutions Proposées

### Solution 1 : SQLite Partagé sur Réseau (Simple mais Limité)

#### Principe
Placer le fichier `ploco.db` sur un partage réseau accessible par tous les utilisateurs.

#### Architecture
```
┌─────────────┐        ┌─────────────┐
│  Client 1   │        │  Client 2   │
│  (Master)   │        │ (Consultant)│
└──────┬──────┘        └──────┬──────┘
       │                      │
       │    Réseau Local      │
       │                      │
       └──────────┬───────────┘
                  │
         ┌────────▼─────────┐
         │  \\server\share  │
         │    ploco.db      │
         └──────────────────┘
```

#### Avantages
- ✅ Très simple à implémenter
- ✅ Pas de serveur à maintenir
- ✅ Fonctionne sur réseau Windows existant
- ✅ Pas de modification majeure du code

#### Inconvénients
- ❌ SQLite pas optimisé pour accès concurrent réseau
- ❌ Risque de corruption de base de données
- ❌ Pas de notifications temps réel (polling nécessaire)
- ❌ Performances dégradées sur réseau
- ❌ Verrouillage de fichier conflictuel

#### Implémentation
1. Ajouter un paramètre de chemin de base de données partagé
2. Implémenter un système de polling (ex: toutes les 2 secondes)
3. Ajouter un fichier de verrou pour le Master
4. Gérer le mode lecture seule pour Consultants

#### Verdict
⚠️ **Non recommandé** pour un usage professionnel (risque de corruption)  
✅ **Acceptable** pour un prototype rapide ou test

---

### Solution 2 : Serveur WebSocket Central (Recommandé)

#### Principe
Un serveur central gère la synchronisation via WebSocket pour communication bidirectionnelle temps réel.

#### Architecture
```
┌─────────────┐                    ┌─────────────┐
│  Client 1   │  WebSocket         │  Client 2   │
│  (Master)   │◄──────────┐   ┌───►│ (Consultant)│
└─────┬───────┘           │   │    └──────┬──────┘
      │ Local SQLite      │   │           │ Local SQLite
      │ ploco.db          │   │           │ ploco.db (readonly)
      └───────────────────┼───┼───────────┘
                          │   │
                    ┌─────▼───▼──────┐
                    │  Serveur Sync  │
                    │  (ASP.NET Core)│
                    │                │
                    │  - WebSocket   │
                    │  - SignalR     │
                    │  - SQLite      │
                    └────────────────┘
```

#### Composants

**1. Serveur de Synchronisation** (ASP.NET Core)
- Service Windows ou console standalone
- SignalR pour WebSocket
- SQLite centralisé pour l'état partagé
- API REST pour opérations administratives
- Gestion des rôles Master/Consultant

**2. Client PlocoManager** (modifications)
- Client SignalR intégré
- Mode "Local" ou "Synchronisé"
- Désactivation des contrôles en mode Consultant
- Queue de messages pour garantir l'ordre

**3. Protocole de Messages**
```json
{
  "type": "LocomotiveMove",
  "timestamp": "2026-02-12T10:30:00Z",
  "userId": "user1",
  "data": {
    "locomotiveId": 123,
    "fromTrackId": 5,
    "toTrackId": 8,
    "offsetX": 120.5
  }
}
```

#### Avantages
- ✅ Communication temps réel (< 100ms latence)
- ✅ Gestion native des rôles et permissions
- ✅ Scalable (peut supporter 10-100+ utilisateurs)
- ✅ Base de données centralisée cohérente
- ✅ Historique complet des actions
- ✅ Pas de risque de corruption
- ✅ Reconnexion automatique
- ✅ Fonctionne sur Internet (pas que LAN)

#### Inconvénients
- ❌ Complexité accrue (serveur à déployer)
- ❌ Maintenance du serveur nécessaire
- ❌ Modifications importantes du code client
- ❌ Nécessite un serveur toujours disponible

#### Implémentation

**Phase 1 : Serveur de Base**
1. Créer un nouveau projet ASP.NET Core
2. Intégrer SignalR pour WebSocket
3. Implémenter la gestion des sessions
4. Créer le système de messages

**Phase 2 : Client**
1. Ajouter le package Microsoft.AspNetCore.SignalR.Client
2. Créer un service SyncService
3. Intercepter les modifications via INotifyPropertyChanged
4. Envoyer les changements au serveur
5. Recevoir et appliquer les changements distants

**Phase 3 : Gestion des Rôles**
1. Système de login simple (nom d'utilisateur)
2. Attribution du rôle Master
3. UI pour transférer le Master
4. Désactivation des contrôles pour Consultants

**Phase 4 : Robustesse**
1. Queue de messages avec retry
2. Détection de déconnexion
3. Réconciliation après reconnexion
4. Gestion des erreurs

#### Verdict
✅ **Fortement recommandé** pour un usage professionnel  
✅ Correspond à la vision du ROADMAP (v1.3.0, v2.0.0)

---

### Solution 3 : Hybrid (SQLite + File Watcher)

#### Principe
Utiliser un dossier partagé avec surveillance de fichiers pour détecter les changements.

#### Architecture
```
┌─────────────┐                    ┌─────────────┐
│  Client 1   │  Write changes     │  Client 2   │
│  (Master)   │                    │ (Consultant)│
└─────┬───────┘                    └──────┬──────┘
      │                                   │
      │  \\server\ploco\changes\          │
      ├──► change_001.json                │
      │    change_002.json  ◄─────────────┤ Read
      │    master.lock                    │
      │    ploco_master.db                │
      └───────────────────────────────────┘
```

#### Principe de Fonctionnement
1. Le Master écrit dans `ploco_master.db`
2. Chaque changement génère un fichier JSON timestampé
3. Les Consultants surveillent le dossier (FileSystemWatcher)
4. Les Consultants appliquent les changements incrémentaux

#### Avantages
- ✅ Pas de serveur dédié nécessaire
- ✅ Fonctionne sur partage réseau Windows
- ✅ Historique des changements persistent
- ✅ Plus robuste que SQLite partagé
- ✅ Implémentation intermédiaire

#### Inconvénients
- ❌ Latence plus élevée (2-5 secondes)
- ❌ Dépendant de la performance du réseau
- ❌ Complexité de la synchronisation
- ❌ Gestion des fichiers à nettoyer périodiquement
- ❌ Pas de garantie d'ordre strict

#### Implémentation
1. Créer un service FileSystemWatcher
2. Sérialiser chaque changement en JSON
3. Numérotation séquentielle des changements
4. Appliquer les changements dans l'ordre
5. Nettoyage périodique des anciens fichiers

#### Verdict
⚠️ **Compromis acceptable** si serveur non envisageable  
✅ Meilleur que Solution 1, moins bon que Solution 2

---

### Solution 4 : Cloud-Based (Azure/AWS) - Future

#### Principe
Utiliser un service cloud managé pour la synchronisation.

#### Options
- **Azure SQL Database** + **Azure SignalR Service**
- **AWS RDS** + **AWS AppSync**
- **Firebase Realtime Database**

#### Avantages
- ✅ Infrastructure managée (pas de maintenance)
- ✅ Haute disponibilité
- ✅ Scalabilité automatique
- ✅ Sauvegardes automatiques
- ✅ Accès depuis Internet

#### Inconvénients
- ❌ Coût mensuel récurrent
- ❌ Dépendance à Internet
- ❌ Problèmes de latence possible
- ❌ Confidentialité des données (cloud tiers)
- ❌ Modification importante de l'architecture

#### Verdict
🔮 **Option future** (mentionnée dans ROADMAP v1.3.0)  
❌ Probablement excessif pour le besoin actuel

---

## ⚖️ Comparaison des Approches

| Critère | SQLite Réseau | WebSocket Serveur | Hybrid Files | Cloud |
|---------|--------------|-------------------|--------------|-------|
| **Complexité** | ⭐ | ⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Performance** | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Fiabilité** | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Temps Réel** | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Coût** | Gratuit | Gratuit (serveur local) | Gratuit | $$$/mois |
| **Maintenance** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Scalabilité** | ⭐ | ⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ |

**Légende** : ⭐ Très faible → ⭐⭐⭐⭐⭐ Excellent

---

## 🏗️ Architecture Recommandée

### Recommandation Principale : **Solution 2 - Serveur WebSocket**

#### Pourquoi ?

1. **Alignement avec le ROADMAP** : Préparation pour v1.3.0 (Sync multi-postes) et v2.0.0 (Multi-utilisateurs)
2. **Qualité professionnelle** : Fiable, performant, scalable
3. **Expérience utilisateur** : Temps réel, pas de latence perceptible
4. **Évolutivité** : Base solide pour futures fonctionnalités (chat, notifications, etc.)

#### Architecture Détaillée

```
┌──────────────────────────────────────────────────────────────┐
│                    CLIENT PLOCOMANAGER                       │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────┐      ┌──────────────┐    ┌─────────────┐  │
│  │ MainWindow  │◄────►│  SyncService │◄──►│  SignalR    │  │
│  │  (UI)       │      │              │    │  Client     │  │
│  └──────┬──────┘      └──────┬───────┘    └──────┬──────┘  │
│         │                    │                   │         │
│  ┌──────▼──────┐      ┌──────▼───────┐           │         │
│  │ ViewModels  │      │ Local Cache  │           │         │
│  │ (MVVM)      │      │ (SQLite)     │           │         │
│  └─────────────┘      └──────────────┘           │         │
│                                                   │         │
└───────────────────────────────────────────────────┼─────────┘
                                                    │
                                            WebSocket/SignalR
                                                    │
┌───────────────────────────────────────────────────┼─────────┐
│                   SERVEUR SYNC                    │         │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────┐      ┌──────────────┐    ┌─────────────┐  │
│  │  SignalR    │◄────►│  SyncHub     │◄──►│ Repository  │  │
│  │  Hub        │      │  (Logic)     │    │             │  │
│  └─────────────┘      └──────┬───────┘    └──────┬──────┘  │
│                              │                   │         │
│  ┌─────────────┐      ┌──────▼───────┐    ┌──────▼──────┐  │
│  │ Role Mgr    │      │ Session Mgr  │    │   SQLite    │  │
│  │ (Master/    │      │ (Conn/Disco) │    │  (Central)  │  │
│  │ Consultant) │      └──────────────┘    └─────────────┘  │
│  └─────────────┘                                           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

#### Flux de Données

**1. Changement Local (Master)**
```
User Action → ViewModel → SyncService → SignalR Client → Server → Broadcast → Consultants
                    ↓
              Local Cache Update
```

**2. Réception Changement (Consultant)**
```
Server → SignalR Client → SyncService → Apply to Cache → Refresh ViewModel → UI Update
```

#### Messages Types

```csharp
// Message de base
public class SyncMessage
{
    public string MessageId { get; set; }      // GUID unique
    public string MessageType { get; set; }    // Type d'opération
    public string UserId { get; set; }         // Émetteur
    public DateTime Timestamp { get; set; }    // Timestamp
    public object Data { get; set; }           // Payload
}

// Types de messages
- "LocomotiveMove"          // Déplacement locomotive
- "LocomotiveStatusChange"  // Changement statut
- "TileCreate"              // Création tile
- "TileUpdate"              // Modification tile
- "TrackUpdate"             // Modification track
- "MasterTransfer"          // Transfert rôle Master
- "UserConnect"             // Connexion utilisateur
- "UserDisconnect"          // Déconnexion
- "BatchImport"             // Import par lot
```

---

## 📐 Plan d'Implémentation

### Phase 1 : Fondations (2-3 semaines)

#### Serveur
1. **Créer projet ASP.NET Core** (`PlocoSync.Server`)
   - Ajouter SignalR
   - Configurer SQLite
   - Structure de base

2. **Implémenter SyncHub**
   - Gestion des connexions
   - Broadcast des messages
   - Validation des opérations

3. **Gestion des Rôles**
   - Système de sessions
   - Attribution Master/Consultant
   - Transfert de rôle

#### Client
1. **Ajouter Package SignalR Client**
   ```xml
   <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
   ```

2. **Créer Service de Synchronisation** (`SyncService.cs`)
   - Connexion au serveur
   - Envoi de messages
   - Réception et application des changements

3. **Mode Sync dans Settings**
   - Option "Activer la synchronisation"
   - URL du serveur
   - Nom d'utilisateur

### Phase 2 : Synchronisation de Base (2-3 semaines)

1. **Intercepter les Modifications**
   - Hook sur les propriétés des modèles
   - Détecter les changements dans ObservableCollections
   - Créer les messages de synchronisation

2. **Appliquer les Modifications Distantes**
   - Recevoir les messages
   - Valider l'origine (Master only)
   - Mettre à jour les modèles locaux
   - Refresh UI

3. **Gestion du Mode Consultant**
   - Désactiver les contrôles d'édition
   - Afficher un indicateur visuel
   - Permettre la demande de Master

### Phase 3 : Robustesse (1-2 semaines)

1. **Gestion des Erreurs**
   - Retry automatique
   - Queue de messages
   - Validation des données

2. **Reconnexion Automatique**
   - Détection de perte de connexion
   - Tentative de reconnexion
   - Resynchronisation après reconnexion

3. **Conflits (rares mais possibles)**
   - Détection de modifications concurrentes
   - Résolution : Master gagne toujours
   - Notification à l'utilisateur

### Phase 4 : UI et Expérience (1 semaine)

1. **Indicateurs Visuels**
   - Icône de connexion (connecté/déconnecté)
   - Badge "Master" ou "Consultant"
   - Liste des utilisateurs connectés

2. **Boîte de Dialogue de Synchronisation**
   - Configuration du serveur
   - Gestion du rôle
   - Transfert du Master

3. **Notifications**
   - Utilisateur connecté/déconnecté
   - Transfert de rôle
   - Erreurs de synchronisation

### Phase 5 : Tests et Documentation (1 semaine)

1. **Tests**
   - Tests unitaires des composants
   - Tests d'intégration avec serveur
   - Tests de charge (5-10 utilisateurs)
   - Tests de robustesse (déconnexion/reconnexion)

2. **Documentation**
   - Guide d'installation du serveur
   - Guide utilisateur
   - Documentation développeur

---

## 🔧 Considérations Techniques

### Performance

#### Client
- **Throttling** : Ne pas envoyer plus de 10 messages/seconde
- **Batching** : Grouper les modifications rapides (ex: drag & drop)
- **Local First** : Toujours mettre à jour le cache local en premier

#### Serveur
- **Broadcasting efficace** : Utiliser les groups SignalR
- **Base de données** : Index appropriés sur SQLite
- **Mémoire** : Limiter la taille de l'historique en mémoire

### Sécurité

#### Authentification
- Phase 1 : Simple nom d'utilisateur (pas de password)
- Phase 2 : Authentification Windows (NTLM)
- Phase 3 : JWT tokens pour authentification

#### Autorisation
- Valider le rôle Master côté serveur
- Refuser les modifications venant de Consultants
- Logger toutes les actions pour audit

#### Réseau
- Utiliser WSS (WebSocket Secure) si exposé sur Internet
- Limiter l'accès au serveur (firewall)
- Chiffrement des données sensibles

### Compatibilité

#### Rétrocompatibilité
- Mode "Local" par défaut (comme actuellement)
- Option d'activer la synchronisation
- Application fonctionne sans serveur

#### Versions
- Versionner le protocole de messages
- Vérifier la compatibilité au connect
- Refuser les clients incompatibles

### Déploiement

#### Serveur
**Option 1 : Service Windows**
```bash
sc create PlocoSyncService binPath="C:\PlocoSync\PlocoSync.Server.exe"
sc start PlocoSyncService
```

**Option 2 : Console Application**
- Lancer manuellement
- Déployer sur un PC toujours allumé

**Option 3 : Docker Container** (avancé)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY PlocoSync.Server /app
ENTRYPOINT ["dotnet", "/app/PlocoSync.Server.dll"]
```

#### Configuration
```json
{
  "SyncServer": {
    "Url": "http://localhost:5000",
    "Enabled": true,
    "UserName": "Jean",
    "AutoReconnect": true,
    "ReconnectDelay": 5000
  }
}
```

---

## 🎯 Recommandations Finales

### Pour Démarrer

**Court Terme** (si besoin urgent) :
- Implémenter **Solution 3 (Hybrid Files)** comme proof-of-concept
- Test avec 2-3 utilisateurs
- Évaluer si acceptable pour usage réel

**Moyen Terme** (recommandé) :
- Implémenter **Solution 2 (WebSocket Serveur)**
- Développement en 6-8 semaines
- Base solide pour évolutions futures

### Migration Progressive

1. **v1.1.0** : Ajouter l'infrastructure (SyncService, mais pas actif)
2. **v1.2.0** : Activer en mode bêta (opt-in)
3. **v1.3.0** : Production (selon ROADMAP existant)
4. **v2.0.0** : Fonctionnalités avancées (permissions granulaires, etc.)

### Alternatives Simples pour Proto

Si le développement complet est trop long, considérer :

**Option Quick-Win** : Mode "Réplication"
- Le Master sauvegarde dans `ploco_master.db`
- Un script PowerShell copie périodiquement vers les Consultants
- Les Consultants ouvrent en lecture seule
- Simple mais unidirectionnel uniquement

---

## 📚 Ressources

### Technologies
- **SignalR** : https://docs.microsoft.com/aspnet/core/signalr/
- **ASP.NET Core** : https://docs.microsoft.com/aspnet/core/
- **SQLite** : https://www.sqlite.org/
- **WPF MVVM** : https://docs.microsoft.com/dotnet/desktop/wpf/

### Exemples
- SignalR Chat Sample : https://github.com/dotnet/AspNetCore.Docs/tree/main/aspnetcore/signalr/samples
- Real-time Collaboration : https://github.com/aspnet/SignalR-samples

---

## 📝 Conclusion

La synchronisation multi-utilisateurs est un **ajout majeur** à PlocoManager qui nécessite une **architecture solide**.

**Ma recommandation** : 
- **Solution 2 (WebSocket Serveur)** pour un système professionnel et pérenne
- Développement progressif sur 6-8 semaines
- Alignement parfait avec le ROADMAP existant (v1.3.0 et v2.0.0)

**Alternative temporaire** :
- **Solution 3 (Hybrid Files)** comme POC rapide (2-3 semaines)
- Permet de tester le concept avec les utilisateurs
- Migration vers Solution 2 ensuite

Cette conception constitue une **base de discussion** pour décider de la meilleure approche selon vos contraintes (temps, ressources, besoins).

---

**Prochaines Étapes** :
1. Valider l'approche choisie
2. Affiner les spécifications
3. Créer des user stories détaillées
4. Démarrer le développement par phases

**Questions ?** Ce document sera mis à jour selon les retours et décisions.

---

*Document préparé par Copilot pour LinkAtPlug - PlocoManager Project*
