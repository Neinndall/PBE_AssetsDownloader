using System;
using System.Windows;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.UI.Dialogs;
using PBE_AssetsDownloader.UI.Views.Settings;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace PBE_AssetsDownloader.UI
{
    public class SettingsChangedEventArgs : EventArgs
    {
        public bool WasResetToDefaults { get; set; }
    }

    public partial class SettingsWindow : Window
    {
        private readonly LogService _logService;
        private readonly AppSettings _appSettings;
        private readonly IServiceProvider _serviceProvider;
        private readonly CustomMessageBoxService _customMessageBoxService;

        public event EventHandler<SettingsChangedEventArgs> SettingsChanged;

        public SettingsWindow(
            AppSettings appSettings,
            IServiceProvider serviceProvider,
            CustomMessageBoxService customMessageBoxService)
        {
            InitializeComponent();

            _appSettings = appSettings;
            _serviceProvider = serviceProvider;
            _customMessageBoxService = customMessageBoxService;

            // Create a local LogService for this window and assign it to our field
            var logger = _serviceProvider.GetRequiredService<ILogger>();
            _logService = new LogService(logger);

            SetupNavigation();
            NavigateToView(_serviceProvider.GetRequiredService<GeneralSettingsView>());
        }

        private void SetupNavigation()
        {
            NavGeneral.Checked += (s, e) => NavigateToView(_serviceProvider.GetRequiredService<GeneralSettingsView>());
            NavHashes.Checked += (s, e) => NavigateToView(_serviceProvider.GetRequiredService<HashPathsSettingsView>());
            NavAdvanced.Checked += (s, e) => NavigateToView(_serviceProvider.GetRequiredService<AdvancedSettingsView>());
            NavLogs.Checked += (s, e) => 
            {
                var logsView = _serviceProvider.GetRequiredService<LogsSettingsView>();
                logsView.SetLogService(_logService);
                NavigateToView(logsView);
            };
        }

        private void NavigateToView(object view)
        {
            if (view is ISettingsView settingsView)
            {
                settingsView.ApplySettingsToUI(_appSettings);
            }
            SettingsContentArea.Content = view;
        }

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            bool? result = _customMessageBoxService.ShowYesNo("Confirm Reset", "Are you sure you want to reset all settings to default values?", this, CustomMessageBoxIcon.Warning);

            if (result == true)
            {
                var defaultSettings = AppSettings.GetDefaultSettings();
                _appSettings.SyncHashesWithCDTB = defaultSettings.SyncHashesWithCDTB;
                _appSettings.CheckJsonDataUpdates = defaultSettings.CheckJsonDataUpdates;
                _appSettings.AutoCopyHashes = defaultSettings.AutoCopyHashes;
                _appSettings.CreateBackUpOldHashes = defaultSettings.CreateBackUpOldHashes;
                _appSettings.OnlyCheckDifferences = defaultSettings.OnlyCheckDifferences;
                _appSettings.NewHashesPath = defaultSettings.NewHashesPath;
                _appSettings.OldHashesPath = defaultSettings.OldHashesPath;
                _appSettings.EnableDiffHistory = defaultSettings.EnableDiffHistory;
                _appSettings.EnableBackgroundUpdates = defaultSettings.EnableBackgroundUpdates;
                _appSettings.BackgroundUpdateFrequency = defaultSettings.BackgroundUpdateFrequency;
                _appSettings.MonitoredJsonDirectories = new(defaultSettings.MonitoredJsonDirectories);
                _appSettings.MonitoredJsonFiles = new(defaultSettings.MonitoredJsonFiles);
                _appSettings.DiffHistory = new(defaultSettings.DiffHistory);

                AppSettings.SaveSettings(_appSettings);
                _customMessageBoxService.ShowInfo("Reset Successful", "Settings have been reset to default values.", this, CustomMessageBoxIcon.Info);

                if (SettingsContentArea.Content is ISettingsView currentView)
                {
                    currentView.ApplySettingsToUI(_appSettings);
                }

                SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { WasResetToDefaults = true });
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.SaveSettings(_appSettings);
            _logService.LogSuccess("Settings updated.");

            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { WasResetToDefaults = false });
        }
    }

    public interface ISettingsView
    {
        void ApplySettingsToUI(AppSettings appSettings);
    }
}
