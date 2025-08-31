using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PBE_AssetsManager.Info;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Dialogs;

namespace PBE_AssetsManager.Views.Settings
{
    public partial class AdvancedSettingsView : UserControl
    {
        private AppSettings _appSettings;
        private readonly LogService _logService;
        private readonly IServiceProvider _serviceProvider; // Keep for InputDialog, or refactor later
        private readonly CustomMessageBoxService _customMessageBoxService;
        private readonly DiffViewService _diffViewService;

        public AdvancedSettingsView(LogService logService, IServiceProvider serviceProvider, CustomMessageBoxService customMessageBoxService, DiffViewService diffViewService)
        {
            InitializeComponent();
            _logService = logService;
            _serviceProvider = serviceProvider;
            _customMessageBoxService = customMessageBoxService;
            _diffViewService = diffViewService;
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
            inputDialog.Initialize("Add URL", "Enter the URL of the file or directory:", "");
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
                inputDialog.Initialize("Edit URL", "Edit the URL:", selectedUrl);
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
                if (_customMessageBoxService.ShowYesNo("Info", $"Are you sure you want to remove '{selectedUrl}'?", Window.GetWindow(this)) == true)
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

        private async void btnViewDiff_Click(object sender, RoutedEventArgs e)
        {
            if (DiffHistoryListView.SelectedItem is JsonDiffHistoryEntry selectedEntry)
            {
                await _diffViewService.ShowFileDiffAsync(selectedEntry.OldFilePath, selectedEntry.NewFilePath, Window.GetWindow(this));
            }
            else
            {
                _customMessageBoxService.ShowWarning("Warning", "Please select a history entry to view.", Window.GetWindow(this));
            }
        }

        private void btnRemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            if (DiffHistoryListView.SelectedItems.Count > 0)
            {
                // Create a copy of the selected items to avoid issues with modifying the collection while iterating.
                var itemsToRemove = DiffHistoryListView.SelectedItems.Cast<JsonDiffHistoryEntry>().ToList();
                
                string message = itemsToRemove.Count == 1
                    ? $"Are you sure you want to delete the history entry for '{itemsToRemove.First().FileName}' from {itemsToRemove.First().Timestamp}? This will delete the backup files and cannot be undone."
                    : $"Are you sure you want to delete the {itemsToRemove.Count} selected history entries? This will delete their backup files and cannot be undone.";

                if (_customMessageBoxService.ShowYesNo("Info", message, Window.GetWindow(this)) == true)
                {
                    try
                    {
                        foreach (var selectedEntry in itemsToRemove)
                        {
                            // Get the directory from one of the file paths
                            string historyDirectoryPath = Path.GetDirectoryName(selectedEntry.OldFilePath);

                            // Delete the physical directory
                            if (!string.IsNullOrEmpty(historyDirectoryPath) && Directory.Exists(historyDirectoryPath))
                            {
                                Directory.Delete(historyDirectoryPath, true);
                            }

                            // Remove from settings
                            _appSettings.DiffHistory.Remove(selectedEntry);
                        }

                        // Refresh UI once after all deletions
                        DiffHistoryListView.ItemsSource = null;
                        DiffHistoryListView.ItemsSource = _appSettings.DiffHistory;
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError(ex, "Error deleting history entries.");
                        _customMessageBoxService.ShowInfo("Error", "Could not delete one or more history entries. Please check the logs for details.", Window.GetWindow(this), CustomMessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                _customMessageBoxService.ShowInfo("Info", "Please select one or more history entries to delete.", Window.GetWindow(this), CustomMessageBoxIcon.Warning);
            }
        }
    }
}