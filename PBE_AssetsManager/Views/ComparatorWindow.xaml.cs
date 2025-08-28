using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using PBE_AssetsManager.Info;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Views.Dialogs;
using PBE_AssetsManager.Views.Helpers;

namespace PBE_AssetsManager.Views
{
    public partial class ComparatorWindow : UserControl
    {
        private readonly WadComparatorService _wadComparatorService;
        private readonly LogService _logService;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly AssetDownloader _assetDownloaderService;
        private readonly HashResolverService _hashResolverService;
        private readonly WadDifferenceService _wadDifferenceService;
        private readonly BackupManager _backupManager;
        private string _oldPbePath;
        private string _newPbePath;

        public ComparatorWindow(WadComparatorService wadComparatorService, LogService logService, CustomMessageBoxService customMessageBoxService, DirectoriesCreator directoriesCreator, AssetDownloader assetDownloaderService, HashResolverService hashResolverService, WadDifferenceService wadDifferenceService, BackupManager backupManager)
        {
            InitializeComponent();
            _wadComparatorService = wadComparatorService;
            _logService = logService;
            _customMessageBoxService = customMessageBoxService;
            _directoriesCreator = directoriesCreator;
            _assetDownloaderService = assetDownloaderService;
            _hashResolverService = hashResolverService;
            _wadDifferenceService = wadDifferenceService;
            _backupManager = backupManager;
        }

        private async void createPbeBackupButton_Click(object sender, RoutedEventArgs e)
        {
            string sourcePbePath = @"C:\Riot Games\League of Legends (PBE)";

            if (!Directory.Exists(sourcePbePath))
            {
                _customMessageBoxService.ShowError("Error", $"PBE directory for backup does not exist: {sourcePbePath}", Window.GetWindow(this));
                return;
            }

            string destinationBackupPath = sourcePbePath + "_old";

            createPbeBackupButton.IsEnabled = false;
            try
            {
                await _backupManager.CreatePbeDirectoryBackupAsync(sourcePbePath, destinationBackupPath);
                _customMessageBoxService.ShowInfo("Info", "PBE directory backup completed successfully.", Window.GetWindow(this));
                _logService.LogSuccess("PBE directory backup completed successfully.");
            }
            catch (DirectoryNotFoundException ex)
            {
                _logService.LogError(ex, "Error creating PBE directory backup");
                _customMessageBoxService.ShowError("Error", ex.Message, Window.GetWindow(this));
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error creating PBE directory backup");
                _customMessageBoxService.ShowError("Error", $"An unexpected error occurred while creating the backup: {ex.Message}", Window.GetWindow(this));
            }
            finally
            {
                createPbeBackupButton.IsEnabled = true;
            }
        }

        private void btnSelectOriginal_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select Original JSON File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                originalFileTextBox.Text = openFileDialog.FileName;
            }
        }

        private void btnSelectNew_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select New JSON File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                newFileTextBox.Text = openFileDialog.FileName;
            }
        }

        private void btnCompare_Click(object sender, RoutedEventArgs e)
        {
            string originalPath = originalFileTextBox.Text;
            string newPath = newFileTextBox.Text;

            if (string.IsNullOrWhiteSpace(originalPath) || string.IsNullOrWhiteSpace(newPath))
            {
                _customMessageBoxService.ShowWarning("Warning", "Please select both an original and a new file.", Window.GetWindow(this));
                return;
            }

            if (!File.Exists(originalPath) || !File.Exists(newPath))
            {
                _customMessageBoxService.ShowError("Error", "One or both of the selected files do not exist.", Window.GetWindow(this));
                return;
            }

            try
            {
                string originalContent = File.ReadAllText(originalPath);
                string newContent = File.ReadAllText(newPath);

                string originalJson = JsonDiffHelper.FormatJson(originalContent);
                string newJson = JsonDiffHelper.FormatJson(newContent);

                var diffWindow = App.ServiceProvider.GetRequiredService<JsonDiffWindow>();
                _ = diffWindow.LoadAndDisplayDiffAsync(originalJson, newJson, Path.GetFileName(originalPath), Path.GetFileName(newPath));
                diffWindow.Show();
            }
            catch (IOException ex)
            {
                _customMessageBoxService.ShowError("Error", $"Error reading files: {ex.Message}", Window.GetWindow(this));
            }
        }
       
        private void btnSelectOldPbeDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select Old PBE Directory";
                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    oldPbeDirectoryTextBox.Text = folderBrowserDialog.FileName;
                    _logService.LogDebug($"Old PBE Directory selected: {folderBrowserDialog.FileName}");
                }
            }
        }

        private void btnSelectNewPbeDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select New PBE Directory";
                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    newPbeDirectoryTextBox.Text = folderBrowserDialog.FileName;
                    _logService.LogDebug($"New PBE Directory selected: {folderBrowserDialog.FileName}");
                }
            }
        }

        private async void compareWadButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(oldPbeDirectoryTextBox.Text) || string.IsNullOrEmpty(newPbeDirectoryTextBox.Text))
            {
                _customMessageBoxService.ShowWarning("Warning", "Please select both PBE directories.", Window.GetWindow(this));
                return;
            }

            _oldPbePath = oldPbeDirectoryTextBox.Text;
            _newPbePath = newPbeDirectoryTextBox.Text;

            compareWadButton.IsEnabled = false;

            try
            {
                await _wadComparatorService.CompareWadsAsync(_oldPbePath, _newPbePath);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "An error occurred during comparison.");
                _customMessageBoxService.ShowError("Error", $"An error occurred during comparison: {ex.Message}", Window.GetWindow(this));
            }
        }

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Load Comparison Results"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(openFileDialog.FileName);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter() }
                    };
                    var loadedResult = JsonSerializer.Deserialize<SerializableComparisonResult>(json, options);

                    if (loadedResult == null || loadedResult.Diffs == null)
                    {
                        _customMessageBoxService.ShowError("Error", "Failed to load or parse the results file: Invalid format.", Window.GetWindow(this));
                        return;
                    }

                    var resultWindow = new WadComparisonResultWindow(loadedResult.Diffs, _customMessageBoxService, _directoriesCreator, _assetDownloaderService, _logService, _wadDifferenceService, loadedResult.OldPbePath, loadedResult.NewPbePath);
                    resultWindow.Show();
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, "Failed to load comparison results.");
                    _customMessageBoxService.ShowError("Error", $"Failed to load or parse the results file: {ex.Message}", Window.GetWindow(this));
                }
            }
        }
    }
}