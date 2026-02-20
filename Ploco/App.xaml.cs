using System;
using System.IO;
using System.Windows;
using Serilog;

namespace Ploco
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ploco", "Logs", "ploco_.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
                .CreateLogger();

            Log.Information("=== Application Ploco Démarrée ===");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("=== Application Ploco Arrêtée ===");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
