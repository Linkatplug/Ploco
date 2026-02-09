using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Ploco.Helpers
{
    /// <summary>
    /// Helper class to save and restore window size and position
    /// </summary>
    public static class WindowSettingsHelper
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Ploco"
        );
        
        private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "WindowSettings.json");

        public class WindowSettings
        {
            public double Width { get; set; }
            public double Height { get; set; }
            public double Left { get; set; }
            public double Top { get; set; }
            public string WindowState { get; set; } = "Normal";
        }

        /// <summary>
        /// Save window settings to JSON file
        /// </summary>
        public static void SaveWindowSettings(Window window, string windowName)
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
                }

                // Load existing settings
                var allSettings = LoadAllSettings();

                // Update settings for this window
                allSettings[windowName] = new WindowSettings
                {
                    Width = window.ActualWidth,
                    Height = window.ActualHeight,
                    Left = window.Left,
                    Top = window.Top,
                    WindowState = window.WindowState.ToString()
                };

                // Save to file
                var json = JsonSerializer.Serialize(allSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                Logger.Error($"Failed to save window settings for {windowName}", ex);
            }
        }

        /// <summary>
        /// Restore window settings from JSON file
        /// </summary>
        public static void RestoreWindowSettings(Window window, string windowName)
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    return; // No settings file yet
                }

                var allSettings = LoadAllSettings();
                
                if (allSettings.ContainsKey(windowName))
                {
                    var settings = allSettings[windowName];
                    
                    // Restore size
                    if (settings.Width > 0 && settings.Height > 0)
                    {
                        window.Width = settings.Width;
                        window.Height = settings.Height;
                    }

                    // Restore position (check if position is valid and visible on screen)
                    if (IsPositionVisibleOnAnyScreen(settings.Left, settings.Top, settings.Width, settings.Height))
                    {
                        window.Left = settings.Left;
                        window.Top = settings.Top;
                    }
                    else
                    {
                        // Center on screen if saved position is not visible
                        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    }

                    // Restore window state
                    if (Enum.TryParse<WindowState>(settings.WindowState, out var windowState))
                    {
                        window.WindowState = windowState;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                Logger.Error($"Failed to restore window settings for {windowName}", ex);
            }
        }

        /// <summary>
        /// Load all settings from JSON file
        /// </summary>
        private static Dictionary<string, WindowSettings> LoadAllSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    return new Dictionary<string, WindowSettings>();
                }

                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<Dictionary<string, WindowSettings>>(json) 
                    ?? new Dictionary<string, WindowSettings>();
            }
            catch
            {
                return new Dictionary<string, WindowSettings>();
            }
        }

        /// <summary>
        /// Check if a window position would be visible on any screen
        /// </summary>
        private static bool IsPositionVisibleOnAnyScreen(double left, double top, double width, double height)
        {
            // Simple check: ensure position is not completely off-screen
            // For WPF, we use System Working Area
            var workingArea = SystemParameters.WorkArea;
            
            // Check if window would be at least partially visible
            var windowRight = left + width;
            var windowBottom = top + height;
            
            // Window is visible if it intersects with working area
            bool isVisible = left < workingArea.Right && 
                           windowRight > workingArea.Left &&
                           top < workingArea.Bottom && 
                           windowBottom > workingArea.Top;
            
            return isVisible;
        }
    }
}
