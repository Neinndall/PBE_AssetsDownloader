using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace PBE_NewFileExtractor.Utils
{
    public class DirectoriesCreator
    {
        private const string BaseAssetsDownloadedPath = @"AssetsDownloaded";
        public string SubAssetsDownloadedPath { get; }

        public DirectoriesCreator()
        {
            SubAssetsDownloadedPath = Path.Combine(BaseAssetsDownloadedPath, DateTime.Now.ToString("dd-M-yyyy--HH-mm"));
        }

        public Task CreateDirResourcesAsync()
        {
            return Task.Run(() => CreateFolders(Path.Combine("Resources")));
        }

        public Task CreateDirAssetsDownloadedAsync()
        {
            return Task.Run(() => CreateFolders(BaseAssetsDownloadedPath));
        }

        public Task CreateSubAssetsDownloadedFolderAsync()
        {
            return Task.Run(() => CreateFolders(SubAssetsDownloadedPath));
        }

        public Task CreateHashesNewDirectoryAsync()
        {
            return Task.Run(() => CreateFolders(Path.Combine("hashes", "new")));
        }

        public string GetHashesNewsDirectoryPath()
        {
            return Path.Combine("hashes", "new");
        }

        // Método para crear todos los directorios necesarios
        public async Task CreateAllDirectoriesAsync()
        {
            await CreateDirResourcesAsync();
            await CreateDirAssetsDownloadedAsync();
            await CreateSubAssetsDownloadedFolderAsync();
            await CreateHashesNewDirectoryAsync(); // Asegura que se cree el directorio hashes/new/
        }

        private static void CreateFolders(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);

                    // Simplificar la ruta quitando "./" o ".\"
                    string simplifiedPath = SimplifyPath(path);
                    Log.Information("Directory created: {0}", simplifiedPath);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error during directory creation: {0}", path);
            }
        }

        private static string SimplifyPath(string path)
        {
            // Quitar ".\" o "./" del inicio de la ruta si está presente
            if (path.StartsWith(@".\"))
            {
                path = path.Substring(2);
            }
            else if (path.StartsWith("./"))
            {
                path = path.Substring(2);
            }

            return path;
        }
    }
}
