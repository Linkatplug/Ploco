using System.Windows;

namespace Ploco.Models
{
    public class LocomotiveDropArgs
    {
        public LocomotiveModel Loco { get; set; }
        public object Target { get; set; } // TrackModel, TileModel, or null/identifier for Pool
        public int InsertIndex { get; set; }
        public Point DropPosition { get; set; }
        public double TargetActualWidth { get; set; }
        public bool IsRollingLineRow { get; set; }
    }
}
