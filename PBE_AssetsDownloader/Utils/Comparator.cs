using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace PBE_AssetsDownloader.Utils
{
    public class FilesComparator
    {
        private string _oldGameHashesPath;
        private string _newGameHashesPath;
        private string _oldLcuHashesPath;
        private string _newLcuHashesPath;
        private string _differencesGameFilePath;
        private string _differencesLcuFilePath;

        // Método para inicializar rutas y ejecutar la comparación
        public void HashesComparator(string newHashesDirectory, string oldHashesDirectory, string resourcesPath)
        {
            // Configura las rutas necesarias
            _newGameHashesPath = Path.Combine(newHashesDirectory, "hashes.game.txt");
            _oldGameHashesPath = Path.Combine(oldHashesDirectory, "hashes.game.txt");
            _newLcuHashesPath = Path.Combine(newHashesDirectory, "hashes.lcu.txt");
            _oldLcuHashesPath = Path.Combine(oldHashesDirectory, "hashes.lcu.txt");
            _differencesGameFilePath = Path.Combine(resourcesPath, "differences_game.txt");
            _differencesLcuFilePath = Path.Combine(resourcesPath, "differences_lcu.txt");
        }

        // Método para verificar las diferencias entre los archivos
        public async Task CheckFilesDiffAsync()
        {
            try
            {
                Log.Information("Checking for differences between {0} and {1}", Path.GetFileName(_oldGameHashesPath), Path.GetFileName(_newGameHashesPath));
                var differencesGame = CompareHashes(_oldGameHashesPath, _newGameHashesPath);
                await SaveDifferencesToFile(differencesGame, _differencesGameFilePath);

                Log.Information("Checking for differences between {0} and {1}", Path.GetFileName(_oldLcuHashesPath), Path.GetFileName(_newLcuHashesPath));
                var differencesLcu = CompareHashes(_oldLcuHashesPath, _newLcuHashesPath);
                await SaveDifferencesToFile(differencesLcu, _differencesLcuFilePath);

                Log.Information("Differences saved to {0} and {1}", _differencesGameFilePath, _differencesLcuFilePath);
            }
            catch (Exception e)
            {
                Log.Error("Error during checking for differences!\nError:\n{0}", e);
                Console.ReadKey();
            }
        }

        // Método para comparar los hashes entre dos archivos
        private IEnumerable<string> CompareHashes(string oldFile, string newFile)
        {
            var oldHashes = File.ReadAllLines(oldFile);
            var newHashes = File.ReadAllLines(newFile);
            return newHashes.Except(oldHashes);
        }

        // Método para guardar las diferencias en un archivo
        private async Task SaveDifferencesToFile(IEnumerable<string> differences, string fileName)
        {
            await File.WriteAllLinesAsync(fileName, differences);
        }
    }
}
