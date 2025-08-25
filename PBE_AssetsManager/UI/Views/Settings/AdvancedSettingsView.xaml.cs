using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PBE_AssetsManager.Info;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.UI.Dialogs;
using PBE_AssetsManager.Utils;

namespace PBE_AssetsManager.UI.Views.Settings
{
    public partial class AdvancedSettingsView : UserControl
    {
        private AppSettings _appSettings;
        private readonly LogService _logService;
        private readonly IServiceProvider _serviceProvider;
        private readonly CustomMessageBoxService _customMessageBoxService;

        public AdvancedSettingsView(LogService logService, IServiceProvider serviceProvider, CustomMessageBoxService customMessageBoxService)
        {
            InitializeComponent();
            _logService = logService;
            _serviceProvider = serviceProvider;
            _customMessageBoxService = customMessageBoxService;
        }

        public void ApplySettingsToUI(AppSettings appSettings)
        {
            _appSettings = appSettings;

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
            if (_appSettings == null) return;

            _appSettings.MonitoredJsonDirectories.Clear();
            _appSettings.MonitoredJsonFiles.Clear();

            if (JsonFilesListBox.ItemsSource is List<string> items)
            {
                foreach (string url in items)
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
        }
        
        private void btnAddJsonFile_Click(object sender, RoutedEventArgs e)
        {
            var inputDialog = _serviceProvider.GetRequiredService<InputDialog>();
            inputDialog.Initialize("Add JSON File URL", "Enter the URL of the JSON file or directory:", "");
            inputDialog.Owner = Window.GetWindow(this);

            if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.InputText))
            {
                var currentItems = (JsonFilesListBox.ItemsSource as List<string> ?? new List<string>());
                currentItems.Add(inputDialog.InputText);
                JsonFilesListBox.ItemsSource = null;
                JsonFilesListBox.ItemsSource = currentItems;
            }
        }

        private void btnEditJsonFile_Click(object sender, RoutedEventArgs e)
        {
            if (JsonFilesListBox.SelectedItem is string selectedUrl)
            {
                var inputDialog = _serviceProvider.GetRequiredService<InputDialog>();
                inputDialog.Initialize("Edit JSON File URL", "Edit the URL:", selectedUrl);
                inputDialog.Owner = Window.GetWindow(this);

                if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.InputText))
                {
                    if (JsonFilesListBox.ItemsSource is List<string> currentItems)
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
            else
            {
                _customMessageBoxService.ShowWarning("Warning", "Please select a URL to edit.", Window.GetWindow(this));
            }
        }

        private void btnRemoveJsonFile_Click(object sender, RoutedEventArgs e)
        {
            if (JsonFilesListBox.SelectedItem is string selectedUrl)
            {
                if (_customMessageBoxService.ShowYesNo("Confirm Removal", $"Are you sure you want to remove '{selectedUrl}'?", Window.GetWindow(this)) == true)
                {
                    if (JsonFilesListBox.ItemsSource is List<string> currentItems)
                    {
                        currentItems.Remove(selectedUrl);
                        JsonFilesListBox.ItemsSource = null;
                        JsonFilesListBox.ItemsSource = currentItems;
                    }
                }
            }
            else
            {
                _customMessageBoxService.ShowWarning("Warning", "Please select a URL to remove.", Window.GetWindow(this));
            }
        }

        private void btnViewDiff_Click(object sender, RoutedEventArgs e)
        {
            if (DiffHistoryListView.SelectedItem is JsonDiffHistoryEntry selectedEntry)
            {
                try
                {
                    var diffWindow = _serviceProvider.GetRequiredService<JsonDiffWindow>();
                    _ = diffWindow.LoadAndDisplayDiffAsync(selectedEntry.OldFilePath, selectedEntry.NewFilePath);
                    diffWindow.Show();
                }
                catch (Exception ex)
                {
                    _logService.LogError($"Error opening diff for {selectedEntry.FileName}. See application_errors.log for details.");
                    _logService.LogCritical(ex, $"AdvancedSettingsView.btnViewDiff_Click Exception for file: {selectedEntry.FileName}");
                    _customMessageBoxService.ShowError("Error", "Could not open diff view. Please check the logs for details.", Window.GetWindow(this));
                }
            }
            else
            {
                _customMessageBoxService.ShowWarning("Warning", "Please select a history entry to view.", Window.GetWindow(this));
            }
        }

        private void btnRemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            if (DiffHistoryListView.SelectedItem is JsonDiffHistoryEntry selectedEntry)
            {
                if (_customMessageBoxService.ShowYesNo("Confirm Deletion", $"Are you sure you want to delete the history entry for '{selectedEntry.FileName}' from {selectedEntry.Timestamp}? This will delete the backup files and cannot be undone.", Window.GetWindow(this)) == true)
                {
                    try
                    {
                        // Get the directory from one of the file paths
                        string historyDirectoryPath = Path.GetDirectoryName(selectedEntry.OldFilePath);

                        // Delete the physical directory
                        if (!string.IsNullOrEmpty(historyDirectoryPath) && Directory.Exists(historyDirectoryPath))
                        {
                            Directory.Delete(historyDirectoryPath, true);
                        }

                        // Remove from settings and refresh UI
                        _appSettings.DiffHistory.Remove(selectedEntry);
                        
                        // We need to re-bind the list to make the UI update correctly after removal.
                        DiffHistoryListView.ItemsSource = null;
                        DiffHistoryListView.ItemsSource = _appSettings.DiffHistory;
                    }
                    catch (Exception ex)
                    {
                        string directoryPath = Path.GetDirectoryName(selectedEntry.OldFilePath);
                        _logService.LogError($"Error deleting history for {selectedEntry.FileName}. See application_errors.log for details.");
                        _logService.LogCritical(ex, $"AdvancedSettingsView.btnDeleteSelected_Click Exception for directory: {directoryPath}");
                        _customMessageBoxService.ShowInfo("Error", "Could not delete history entry. Please check the logs for details.", Window.GetWindow(this), CustomMessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                _customMessageBoxService.ShowInfo("No Selection", "Please select a history entry to delete.", Window.GetWindow(this), CustomMessageBoxIcon.Warning);
            }
        }
    }
}