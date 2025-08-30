using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Controls;

namespace PBE_AssetsManager.Views
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly LogService _logService;
        private readonly AppSettings _appSettings;
        private readonly UpdateManager _updateManager;
        private readonly AssetDownloader _assetDownloader;
        private readonly WadComparatorService _wadComparatorService;
        private readonly ProgressUIManager _progressUIManager;
        private readonly UpdateCheckService _updateCheckService;

        private string _latestAppVersionAvailable;

        public MainWindow(
            IServiceProvider serviceProvider,
            LogService logService,
            AppSettings appSettings,
            UpdateManager updateManager,
            AssetDownloader assetDownloader,
            WadComparatorService wadComparatorService,
            DirectoriesCreator directoriesCreator,
            CustomMessageBoxService customMessageBoxService,
            WadDifferenceService wadDifferenceService,
            UpdateCheckService updateCheckService)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _logService = logService;
            _appSettings = appSettings;
            _updateManager = updateManager;
            _assetDownloader = assetDownloader;
            _wadComparatorService = wadComparatorService;
            _updateCheckService = updateCheckService;

            _progressUIManager = new ProgressUIManager(
                logService, serviceProvider, customMessageBoxService, directoriesCreator, 
                assetDownloader, wadDifferenceService,
                ProgressSummaryButton, ProgressIcon, this
            );

            _logService.SetLogOutput(LogView.richTextBoxLogs);

            _assetDownloader.DownloadStarted += _progressUIManager.OnDownloadStarted;
            _assetDownloader.DownloadProgressChanged += _progressUIManager.OnDownloadProgressChanged;
            _assetDownloader.DownloadCompleted += _progressUIManager.OnDownloadCompleted;

            _wadComparatorService.ComparisonStarted += _progressUIManager.OnComparisonStarted;
            _wadComparatorService.ComparisonProgressChanged += _progressUIManager.OnComparisonProgressChanged;
            _wadComparatorService.ComparisonCompleted += _progressUIManager.OnComparisonCompleted;

            _updateCheckService.UpdatesFound += OnUpdatesFound;

            Sidebar.NavigationRequested += OnSidebarNavigationRequested;
            LoadHomeWindow();

            if (IsAnySettingActive())
            {
                _logService.Log("Settings configured on startup.");
            }

            _updateCheckService.Start();
            _ = _updateCheckService.CheckForAllUpdatesAsync();
        }

        private void OnUpdatesFound(string message, string latestVersion)
        {
            if (!string.IsNullOrEmpty(latestVersion))
            {
                _latestAppVersionAvailable = latestVersion;
            }
            ShowNotification(true, message);
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
            _updateCheckService.Stop();
            _updateCheckService.Start();
        }
    }
}