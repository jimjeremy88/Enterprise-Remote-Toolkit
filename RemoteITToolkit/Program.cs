using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using RemoteITToolkit.Core.Entities;
using RemoteITToolkit.Core.Interfaces;
using RemoteITToolkit.Infrastructure.Database;
using RemoteITToolkit.Infrastructure.Logging;
using RemoteITToolkit.Presentation.Forms;
using RemoteITToolkit.Services;

namespace RemoteITToolkit
{
    static class Program
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += GlobalExceptionHandler;
            AppDomain.CurrentDomain.UnhandledException += GlobalUnhandledExceptionHandler;

            ConfigureServices();

            var mainForm = ServiceProvider.GetRequiredService<MainForm>();
            Application.Run(mainForm);
        }

        private static void ConfigureServices()
        {
            var services = new ServiceCollection();

            string dbFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
            if (!Directory.Exists(dbFolder)) Directory.CreateDirectory(dbFolder);
            string dbPath = Path.Combine(dbFolder, "RemoteIT.db");
            string connectionString = $"Data Source={dbPath};Version=3;";

            var dbInitializer = new SqliteDatabaseInitializer(connectionString);
            dbInitializer.Initialize();

            var logger = new EnterpriseLogger(connectionString);
            services.AddSingleton<ILogger>(logger);
            services.AddSingleton<IExtendedLogger>(logger);
            services.AddTransient<ISettingsRepository>(provider => new SettingsRepository(connectionString, provider.GetRequiredService<ILogger>()));

            services.AddTransient<ISystemInfoService, SystemInfoService>();
            services.AddTransient<ISystemAnalyzerService, SystemAnalyzerService>();
            services.AddTransient<INetworkToolsService, NetworkToolsService>();
            services.AddTransient<ISettingsService, SettingsService>();
            services.AddTransient<IReportGeneratorService, ReportGeneratorService>();
            services.AddTransient<IWindowsToolsService, WindowsToolsService>();
            services.AddTransient<ISystemQueryService, SystemQueryService>();

            services.AddSingleton<IRemoteSupportService>(provider =>
            {
                var service = new RemoteSupportService(provider.GetRequiredService<IExtendedLogger>());
                service.StartClipboardMonitor();
                return service;
            });

            services.AddTransient<MainForm>();
            ServiceProvider = services.BuildServiceProvider();
        }

        private static void GlobalExceptionHandler(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"An error occurred: {e.Exception.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"A fatal error occurred: {ex.Message}", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }
    }
}