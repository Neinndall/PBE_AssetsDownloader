using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using PBE_AssetsManager.Info;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Dialogs;
using PBE_AssetsManager.Views.Controls;

namespace PBE_AssetsManager.Views
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly LogService _logService;
        private readonly AppSettings _appSettings;
        private readonly Status _status;
        private readonly JsonDataService _jsonDataService;
        private readonly UpdateManager _updateManager;
        private readonly AssetDownloader _assetDownloader;
        private readonly WadComparatorService _wadComparatorService;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly CustomMessageBoxService _customMessageBoxService;

        private Timer _updateTimer;
        private Storyboard _spinningIconAnimationStoryboard;
        private ProgressDetailsWindow _progressDetailsWindow;
        private string _latestAppVersionAvailable;
        private int _totalFiles;

        public MainWindow(
            IServiceProvider serviceProvider,
            LogService logService,
            AppSettings appSettings,
            Status status,
            JsonDataService jsonDataService,
            UpdateManager updateManager,
            AssetDownloader assetDownloader,
            WadComparatorService wadComparatorService,
            DirectoriesCreator directoriesCreator,
            CustomMessageBoxService customMessageBoxService)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _logService = logService;
            _appSettings = appSettings;
            _status = status;
            _jsonDataService = jsonDataService;
            _updateManager = updateManager;
            _assetDownloader = assetDownloader;
            _wadComparatorService = wadComparatorService;
            _directoriesCreator = directoriesCreator;
            _customMessageBoxService = customMessageBoxService;

            _logService.SetLogOutput(LogView.richTextBoxLogs);

            _assetDownloader.DownloadStarted += OnDownloadStarted;
            _assetDownloader.DownloadProgressChanged += OnDownloadProgressChanged;
            _assetDownloader.DownloadCompleted += OnDownloadCompleted;

            _wadComparatorService.ComparisonStarted += OnComparisonStarted;
            _wadComparatorService.ComparisonProgressChanged += OnComparisonProgressChanged;
            _wadComparatorService.ComparisonCompleted += OnComparisonCompleted;

            Sidebar.NavigationRequested += OnSidebarNavigationRequested;
            LoadHomeWindow(); // Corrected initial call

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
                   _appSettings.SaveDiffHistory ||
                   _appSettings.BackgroundUpdates;
        }

        private void SetupUpdateTimer()
        {
            if (_appSettings.BackgroundUpdates)
            {
                if (_updateTimer == null)
                {
                    _updateTimer = new Timer();
                    _updateTimer.Elapsed += async (sender, e) => await CheckForAllUpdatesAsync(true);
                    _updateTimer.AutoReset = true;
                }
                _updateTimer.Interval = _appSettings.UpdateCheckFrequency * 60 * 1000;
                _updateTimer.Enabled = true;
                _logService.LogDebug($"Background update timer started. Frequency: {_appSettings.UpdateCheckFrequency} minutes.");
            }
            else if (_updateTimer != null)
            {
                _updateTimer.Enabled = false;
                _logService.LogDebug("Background update timer stopped.");
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
            Dispatcher.Invoke(() => 
            {
                _totalFiles = totalFiles;
                ProgressSummaryButton.Visibility = Visibility.Visible;
                ProgressSummaryButton.ToolTip = "Click to see download details";

                if (_spinningIconAnimationStoryboard == null)
                {
                    var originalStoryboard = (Storyboard)FindResource("SpinningIconAnimation");
                    _spinningIconAnimationStoryboard = originalStoryboard?.Clone();
                    if (_spinningIconAnimationStoryboard != null) Storyboard.SetTarget(_spinningIconAnimationStoryboard, ProgressIcon);
                }
                _spinningIconAnimationStoryboard?.Begin();

                _progressDetailsWindow = new ProgressDetailsWindow(_logService, "Download Details");
                _progressDetailsWindow.Owner = this;
                _progressDetailsWindow.HeaderIconKind = "Download";
                _progressDetailsWindow.HeaderText = "Download Details";
                _progressDetailsWindow.Closed += (s, e) => _progressDetailsWindow = null;
                _progressDetailsWindow.UpdateProgress(0, totalFiles, "Initializing...", true, null);
            });
        }

        private void OnDownloadProgressChanged(int completedFiles, int totalFiles, string currentFileName, bool isSuccess, string errorMessage)
        {
            Dispatcher.Invoke(() => 
            {
                _progressDetailsWindow?.UpdateProgress(completedFiles, totalFiles, currentFileName, isSuccess, errorMessage);
            });
        }

        private void OnDownloadCompleted()
        {
            Dispatcher.Invoke(() => 
            {
                ProgressSummaryButton.Visibility = Visibility.Collapsed;
                _spinningIconAnimationStoryboard?.Stop();
                _spinningIconAnimationStoryboard = null;
                _progressDetailsWindow?.Close();
            });
        }

        private void OnComparisonStarted(int totalFiles)
        {
            Dispatcher.Invoke(() =>
            {
                _totalFiles = totalFiles;
                ProgressSummaryButton.Visibility = Visibility.Visible;
                ProgressSummaryButton.ToolTip = "Click to see comparison details";

                if (_spinningIconAnimationStoryboard == null)
                {
                    var originalStoryboard = (Storyboard)FindResource("SpinningIconAnimation");
                    _spinningIconAnimationStoryboard = originalStoryboard?.Clone();
                    if (_spinningIconAnimationStoryboard != null) Storyboard.SetTarget(_spinningIconAnimationStoryboard, ProgressIcon);
                }
                _spinningIconAnimationStoryboard?.Begin();

                _progressDetailsWindow = new ProgressDetailsWindow(_logService, "Comparison Details");
                _progressDetailsWindow.Owner = this;
                _progressDetailsWindow.OperationVerb = "Comparing";
                _progressDetailsWindow.HeaderIconKind = "Compare";
                _progressDetailsWindow.HeaderText = "Comparison Details";
                _progressDetailsWindow.Closed += (s, e) => _progressDetailsWindow = null;
                _progressDetailsWindow.UpdateProgress(0, totalFiles, "Comparison starting...", true, null);
            });
        }

        private void OnComparisonProgressChanged(int completedFiles, string currentFile, bool isSuccess, string errorMessage)
        {
            Dispatcher.Invoke(() =>
            {
                _progressDetailsWindow?.UpdateProgress(completedFiles, _totalFiles, currentFile, isSuccess, errorMessage);
            });
        }

        private void OnComparisonCompleted(List<ChunkDiff> allDiffs, string oldPbePath, string newPbePath)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressSummaryButton.Visibility = Visibility.Collapsed;
                _spinningIconAnimationStoryboard?.Stop();
                _spinningIconAnimationStoryboard = null;
                _progressDetailsWindow?.Close();

                                if (allDiffs != null)
                {
                    var resultWindow = new WadComparisonResultWindow(allDiffs, _customMessageBoxService, _directoriesCreator, _assetDownloader, _logService, oldPbePath, newPbePath);
                    resultWindow.Owner = this;
                    resultWindow.Show();
                }
            });
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
                _logService.Log("No active process to show details for.");
            }
        }

        private void OnSidebarNavigationRequested(string viewTag)
        {
            switch (viewTag)
            {
                case "Home": LoadHomeWindow(); break;
                case "Export": LoadExportWindow(); break;
                case "Explorer": LoadExplorerWindow(); break;
                case "Comparator": LoadComparatorWindow(); break;
                case "Settings": btnSettings_Click(null, null); break;
                case "Help": btnHelp_Click(null, null); break;
            }
        }

        private void LoadHomeWindow()
        {
            MainContentArea.Content = _serviceProvider.GetRequiredService<HomeWindow>();
        }

        private void LoadExplorerWindow()
        {
            MainContentArea.Content = _serviceProvider.GetRequiredService<ExplorerWindow>();
        }

        private void LoadExportWindow()
        {
            MainContentArea.Content = _serviceProvider.GetRequiredService<ExportWindow>();
        }

        private void LoadComparatorWindow()
        {
            MainContentArea.Content = _serviceProvider.GetRequiredService<ComparatorWindow>();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = _serviceProvider.GetRequiredService<HelpWindow>();
            helpWindow.ShowDialog();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
            settingsWindow.Owner = this;
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
