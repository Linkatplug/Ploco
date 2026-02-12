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
        public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
    }

    public class SessionManager
    {
        private readonly ConcurrentDictionary<string, UserSession> _sessions = new();
        private string? _currentMasterId;
        private readonly ILogger<SessionManager> _logger;

        public SessionManager(ILogger<SessionManager> logger)
        {
            _logger = logger;
        }

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
                _logger.LogInformation($"User {userName} ({userId}) connected as Master");
            }
            else
            {
                _logger.LogInformation($"User {userName} ({userId}) connected as Consultant");
            }

            return _sessions.TryAdd(connectionId, session);
        }

        public UserSession? RemoveSession(string connectionId)
        {
            if (_sessions.TryRemove(connectionId, out var session))
            {
                _logger.LogInformation($"User {session.UserName} ({session.UserId}) disconnected");

                // Si le Master se déconnecte, transférer à quelqu'un d'autre
                if (session.IsMaster && _sessions.Count > 0)
                {
                    var newMaster = _sessions.Values.FirstOrDefault();
                    if (newMaster != null)
                    {
                        newMaster.IsMaster = true;
                        _currentMasterId = newMaster.UserId;
                        _logger.LogInformation($"Master role automatically transferred to {newMaster.UserName} ({newMaster.UserId})");
                    }
                    else
                    {
                        _currentMasterId = null;
                    }
                }
                else if (session.IsMaster)
                {
                    _currentMasterId = null;
                    _logger.LogInformation("No Master - all users disconnected");
                }

                return session;
            }
            return null;
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

        public bool TransferMaster(string currentMasterConnectionId, string newMasterUserId)
        {
            // Vérifier que l'émetteur est bien le Master actuel
            if (!IsMaster(currentMasterConnectionId))
            {
                _logger.LogWarning($"Transfer rejected: {currentMasterConnectionId} is not Master");
                return false;
            }

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
                _logger.LogInformation($"Master transferred from {currentMaster?.UserName} to {newMaster.UserName}");
                return true;
            }

            _logger.LogWarning($"Transfer failed: User {newMasterUserId} not found");
            return false;
        }

        public List<UserSession> GetAllSessions()
        {
            return _sessions.Values.OrderBy(s => s.ConnectedAt).ToList();
        }

        public string? GetCurrentMasterId()
        {
            return _currentMasterId;
        }

        public UserSession? GetMasterSession()
        {
            return _sessions.Values.FirstOrDefault(s => s.IsMaster);
        }

        public void UpdateHeartbeat(string connectionId)
        {
            var session = GetSession(connectionId);
            if (session != null)
            {
                session.LastHeartbeat = DateTime.UtcNow;
            }
        }
    }
}
