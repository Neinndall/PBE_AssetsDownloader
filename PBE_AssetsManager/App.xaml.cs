using PBE_AssetsManager.Services.Hashes;
using PBE_AssetsManager.Services.Comparator;
using PBE_AssetsManager.Services.Downloads;
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Services.Models;
using PBE_AssetsManager.Services.Explorer;
using PBE_AssetsManager.Services.Monitor;
using PBE_AssetsManager.Services.Versions;
using PBE_AssetsManager.Utils;
using Serilog;
using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using Serilog.Events;
using Microsoft.Extensions.DependencyInjection;
using PBE_AssetsManager.Views;
using PBE_AssetsManager.Views.Dialogs;
using PBE_AssetsManager.Views.Models;
using PBE_AssetsManager.Views.Help;
using PBE_AssetsManager.Views.Settings;
using PBE_AssetsManager.Views.Controls;

namespace PBE_AssetsManager
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Logging
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .WriteTo.Logger(lc => lc
                    // .Filter.ByIncludingOnly(e => e.Level < LogEventLevel.Fatal) // Information, Warning, Error (Original)
                    .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Information && e.Level < LogEventLevel.Fatal) // Information, Warning, Error (Excludes Debug)
                    .WriteTo.File("logs/application.log", 
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}"))
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error && e.Exception != null) // Error, Fatal and with an Exception
                    .WriteTo.File("logs/application_errors.log", 
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
                .CreateLogger();

            Log.Logger = logger; // Assign the logger to the static Log.Logger
            services.AddSingleton<ILogger>(logger);
            
            // Core Services
            services.AddSingleton<PbeStatusService>();
            services.AddSingleton<LogService>();
            services.AddSingleton<HttpClient>();
            services.AddSingleton<DirectoriesCreator>();
            services.AddSingleton(provider => AppSettings.LoadSettings());
            services.AddSingleton<Requests>();
            services.AddSingleton<AssetDownloader>();
            services.AddSingleton<JsonDataService>();
            services.AddSingleton<Status>();
            services.AddSingleton<UpdateManager>();
            services.AddSingleton<UpdateExtractor>();
            services.AddSingleton<AssetsPreview>();
            services.AddSingleton<Resources>();
            services.AddSingleton<DirectoryCleaner>();
            services.AddSingleton<BackupManager>();
            services.AddSingleton<HashCopier>();
            services.AddSingleton<UpdateCheckService>();
            services.AddSingleton<ProgressUIManager>();
            services.AddTransient<ExplorerPreviewService>();
            services.AddSingleton<WadSearchBoxService>();
            services.AddSingleton<JsBeautifierService>();
            services.AddSingleton<DiffViewService>();
            services.AddSingleton<MonitorService>();

            // Versions Service
            services.AddSingleton<VersionService>();

            // Hashes Services
            services.AddSingleton<HashesManager>();
            services.AddSingleton<HashResolverService>();

            // Comparator Services
            services.AddSingleton<WadComparatorService>();
            services.AddSingleton<WadDifferenceService>();
            services.AddSingleton<WadPackagingService>();
            services.AddSingleton<WadNodeLoaderService>();
            services.AddSingleton<WadExtractionService>();

            // Model Viewer Services
            services.AddSingleton<ModelLoadingService>();

            // Main Application Logic Service
            services.AddTransient<ExtractionService>();

            // Windows, Views, and Dialogs
            services.AddTransient<MainWindow>();
            services.AddTransient<HomeWindow>();
            services.AddTransient<ExportWindow>();
            services.AddTransient<ExplorerWindow>();
            services.AddTransient<ComparatorWindow>();
            services.AddTransient<ModelWindow>();
            services.AddTransient<MonitorWindow>();
            services.AddTransient<HelpWindow>();
            services.AddTransient<JsonDiffWindow>();
            services.AddTransient<PreviewAssetsWindow>();
            services.AddTransient<SettingsWindow>();
            services.AddTransient<ProgressDetailsWindow>();
            services.AddTransient<UpdateProgressWindow>();
            services.AddTransient<UpdateModeDialog>();
            services.AddTransient<InputDialog>();
            services.AddTransient<ConfirmationDialog>();
            services.AddSingleton<CustomMessageBoxService>();

            // Secondary Views
            services.AddTransient<LogView>();
            services.AddTransient<GeneralSettingsView>();
            services.AddTransient<AdvancedSettingsView>();
            services.AddTransient<HashPathsSettingsView>();
            services.AddTransient<LogsSettingsView>();
            services.AddTransient<AboutView>();
            services.AddTransient<BugReportsView>();
            services.AddTransient<ChangelogsView>();
            services.AddTransient<UpdatesView>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var logService = ServiceProvider.GetRequiredService<LogService>();
            var customMessageBoxService = ServiceProvider.GetRequiredService<CustomMessageBoxService>();
            
            SetupGlobalExceptionHandling(logService, customMessageBoxService);

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void SetupGlobalExceptionHandling(LogService logService, CustomMessageBoxService customMessageBoxService)
        {
            DispatcherUnhandledException += (sender, args) =>
            {
                var ex = args.Exception;
                logService.LogError("An unhandled UI exception occurred. See application_errors.log for details.");
                logService.LogCritical(ex, "Unhandled UI Exception");

                customMessageBoxService.ShowError(
                    "Error",
                    "A critical error occurred in the UI. Please check the logs for details.",
                    null,
                    CustomMessageBoxIcon.Error
                );
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                logService.LogError("An unhandled non-UI exception occurred. See application_errors.log for details.");
                logService.LogCritical(ex, "Unhandled Non-UI Exception");

                customMessageBoxService.ShowError(
                    "Error",
                    "A critical error occurred in a background process. Please check the logs for details.",
                    null,
                    CustomMessageBoxIcon.Error
                );
            };
        }
    }
}