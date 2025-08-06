using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using System.Net.Http;
using System.Reflection;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;

using PBE_AssetsDownloader.UI.Controls;
using PBE_AssetsDownloader.Info;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Services;
using Material.Icons;
using Serilog; // Added Serilog using directive

namespace PBE_AssetsDownloader.UI
{
  public partial class MainWindow : Window
  {
    private readonly LogService _logService;
    private readonly HttpClient _httpClient;
    private readonly DirectoriesCreator _directoriesCreator;
    private readonly Requests _requests;
    private readonly Status _status;
    private readonly AssetDownloader _assetDownloader;
    private readonly AppSettings _appSettings;
    private readonly JsonDataService _jsonDataService;

    private HomeWindow _homeViewInstance;
    private ExportWindow _exportViewInstance;

    private ProgressDetailsWindow _progressDetailsWindow;
    
    public MainWindow(
        LogService logService,
        HttpClient httpClient,
        DirectoriesCreator directoriesCreator,
        Requests requests,
        Status status,
        AssetDownloader assetDownloader,
        AppSettings appSettings,
        JsonDataService jsonDataService)

    {
      InitializeComponent();

      _logService = logService;

      // LOG PARA EXCLUSIVAMENTE MAINWINDOW (EXPORT Y HOME)
      _logService.SetLogOutput(LogView.LogRichTextBox, LogView.LogScrollViewerControl);

      _httpClient = httpClient;
      _directoriesCreator = directoriesCreator;
      _requests = requests;
      _status = status; // Asignamos la instancia de Status que ya recibimos
      _assetDownloader = assetDownloader;
      _appSettings = appSettings; // Asignamos la instancia inyectada
      _jsonDataService = jsonDataService; // Asignamos la instancia inyectada

      _homeViewInstance = new HomeWindow(
          _logService,
          _httpClient,
          _directoriesCreator,
          _requests,
          _status,
          _assetDownloader,
          _appSettings
      );

      _exportViewInstance = new ExportWindow(
          _logService,
          _httpClient,
          _directoriesCreator,
          _assetDownloader
      );

      // Conectar eventos de progreso del AssetDownloader
      _assetDownloader.DownloadStarted += OnDownloadStarted;
      _assetDownloader.DownloadProgressChanged += OnDownloadProgressChanged;
      _assetDownloader.DownloadCompleted += OnDownloadCompleted;
      
      // Conectar evento de solicitud de detalles de progreso del LogView
      LogView.ProgressDetailsRequested += OnProgressDetailsRequested;
      
      // Conectar el evento de navegación del Sidebar
      Sidebar.NavigationRequested += OnSidebarNavigationRequested;
      LoadHomeView();

      var configLogs = new (bool enabled, string message)[]
      {
        (_appSettings.SyncHashesWithCDTB, "Sync enabled on startup."),
        (_appSettings.AutoCopyHashes, "Automatically replace old hashes enabled."),
        (_appSettings.CreateBackUpOldHashes, "Backup old hashes enabled."),
        (_appSettings.OnlyCheckDifferences, "Check only differences enabled."),
        (_appSettings.CheckJsonDataUpdates, "Check json files enabled.")
      };

      foreach (var (enabled, message) in configLogs)
      {
        if (enabled) _logService.Log(message);
      }

      // Comprobar actualizaciones de hashes con el servidor de CDTB
      if (_appSettings.SyncHashesWithCDTB)
      {
        _ = _status.SyncHashesIfNeeds(_appSettings.SyncHashesWithCDTB);
      }

      // Comprobar actualizaciones de datos JSON si está habilitado
      if (_appSettings.CheckJsonDataUpdates)
      {
        _ = _jsonDataService.CheckJsonDataUpdatesAsync();
      }

      _ = UpdateManager.CheckForUpdatesAsync(this, false);
    }


    private void OnDownloadStarted(int totalFiles)
    {
        LogView.IsProgressVisible = true;
        LogView.ProgressIconKind = MaterialIconKind.Loading;
        
        // Initialize ProgressDetailsWindow here
        _progressDetailsWindow = new ProgressDetailsWindow(_logService);
        _progressDetailsWindow.Owner = this;
        _progressDetailsWindow.Closed += ProgressDetailsWindow_Closed; // Subscribe to Closed event
        _progressDetailsWindow.UpdateProgress(0, totalFiles, "Iniciando...", true, null); // Initialize with 0 completed
    }

    private void ProgressDetailsWindow_Closed(object sender, EventArgs e)
    {
        // Hide the window instead of setting the reference to null
        _progressDetailsWindow?.Hide();
    }

    private void OnDownloadProgressChanged(int completedFiles, int totalFiles, string currentFileName, bool isSuccess, string errorMessage)
    {
        _progressDetailsWindow?.UpdateProgress(completedFiles, totalFiles, currentFileName, isSuccess, errorMessage);
    }

    private void OnDownloadCompleted()
    {
        LogView.IsProgressVisible = false;
        _progressDetailsWindow?.Close();
        _progressDetailsWindow = null;
        Serilog.Log.Information("Descarga de activos completada."); // Log to file only
    }

    private void OnProgressDetailsRequested()
    {
        if (_progressDetailsWindow != null && !_progressDetailsWindow.IsVisible)
        {
            _progressDetailsWindow.Show();
        }
        else if (_progressDetailsWindow == null)
        {
            // If download completed and window was closed, but user clicks again
            _logService.Log("No hay una descarga activa para mostrar detalles.");
        }
    }
    
    // Método que maneja la navegación desde el Sidebar
    private void OnSidebarNavigationRequested(string viewTag)
    {
      switch (viewTag)
      {
        case "Home":
          LoadHomeView();
          break;
        case "Export":
          LoadExportView();
          break;
        case "Settings":
          btnSettings_Click(null, null);
          break;
        case "Help":
          btnHelp_Click(null, null);
          break;
        default:
          break;
      }
    }

    // Por si tengo otros botones que usen este método
    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
      // El 'sender' del evento que nos llega es el botón original que se pulsó.
      var button = e.OriginalSource as Button;
      if (button == null) return;

      string viewTag = button.Tag as string;
      OnSidebarNavigationRequested(viewTag);
    }

    private void LoadHomeView()
    {
      MainContentArea.Content = _homeViewInstance;
    }

    private void LoadExportView()
    {
      MainContentArea.Content = _exportViewInstance;
    }

    private void btnHelp_Click(object sender, RoutedEventArgs e)
    {
      var helpWindow = new HelpWindow(_logService);
      helpWindow.ShowDialog();
    }

    private void btnSettings_Click(object sender, RoutedEventArgs e)
    {
      var settingsWindow = new SettingsWindow(
          new LogService(), // Añadimos nuevo sistema de LogService para que los logs de Settings no se registren en el MainLog
          _httpClient,
          _directoriesCreator,
          _requests,
          _status,
          _appSettings
      );

      settingsWindow.SettingsChanged += OnSettingsChanged;
      settingsWindow.ShowDialog();
    }

    // Metodo para actualizar en Home las rutas de hashes predeterminadas
    private void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
    {
      _homeViewInstance?.UpdateSettings(_appSettings, e.WasResetToDefaults);
    }
  }
}
