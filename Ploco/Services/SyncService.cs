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
        private bool _isConnecting;

        public event EventHandler<SyncMessage>? ChangeReceived;
        public event EventHandler<bool>? MasterStatusChanged;
        public event EventHandler<bool>? ConnectionStatusChanged;
        public event EventHandler<string>? MasterRequested;

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
                    var result = await _connection.InvokeAsync<dynamic>(
                        "Connect",
                        _config.UserId,
                        _config.UserName
                    );

                    _isMaster = result.IsMaster;
                    MasterStatusChanged?.Invoke(this, _isMaster);
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
                _isConnecting = false;

                ConnectionStatusChanged?.Invoke(this, true);
                MasterStatusChanged?.Invoke(this, _isMaster);

                Logger.Info($"Connected as {(_isMaster ? "Master" : "Consultant")}", "Sync");
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

        private void HandleMasterTransferred(dynamic data)
        {
            string newMasterId = data.NewMasterId.ToString();
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

            // Si le Master se déconnecte et qu'un nouveau Master est désigné
            if (data.WasMaster && data.NewMasterId != null)
            {
                string newMasterId = data.NewMasterId.ToString();
                _isMaster = (newMasterId == _config.UserId);
                if (_isMaster)
                {
                    MasterStatusChanged?.Invoke(this, true);
                    Logger.Info("You are now the Master!", "Sync");
                }
            }
        }

        private void HandleMasterRequested(dynamic data)
        {
            string requesterName = data.RequesterName.ToString();
            Logger.Info($"Master requested by: {requesterName}", "Sync");
            MasterRequested?.Invoke(this, requesterName);
        }

        public void Dispose()
        {
            DisconnectAsync().Wait();
        }
    }
}
