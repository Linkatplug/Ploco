using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Ploco.Models;

namespace Ploco
{
    public partial class HistoriqueWindow : Window
    {
        public HistoriqueWindow(IEnumerable<HistoryEntry> historyEntries)
        {
            InitializeComponent();
            var entries = historyEntries?.ToList() ?? new List<HistoryEntry>();
            if (entries.Any())
            {
                tbHistorique.Text = string.Join(Environment.NewLine, entries.Select(entry =>
                    $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} | {entry.Action} | {entry.Details}"));
            }
            else
            {
                tbHistorique.Text = "Aucun historique disponible.";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Restore window settings
            Helpers.WindowSettingsHelper.RestoreWindowSettings(this, "HistoriqueWindow");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save window settings
            Helpers.WindowSettingsHelper.SaveWindowSettings(this, "HistoriqueWindow");
        }
    }
}
