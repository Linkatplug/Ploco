using System;

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
        public bool ForceConsultantMode { get; set; } = false; // 🆕 Force le mode Consultant (lecture seule)
        public bool RequestMasterOnConnect { get; set; } = false; // 🆕 Demander le Master au démarrage si possible
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
        public string? Name { get; set; }
        public double? X { get; set; }
        public double? Y { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
    }

    // DTOs pour les messages SignalR (éviter dynamic/JsonElement)
    
    public class SyncConnectResponse
    {
        public bool IsMaster { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

    public class MasterTransferredMessage
    {
        public string NewMasterId { get; set; } = string.Empty;
        public string NewMasterName { get; set; } = string.Empty;
    }

    public class UserConnectedMessage
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

    public class UserDisconnectedMessage
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool WasMaster { get; set; }
        public string? NewMasterId { get; set; }
        public string? NewMasterName { get; set; }
    }

    public class MasterRequestedMessage
    {
        public string RequesterId { get; set; } = string.Empty;
        public string RequesterName { get; set; } = string.Empty;
    }
}
