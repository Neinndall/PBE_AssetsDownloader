using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LeagueToolkit.Core.Wad;
using Microsoft.Win32;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader.UI.Dialogs
{
    public class SerializableChunkDiff
    {
        public ChunkDiffType Type { get; set; }
        public string OldPath { get; set; }
        public string NewPath { get; set; }
        public string SourceWadFile { get; set; }
        public ulong? OldUncompressedSize { get; set; }
        public ulong? NewUncompressedSize { get; set; }
    }

    public partial class WadComparisonResultWindow : Window
    {
        private readonly List<SerializableChunkDiff> _serializableDiffs;

        public WadComparisonResultWindow(List<ChunkDiff> diffs)
        {
            InitializeComponent();
            _serializableDiffs = diffs.Select(d => new SerializableChunkDiff
            {
                Type = d.Type,
                OldPath = d.OldPath,
                NewPath = d.NewPath,
                SourceWadFile = d.SourceWadFile,
                OldUncompressedSize = (d.Type == ChunkDiffType.New) ? (ulong?)null : (ulong?)d.OldChunk.UncompressedSize,
                NewUncompressedSize = (d.Type == ChunkDiffType.Removed) ? (ulong?)null : (ulong?)d.NewChunk.UncompressedSize
            }).ToList();
            PopulateResults(_serializableDiffs);
        }

        public WadComparisonResultWindow(List<SerializableChunkDiff> serializableDiffs)
        {
            InitializeComponent();
            _serializableDiffs = serializableDiffs;
            PopulateResults(_serializableDiffs);
        }

        private void PopulateResults(List<SerializableChunkDiff> diffs)
        {
            resultsTreeView.Items.Clear();

            var groupedByWad = diffs.GroupBy(d => d.SourceWadFile)
                                    .OrderBy(g => g.Key);

            foreach (var wadGroup in groupedByWad)
            {
                var wadNode = new TreeViewItem
                {
                    Header = $"{wadGroup.Key} ({wadGroup.Count()})",
                    IsExpanded = true,
                    FontWeight = FontWeights.Bold
                };
                resultsTreeView.Items.Add(wadNode);

                var groupedByType = wadGroup.GroupBy(d => d.Type)
                                            .OrderBy(g => g.Key.ToString());

                foreach (var typeGroup in groupedByType)
                {
                    var typeNode = new TreeViewItem
                    {
                        Header = $"{typeGroup.Key} ({typeGroup.Count()})",
                        Foreground = GetBrushForDiffType(typeGroup.Key),
                        IsExpanded = true
                    };
                    wadNode.Items.Add(typeNode);

                    foreach (var diff in typeGroup.OrderBy(d => d.NewPath ?? d.OldPath))
                    {
                        var fileNode = new TreeViewItem();
                        fileNode.Header = FormatDiffHeader(diff);
                        typeNode.Items.Add(fileNode);
                    }
                }
            }
        }

        private string FormatDiffHeader(SerializableChunkDiff diff)
        {
            switch (diff.Type)
            {
                case ChunkDiffType.New:
                    return $"{diff.NewPath} (Size: {diff.NewUncompressedSize / 1024 ?? 0} KB)";
                case ChunkDiffType.Removed:
                    return $"{diff.OldPath} (Size: {diff.OldUncompressedSize / 1024 ?? 0} KB)";
                case ChunkDiffType.Modified:
                    var oldSize = diff.OldUncompressedSize / 1024 ?? 0;
                    var newSize = diff.NewUncompressedSize / 1024 ?? 0;
                    var sizeDiff = newSize - oldSize;
                    return $"{diff.NewPath} (Size: {oldSize} KB -> {newSize} KB, Diff: {sizeDiff:+#;-#;0} KB)";
                case ChunkDiffType.Renamed:
                    return $"{diff.OldPath} -> {diff.NewPath}";
                default:
                    return "Unknown change";
            }
        }

        private Brush GetBrushForDiffType(ChunkDiffType type)
        {
            switch (type)
            {
                case ChunkDiffType.New:
                    return Brushes.Green;
                case ChunkDiffType.Removed:
                    return Brushes.Red;
                case ChunkDiffType.Modified:
                    return Brushes.Blue;
                case ChunkDiffType.Renamed:
                    return Brushes.Purple;
                default:
                    return Brushes.Black;
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
                    var json = JsonSerializer.Serialize(_serializableDiffs, options);
                    File.WriteAllText(saveFileDialog.FileName, json);
                    MessageBox.Show("Results saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save results: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}