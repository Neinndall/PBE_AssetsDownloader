using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PBE_AssetsDownloader.Utils;
using Serilog; // Added Serilog using directive

namespace PBE_AssetsDownloader.Services
{
    public class AssetDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly LogService _logService;

        public List<string> ExcludedExtensions => _excludedExtensions;

        private readonly List<string> _excludedExtensions = new()
        {
            ".luabin", ".luabin64", ".preload", ".scb",
            ".sco", ".skl", ".mapgeo", ".subchunktoc", ".stringtable",
            ".anm", ".dat", ".bnk", ".wpk",
            ".cfg", ".cfgbin"
        };

        // Eventos de progreso: ahora con total global, progreso acumulado, éxito y mensaje de error
        public event Action<int> DownloadStarted;
        public event Action<int, int, string, bool, string> DownloadProgressChanged;
        public event Action DownloadCompleted;

        public AssetDownloader(HttpClient httpClient, DirectoriesCreator directoriesCreator, LogService logService)
        {
            _httpClient = httpClient;
            _directoriesCreator = directoriesCreator;
            _logService = logService;
        }

        // Métodos públicos para notificar eventos (para ser llamados desde otras clases)
        public void NotifyDownloadStarted(int totalFiles)
        {
            DownloadStarted?.Invoke(totalFiles);
        }

        public void NotifyDownloadProgressChanged(int completedFiles, int totalFiles, string currentFileName, bool isSuccess, string errorMessage)
        {
            DownloadProgressChanged?.Invoke(completedFiles, totalFiles, currentFileName, isSuccess, errorMessage);
        }

        public void NotifyDownloadCompleted()
        {
            DownloadCompleted?.Invoke();
        }

        // Modificado para aceptar el total global y un offset de archivos completados
        public async Task<List<string>> DownloadAssets(IEnumerable<(string, string)> assets, string downloadDirectory, List<string> notFoundAssets, int overallTotalFiles, int completedFilesOffset)
        {
            var assetsList = assets.ToList();
            int currentBatchTotalFiles = assetsList.Count;
            int currentBatchCompletedFiles = 0;

            _logService.Log("Starting download of assets ...");
            foreach (var (relativePath, baseUrl) in assetsList)
            {
                string url = baseUrl + relativePath;
                string originalUrl = url;

                url = AssetUrlRules.Adjust(url);

                if (string.IsNullOrEmpty(url))
                {
                    _logService.LogDebug($"Asset skipped due to empty URL after adjustment: {originalUrl}");
                    continue;
                }

                // DownloadFileAsync now returns a tuple with success status and error message
                var (success, errorMsg) = await DownloadFileAsync(url, downloadDirectory, originalUrl);
                if (!success)
                {
                    notFoundAssets.Add(originalUrl);
                }

                currentBatchCompletedFiles++;
                // Disparamos el progreso con el total global, el progreso acumulado, el éxito y el mensaje de error
                NotifyDownloadProgressChanged(completedFilesOffset + currentBatchCompletedFiles, overallTotalFiles, Path.GetFileName(url), success, errorMsg);
            }

            return notFoundAssets;
        }

        // DownloadFileAsync now returns a tuple (bool success, string errorMessage)
        public async Task<(bool success, string errorMessage)> DownloadFileAsync(string url, string downloadDirectory, string originalUrl)
        {
            string fileName = Path.GetFileName(url);
            string extensionFolder = _directoriesCreator.CreateAssetDirectoryPath(url, downloadDirectory);
            string filePath = Path.Combine(extensionFolder, fileName);

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                    Serilog.Log.Information($"Downloaded: {fileName}"); // Log to file only
                    return (true, null);
                }
                else
                {
                    string errorMsg = $"Failed to download '{fileName}'. Reason: {response.StatusCode} - {response.ReasonPhrase}";
                    Serilog.Log.Error(errorMsg); // Log to file only
                    return (false, errorMsg);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error downloading {url}: {ex.Message}";
                Serilog.Log.Error(ex, errorMsg); // Log to file only
                return (false, errorMsg);
            }
        }

        public async Task<string> DownloadAssetTextAsync(string assetUrl)
        {
            try
            {
                var response = await _httpClient.GetAsync(assetUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    _logService.LogError($"Failed to download text content from {assetUrl}. Status Code: {response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Error downloading text content from {assetUrl}");
                return null;
            }
        }

        public async Task<(bool IsAvailable, string ErrorMessage)> CheckAssetAvailabilityAsync(string assetUrl)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Head, assetUrl))
                {
                    var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    if (response.IsSuccessStatusCode)
                    {
                        return (true, null);
                    }
                    else
                    {
                        var errorMessage = $"Asset not available. Status: {response.StatusCode}";
                        return (false, errorMessage);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                var errorMessage = $"HTTP request failed while checking {assetUrl}: {ex.Message}";
                _logService.LogError(errorMessage);
                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                var errorMessage = $"An unexpected error occurred while checking {assetUrl}: {ex.Message}";
                _logService.LogError(errorMessage);
                return (false, errorMessage);
            }
        }

        public async Task<string> DownloadAssetIfNeeded(string assetUrl, string assetName)
        {
            // Creamos la carpeta necesaria + Mensaje de creacion
            await _directoriesCreator.CreatePreviewAssetsAsync();

            // Obtener la ruta de la carpeta de preview assets
            string previewAssetsPath = _directoriesCreator.PreviewAssetsPath;

            string localFilePath = Path.Combine(previewAssetsPath, assetName);

            if (File.Exists(localFilePath))
            {
                _logService.LogDebug($"Asset already exists locally: {localFilePath}");
                return localFilePath;
            }

            try
            {
                var response = await _httpClient.GetAsync(assetUrl);

                if (response.IsSuccessStatusCode)
                {
                    // Corrected FileStream constructor call
                    await using (var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                    return localFilePath;
                }
                else
                {
                    _logService.LogError($"Failed to download '{assetName}' from {assetUrl}. Status code: {response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logService.LogError(httpEx, $"HTTP error downloading '{assetName}' from {assetUrl}.");
                return null;
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"General error downloading asset '{assetName}' from {assetUrl}.");
                return null;
            }
        }
    }
}
