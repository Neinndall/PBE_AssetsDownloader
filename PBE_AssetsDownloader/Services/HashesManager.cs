using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace PBE_AssetsDownloader.Services
{
    public class HashesManager
    {
        private readonly string _oldHashesDirectory;
        private readonly string _newHashesDirectory;
        private readonly string _resourcesPath;
        private readonly List<string> _excludedExtensions;

        // Modificar constructor para aceptar resourcesPath
        public HashesManager(string oldHashesDirectory, string newHashesDirectory, string resourcesPath, List<string> excludedExtensions)
        {
            _oldHashesDirectory = oldHashesDirectory;
            _newHashesDirectory = newHashesDirectory;
            _resourcesPath = resourcesPath;
            _excludedExtensions = excludedExtensions;
        }

        public async Task CompareHashesAsync()
        {
            // Mensaje indicando que estamos revisando y filtrando hashes
            Log.Information("Comparing and filtering hashes, please wait...");

            // Combina las rutas usando los directorios proporcionados
            string oldGameHashesPath = Path.Combine(_oldHashesDirectory, "hashes.game.txt");
            string oldLcuHashesPath = Path.Combine(_oldHashesDirectory, "hashes.lcu.txt");
            string newGameHashesPath = Path.Combine(_newHashesDirectory, "hashes.game.txt");
            string newLcuHashesPath = Path.Combine(_newHashesDirectory, "hashes.lcu.txt");

            // Lee los archivos de hashes de forma asíncrona
            var oldGameHashesTask = File.ReadAllLinesAsync(oldGameHashesPath);
            var oldLcuHashesTask = File.ReadAllLinesAsync(oldLcuHashesPath);
            var newGameHashesTask = File.ReadAllLinesAsync(newGameHashesPath);
            var newLcuHashesTask = File.ReadAllLinesAsync(newLcuHashesPath);

            await Task.WhenAll(oldGameHashesTask, oldLcuHashesTask, newGameHashesTask, newLcuHashesTask);

            var oldGameHashes = await oldGameHashesTask;
            var oldLcuHashes = await oldLcuHashesTask;
            var newGameHashes = await newGameHashesTask;
            var newLcuHashes = await newLcuHashesTask;

            // Convertir oldHashes a HashSet para mejorar la búsqueda
            var oldGameHashesSet = new HashSet<string>(oldGameHashes);
            var oldLcuHashesSet = new HashSet<string>(oldLcuHashes);

            // Comparar hashes en paralelo
            var differencesGame = new ConcurrentBag<string>();
            var differencesLcu = new ConcurrentBag<string>();

            Parallel.ForEach(newGameHashes, newHash =>
            {
                if (!oldGameHashesSet.Contains(newHash))
                {
                    differencesGame.Add(newHash);
                }
            });

            Parallel.ForEach(newLcuHashes, newHash =>
            {
                if (!oldLcuHashesSet.Contains(newHash))
                {
                    differencesLcu.Add(newHash);
                }
            });

            // Llamar al método de filtrado para las diferencias de Game
            await FilterAndSaveDifferencesAsync(differencesGame.ToList(), differencesLcu.ToList());
        }

        // Método para filtrar y guardar las diferencias después de eliminar duplicados .tex
        public async Task FilterAndSaveDifferencesAsync(List<string> differencesGame, List<string> differencesLcu)
        {
            // Lee los hashes antiguos de 'hashes.game.txt'
            string oldHashesPath = Path.Combine(_oldHashesDirectory, "hashes.game.txt");
            var oldHashes = await File.ReadAllLinesAsync(oldHashesPath);
            
            // Convertir oldHashes a HashSet para mejorar la búsqueda
            var oldHashesSet = new HashSet<string>(oldHashes);

            // Filtrar las líneas que terminan en .tex o están en la lista de extensiones excluidas
            var filteredDifferencesGame = new List<string>();

            foreach (var line in differencesGame)
            {
                var parts = line.Split(' ');
                var filePath = parts[1]; // Asumiendo que la ruta está en la segunda posición

                // Verificamos si la extensión del archivo está en la lista de exclusiones
                var fileExtension = Path.GetExtension(filePath);
                if (_excludedExtensions.Contains(fileExtension))
                {
                    continue;  // Excluir si está en las extensiones excluidas
                }

                if (filePath.EndsWith(".tex"))
                {
                    // Extraemos el nombre base del archivo .tex (sin la extensión)
                    string baseFilePath = filePath.Replace(".tex", "");
                    // Verificamos si ya existe en los viejos hashes
                    bool foundInOldHashes = oldHashesSet.Any(oldHash => oldHash.Contains(baseFilePath));
                    // Si no existe en los viejos hashes, lo añadimos a las diferencias filtradas
                    if (!foundInOldHashes)
                    {
                        filteredDifferencesGame.Add(line);  // Agregar si no está en los viejos hashes
                    }
                }
                else
                {
                    filteredDifferencesGame.Add(line);  // Agregar si no es un archivo .tex
                }
            }

            // Guardar las diferencias filtradas
            await File.WriteAllLinesAsync(Path.Combine(_resourcesPath, "differences_game.txt"), filteredDifferencesGame);
            await File.WriteAllLinesAsync(Path.Combine(_resourcesPath, "differences_lcu.txt"), differencesLcu);

            Log.Information("Filtered differences saved to {0}", Path.Combine(_resourcesPath, "differences_game.txt"));
            Log.Information("Filtered differences saved to {0}", Path.Combine(_resourcesPath, "differences_lcu.txt"));
        }
    }
}