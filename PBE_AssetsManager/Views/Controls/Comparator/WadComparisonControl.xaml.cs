using PBE_AssetsManager.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using PBE_AssetsManager.Views.Models;
using PBE_AssetsManager.Services.Comparator;
using PBE_AssetsManager.Services.Downloads;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Services.Monitor;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Services.Hashes;

namespace PBE_AssetsManager.Views.Controls.Comparator
{
    public class LoadWadComparisonEventArgs : EventArgs
    {
        public List<SerializableChunkDiff> Diffs { get; }
        public string OldPath { get; }
        public string NewPath { get; }
        public string JsonPath { get; }

        public LoadWadComparisonEventArgs(List<SerializableChunkDiff> diffs, string oldPath, string newPath, string jsonPath)
        {
            Diffs = diffs;
            OldPath = oldPath;
            NewPath = newPath;
            JsonPath = jsonPath;
        }
    }

    public partial class WadComparisonControl : UserControl
    {
        public WadComparatorService WadComparatorService { get; set; }
        public LogService LogService { get; set; }
        public CustomMessageBoxService CustomMessageBoxService { get; set; }
        public DirectoriesCreator DirectoriesCreator { get; set; }
        public AssetDownloader AssetDownloaderService { get; set; }
        public WadDifferenceService WadDifferenceService { get; set; }
        public WadPackagingService WadPackagingService { get; set; }
        public BackupManager BackupManager { get; set; }
        public AppSettings AppSettings { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
        public DiffViewService DiffViewService { get; set; }
        public HashResolverService HashResolverService { get; set; }

        public event EventHandler<LoadWadComparisonEventArgs> LoadWadComparisonRequested;

        private string _oldLolPath;
        private string _newLolPath;

        public WadComparisonControl()
        {
            InitializeComponent();
            Loaded += WadComparisonControl_Loaded;
            Unloaded += WadComparisonControl_Unloaded;
        }

        private void WadComparisonControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (WadComparatorService != null)
            {
                WadComparatorService.ComparisonCompleted += WadComparatorService_ComparisonCompleted;
            }
        }

        private void WadComparisonControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (WadComparatorService != null)
            {
                WadComparatorService.ComparisonCompleted -= WadComparatorService_ComparisonCompleted;
            }
        }

        private void WadComparatorService_ComparisonCompleted(List<ChunkDiff> allDiffs, string oldLolPath, string newLolPath)
        {
            Dispatcher.Invoke(() =>
            {
                if (allDiffs != null)
                {
                    var serializableDiffs = allDiffs.Select(d => new SerializableChunkDiff
                    {
                        Type = d.Type,
                        OldPath = d.OldPath,
                        NewPath = d.NewPath,
                        SourceWadFile = d.SourceWadFile,
                        OldPathHash = d.OldChunk.PathHash,
                        NewPathHash = d.NewChunk.PathHash,
                        OldUncompressedSize = (d.Type == ChunkDiffType.New) ? (ulong?)null : (ulong)d.OldChunk.UncompressedSize,
                        NewUncompressedSize = (d.Type == ChunkDiffType.Removed) ? (ulong?)null : (ulong)d.NewChunk.UncompressedSize,
                        OldCompressionType = (d.Type == ChunkDiffType.New) ? null : d.OldChunk.Compression,
                        NewCompressionType = (d.Type == ChunkDiffType.Removed) ? null : d.NewChunk.Compression
                    }).ToList();

                    var resultWindow = new WadComparisonResultWindow(serializableDiffs, ServiceProvider, CustomMessageBoxService, DirectoriesCreator, AssetDownloaderService, LogService, WadDifferenceService, WadPackagingService, DiffViewService, HashResolverService, oldLolPath, newLolPath);
                    resultWindow.Owner = Window.GetWindow(this);
                    resultWindow.Show();
                }
            });
        }

        private async void createLolBackupButton_Click(object sender, RoutedEventArgs e)
        {
            string sourceLolPath = AppSettings.LolDirectory;

            if (string.IsNullOrEmpty(sourceLolPath))
            {
                CustomMessageBoxService.ShowWarning("Warning", "LoL directory is not configured. Please set it in Settings > Default Paths.", Window.GetWindow(this));
                return;
            }

            if (!Directory.Exists(sourceLolPath))
            {
                CustomMessageBoxService.ShowError("Error", $"The configured LoL directory does not exist: {sourceLolPath}", Window.GetWindow(this));
                return;
            }

            string destinationBackupPath = sourceLolPath + "_old";

            createLolBackupButton.IsEnabled = false;
            try
            {
                await BackupManager.CreateLolDirectoryBackupAsync(sourceLolPath, destinationBackupPath);
                CustomMessageBoxService.ShowInfo("Info", "LoL directory backup completed successfully.", Window.GetWindow(this));
                LogService.LogSuccess("LoL directory backup completed successfully.");
            }
            catch (DirectoryNotFoundException ex)
            {
                LogService.LogError(ex, "Error creating LoL directory backup");
                CustomMessageBoxService.ShowError("Error", ex.Message, Window.GetWindow(this));
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Error creating LoL directory backup");
                CustomMessageBoxService.ShowError("Error", $"An unexpected error occurred while creating the backup: {ex.Message}", Window.GetWindow(this));
            }
            finally
            {
                createLolBackupButton.IsEnabled = true;
            }
        }

        private void btnSelectOldLolDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select Old Directory";
                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    oldLolDirectoryTextBox.Text = folderBrowserDialog.FileName;
                    LogService.LogDebug($"Old Directory selected: {folderBrowserDialog.FileName}");
                }
            }
        }

        private void btnSelectNewLolDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select New Directory";
                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    newLolDirectoryTextBox.Text = folderBrowserDialog.FileName;
                    LogService.LogDebug($"New Directory selected: {folderBrowserDialog.FileName}");
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
                    if (string.IsNullOrEmpty(oldLolDirectoryTextBox.Text) || string.IsNullOrEmpty(newLolDirectoryTextBox.Text))
                    {
                        CustomMessageBoxService.ShowWarning("Warning", "Please select both directories.", Window.GetWindow(this));
                        compareWadButton.IsEnabled = true;
                        return;
                    }
                    _oldLolPath = oldLolDirectoryTextBox.Text;
                    _newLolPath = newLolDirectoryTextBox.Text;
                    await WadComparatorService.CompareWadsAsync(_oldLolPath, _newLolPath);
                }
                else // By File
                {
                    if (string.IsNullOrEmpty(oldWadFileTextBox.Text) || string.IsNullOrEmpty(newWadFileTextBox.Text))
                    {
                        CustomMessageBoxService.ShowWarning("Warning", "Please select both WAD files.", Window.GetWindow(this));
                        compareWadButton.IsEnabled = true;
                        return;
                    }
                    await WadComparatorService.CompareSingleWadAsync(oldWadFileTextBox.Text, newWadFileTextBox.Text);
                }
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "An error occurred during comparison.");
                CustomMessageBoxService.ShowError("Error", $"An error occurred during comparison: {ex.Message}", Window.GetWindow(this));
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
                    CustomMessageBoxService.ShowError("Error", "Failed to load or parse the results file: Invalid format.", Window.GetWindow(this));
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
                    CustomMessageBoxService.ShowWarning("Warning", "Could not find the 'wad_chunks' directory. Viewing differences might fail if the original PBE directories are not present.", Window.GetWindow(this));
                    oldPathToUse = loadedResult.OldLolPath;
                    newPathToUse = loadedResult.NewLolPath;
                }

                LoadWadComparisonRequested?.Invoke(this, new LoadWadComparisonEventArgs(loadedResult.Diffs, oldPathToUse, newPathToUse, jsonPath));
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Failed to load comparison results.");
                CustomMessageBoxService.ShowError("Error", $"Failed to load or parse the results file: {ex.Message}", Window.GetWindow(this));
            }
        }
    }
}
