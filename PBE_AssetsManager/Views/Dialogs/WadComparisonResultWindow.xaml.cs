using System;
using PBE_AssetsManager.Info;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PBE_AssetsManager.Views.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace PBE_AssetsManager.Views.Dialogs
{
    #region ViewModel Classes
    public class WadGroupViewModel
    {
        public string WadName { get; set; }
        public int DiffCount { get; set; }
        public string WadNameWithCount => $"{WadName} ({DiffCount})";
        public List<DiffTypeGroupViewModel> Types { get; set; }
    }

    public class DiffTypeGroupViewModel
    {
        public ChunkDiffType Type { get; set; }
        public int DiffCount { get; set; }
        public string TypeNameWithCount => $"{Type} ({DiffCount})";
        public List<SerializableChunkDiff> Diffs { get; set; }
    }
    #endregion

    public partial class WadComparisonResultWindow : Window
    {
        private readonly List<SerializableChunkDiff> _serializableDiffs;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly AssetDownloader _assetDownloaderService;
        private readonly LogService _logService;
        private readonly WadDifferenceService _wadDifferenceService;
        private readonly WadPackagingService _wadPackagingService;
        private readonly string _oldPbePath;
        private readonly string _newPbePath;
        private readonly string _sourceJsonPath; // Path to the loaded wadcomparison.json

        public WadComparisonResultWindow(List<ChunkDiff> diffs, CustomMessageBoxService customMessageBoxService, DirectoriesCreator directoriesCreator, AssetDownloader assetDownloaderService, LogService logService, WadDifferenceService wadDifferenceService, WadPackagingService wadPackagingService, string oldPbePath, string newPbePath)
        {
            InitializeComponent();
            _customMessageBoxService = customMessageBoxService;
            _directoriesCreator = directoriesCreator;
            _assetDownloaderService = assetDownloaderService;
            _logService = logService;
            _wadDifferenceService = wadDifferenceService;
            _wadPackagingService = wadPackagingService;
            _oldPbePath = oldPbePath;
            _newPbePath = newPbePath;
            _sourceJsonPath = null; // Not loaded from a file
            _serializableDiffs = diffs.Select(d => new SerializableChunkDiff
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
            PopulateResults(_serializableDiffs);
        }

        public WadComparisonResultWindow(List<SerializableChunkDiff> serializableDiffs, CustomMessageBoxService customMessageBoxService, DirectoriesCreator directoriesCreator, AssetDownloader assetDownloaderService, LogService logService, WadDifferenceService wadDifferenceService, WadPackagingService wadPackagingService, string oldPbePath = null, string newPbePath = null, string sourceJsonPath = null)
        {
            InitializeComponent();
            _customMessageBoxService = customMessageBoxService;
            _directoriesCreator = directoriesCreator;
            _assetDownloaderService = assetDownloaderService;
            _logService = logService;
            _wadDifferenceService = wadDifferenceService;
            _wadPackagingService = wadPackagingService;
            _serializableDiffs = serializableDiffs;
            _oldPbePath = oldPbePath;
            _newPbePath = newPbePath;
            _sourceJsonPath = sourceJsonPath; // Store the path of the loaded file
            PopulateResults(_serializableDiffs);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text;
            searchPlaceholder.Visibility = string.IsNullOrEmpty(searchText) ? Visibility.Visible : Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                PopulateResults(_serializableDiffs);
            }
            else
            {
                var filteredDiffs = _serializableDiffs
                    .Where(d => d.FileName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                PopulateResults(filteredDiffs);
            }
        }

        private void PopulateResults(List<SerializableChunkDiff> diffs)
        {
            var groupedByWad = diffs.GroupBy(d => d.SourceWadFile)
                                    .OrderBy(g => g.Key);

            summaryTextBlock.Text = $"Found {diffs.Count} differences across {groupedByWad.Count()} WAD files.";

            var wadGroups = groupedByWad.Select(wadGroup => new WadGroupViewModel
            {
                WadName = wadGroup.Key,
                DiffCount = wadGroup.Count(),
                Types = wadGroup.GroupBy(d => d.Type)
                                .OrderBy(g => g.Key.ToString())
                                .Select(typeGroup => new DiffTypeGroupViewModel
                                {
                                    Type = typeGroup.Key,
                                    DiffCount = typeGroup.Count(),
                                    Diffs = typeGroup.OrderBy(d => d.NewPath ?? d.OldPath).ToList()
                                }).ToList()
            }).ToList();

            resultsTreeView.ItemsSource = wadGroups;
        }

        private string FormatSize(ulong? sizeInBytes)
        {
            if (sizeInBytes == null) return "N/A";
            double sizeInKB = (double)sizeInBytes / 1024.0;
            return $"{sizeInKB:F2} KB";
        }

        private void ResultsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is SerializableChunkDiff diff)
            {
                noSelectionPanel.Visibility = Visibility.Collapsed;
                detailsContentPanel.Visibility = Visibility.Visible;

                // Reset panel visibility
                renamedOldNamePanel.Visibility = Visibility.Collapsed;
                renamedNewNamePanel.Visibility = Visibility.Collapsed;
                genericFileNamePanel.Visibility = Visibility.Collapsed;
                pathPanel.Visibility = Visibility.Visible; // Default to visible
                oldSizePanel.Visibility = Visibility.Visible;
                newSizePanel.Visibility = Visibility.Visible;

                if (diff.Type == ChunkDiffType.Renamed)
                {
                    renamedOldNamePanel.Visibility = Visibility.Visible;
                    renamedNewNamePanel.Visibility = Visibility.Visible;
                    pathPanel.Visibility = Visibility.Collapsed; // Hide generic path for renames

                    renamedOldNameTextBlock.Text = diff.OldPath;
                    renamedNewNameTextBlock.Text = diff.NewPath;
                }
                else
                {
                    genericFileNamePanel.Visibility = Visibility.Visible;
                    string currentPath = diff.NewPath ?? diff.OldPath;
                    genericFileNameTextBlock.Text = Path.GetFileName(currentPath);
                    pathTextBlock.Text = Path.GetDirectoryName(currentPath) ?? "N/A";
                }

                changeTypeTextBlock.Text = diff.Type.ToString();
                sourceWadTextBlock.Text = diff.SourceWadFile;

                oldSizeTextBlock.Text = FormatSize(diff.OldUncompressedSize);
                newSizeTextBlock.Text = FormatSize(diff.NewUncompressedSize);

                if (diff.Type == ChunkDiffType.New)
                {
                    oldSizePanel.Visibility = Visibility.Collapsed;
                }
                else if (diff.Type == ChunkDiffType.Removed)
                {
                    newSizePanel.Visibility = Visibility.Collapsed;
                }
                else if (diff.Type == ChunkDiffType.Modified)
                {
                    long sizeDiff = (long)(diff.NewUncompressedSize ?? 0) - (long)(diff.OldUncompressedSize ?? 0);
                    if (sizeDiff != 0)
                    {
                        string diffSign = sizeDiff > 0 ? "+" : "";
                        newSizeTextBlock.Text += $" ({diffSign}{FormatSize((ulong)Math.Abs(sizeDiff))})";
                    }
                }
            }
            else
            {
                noSelectionPanel.Visibility = Visibility.Visible;
                detailsContentPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sourceJsonPath != null)
            {
                _customMessageBoxService.ShowInfo("Info", "This result is already saved.", this);
                return;
            }

            try
            {
                // 1. Use the paths from DirectoriesCreator
                string comparisonFullPath = _directoriesCreator.WadComparisonFullPath;
                string oldChunksPath = _directoriesCreator.OldChunksPath;
                string newChunksPath = _directoriesCreator.NewChunksPath;
                Directory.CreateDirectory(oldChunksPath);
                Directory.CreateDirectory(newChunksPath);

                // 2. Create lean WAD package
                _logService.Log("Starting lean WAD packaging process...");
                await _wadPackagingService.CreateLeanWadPackageAsync(_serializableDiffs, _oldPbePath, _newPbePath, oldChunksPath, newChunksPath);
                _logService.LogSuccess("Finished lean WAD packaging process.");

                // 3. Save the wadcomparison.json file
                string jsonFilePath = Path.Combine(comparisonFullPath, "wadcomparison.json");
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var comparisonResult = new SerializableComparisonResult
                {
                    // We save the original PBE paths for informational purposes
                    OldPbePath = _oldPbePath,
                    NewPbePath = _newPbePath,
                    Diffs = _serializableDiffs
                };
                var json = JsonSerializer.Serialize(comparisonResult, options);
                await File.WriteAllTextAsync(jsonFilePath, json);

                _logService.Log("Finished saving comparison WAD files.");
                _customMessageBoxService.ShowSuccess("Success", $"Results and associated WAD files saved successfully to: {comparisonFullPath}", this);
            }
            catch (Exception ex)
            {
                _customMessageBoxService.ShowError("Error", $"Failed to save results: {ex.Message}", this);
                _logService.LogError(ex, "Failed to save comparison results.");
            }
        }

        private async void ViewDifferences_Click(object sender, RoutedEventArgs e)
        {
            if (resultsTreeView.SelectedItem is not SerializableChunkDiff diff)
            {
                return;
            }

            // This method now works consistently whether the paths are to a PBE directory or a saved wad_files directory.
            if (string.IsNullOrEmpty(_oldPbePath) || string.IsNullOrEmpty(_newPbePath))
            {
                _customMessageBoxService.ShowInfo("Info", "The paths to the PBE or saved WAD directories are missing.", this);
                return;
            }

            var (dataType, oldData, newData, oldPath, newPath) = await _wadDifferenceService.PrepareDifferenceDataAsync(diff, _oldPbePath, _newPbePath);

            switch (dataType)
            {
                case "json":
                    string oldJson = JsonDiffHelper.FormatJson((string)oldData);
                    string newJson = JsonDiffHelper.FormatJson((string)newData);

                    var jsonDiffWindow = App.ServiceProvider.GetRequiredService<JsonDiffWindow>();
                    _ = jsonDiffWindow.LoadAndDisplayDiffAsync(oldJson, newJson, oldPath, newPath);
                    jsonDiffWindow.Owner = this;
                    jsonDiffWindow.Show();
                    break;

                case "image":
                    var imageDiffWindow = new ImageDiffWindow((BitmapSource)oldData, (BitmapSource)newData, oldPath, newPath) { Owner = this };
                    imageDiffWindow.Show();
                    break;

                case "unsupported":
                case "error":
                    // The service already shows a message box in these cases.
                    break;
            }
        }

        private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.IsSelected = true;
                e.Handled = true;
            }
        }

        static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            var contextMenu = sender as ContextMenu;
            if (contextMenu == null) return;

            // --- Logic for "View Differences" ---
            var viewDiffMenuItem = contextMenu.Items.OfType<MenuItem>()
                                                    .FirstOrDefault(m => "View Differences".Equals(m.Header as string));
            if (viewDiffMenuItem != null)
            {
                viewDiffMenuItem.IsEnabled = false; // Default to disabled
                if (resultsTreeView.SelectedItem is SerializableChunkDiff diff)
                {
                    // Enable ONLY for Modified files.
                    if (diff.Type == ChunkDiffType.Modified)
                    {
                        viewDiffMenuItem.IsEnabled = true;
                    }
                }
            }

            // --- Existing logic for "Download Selected" ---
            var downloadMenuItem = contextMenu.Items.OfType<MenuItem>()
                                                    .FirstOrDefault(m => "Download Selected".Equals(m.Header as string));
            if (downloadMenuItem != null)
            {
                downloadMenuItem.IsEnabled = false;

                if (resultsTreeView.SelectedItem == null) return;

                List<SerializableChunkDiff> downloadableDiffs = GetDownloadableDiffsFromSelection();
                if (downloadableDiffs.Any())
                {
                    downloadMenuItem.IsEnabled = true;
                }
            }
        }

        private async void DownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            List<SerializableChunkDiff> diffsToDownload = GetDownloadableDiffsFromSelection();
            if (!diffsToDownload.Any())
            {
                _customMessageBoxService.ShowInfo("Info", "No downloadable files (New or Modified) in the current selection.", this);
                return;
            }

            _logService.Log($"Starting download of {diffsToDownload.Count} assets from WAD comparison...");
            try
            {
                int successCount = await _assetDownloaderService.DownloadWadAssetsAsync(diffsToDownload);

                if (successCount == diffsToDownload.Count)
                {
                    _customMessageBoxService.ShowSuccess("Success", $"Successfully downloaded {successCount} asset(s).");
                }
                else
                {
                    _customMessageBoxService.ShowWarning("Partial Success", $"Successfully downloaded {successCount} out of {diffsToDownload.Count} asset(s). Check logs for details.");
                }
            }
            catch (Exception ex)
            {
                _customMessageBoxService.ShowError("Error", $"An error occurred during download: {ex.Message}", this);
                _logService.LogError(ex, "Download failed.");
            }
        }

        private List<SerializableChunkDiff> GetDownloadableDiffsFromSelection()
        {
            var selectedItem = resultsTreeView.SelectedItem;
            var downloadableDiffs = new List<SerializableChunkDiff>();

            if (selectedItem is SerializableChunkDiff singleDiff)
            {
                if (singleDiff.Type == ChunkDiffType.New || singleDiff.Type == ChunkDiffType.Modified || singleDiff.Type == ChunkDiffType.Renamed)
                {
                    downloadableDiffs.Add(singleDiff);
                }
            }
            else if (selectedItem is DiffTypeGroupViewModel typeGroup)
            {
                if (typeGroup.Type == ChunkDiffType.New || typeGroup.Type == ChunkDiffType.Modified || typeGroup.Type == ChunkDiffType.Renamed)
                {
                    downloadableDiffs.AddRange(typeGroup.Diffs);
                }
            }
            else if (selectedItem is WadGroupViewModel wadGroup)
            {
                downloadableDiffs.AddRange(wadGroup.Types
                    .Where(t => t.Type == ChunkDiffType.New || t.Type == ChunkDiffType.Modified || t.Type == ChunkDiffType.Renamed)
                    .SelectMany(t => t.Diffs));
            }

            return downloadableDiffs;
        }
    }
}