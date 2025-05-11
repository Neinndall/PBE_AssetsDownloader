using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace PBE_AssetsDownloader.Utils
{
    public class DirectoriesCreator
    {
        public string ResourcesPath { get; private set; }
        public string SubAssetsDownloadedPath { get; }

        public DirectoriesCreator()
        {
            string date = DateTime.Now.ToString("dd-M-yyyy.H.mm.ss");  // Con horas, minutos y segundos

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
            // Extraer la ruta del asset desde la URL
            string path = new Uri(url).AbsolutePath; // /pbe/plugins/...

            // Quitar el prefijo "/pbe/"
            if (path.StartsWith("/pbe/"))
            {
                path = path.Substring(5); // Eliminar "/pbe/"
            }

            // Reemplazar "rcp-be-lol-game-data/global/" por "GameData/"
            string patternToReplace = "rcp-be-lol-game-data/global/default/";
            if (path.Contains(patternToReplace))
            {
                path = path.Replace(patternToReplace, "rcp-be-lol-game-data/");
            }

            // Convertir a formato de Windows
            string safePath = path.Replace("/", "\\");

            // Reemplazar caracteres no válidos en la ruta
            foreach (char invalidChar in Path.GetInvalidPathChars())
            {
                safePath = safePath.Replace(invalidChar.ToString(), "_");
            }

            // Crear el path completo
            string fullDirectoryPath = Path.Combine(downloadDirectory, safePath);
            string directory = Path.GetDirectoryName(fullDirectoryPath);

            // Si la carpeta no existe, crearla
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory); // Crear las carpetas necesarias
            }

            return directory; // Devolver la carpeta donde se guardará el archivo
        }


        private static Task CreateFoldersAsync(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    Log.Information("Directory created: {0}", path);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error during directory creation: {0}", path);
            }

            return Task.CompletedTask;
        }
    }
}
