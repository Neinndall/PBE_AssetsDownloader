using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader.Utils
{
    public class Resources
    {
        private readonly HttpClient _httpClient;
        private readonly AssetDownloader _assetDownloader;
        private readonly DirectoriesCreator _directoryCreator;

        public Resources(HttpClient httpClient, DirectoriesCreator directoryCreator)
        {
            _httpClient = httpClient;
            _directoryCreator = directoryCreator;
            _assetDownloader = new AssetDownloader(_httpClient, _directoryCreator);
        }

        public async Task GetResourcesFiles()
        {
            await _directoryCreator.CreateDirAssetsDownloadedAsync();

            // Obtener la ruta de Resources con timestamp
            var resourcesPath = _directoryCreator.ResourcesPath;
            var differencesGameFilePath = Path.Combine(resourcesPath, "differences_game.txt");
            var differencesLcuFilePath = Path.Combine(resourcesPath, "differences_lcu.txt");

            // Verificar si los archivos de diferencias existen
            if (!File.Exists(differencesGameFilePath) || !File.Exists(differencesLcuFilePath))
            {
                Log.Error("One or more diff files do not exist.");
                return;
            }

            try
            {
                // Leer las líneas de los archivos de diferencias
                var gameDifferences = await File.ReadAllLinesAsync(differencesGameFilePath);
                var lcuDifferences = await File.ReadAllLinesAsync(differencesLcuFilePath);
                var downloadDirectory = _directoryCreator.SubAssetsDownloadedPath;

                // Inicializar las listas para los activos no encontrados
                var notFoundGameAssets = new List<string>();
                var notFoundLcuAssets = new List<string>();

                // Descargar los assets y obtener las URLs no encontradas
                var notFoundGameAssetsResult = await _assetDownloader.DownloadAssets(gameDifferences, "https://raw.communitydragon.org/pbe/game/", downloadDirectory, Log.Information, notFoundGameAssets);
                var notFoundLcuAssetsResult = await _assetDownloader.DownloadAssets(lcuDifferences, "https://raw.communitydragon.org/pbe/", downloadDirectory, Log.Information, notFoundLcuAssets);

                // Guardar las URLs no encontradas
                await SaveNotFoundAssets(notFoundGameAssetsResult, notFoundLcuAssetsResult, resourcesPath);
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing difference files: {ex.Message}");
                throw;
            }
        }

        private async Task SaveNotFoundAssets(List<string> notFoundGameAssets, List<string> notFoundLcuAssets, string resourcesPath)
        {
            if (string.IsNullOrWhiteSpace(resourcesPath))
                throw new ArgumentException("The resource path is not defined.", nameof(resourcesPath));

            // Modificar las URLs no encontradas para los Game Assets
            var modifiedNotFoundGameAssets = notFoundGameAssets
                .Select(url => url.EndsWith(".dds") ? url.Replace(".dds", ".png") : url)
                .Select(url => url.EndsWith(".tex") ? url.Replace(".tex", ".png") : url)
                .ToList();

            // Combinar todas las URLs no encontradas
            var allNotFoundAssets = modifiedNotFoundGameAssets.Concat(notFoundLcuAssets).ToList();
            var notFoundFilePath = Path.Combine(resourcesPath, "NotFounds.txt");

            try
            {
                // Guardar las URLs no encontradas en un archivo
                await File.WriteAllLinesAsync(notFoundFilePath, allNotFoundAssets);
            }
            catch (Exception ex)
            {
                Log.Error($"Error al guardar NotFounds.txt: {ex.Message}");
                throw;
            }
        }
    }
}