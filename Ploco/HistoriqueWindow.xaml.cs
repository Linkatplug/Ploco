using System.IO;
using System.Windows;

namespace Ploco
{
    public partial class HistoriqueWindow : Window
    {
        public HistoriqueWindow()
        {
            InitializeComponent();
            // Vérifie si le fichier log existe et le charge
            if (File.Exists("StatutModificationLog.txt"))
            {
                tbHistorique.Text = File.ReadAllText("StatutModificationLog.txt");
            }
            else
            {
                tbHistorique.Text = "Aucun historique disponible.";
            }
        }
    }
}
