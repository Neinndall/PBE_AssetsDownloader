using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PBE_NewFileExtractor.Services
{
    public class HashesManager
    {
        private readonly string _oldHashesDirectory;
        private readonly string _newHashesDirectory;

        public HashesManager(string oldHashesDirectory, string newHashesDirectory)
        {
            _oldHashesDirectory = oldHashesDirectory;
            _newHashesDirectory = newHashesDirectory;
        }

        public async Task CompareHashesAsync()
        {
            string oldGameHashesPath = Path.Combine(_oldHashesDirectory, "hashes.game.txt");
            string oldLcuHashesPath = Path.Combine(_oldHashesDirectory, "hashes.lcu.txt");
            string newGameHashesPath = Path.Combine(_newHashesDirectory, "hashes.game.txt");
            string newLcuHashesPath = Path.Combine(_newHashesDirectory, "hashes.lcu.txt");

            var oldGameHashes = await File.ReadAllLinesAsync(oldGameHashesPath);
            var oldLcuHashes = await File.ReadAllLinesAsync(oldLcuHashesPath);
            var newGameHashes = await File.ReadAllLinesAsync(newGameHashesPath);
            var newLcuHashes = await File.ReadAllLinesAsync(newLcuHashesPath);

            var differencesGame = newGameHashes.Except(oldGameHashes).ToList();
            var differencesLcu = newLcuHashes.Except(oldLcuHashes).ToList();

            var resourcesDirectory = "Resources";
            Directory.CreateDirectory(resourcesDirectory);

            await File.WriteAllLinesAsync(Path.Combine(resourcesDirectory, "differences_game.txt"), differencesGame);
            await File.WriteAllLinesAsync(Path.Combine(resourcesDirectory, "differences_lcu.txt"), differencesLcu);
        }
    }
}