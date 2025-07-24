using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog; // Aunque no se usa directamente en este archivo, es bueno mantenerlo si otros lo usan
using PBE_AssetsDownloader.Services; // Asegúrate de tener este using para LogService

namespace PBE_AssetsDownloader.Utils
{
    public class DirectoriesCreator
    {
        public string ResourcesPath { get; private set; }
        public string SubAssetsDownloadedPath { get; }
        private readonly LogService _logService; // Campo para almacenar la instancia de LogService

        // Constructor por defecto (sin argumentos) - Mantenerlo si lo necesitas en algún otro lugar
        // Considera si este constructor es realmente necesario si siempre inyectas LogService.
        // Si no se usa, es mejor eliminarlo para forzar la inyección de LogService.
        public DirectoriesCreator(LogService logService)
        {
            _logService = logService;

            // Mueve la inicialización de Paths a este constructor también
            // O crea un método InitializePaths() que ambos constructores llamen.
            string date = DateTime.Now.ToString("dd-M-yyyy.H.mm.ss");
            SubAssetsDownloadedPath = Path.Combine("AssetsDownloaded", date);
            ResourcesPath = Path.Combine("Resources", date);
        }


        public Task CreateDirResourcesAsync() => CreateFoldersAsync(ResourcesPath);

        public Task CreateDirAssetsDownloadedAsync() => CreateFoldersAsync(SubAssetsDownloadedPath);

        public Task CreateHashesNewDirectoryAsync() => CreateFoldersAsync(Path.Combine("hashes", "new"));

        public Task CreateBackUpOldHashesAsync() => CreateFoldersAsync(Path.Combine("hashes", "olds", "BackUp", DateTime.Now.ToString("dd-M-yyyy.H.mm.ss")));

        public string GetHashesNewsDirectoryPath() => Path.Combine("hashes", "new");

        public string GetBackUpOldHashesPath() => Path.Combine("hashes", "olds", "BackUp", DateTime.Now.ToString("dd-M-yyyy.H.mm.ss"));

        public async Task CreateAllDirectoriesAsync()
        {
            await CreateDirResourcesAsync();
            await CreateDirAssetsDownloadedAsync();
            await CreateHashesNewDirectoryAsync();
        }

        public string CreateAssetDirectoryFromPath(string url, string downloadDirectory)
        {
            // ... (tu código existente para CreateAssetDirectoryFromPath)
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

        // Modifica este método para usar _logService si está disponible
        private Task CreateFoldersAsync(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    // MODIFICACIÓN: Usar interpolación de cadenas
                    _logService.Log($"Directory created: {path}");
                }
            }
            catch (Exception e)
            {
                // MODIFICACIÓN: Usar interpolación de cadenas para el mensaje
                _logService.LogError(e, $"Error during directory creation for path: {path}");
            }

            return Task.CompletedTask;
        }
    }
}