# 🛠️ Guide d'Implémentation - Synchronisation Multi-Utilisateurs

**Date** : 12 février 2026  
**Version** : 1.0  

---

## 📋 Vue d'Ensemble

Ce document fournit des **exemples de code concrets** pour implémenter la synchronisation multi-utilisateurs dans PlocoManager en utilisant l'approche **WebSocket avec SignalR**.

---

## 📁 Structure des Projets

```
PlocoManager/
├── Ploco/                          # Application WPF existante
│   ├── Services/
│   │   └── SyncService.cs          # 🆕 Service de synchronisation
│   ├── Models/
│   │   └── SyncModels.cs           # 🆕 Modèles de messages
│   └── Dialogs/
│       └── SyncConfigDialog.xaml   # 🆕 Configuration sync
│
└── PlocoSync.Server/               # 🆕 Nouveau projet serveur
    ├── Hubs/
    │   └── PlocoSyncHub.cs         # Hub SignalR principal
    ├── Services/
    │   ├── SessionManager.cs       # Gestion des sessions
    │   └── RoleManager.cs          # Gestion Master/Consultant
    ├── Data/
    │   └── SyncRepository.cs       # Accès données centralisé
    └── Program.cs                  # Point d'entrée serveur
```

---

## 🔧 PARTIE 1 : SERVEUR DE SYNCHRONISATION

### Étape 1.1 : Créer le Projet Serveur

```bash
# Créer le projet ASP.NET Core
cd PlocoManager
dotnet new web -n PlocoSync.Server
cd PlocoSync.Server

# Ajouter les packages nécessaires
dotnet add package Microsoft.AspNetCore.SignalR
dotnet add package Microsoft.Data.Sqlite
dotnet add package Newtonsoft.Json
```

### Étape 1.2 : Modèles de Messages

**PlocoSync.Server/Models/SyncMessage.cs**
```csharp
namespace PlocoSync.Server.Models
{
    public class SyncMessage
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string MessageType { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public object? Data { get; set; }
    }

    public class LocomotiveMoveData
    {
        public int LocomotiveId { get; set; }
        public int? FromTrackId { get; set; }
        public int ToTrackId { get; set; }
        public double? OffsetX { get; set; }
    }

    public class LocomotiveStatusChangeData
    {
        public int LocomotiveId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? TractionPercent { get; set; }
        public string? HsReason { get; set; }
        public string? DefautInfo { get; set; }
        public string? TractionInfo { get; set; }
    }

    public class TileUpdateData
    {
        public int TileId { get; set; }
        public double? X { get; set; }
        public double? Y { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
    }

    public class MasterTransferData
    {
        public string NewMasterUserId { get; set; } = string.Empty;
    }
}
```

### Étape 1.3 : Gestion des Sessions

**PlocoSync.Server/Services/SessionManager.cs**
```csharp
using System.Collections.Concurrent;

namespace PlocoSync.Server.Services
{
    public class UserSession
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsMaster { get; set; }
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    }

    public class SessionManager
    {
        private readonly ConcurrentDictionary<string, UserSession> _sessions = new();
        private string? _currentMasterId;

        public bool AddSession(string connectionId, string userId, string userName)
        {
            var session = new UserSession
            {
                ConnectionId = connectionId,
                UserId = userId,
                UserName = userName,
                IsMaster = _currentMasterId == null // Premier utilisateur devient Master
            };

            if (session.IsMaster)
            {
                _currentMasterId = userId;
            }

            return _sessions.TryAdd(connectionId, session);
        }

        public bool RemoveSession(string connectionId)
        {
            if (_sessions.TryRemove(connectionId, out var session))
            {
                // Si le Master se déconnecte, transférer à quelqu'un d'autre
                if (session.IsMaster && _sessions.Count > 0)
                {
                    var newMaster = _sessions.Values.FirstOrDefault();
                    if (newMaster != null)
                    {
                        newMaster.IsMaster = true;
                        _currentMasterId = newMaster.UserId;
                    }
                    else
                    {
                        _currentMasterId = null;
                    }
                }
                else if (session.IsMaster)
                {
                    _currentMasterId = null;
                }
                return true;
            }
            return false;
        }

        public UserSession? GetSession(string connectionId)
        {
            _sessions.TryGetValue(connectionId, out var session);
            return session;
        }

        public bool IsMaster(string connectionId)
        {
            var session = GetSession(connectionId);
            return session?.IsMaster ?? false;
        }

        public bool TransferMaster(string newMasterUserId)
        {
            // Retirer le Master actuel
            var currentMaster = _sessions.Values.FirstOrDefault(s => s.IsMaster);
            if (currentMaster != null)
            {
                currentMaster.IsMaster = false;
            }

            // Assigner le nouveau Master
            var newMaster = _sessions.Values.FirstOrDefault(s => s.UserId == newMasterUserId);
            if (newMaster != null)
            {
                newMaster.IsMaster = true;
                _currentMasterId = newMasterUserId;
                return true;
            }

            return false;
        }

        public List<UserSession> GetAllSessions()
        {
            return _sessions.Values.ToList();
        }

        public string? GetCurrentMasterId()
        {
            return _currentMasterId;
        }
    }
}
```

### Étape 1.4 : Hub SignalR Principal

**PlocoSync.Server/Hubs/PlocoSyncHub.cs**
```csharp
using Microsoft.AspNetCore.SignalR;
using PlocoSync.Server.Models;
using PlocoSync.Server.Services;

namespace PlocoSync.Server.Hubs
{
    public class PlocoSyncHub : Hub
    {
        private readonly SessionManager _sessionManager;
        private readonly ILogger<PlocoSyncHub> _logger;

        public PlocoSyncHub(SessionManager sessionManager, ILogger<PlocoSyncHub> logger)
        {
            _sessionManager = sessionManager;
            _logger = logger;
        }

        public async Task<object> Connect(string userId, string userName)
        {
            var connectionId = Context.ConnectionId;
            _sessionManager.AddSession(connectionId, userId, userName);
            
            var session = _sessionManager.GetSession(connectionId);
            _logger.LogInformation($"User {userName} connected as {(session?.IsMaster ?? false ? "Master" : "Consultant")}");

            // Notifier tous les autres clients
            await Clients.Others.SendAsync("UserConnected", new
            {
                UserId = userId,
                UserName = userName,
                IsMaster = session?.IsMaster ?? false
            });

            // Retourner les informations de session
            return new
            {
                Success = true,
                IsMaster = session?.IsMaster ?? false,
                MasterId = _sessionManager.GetCurrentMasterId(),
                ConnectedUsers = _sessionManager.GetAllSessions().Select(s => new
                {
                    s.UserId,
                    s.UserName,
                    s.IsMaster
                })
            };
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var session = _sessionManager.GetSession(Context.ConnectionId);
            if (session != null)
            {
                var wasMaster = session.IsMaster;
                _sessionManager.RemoveSession(Context.ConnectionId);
                
                _logger.LogInformation($"User {session.UserName} disconnected");

                // Notifier les autres
                await Clients.Others.SendAsync("UserDisconnected", new
                {
                    session.UserId,
                    session.UserName,
                    WasMaster = wasMaster,
                    NewMasterId = wasMaster ? _sessionManager.GetCurrentMasterId() : null
                });
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task<bool> SendChange(SyncMessage message)
        {
            // Vérifier que l'émetteur est le Master
            if (!_sessionManager.IsMaster(Context.ConnectionId))
            {
                _logger.LogWarning($"Non-master user tried to send change: {message.UserId}");
                return false;
            }

            // Broadcaster le changement à tous sauf l'émetteur
            await Clients.Others.SendAsync("ReceiveChange", message);
            
            _logger.LogInformation($"Change broadcasted: {message.MessageType} from {message.UserId}");
            return true;
        }

        public async Task<bool> RequestMaster()
        {
            var session = _sessionManager.GetSession(Context.ConnectionId);
            if (session == null)
            {
                return false;
            }

            // Envoyer une demande au Master actuel
            var currentMasterId = _sessionManager.GetCurrentMasterId();
            if (currentMasterId != null)
            {
                var masterSession = _sessionManager.GetAllSessions()
                    .FirstOrDefault(s => s.UserId == currentMasterId);
                
                if (masterSession != null)
                {
                    await Clients.Client(masterSession.ConnectionId).SendAsync("MasterRequested", new
                    {
                        RequesterId = session.UserId,
                        RequesterName = session.UserName
                    });
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> TransferMaster(string newMasterUserId)
        {
            // Vérifier que l'émetteur est le Master actuel
            if (!_sessionManager.IsMaster(Context.ConnectionId))
            {
                return false;
            }

            if (_sessionManager.TransferMaster(newMasterUserId))
            {
                _logger.LogInformation($"Master transferred to {newMasterUserId}");
                
                // Notifier tous les clients
                await Clients.All.SendAsync("MasterTransferred", new
                {
                    NewMasterId = newMasterUserId
                });
                
                return true;
            }

            return false;
        }

        public List<object> GetConnectedUsers()
        {
            return _sessionManager.GetAllSessions().Select(s => new
            {
                s.UserId,
                s.UserName,
                s.IsMaster,
                ConnectedAt = s.ConnectedAt.ToString("o")
            }).Cast<object>().ToList();
        }
    }
}
```

### Étape 1.5 : Configuration du Serveur

**PlocoSync.Server/Program.cs**
```csharp
using PlocoSync.Server.Hubs;
using PlocoSync.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Ajouter les services
builder.Services.AddSignalR();
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configurer le pipeline
app.UseCors();
app.MapHub<PlocoSyncHub>("/syncHub");

app.MapGet("/", () => "PlocoSync Server is running");
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow });

app.Run("http://*:5000");
```

---

## 🖥️ PARTIE 2 : CLIENT PLOCOMANAGER

### Étape 2.1 : Ajouter les Packages

Modifier **Ploco/Ploco.csproj**
```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.6" />
  <!-- 🆕 Ajouter SignalR Client -->
  <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
</ItemGroup>
```

### Étape 2.2 : Modèles de Synchronisation

**Ploco/Models/SyncModels.cs** (nouveau fichier)
```csharp
namespace Ploco.Models
{
    public class SyncMessage
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string MessageType { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public object? Data { get; set; }
    }

    public class SyncConfiguration
    {
        public bool Enabled { get; set; }
        public string ServerUrl { get; set; } = "http://localhost:5000";
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool AutoReconnect { get; set; } = true;
        public int ReconnectDelaySeconds { get; set; } = 5;
    }
}
```

### Étape 2.3 : Service de Synchronisation

**Ploco/Services/SyncService.cs** (nouveau fichier)
```csharp
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Ploco.Models;
using Ploco.Helpers;
using Newtonsoft.Json;

namespace Ploco.Services
{
    public class SyncService : IDisposable
    {
        private HubConnection? _connection;
        private readonly SyncConfiguration _config;
        private bool _isMaster;
        private bool _isConnected;

        public event EventHandler<SyncMessage>? ChangeReceived;
        public event EventHandler<bool>? MasterStatusChanged;
        public event EventHandler<bool>? ConnectionStatusChanged;

        public bool IsConnected => _isConnected;
        public bool IsMaster => _isMaster;

        public SyncService(SyncConfiguration config)
        {
            _config = config;
        }

        public async Task<bool> ConnectAsync()
        {
            if (!_config.Enabled || string.IsNullOrWhiteSpace(_config.ServerUrl))
            {
                Logger.Warning("Sync not enabled or server URL not configured", "Sync");
                return false;
            }

            try
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl($"{_config.ServerUrl}/syncHub")
                    .WithAutomaticReconnect()
                    .Build();

                // Configurer les handlers
                _connection.On<SyncMessage>("ReceiveChange", HandleChangeReceived);
                _connection.On<object>("MasterTransferred", HandleMasterTransferred);
                _connection.On<object>("UserConnected", HandleUserConnected);
                _connection.On<object>("UserDisconnected", HandleUserDisconnected);
                _connection.On<object>("MasterRequested", HandleMasterRequested);

                _connection.Closed += async (error) =>
                {
                    _isConnected = false;
                    ConnectionStatusChanged?.Invoke(this, false);
                    Logger.Warning("Connection closed", "Sync");

                    if (_config.AutoReconnect)
                    {
                        await Task.Delay(_config.ReconnectDelaySeconds * 1000);
                        await ConnectAsync();
                    }
                };

                await _connection.StartAsync();
                
                // S'enregistrer auprès du serveur
                var result = await _connection.InvokeAsync<dynamic>(
                    "Connect", 
                    _config.UserId, 
                    _config.UserName
                );

                _isMaster = result.IsMaster;
                _isConnected = true;
                
                ConnectionStatusChanged?.Invoke(this, true);
                MasterStatusChanged?.Invoke(this, _isMaster);
                
                Logger.Info($"Connected as {(_isMaster ? "Master" : "Consultant")}", "Sync");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to connect: {ex.Message}", "Sync");
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);
            }
        }

        public async Task<bool> SendChangeAsync(string messageType, object data)
        {
            if (!_isConnected || !_isMaster || _connection == null)
            {
                Logger.Warning("Cannot send change: not connected or not master", "Sync");
                return false;
            }

            try
            {
                var message = new SyncMessage
                {
                    MessageType = messageType,
                    UserId = _config.UserId,
                    Data = data
                };

                var success = await _connection.InvokeAsync<bool>("SendChange", message);
                if (success)
                {
                    Logger.Info($"Change sent: {messageType}", "Sync");
                }
                return success;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to send change: {ex.Message}", "Sync");
                return false;
            }
        }

        public async Task<bool> RequestMasterAsync()
        {
            if (!_isConnected || _connection == null)
            {
                return false;
            }

            try
            {
                return await _connection.InvokeAsync<bool>("RequestMaster");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to request master: {ex.Message}", "Sync");
                return false;
            }
        }

        public async Task<bool> TransferMasterAsync(string newMasterUserId)
        {
            if (!_isConnected || !_isMaster || _connection == null)
            {
                return false;
            }

            try
            {
                var success = await _connection.InvokeAsync<bool>("TransferMaster", newMasterUserId);
                if (success)
                {
                    _isMaster = false;
                    MasterStatusChanged?.Invoke(this, false);
                }
                return success;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to transfer master: {ex.Message}", "Sync");
                return false;
            }
        }

        private void HandleChangeReceived(SyncMessage message)
        {
            Logger.Info($"Change received: {message.MessageType}", "Sync");
            ChangeReceived?.Invoke(this, message);
        }

        private void HandleMasterTransferred(dynamic data)
        {
            string newMasterId = data.NewMasterId;
            _isMaster = (newMasterId == _config.UserId);
            MasterStatusChanged?.Invoke(this, _isMaster);
            Logger.Info($"Master transferred to: {newMasterId}", "Sync");
        }

        private void HandleUserConnected(dynamic data)
        {
            Logger.Info($"User connected: {data.UserName}", "Sync");
        }

        private void HandleUserDisconnected(dynamic data)
        {
            Logger.Info($"User disconnected: {data.UserName}", "Sync");
        }

        private void HandleMasterRequested(dynamic data)
        {
            // Afficher une notification pour demander confirmation
            Logger.Info($"Master requested by: {data.RequesterName}", "Sync");
        }

        public void Dispose()
        {
            _connection?.DisposeAsync().AsTask().Wait();
        }
    }
}
```

### Étape 2.4 : Intégration dans MainWindow

Modifier **Ploco/MainWindow.xaml.cs**

```csharp
// Ajouter en haut de la classe MainWindow
using Ploco.Services;

public partial class MainWindow : Window
{
    // ... champs existants ...
    
    // 🆕 Ajouter le service de synchronisation
    private SyncService? _syncService;
    private bool _isApplyingRemoteChange = false; // Flag pour éviter les boucles

    public MainWindow()
    {
        InitializeComponent();
        // ... code existant ...
        
        // 🆕 Initialiser la synchronisation si activée
        InitializeSyncService();
    }

    private void InitializeSyncService()
    {
        // Charger la configuration (depuis un fichier JSON ou settings)
        var config = LoadSyncConfiguration();
        
        if (config.Enabled)
        {
            _syncService = new SyncService(config);
            _syncService.ChangeReceived += SyncService_ChangeReceived;
            _syncService.MasterStatusChanged += SyncService_MasterStatusChanged;
            _syncService.ConnectionStatusChanged += SyncService_ConnectionStatusChanged;
            
            // Connecter de manière asynchrone
            _ = _syncService.ConnectAsync();
        }
    }

    private SyncConfiguration LoadSyncConfiguration()
    {
        // À implémenter : charger depuis un fichier de configuration
        // Pour l'instant, retourner une config par défaut
        return new SyncConfiguration
        {
            Enabled = false, // Désactivé par défaut
            ServerUrl = "http://localhost:5000",
            UserId = Environment.UserName,
            UserName = Environment.UserName,
            AutoReconnect = true
        };
    }

    private void SyncService_ChangeReceived(object? sender, SyncMessage message)
    {
        // Appliquer le changement sur le thread UI
        Dispatcher.Invoke(() =>
        {
            _isApplyingRemoteChange = true;
            try
            {
                ApplyRemoteChange(message);
            }
            finally
            {
                _isApplyingRemoteChange = false;
            }
        });
    }

    private void ApplyRemoteChange(SyncMessage message)
    {
        Logger.Info($"Applying remote change: {message.MessageType}", "Sync");

        switch (message.MessageType)
        {
            case "LocomotiveMove":
                ApplyLocomotiveMove(message.Data);
                break;
            
            case "LocomotiveStatusChange":
                ApplyLocomotiveStatusChange(message.Data);
                break;
            
            case "TileUpdate":
                ApplyTileUpdate(message.Data);
                break;
            
            default:
                Logger.Warning($"Unknown message type: {message.MessageType}", "Sync");
                break;
        }
    }

    private void ApplyLocomotiveMove(object data)
    {
        // Désérialiser les données
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
        var moveData = Newtonsoft.Json.JsonConvert.DeserializeObject<LocomotiveMoveData>(json);
        
        if (moveData == null) return;

        // Trouver la locomotive
        var loco = _locomotives.FirstOrDefault(l => l.Id == moveData.LocomotiveId);
        if (loco == null) return;

        // Retirer de l'ancienne voie
        if (moveData.FromTrackId.HasValue)
        {
            var oldTrack = FindTrackById(moveData.FromTrackId.Value);
            oldTrack?.Locomotives.Remove(loco);
        }

        // Ajouter à la nouvelle voie
        var newTrack = FindTrackById(moveData.ToTrackId);
        if (newTrack != null)
        {
            loco.AssignedTrackId = moveData.ToTrackId;
            loco.AssignedTrackOffsetX = moveData.OffsetX;
            newTrack.Locomotives.Add(loco);
        }

        Logger.Info($"Applied locomotive move: Loco {loco.Number} to Track {moveData.ToTrackId}", "Sync");
    }

    private void ApplyLocomotiveStatusChange(object data)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
        var statusData = Newtonsoft.Json.JsonConvert.DeserializeObject<LocomotiveStatusChangeData>(json);
        
        if (statusData == null) return;

        var loco = _locomotives.FirstOrDefault(l => l.Id == statusData.LocomotiveId);
        if (loco == null) return;

        loco.Status = Enum.Parse<LocomotiveStatus>(statusData.Status);
        loco.TractionPercent = statusData.TractionPercent;
        loco.HsReason = statusData.HsReason;
        loco.DefautInfo = statusData.DefautInfo;
        loco.TractionInfo = statusData.TractionInfo;

        Logger.Info($"Applied status change: Loco {loco.Number} = {statusData.Status}", "Sync");
    }

    private void ApplyTileUpdate(object data)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
        var tileData = Newtonsoft.Json.JsonConvert.DeserializeObject<TileUpdateData>(json);
        
        if (tileData == null) return;

        var tile = _tiles.FirstOrDefault(t => t.Id == tileData.TileId);
        if (tile == null) return;

        if (tileData.X.HasValue) tile.X = tileData.X.Value;
        if (tileData.Y.HasValue) tile.Y = tileData.Y.Value;
        if (tileData.Width.HasValue) tile.Width = tileData.Width.Value;
        if (tileData.Height.HasValue) tile.Height = tileData.Height.Value;

        Logger.Info($"Applied tile update: Tile {tile.Name}", "Sync");
    }

    private TrackModel? FindTrackById(int trackId)
    {
        foreach (var tile in _tiles)
        {
            var track = tile.Tracks.FirstOrDefault(t => t.Id == trackId);
            if (track != null) return track;
        }
        return null;
    }

    private void SyncService_MasterStatusChanged(object? sender, bool isMaster)
    {
        Dispatcher.Invoke(() =>
        {
            // Mettre à jour l'UI pour indiquer le statut Master/Consultant
            UpdateMasterStatus(isMaster);
        });
    }

    private void SyncService_ConnectionStatusChanged(object? sender, bool isConnected)
    {
        Dispatcher.Invoke(() =>
        {
            // Mettre à jour l'indicateur de connexion dans l'UI
            UpdateConnectionStatus(isConnected);
        });
    }

    private void UpdateMasterStatus(bool isMaster)
    {
        // À implémenter : Afficher un badge "Master" ou "Consultant" dans l'UI
        // Activer/désactiver les contrôles selon le statut
        Logger.Info($"Status changed to: {(isMaster ? "Master" : "Consultant")}", "Sync");
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        // À implémenter : Afficher un indicateur de connexion dans l'UI
        Logger.Info($"Connection status: {(isConnected ? "Connected" : "Disconnected")}", "Sync");
    }

    // 🆕 Modifier les méthodes existantes pour envoyer les changements
    // Exemple : Quand une locomotive est déplacée
    private async void OnLocomotiveMoved(LocomotiveModel loco, int? fromTrackId, int toTrackId, double? offsetX)
    {
        // Ne pas envoyer si on applique un changement distant
        if (_isApplyingRemoteChange) return;

        // Envoyer le changement au serveur
        if (_syncService != null && _syncService.IsMaster)
        {
            await _syncService.SendChangeAsync("LocomotiveMove", new
            {
                LocomotiveId = loco.Id,
                FromTrackId = fromTrackId,
                ToTrackId = toTrackId,
                OffsetX = offsetX
            });
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // Déconnecter le service de synchronisation
        _syncService?.DisconnectAsync().Wait();
        _syncService?.Dispose();
        
        base.OnClosed(e);
    }
}
```

---

## 🎨 PARTIE 3 : INTERFACE UTILISATEUR

### Étape 3.1 : Dialog de Configuration

**Ploco/Dialogs/SyncConfigDialog.xaml**
```xml
<Window x:Class="Ploco.Dialogs.SyncConfigDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Configuration de la Synchronisation"
        Width="500" Height="350"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" FontSize="16" FontWeight="Bold" Margin="0,0,0,20">
            Synchronisation Multi-Utilisateurs
        </TextBlock>

        <CheckBox Grid.Row="1" x:Name="EnabledCheckBox" Content="Activer la synchronisation" Margin="0,0,0,15"/>

        <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,0,0,10">
            <TextBlock Text="URL du serveur :" Width="120" VerticalAlignment="Center"/>
            <TextBox x:Name="ServerUrlTextBox" Width="300" Text="http://localhost:5000"/>
        </StackPanel>

        <StackPanel Grid.Row="3" Orientation="Horizontal" Margin="0,0,0,10">
            <TextBlock Text="Nom d'utilisateur :" Width="120" VerticalAlignment="Center"/>
            <TextBox x:Name="UserNameTextBox" Width="300"/>
        </StackPanel>

        <CheckBox Grid.Row="4" x:Name="AutoReconnectCheckBox" Content="Reconnexion automatique" 
                  IsChecked="True" Margin="0,10,0,0"/>

        <Border Grid.Row="5" Background="#FFF9F9F9" Padding="10" Margin="0,20,0,0" CornerRadius="5">
            <StackPanel>
                <TextBlock FontWeight="Bold" Margin="0,0,0,5">ℹ️ Information</TextBlock>
                <TextBlock TextWrapping="Wrap" Foreground="#FF666666">
                    La synchronisation permet à plusieurs utilisateurs de travailler ensemble.
                    Le premier utilisateur connecté devient "Master" et peut modifier les données.
                    Les autres sont "Consultants" et visualisent les changements en temps réel.
                </TextBlock>
            </StackPanel>
        </Border>

        <StackPanel Grid.Row="6" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0">
            <Button Content="Tester la connexion" Width="140" Height="30" Margin="0,0,10,0" Click="TestConnection_Click"/>
            <Button Content="OK" Width="80" Height="30" Margin="0,0,10,0" Click="OK_Click" IsDefault="True"/>
            <Button Content="Annuler" Width="80" Height="30" Click="Cancel_Click" IsCancel="True"/>
        </StackPanel>
    </Grid>
</Window>
```

**Ploco/Dialogs/SyncConfigDialog.xaml.cs**
```csharp
using System.Windows;
using Ploco.Models;
using Ploco.Services;

namespace Ploco.Dialogs
{
    public partial class SyncConfigDialog : Window
    {
        public SyncConfiguration Configuration { get; private set; }

        public SyncConfigDialog(SyncConfiguration? existingConfig = null)
        {
            InitializeComponent();

            Configuration = existingConfig ?? new SyncConfiguration
            {
                ServerUrl = "http://localhost:5000",
                UserName = Environment.UserName,
                AutoReconnect = true
            };

            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            EnabledCheckBox.IsChecked = Configuration.Enabled;
            ServerUrlTextBox.Text = Configuration.ServerUrl;
            UserNameTextBox.Text = Configuration.UserName;
            AutoReconnectCheckBox.IsChecked = Configuration.AutoReconnect;
        }

        private void SaveConfiguration()
        {
            Configuration.Enabled = EnabledCheckBox.IsChecked ?? false;
            Configuration.ServerUrl = ServerUrlTextBox.Text;
            Configuration.UserName = UserNameTextBox.Text;
            Configuration.UserId = UserNameTextBox.Text; // Utiliser le nom comme ID pour l'instant
            Configuration.AutoReconnect = AutoReconnectCheckBox.IsChecked ?? true;
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();

            var testService = new SyncService(Configuration);
            var success = await testService.ConnectAsync();

            if (success)
            {
                MessageBox.Show("Connexion réussie !", "Test de connexion", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await testService.DisconnectAsync();
            }
            else
            {
                MessageBox.Show("Échec de la connexion. Vérifiez l'URL du serveur.", "Test de connexion",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            testService.Dispose();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
```

---

## 🚀 DÉMARRAGE ET TESTS

### Test Étape par Étape

**1. Compiler et Lancer le Serveur**
```bash
cd PlocoSync.Server
dotnet run
# Le serveur devrait écouter sur http://localhost:5000
```

**2. Lancer la première instance de Ploco (Master)**
- Ouvrir PlocoManager
- Aller dans Options > Configuration de la Synchronisation
- Activer la synchronisation
- Nom : "Alice"
- Cliquer "Tester la connexion" → devrait réussir
- OK
- Alice devient automatiquement Master

**3. Lancer la seconde instance de Ploco (Consultant)**
- Ouvrir une nouvelle instance de PlocoManager
- Configurer la synchronisation
- Nom : "Bob"
- Bob devient automatiquement Consultant

**4. Tester la Synchronisation**
- Alice (Master) : Déplacer une locomotive
- Bob (Consultant) : Devrait voir le déplacement immédiatement
- Alice : Changer le statut d'une locomotive
- Bob : Devrait voir le changement

---

## 📝 Résumé des Fichiers à Créer/Modifier

### Nouveaux Fichiers
```
PlocoSync.Server/               # Nouveau projet serveur
├── Program.cs
├── Models/SyncMessage.cs
├── Services/SessionManager.cs
└── Hubs/PlocoSyncHub.cs

Ploco/
├── Services/SyncService.cs     # Nouveau
├── Models/SyncModels.cs        # Nouveau
└── Dialogs/SyncConfigDialog.xaml(.cs)  # Nouveau
```

### Fichiers à Modifier
```
Ploco/
├── Ploco.csproj               # Ajouter package SignalR
└── MainWindow.xaml.cs         # Intégrer SyncService
```

---

## 🎯 Prochaines Étapes

1. **Implémenter les fonctionnalités de base** selon ce guide
2. **Tester avec 2-3 utilisateurs** en réseau local
3. **Ajouter plus de types de messages** (création de tiles, etc.)
4. **Améliorer l'UI** (indicateurs visuels, liste des utilisateurs)
5. **Robustesse** (gestion d'erreurs, reconnexion, etc.)

---

**Questions ?** Ce guide sera mis à jour avec les retours d'implémentation.

*Guide préparé par Copilot pour LinkAtPlug - PlocoManager Project*
