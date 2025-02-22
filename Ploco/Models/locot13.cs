using System;
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
        private string _defautMoteurDetails;
        private string _emDetails;
        private string _atevapDetails;
        private string _modificationNotes;
        private DateTime? _lastModificationDate;

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

        // Nouvelles propriétés pour les infos supplémentaires :
        public string DefautMoteurDetails
        {
            get => _defautMoteurDetails;
            set
            {
                if (_defautMoteurDetails != value)
                {
                    _defautMoteurDetails = value;
                    OnPropertyChanged(nameof(DefautMoteurDetails));
                }
            }
        }

        public string EMDetails
        {
            get => _emDetails;
            set
            {
                if (_emDetails != value)
                {
                    _emDetails = value;
                    OnPropertyChanged(nameof(EMDetails));
                }
            }
        }

        public string ATEVAPDetails
        {
            get => _atevapDetails;
            set
            {
                if (_atevapDetails != value)
                {
                    _atevapDetails = value;
                    OnPropertyChanged(nameof(ATEVAPDetails));
                }
            }
        }

        public string ModificationNotes
        {
            get => _modificationNotes;
            set
            {
                if (_modificationNotes != value)
                {
                    _modificationNotes = value;
                    OnPropertyChanged(nameof(ModificationNotes));
                }
            }
        }

        public DateTime? LastModificationDate
        {
            get => _lastModificationDate;
            set
            {
                if (_lastModificationDate != value)
                {
                    _lastModificationDate = value;
                    OnPropertyChanged(nameof(LastModificationDate));
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
