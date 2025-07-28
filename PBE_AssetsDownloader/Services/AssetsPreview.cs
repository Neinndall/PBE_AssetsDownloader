// PBE_AssetsDownloader/Services/AssetsPreview.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Serilog; // Necesario para el Log estático en Dispose si no usas _logger aquí.
using PBE_AssetsDownloader.Info;


namespace PBE_AssetsDownloader.Services
{
    // Define a simple enum or DTO to tell the UI what type of content to display
    public enum AssetContentType
    {
        Image,
        Audio,
        Video,
        Text,
        ExternalProgram, // To be opened by the default system application
        Unsupported,
        NotFound // Asset not found locally
    }

    // A simple DTO to pass preview information to the UI
    public class PreviewData
    {
        public AssetContentType ContentType { get; set; }
        public string LocalFilePath { get; set; } // Path for external files
        public string AssetUrl { get; set; } // URL for images
        public string TextContent { get; set; } // Content for text files
        public string Message { get; set; } // Optional message
    }

    public class AssetsPreview
    {
        private readonly LogService _logService;
        private readonly AssetDownloader _assetDownloader;
        private readonly string _previewAssetsPath;
        private readonly Func<IEnumerable<string>, List<string>, List<string>> _filterAssetsByType;

        private const string BASE_GAME_URL = "https://raw.communitydragon.org/pbe/game/";
        private const string BASE_LCU_URL = "https://raw.communitydragon.org/pbe/";

        public AssetsPreview(
            AssetDownloader assetDownloader, 
            Func<IEnumerable<string>, List<string>, List<string>> filterAssetsByType, 
            LogService logService)
        {
            _logService = logService;
            _assetDownloader = assetDownloader;
            _filterAssetsByType = filterAssetsByType;
            
            string baseApplicationPath = AppDomain.CurrentDomain.BaseDirectory;
            _previewAssetsPath = Path.Combine(baseApplicationPath, "PBE_PreviewAssets", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(_previewAssetsPath);
        }

        public List<AssetInfo> GetAssetsForPreview(string inputFolder, List<string> selectedAssetTypes)
        {
            // Using _logService instance instead of static Log
            _logService.LogDebug($"AssetsPreview: Starting GetAssetsForPreview. Input folder: '{inputFolder}'");

            var allAssets = new List<AssetInfo>();

            var differencesGamePath = Path.Combine(inputFolder, "differences_game.txt");
            var differencesLcuPath = Path.Combine(inputFolder, "differences_lcu.txt");

            _logService.LogDebug($"AssetsPreview: Checking game differences file at '{differencesGamePath}'. Exists: {File.Exists(differencesGamePath)}");
            _logService.LogDebug($"AssetsPreview: Checking lcu differences file at '{differencesLcuPath}'. Exists: {File.Exists(differencesLcuPath)}");

            var gameLines = File.Exists(differencesGamePath) ? File.ReadAllLines(differencesGamePath) : Array.Empty<string>();
            var lcuLines = File.Exists(differencesLcuPath) ? File.ReadAllLines(differencesLcuPath) : Array.Empty<string>();

            _logService.LogDebug($"AssetsPreview: Read {gameLines.Length} game lines and {lcuLines.Length} lcu lines.");

            // Usa la función FilterAssetsByType que se pasa desde ExportWindow
            var gameAssetsRelativePaths = _filterAssetsByType(gameLines, selectedAssetTypes);
            var lcuAssetsRelativePaths = _filterAssetsByType(lcuLines, selectedAssetTypes);

            _logService.LogDebug($"AssetsPreview: Filtered {gameAssetsRelativePaths.Count} game asset paths and {lcuAssetsRelativePaths.Count} lcu asset paths.");

            foreach (var relativePath in gameAssetsRelativePaths)
            {
                string adjustedPath = PBE_AssetsDownloader.Utils.AssetUrlRules.Adjust(relativePath);
                if (string.IsNullOrEmpty(adjustedPath)) continue;

                string fullUrl = $"{BASE_GAME_URL}{adjustedPath}";
                string assetName = Path.GetFileName(adjustedPath);
                allAssets.Add(new AssetInfo { Name = assetName, Url = fullUrl });
            }

            foreach (var relativePath in lcuAssetsRelativePaths)
            {
                string adjustedPath = PBE_AssetsDownloader.Utils.AssetUrlRules.Adjust(relativePath);
                if (string.IsNullOrEmpty(adjustedPath)) continue;

                string fullUrl = $"{BASE_LCU_URL}{adjustedPath}";
                string assetName = Path.GetFileName(adjustedPath);
                allAssets.Add(new AssetInfo { Name = assetName, Url = fullUrl });
            }

            _logService.LogDebug($"AssetsPreview: Total assets collected for preview: {allAssets.Count}");
            return allAssets;
        }

        // Este método se encarga de la descarga y de determinar el tipo de contenido
        // Ya NO recibe un 'Border' para mostrar. Devuelve un objeto PreviewData.
        public async Task<PreviewData> GetPreviewData(string assetUrl, string assetName)
        {
            string assetExtension = Path.GetExtension(assetName)?.ToLower();

            switch (assetExtension)
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".gif":
                    // Intentamos verificar si la imagen es accesible
                    try
                    {
                        var result = await _assetDownloader.CheckAssetAvailabilityAsync(assetUrl);
                        if (result.IsAvailable)
                        {
                            return new PreviewData { ContentType = AssetContentType.Image, AssetUrl = assetUrl };
                        }
                        else
                        {
                            return new PreviewData { ContentType = AssetContentType.NotFound, Message = result.ErrorMessage };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError($"Error checking image availability: {ex.Message}");
                        return new PreviewData { ContentType = AssetContentType.NotFound, Message = "Could not verify image availability." };
                    }

                case ".txt":
                case ".json":
                case ".xml":
                    try
                    {
                        var textContent = await _assetDownloader.DownloadAssetTextAsync(assetUrl);
                        if (textContent != null)
                        {
                            return new PreviewData { ContentType = AssetContentType.Text, TextContent = textContent };
                        }
                        else
                        {
                            return new PreviewData { ContentType = AssetContentType.NotFound, Message = "Could not download text content." };
                        }
                    }
                    catch (Exception)
                    {
                        return new PreviewData { ContentType = AssetContentType.NotFound, Message = "Error downloading text content." };
                    }

                default:
                    string localPath = await _assetDownloader.DownloadAssetIfNeeded(assetUrl, _previewAssetsPath, assetName);
                    if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath))
                    {
                        return new PreviewData { ContentType = AssetContentType.NotFound, Message = "Asset could not be found or downloaded." };
                    }

                    return assetExtension switch
                    {
                        ".wav" or ".mp3" or ".ogg" => new PreviewData { ContentType = AssetContentType.Audio, LocalFilePath = localPath },
                        ".mp4" or ".webm" => new PreviewData { ContentType = AssetContentType.Video, LocalFilePath = localPath },
                        _ => new PreviewData { ContentType = AssetContentType.ExternalProgram, LocalFilePath = localPath, Message = $"Asset type '{assetExtension}' not directly supported for preview. Will attempt to open with default application." }
                    };
            }
        }

        
    }
}