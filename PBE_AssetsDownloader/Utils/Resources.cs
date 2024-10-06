using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;
using PBE_NewFileExtractor.Services;

namespace PBE_NewFileExtractor.Utils
{
    public class Resources
    {
        private readonly HttpClient _httpClient;
        private readonly AssetDownloader _assetDownloader;

        public Resources(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _assetDownloader = new AssetDownloader(_httpClient);
        }

        public async Task DownloadAssetsAsync()
        {
            // Definir las rutas para los archivos de diferencias
            string differencesGameFilePath = Path.Combine("Resources", "differences_game.txt");
            string differencesLcuFilePath = Path.Combine("Resources", "differences_lcu.txt");

            var directoryCreator = new DirectoriesCreator();
            await directoryCreator.CreateSubAssetsDownloadedFolderAsync();

            var differencesGameFileLines = await File.ReadAllLinesAsync(differencesGameFilePath);
            var differencesLcuFileLines = await File.ReadAllLinesAsync(differencesLcuFilePath);

            var downloadDirectory = directoryCreator.SubAssetsDownloadedPath;

            var notFoundGameAssets = await _assetDownloader.DownloadAssets(differencesGameFileLines, "https://raw.communitydragon.org/pbe/game/", downloadDirectory, Log.Information);
            var notFoundLcuAssets = await _assetDownloader.DownloadAssets(differencesLcuFileLines, "https://raw.communitydragon.org/pbe/", downloadDirectory, Log.Information);

            // Guardar URLs no encontrados
            await File.WriteAllLinesAsync(Path.Combine("Resources", "NotFounds.txt"), notFoundGameAssets.Concat(notFoundLcuAssets));
        }
    }
}