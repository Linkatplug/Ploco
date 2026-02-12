using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Ploco.Models;
using Ploco.Helpers;
using Newtonsoft.Json;

namespace Ploco.Services
{
    public class SyncService : IAsyncDisposable, IDisposable
    {
        private HubConnection? _connection;
        private readonly SyncConfiguration _config;
        private bool _isMaster;
        private bool _isConnected;
        private bool _isConnecting;
        private Timer? _heartbeatTimer;

        public event EventHandler<SyncMessage>? ChangeReceived;
        public event EventHandler<bool>? MasterStatusChanged;
        public event EventHandler<bool>? ConnectionStatusChanged;
        public event EventHandler<(string RequesterId, string RequesterName)>? MasterRequested;

        public bool IsConnected => _isConnected;
        public bool IsMaster => _isMaster;
        public SyncConfiguration Configuration => _config;

        public SyncService(SyncConfiguration config)
        {
            _config = config;
        }

        public async Task<bool> ConnectAsync()
        {
            if (_isConnecting || _isConnected)
            {
                Logger.Warning("Already connected or connecting", "Sync");
                return _isConnected;
            }

            if (!_config.Enabled || string.IsNullOrWhiteSpace(_config.ServerUrl))
            {
                Logger.Warning("Sync not enabled or server URL not configured", "Sync");
                return false;
            }

            _isConnecting = true;

            try
            {
                Logger.Info($"Connecting to sync server: {_config.ServerUrl}", "Sync");

                _connection = new HubConnectionBuilder()
                    .WithUrl($"{_config.ServerUrl}/syncHub")
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                    .Build();

                // Configurer les handlers avec types fortement typés
                _connection.On<SyncMessage>("ReceiveChange", HandleChangeReceived);
                _connection.On<MasterTransferredMessage>("MasterTransferred", HandleMasterTransferred);
                _connection.On<UserConnectedMessage>("UserConnected", HandleUserConnected);
                _connection.On<UserDisconnectedMessage>("UserDisconnected", HandleUserDisconnected);
                _connection.On<MasterRequestedMessage>("MasterRequested", HandleMasterRequested);

                _connection.Closed += async (error) =>
                {
                    _isConnected = false;
                    StopHeartbeat();
                    ConnectionStatusChanged?.Invoke(this, false);
                    Logger.Warning($"Connection closed: {error?.Message ?? "Unknown"}", "Sync");

                    if (_config.AutoReconnect)
                    {
                        await Task.Delay(_config.ReconnectDelaySeconds * 1000);
                        _ = ConnectAsync();
                    }
                };

                _connection.Reconnecting += (error) =>
                {
                    _isConnected = false;
                    StopHeartbeat();
                    ConnectionStatusChanged?.Invoke(this, false);
                    Logger.Info("Reconnecting...", "Sync");
                    return Task.CompletedTask;
                };

                _connection.Reconnected += async (connectionId) =>
                {
                    Logger.Info("Reconnected successfully", "Sync");
                    _isConnected = true;
                    ConnectionStatusChanged?.Invoke(this, true);

                    // Se réenregistrer
                    var result = await _connection.InvokeAsync<SyncConnectResponse>(
                        "Connect",
                        _config.UserId,
                        _config.UserName
                    );

                    // Respecter ForceConsultantMode après reconnexion
                    bool serverAssignedMaster = result.IsMaster;
                    _isMaster = _config.ForceConsultantMode ? false : serverAssignedMaster;
                    MasterStatusChanged?.Invoke(this, _isMaster);
                    
                    // Redémarrer le heartbeat
                    StartHeartbeat();
                };

                await _connection.StartAsync();

                // S'enregistrer auprès du serveur
                var result = await _connection.InvokeAsync<SyncConnectResponse>(
                    "Connect",
                    _config.UserId,
                    _config.UserName
                );

                // Gérer ForceConsultantMode - toujours consultant même si serveur donne master
                bool serverAssignedMaster = result.IsMaster;
                _isMaster = _config.ForceConsultantMode ? false : serverAssignedMaster;
                
                if (_config.ForceConsultantMode && serverAssignedMaster)
                {
                    Logger.Info("ForceConsultantMode active: Consultant forcé même si Master assigné", "Sync");
                }
                
                _isConnected = true;
                _isConnecting = false;

                ConnectionStatusChanged?.Invoke(this, true);
                MasterStatusChanged?.Invoke(this, _isMaster);

                Logger.Info($"Connected as {(_isMaster ? "Master" : "Consultant")}", "Sync");

                // Démarrer le heartbeat timer
                StartHeartbeat();

                // Si RequestMasterOnConnect et pas consultant forcé, demander master
                if (_config.RequestMasterOnConnect && !_config.ForceConsultantMode && !_isMaster)
                {
                    Logger.Info("RequestMasterOnConnect: Demande du rôle Master...", "Sync");
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1000); // Délai pour stabilisation
                        await RequestMasterAsync();
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                _isConnecting = false;
                _isConnected = false;
                Logger.Error("Failed to connect", ex, "Sync");
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            StopHeartbeat();
            
            if (_connection != null)
            {
                try
                {
                    await _connection.StopAsync();
                    await _connection.DisposeAsync();
                    _connection = null;
                    _isConnected = false;
                    ConnectionStatusChanged?.Invoke(this, false);
                    Logger.Info("Disconnected from sync server", "Sync");
                }
                catch (Exception ex)
                {
                    Logger.Error("Error disconnecting", ex, "Sync");
                }
            }
        }

        public async Task<bool> SendChangeAsync(string messageType, object data)
        {
            if (!_isConnected || !_isMaster || _connection == null)
            {
                Logger.Warning("Cannot send change: not connected or not master", "Sync");
                return false;
            }

            // Sécurité supplémentaire: refuser si consultant forcé
            if (_config.ForceConsultantMode)
            {
                Logger.Warning("Cannot send change: ForceConsultantMode is active", "Sync");
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
                Logger.Error("Failed to send change", ex, "Sync");
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
                Logger.Error("Failed to request master", ex, "Sync");
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
                    Logger.Info($"Master transferred to: {newMasterUserId}", "Sync");
                }
                return success;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to transfer master", ex, "Sync");
                return false;
            }
        }

        private void HandleChangeReceived(SyncMessage message)
        {
            Logger.Info($"Change received: {message.MessageType}", "Sync");
            ChangeReceived?.Invoke(this, message);
        }

        private void HandleMasterTransferred(MasterTransferredMessage message)
        {
            string newMasterId = message.NewMasterId;
            
            // Respecter ForceConsultantMode même lors d'un transfert
            bool shouldBeMaster = (newMasterId == _config.UserId) && !_config.ForceConsultantMode;
            _isMaster = shouldBeMaster;
            
            if (newMasterId == _config.UserId && _config.ForceConsultantMode)
            {
                Logger.Info("Master transféré mais ForceConsultantMode actif - restant Consultant", "Sync");
            }
            
            MasterStatusChanged?.Invoke(this, _isMaster);
            Logger.Info($"Master transferred to: {newMasterId} (Je suis Master: {_isMaster})", "Sync");
        }

        private void HandleUserConnected(UserConnectedMessage message)
        {
            Logger.Info($"User connected: {message.UserName}", "Sync");
        }

        private void HandleUserDisconnected(UserDisconnectedMessage message)
        {
            Logger.Info($"User disconnected: {message.UserName}", "Sync");

            // Si le Master se déconnecte et qu'un nouveau Master est désigné
            if (message.WasMaster && message.NewMasterId != null)
            {
                string newMasterId = message.NewMasterId;
                
                // Respecter ForceConsultantMode
                bool shouldBeMaster = (newMasterId == _config.UserId) && !_config.ForceConsultantMode;
                _isMaster = shouldBeMaster;
                
                if (_isMaster)
                {
                    MasterStatusChanged?.Invoke(this, true);
                    Logger.Info("You are now the Master!", "Sync");
                }
                else if (newMasterId == _config.UserId && _config.ForceConsultantMode)
                {
                    Logger.Info("Master proposé mais ForceConsultantMode actif - restant Consultant", "Sync");
                }
            }
        }

        private void HandleMasterRequested(MasterRequestedMessage message)
        {
            string requesterId = message.RequesterId;
            string requesterName = message.RequesterName;
            Logger.Info($"Master requested by: {requesterName} (ID: {requesterId})", "Sync");
            MasterRequested?.Invoke(this, (requesterId, requesterName));
        }

        private void StartHeartbeat()
        {
            StopHeartbeat();
            
            // Timer qui envoie un heartbeat toutes les 5 secondes
            _heartbeatTimer = new Timer(async _ =>
            {
                if (_isConnected && _connection != null)
                {
                    try
                    {
                        await _connection.InvokeAsync("Heartbeat");
                        Logger.Debug("Heartbeat sent", "Sync");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Heartbeat failed: {ex.Message}", "Sync");
                    }
                }
            }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            
            Logger.Info("Heartbeat timer started (5s interval)", "Sync");
        }

        private void StopHeartbeat()
        {
            if (_heartbeatTimer != null)
            {
                _heartbeatTimer.Dispose();
                _heartbeatTimer = null;
                Logger.Info("Heartbeat timer stopped", "Sync");
            }
        }

        /// <summary>
        /// Gets the current shared state from the server
        /// </summary>
        /// <returns>Database bytes or null if no state exists</returns>
        public async Task<byte[]?> GetStateAsync()
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected)
            {
                Logger.Warning("Cannot get state: Not connected", "Sync");
                return null;
            }

            try
            {
                Logger.Info("Requesting state from server", "Sync");
                var stateBytes = await _connection.InvokeAsync<byte[]?>("GetState");
                
                if (stateBytes == null)
                {
                    Logger.Info("Server has no state (starting fresh)", "Sync");
                }
                else
                {
                    Logger.Info($"Received state from server: {stateBytes.Length} bytes", "Sync");
                }

                return stateBytes;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get state from server: {ex.Message}", ex, "Sync");
                return null;
            }
        }

        /// <summary>
        /// Saves the current state to the server (Master only)
        /// </summary>
        /// <param name="dbBytes">Database file bytes</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SaveStateAsync(byte[] dbBytes)
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected)
            {
                Logger.Warning("Cannot save state: Not connected", "Sync");
                return false;
            }

            if (!_isMaster)
            {
                Logger.Warning("Cannot save state: Not Master", "Sync");
                return false;
            }

            try
            {
                Logger.Info($"Saving state to server: {dbBytes.Length} bytes", "Sync");
                var success = await _connection.InvokeAsync<bool>("SaveState", dbBytes);
                
                if (success)
                {
                    Logger.Info("State saved to server successfully", "Sync");
                }
                else
                {
                    Logger.Warning("Server rejected state save", "Sync");
                }

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save state to server: {ex.Message}", ex, "Sync");
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            StopHeartbeat();
            await DisconnectAsync();
        }

        public void Dispose()
        {
            StopHeartbeat();
            DisconnectAsync().Wait();
        }
    }
}
