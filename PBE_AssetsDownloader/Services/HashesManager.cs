using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PBE_AssetsDownloader.Services
{
    public class HashesManager
    {
        private readonly string _oldHashesDirectory;
        private readonly string _newHashesDirectory;
        private readonly string _resourcesPath;

        // Modificar constructor para aceptar resourcesPath
        public HashesManager(string oldHashesDirectory, string newHashesDirectory, string resourcesPath)
        {
            _oldHashesDirectory = oldHashesDirectory;
            _newHashesDirectory = newHashesDirectory;
            _resourcesPath = resourcesPath;
        }

        public async Task CompareHashesAsync()
        {
            // Combina las rutas usando los directorios proporcionados
            string oldGameHashesPath = Path.Combine(_oldHashesDirectory, "hashes.game.txt");
            string oldLcuHashesPath = Path.Combine(_oldHashesDirectory, "hashes.lcu.txt");
            string newGameHashesPath = Path.Combine(_newHashesDirectory, "hashes.game.txt");
            string newLcuHashesPath = Path.Combine(_newHashesDirectory, "hashes.lcu.txt");

            // Lee los archivos de hashes
            var oldGameHashes = await File.ReadAllLinesAsync(oldGameHashesPath);
            var oldLcuHashes = await File.ReadAllLinesAsync(oldLcuHashesPath);
            var newGameHashes = await File.ReadAllLinesAsync(newGameHashesPath);
            var newLcuHashes = await File.ReadAllLinesAsync(newLcuHashesPath);

            // Compara los hashes
            var differencesGame = newGameHashes.Except(oldGameHashes).ToList();
            var differencesLcu = newLcuHashes.Except(oldLcuHashes).ToList();

            // Guarda las diferencias en los archivos correspondientes dentro del directorio de recursos
            await File.WriteAllLinesAsync(Path.Combine(_resourcesPath, "differences_game.txt"), differencesGame);
            await File.WriteAllLinesAsync(Path.Combine(_resourcesPath, "differences_lcu.txt"), differencesLcu);
        }
    }
}
