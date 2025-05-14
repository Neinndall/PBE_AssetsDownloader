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
        // Directorios de los hashes antiguos y nuevos, y el directorio de recursos
        private readonly string _oldHashesDirectory;
        private readonly string _newHashesDirectory;
        private readonly string _resourcesPath;
        private readonly List<string> _excludedExtensions;

        // Constructor que recibe los directorios y las extensiones excluidas
        public HashesManager(string oldHashesDirectory, string newHashesDirectory, string resourcesPath, List<string> excludedExtensions)
        {
            _oldHashesDirectory = oldHashesDirectory;
            _newHashesDirectory = newHashesDirectory;
            _resourcesPath = resourcesPath;
            _excludedExtensions = excludedExtensions;
        }

        // Método principal para comparar los hashes
        public async Task CompareHashesAsync()
        {
            // Mensaje de log indicando que se está comparando y filtrando hashes
            Log.Information("Comparing and filtering hashes, please wait...");

            // Definimos las rutas de los archivos de hashes
            string oldGameHashesPath = Path.Combine(_oldHashesDirectory, "hashes.game.txt");
            string oldLcuHashesPath = Path.Combine(_oldHashesDirectory, "hashes.lcu.txt");
            string newGameHashesPath = Path.Combine(_newHashesDirectory, "hashes.game.txt");
            string newLcuHashesPath = Path.Combine(_newHashesDirectory, "hashes.lcu.txt");

            // Leemos los archivos de hashes de forma asíncrona
            var oldGameHashesTask = File.ReadAllLinesAsync(oldGameHashesPath);
            var oldLcuHashesTask = File.ReadAllLinesAsync(oldLcuHashesPath);
            var newGameHashesTask = File.ReadAllLinesAsync(newGameHashesPath);
            var newLcuHashesTask = File.ReadAllLinesAsync(newLcuHashesPath);

            // Esperamos que se completen todas las lecturas
            await Task.WhenAll(oldGameHashesTask, oldLcuHashesTask, newGameHashesTask, newLcuHashesTask);

            // Asignamos los resultados leídos de los archivos
            var oldGameHashes = await oldGameHashesTask;
            var oldLcuHashes = await oldLcuHashesTask;
            var newGameHashes = await newGameHashesTask;
            var newLcuHashes = await newLcuHashesTask;

            // Convertimos los hashes antiguos en HashSet para mejorar la búsqueda (O(1) de complejidad)
            var oldGameHashesSet = new HashSet<string>(oldGameHashes);
            var oldLcuHashesSet = new HashSet<string>(oldLcuHashes);

            // Usamos Parallel.ForEach para comparar los hashes en paralelo y mejorar el rendimiento
            var differencesGame = new ConcurrentBag<string>();
            var differencesLcu = new ConcurrentBag<string>();

            // Comparamos los nuevos hashes con los antiguos en paralelo (para hashes de "game")
            Parallel.ForEach(newGameHashes, newHash =>
            {
                if (!oldGameHashesSet.Contains(newHash))
                {
                    differencesGame.Add(newHash);
                }
            });

            // Comparamos los nuevos hashes con los antiguos en paralelo (para hashes de "lcu")
            Parallel.ForEach(newLcuHashes, newHash =>
            {
                if (!oldLcuHashesSet.Contains(newHash))
                {
                    differencesLcu.Add(newHash);
                }
            });

            // Llamamos al método de filtrado para las diferencias encontradas
            await FilterAndSaveDifferencesAsync(differencesGame.ToList(), differencesLcu.ToList());
        }

        // Método que filtra y guarda las diferencias, eliminando las que no cumplen ciertos criterios
        public async Task FilterAndSaveDifferencesAsync(List<string> differencesGame, List<string> differencesLcu)
        {
            // Leemos el archivo de hashes antiguos (game.txt) para realizar comparaciones
            string oldHashesPath = Path.Combine(_oldHashesDirectory, "hashes.game.txt");
            var oldHashes = await File.ReadAllLinesAsync(oldHashesPath);

            // Creamos un HashSet con las rutas de los archivos, eliminando la extensión para comparación de .tex
            var oldPathsWithoutTex = new HashSet<string>(
                oldHashes
                    .Select(line => line.Split(' ')[1])  // Obtenemos la ruta del archivo
                    .Where(path => path.EndsWith(".dds"))  // Filtramos por las extensiones que nos interesan (en este caso solo .dds)
                    .Select(path => path[..path.LastIndexOf('.')])  // Eliminamos la extensión para la comparación
            );

            // Listas para almacenar las diferencias filtradas
            var filteredDifferencesGame = new ConcurrentBag<string>();
            var filteredDifferencesLcu = new ConcurrentBag<string>();

            // Procesamos las diferencias de GAME en paralelo
            Parallel.ForEach(differencesGame, line =>
            {
                try
                {
                    var parts = line.Split(' ');  // Dividimos la línea en partes
                    var filePath = parts[1];     // Obtenemos la ruta del archivo
                    var extension = Path.GetExtension(filePath);  // Obtenemos la extensión del archivo

                    // Si la extensión está en la lista de excluidas, ignoramos este archivo
                    if (_excludedExtensions.Contains(extension))
                        return;

                    // Si la ruta es ignorada por las reglas personalizadas, la descartamos
                    string adjusted = AssetDownloader.AdjustUrlBasedOnRules(filePath);
                    if (adjusted == null)
                        return;

                    // Si es un archivo .tex, lo comparamos con las rutas antiguas sin extensión
                    if (filePath.EndsWith(".tex"))
                    {
                        var baseFile = filePath[..filePath.LastIndexOf('.')]; // Removemos la extensión .tex
                        if (!oldPathsWithoutTex.Contains(baseFile))  // Si no lo encontramos en las rutas sin extensión, lo añadimos a las diferencias
                        {
                            filteredDifferencesGame.Add(line);
                        }
                    }
                    else
                    {
                        filteredDifferencesGame.Add(line);  // Si no es un .tex, lo añadimos directamente
                    }
                }
                catch (Exception ex)
                {
                    // Si ocurre algún error procesando una línea, lo registramos en los logs
                    Log.Warning("Error filtrando línea GAME '{Line}': {Message}", line, ex.Message);
                }
            });

            // Procesamos las diferencias de LCU en paralelo
            Parallel.ForEach(differencesLcu, line =>
            {
                try
                {
                    var parts = line.Split(' ');  // Dividimos la línea en partes
                    var filePath = parts[1];     // Obtenemos la ruta del archivo

                    // Si la ruta es ignorada por las reglas personalizadas, la descartamos
                    string adjusted = AssetDownloader.AdjustUrlBasedOnRules(filePath);
                    if (adjusted != null)
                    {
                        filteredDifferencesLcu.Add(line);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Error filtrando línea LCU '{Line}': {Message}", line, ex.Message);
                }
            });

            // Guardamos las diferencias filtradas en los archivos correspondientes
            await File.WriteAllLinesAsync(Path.Combine(_resourcesPath, "differences_game.txt"), filteredDifferencesGame);
            await File.WriteAllLinesAsync(Path.Combine(_resourcesPath, "differences_lcu.txt"), filteredDifferencesLcu);

            // Mensajes de log indicando que las diferencias se guardaron correctamente
            Log.Information("Filtered differences saved to {0}", Path.Combine(_resourcesPath, "differences_game.txt"));
            Log.Information("Filtered differences saved to {0}", Path.Combine(_resourcesPath, "differences_lcu.txt"));
        }
    }
}
