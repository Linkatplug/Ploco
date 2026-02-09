using Ploco.Helpers;
using Ploco.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Ploco
{
    public partial class ImportWindow : Window
    {
        private readonly IEnumerable<LocomotiveModel> _locomotives;
        private readonly Action _onImportComplete;

        public ImportWindow(IEnumerable<LocomotiveModel> locomotives, Action onImportComplete)
        {
            InitializeComponent();
            _locomotives = locomotives;
            _onImportComplete = onImportComplete;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowSettingsHelper.RestoreWindowSettings(this, nameof(ImportWindow));
            
            // Try to load clipboard content
            try
            {
                if (Clipboard.ContainsText())
                {
                    TxtLocomotives.Text = Clipboard.GetText();
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error loading clipboard: {ex.Message}", "ImportWindow");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowSettingsHelper.SaveWindowSettings(this, nameof(ImportWindow));
        }

        private void BtnImportLocomotives_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = TxtLocomotives.Text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show("Veuillez coller les numéros de locomotives.", 
                        "Aucune donnée", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Parse locomotive numbers from text
                var importedNumbers = new HashSet<int>();
                var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (int.TryParse(trimmed, out int number))
                    {
                        importedNumbers.Add(number);
                    }
                }

                if (importedNumbers.Count == 0)
                {
                    MessageBox.Show("Aucun numéro de locomotive valide trouvé.", 
                        "Aucune donnée valide", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Perform the import
                int added = 0;
                int removed = 0;
                int unchanged = 0;

                foreach (var loco in _locomotives)
                {
                    bool shouldBeInSibelit = importedNumbers.Contains(loco.Number);
                    bool isInSibelit = loco.Pool == "Sibelit";

                    if (shouldBeInSibelit && !isInSibelit)
                    {
                        // Add to Sibelit
                        loco.Pool = "Sibelit";
                        added++;
                        Logger.Info($"Locomotive {loco.Number} ajoutée à Sibelit", "ImportWindow");
                    }
                    else if (!shouldBeInSibelit && isInSibelit)
                    {
                        // Return to Lineas
                        loco.Pool = "Lineas";
                        removed++;
                        Logger.Info($"Locomotive {loco.Number} retournée à Lineas", "ImportWindow");
                    }
                    else if (shouldBeInSibelit && isInSibelit)
                    {
                        // Already in Sibelit, no change
                        unchanged++;
                    }
                }

                // Show result
                var message = $"Import terminé!\n\n" +
                              $"- {added} locomotive(s) ajoutée(s) à Sibelit\n" +
                              $"- {removed} locomotive(s) retournée(s) à Lineas\n" +
                              $"- {unchanged} locomotive(s) déjà dans Sibelit (inchangées)";

                MessageBox.Show(message, "Import réussi", MessageBoxButton.OK, MessageBoxImage.Information);

                Logger.Info($"Import locomotives: {added} ajoutées, {removed} retirées, {unchanged} inchangées", "ImportWindow");

                // Notify parent to refresh and save
                _onImportComplete?.Invoke();

                // Close window
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.Error("Error importing locomotives", ex, "ImportWindow");
                MessageBox.Show($"Erreur lors de l'import: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImportDates_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Cette fonctionnalité est en cours de développement.", 
                "En cours de développement", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
