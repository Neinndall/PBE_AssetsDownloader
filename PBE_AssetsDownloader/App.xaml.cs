// PBE_AssetsDownloader/App.xaml.cs
using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using Serilog;
using System.Windows;
using Serilog.Events;

using PBE_AssetsDownloader.UI;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.UI.Dialogs;
using PBE_AssetsDownloader.UI.Helpers;

namespace PBE_AssetsDownloader
{
    public partial class App : Application
    {
        private LogService _logService;
        private HttpClient _httpClient;
        private DirectoriesCreator _directoriesCreator;
        private Requests _requests;
        private Status _status;
        private AssetDownloader _assetDownloader;
        private AppSettings _appSettings;
        private JsonDataService _jsonDataService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    Path.Combine("logs", "application.log"),
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .WriteTo.File(
                    Path.Combine("logs", "application_errors.log"),
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Error,
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .WriteTo.Debug(restrictedToMinimumLevel: LogEventLevel.Information)
                .CreateLogger();

            Log.Information("Application starting. Serilog initialized.");

            // Inicializamos todos los servicios y settings compartidos
            _logService = new LogService();
            _httpClient = new HttpClient();
            _directoriesCreator = new DirectoriesCreator(_logService);
            _requests = new Requests(_httpClient, _directoriesCreator, _logService);
            _assetDownloader = new AssetDownloader(_httpClient, _directoriesCreator, _logService);
            _appSettings = AppSettings.LoadSettings(); // Cargar aquí una sola vez
            _jsonDataService = new JsonDataService(_logService, _httpClient, _appSettings, _directoriesCreator, _requests);
            _status = new Status(_logService, _httpClient, _requests, _appSettings, _jsonDataService);

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            CleanUpOldPreviewDirectories();

            var mainWindow = new MainWindow(
                _logService,
                _httpClient,
                _directoriesCreator,
                _requests,
                _status,
                _assetDownloader,
                _appSettings,
                _jsonDataService
            );

            mainWindow.Show();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // Loguear la excepción completa a Serilog para el archivo de errores
            string errorMessage = $"Unhandled UI exception caught in App.xaml.cs (DispatcherUnhandledException).\nMessage: {e.Exception.Message}\nSource: {e.Exception.Source}\nInnerException: {e.Exception.InnerException?.Message}";
            Serilog.Log.Error(e.Exception, errorMessage);
            CustomMessageBox.ShowInfo("Error de UI", $"Un error inesperado ha ocurrido en la interfaz de usuario: {e.Exception.Message}\nConsulte el archivo de registro para más detalles.", null, CustomMessageBoxIcon.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            // Loguear la excepción completa a Serilog para el archivo de errores
            Serilog.Log.Error(ex, "Unhandled non-UI exception caught in App.xaml.cs (AppDomain_UnhandledException).");
            CustomMessageBox.ShowInfo("Error Crítico", $"Un error inesperado ha ocurrido: {ex?.Message}\nConsulte el archivo de registro para más detalles.", null, CustomMessageBoxIcon.Error);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _logService.Log("Application exiting. Flushing and closing Serilog.");
            Log.CloseAndFlush();
        }

        private void CleanUpOldPreviewDirectories()
        {
            try
            {
                // Llamamos al directorio de PreviewAssets
                string previewParentDirectory = _directoriesCreator.PreviewAssetsPath;

                if (Directory.Exists(previewParentDirectory))
                {
                    // Eliminar archivos sueltos directamente en el directorio padre
                    var files = Directory.GetFiles(previewParentDirectory);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            _logService.LogDebug($"Successfully deleted old preview file: {file}");
                        }
                        catch (IOException ex)
                        {
                            _logService.LogWarning($"Could not delete old preview file {file}, it might still be in use. Error: {ex.Message}");
                        }
                    }

                    var subDirectories = Directory.GetDirectories(previewParentDirectory);
                    foreach (var dir in subDirectories)
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                            _logService.LogDebug($"Successfully deleted old preview directory: {dir}");
                        }
                        catch (IOException ex)
                        {
                            _logService.LogWarning($"Could not delete old preview directory {dir}, it might still be in use. Error: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "An error occurred while cleaning up old preview directories.");
            }
        }

        public static async Task RunExtraction(
            LogService logService,
            HttpClient httpClient,
            DirectoriesCreator directoriesCreator,
            AssetDownloader assetDownloader,
            Requests requests,
            string newHashesDirectory,
            string oldHashesDirectory,
            bool syncHashesWithCDTB,
            bool autoCopyHashes,
            bool createBackUpOldHashes,
            bool onlyCheckDifferences,
            bool checkJsonDataUpdates)
        {
            try
            {
                await directoriesCreator.CreateAllDirectoriesAsync().ConfigureAwait(false);

                var hashesManager = new HashesManager(
                    oldHashesDirectory,
                    newHashesDirectory,
                    directoriesCreator.ResourcesPath,
                    assetDownloader.ExcludedExtensions,
                    logService
                );

                await hashesManager.CompareHashesAsync();

                var resourcesDownloader = new Resources(httpClient, directoriesCreator, logService, assetDownloader);
                await resourcesDownloader.GetResourcesFiles();
                logService.LogSuccess("Download complete.");

                var directoryCleaner = new DirectoryCleaner(directoriesCreator, logService);
                directoryCleaner.CleanEmptyDirectories();

                var HashBackUp = new HashBackUp(directoriesCreator, logService);
                await HashBackUp.HandleBackUpAsync(createBackUpOldHashes);

                var hashCopier = new HashCopier(logService, directoriesCreator);
                await hashCopier.HandleCopyAsync(autoCopyHashes);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unexpected error has occurred during RunExtraction.");
                logService.LogError($"¡ERROR crítico durante la extracción!: {ex.Message}");
            }
        }
    }
}
