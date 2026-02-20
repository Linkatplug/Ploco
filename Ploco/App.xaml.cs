using System;
using System.IO;
using System.Windows;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Ploco.Data;
using Ploco.ViewModels;

namespace Ploco
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ploco", "Logs", "ploco_.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
                .CreateLogger();

            Log.Information("=== Application Ploco Démarrée ===");

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IPlocoRepository>(provider => new PlocoRepository("ploco.db"));
            services.AddTransient<MainViewModel>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("=== Application Ploco Arrêtée ===");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
