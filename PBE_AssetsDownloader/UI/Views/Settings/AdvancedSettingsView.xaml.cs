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
using Microsoft.Extensions.DependencyInjection;

namespace PBE_AssetsDownloader.UI.Views.Settings
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
                _customMessageBoxService.ShowInfo("No Selection", "Please select a URL to edit.", Window.GetWindow(this), CustomMessageBoxIcon.Warning);
            }
        }

        private void btnRemoveJsonFile_Click(object sender, RoutedEventArgs e)
        {
            if (JsonFilesListBox.SelectedItem is string selectedUrl)
            {
                if (_customMessageBoxService.ShowYesNo("Confirm Removal", $"Are you sure you want to remove '{selectedUrl}'?", Window.GetWindow(this), CustomMessageBoxIcon.Warning) == true)
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
                _customMessageBoxService.ShowInfo("No Selection", "Please select a URL to remove.", Window.GetWindow(this), CustomMessageBoxIcon.Warning);
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

                    var diffWindow = _serviceProvider.GetRequiredService<JsonDiffWindow>();
                    diffWindow.LoadDiff(oldContent, newContent);
                    diffWindow.Show();
                }
                catch (Exception ex)
                {
                    _logService.LogError($"Error opening diff for {selectedEntry.FileName}. See application_errors.log for details.");
                    _logService.LogCritical(ex, $"AdvancedSettingsView.btnViewDiff_Click Exception for file: {selectedEntry.FileName}");
                    _customMessageBoxService.ShowInfo("Error", $"Could not open diff view. Please check the logs for details.", Window.GetWindow(this), CustomMessageBoxIcon.Error);
                }
            }
            else
            {
                _customMessageBoxService.ShowInfo("No Selection", "Please select a history entry to view.", Window.GetWindow(this), CustomMessageBoxIcon.Warning);
            }
        }

        private void btnClearHistory_Click(object sender, RoutedEventArgs e)
        {
            if (_customMessageBoxService.ShowYesNo("Confirm Clear", "Are you sure you want to clear the entire difference history? This action cannot be undone.", Window.GetWindow(this), CustomMessageBoxIcon.Warning) == true)
            {
                _appSettings.DiffHistory.Clear();
                ApplySettingsToUI(_appSettings);
            }
        }
    }
}