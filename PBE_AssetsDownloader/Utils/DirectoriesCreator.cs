using System;
using System.IO;
using System.Threading.Tasks;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader.Utils
{
    public class DirectoriesCreator
    {
        private readonly LogService _logService;

        public string ResourcesPath { get; private set; }
        public string PreviewAssetsPath { get; private set; }
        public string HashesNewPath { get; private set; }
        public string HashesOldsPaths { get; private set; }
        public string JsonCacheNewPath { get; private set; }
        public string JsonCacheOldPath { get; private set; }
        public string JsonCacheHistoryPath { get; private set; }
        public string SubAssetsDownloadedPath { get; private set; }
        public string BackUpOldHashesPath { get; private set; }

        public DirectoriesCreator(LogService logService)
        {
            _logService = logService;

            string date = DateTime.Now.ToString("dd-M-yyyy.H.mm.ss");
            SubAssetsDownloadedPath = Path.Combine("AssetsDownloaded", date);
            ResourcesPath = Path.Combine("Resources", date);

            HashesNewPath = Path.Combine("hashes", "new");
            HashesOldsPaths = Path.Combine("hashes", "olds");

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolderPath = Path.Combine(appDataPath, "PBE_AssetsDownloader");

            PreviewAssetsPath = Path.Combine(appFolderPath, "PBE_PreviewAssets");
            JsonCacheNewPath = Path.Combine(appFolderPath, "json_cache", "new");
            JsonCacheOldPath = Path.Combine(appFolderPath, "json_cache", "old");
            JsonCacheHistoryPath = Path.Combine(appFolderPath, "json_cache", "history");
            BackUpOldHashesPath = Path.Combine("hashes", "olds", "BackUp", date);
        }

        public Task CreateDirResourcesAsync() => CreateFoldersAsync(ResourcesPath);
        public Task CreateDirSubAssetsDownloadedAsync() => CreateFoldersAsync(SubAssetsDownloadedPath);
        public Task CreateBackUpOldHashesAsync() => CreateFoldersAsync(BackUpOldHashesPath);
        public Task CreatePreviewAssetsAsync() => CreateFoldersAsync(PreviewAssetsPath);
        public Task CreateDirJsonCacheNewAsync() => CreateFoldersAsync(JsonCacheNewPath);
        public Task CreateDirJsonCacheOldAsync() => CreateFoldersAsync(JsonCacheOldPath);
        public Task CreateDirJsonCacheHistoryAsync() => CreateFoldersAsync(JsonCacheHistoryPath);

        public async Task CreateAllDirectoriesAsync()
        {
            await CreateDirSubAssetsDownloadedAsync();
            await CreateDirResourcesAsync();
        }

        public string CreateAssetDirectoryPath(string url, string downloadDirectory)
        {
            string path = new Uri(url).AbsolutePath;

            if (path.StartsWith("/pbe/"))
            {
                path = path.Substring(5); // Eliminar "/pbe/"
            }

            // Reemplazar "rcp-be-lol-game-data/global/" por "rcp-be-lol-game-data/"
            string patternToReplace = "rcp-be-lol-game-data/global/default/";
            if (path.Contains(patternToReplace))
            {
                path = path.Replace(patternToReplace, "rcp-be-lol-game-data/");
            }

            // Mantener la estructura de carpetas en Windows
            string safePath = path.Replace("/", "\\");

            foreach (char invalidChar in Path.GetInvalidPathChars())
            {
                safePath = safePath.Replace(invalidChar.ToString(), "_");
            }

            string fullDirectoryPath = Path.Combine(downloadDirectory, safePath);
            string directory = Path.GetDirectoryName(fullDirectoryPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                // Opcional: _logService.LogDebug($"Created directory for asset: {directory}");
            }

            return directory;
        }

        private Task CreateFoldersAsync(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    // Usar interpolación de cadenas para el mensaje
                    Directory.CreateDirectory(path);
                    _logService.Log($"Directory created successfully at: {path}");
                }
            }
            catch (Exception e)
            {
                // Usar interpolación de cadenas para el mensaje
                _logService.LogError(e, $"Error during directory creation for path: {path}");
            }

            return Task.CompletedTask;
        }
    }
}
