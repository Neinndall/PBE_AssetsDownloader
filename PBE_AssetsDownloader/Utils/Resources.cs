// PBE_AssetsDownloader/Utils/Resources.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader.Utils
{
    public class Resources
    {
        private readonly HttpClient _httpClient;
        private readonly AssetDownloader _assetDownloader; // Ahora será inyectado
        private readonly DirectoriesCreator _directoryCreator;
        private readonly LogService _logService; 

        // Constructor: Ahora recibe AssetDownloader como dependencia adicional
        public Resources(HttpClient httpClient, DirectoriesCreator directoryCreator, LogService logService, AssetDownloader assetDownloader)
        {
            _httpClient = httpClient;
            _directoryCreator = directoryCreator;
            _logService = logService;
            _assetDownloader = assetDownloader; // ¡Asignamos la instancia inyectada!
        }

        public async Task GetResourcesFiles()
        {
            await _directoryCreator.CreateDirAssetsDownloadedAsync();

            var resourcesPath = _directoryCreator.ResourcesPath;
            var differencesGameFilePath = Path.Combine(resourcesPath, "differences_game.txt");
            var differencesLcuFilePath = Path.Combine(resourcesPath, "differences_lcu.txt");

            if (!File.Exists(differencesGameFilePath) || !File.Exists(differencesLcuFilePath))
            {
                _logService.LogError("One or more diff files do not exist in the resources path.");
                return;
            }

            try
            {
                var gameDifferences = await File.ReadAllLinesAsync(differencesGameFilePath);
                var lcuDifferences = await File.ReadAllLinesAsync(differencesLcuFilePath);
                var downloadDirectory = _directoryCreator.SubAssetsDownloadedPath;

                var notFoundGameAssets = new List<string>(); 
                var notFoundLcuAssets = new List<string>();

                // Usamos el _assetDownloader inyectado
                await _assetDownloader.DownloadAssets(gameDifferences, "https://raw.communitydragon.org/pbe/game/", downloadDirectory, notFoundGameAssets);
                await _assetDownloader.DownloadAssets(lcuDifferences, "https://raw.communitydragon.org/pbe/", downloadDirectory, notFoundLcuAssets);

                await SaveNotFoundAssets(notFoundGameAssets, notFoundLcuAssets, resourcesPath);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Error processing difference files in Resources: {ex.Message}");
                throw; 
            }
        }

        private async Task SaveNotFoundAssets(List<string> notFoundGameAssets, List<string> notFoundLcuAssets, string resourcesPath)
        {
            if (string.IsNullOrWhiteSpace(resourcesPath))
                throw new ArgumentException("The resource path is not defined.", nameof(resourcesPath));

            var modifiedNotFoundGameAssets = notFoundGameAssets
                .Select(url => url.EndsWith(".dds") ? url.Replace(".dds", ".png") : url)
                .Select(url => url.EndsWith(".tex") ? url.Replace(".tex", ".png") : url)
                .ToList();

            var allNotFoundAssets = modifiedNotFoundGameAssets
                .Concat(notFoundLcuAssets)
                .ToList();
            
            var notFoundFilePath = Path.Combine(resourcesPath, "NotFounds.txt");

            try
            {
                await File.WriteAllLinesAsync(notFoundFilePath, allNotFoundAssets);
                _logService.Log($"Successfully saved not found assets to: {notFoundFilePath}");
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Error saving NotFounds.txt: {ex.Message}");
                throw;
            }
        }
    }
}