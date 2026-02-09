using System;
using System.IO;
using System.Text;

namespace Ploco.Helpers
{
    /// <summary>
    /// Centralized logging system for the Ploco application.
    /// Logs all important events to help diagnose issues and track operations.
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string? _logFilePath;
        private static bool _isInitialized = false;

        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }

        /// <summary>
        /// Gets the path to the logs directory
        /// </summary>
        public static string LogsDirectory
        {
            get
            {
                var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var logsPath = Path.Combine(appDataFolder, "Ploco", "Logs");
                
                if (!Directory.Exists(logsPath))
                {
                    Directory.CreateDirectory(logsPath);
                }
                
                return logsPath;
            }
        }

        /// <summary>
        /// Gets the current log file path
        /// </summary>
        public static string CurrentLogFilePath
        {
            get
            {
                if (!_isInitialized)
                {
                    Initialize();
                }
                return _logFilePath!;
            }
        }

        /// <summary>
        /// Initializes the logger with a new log file for the current session
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_isInitialized)
                    return;

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var fileName = $"Ploco_{timestamp}.log";
                _logFilePath = Path.Combine(LogsDirectory, fileName);

                // Create the log file and write header
                WriteToFile($"=== Ploco Application Log - Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                WriteToFile($"Log file: {_logFilePath}");
                WriteToFile("");

                _isInitialized = true;

                // Clean up old log files (keep last 30 days)
                CleanOldLogs();
            }
        }

        /// <summary>
        /// Logs a debug message
        /// </summary>
        public static void Debug(string message, string? context = null)
        {
            Log(LogLevel.Debug, message, context);
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        public static void Info(string message, string? context = null)
        {
            Log(LogLevel.Info, message, context);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        public static void Warning(string message, string? context = null)
        {
            Log(LogLevel.Warning, message, context);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        public static void Error(string message, Exception? exception = null, string? context = null)
        {
            var fullMessage = message;
            if (exception != null)
            {
                fullMessage += $"\nException: {exception.GetType().Name}";
                fullMessage += $"\nMessage: {exception.Message}";
                fullMessage += $"\nStackTrace: {exception.StackTrace}";
                
                if (exception.InnerException != null)
                {
                    fullMessage += $"\nInner Exception: {exception.InnerException.Message}";
                }
            }
            
            Log(LogLevel.Error, fullMessage, context);
        }

        /// <summary>
        /// Main logging method
        /// </summary>
        private static void Log(LogLevel level, string message, string? context = null)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            lock (_lock)
            {
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var levelStr = level.ToString().ToUpper().PadRight(7);
                    var contextStr = !string.IsNullOrEmpty(context) ? $"[{context}] " : "";
                    
                    var logLine = $"[{timestamp}] [{levelStr}] {contextStr}{message}";
                    
                    WriteToFile(logLine);
                }
                catch (Exception ex)
                {
                    // If logging fails, write to console as fallback
                    Console.WriteLine($"Logger Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Writes a line to the log file
        /// </summary>
        private static void WriteToFile(string line)
        {
            try
            {
                File.AppendAllText(_logFilePath!, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Silently fail if we can't write to the log file
            }
        }

        /// <summary>
        /// Cleans up old log files (older than 30 days)
        /// </summary>
        private static void CleanOldLogs()
        {
            try
            {
                var logsDir = new DirectoryInfo(LogsDirectory);
                var cutoffDate = DateTime.Now.AddDays(-30);
                
                foreach (var file in logsDir.GetFiles("Ploco_*.log"))
                {
                    if (file.CreationTime < cutoffDate)
                    {
                        try
                        {
                            file.Delete();
                            Info($"Deleted old log file: {file.Name}", "Logger");
                        }
                        catch
                        {
                            // Ignore errors when deleting old logs
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors in cleanup
            }
        }

        /// <summary>
        /// Logs application shutdown
        /// </summary>
        public static void Shutdown()
        {
            Info("Application shutting down", "Application");
            WriteToFile("");
            WriteToFile($"=== Ploco Application Log - Ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        }
    }
}
