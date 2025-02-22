using System.Windows;
using Ploco.Models;

namespace Ploco
{
    public partial class HistoriqueDialog : Window
    {
        public HistoriqueDialog(Locomotive loco)
        {
            InitializeComponent();
            // Définir l'objet locomotive comme contexte de données
            DataContext = loco;
        }

        private void btnFermer_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
