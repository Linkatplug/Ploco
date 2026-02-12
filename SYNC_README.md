# 🔄 Guide de Synchronisation Multi-Utilisateurs - PlocoManager

## Vue d'Ensemble

La synchronisation WebSocket/SignalR permet à plusieurs utilisateurs de travailler simultanément sur PlocoManager avec un système Master/Consultant.

## Architecture

```
┌─────────────┐                    ┌─────────────┐
│  Client 1   │  WebSocket         │  Client 2   │
│  (Master)   │◄──────────┐   ┌───►│ (Consultant)│
└─────────────┘           │   │    └─────────────┘
                          │   │
                    ┌─────▼───▼──────┐
                    │  Serveur Sync  │
                    │  (ASP.NET Core)│
                    │   + SignalR    │
                    └────────────────┘
```

## 🚀 Démarrage Rapide

### 1. Lancer le Serveur

#### Option A : Visual Studio
1. Ouvrir `Ploco.sln` dans Visual Studio
2. Définir `PlocoSync.Server` comme projet de démarrage
3. Appuyer sur F5 ou Démarrer

#### Option B : Ligne de commande
```bash
cd PlocoSync.Server
dotnet run
```

Le serveur démarre sur `http://localhost:5000`

Vérifiez que ça fonctionne en ouvrant http://localhost:5000 dans un navigateur.

### Build serveur en EXE

Pour créer un exécutable autonome du serveur (sans nécessiter l'installation de .NET) :

#### Windows (64-bit)
```bash
cd PlocoSync.Server
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

L'exécutable sera créé dans : `bin/Release/net8.0/win-x64/publish/PlocoSync.Server.exe`

#### Linux (64-bit)
```bash
cd PlocoSync.Server
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
```

L'exécutable sera créé dans : `bin/Release/net8.0/linux-x64/publish/PlocoSync.Server`

#### Lancer le serveur EXE

Pour lancer le serveur sur toutes les interfaces réseau (accessible depuis d'autres PC) :

```bash
# Windows
PlocoSync.Server.exe --urls http://0.0.0.0:5000

# Linux
./PlocoSync.Server --urls http://0.0.0.0:5000
```

**Note** : `0.0.0.0` permet l'accès depuis d'autres machines du réseau. Pour localhost uniquement, utilisez `http://localhost:5000`.

#### Configuration du pare-feu

**Windows** :
```powershell
# Autoriser le port 5000 en entrée
netsh advfirewall firewall add rule name="PlocoSync Server" dir=in action=allow protocol=TCP localport=5000

# Ou via l'interface graphique :
# Panneau de configuration > Pare-feu Windows Defender > Paramètres avancés > Règles de trafic entrant > Nouvelle règle
```

**Linux** :
```bash
# UFW (Ubuntu/Debian)
sudo ufw allow 5000/tcp

# FirewallD (CentOS/RHEL/Fedora)
sudo firewall-cmd --permanent --add-port=5000/tcp
sudo firewall-cmd --reload

# iptables
sudo iptables -A INPUT -p tcp --dport 5000 -j ACCEPT
sudo iptables-save > /etc/iptables/rules.v4
```

**⚠️ Important** : N'ouvrez le port que sur les réseaux de confiance (réseau local). Pour un accès Internet, utilisez HTTPS et authentification.

### 2. Configurer les Clients

Pour l'instant, la synchronisation est **désactivée par défaut**.

Pour l'activer, modifiez la méthode `LoadSyncConfiguration()` dans `MainWindow.xaml.cs` :

```csharp
private SyncConfiguration LoadSyncConfiguration()
{
    return new SyncConfiguration
    {
        Enabled = true,  // ← Changer à true
        ServerUrl = "http://localhost:5000",
        UserId = Environment.UserName,
        UserName = Environment.UserName,
        AutoReconnect = true
    };
}
```

### 3. Tester avec 2 Clients

1. **Client 1** : Lancer PlocoManager
   - Se connecte automatiquement au serveur
   - Devient **Master** automatiquement (premier connecté)
   - Peut modifier les données

2. **Client 2** : Lancer une 2ème instance de PlocoManager
   - Se connecte automatiquement
   - Devient **Consultant** automatiquement
   - Voit les modifications du Master en temps réel

## 📋 Fonctionnalités Implémentées

### ✅ Phase 1 & 2 : Infrastructure (Complétée)
- [x] Serveur PlocoSync.Server avec SignalR
- [x] Gestion des sessions (SessionManager)
- [x] Hub SignalR (PlocoSyncHub)
- [x] Client SyncService
- [x] Modèles de messages
- [x] Connexion/Déconnexion automatique

### ✅ Phase 3 : Intégration de Base (Complétée)
- [x] SyncService intégré dans MainWindow
- [x] Réception de changements distants
- [x] Application des changements (locomotives, statuts, tiles)
- [x] Gestion des événements Master/Consultant
- [x] Logs de synchronisation

### 🚧 À Compléter

#### Phase 3 : Interception des Modifications
- [ ] Intercepter déplacements de locomotives (drag & drop)
- [ ] Intercepter changements de statut
- [ ] Intercepter modifications de tiles
- [ ] Envoyer les changements au serveur

#### Phase 4 : Interface Utilisateur
- [ ] Dialog de configuration de synchronisation
- [ ] Menu Options > Synchronisation
- [ ] Indicateur visuel Master/Consultant
- [ ] Indicateur de connexion
- [ ] Désactivation des contrôles en mode Consultant

#### Phase 5 : Tests et Documentation
- [ ] Tests de connexion
- [ ] Tests de synchronisation
- [ ] Tests de transfert Master
- [ ] Documentation utilisateur finale

## 🔍 Endpoints du Serveur

- `GET http://localhost:5000/` - Info serveur
- `GET http://localhost:5000/health` - Health check
- `GET http://localhost:5000/sessions` - Liste des sessions actives
- `WS  http://localhost:5000/syncHub` - Hub SignalR

## 📊 Types de Messages Supportés

### Actuellement Implémentés
1. **LocomotiveMove** - Déplacement d'une locomotive
2. **LocomotiveStatusChange** - Changement de statut
3. **TileUpdate** - Modification d'une tuile

### Structure d'un Message
```json
{
  "messageId": "uuid",
  "messageType": "LocomotiveMove",
  "userId": "Alice",
  "timestamp": "2026-02-12T10:30:00Z",
  "data": {
    "locomotiveId": 123,
    "fromTrackId": 5,
    "toTrackId": 8,
    "offsetX": 120.5
  }
}
```

## 🔧 Configuration

### Configuration Serveur
Modifier `PlocoSync.Server/appsettings.json` si nécessaire.

### Configuration Client
La configuration est actuellement en dur dans `LoadSyncConfiguration()`.

**TODO** : Créer un fichier de configuration séparé ou utiliser les settings existants.

## 📝 Logs

Les logs de synchronisation sont automatiquement écrits via le système Logger existant :
- Emplacement : `%AppData%\Ploco\Logs\`
- Contexte : `[Sync]`

Exemples de logs :
```
[2026-02-12 10:30:00.123] [INFO   ] [Sync] Synchronization service initialized
[2026-02-12 10:30:01.456] [INFO   ] [Sync] Connected as Master
[2026-02-12 10:30:15.789] [INFO   ] [Sync] Change sent: LocomotiveMove
[2026-02-12 10:30:16.012] [INFO   ] [Sync] Change received: LocomotiveMove
```

## 🐛 Dépannage

### Le serveur ne démarre pas
- Vérifier que le port 5000 n'est pas déjà utilisé
- Essayer de changer le port dans `Program.cs` : `app.Run("http://*:5001");`

### Le client ne se connecte pas
- Vérifier que le serveur est bien démarré
- Vérifier l'URL dans la configuration
- Vérifier les logs pour plus de détails

### Les changements ne sont pas synchronisés
- Vérifier que le Master a bien le rôle (voir les logs)
- Vérifier que les méthodes d'interception sont appelées
- Vérifier les logs du serveur et du client

## 🔐 Sécurité

**⚠️ Important** : Cette implémentation est pour réseau local uniquement.

Pour un déploiement en production :
- [ ] Ajouter authentification (JWT, Windows Auth)
- [ ] Utiliser HTTPS/WSS
- [ ] Ajouter validation des données
- [ ] Limiter les CORS
- [ ] Ajouter rate limiting

## 📚 Documentation Complète

Consultez les documents de conception dans `/docs` :
- `SYNC_DESIGN.md` - Architecture complète
- `SYNC_IMPLEMENTATION_GUIDE.md` - Guide d'implémentation détaillé
- `SYNC_DIAGRAMS.md` - Diagrammes visuels
- `SYNC_QUICKSTART.md` - Alternative file-based

## 🎯 Prochaines Étapes

1. **Tester l'infrastructure actuelle** avec 2 instances
2. **Implémenter l'interception** des modifications utilisateur
3. **Créer l'interface utilisateur** de configuration
4. **Tester en conditions réelles** avec plusieurs utilisateurs
5. **Documenter** pour les utilisateurs finaux

## 💡 Notes de Développement

### Comment Ajouter un Nouveau Type de Message

1. Ajouter le modèle dans `SyncModels.cs` (client et serveur)
2. Ajouter le cas dans `ApplyRemoteChange()` du client
3. Intercepter la modification et appeler `SendChangeAsync()`
4. Tester !

### Structure du Code

**Serveur** :
- `Models/` - Modèles de données
- `Services/SessionManager.cs` - Gestion des sessions
- `Hubs/PlocoSyncHub.cs` - Hub SignalR principal
- `Program.cs` - Configuration

**Client** :
- `Models/SyncModels.cs` - Modèles de synchronisation
- `Services/SyncService.cs` - Service de connexion
- `MainWindow.xaml.cs` - Intégration (#region Synchronization)

---

**Version** : 1.0.0  
**Date** : 12 février 2026  
**Statut** : En développement - Infrastructure fonctionnelle
