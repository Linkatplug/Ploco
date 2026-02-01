using System.Collections.Generic;

namespace Ploco.Models
{
    public class AppState
    {
        public List<RollingStockSeries> Series { get; set; } = new();
        public List<LocomotiveModel> Locomotives { get; set; } = new();
        public List<TileModel> Tiles { get; set; } = new();
        public string ActivePoolName { get; set; } = "Lineas";
        public bool HideNonActivePool { get; set; }
    }
}
