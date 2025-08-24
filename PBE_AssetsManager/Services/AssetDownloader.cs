using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Serilog;
using System.Threading.Tasks;
using PBE_AssetsManager.Utils;

namespace PBE_AssetsManager.Services
{
    public class AssetDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly LogService _logService;
        public event Action<int> DownloadStarted;
        public event Action<int, int, string, bool, string> DownloadProgressChanged;
        public event Action DownloadCompleted;

        public AssetDownloader(HttpClient httpClient, DirectoriesCreator directoriesCreator, LogService logService)
        {
            _httpClient = httpClient;
            _directoriesCreator = directoriesCreator;
            _logService = logService;
        }

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
            _logService.LogSuccess("Download of assets completed.");
            DownloadCompleted?.Invoke();
        }

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

                var (success, errorMsg) = await DownloadFileAsync(url, downloadDirectory, originalUrl);
                if (!success)
                {
                    notFoundAssets.Add(originalUrl);
                }

                currentBatchCompletedFiles++;
                NotifyDownloadProgressChanged(completedFilesOffset + currentBatchCompletedFiles, overallTotalFiles, Path.GetFileName(url), success, errorMsg);
            }

            return notFoundAssets;
        }

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
                    Log.Information($"Downloaded: {fileName}"); // Log to file only
                    return (true, null);
                }
                else
                {
                    string errorMsg = $"Failed to download '{fileName}'. Reason: {response.StatusCode} - {response.ReasonPhrase}";
                    Log.Error(errorMsg); // Log to file only
                    return (false, errorMsg);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error downloading {url}. See application_errors.log for details.";
                _logService.LogError(errorMsg);
                _logService.LogCritical(ex, $"AssetDownloader.DownloadFileAsync Exception for URL: {url}");
                return (false, ex.Message);
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
                _logService.LogError($"Error downloading text content from {assetUrl}. See application_errors.log for details.");
                _logService.LogCritical(ex, $"AssetDownloader.DownloadAssetTextAsync Exception for URL: {assetUrl}");
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
                    return (response.IsSuccessStatusCode, response.IsSuccessStatusCode ? null : $"Asset not available. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"An error occurred while checking {assetUrl}. See application_errors.log for details.");
                _logService.LogCritical(ex, $"AssetDownloader.CheckAssetAvailabilityAsync Exception for URL: {assetUrl}");
                return (false, ex.Message);
            }
        }

        public async Task<string> DownloadAssetIfNeeded(string assetUrl, string assetName)
        {
            await _directoriesCreator.CreatePreviewAssetsAsync();
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
            catch (Exception ex)
            {
                _logService.LogError($"Error downloading asset '{assetName}' from {assetUrl}. See application_errors.log for details.");
                _logService.LogCritical(ex, $"AssetDownloader.DownloadAssetIfNeeded Exception for URL: {assetUrl}");
                return null;
            }
        }
    }
}