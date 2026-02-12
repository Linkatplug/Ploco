using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Ploco.Models;
using Ploco.Services;
using Ploco.Helpers;

namespace Ploco.Dialogs
{
    public partial class SyncStartupDialog : Window
    {
        public enum SyncMode
        {
            Disabled,
            Master,
            Consultant
        }

        public SyncConfiguration Configuration { get; private set; }
        public bool ShouldQuit { get; private set; }

        public SyncStartupDialog()
        {
            InitializeComponent();
            Configuration = new SyncConfiguration();
            LoadSavedConfiguration();
        }

        private void LoadSavedConfiguration()
        {
            try
            {
                // Load configuration using SyncConfigStore
                var savedConfig = SyncConfigStore.LoadOrDefault();
                
                // Apply saved values to UI
                ServerUrlTextBox.Text = savedConfig.ServerUrl;
                UserNameTextBox.Text = savedConfig.UserName;
                
                // Determine mode from saved configuration and whether to remember
                if (!savedConfig.Enabled)
                {
                    RadioDisabled.IsChecked = true;
                    RememberChoiceCheckBox.IsChecked = false;
                }
                else if (savedConfig.ForceConsultantMode)
                {
                    RadioConsultant.IsChecked = true;
                    RememberChoiceCheckBox.IsChecked = true;
                }
                else if (savedConfig.RequestMasterOnConnect)
                {
                    RadioMaster.IsChecked = true;
                    RememberChoiceCheckBox.IsChecked = true;
                }
                else if (savedConfig.Enabled)
                {
                    // Enabled but no specific mode flags - could be either
                    RadioMaster.IsChecked = true;
                    RememberChoiceCheckBox.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load sync configuration", ex, "SyncStartup");
                // Set defaults on error
                ServerUrlTextBox.Text = "http://localhost:5000";
                UserNameTextBox.Text = Environment.UserName;
                RadioDisabled.IsChecked = true;
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                if (RememberChoiceCheckBox.IsChecked == true)
                {
                    // Save using SyncConfigStore
                    SyncConfigStore.Save(Configuration);
                }
                else
                {
                    // Delete saved configuration if user doesn't want to remember
                    SyncConfigStore.Delete();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save sync configuration", ex, "SyncStartup");
            }
        }

        private void Mode_Changed(object sender, RoutedEventArgs e)
        {
            // Activer/désactiver le panneau de configuration du serveur
            bool syncEnabled = RadioMaster.IsChecked == true || RadioConsultant.IsChecked == true;
            ServerConfigPanel.IsEnabled = syncEnabled;

            if (!syncEnabled)
            {
                ConnectionStatusText.Text = "";
            }
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ServerUrlTextBox.Text))
            {
                MessageBox.Show("Veuillez saisir l'adresse du serveur.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(UserNameTextBox.Text))
            {
                MessageBox.Show("Veuillez saisir votre nom d'utilisateur.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TestConnectionButton.IsEnabled = false;
            ConnectionStatusText.Text = "Test en cours...";
            ConnectionStatusText.Foreground = new SolidColorBrush(Colors.Gray);

            try
            {
                var testConfig = new SyncConfiguration
                {
                    Enabled = true,
                    ServerUrl = ServerUrlTextBox.Text.Trim(),
                    UserId = UserNameTextBox.Text.Trim(),
                    UserName = UserNameTextBox.Text.Trim(),
                    AutoReconnect = false
                };

                var syncService = new SyncService(testConfig);
                var connected = await syncService.ConnectAsync();

                if (connected)
                {
                    ConnectionStatusText.Text = "✓ Connexion réussie !";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Colors.Green);
                    
                    await syncService.DisconnectAsync();
                    syncService.Dispose();
                }
                else
                {
                    ConnectionStatusText.Text = "✗ Connexion échouée. Vérifiez l'URL du serveur.";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
            catch (Exception ex)
            {
                ConnectionStatusText.Text = $"✗ Erreur : {ex.Message}";
                ConnectionStatusText.Foreground = new SolidColorBrush(Colors.Red);
                Logger.Error("Connection test failed", ex, "SyncStartup");
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
            }
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            // Build SyncConfiguration from dialog values
            SyncMode mode;
            if (RadioDisabled.IsChecked == true)
            {
                mode = SyncMode.Disabled;
            }
            else if (RadioMaster.IsChecked == true)
            {
                mode = SyncMode.Master;
            }
            else if (RadioConsultant.IsChecked == true)
            {
                mode = SyncMode.Consultant;
            }
            else
            {
                mode = SyncMode.Disabled;
            }

            // Valider si la synchronisation est activée
            if (mode != SyncMode.Disabled)
            {
                if (string.IsNullOrWhiteSpace(ServerUrlTextBox.Text))
                {
                    MessageBox.Show("Veuillez saisir l'adresse du serveur.", "Validation", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(UserNameTextBox.Text))
                {
                    MessageBox.Show("Veuillez saisir votre nom d'utilisateur.", "Validation", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Create complete SyncConfiguration
            Configuration = new SyncConfiguration
            {
                Enabled = mode != SyncMode.Disabled,
                ServerUrl = ServerUrlTextBox.Text.Trim(),
                UserId = UserNameTextBox.Text.Trim(),
                UserName = UserNameTextBox.Text.Trim(),
                AutoReconnect = true,
                ReconnectDelaySeconds = 5,
                ForceConsultantMode = mode == SyncMode.Consultant,
                RequestMasterOnConnect = mode == SyncMode.Master
            };

            // Sauvegarder la configuration si demandé
            SaveConfiguration();

            ShouldQuit = false;
            DialogResult = true;
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            ShouldQuit = true;
            DialogResult = false;
        }
    }
}
