using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using PBE_AssetsManager.Info;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Views.Dialogs;

namespace PBE_AssetsManager.Views.Comparator
{
    public partial class WadComparisonControl : UserControl
    {
        private readonly WadComparatorService _wadComparatorService;
        private readonly LogService _logService;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly AssetDownloader _assetDownloaderService;
        private readonly WadDifferenceService _wadDifferenceService;
        private readonly WadPackagingService _wadPackagingService;
        private readonly BackupManager _backupManager;
        private readonly AppSettings _appSettings;
        private string _oldPbePath;
        private string _newPbePath;

        public WadComparisonControl()
        {
            InitializeComponent();
            _wadComparatorService = App.ServiceProvider.GetRequiredService<WadComparatorService>();
            _logService = App.ServiceProvider.GetRequiredService<LogService>();
            _customMessageBoxService = App.ServiceProvider.GetRequiredService<CustomMessageBoxService>();
            _directoriesCreator = App.ServiceProvider.GetRequiredService<DirectoriesCreator>();
            _assetDownloaderService = App.ServiceProvider.GetRequiredService<AssetDownloader>();
            _wadDifferenceService = App.ServiceProvider.GetRequiredService<WadDifferenceService>();
            _wadPackagingService = App.ServiceProvider.GetRequiredService<WadPackagingService>();
            _backupManager = App.ServiceProvider.GetRequiredService<BackupManager>();
            _appSettings = App.ServiceProvider.GetRequiredService<AppSettings>();
        }

        private async void createPbeBackupButton_Click(object sender, RoutedEventArgs e)
        {
            string sourcePbePath = _appSettings.PbeDirectory;

            if (string.IsNullOrEmpty(sourcePbePath))
            {
                _customMessageBoxService.ShowWarning("Warning", "PBE directory is not configured. Please set it in Settings > Default Paths.", Window.GetWindow(this));
                return;
            }

            if (!Directory.Exists(sourcePbePath))
            {
                _customMessageBoxService.ShowError("Error", $"The configured PBE directory does not exist: {sourcePbePath}", Window.GetWindow(this));
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

        private void btnSelectOldPbeDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select Old Directory";
                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    oldPbeDirectoryTextBox.Text = folderBrowserDialog.FileName;
                    _logService.LogDebug($"Old Directory selected: {folderBrowserDialog.FileName}");
                }
            }
        }

        private void btnSelectNewPbeDirectory_Click(object sender, RoutedEventArgs e)
        { 
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select New Directory";
                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    newPbeDirectoryTextBox.Text = folderBrowserDialog.FileName;
                    _logService.LogDebug($"New Directory selected: {folderBrowserDialog.FileName}");
                }
            }
        }

        private void btnSelectOldWadFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "WAD files (*.wad, *.wad.client)|*.wad;*.wad.client|All files (*.*)|*.*",
                Title = "Select Old WAD File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                oldWadFileTextBox.Text = openFileDialog.FileName;
            }
        }

        private void btnSelectNewWadFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "WAD files (*.wad, *.wad.client)|*.wad;*.wad.client|All files (*.*)|*.*",
                Title = "Select New WAD File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                newWadFileTextBox.Text = openFileDialog.FileName;
            }
        }

        private async void compareWadButton_Click(object sender, RoutedEventArgs e)
        {
            compareWadButton.IsEnabled = false;
            try
            {
                if (wadComparatorTabControl.SelectedIndex == 0) // By Directory
                {
                    if (string.IsNullOrEmpty(oldPbeDirectoryTextBox.Text) || string.IsNullOrEmpty(newPbeDirectoryTextBox.Text))
                    {
                        _customMessageBoxService.ShowWarning("Warning", "Please select both directories.", Window.GetWindow(this));
                        compareWadButton.IsEnabled = true;
                        return;
                    }
                    _oldPbePath = oldPbeDirectoryTextBox.Text;
                    _newPbePath = newPbeDirectoryTextBox.Text;
                    await _wadComparatorService.CompareWadsAsync(_oldPbePath, _newPbePath);
                }
                else // By File
                {
                    if (string.IsNullOrEmpty(oldWadFileTextBox.Text) || string.IsNullOrEmpty(newWadFileTextBox.Text))
                    {
                        _customMessageBoxService.ShowWarning("Warning", "Please select both WAD files.", Window.GetWindow(this));
                        compareWadButton.IsEnabled = true;
                        return;
                    }
                    await _wadComparatorService.CompareSingleWadAsync(oldWadFileTextBox.Text, newWadFileTextBox.Text);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "An error occurred during comparison.");
                _customMessageBoxService.ShowError("Error", $"An error occurred during comparison: {ex.Message}", Window.GetWindow(this));
            }
            finally
            {
                compareWadButton.IsEnabled = true;
            }
        }

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "WAD Comparison JSON (*.json)|*.json",
                Title = "Load WAD Comparison Results"
            };

            if (openFileDialog.ShowDialog() != true) return;

            try
            {
                string jsonPath = openFileDialog.FileName;
                string jsonContent = File.ReadAllText(jsonPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
                var loadedResult = JsonSerializer.Deserialize<WadComparisonData>(jsonContent, options);

                if (loadedResult == null || loadedResult.Diffs == null)
                {
                    _customMessageBoxService.ShowError("Error", "Failed to load or parse the results file: Invalid format.", Window.GetWindow(this));
                    return;
                }

                string comparisonDir = Path.GetDirectoryName(jsonPath);
                string oldChunksPath = Path.Combine(comparisonDir, "wad_chunks", "old");
                string newChunksPath = Path.Combine(comparisonDir, "wad_chunks", "new");

                string oldPathToUse;
                string newPathToUse;

                if (Directory.Exists(oldChunksPath) && Directory.Exists(newChunksPath))
                {
                    oldPathToUse = oldChunksPath;
                    newPathToUse = newChunksPath;
                }
                else
                {
                    _customMessageBoxService.ShowWarning("Warning", "Could not find the 'wad_chunks' directory. Viewing differences might fail if the original PBE directories are not present.", Window.GetWindow(this));
                    oldPathToUse = loadedResult.OldPbePath;
                    newPathToUse = loadedResult.NewPbePath;
                }

                var resultWindow = new WadComparisonResultWindow(loadedResult.Diffs, _customMessageBoxService, _directoriesCreator, _assetDownloaderService, _logService, _wadDifferenceService, _wadPackagingService, oldPathToUse, newPathToUse, jsonPath);
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
