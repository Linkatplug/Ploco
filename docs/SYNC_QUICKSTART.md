# ⚡ Quick Start - Prototype de Synchronisation en 2-3 Semaines

**Date** : 12 février 2026  
**Objectif** : Créer un prototype fonctionnel rapidement pour valider le concept

---

## 🎯 Objectif

Si vous voulez tester l'idée de synchronisation **rapidement** sans développer la solution complète, ce guide propose une approche simplifiée qui peut être implémentée en **2-3 semaines**.

---

## 🚀 Approche Simplifiée : Solution Hybrid (File-Based)

### Principe

Au lieu d'un serveur WebSocket, utiliser un **dossier partagé** sur le réseau avec des fichiers JSON pour communiquer les changements.

```
\\server\ploco_sync\
├── ploco_master.db          # Base de données du Master
├── master.lock              # Fichier de verrouillage
├── changes\                 # Dossier des changements
│   ├── 001_loco_move.json
│   ├── 002_status_change.json
│   └── 003_tile_update.json
└── users\                   # Présence des utilisateurs
    ├── alice.json
    └── bob.json
```

### Avantages
- ✅ Pas de serveur à maintenir
- ✅ Fonctionne sur réseau Windows existant
- ✅ Simple à implémenter
- ✅ Bon pour tester le concept

### Limitations
- ⚠️ Latence 2-5 secondes (vs < 100ms pour WebSocket)
- ⚠️ Nécessite un partage réseau
- ⚠️ Moins scalable (< 10 utilisateurs)

---

## 📁 Structure des Fichiers

### 1. Fichier de Verrouillage Master

**master.lock**
```json
{
  "userId": "Alice",
  "userName": "Alice Dupont",
  "lockedAt": "2026-02-12T10:30:00Z",
  "heartbeat": "2026-02-12T10:35:00Z"
}
```

### 2. Fichier de Changement

**changes/001_loco_move.json**
```json
{
  "sequenceId": 1,
  "timestamp": "2026-02-12T10:30:15Z",
  "userId": "Alice",
  "type": "LocomotiveMove",
  "data": {
    "locomotiveId": 123,
    "fromTrackId": 5,
    "toTrackId": 8,
    "offsetX": 120.5
  }
}
```

### 3. Fichier de Présence

**users/alice.json**
```json
{
  "userId": "Alice",
  "userName": "Alice Dupont",
  "isMaster": true,
  "lastHeartbeat": "2026-02-12T10:35:00Z",
  "connectedAt": "2026-02-12T10:30:00Z"
}
```

---

## 💻 Implémentation Simplifiée

### Étape 1 : Service de Synchronisation Simplifié

**Ploco/Services/SimpleSyncService.cs**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ploco.Models;
using Ploco.Helpers;

namespace Ploco.Services
{
    public class SimpleSyncService : IDisposable
    {
        private readonly string _syncFolder;
        private readonly string _userId;
        private readonly string _userName;
        private bool _isMaster;
        private int _lastSequenceId;
        private Timer? _heartbeatTimer;
        private Timer? _changeWatchTimer;
        private FileSystemWatcher? _fileWatcher;

        public event EventHandler<SyncMessage>? ChangeReceived;
        public event EventHandler<bool>? MasterStatusChanged;

        public bool IsMaster => _isMaster;

        public SimpleSyncService(string syncFolder, string userId, string userName)
        {
            _syncFolder = syncFolder;
            _userId = userId;
            _userName = userName;
            
            EnsureFolders();
        }

        private void EnsureFolders()
        {
            Directory.CreateDirectory(_syncFolder);
            Directory.CreateDirectory(Path.Combine(_syncFolder, "changes"));
            Directory.CreateDirectory(Path.Combine(_syncFolder, "users"));
        }

        public async Task<bool> StartAsync()
        {
            try
            {
                // Vérifier si on peut devenir Master
                var masterLockPath = Path.Combine(_syncFolder, "master.lock");
                _isMaster = TryAcquireMasterLock(masterLockPath);

                // Créer le fichier de présence
                await CreatePresenceFileAsync();

                // Démarrer le heartbeat
                _heartbeatTimer = new Timer(
                    async _ => await HeartbeatAsync(),
                    null,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(10)
                );

                if (!_isMaster)
                {
                    // Démarrer la surveillance des changements
                    StartChangeWatcher();
                }

                Logger.Info($"Started as {(_isMaster ? "Master" : "Consultant")}", "SimpleSync");
                MasterStatusChanged?.Invoke(this, _isMaster);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to start: {ex.Message}", "SimpleSync");
                return false;
            }
        }

        private bool TryAcquireMasterLock(string lockPath)
        {
            try
            {
                // Vérifier si un Master existe déjà
                if (File.Exists(lockPath))
                {
                    var content = File.ReadAllText(lockPath);
                    var lockData = JsonSerializer.Deserialize<MasterLock>(content);
                    
                    // Vérifier si le heartbeat est récent (< 30 secondes)
                    if (lockData != null && 
                        (DateTime.UtcNow - lockData.Heartbeat).TotalSeconds < 30)
                    {
                        return false; // Master existant et actif
                    }
                }

                // Créer le fichier de verrouillage
                var masterLock = new MasterLock
                {
                    UserId = _userId,
                    UserName = _userName,
                    LockedAt = DateTime.UtcNow,
                    Heartbeat = DateTime.UtcNow
                };

                File.WriteAllText(lockPath, JsonSerializer.Serialize(masterLock));
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to acquire master lock: {ex.Message}", "SimpleSync");
                return false;
            }
        }

        private async Task CreatePresenceFileAsync()
        {
            var presencePath = Path.Combine(_syncFolder, "users", $"{_userId}.json");
            var presence = new UserPresence
            {
                UserId = _userId,
                UserName = _userName,
                IsMaster = _isMaster,
                LastHeartbeat = DateTime.UtcNow,
                ConnectedAt = DateTime.UtcNow
            };

            await File.WriteAllTextAsync(presencePath, JsonSerializer.Serialize(presence));
        }

        private async Task HeartbeatAsync()
        {
            try
            {
                if (_isMaster)
                {
                    // Mettre à jour le heartbeat dans master.lock
                    var lockPath = Path.Combine(_syncFolder, "master.lock");
                    if (File.Exists(lockPath))
                    {
                        var content = File.ReadAllText(lockPath);
                        var lockData = JsonSerializer.Deserialize<MasterLock>(content);
                        if (lockData != null && lockData.UserId == _userId)
                        {
                            lockData.Heartbeat = DateTime.UtcNow;
                            File.WriteAllText(lockPath, JsonSerializer.Serialize(lockData));
                        }
                    }
                }

                // Mettre à jour le fichier de présence
                await CreatePresenceFileAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"Heartbeat failed: {ex.Message}", "SimpleSync");
            }
        }

        public async Task<bool> SendChangeAsync(string messageType, object data)
        {
            if (!_isMaster)
            {
                Logger.Warning("Only Master can send changes", "SimpleSync");
                return false;
            }

            try
            {
                var changeId = Interlocked.Increment(ref _lastSequenceId);
                var message = new SyncMessage
                {
                    MessageId = changeId.ToString("D6"),
                    MessageType = messageType,
                    UserId = _userId,
                    Timestamp = DateTime.UtcNow,
                    Data = data
                };

                var changePath = Path.Combine(
                    _syncFolder, 
                    "changes", 
                    $"{changeId:D6}_{messageType}.json"
                );

                await File.WriteAllTextAsync(changePath, JsonSerializer.Serialize(message));
                Logger.Info($"Change sent: {messageType}", "SimpleSync");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to send change: {ex.Message}", "SimpleSync");
                return false;
            }
        }

        private void StartChangeWatcher()
        {
            // Surveiller le dossier des changements
            _fileWatcher = new FileSystemWatcher(Path.Combine(_syncFolder, "changes"))
            {
                Filter = "*.json",
                EnableRaisingEvents = true
            };

            _fileWatcher.Created += async (sender, e) =>
            {
                await Task.Delay(100); // Attendre que le fichier soit complètement écrit
                ProcessChangeFile(e.FullPath);
            };

            // Également vérifier périodiquement pour les changements manqués
            _changeWatchTimer = new Timer(
                _ => CheckForNewChanges(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2)
            );
        }

        private void CheckForNewChanges()
        {
            try
            {
                var changesPath = Path.Combine(_syncFolder, "changes");
                var files = Directory.GetFiles(changesPath, "*.json")
                    .OrderBy(f => f)
                    .ToList();

                foreach (var file in files)
                {
                    ProcessChangeFile(file);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking changes: {ex.Message}", "SimpleSync");
            }
        }

        private void ProcessChangeFile(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var message = JsonSerializer.Deserialize<SyncMessage>(content);

                if (message == null || string.IsNullOrEmpty(message.MessageId))
                    return;

                // Extraire le numéro de séquence
                if (!int.TryParse(message.MessageId.Split('_')[0], out int sequenceId))
                    return;

                // Ne traiter que les nouveaux messages
                if (sequenceId <= _lastSequenceId)
                    return;

                _lastSequenceId = sequenceId;

                Logger.Info($"Processing change: {message.MessageType}", "SimpleSync");
                ChangeReceived?.Invoke(this, message);

                // Nettoyer les vieux fichiers (garder seulement les 100 derniers)
                CleanOldChanges();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing change file: {ex.Message}", "SimpleSync");
            }
        }

        private void CleanOldChanges()
        {
            try
            {
                var changesPath = Path.Combine(_syncFolder, "changes");
                var files = Directory.GetFiles(changesPath, "*.json")
                    .OrderBy(f => f)
                    .ToList();

                if (files.Count > 100)
                {
                    var toDelete = files.Take(files.Count - 100);
                    foreach (var file in toDelete)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch { }
        }

        public void Dispose()
        {
            _heartbeatTimer?.Dispose();
            _changeWatchTimer?.Dispose();
            _fileWatcher?.Dispose();

            // Supprimer le fichier de présence
            try
            {
                var presencePath = Path.Combine(_syncFolder, "users", $"{_userId}.json");
                if (File.Exists(presencePath))
                {
                    File.Delete(presencePath);
                }

                // Si on est Master, supprimer le lock
                if (_isMaster)
                {
                    var lockPath = Path.Combine(_syncFolder, "master.lock");
                    if (File.Exists(lockPath))
                    {
                        File.Delete(lockPath);
                    }
                }
            }
            catch { }
        }
    }

    public class MasterLock
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime LockedAt { get; set; }
        public DateTime Heartbeat { get; set; }
    }

    public class UserPresence
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsMaster { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}
```

### Étape 2 : Configuration dans MainWindow

**Modification de MainWindow.xaml.cs**

```csharp
private SimpleSyncService? _simpleSyncService;

private void InitializeSimpleSyncService()
{
    // Charger le chemin du dossier partagé depuis la configuration
    var syncFolder = @"\\server\ploco_sync"; // À configurer
    
    _simpleSyncService = new SimpleSyncService(
        syncFolder,
        Environment.UserName,
        Environment.UserName
    );
    
    _simpleSyncService.ChangeReceived += SyncService_ChangeReceived;
    _simpleSyncService.MasterStatusChanged += SyncService_MasterStatusChanged;
    
    _ = _simpleSyncService.StartAsync();
}

// Le reste du code reste identique à la solution WebSocket
```

---

## 📋 Checklist d'Implémentation Rapide

### Semaine 1 : Base
- [ ] Créer `SimpleSyncService.cs`
- [ ] Tester la création de fichiers dans le dossier partagé
- [ ] Implémenter le système de verrouillage Master
- [ ] Tester avec 2 instances sur le même PC

### Semaine 2 : Intégration
- [ ] Intégrer dans `MainWindow.xaml.cs`
- [ ] Intercepter les déplacements de locomotives
- [ ] Appliquer les changements reçus
- [ ] Tester avec 2 PC sur le réseau

### Semaine 3 : Finition
- [ ] Ajouter l'UI de configuration
- [ ] Gérer les erreurs
- [ ] Nettoyer les vieux fichiers
- [ ] Tests finaux

---

## 🧪 Test Rapide

### 1. Créer le Dossier Partagé

Sur un PC Windows :
```
1. Créer C:\PlocoSync
2. Clic droit > Propriétés > Partage
3. Partager avec "Tout le monde" (lecture/écriture)
4. Noter le chemin : \\NOM-PC\PlocoSync
```

### 2. Lancer 2 Instances

**PC 1 (Master)**
```
- Ouvrir PlocoManager
- Configurer : \\NOM-PC\PlocoSync
- Nom : Alice
- → Devient Master automatiquement
```

**PC 2 (Consultant)**
```
- Ouvrir PlocoManager
- Configurer : \\NOM-PC\PlocoSync
- Nom : Bob
- → Devient Consultant automatiquement
```

### 3. Tester

- Alice déplace une locomotive
- Attendre 2-3 secondes
- Bob voit le changement apparaître

---

## 🎯 Avantages de cette Approche

### Pour le Prototypage
- ✅ Implémentation rapide (2-3 semaines)
- ✅ Pas de serveur à déployer
- ✅ Utilise l'infrastructure réseau existante
- ✅ Facile à tester

### Pour la Validation
- ✅ Permet de tester le concept avec les utilisateurs
- ✅ Valide le workflow Master/Consultant
- ✅ Identifie les besoins réels

### Pour la Migration
- ✅ Code réutilisable pour la solution WebSocket
- ✅ Même interface `SyncService`
- ✅ Migration transparente pour l'utilisateur

---

## 🔄 Migration vers WebSocket Plus Tard

Une fois le prototype validé, migrer vers la solution WebSocket :

```csharp
// Remplacer SimpleSyncService par SyncService
// private SimpleSyncService? _syncService;
private SyncService? _syncService;

// Le reste du code ne change pas !
```

---

## ⚠️ Limitations à Connaître

### Performance
- Latence : 2-5 secondes (vs < 100ms pour WebSocket)
- Dépend de la vitesse du réseau
- Peut être plus lent avec beaucoup de changements

### Scalabilité
- Maximum 5-10 utilisateurs
- Performance dégrade avec beaucoup de fichiers
- Nécessite nettoyage régulier

### Fiabilité
- Dépend de la stabilité du partage réseau
- Problèmes si partage déconnecté
- Pas de garantie d'ordre strict

---

## 💡 Améliorations Possibles

### Court Terme
- [ ] Compression des fichiers JSON
- [ ] Batching de plusieurs changements
- [ ] Confirmation de réception

### Moyen Terme
- [ ] Historique persistant
- [ ] Résolution de conflits améliorée
- [ ] Statistiques de synchronisation

---

## 📊 Comparaison : Prototype vs Production

| Aspect | Prototype (File) | Production (WebSocket) |
|--------|------------------|------------------------|
| Temps implémentation | 2-3 semaines | 6-8 semaines |
| Latence | 2-5 secondes | < 100ms |
| Utilisateurs | 5-10 | 50+ |
| Infrastructure | Partage réseau | Serveur dédié |
| Maintenance | Faible | Moyenne |
| Coût | Gratuit | Gratuit (serveur local) |

---

## 🎯 Conclusion

Cette approche simplifiée est **idéale pour** :
- ✅ Valider rapidement le concept
- ✅ Tester avec les utilisateurs réels
- ✅ Identifier les besoins avant investissement complet
- ✅ Démarrer la synchronisation sans infrastructure lourde

**Ensuite, migrez vers la solution WebSocket** pour :
- Performance temps réel
- Meilleure scalabilité
- Fonctionnalités avancées

---

## 📞 Prochaines Étapes

1. **Décider** : Prototype d'abord ou solution complète directement ?
2. **Configurer** : Créer le dossier partagé
3. **Implémenter** : Suivre le code ci-dessus
4. **Tester** : Valider avec 2-3 utilisateurs
5. **Évaluer** : Prototype suffisant ou migrer vers WebSocket ?

---

*Guide Quick Start créé pour PlocoManager - Synchronisation Multi-Utilisateurs*
