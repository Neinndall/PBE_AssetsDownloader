using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using LeagueToolkit.Core.Renderer;
using LeagueToolkit.Core.Wad;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using PBE_AssetsManager.Info;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Views.Helpers;
using System.Windows.Input;
using System.Windows.Media;

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

    public class SerializableChunkDiff
    {
        public ChunkDiffType Type { get; set; }
        public string OldPath { get; set; }
        public string NewPath { get; set; }
        public string SourceWadFile { get; set; }
        public ulong? OldUncompressedSize { get; set; }
        public ulong? NewUncompressedSize { get; set; }
        public ulong OldPathHash { get; set; }
        public ulong NewPathHash { get; set; }
        public string Path => NewPath ?? OldPath;
        public string FileName => System.IO.Path.GetFileName(Path);
    }
    #endregion

    public partial class WadComparisonResultWindow : Window
    {
        private readonly List<SerializableChunkDiff> _serializableDiffs;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private readonly string _oldPbePath;
        private readonly string _newPbePath;

        public WadComparisonResultWindow(List<ChunkDiff> diffs, CustomMessageBoxService customMessageBoxService, string oldPbePath, string newPbePath)
        {
            InitializeComponent();
            _customMessageBoxService = customMessageBoxService;
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

        public WadComparisonResultWindow(List<SerializableChunkDiff> serializableDiffs, CustomMessageBoxService customMessageBoxService, string oldPbePath = null, string newPbePath = null)
        {
            InitializeComponent();
            _customMessageBoxService = customMessageBoxService;
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
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Save Comparison Results",
                FileName = $"WadComparison_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
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
                    File.WriteAllText(saveFileDialog.FileName, json);
                    _customMessageBoxService.ShowSuccess("Success", "Results saved successfully!", this);
                }
                catch (Exception ex)
                {
                    _customMessageBoxService.ShowError("Error", $"Failed to save results: {ex.Message}", this);
                }
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
                    
                    var pixelData = mainMip.Span.ToArray();

                    return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixelData, width * 4);
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
    }
}