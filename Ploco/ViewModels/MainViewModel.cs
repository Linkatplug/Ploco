using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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

        public MainViewModel()
        {
            // Initialisation de base
        }
    }
}
