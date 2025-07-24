// PBE_AssetsDownloader/UI/SettingsWindow.xaml.cs
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows; // For Window, MessageBox
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.WindowsAPICodePack.Dialogs;
using PBE_AssetsDownloader.Info;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader.UI
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        // Declare fields for injected instances
        private readonly LogService _logService;
        private readonly HttpClient _httpClient;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly Requests _requests;
        private readonly Status _status;

        // Private instance of AppSettings to mirror MainWindow's pattern
        private AppSettings _appSettings; 
        
        // Events to notify MainWindow of changes
        public event EventHandler SettingsChanged;
        
        // Constructor now receives ALL necessary dependencies
        public SettingsWindow(
            LogService logService,
            HttpClient httpClient,
            DirectoriesCreator directoriesCreator,
            Requests requests,
            Status status,
            AppSettings appSettings)
        {
            InitializeComponent();

            // Assign injected instances to readonly fields
            _logService = logService;
            _httpClient = httpClient;
            _directoriesCreator = directoriesCreator;
            _requests = requests;
            _status = status;
            _appSettings = appSettings; // usar instancia inyectada

            // Configure log output for this window
            _logService.SetLogOutput(richTextBoxLogs, LogScrollViewer);
            ApplySettingsToUI();
        }
        
        private void ApplySettingsToUI()
        {
            checkBoxSyncHashes.IsChecked = _appSettings.SyncHashesWithCDTB;
            checkBoxCheckJsonData.IsChecked = _appSettings.CheckJsonDataUpdates; // Cargar el nuevo valor
            checkBoxAutoCopy.IsChecked = _appSettings.AutoCopyHashes;
            checkBoxCreateBackUp.IsChecked = _appSettings.CreateBackUpOldHashes;
            checkBoxOnlyCheckDifferences.IsChecked = _appSettings.OnlyCheckDifferences;
            textBoxNewHashPath.Text = _appSettings.NewHashesPath;
            textBoxOldHashPath.Text = _appSettings.OldHashesPath;
        }

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to reset all settings to default values?", "Confirm Reset",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Preservar HashesSizes
                var currentHashSizes = _appSettings.HashesSizes;

                // Obtener valores por defecto
                var defaultSettings = AppSettings.GetDefaultSettings();

                // Sobrescribir propiedades individualmente
                _appSettings.SyncHashesWithCDTB = defaultSettings.SyncHashesWithCDTB;
                _appSettings.CheckJsonDataUpdates = defaultSettings.CheckJsonDataUpdates; // Resetear el nuevo valor
                _appSettings.AutoCopyHashes = defaultSettings.AutoCopyHashes;
                _appSettings.CreateBackUpOldHashes = defaultSettings.CreateBackUpOldHashes;
                _appSettings.OnlyCheckDifferences = defaultSettings.OnlyCheckDifferences;
                _appSettings.NewHashesPath = defaultSettings.NewHashesPath;
                _appSettings.OldHashesPath = defaultSettings.OldHashesPath;
                
                // Guardar
                AppSettings.SaveSettings(_appSettings);

                // Actualizar UI
                ApplySettingsToUI();

                MessageBox.Show("Settings have been reset to default values.", "Reset Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void btnBrowseNew_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new CommonOpenFileDialog())
            {
                folderDialog.IsFolderPicker = true;
                folderDialog.Title = "Select New Hashes Folder";

                if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    _appSettings.NewHashesPath = folderDialog.FileName; // Update the _appSettings instance
                    textBoxNewHashPath.Text = _appSettings.NewHashesPath; // Update the UI
                    // No need to save here, save happens on btnSave_Click
                }
            }
        }

        private void btnBrowseOld_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new CommonOpenFileDialog())
            {
                folderDialog.IsFolderPicker = true;
                folderDialog.Title = "Select Old Hashes Folder";

                if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    _appSettings.OldHashesPath = folderDialog.FileName; // Update the _appSettings instance
                    textBoxOldHashPath.Text = _appSettings.OldHashesPath; // Update the UI
                    // No need to save here, save happens on btnSave_Click
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1. Update the _appSettings instance properties with the current UI control values
            _appSettings.SyncHashesWithCDTB = checkBoxSyncHashes.IsChecked ?? false;
            _appSettings.CheckJsonDataUpdates = checkBoxCheckJsonData.IsChecked ?? false; // Guardar el nuevo valor
            _appSettings.AutoCopyHashes = checkBoxAutoCopy.IsChecked ?? false;
            _appSettings.CreateBackUpOldHashes = checkBoxCreateBackUp.IsChecked ?? false;
            _appSettings.OnlyCheckDifferences = checkBoxOnlyCheckDifferences.IsChecked ?? false;
            // NewHashesPath and OldHashesPath were already updated in btnBrowseNew/Old_Click on _appSettings

            // 2. Save the updated settings using the centralized method
            AppSettings.SaveSettings(_appSettings);

            _logService.LogSuccess("Settings updated.");

            // Notify MainWindow that settings have changed
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task DownloadFiles()
        {
            string downloadDirectory = _directoriesCreator.GetHashesNewsDirectoryPath();

            try
            {
                // Solo pasamos el directorio, el logging se hace dentro de Requests
                await _requests.DownloadHashesFilesAsync(downloadDirectory);
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error during download: {ex.Message}");
            }
        }

    }
}