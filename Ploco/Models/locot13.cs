using System.ComponentModel;

namespace Ploco.Models
{
    public enum StatutLocomotive
    {
        Ok,             // Vert = Loc OK
        DefautMineur,   // Jaune = Défaut Mineur
        AControler,     // Orange = Défaut à contrôler
        HS              // Rouge = HS / inutilisable
    }

    public class Locomotive : INotifyPropertyChanged
    {
        private bool _isOnCanvas;
        private StatutLocomotive _statut;

        public int NumeroSerie { get; set; }

        public bool IsOnCanvas
        {
            get => _isOnCanvas;
            set
            {
                if (_isOnCanvas != value)
                {
                    _isOnCanvas = value;
                    OnPropertyChanged(nameof(IsOnCanvas));
                }
            }
        }

        public StatutLocomotive Statut
        {
            get => _statut;
            set
            {
                if (_statut != value)
                {
                    _statut = value;
                    OnPropertyChanged(nameof(Statut));
                }
            }
        }

        public override string ToString()
        {
            return $"T13-{NumeroSerie}";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
