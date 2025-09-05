using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PBE_AssetsManager.Views.Dialogs;
using PBE_AssetsManager.Views.Models;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Services.Monitor;
using PBE_AssetsManager.Utils;

namespace PBE_AssetsManager.Views.Controls.Monitor
{
    public partial class HistoryViewControl : UserControl
    {
        public AppSettings AppSettings { get; set; }
        public LogService LogService { get; set; }
        public CustomMessageBoxService CustomMessageBoxService { get; set; }
        public DiffViewService DiffViewService { get; set; }

        public HistoryViewControl()
        {
            InitializeComponent();
            this.Loaded += HistoryViewControl_Loaded;
        }

        private void HistoryViewControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppSettings != null)
            {
                DiffHistoryListView.ItemsSource = null;
                DiffHistoryListView.ItemsSource = AppSettings.DiffHistory;
            }
        }

        private async void btnViewDiff_Click(object sender, RoutedEventArgs e)
        {
            if (DiffHistoryListView.SelectedItem is JsonDiffHistoryEntry selectedEntry)
            {
                await DiffViewService.ShowFileDiffAsync(selectedEntry.OldFilePath, selectedEntry.NewFilePath, Window.GetWindow(this));
            }
            else
            {
                CustomMessageBoxService.ShowWarning("Warning", "Please select a history entry to view.", Window.GetWindow(this));
            }
        }

        private void btnRemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            if (DiffHistoryListView.SelectedItems.Count > 0)
            {
                var itemsToRemove = DiffHistoryListView.SelectedItems.Cast<JsonDiffHistoryEntry>().ToList();
                
                string message = itemsToRemove.Count == 1
                    ? $"Are you sure you want to delete the history entry for '{itemsToRemove.First().FileName}' from {itemsToRemove.First().Timestamp}? This will delete the backup files and cannot be undone."
                    : $"Are you sure you want to delete the {itemsToRemove.Count} selected history entries? This will delete their backup files and cannot be undone.";

                if (CustomMessageBoxService.ShowYesNo("Info", message, Window.GetWindow(this)) == true)
                {
                    try
                    {
                        foreach (var selectedEntry in itemsToRemove)
                        {
                            string historyDirectoryPath = Path.GetDirectoryName(selectedEntry.OldFilePath);

                            if (!string.IsNullOrEmpty(historyDirectoryPath) && Directory.Exists(historyDirectoryPath))
                            {
                                Directory.Delete(historyDirectoryPath, true);
                            }

                            AppSettings.DiffHistory.Remove(selectedEntry);
                        }

                        AppSettings.SaveSettings(AppSettings);
                        // Refresh UI by re-setting the ItemsSource
                        DiffHistoryListView.ItemsSource = null;
                        DiffHistoryListView.ItemsSource = AppSettings.DiffHistory;
                    }
                    catch (Exception ex)
                    {
                        LogService.LogError(ex, "Error deleting history entries.");
                        CustomMessageBoxService.ShowInfo("Error", "Could not delete one or more history entries. Please check the logs for details.", Window.GetWindow(this), CustomMessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                CustomMessageBoxService.ShowInfo("Info", "Please select one or more history entries to delete.", Window.GetWindow(this), CustomMessageBoxIcon.Warning);
            }
        }
    }
}