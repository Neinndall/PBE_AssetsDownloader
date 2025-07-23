using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
// Eliminamos 'using Serilog;'
// using Serilog; 
using PBE_AssetsDownloader.Utils; // Asegúrate de que AssetUrlRules y DirectoriesCreator estén aquí

namespace PBE_AssetsDownloader.Services
{
    public class AssetDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly LogService _logService; // Instancia de LogService inyectada

        public List<string> ExcludedExtensions => _excludedExtensions;
        
        // Lista de extensiones excluidas
        private readonly List<string> _excludedExtensions = new() 
        {
            ".luabin", ".luabin64", ".preload", ".scb",
            ".sco", ".skl", ".mapgeo", ".subchunktoc", ".stringtable",
            ".anm", ".dat", ".bnk", ".wpk", 
            ".cfg", ".cfgbin"
        };

        // Constructor: Ahora recibe LogService como dependencia
        public AssetDownloader(HttpClient httpClient, DirectoriesCreator directoriesCreator, LogService logService)
        {
            _httpClient = httpClient; // Usando tu sintaxis preferida
            _directoriesCreator = directoriesCreator; // Usando tu sintaxis preferida
            _logService = logService; // Asignamos el LogService inyectado
        }

        // Eliminamos el parámetro logAction
        public async Task<List<string>> DownloadAssets(IEnumerable<string> differences, string baseUrl, string downloadDirectory, List<string> notFoundAssets)
        {
            // notFoundAssets ya es la lista que se pasa y se espera que se modifique.
            // La variable local NotFoundAssets era redundante.

            if (!Directory.Exists(downloadDirectory))
            {
                // Usar _directoriesCreator para crear directorios si es su responsabilidad,
                // o simplemente Directory.CreateDirectory si es una operación básica aquí.
                // Por ahora, se mantiene Directory.CreateDirectory.
                _logService.LogDebug($"Creating download directory: {downloadDirectory}");
                Directory.CreateDirectory(downloadDirectory);
            }

            foreach (var line in differences)
            {
                string[] parts = line.Split(' ');
                string relativePath = parts.Length >= 2 ? parts[1] : parts[0]; 

                string url = baseUrl + relativePath;
                string originalUrl = url;

                url = AssetUrlRules.Adjust(url);

                if (string.IsNullOrEmpty(url))
                {
                    _logService.LogDebug($"Asset skipped due to empty URL after adjustment: {originalUrl}"); // Log para depuración
                    continue; // No añadir a NotFounds.txt si fue ignorada
                }
                
                // Llamamos a DownloadFileAsync sin pasar logAction
                var result = await DownloadFileAsync(url, downloadDirectory, originalUrl); 
                if (!result)
                {
                    // Añadir a la lista notFoundAssets que se pasa como parámetro
                    notFoundAssets.Add(originalUrl); 
                }
            }
            
            return notFoundAssets; // Devolver la lista que se ha modificado
        }

        // Eliminamos el parámetro logAction
        public async Task<bool> DownloadFileAsync(string url, string downloadDirectory, string originalUrl)
        {
            try
            {
                var fileName = Path.GetFileName(url);
                // var finalExtension = Path.GetExtension(fileName); // No se usa, se puede eliminar

                string extensionFolder = _directoriesCreator.CreateAssetDirectoryFromPath(url, downloadDirectory);
                var filePath = Path.Combine(extensionFolder, fileName);

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                    _logService.Log($"Downloaded: {fileName}"); // Usamos _logService directamente
                    return true;
                }
                else
                {
                    _logService.LogError($"Failed to download '{fileName}' from {url}. Status Code: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Error downloading {url}"); 
                return false;
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
                        // No log needed here, just return the message for the UI.
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

        public async Task<string> DownloadAssetIfNeeded(string assetUrl, string previewAssetsPath, string assetName)
        {
            if (!Directory.Exists(previewAssetsPath))
            {
                _logService.LogDebug($"Creating preview assets directory: {previewAssetsPath}");
                Directory.CreateDirectory(previewAssetsPath);
            }

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
                    await using (var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write))
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