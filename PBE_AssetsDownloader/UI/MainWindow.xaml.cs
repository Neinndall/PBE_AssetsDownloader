using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using PBE_AssetsDownloader.UI.Controls;
using PBE_AssetsDownloader.Info;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Services;
using Material.Icons;
using Serilog; // Added Serilog using directive
using System.Timers;

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
    private System.Timers.Timer _updateTimer;
    private Storyboard _spinningIconAnimationStoryboard;

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
      _logService.SetLogOutput(LogView.richTextBoxLogs);

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
      
      // Conectar el evento de navegación del Sidebar
      Sidebar.NavigationRequested += OnSidebarNavigationRequested;
      LoadHomeView();

      var configLogs = new (bool enabled, string message)[]
      {
        (_appSettings.SyncHashesWithCDTB, "Sync enabled on startup."),
        (_appSettings.AutoCopyHashes, "Automatically replace old hashes enabled."),
        (_appSettings.CreateBackUpOldHashes, "Backup old hashes enabled."),
        (_appSettings.OnlyCheckDifferences, "Check only differences enabled."),
        (_appSettings.CheckJsonDataUpdates, "Check json files enabled."),
        (_appSettings.EnableDiffHistory, "Enable difference history enabled.")
      };

      foreach (var (enabled, message) in configLogs)
      {
        if (enabled) _logService.Log(message);
      }

      SetupUpdateTimer();

      // Initial check on startup
      _ = CheckForAllUpdatesAsync();

      _ = UpdateManager.CheckForUpdatesAsync(this, false);
    }

    private void SetupUpdateTimer()
    {
        if (_appSettings.EnableBackgroundUpdates)
        {
            if (_updateTimer == null)
            {
                _updateTimer = new System.Timers.Timer();
                _updateTimer.Elapsed += async (sender, e) => await CheckForAllUpdatesAsync();
                _updateTimer.AutoReset = true;
            }
            // Update interval from settings (convert minutes to milliseconds)
            _updateTimer.Interval = _appSettings.BackgroundUpdateFrequency * 60 * 1000;
            _updateTimer.Enabled = true;
            _logService.Log($"Background update timer started. Frequency: {_appSettings.BackgroundUpdateFrequency} minutes.");
        }
        else
        {
            if (_updateTimer != null)
            {
                _updateTimer.Enabled = false;
                _logService.Log("Background update timer stopped.");
            }
        }
    }

    public async Task CheckForAllUpdatesAsync()
    {
        bool hashesUpdated = false;
        if (_appSettings.SyncHashesWithCDTB)
        {
            hashesUpdated = await _status.SyncHashesIfNeeds(_appSettings.SyncHashesWithCDTB);
        }

        bool jsonUpdated = false;
        if (_appSettings.CheckJsonDataUpdates)
        {
            jsonUpdated = await _jsonDataService.CheckJsonDataUpdatesAsync();
        }

        if (hashesUpdated || jsonUpdated)
        {
            ShowNotification(true);
        }
    }

    public void ShowNotification(bool show)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateNotificationIcon.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    private void UpdateNotificationIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ShowNotification(false);
        e.Handled = true; // Consume the event to prevent it from bubbling up to the window
    }

    private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (UpdateNotificationIcon.IsVisible)
        {
            ShowNotification(false);
        }
    }

    private void OnDownloadStarted(int totalFiles)
    {
        ProgressSummaryButton.Visibility = Visibility.Visible;
        
        // Initialize and start animation
        if (_spinningIconAnimationStoryboard == null)
        {
            var originalStoryboard = (Storyboard)FindResource("SpinningIconAnimation");
            if (originalStoryboard != null)
            {
                _spinningIconAnimationStoryboard = originalStoryboard.Clone();
                Storyboard.SetTarget(_spinningIconAnimationStoryboard, ProgressIcon);
            }
        }
        _spinningIconAnimationStoryboard?.Begin();

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
        ProgressSummaryButton.Visibility = Visibility.Collapsed;
        _spinningIconAnimationStoryboard?.Stop();
        _spinningIconAnimationStoryboard = null;

        _progressDetailsWindow?.Close();
        _progressDetailsWindow = null;
        Serilog.Log.Information("Descarga de activos completada."); // Log to file only
    }

    private void ProgressSummaryButton_Click(object sender, RoutedEventArgs e)
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
      SetupUpdateTimer(); // Start or stop the timer based on the new settings
    }
  }
}
