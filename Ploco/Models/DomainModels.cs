using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ploco.Models
{
    public enum LocomotiveStatus
    {
        Ok,
        DefautMineur,
        AControler,
        HS
    }

    public enum TileType
    {
        Depot,
        ArretLigne,
        VoieGarage
    }

    public class RollingStockSeries
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StartNumber { get; set; }
        public int EndNumber { get; set; }
    }

    public class LocomotiveModel : INotifyPropertyChanged
    {
        private LocomotiveStatus _status;
        private int? _assignedTrackId;

        public int Id { get; set; }
        public int SeriesId { get; set; }
        public string SeriesName { get; set; } = string.Empty;
        public int Number { get; set; }

        public LocomotiveStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public int? AssignedTrackId
        {
            get => _assignedTrackId;
            set
            {
                if (_assignedTrackId != value)
                {
                    _assignedTrackId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DisplayName => $"{SeriesName}-{Number}";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TrackModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;

        public int Id { get; set; }
        public int TileId { get; set; }
        public int Position { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<LocomotiveModel> Locomotives { get; } = new ObservableCollection<LocomotiveModel>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TileModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private double _x;
        private double _y;

        public int Id { get; set; }
        public TileType Type { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? LocationPreset { get; set; }
        public int? GarageTrackNumber { get; set; }

        public double X
        {
            get => _x;
            set
            {
                if (_x != value)
                {
                    _x = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Y
        {
            get => _y;
            set
            {
                if (_y != value)
                {
                    _y = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<TrackModel> Tracks { get; } = new ObservableCollection<TrackModel>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
