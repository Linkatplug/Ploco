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
    }
}
