﻿using System;
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

        public Resources(HttpClient httpClient, DirectoriesCreator directoryCreator)
        {
            _httpClient = httpClient;
            _assetDownloader = new AssetDownloader(_httpClient, directoryCreator);
        }

        public async Task GetResourcesFiles()
        {
            var directoryCreator = new DirectoriesCreator();
            await directoryCreator.CreateDirAssetsDownloadedAsync();

            // Obtener la ruta de Resources con timestamp
            var resourcesPath = directoryCreator.ResourcesPath;
            var differencesGameFilePath = Path.Combine(resourcesPath, "differences_game.txt");
            var differencesLcuFilePath = Path.Combine(resourcesPath, "differences_lcu.txt");

            // Verificar si los archivos de diferencias existen
            if (!File.Exists(differencesGameFilePath) || !File.Exists(differencesLcuFilePath))
            {
                Log.Error("Uno o más archivos de diferencias no existen.");
                return;
            }

            try
            {
                // Leer las líneas de los archivos de diferencias
                var gameDifferences = await File.ReadAllLinesAsync(differencesGameFilePath);
                var lcuDifferences = await File.ReadAllLinesAsync(differencesLcuFilePath);
                var downloadDirectory = directoryCreator.SubAssetsDownloadedPath;

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
                Log.Error($"Error al procesar los archivos de diferencias: {ex.Message}");
                throw;
            }
        }

        private async Task SaveNotFoundAssets(List<string> notFoundGameAssets, List<string> notFoundLcuAssets, string resourcesPath)
        {
            if (string.IsNullOrWhiteSpace(resourcesPath))
                throw new ArgumentException("La ruta de recursos no está definida.", nameof(resourcesPath));

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