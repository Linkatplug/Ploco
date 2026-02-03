using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ploco.Models
{
    public class RollingLineModel : INotifyPropertyChanged
    {
        private int _number;

        public int Id { get; set; }
        public int TileId { get; set; }

        public int Number
        {
            get => _number;
            set
            {
                if (_number != value)
                {
                    _number = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<LocomotiveModel> Locomotives { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
