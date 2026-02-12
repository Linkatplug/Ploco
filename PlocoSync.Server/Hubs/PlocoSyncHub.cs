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
            
            // Notifier tous les autres clients
            await Clients.Others.SendAsync("UserConnected", new
            {
                UserId = userId,
                UserName = userName,
                IsMaster = session?.IsMaster ?? false,
                ConnectedAt = DateTime.UtcNow
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
                    s.IsMaster,
                    s.ConnectedAt
                }).ToList()
            };
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var session = _sessionManager.RemoveSession(Context.ConnectionId);
            if (session != null)
            {
                var wasMaster = session.IsMaster;
                var newMasterId = wasMaster ? _sessionManager.GetCurrentMasterId() : null;

                // Notifier les autres
                await Clients.Others.SendAsync("UserDisconnected", new
                {
                    session.UserId,
                    session.UserName,
                    WasMaster = wasMaster,
                    NewMasterId = newMasterId,
                    NewMaster = newMasterId != null ? _sessionManager.GetMasterSession() : null
                });
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task<bool> SendChange(SyncMessage message)
        {
            // Vérifier que l'émetteur est le Master
            if (!_sessionManager.IsMaster(Context.ConnectionId))
            {
                _logger.LogWarning($"Non-master user tried to send change: {message.UserId} - Type: {message.MessageType}");
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
            var masterSession = _sessionManager.GetMasterSession();
            if (masterSession != null)
            {
                await Clients.Client(masterSession.ConnectionId).SendAsync("MasterRequested", new
                {
                    RequesterId = session.UserId,
                    RequesterName = session.UserName
                });
                _logger.LogInformation($"Master request from {session.UserName} sent to {masterSession.UserName}");
                return true;
            }

            return false;
        }

        public async Task<bool> TransferMaster(string newMasterUserId)
        {
            // Vérifier que l'émetteur est le Master actuel
            if (!_sessionManager.IsMaster(Context.ConnectionId))
            {
                _logger.LogWarning($"Transfer rejected: {Context.ConnectionId} is not Master");
                return false;
            }

            if (_sessionManager.TransferMaster(Context.ConnectionId, newMasterUserId))
            {
                // Notifier tous les clients
                var newMasterSession = _sessionManager.GetAllSessions()
                    .FirstOrDefault(s => s.UserId == newMasterUserId);

                await Clients.All.SendAsync("MasterTransferred", new
                {
                    NewMasterId = newMasterUserId,
                    NewMasterName = newMasterSession?.UserName
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

        public async Task Heartbeat()
        {
            _sessionManager.UpdateHeartbeat(Context.ConnectionId);
            await Task.CompletedTask;
        }
    }
}
