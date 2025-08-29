using System;
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
        private readonly Dictionary<uint, string> _binHashesMap = new Dictionary<uint, string>();
        private readonly Dictionary<uint, string> _binEntriesMap = new Dictionary<uint, string>();
        private readonly Dictionary<uint, string> _binFieldsMap = new Dictionary<uint, string>();
        private readonly Dictionary<uint, string> _binTypesMap = new Dictionary<uint, string>();

        private readonly DirectoriesCreator _directoriesCreator;

        public HashResolverService(DirectoriesCreator directoriesCreator)
        {
            _directoriesCreator = directoriesCreator;
        }

        public async Task LoadHashesAsync()
        {
            if (_hashToPathMap.Count > 0) return;

            var newHashesDir = _directoriesCreator.HashesNewPath;

            var gameHashesFile = Path.Combine(newHashesDir, "hashes.game.txt");
            var lcuHashesFile = Path.Combine(newHashesDir, "hashes.lcu.txt");

            await LoadHashesFromFile(gameHashesFile, _hashToPathMap, text => (ulong.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out ulong hash), hash));
            await LoadHashesFromFile(lcuHashesFile, _hashToPathMap, text => (ulong.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out ulong hash), hash));
        }

        public async Task LoadBinHashesAsync()
        {
            if (_binHashesMap.Count > 0) return; // Already loaded

            var binHashesDir = _directoriesCreator.HashesNewPath;
            var binHashesFile = Path.Combine(binHashesDir, "hashes.binhashes.txt");
            var binEntriesFile = Path.Combine(binHashesDir, "hashes.binentries.txt");
            var binFieldsFile = Path.Combine(binHashesDir, "hashes.binfields.txt");
            var binTypesFile = Path.Combine(binHashesDir, "hashes.bintypes.txt");

            await LoadHashesFromFile(binHashesFile, _binHashesMap, text => (uint.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out uint hash), hash));
            await LoadHashesFromFile(binEntriesFile, _binEntriesMap, text => (uint.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out uint hash), hash));
            await LoadHashesFromFile(binFieldsFile, _binFieldsMap, text => (uint.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out uint hash), hash));
            await LoadHashesFromFile(binTypesFile, _binTypesMap, text => (uint.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out uint hash), hash));
        }

        private async Task LoadHashesFromFile<T>(string filePath, IDictionary<T, string> map, Func<string, (bool, T)> parser)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            await Task.Run(() =>
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Trim().Split(' ');
                    if (parts.Length == 2)
                    {
                        var (success, hash) = parser(parts[0]);
                        if (success)
                        {
                            var path = parts[1];
                            map[hash] = path;
                        }
                    }
                }
            });
        }

        public string ResolveHash(ulong pathHash)
        {
            return _hashToPathMap.TryGetValue(pathHash, out var path) ? path : pathHash.ToString("x16");
        }

        public string ResolveBinHashGeneral(uint hash)
        {
            if (_binEntriesMap.TryGetValue(hash, out var path)) return path;
            if (_binFieldsMap.TryGetValue(hash, out path)) return path;
            if (_binTypesMap.TryGetValue(hash, out path)) return path;
            if (_binHashesMap.TryGetValue(hash, out path)) return path;
            return hash.ToString("x8");
        }
    }
}