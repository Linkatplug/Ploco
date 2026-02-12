# 🖥️ Serveur de Synchronisation PlocoManager

## Description

Le serveur de synchronisation permet à plusieurs utilisateurs de travailler simultanément sur PlocoManager avec un système Master/Consultant en temps réel via WebSocket.

## 🚀 Compilation du Serveur

### Windows

Double-cliquez sur `build_server.bat` ou exécutez dans un terminal :
```cmd
build_server.bat
```

### Linux/Mac

```bash
chmod +x build_server.sh
./build_server.sh
```

Le serveur sera compilé dans le dossier `publish/PlocoSync.Server/`

## ▶️ Lancement du Serveur

### Depuis la Compilation

**Windows** :
```cmd
cd publish\PlocoSync.Server
PlocoSync.Server.exe
```

**Linux/Mac** :
```bash
cd publish/PlocoSync.Server
./PlocoSync.Server
```

### Depuis Visual Studio

1. Ouvrir `Ploco.sln` dans Visual Studio
2. Définir `PlocoSync.Server` comme projet de démarrage
3. Appuyer sur F5

### Depuis la Ligne de Commande (Développement)

```bash
cd PlocoSync.Server
dotnet run
```

## 🌐 Accès au Serveur

Une fois lancé, le serveur est accessible sur :

- **URL** : http://localhost:5000
- **Hub SignalR** : http://localhost:5000/syncHub
- **Health Check** : http://localhost:5000/health
- **Sessions** : http://localhost:5000/sessions

## 📋 Vérification du Fonctionnement

Ouvrez http://localhost:5000 dans un navigateur. Vous devriez voir :

```json
{
  "service": "PlocoSync Server",
  "status": "Running",
  "version": "1.0.0",
  "timestamp": "2026-02-12T..."
}
```

## 👥 Utilisation avec les Clients

1. **Lancer le serveur** (ce programme)
2. **Lancer PlocoManager** sur le premier PC
   - Choisir "Mode Master" dans le dialog de démarrage
   - Entrer l'URL du serveur : `http://ADRESSE-SERVEUR:5000`
3. **Lancer PlocoManager** sur le second PC
   - Choisir "Mode Consultation" dans le dialog de démarrage
   - Entrer la même URL du serveur

## 🔧 Configuration

### Changer le Port

Modifier dans `PlocoSync.Server/Program.cs` :

```csharp
app.Run("http://*:5000");  // Changer 5000 par le port désiré
```

### Réseau Local vs Internet

**Réseau Local** : 
- Utiliser l'adresse IP locale du serveur : `http://192.168.1.100:5000`
- Exemple : Si le serveur est sur 192.168.1.50, les clients se connectent à `http://192.168.1.50:5000`

**Internet** :
- ⚠️ Nécessite configuration avancée (HTTPS, authentification, firewall)
- Non recommandé sans sécurité additionnelle

## 📊 Monitoring

### Voir les Sessions Actives

```bash
curl http://localhost:5000/sessions
```

Retourne :
```json
{
  "totalSessions": 2,
  "masterId": "Alice",
  "sessions": [
    {
      "userId": "Alice",
      "userName": "Alice",
      "isMaster": true,
      "connectedAt": "2026-02-12T10:30:00",
      "lastHeartbeat": "2026-02-12T10:35:00"
    },
    {
      "userId": "Bob",
      "userName": "Bob",
      "isMaster": false,
      "connectedAt": "2026-02-12T10:31:00",
      "lastHeartbeat": "2026-02-12T10:35:00"
    }
  ]
}
```

### Logs du Serveur

Les logs apparaissent dans la console où le serveur est lancé.

Exemple :
```
info: PlocoSync.Server.Services.SessionManager[0]
      User Alice (Alice) connected as Master
info: PlocoSync.Server.Services.SessionManager[0]
      User Bob (Bob) connected as Consultant
info: PlocoSync.Server.Hubs.PlocoSyncHub[0]
      Change broadcasted: LocomotiveMove from Alice
```

## 🛑 Arrêter le Serveur

- **Console** : Appuyer sur `Ctrl+C`
- **Windows Service** : `sc stop PlocoSyncService`

## 🔒 Sécurité

**⚠️ Important** : Cette version est prévue pour un réseau local de confiance.

Pour un déploiement en production :
- Activer HTTPS
- Ajouter authentification
- Restreindre CORS
- Configurer un firewall

## 🐛 Dépannage

### Le serveur ne démarre pas

**Erreur "Port already in use"** :
- Un autre programme utilise le port 5000
- Solution : Changer le port dans `Program.cs`

**Erreur "Permission denied"** :
- Windows : Lancer en tant qu'administrateur
- Linux : Utiliser `sudo` ou changer le port (> 1024)

### Les clients ne se connectent pas

1. Vérifier que le serveur est bien démarré
2. Vérifier l'URL (IP correcte, port correct)
3. Vérifier le firewall Windows/Linux
4. Tester avec `curl http://SERVEUR:5000`

### Performances lentes

- Vérifier la qualité du réseau
- Limiter le nombre de clients simultanés (< 20 recommandé)
- Utiliser un PC dédié pour le serveur

## 📁 Structure du Projet

```
PlocoSync.Server/
├── Program.cs              # Configuration et point d'entrée
├── Hubs/
│   └── PlocoSyncHub.cs    # Hub SignalR
├── Services/
│   └── SessionManager.cs  # Gestion des sessions
└── Models/
    └── SyncMessage.cs     # Modèles de messages
```

## 📚 Documentation Complète

Consultez les autres documents :
- `/docs/SYNC_DESIGN.md` - Architecture complète
- `/docs/SYNC_IMPLEMENTATION_GUIDE.md` - Guide d'implémentation
- `/SYNC_README.md` - Guide utilisateur
- `/IMPLEMENTATION_STATUS.md` - État de l'implémentation

## 🆘 Support

Pour toute question ou problème :
1. Consulter les logs du serveur
2. Consulter les logs du client (`%AppData%\Ploco\Logs\`)
3. Vérifier l'endpoint `/sessions` pour voir les connexions actives

---

**Version** : 1.0.0  
**Date** : 12 février 2026  
**Auteur** : PlocoManager Team
