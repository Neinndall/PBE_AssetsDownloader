using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PBE_AssetsManager.Services.Downloads;
using PBE_AssetsManager.Services;

namespace PBE_AssetsManager.Views.Controls.Export
{
    public class PreviewRequestedEventArgs : EventArgs
    {
        public string DifferencesPath { get; }
        public List<string> SelectedAssetTypes { get; }
        public Func<IEnumerable<string>, List<string>, List<string>> FilterLogic { get; }

        public PreviewRequestedEventArgs(string differencesPath, List<string> selectedAssetTypes, Func<IEnumerable<string>, List<string>, List<string>> filterLogic)
        {
            DifferencesPath = differencesPath;
            SelectedAssetTypes = selectedAssetTypes;
            FilterLogic = filterLogic;
        }
    }

    public partial class FilterConfigControl : UserControl
    {
        public LogService LogService { get; set; }
        public AssetDownloader AssetDownloader { get; set; }
        public CustomMessageBoxService CustomMessageBoxService { get; set; }

        public event EventHandler<PreviewRequestedEventArgs> PreviewRequested;

        private readonly CheckBox[] _individualCheckboxes;

        public FilterConfigControl()
        {
            InitializeComponent();
            _individualCheckboxes = new[] { chkImages, chkAudios, chkPlugins, chkGame };
            SetupCheckboxEvents();
        }

        public void DoPreview(string differencesPath)
        {
            if (!ValidateInputPath(differencesPath)) return;

            var selectedAssetTypes = GetSelectedAssetTypes();
            LogSelectedTypes(selectedAssetTypes);

            if (!selectedAssetTypes.Any())
            {
                ShowWarning("Select at least one type for preview.", "Type not selected");
                return;
            }

            PreviewRequested?.Invoke(this, new PreviewRequestedEventArgs(differencesPath, selectedAssetTypes, FilterAssetsByType));
        }

        public async Task DoDownload(string differencesPath, string downloadPath)
        {
            if (!ValidateInputPath(differencesPath) || !ValidateDownloadPath(downloadPath)) return;

            var selectedAssetTypes = GetSelectedAssetTypes();
            if (!selectedAssetTypes.Any())
            {
                ShowWarning("Select at least one type for download.", "Type not selected");
                return;
            }

            var (gameLines, lcuLines) = await ReadDifferenceFiles(differencesPath);
            if (!gameLines.Any() && !lcuLines.Any())
            {
                ShowWarning("No assets were found with the provided differences.", "Warning");
                return;
            }

            await DownloadAssets(gameLines, lcuLines, selectedAssetTypes, downloadPath);
        }

        private void SetupCheckboxEvents()
        {
            chkAll.Checked += ChkAll_Checked;
            chkAll.Unchecked += ChkAll_Unchecked;

            foreach (var checkbox in _individualCheckboxes)
            {
                checkbox.Checked += IndividualCheckBox_Changed;
                checkbox.Unchecked += IndividualCheckBox_Changed;
            }
        }

        private void ChkAll_Checked(object sender, RoutedEventArgs e)
        {
            SetIndividualCheckboxes(false);
        }

        private void ChkAll_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!HasAnyIndividualCheckboxSelected())
                chkAll.IsChecked = true;
        }

        private void IndividualCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.IsChecked.GetValueOrDefault())
            {
                chkAll.IsChecked = false;
            }
            else if (!HasAnyIndividualCheckboxSelected())
            {
                chkAll.IsChecked = true;
            }
        }

        private void SetIndividualCheckboxes(bool value)
        {
            foreach (var checkbox in _individualCheckboxes)
            {
                checkbox.IsChecked = value;
            }
        }

        private bool HasAnyIndividualCheckboxSelected()
        {
            return _individualCheckboxes.Any(cb => cb.IsChecked.GetValueOrDefault());
        }

        private List<string> GetSelectedAssetTypes()
        {
            var selectedTypes = new List<string>();

            if (chkAll.IsChecked.GetValueOrDefault())
            {
                selectedTypes.Add("All");
            }
            else
            {
                selectedTypes.AddRange(
                    _individualCheckboxes.Where(cb => cb.IsChecked.GetValueOrDefault())
                                         .Select(cb => cb.Tag as string)
                );
            }

            return selectedTypes;
        }

        private bool ValidateInputPath(string inputFolder)
        {
            if (string.IsNullOrWhiteSpace(inputFolder) || !Directory.Exists(inputFolder))
            {
                ShowWarning("Select a valid folder that contains the differences_game and differences_lcu files.", "Invalid path");
                return false;
            }
            return true;
        }

        private bool ValidateDownloadPath(string downloadFolder)
        {
            if (string.IsNullOrWhiteSpace(downloadFolder) || !Directory.Exists(downloadFolder))
            {
                ShowWarning("Select a valid folder to save the exported assets.", "Invalid Folder");
                return false;
            }
            return true;
        }

        private void ShowWarning(string message, string title)
        {
            LogService.LogWarning(message);
            CustomMessageBoxService.ShowWarning(title, message, Window.GetWindow(this));
        }

        private void LogSelectedTypes(List<string> selectedAssetTypes)
        {
            if (selectedAssetTypes.Any())
                LogService.LogDebug($"ExportWindow: Selected asset types: {string.Join(", ", selectedAssetTypes)}");
            else
                LogService.LogDebug("ExportWindow: No asset types were selected.");
        }

        private async Task<(string[] gameLines, string[] lcuLines)> ReadDifferenceFiles(string differencesPath)
        {
            var differencesGamePath = Path.Combine(differencesPath, "differences_game.txt");
            var differencesLcuPath = Path.Combine(differencesPath, "differences_lcu.txt");

            var gameLines = File.Exists(differencesGamePath) ? await File.ReadAllLinesAsync(differencesGamePath) : Array.Empty<string>();
            var lcuLines = File.Exists(differencesLcuPath) ? await File.ReadAllLinesAsync(differencesLcuPath) : Array.Empty<string>();

            return (gameLines, lcuLines);
        }

        private async Task DownloadAssets(string[] gameLines, string[] lcuLines, List<string> selectedAssetTypes, string downloadPath)
        {
            var notFoundAssets = new List<string>();
            var gameAssetsList = FilterAssetsByType(gameLines, selectedAssetTypes);
            var lcuAssetsList = FilterAssetsByType(lcuLines, selectedAssetTypes);

            var gameAssets = gameAssetsList.Select(asset => (asset, "https://raw.communitydragon.org/pbe/game/"));
            var lcuAssets = lcuAssetsList.Select(asset => (asset, "https://raw.communitydragon.org/pbe/"));

            var allAssets = gameAssets.Concat(lcuAssets).ToList();
            int overallTotalFiles = allAssets.Count;

            AssetDownloader.NotifyDownloadStarted(overallTotalFiles);

            LogService.Log($"Total GAME assets to download: {gameAssetsList.Count} and Total LCU assets to download: {lcuAssetsList.Count}");
            await AssetDownloader.DownloadAssets(
                allAssets,
                downloadPath,
                notFoundAssets,
                overallTotalFiles,
                0
            );

            HandleDownloadCompletion(notFoundAssets, downloadPath);
            AssetDownloader.NotifyDownloadCompleted(); // Notify completion after all downloads
        }

        private void HandleDownloadCompletion(List<string> notFoundAssets, string downloadPath)
        { 
            if (notFoundAssets.Any())
            {
                string notFoundFilePath = Path.Combine(downloadPath, "NotFoundAssets.txt");
                File.WriteAllLines(notFoundFilePath, notFoundAssets);

                string message = $"Some assets could not be downloaded. A list of missing assets has been saved to NotFoundAssets.txt";
                LogService.LogWarning(message);
            }
            else
            {
                LogService.LogSuccess("Download completed successfully!");
            }
        }

        private List<string> FilterAssetsByType(IEnumerable<string> lines, List<string> selectedTypes)
        {
            var filteredAndParsedLines = lines
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split(' ').Skip(1).FirstOrDefault())
                .Where(path => path != null)
                .ToList();

            LogService.LogDebug($"FilterAssetsByType: Total parsed lines (before type filter): {filteredAndParsedLines.Count}");

            if (selectedTypes.Any(type => type.Equals("All", StringComparison.OrdinalIgnoreCase)))
                return filteredAndParsedLines.Distinct().ToList();

            var finalFilteredAssets = filteredAndParsedLines
                .Where(path => IsPathMatchingSelectedTypes(path, selectedTypes))
                .Distinct()
                .ToList();

            LogService.LogDebug($"FilterAssetsByType: Total assets after type filter and distinct: {finalFilteredAssets.Count}");
            return finalFilteredAssets;
        }

        private bool IsPathMatchingSelectedTypes(string path, List<string> selectedTypes)
        {
            var typeCheckers = new Dictionary<string, Func<string, bool>>
            {
                { "Images", p => p.EndsWith(".tex", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) },
                { "Audios", p => p.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) },
                { "Plugins", p => p.StartsWith("plugins/", StringComparison.OrdinalIgnoreCase) },
                { "Game", p => p.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) }
            };

            foreach (var type in selectedTypes)
            {
                if (typeCheckers.TryGetValue(type, out var checker) && checker(path))
                {
                    LogService.LogDebug($"FilterAssetsByType: Path '{path}' matched '{type}'.");
                    return true;
                }
            }
            return false;
        }
    }
}