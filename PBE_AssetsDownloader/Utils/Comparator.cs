using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace PBE_NewFileExtractor.Utils
{
    public class FilesComparator
    {
        private readonly string _oldGameHashesPath;
        private readonly string _newGameHashesPath;
        private readonly string _oldLcuHashesPath;
        private readonly string _newLcuHashesPath;
        private readonly string _differencesGameFilePath;
        private readonly string _differencesLcuFilePath;

        public FilesComparator(string oldGameHashesPath, string newGameHashesPath, string oldLcuHashesPath, string newLcuHashesPath, string differencesGameFilePath, string differencesLcuFilePath)
        {
            _oldGameHashesPath = oldGameHashesPath;
            _newGameHashesPath = newGameHashesPath;
            _oldLcuHashesPath = oldLcuHashesPath;
            _newLcuHashesPath = newLcuHashesPath;
            _differencesGameFilePath = differencesGameFilePath;
            _differencesLcuFilePath = differencesLcuFilePath;
        }

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

        private IEnumerable<string> CompareHashes(string oldFile, string newFile)
        {
            var oldHashes = File.ReadAllLines(oldFile);
            var newHashes = File.ReadAllLines(newFile);
            return newHashes.Except(oldHashes);
        }

        private async Task SaveDifferencesToFile(IEnumerable<string> differences, string fileName)
        {
            await File.WriteAllLinesAsync(fileName, differences);
        }
    }
}
