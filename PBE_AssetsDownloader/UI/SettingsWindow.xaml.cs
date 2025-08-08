using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAPICodePack.Dialogs;
using PBE_AssetsDownloader.Info;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.UI.Dialogs;
using PBE_AssetsDownloader.UI.Views.SettingsViews;

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

        public event EventHandler<SettingsChangedEventArgs> SettingsChanged;

        private readonly GeneralSettingsView _generalSettingsView;
        private readonly HashPathsSettingsView _hashPathsSettingsView;
        private readonly AdvancedSettingsView _advancedSettingsView;
        private readonly LogsSettingsView _logsSettingsView;

        public SettingsWindow(
            HttpClient httpClient,
            DirectoriesCreator directoriesCreator,
            Requests requests,
            Status status,
            AppSettings appSettings)
        {
            InitializeComponent();

            _logService = new LogService(); // Nueva instancia aislada
            _appSettings = appSettings;

            _generalSettingsView = new GeneralSettingsView();
            _generalSettingsView.ApplySettingsToUI(_appSettings);

            _hashPathsSettingsView = new HashPathsSettingsView();
            _hashPathsSettingsView.ApplySettingsToUI(_appSettings, _logService);

            _advancedSettingsView = new AdvancedSettingsView();
            _advancedSettingsView.ApplySettingsToUI(_appSettings, _logService);

            _logsSettingsView = new LogsSettingsView();
            _logsSettingsView.ApplySettingsToUI(_logService);

            NavGeneral.Checked += (s, e) => NavigateToView(_generalSettingsView);
            NavHashes.Checked += (s, e) => NavigateToView(_hashPathsSettingsView);
            NavAdvanced.Checked += (s, e) => NavigateToView(_advancedSettingsView);
            NavLogs.Checked += (s, e) => NavigateToView(_logsSettingsView);

            NavigateToView(_generalSettingsView);
        }

        private void NavigateToView(object view)
        {
            SettingsContentArea.Content = view;
        }

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            bool? result = CustomMessageBox.ShowYesNo("Confirm Reset", "Are you sure you want to reset all settings to default values?", this, CustomMessageBoxIcon.Warning); 

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
                
                _appSettings.MonitoredJsonDirectories = new List<string>(defaultSettings.MonitoredJsonDirectories);
                _appSettings.MonitoredJsonFiles = new List<string>(defaultSettings.MonitoredJsonFiles);
                _appSettings.DiffHistory = new List<JsonDiffHistoryEntry>(defaultSettings.DiffHistory);

                AppSettings.SaveSettings(_appSettings);
                CustomMessageBox.ShowInfo("Reset Successful", "Settings have been reset to default values.", this, CustomMessageBoxIcon.Info);  

                _generalSettingsView.ApplySettingsToUI(_appSettings);
                _hashPathsSettingsView.ApplySettingsToUI(_appSettings, _logService);
                _advancedSettingsView.ApplySettingsToUI(_appSettings, _logService);

                SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { WasResetToDefaults = true });
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            _generalSettingsView.SaveSettings();
            _hashPathsSettingsView.SaveSettings();
            _advancedSettingsView.SaveSettings();

            AppSettings.SaveSettings(_appSettings);
            _logService.LogSuccess("Settings updated.");

            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { WasResetToDefaults = false });
        }
    }
}