using System;
using System.IO;
using System.Text.Json;
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
        private const string ConfigFileName = "sync_config.json";

        public enum SyncMode
        {
            Disabled,
            Master,
            Consultant
        }

        public class SyncStartupConfiguration
        {
            public SyncMode Mode { get; set; }
            public string ServerUrl { get; set; } = "http://localhost:5000";
            public string UserName { get; set; } = string.Empty;
            public bool RememberChoice { get; set; }
        }

        public SyncStartupConfiguration Configuration { get; private set; }
        public bool ShouldQuit { get; private set; }

        public SyncStartupDialog()
        {
            InitializeComponent();
            Configuration = new SyncStartupConfiguration();
            LoadSavedConfiguration();
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            // Définir le nom d'utilisateur par défaut
            if (string.IsNullOrWhiteSpace(UserNameTextBox.Text))
            {
                UserNameTextBox.Text = Environment.UserName;
            }
        }

        private string GetConfigFilePath()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var plocoFolder = Path.Combine(appDataFolder, "Ploco");
            
            if (!Directory.Exists(plocoFolder))
            {
                Directory.CreateDirectory(plocoFolder);
            }
            
            return Path.Combine(plocoFolder, ConfigFileName);
        }

        private void LoadSavedConfiguration()
        {
            try
            {
                var configPath = GetConfigFilePath();
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var savedConfig = JsonSerializer.Deserialize<SyncStartupConfiguration>(json);
                    
                    if (savedConfig != null && savedConfig.RememberChoice)
                    {
                        // Appliquer la configuration sauvegardée
                        ServerUrlTextBox.Text = savedConfig.ServerUrl;
                        UserNameTextBox.Text = savedConfig.UserName;
                        RememberChoiceCheckBox.IsChecked = true;

                        switch (savedConfig.Mode)
                        {
                            case SyncMode.Disabled:
                                RadioDisabled.IsChecked = true;
                                break;
                            case SyncMode.Master:
                                RadioMaster.IsChecked = true;
                                break;
                            case SyncMode.Consultant:
                                RadioConsultant.IsChecked = true;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load sync configuration", ex, "SyncStartup");
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                if (RememberChoiceCheckBox.IsChecked == true)
                {
                    var configPath = GetConfigFilePath();
                    var json = JsonSerializer.Serialize(Configuration, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    File.WriteAllText(configPath, json);
                }
                else
                {
                    // Si on ne veut pas se souvenir, supprimer le fichier existant
                    var configPath = GetConfigFilePath();
                    if (File.Exists(configPath))
                    {
                        File.Delete(configPath);
                    }
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
            // Déterminer le mode choisi
            if (RadioDisabled.IsChecked == true)
            {
                Configuration.Mode = SyncMode.Disabled;
            }
            else if (RadioMaster.IsChecked == true)
            {
                Configuration.Mode = SyncMode.Master;
            }
            else if (RadioConsultant.IsChecked == true)
            {
                Configuration.Mode = SyncMode.Consultant;
            }

            // Valider si la synchronisation est activée
            if (Configuration.Mode != SyncMode.Disabled)
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

                Configuration.ServerUrl = ServerUrlTextBox.Text.Trim();
                Configuration.UserName = UserNameTextBox.Text.Trim();
            }

            Configuration.RememberChoice = RememberChoiceCheckBox.IsChecked == true;

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
