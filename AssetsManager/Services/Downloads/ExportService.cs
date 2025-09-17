
using AssetsManager.Services.Core;
using AssetsManager.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace AssetsManager.Services.Downloads
{
    public class ExportService
    {
        private readonly LogService _logService;
        private readonly AssetDownloader _assetDownloader;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private readonly AssetsPreview _assetsPreview;

        public string DifferencesPath { get; set; }
        public string DownloadTargetPath { get; set; }
        public List<string> SelectedAssetTypes { get; set; } = new List<string>();
        public Func<IEnumerable<string>, List<string>, List<string>> FilterLogic { get; set; }

        public ExportService(
            LogService logService,
            AssetDownloader assetDownloader,
            CustomMessageBoxService customMessageBoxService,
            AssetsPreview assetsPreview)
        {
            _logService = logService;
            _assetDownloader = assetDownloader;
            _customMessageBoxService = customMessageBoxService;
            _assetsPreview = assetsPreview;
        }

        public void DoPreview()
        {
            if (!ValidateInputPath(DifferencesPath)) return;

            _logService.LogDebug($"Selected asset types: {string.Join(", ", SelectedAssetTypes)}");

            if (!SelectedAssetTypes.Any())
            {
                _customMessageBoxService.ShowWarning("Type not selected", "Select at least one type for preview.", null);
                return;
            }

            var previewWindow = new PreviewAssetsWindow(_logService, _assetsPreview, _customMessageBoxService);
            previewWindow.InitializeData(DifferencesPath, SelectedAssetTypes, FilterLogic);
            previewWindow.ShowDialog();
        }

        public async Task DoDownload()
        {
            if (!ValidateInputPath(DifferencesPath) || !ValidateDownloadPath(DownloadTargetPath)) return;

            if (!SelectedAssetTypes.Any())
            {
                _customMessageBoxService.ShowWarning("Type not selected", "Select at least one type for download.", null);
                return;
            }

            var (gameLines, lcuLines) = await ReadDifferenceFiles(DifferencesPath);
            if (!gameLines.Any() && !lcuLines.Any())
            {
                _customMessageBoxService.ShowWarning("Warning", "No assets were found with the provided differences.", null);
                return;
            }

            await DownloadAssets(gameLines, lcuLines, SelectedAssetTypes, DownloadTargetPath);
        }

        private bool ValidateInputPath(string inputFolder)
        {
            if (string.IsNullOrWhiteSpace(inputFolder) || !Directory.Exists(inputFolder))
            {
                _customMessageBoxService.ShowWarning("Invalid path", "Select a valid folder that contains the differences_game and differences_lcu files.", null);
                return false;
            }
            return true;
        }

        private bool ValidateDownloadPath(string downloadFolder)
        {
            if (string.IsNullOrWhiteSpace(downloadFolder) || !Directory.Exists(downloadFolder))
            {
                _customMessageBoxService.ShowWarning("Invalid Folder", "Select a valid folder to save the exported assets.", null);
                return false;
            }
            return true;
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
            var gameAssetsList = FilterLogic(gameLines, selectedAssetTypes);
            var lcuAssetsList = FilterLogic(lcuLines, selectedAssetTypes);

            var gameAssets = gameAssetsList.Select(asset => (asset, "https://raw.communitydragon.org/pbe/game/"));
            var lcuAssets = lcuAssetsList.Select(asset => (asset, "https://raw.communitydragon.org/pbe/"));

            var allAssets = gameAssets.Concat(lcuAssets).ToList();
            int overallTotalFiles = allAssets.Count;

            _assetDownloader.NotifyDownloadStarted(overallTotalFiles);

            _logService.Log($"Total GAME assets to download: {gameAssetsList.Count} and Total LCU assets to download: {lcuAssetsList.Count}");
            await _assetDownloader.DownloadAssets(
                allAssets,
                downloadPath,
                notFoundAssets,
                overallTotalFiles,
                0
            );

            HandleDownloadCompletion(notFoundAssets, downloadPath);
            _assetDownloader.NotifyDownloadCompleted();
        }

        private void HandleDownloadCompletion(List<string> notFoundAssets, string downloadPath)
        {
            if (notFoundAssets.Any())
            {
                string notFoundFilePath = Path.Combine(downloadPath, "NotFoundAssets.txt");
                File.WriteAllLines(notFoundFilePath, notFoundAssets);

                string message = $"Some assets could not be downloaded. A list of missing assets has been saved to NotFoundAssets.txt";
                _logService.LogWarning(message);
            }
            else
            {
                _logService.LogSuccess("Download completed successfully!");
            }
        }
    }
}
