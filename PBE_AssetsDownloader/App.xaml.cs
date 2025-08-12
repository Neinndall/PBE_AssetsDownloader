using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.Utils;
using Serilog;
using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PBE_AssetsDownloader.UI;
using PBE_AssetsDownloader.UI.Dialogs;
using PBE_AssetsDownloader.UI.Views;
using PBE_AssetsDownloader.UI.Models;
using PBE_AssetsDownloader.UI.Views.Help;
using PBE_AssetsDownloader.UI.Views.Settings;
using Serilog.Events;

namespace PBE_AssetsDownloader
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
            services.AddSingleton<ILogger>(sp => new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .WriteTo.Logger(lc => lc
                    .MinimumLevel.Information() // Set minimum level for this sink to Information
                    .WriteTo.File("logs/application.log", rollingInterval: RollingInterval.Day))
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Fatal) // Include only Fatal
                    .WriteTo.File("logs/application_errors.log", rollingInterval: RollingInterval.Day))
                .CreateLogger());

            services.AddSingleton<LogService>();

            // Core Services
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
            services.AddSingleton<HashesManager>();
            services.AddSingleton<Resources>();
            services.AddSingleton<DirectoryCleaner>();
            services.AddSingleton<HashBackUp>();
            services.AddSingleton<HashCopier>();

            // Main Application Logic Service
            services.AddTransient<ExtractionService>();

            // Windows, Views, and Dialogs
            services.AddTransient<MainWindow>();
            services.AddTransient<HomeWindow>();
            services.AddTransient<ExportWindow>();
            services.AddTransient<ExplorerWindow>();
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

            // Initialize LibVLC using the centralized manager at application startup
            VlcManager.Initialize(logService);

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