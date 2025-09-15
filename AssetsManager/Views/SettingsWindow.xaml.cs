using System;
using System.Windows;
using Serilog;
using AssetsManager.Utils;
using AssetsManager.Services;
using AssetsManager.Services.Core;
using AssetsManager.Views.Dialogs;
using AssetsManager.Views.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace AssetsManager.Views
{
    public class SettingsChangedEventArgs : EventArgs
    {
        public bool WasResetToDefaults { get; set; }
    }

    public partial class SettingsWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly LogService _logService;
        private readonly AppSettings _appSettings;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private readonly GeneralSettingsView _generalSettingsView;
        private readonly HashPathsSettingsView _hashPathsSettingsView;
        private readonly AdvancedSettingsView _advancedSettingsView;
        private readonly LogsSettingsView _logsSettingsView;

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

            // Instantiate all views
            _generalSettingsView = _serviceProvider.GetRequiredService<GeneralSettingsView>();
            _hashPathsSettingsView = _serviceProvider.GetRequiredService<HashPathsSettingsView>();
            _advancedSettingsView = _serviceProvider.GetRequiredService<AdvancedSettingsView>();
            _logsSettingsView = _serviceProvider.GetRequiredService<LogsSettingsView>();

            // Apply settings to all views                        
            _generalSettingsView.ApplySettingsToUI(_appSettings);      
            _hashPathsSettingsView.ApplySettingsToUI(_appSettings);    
            _advancedSettingsView.ApplySettingsToUI(_appSettings);     
            _logsSettingsView.ApplySettingsToUI(_appSettings);         
            _logsSettingsView.SetLogService(_logService);

            SetupNavigation();
            NavigateToView(_generalSettingsView);
        }

        private void SetupNavigation()
        {
            NavGeneral.Checked += (s, e) => NavigateToView(_generalSettingsView);
            NavHashes.Checked += (s, e) => NavigateToView(_hashPathsSettingsView);
            NavAdvanced.Checked += (s, e) => NavigateToView(_advancedSettingsView);
            NavLogs.Checked += (s, e) => NavigateToView(_logsSettingsView);
        }

        private void NavigateToView(object view)
        {
            SettingsContentArea.Content = view;
        }

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            bool? result = _customMessageBoxService.ShowYesNo("Confirm Reset", "Are you sure you want to reset all settings to default values?", this);

            if (result == true)
            {
                _appSettings.ResetToDefaults();

                AppSettings.SaveSettings(_appSettings);
                _customMessageBoxService.ShowInfo("Info", "Settings have been reset to default values.", this);

                // Apply settings to all views                         
                _generalSettingsView.ApplySettingsToUI(_appSettings);  
                _hashPathsSettingsView.ApplySettingsToUI(_appSettings);
                _advancedSettingsView.ApplySettingsToUI(_appSettings); 
                _logsSettingsView.ApplySettingsToUI(_appSettings);     

                SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { WasResetToDefaults = true });
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Save settings from all views
            _generalSettingsView.SaveSettings();
            _hashPathsSettingsView.SaveSettings();
            _advancedSettingsView.SaveSettings();
            _logsSettingsView.SaveSettings();

            AppSettings.SaveSettings(_appSettings);
            _logService.LogSuccess("Settings updated.");

            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { WasResetToDefaults = false });
        }
    }
}