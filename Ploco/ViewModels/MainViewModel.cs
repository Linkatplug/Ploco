using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ploco.Models;

namespace Ploco.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        // Données d'état qui étaient auparavant dans le code-behind de MainWindow
        
        [ObservableProperty]
        private ObservableCollection<LocomotiveModel> _locomotives = new();

        [ObservableProperty]
        private ObservableCollection<TileModel> _tiles = new();

        [ObservableProperty]
        private ObservableCollection<LayoutPreset> _layoutPresets = new();

        // Référence au repository
        private Ploco.Data.PlocoRepository? _repository;

        // Événements pour indiquer à la vue qu'elle doit mettre à jour son UI (semi-MVVM temporaire)
        public event Action? OnStateLoaded;
        public event Action? OnStatePersisted;

        public MainViewModel()
        {
            // Initialisation de base
        }

        public void Initialize(Ploco.Data.PlocoRepository repository, Action persistStateCallback, Action loadStateCallback)
        {
            _repository = repository;
            OnStatePersisted = persistStateCallback;
            OnStateLoaded = loadStateCallback;
        }

        // --- Commandes ---

        [RelayCommand]
        public void SaveDatabase()
        {
            OnStatePersisted?.Invoke();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Base de données Ploco (*.db)|*.db|Tous les fichiers (*.*)|*.*",
                FileName = "ploco.db"
            };

            if (dialog.ShowDialog() == true && _repository != null)
            {
                _repository.CopyDatabaseTo(dialog.FileName);
            }
        }

        [RelayCommand]
        public void LoadDatabase()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Base de données Ploco (*.db)|*.db|Tous les fichiers (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true && _repository != null)
            {
                if (!_repository.ReplaceDatabaseWith(dialog.FileName))
                {
                    System.Windows.MessageBox.Show("Le fichier sélectionné n'est pas une base SQLite valide.", "Chargement", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                _repository.Initialize();
                OnStateLoaded?.Invoke();
            }
        }
    }
}
