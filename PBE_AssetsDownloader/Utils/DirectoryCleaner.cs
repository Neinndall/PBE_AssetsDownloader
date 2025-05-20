using System;
using System.IO;
using System.Linq;
using Serilog;

namespace PBE_AssetsDownloader.Utils
{
    public class DirectoryCleaner
    {
        private readonly DirectoriesCreator _directoriesCreator;

        // Constructor que recibe una instancia de DirectoriesCreator
        public DirectoryCleaner(DirectoriesCreator directoriesCreator)
        {
            _directoriesCreator = directoriesCreator ?? throw new ArgumentNullException(nameof(directoriesCreator));
        }

        public void CleanEmptyDirectories()
        {
            // Accedemos a SubAssetsDownloadedPath desde la instancia de DirectoriesCreator
            var subAssetsDownloadedPath = _directoriesCreator.SubAssetsDownloadedPath;
            
            if (!Directory.Exists(subAssetsDownloadedPath))
            {
                Log.Warning("The folder doesn't exist: {Path}", subAssetsDownloadedPath);
                return;
            }
                
            string[] rootFoldersToClean = new[]
            {
                Path.Combine(subAssetsDownloadedPath, "plugins"),
                Path.Combine(subAssetsDownloadedPath, "game")
                
            };
        
            foreach (var folder in rootFoldersToClean)
            {
                if (Directory.Exists(folder))
                {
                    DeleteEmptyDirectoriesRecursively(folder);
                }
            }
        }
        
        private void DeleteEmptyDirectoriesRecursively(string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    // La carpeta ya fue eliminada o nunca existió, no se hace nada
                    return;
                }
        
                // Procesamos todas las subcarpetas de forma recursiva
                foreach (var subDir in Directory.GetDirectories(directory))
                {
                    DeleteEmptyDirectoriesRecursively(subDir);
                }
        
                // Si la carpeta está vacía (sin archivos ni subcarpetas), la eliminamos
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    // Eliminar la carpeta vacía
                    Directory.Delete(directory);
                    // Log.Information("Carpeta vacía eliminada: {Directory}", directory); // Creo que no es necesario mostrar los logs
                }
            }
            catch (Exception ex)
            {
                // Solo logueamos si ocurre un error real al intentar procesar una carpeta
                Log.Warning("The folder could not be processed: {Directory}. Error: {Message}", directory, ex.Message);
            }
        }
    }
}
