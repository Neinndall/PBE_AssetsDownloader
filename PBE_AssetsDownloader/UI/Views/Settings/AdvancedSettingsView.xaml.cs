using PBE_AssetsDownloader.Info;
using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.UI.Dialogs;
using PBE_AssetsDownloader.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsDownloader.UI.Views.Settings
{
    public partial class AdvancedSettingsView : UserControl
    {
        private AppSettings _appSettings;
        private LogService _logService;

        public AdvancedSettingsView() 
        {
            InitializeComponent();
        }

        public void ApplySettingsToUI(AppSettings appSettings, LogService logService)
        {
            _appSettings = appSettings;
            _logService = logService;

            var combinedList = new List<string>();
            if (_appSettings.MonitoredJsonDirectories != null) combinedList.AddRange(_appSettings.MonitoredJsonDirectories);
            if (_appSettings.MonitoredJsonFiles != null) combinedList.AddRange(_appSettings.MonitoredJsonFiles);

            JsonFilesListBox.ItemsSource = null;
            JsonFilesListBox.ItemsSource = combinedList;

            DiffHistoryListView.ItemsSource = null;
            DiffHistoryListView.ItemsSource = _appSettings.DiffHistory;
        }

        public void SaveSettings()
        {
            _appSettings.MonitoredJsonDirectories.Clear();
            _appSettings.MonitoredJsonFiles.Clear();

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
        }

        private void btnAddJsonFile_Click(object sender, RoutedEventArgs e)
        {
            var inputDialog = new InputDialog("Add JSON File URL", "Enter the URL of the JSON file or directory:", "");
            if (inputDialog.ShowDialog() == true)
            {
                if (!string.IsNullOrWhiteSpace(inputDialog.InputText))
                {
                    var currentItems = JsonFilesListBox.ItemsSource as List<string> ?? new List<string>();
                    currentItems.Add(inputDialog.InputText);
                    JsonFilesListBox.ItemsSource = null;
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
                                JsonFilesListBox.ItemsSource = null;
                                JsonFilesListBox.ItemsSource = currentItems;
                            }
                        }
                    }
                }
            }
            else
            {
                CustomMessageBox.ShowInfo("No Selection", "Please select a URL to edit.", Window.GetWindow(this), CustomMessageBoxIcon.Warning); 
            }
        }

        private void btnRemoveJsonFile_Click(object sender, RoutedEventArgs e)
        {
            if (JsonFilesListBox.SelectedItem is string selectedUrl)
            {
                bool? result = CustomMessageBox.ShowYesNo("Confirm Removal", $"Are you sure you want to remove '{selectedUrl}'?", Window.GetWindow(this), CustomMessageBoxIcon.Warning);
                if (result == true)
                {
                    var currentItems = JsonFilesListBox.ItemsSource as List<string>;
                    if (currentItems != null)
                    {
                        currentItems.Remove(selectedUrl);
                        JsonFilesListBox.ItemsSource = null;
                        JsonFilesListBox.ItemsSource = currentItems;
                    }
                }
            }
            else
            {
                CustomMessageBox.ShowInfo("No Selection", "Please select a URL to remove.", Window.GetWindow(this), CustomMessageBoxIcon.Warning);
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
                    CustomMessageBox.ShowInfo("Error", $"Could not open diff view. Please check the logs for details.", Window.GetWindow(this), CustomMessageBoxIcon.Error);
                }
            }
            else
            {
                CustomMessageBox.ShowInfo("No Selection", "Please select a history entry to view.", Window.GetWindow(this), CustomMessageBoxIcon.Warning);
            }
        }

        private void btnClearHistory_Click(object sender, RoutedEventArgs e)
        {
            bool? result = CustomMessageBox.ShowYesNo("Confirm Clear", "Are you sure you want to clear the entire difference history? This action cannot be undone.", Window.GetWindow(this), CustomMessageBoxIcon.Warning);
            if (result == true)
            {
                _appSettings.DiffHistory.Clear();
                ApplySettingsToUI(_appSettings, _logService);
            }
        }
    }
}