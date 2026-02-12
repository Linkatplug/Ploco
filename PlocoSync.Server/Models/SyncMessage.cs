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
        public string? Name { get; set; }
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
