using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using PBE_AssetsDownloader.UI.Dialogs;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Services;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace PBE_AssetsDownloader.UI
{
    public partial class MainWindow : Window
    {
        private readonly LogService _logService;
        private readonly AppSettings _appSettings;
        private readonly Status _status;
        private readonly JsonDataService _jsonDataService;
        private readonly UpdateManager _updateManager;
        private readonly AssetDownloader _assetDownloader;
        private readonly IServiceProvider _serviceProvider;

        private Timer _updateTimer;
        private Storyboard _spinningIconAnimationStoryboard;
        private ProgressDetailsWindow _progressDetailsWindow;
        private string _latestAppVersionAvailable;

        public MainWindow(
            LogService logService,
            AppSettings appSettings,
            Status status,
            JsonDataService jsonDataService,
            UpdateManager updateManager,
            AssetDownloader assetDownloader,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _logService = logService;
            _appSettings = appSettings;
            _status = status;
            _jsonDataService = jsonDataService;
            _updateManager = updateManager;
            _assetDownloader = assetDownloader;
            _serviceProvider = serviceProvider;

            // LOG PARA EXCLUSIVAMENTE MAINWINDOW (EXPORT Y HOME)
            _logService.SetLogOutput(LogView.richTextBoxLogs);

            _assetDownloader.DownloadStarted += OnDownloadStarted;
            _assetDownloader.DownloadProgressChanged += OnDownloadProgressChanged;
            _assetDownloader.DownloadCompleted += OnDownloadCompleted;

            Sidebar.NavigationRequested += OnSidebarNavigationRequested;
            LoadHomeView();

            if (IsAnySettingActive())
            {
                _logService.Log("Settings configured on startup.");
            }

            SetupUpdateTimer();
            _ = CheckForAllUpdatesAsync();
        }

        private bool IsAnySettingActive()
        {
            return _appSettings.SyncHashesWithCDTB ||
                   _appSettings.AutoCopyHashes ||
                   _appSettings.CreateBackUpOldHashes ||
                   _appSettings.OnlyCheckDifferences ||
                   _appSettings.CheckJsonDataUpdates ||
                   _appSettings.EnableDiffHistory ||
                   _appSettings.EnableBackgroundUpdates;
        }

        private void SetupUpdateTimer()
        {
            if (_appSettings.EnableBackgroundUpdates)
            {
                if (_updateTimer == null)
                {
                    _updateTimer = new Timer();
                    _updateTimer.Elapsed += async (sender, e) => await CheckForAllUpdatesAsync(true);
                    _updateTimer.AutoReset = true;
                }
                _updateTimer.Interval = _appSettings.BackgroundUpdateFrequency * 60 * 1000;
                _updateTimer.Enabled = true;
                _logService.LogDebug($"Background update timer started. Frequency: {_appSettings.BackgroundUpdateFrequency} minutes.");
            }
            else if (_updateTimer != null)
            {
                _updateTimer.Enabled = false;
                _logService.Log("Background update timer stopped.");
            }
        }

        public async Task CheckForAllUpdatesAsync(bool silent = false)
        {
            bool hashesUpdated = _appSettings.SyncHashesWithCDTB && await _status.SyncHashesIfNeeds(_appSettings.SyncHashesWithCDTB, silent);
            bool jsonUpdated = _appSettings.CheckJsonDataUpdates && await _jsonDataService.CheckJsonDataUpdatesAsync(silent);
            var (appUpdateAvailable, newVersion) = await _updateManager.IsNewVersionAvailableAsync();

            if (appUpdateAvailable)
            {
                _latestAppVersionAvailable = newVersion;
            }

            if (appUpdateAvailable || jsonUpdated || (hashesUpdated && silent))
            {
                var messages = new System.Collections.Generic.List<string>();
                if (appUpdateAvailable) messages.Add($"Version {_latestAppVersionAvailable} is available!");
                if (hashesUpdated && silent) messages.Add("New hashes are available.");
                if (jsonUpdated) messages.Add("JSON files have been updated.");
                if (messages.Count > 0) { ShowNotification(true, string.Join(" | ", messages));
                }
            }
        }

        public void ShowNotification(bool show, string message = "Updates have been detected. Click to dismiss.")
        {
            Dispatcher.Invoke(() =>
            {
                UpdateNotificationIcon.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                if (show) UpdateNotificationIcon.ToolTip = message;
            });
        }

        private async void UpdateNotificationIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowNotification(false);
            if (!string.IsNullOrEmpty(_latestAppVersionAvailable))
            {
                await _updateManager.CheckForUpdatesAsync(this, true);
                _latestAppVersionAvailable = null;
            }
            e.Handled = true;
        }

        private void OnDownloadStarted(int totalFiles)
        {
            ProgressSummaryButton.Visibility = Visibility.Visible;

            if (_spinningIconAnimationStoryboard == null)
            {
                var originalStoryboard = (Storyboard)FindResource("SpinningIconAnimation");
                _spinningIconAnimationStoryboard = originalStoryboard?.Clone();
                if (_spinningIconAnimationStoryboard != null) Storyboard.SetTarget(_spinningIconAnimationStoryboard, ProgressIcon);
            }
            _spinningIconAnimationStoryboard?.Begin();

            _progressDetailsWindow = _serviceProvider.GetRequiredService<ProgressDetailsWindow>();
            _progressDetailsWindow.Owner = this;
            _progressDetailsWindow.Closed += (s, e) => _progressDetailsWindow = null;
            _progressDetailsWindow.UpdateProgress(0, totalFiles, "Initializing...", true, null);
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
        }

        private void ProgressSummaryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_progressDetailsWindow != null)
            {
                if (!_progressDetailsWindow.IsVisible) _progressDetailsWindow.Show();
                _progressDetailsWindow.Activate();
            }
            else
            {
                _logService.Log("No active download to show details for.");
            }
        }

        private void OnSidebarNavigationRequested(string viewTag)
        {
            switch (viewTag)
            {
                case "Home": LoadHomeView(); break;
                case "Export": LoadExportView(); break;
                case "Explorer": LoadExplorerView(); break;
                case "Settings": btnSettings_Click(null, null); break;
                case "Help": btnHelp_Click(null, null); break;
            }
        }

        private void LoadHomeView()
        {
            MainContentArea.Content = _serviceProvider.GetRequiredService<HomeWindow>();
        }

        private void LoadExplorerView()
        {
            MainContentArea.Content = _serviceProvider.GetRequiredService<ExplorerWindow>();
        }

        private void LoadExportView()
        {
            MainContentArea.Content = _serviceProvider.GetRequiredService<ExportWindow>();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = _serviceProvider.GetRequiredService<HelpWindow>();
            helpWindow.ShowDialog();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
            settingsWindow.SettingsChanged += OnSettingsChanged;
            settingsWindow.ShowDialog();
        }

        private void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            if (MainContentArea.Content is HomeWindow homeView)
            {
                homeView.UpdateSettings(_appSettings, e.WasResetToDefaults);
            }
            SetupUpdateTimer();
        }
    }
}