using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AssetsManager.Info;
using AssetsManager.Utils;
using AssetsManager.Views.Models;
using AssetsManager.Services.Core;

namespace AssetsManager.Services.Downloads
{
    public enum AssetContentType
    {
        Image,
        Audio,
        Video,
        Text,
        ExternalProgram,
        Unsupported,
        NotFound
    }

    public class PreviewData
    {
        public AssetContentType ContentType { get; set; }
        public string LocalFilePath { get; set; }
        public string AssetUrl { get; set; }
        public string TextContent { get; set; }
        public string Message { get; set; }
    }

    public class AssetsPreview
    {
        private readonly LogService _logService;
        private readonly AssetDownloader _assetDownloader;
        private readonly DirectoriesCreator _directoriesCreator;

        private const string BASE_GAME_URL = "https://raw.communitydragon.org/pbe/game/";
        private const string BASE_LCU_URL = "https://raw.communitydragon.org/pbe/";

        public AssetsPreview(
            AssetDownloader assetDownloader,
            LogService logService,
            DirectoriesCreator directoriesCreator)
        {
            _logService = logService;
            _assetDownloader = assetDownloader;
            _directoriesCreator = directoriesCreator;
        }

        public List<AssetInfo> GetAssetsForPreview(
            string inputFolder,
            List<string> selectedAssetTypes,
            Func<IEnumerable<string>, List<string>, List<string>> filterAssetsByType)
        {
            _logService.LogDebug($"AssetsPreview: Starting GetAssetsForPreview. Input folder: '{inputFolder}'");

            var allAssets = new List<AssetInfo>();

            var differencesGamePath = Path.Combine(inputFolder, "differences_game.txt");
            var differencesLcuPath = Path.Combine(inputFolder, "differences_lcu.txt");

            var gameLines = File.Exists(differencesGamePath)
                ? File.ReadAllLines(differencesGamePath)
                : Array.Empty<string>();

            var lcuLines = File.Exists(differencesLcuPath)
                ? File.ReadAllLines(differencesLcuPath)
                : Array.Empty<string>();

            var gameAssetsRelativePaths = filterAssetsByType(gameLines, selectedAssetTypes);
            var lcuAssetsRelativePaths = filterAssetsByType(lcuLines, selectedAssetTypes);

            foreach (var relativePath in gameAssetsRelativePaths)
            {
                string adjustedPath = AssetUrlRules.Adjust(relativePath);
                if (string.IsNullOrEmpty(adjustedPath)) continue;

                string fullUrl = $"{BASE_GAME_URL}{adjustedPath}";
                string assetName = Path.GetFileName(adjustedPath);

                allAssets.Add(new AssetInfo
                {
                    Name = assetName,
                    Url = fullUrl
                });
            }

            foreach (var relativePath in lcuAssetsRelativePaths)
            {
                string adjustedPath = AssetUrlRules.Adjust(relativePath);
                if (string.IsNullOrEmpty(adjustedPath)) continue;

                string fullUrl = $"{BASE_LCU_URL}{adjustedPath}";
                string assetName = Path.GetFileName(adjustedPath);

                allAssets.Add(new AssetInfo
                {
                    Name = assetName,
                    Url = fullUrl
                });
            }

            _logService.LogDebug($"AssetsPreview: Total assets collected for preview: {allAssets.Count}");
            return allAssets;
        }

        public async Task<PreviewData> GetPreviewData(string assetUrl, string assetName)
        {
            string assetExtension = Path.GetExtension(assetName)?.ToLower();

            switch (assetExtension)
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".gif":
                    try
                    {
                        var result = await _assetDownloader.CheckAssetAvailabilityAsync(assetUrl);
                        if (result.IsAvailable)
                        {
                            return new PreviewData
                            {
                                ContentType = AssetContentType.Image,
                                AssetUrl = assetUrl
                            };
                        }

                        return new PreviewData
                        {
                            ContentType = AssetContentType.NotFound,
                            Message = "Asset Not Found in the Server"
                        };
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError(ex, "Error checking image availability");

                        return new PreviewData
                        {
                            ContentType = AssetContentType.NotFound,
                            Message = "Asset Not Found in the Server"
                        };
                    }

                case ".txt":
                case ".json":
                case ".xml":
                    try
                    {
                        var textContent = await _assetDownloader.DownloadAssetTextAsync(assetUrl);
                        if (textContent != null)
                        {
                            return new PreviewData
                            {
                                ContentType = AssetContentType.Text,
                                TextContent = textContent
                            };
                        }

                        return new PreviewData
                        {
                            ContentType = AssetContentType.NotFound,
                            Message = "Asset Not Found in the Server"
                        };
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError(ex, "Error downloading text content.");

                        return new PreviewData
                        {
                            ContentType = AssetContentType.NotFound,
                            Message = "Asset Not Found in the Server"
                        };
                    }

                default:
                    string localPath = await _assetDownloader.DownloadAssetIfNeeded(assetUrl, assetName);

                    if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath))
                    {
                        return new PreviewData
                        {
                            ContentType = AssetContentType.NotFound,
                            Message = "Asset Not Found in the Server"
                        };
                    }

                    return assetExtension switch
                    {
                        ".wav" or ".mp3" or ".ogg" => new PreviewData
                        {
                            ContentType = AssetContentType.Audio,
                            LocalFilePath = localPath
                        },
                        ".mp4" or ".webm" => new PreviewData
                        {
                            ContentType = AssetContentType.Video,
                            LocalFilePath = localPath
                        },
                        _ => new PreviewData
                        {
                            ContentType = AssetContentType.ExternalProgram,
                            LocalFilePath = localPath,
                            Message = $"Asset type '{assetExtension}' not directly supported for preview. Will attempt to open with default application."
                        }
                    };
            }
        }
    }
}

