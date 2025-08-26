using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LeagueToolkit.Hashing;
using PBE_AssetsManager.Utils;

namespace PBE_AssetsManager.Services
{
    public class HashResolverService
    {
        private readonly Dictionary<ulong, string> _hashToPathMap = new Dictionary<ulong, string>();
        private readonly DirectoriesCreator _directoriesCreator;

        public HashResolverService(DirectoriesCreator directoriesCreator)
        {
            _directoriesCreator = directoriesCreator;
        }

        public async Task LoadHashesAsync()
        {
            _hashToPathMap.Clear();

            var newHashesDir = _directoriesCreator.HashesNewPath;

            var gameHashesFile = Path.Combine(newHashesDir, "hashes.game.txt");
            var lcuHashesFile = Path.Combine(newHashesDir, "hashes.lcu.txt");

            await LoadHashesFromFile(gameHashesFile);
            await LoadHashesFromFile(lcuHashesFile);
        }

        private async Task LoadHashesFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                // Log or handle the case where the file doesn't exist
                return;
            }

            await Task.Run(() =>
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Trim().Split(' ');
                    if (parts.Length == 2 && ulong.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out ulong hash))
                    {
                        var path = parts[1];
                        _hashToPathMap[hash] = path;
                    }
                }
            });
        }

        public string ResolveHash(ulong pathHash)
        {
            return _hashToPathMap.TryGetValue(pathHash, out var path) ? path : pathHash.ToString("x16");
        }
    }
}