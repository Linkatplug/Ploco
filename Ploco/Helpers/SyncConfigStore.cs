using System;
using System.IO;
using System.Text.Json;
using Ploco.Models;

namespace Ploco.Helpers
{
    /// <summary>
    /// Helper class to load and save SyncConfiguration from/to %AppData%\PlocoManager\sync_config.json
    /// </summary>
    public static class SyncConfigStore
    {
        private const string ConfigFileName = "sync_config.json";
        
        /// <summary>
        /// Gets the path to the PlocoManager AppData directory
        /// </summary>
        private static string GetAppDataDirectory()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var plocoManagerFolder = Path.Combine(appDataFolder, "PlocoManager");
            
            if (!Directory.Exists(plocoManagerFolder))
            {
                Directory.CreateDirectory(plocoManagerFolder);
            }
            
            return plocoManagerFolder;
        }
        
        /// <summary>
        /// Gets the full path to the sync_config.json file
        /// </summary>
        private static string GetConfigFilePath()
        {
            return Path.Combine(GetAppDataDirectory(), ConfigFileName);
        }
        
        /// <summary>
        /// Loads the SyncConfiguration from file, or returns default configuration if file doesn't exist
        /// </summary>
        /// <returns>SyncConfiguration loaded from file or default values</returns>
        public static SyncConfiguration LoadOrDefault()
        {
            try
            {
                var configPath = GetConfigFilePath();
                
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<SyncConfiguration>(json);
                    
                    if (config != null)
                    {
                        Logger.Info($"Loaded sync configuration from {configPath}", "SyncConfigStore");
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load sync configuration, using defaults", ex, "SyncConfigStore");
            }
            
            // Return default configuration
            return CreateDefaultConfiguration();
        }
        
        /// <summary>
        /// Saves the SyncConfiguration to file
        /// </summary>
        /// <param name="config">Configuration to save</param>
        public static void Save(SyncConfiguration config)
        {
            try
            {
                var configPath = GetConfigFilePath();
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(configPath, json);
                Logger.Info($"Saved sync configuration to {configPath}", "SyncConfigStore");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save sync configuration", ex, "SyncConfigStore");
                throw;
            }
        }
        
        /// <summary>
        /// Deletes the configuration file if it exists
        /// </summary>
        public static void Delete()
        {
            try
            {
                var configPath = GetConfigFilePath();
                
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                    Logger.Info($"Deleted sync configuration from {configPath}", "SyncConfigStore");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to delete sync configuration", ex, "SyncConfigStore");
            }
        }
        
        /// <summary>
        /// Creates a default SyncConfiguration
        /// </summary>
        /// <returns>Default configuration with Enabled=false</returns>
        private static SyncConfiguration CreateDefaultConfiguration()
        {
            var userName = Environment.UserName;
            
            return new SyncConfiguration
            {
                Enabled = false,
                ServerUrl = "http://localhost:5000",
                UserId = userName,
                UserName = userName,
                AutoReconnect = true,
                ReconnectDelaySeconds = 5,
                ForceConsultantMode = false,
                RequestMasterOnConnect = false
            };
        }
    }
}
