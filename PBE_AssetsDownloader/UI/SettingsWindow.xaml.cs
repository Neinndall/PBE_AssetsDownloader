// PBE_AssetsDownloader/UI/SettingsWindow.xaml.cs
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows; // For Window, MessageBox
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq; // Added for ListBox.Items.Cast<string>()
using Microsoft.WindowsAPICodePack.Dialogs;
using PBE_AssetsDownloader.Info;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.UI.Dialogs; // Added for InputDialog
using PBE_AssetsDownloader.UI.Helpers;

namespace PBE_AssetsDownloader.UI
{
    // Define custom event arguments to pass more data
    public class SettingsChangedEventArgs : EventArgs
    {
        public bool WasResetToDefaults { get; set; }
    }

    public partial class SettingsWindow : Window
    {
        private readonly LogService _logService;
        private readonly HttpClient _httpClient;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly Requests _requests;
        private readonly Status _status;

        // The single instance of AppSettings, shared across the application
        private readonly AppSettings _appSettings;

        // Use the custom event handler
        public event EventHandler<SettingsChangedEventArgs> SettingsChanged;

        public SettingsWindow(
            LogService logService,
            HttpClient httpClient,
            DirectoriesCreator directoriesCreator,
            Requests requests,
            Status status,
            AppSettings appSettings)
        {
            InitializeComponent();

            _logService = logService;
            _httpClient = httpClient;
            _directoriesCreator = directoriesCreator;
            _requests = requests;
            _status = status;
            _appSettings = appSettings; // Use the shared AppSettings instance

            _logService.SetLogOutput(richTextBoxLogs, LogScrollViewer);
            ApplySettingsToUI(); // Load current settings into UI controls
        }

        private void ApplySettingsToUI()
        {
            // Bind UI to the current _appSettings values
            checkBoxSyncHashes.IsChecked = _appSettings.SyncHashesWithCDTB;
            checkBoxCheckJsonData.IsChecked = _appSettings.CheckJsonDataUpdates;
            checkBoxAutoCopy.IsChecked = _appSettings.AutoCopyHashes;
            checkBoxCreateBackUp.IsChecked = _appSettings.CreateBackUpOldHashes;
            checkBoxOnlyCheckDifferences.IsChecked = _appSettings.OnlyCheckDifferences;
            textBoxNewHashPath.Text = _appSettings.NewHashesPath;
            textBoxOldHashPath.Text = _appSettings.OldHashesPath;
            checkBoxEnableDiffHistory.IsChecked = _appSettings.EnableDiffHistory;

            // Cargar la lista combinada de URLs de JSON
            var combinedList = new List<string>();
            if (_appSettings.MonitoredJsonDirectories != null) combinedList.AddRange(_appSettings.MonitoredJsonDirectories);
            if (_appSettings.MonitoredJsonFiles != null) combinedList.AddRange(_appSettings.MonitoredJsonFiles);

            JsonFilesListBox.ItemsSource = null; // Limpiar antes de recargar
            JsonFilesListBox.ItemsSource = combinedList;

            // Cargar el historial de diferencias
            DiffHistoryListView.ItemsSource = null;
            DiffHistoryListView.ItemsSource = _appSettings.DiffHistory;
        }

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            bool? result = CustomMessageBox.ShowYesNo("Confirm Reset", "Are you sure you want to reset all settings to default values?", this, CustomMessageBoxIcon.Warning); 

            if (result == true)
            {
                var defaultSettings = AppSettings.GetDefaultSettings();
                
                // Apply defaults directly to the shared _appSettings instance
                _appSettings.SyncHashesWithCDTB = defaultSettings.SyncHashesWithCDTB;
                _appSettings.CheckJsonDataUpdates = defaultSettings.CheckJsonDataUpdates;
                _appSettings.AutoCopyHashes = defaultSettings.AutoCopyHashes;
                _appSettings.CreateBackUpOldHashes = defaultSettings.CreateBackUpOldHashes;
                _appSettings.OnlyCheckDifferences = defaultSettings.OnlyCheckDifferences;
                _appSettings.NewHashesPath = defaultSettings.NewHashesPath;
                _appSettings.OldHashesPath = defaultSettings.OldHashesPath;
                _appSettings.EnableDiffHistory = defaultSettings.EnableDiffHistory;
                
                // Asegurarse de copiar las nuevas listas
                _appSettings.MonitoredJsonDirectories = new List<string>(defaultSettings.MonitoredJsonDirectories);
                _appSettings.MonitoredJsonFiles = new List<string>(defaultSettings.MonitoredJsonFiles);
                _appSettings.DiffHistory = new List<JsonDiffHistoryEntry>(defaultSettings.DiffHistory);

                AppSettings.SaveSettings(_appSettings);
                CustomMessageBox.ShowInfo("Reset Successful", "Settings have been reset to default values.", this, CustomMessageBoxIcon.Info);  

                // Update the UI to show the new default values
                ApplySettingsToUI();

                // Raise the event with the flag set to true
                SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { WasResetToDefaults = true });
            }
        }

        private void btnBrowseNew_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select New Hashes Folder";

                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    // Update the TextBox directly. The value will be saved on btnSave_Click.
                    textBoxNewHashPath.Text = folderBrowserDialog.FileName;
                }
            }
        }

        private void btnBrowseOld_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select Old Hashes Folder";

                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    // Update the TextBox directly. The value will be saved on btnSave_Click.
                    textBoxOldHashPath.Text = folderBrowserDialog.FileName;
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Read values directly from UI controls and apply to _appSettings
            _appSettings.SyncHashesWithCDTB = checkBoxSyncHashes.IsChecked ?? false;
            _appSettings.CheckJsonDataUpdates = checkBoxCheckJsonData.IsChecked ?? false;
            _appSettings.AutoCopyHashes = checkBoxAutoCopy.IsChecked ?? false;
            _appSettings.CreateBackUpOldHashes = checkBoxCreateBackUp.IsChecked ?? false;
            _appSettings.OnlyCheckDifferences = checkBoxOnlyCheckDifferences.IsChecked ?? false;
            _appSettings.NewHashesPath = textBoxNewHashPath.Text;
            _appSettings.OldHashesPath = textBoxOldHashPath.Text;
            _appSettings.EnableDiffHistory = checkBoxEnableDiffHistory.IsChecked ?? false;

            // Limpiar las listas existentes antes de rellenarlas
            _appSettings.MonitoredJsonDirectories.Clear();
            _appSettings.MonitoredJsonFiles.Clear();

            // Rellenar las listas desde el ListBox, clasificando por tipo de URL
            foreach (string url in JsonFilesListBox.Items.Cast<string>())
            {
                if (url.EndsWith("/"))
                {
                    _appSettings.MonitoredJsonDirectories.Add(url);
                }
                else
                {
                    _appSettings.MonitoredJsonFiles.Add(url);
                }
            }

            AppSettings.SaveSettings(_appSettings);
            _logService.LogSuccess("Settings updated.");

            // Raise the event with the flag set to false
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { WasResetToDefaults = false });
        }

        private void btnAddJsonFile_Click(object sender, RoutedEventArgs e)
        {
            var inputDialog = new InputDialog("Add JSON File URL", "Enter the URL of the JSON file or directory:", "");
            if (inputDialog.ShowDialog() == true)
            {
                if (!string.IsNullOrWhiteSpace(inputDialog.InputText))
                {
                    // Añadir directamente al ListBox, la clasificación se hará al guardar
                    var currentItems = JsonFilesListBox.ItemsSource as List<string> ?? new List<string>();
                    currentItems.Add(inputDialog.InputText);
                    JsonFilesListBox.ItemsSource = null; // Forzar la actualización del ListBox
                    JsonFilesListBox.ItemsSource = currentItems;
                }
            }
        }

        private void btnEditJsonFile_Click(object sender, RoutedEventArgs e)
        {
            if (JsonFilesListBox.SelectedItem is string selectedUrl)
            {
                var inputDialog = new InputDialog("Edit JSON File URL", "Edit the URL:", selectedUrl);
                if (inputDialog.ShowDialog() == true)
                {
                    if (!string.IsNullOrWhiteSpace(inputDialog.InputText))
                    {
                        var currentItems = JsonFilesListBox.ItemsSource as List<string>;
                        if (currentItems != null)
                        {
                            int index = currentItems.IndexOf(selectedUrl);
                            if (index != -1)
                            {
                                currentItems[index] = inputDialog.InputText;
                                JsonFilesListBox.ItemsSource = null; // Forzar la actualización del ListBox
                                JsonFilesListBox.ItemsSource = currentItems;
                            }
                        }
                    }
                }
            }
            else
            {
                CustomMessageBox.ShowInfo("No Selection", "Please select a URL to edit.", this, CustomMessageBoxIcon.Warning); 
            }
        }

        private void btnRemoveJsonFile_Click(object sender, RoutedEventArgs e)
        {
            if (JsonFilesListBox.SelectedItem is string selectedUrl)
            {
                bool? result = CustomMessageBox.ShowYesNo("Confirm Removal", $"Are you sure you want to remove '{selectedUrl}'?", this, CustomMessageBoxIcon.Warning);
                if (result == true)
                {
                    var currentItems = JsonFilesListBox.ItemsSource as List<string>;
                    if (currentItems != null)
                    {
                        currentItems.Remove(selectedUrl);
                        JsonFilesListBox.ItemsSource = null; // Forzar la actualización del ListBox
                        JsonFilesListBox.ItemsSource = currentItems;
                    }
                }
            }
            else
            {
                CustomMessageBox.ShowInfo("No Selection", "Please select a URL to remove.", this, CustomMessageBoxIcon.Warning);
            }
        }

        private void btnViewDiff_Click(object sender, RoutedEventArgs e)
        {
            if (DiffHistoryListView.SelectedItem is JsonDiffHistoryEntry selectedEntry)
            {
                try
                {
                    string oldContent = File.Exists(selectedEntry.OldFilePath) ? File.ReadAllText(selectedEntry.OldFilePath) : "";
                    string newContent = File.Exists(selectedEntry.NewFilePath) ? File.ReadAllText(selectedEntry.NewFilePath) : "";
                    var diffWindow = new JsonDiffWindow(oldContent, newContent);
                    diffWindow.Show();
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, $"Error opening diff for {selectedEntry.FileName}");
                    CustomMessageBox.ShowInfo("Error", $"Could not open diff view. Please check the logs for details.", this, CustomMessageBoxIcon.Error);
                }
            }
            else
            {
                CustomMessageBox.ShowInfo("No Selection", "Please select a history entry to view.", this, CustomMessageBoxIcon.Warning);
            }
        }

        private void btnClearHistory_Click(object sender, RoutedEventArgs e)
        {
            bool? result = CustomMessageBox.ShowYesNo("Confirm Clear", "Are you sure you want to clear the entire difference history? This action cannot be undone.", this, CustomMessageBoxIcon.Warning);
            if (result == true)
            {
                _appSettings.DiffHistory.Clear();
                AppSettings.SaveSettings(_appSettings);
                ApplySettingsToUI(); // Refresh the view
                _logService.LogSuccess("Difference history cleared.");
            }
        }
    }
}
