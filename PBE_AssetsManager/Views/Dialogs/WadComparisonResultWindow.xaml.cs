using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BCnEncoder.Shared;
using LeagueToolkit.Core.Renderer;
using LeagueToolkit.Core.Wad;
using Microsoft.Extensions.DependencyInjection;
using PBE_AssetsManager.Info;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Helpers;

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
        private readonly string _oldPbePath;
        private readonly string _newPbePath;

        public WadComparisonResultWindow(List<ChunkDiff> diffs, CustomMessageBoxService customMessageBoxService, DirectoriesCreator directoriesCreator, AssetDownloader assetDownloaderService, LogService logService, string oldPbePath, string newPbePath)
        {
            InitializeComponent();
            _customMessageBoxService = customMessageBoxService;
            _directoriesCreator = directoriesCreator;
            _assetDownloaderService = assetDownloaderService;
            _logService = logService;
            _oldPbePath = oldPbePath;
            _newPbePath = newPbePath;
            _serializableDiffs = diffs.Select(d => new SerializableChunkDiff
            {
                Type = d.Type,
                OldPath = d.OldPath,
                NewPath = d.NewPath,
                SourceWadFile = d.SourceWadFile,
                OldPathHash = d.OldChunk.PathHash,
                NewPathHash = d.NewChunk.PathHash,
                OldUncompressedSize = (d.Type == ChunkDiffType.New) ? (ulong?)null : (ulong)d.OldChunk.UncompressedSize,
                NewUncompressedSize = (d.Type == ChunkDiffType.Removed) ? (ulong?)null : (ulong)d.NewChunk.UncompressedSize
            }).ToList();
            PopulateResults(_serializableDiffs);
        }

        public WadComparisonResultWindow(List<SerializableChunkDiff> serializableDiffs, CustomMessageBoxService customMessageBoxService, DirectoriesCreator directoriesCreator, AssetDownloader assetDownloaderService, LogService logService, string oldPbePath = null, string newPbePath = null)
        {
            InitializeComponent();
            _customMessageBoxService = customMessageBoxService;
            _directoriesCreator = directoriesCreator;
            _assetDownloaderService = assetDownloaderService;
            _logService = logService;
            _serializableDiffs = serializableDiffs;
            _oldPbePath = oldPbePath;
            _newPbePath = newPbePath;
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

                renamedOldNamePanel.Visibility = Visibility.Collapsed;
                renamedNewNamePanel.Visibility = Visibility.Collapsed;
                genericFileNamePanel.Visibility = Visibility.Collapsed;
                oldSizePanel.Visibility = Visibility.Visible;
                newSizePanel.Visibility = Visibility.Visible;

                string directoryPath;

                if (diff.Type == ChunkDiffType.Renamed)
                {
                    renamedOldNamePanel.Visibility = Visibility.Visible;
                    renamedNewNamePanel.Visibility = Visibility.Visible;
                    
                    renamedOldNameTextBlock.Text = Path.GetFileName(diff.OldPath);
                    renamedNewNameTextBlock.Text = Path.GetFileName(diff.NewPath);
                    directoryPath = Path.GetDirectoryName(diff.NewPath);
                }
                else
                {
                    genericFileNamePanel.Visibility = Visibility.Visible;
                    string currentPath = diff.NewPath ?? diff.OldPath;
                    genericFileNameTextBlock.Text = Path.GetFileName(currentPath);
                    directoryPath = Path.GetDirectoryName(currentPath);
                }

                pathTextBlock.Text = string.IsNullOrEmpty(directoryPath) ? "N/A" : directoryPath;

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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string saveFolderPath = _directoriesCreator.WadComparisonSavePath;

                if (!Directory.Exists(saveFolderPath))
                {
                    Directory.CreateDirectory(saveFolderPath);
                }

                string fileName = $"WadComparison_{{DateTime.Now:yyyyMMdd_HHmmss}}.json";
                string fullPath = Path.Combine(saveFolderPath, fileName);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var comparisonResult = new SerializableComparisonResult
                {
                    OldPbePath = _oldPbePath,
                    NewPbePath = _newPbePath,
                    Diffs = _serializableDiffs
                };

                var json = JsonSerializer.Serialize(comparisonResult, options);
                File.WriteAllText(fullPath, json);
                _customMessageBoxService.ShowSuccess("Success", $"Results saved successfully to: {fullPath}", this);
            }
            catch (Exception ex)
            {
                _customMessageBoxService.ShowError("Error", $"Failed to save results: {ex.Message}", this);
            }
        }

        private void ViewDifferences_Click(object sender, RoutedEventArgs e)
        {
            if (resultsTreeView.SelectedItem is not SerializableChunkDiff diff)
            {
                return;
            }

            if (string.IsNullOrEmpty(_oldPbePath) || string.IsNullOrEmpty(_newPbePath))
            {
                _customMessageBoxService.ShowInfo("Info", "Difference viewing is not available for results loaded from a file.", this);
                return;
            }

            try
            {
                string extension = Path.GetExtension(diff.Path).ToLowerInvariant();
                var textExtensions = new[] { ".json", ".txt", ".lua", ".xml", ".yaml", ".yml", ".ini", ".log" };
                var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tex" };

                string oldWadPath = Path.Combine(_oldPbePath, diff.SourceWadFile);
                string newWadPath = Path.Combine(_newPbePath, diff.SourceWadFile);

                byte[] oldData = null;
                byte[] newData = null;

                using (var oldWad = new WadFile(oldWadPath))
                {
                    if (oldWad.Chunks.TryGetValue(diff.OldPathHash, out WadChunk oldChunk))
                    {
                        using (var decompressedChunk = oldWad.LoadChunkDecompressed(oldChunk))
                        {
                            oldData = decompressedChunk.Span.ToArray();
                        }
                    }
                }

                using (var newWad = new WadFile(newWadPath))
                {
                    if (newWad.Chunks.TryGetValue(diff.NewPathHash, out WadChunk newChunk))
                    {
                        using (var decompressedChunk = newWad.LoadChunkDecompressed(newChunk))
                        {
                            newData = decompressedChunk.Span.ToArray();
                        }
                    }
                }

                if (oldData == null || newData == null)
                {
                    _customMessageBoxService.ShowError("Error", "Could not extract data for one or both files.", this);
                    return;
                }

                if (textExtensions.Contains(extension))
                {
                    string oldText = Encoding.UTF8.GetString(oldData);
                    string newText = Encoding.UTF8.GetString(newData);

                    string oldFormatted = JsonDiffHelper.FormatJson(oldText);
                    string newFormatted = JsonDiffHelper.FormatJson(newText);

                    var diffWindow = App.ServiceProvider.GetRequiredService<JsonDiffWindow>();
                    _ = diffWindow.LoadAndDisplayDiffAsync(oldFormatted, newFormatted, $"Old: {diff.Path}", $"New: {diff.Path}");
                    diffWindow.Owner = this;
                    diffWindow.Show();
                }
                else if (imageExtensions.Contains(extension))
                {
                    var oldImage = ToBitmapSource(oldData, extension);
                    var newImage = ToBitmapSource(newData, extension);

                    if (oldImage == null || newImage == null)
                    {
                        _customMessageBoxService.ShowError("Error", "Could not decode one or both images.", this);
                        return;
                    }

                    var imageDiffWindow = new ImageDiffWindow(oldImage, newImage) { Owner = this };
                    imageDiffWindow.Show();
                }
                else
                {
                    _customMessageBoxService.ShowInfo("Info", $"File type '{extension}' is not supported for comparison.", this);
                }
            }
            catch (Exception ex)
            {
                _customMessageBoxService.ShowError("Error", $"An error occurred while preparing the diff view: {ex.Message}", this);
            }
        }

        private BitmapSource ToBitmapSource(byte[] data, string extension)
        {
            if (data == null || data.Length == 0) return null;

            if (extension == ".tex" || extension == ".dds")
            {
                using (var stream = new MemoryStream(data))
                {
                    var texture = LeagueToolkit.Core.Renderer.Texture.Load(stream);
                    if (texture.Mips.Length == 0) return null;

                    var mainMip = texture.Mips[0];
                    var width = mainMip.Width;
                    var height = mainMip.Height;

                    if (mainMip.Span.TryGetSpan(out Span<ColorRgba32> pixelSpan))
                    {
                        var pixelByteSpan = MemoryMarshal.AsBytes(pixelSpan);
                        return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixelByteSpan.ToArray(), width * 4);
                    }

                    return null; // Should not happen with how Texture is created
                }
            }
            else
            {
                using (var stream = new MemoryStream(data))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                    return bitmapImage;
                }
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
            // Get the ContextMenu instance from the sender
            var contextMenu = sender as ContextMenu;
            if (contextMenu == null) return;

            // Find the "Download Selected" MenuItem dynamically by its header.
            // This is more robust than relying on a fixed index, which was causing crashes.
            var downloadMenuItem = contextMenu.Items.OfType<MenuItem>()
                                                    .FirstOrDefault(m => "Download Selected".Equals(m.Header as string));
            if (downloadMenuItem != null)
            {
                downloadMenuItem.IsEnabled = false; // Default to disabled

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
                _logService.LogError($"Download failed: {ex.ToString()}");
            }
            finally
            {
                _logService.Log("Asset download from WAD comparison finished.");
            }
        }

        private List<SerializableChunkDiff> GetDownloadableDiffsFromSelection()
        {
            var selectedItem = resultsTreeView.SelectedItem;
            var downloadableDiffs = new List<SerializableChunkDiff>();

            if (selectedItem is SerializableChunkDiff singleDiff)
            {
                if (singleDiff.Type == ChunkDiffType.New || singleDiff.Type == ChunkDiffType.Modified)
                {
                    downloadableDiffs.Add(singleDiff);
                }
            }
            else if (selectedItem is DiffTypeGroupViewModel typeGroup)
            {
                if (typeGroup.Type == ChunkDiffType.New || typeGroup.Type == ChunkDiffType.Modified)
                {
                    downloadableDiffs.AddRange(typeGroup.Diffs);
                }
            }
            else if (selectedItem is WadGroupViewModel wadGroup)
            {
                downloadableDiffs.AddRange(wadGroup.Types
                    .Where(t => t.Type == ChunkDiffType.New || t.Type == ChunkDiffType.Modified)
                    .SelectMany(t => t.Diffs));
            }

            return downloadableDiffs;
        }
    }
}