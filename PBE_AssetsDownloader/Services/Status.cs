using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;
using PBE_AssetsDownloader.Utils;

namespace PBE_AssetsDownloader.Services
{
    public class Status
    {
        private readonly string statusUrl = "https://raw.communitydragon.org/data/hashes/lol/";
        private const string GAME_HASHES_FILENAME = "hashes.game.txt";
        private const string LCPU_HASHES_FILENAME = "hashes.lcu.txt";
        
        private readonly string configFilePath = "config.json";
        
        public string CurrentStatus { get; private set; }
        private Action<string> _logAction;
        private static readonly HttpClient client = new HttpClient(); // Reusar HttpClient

        public Status(Action<string> logAction)
        {
            _logAction = logAction;
        }

        // Maneja si debe actualizar los hashes del servidor
        public async Task SyncHashesIfNeeds(bool syncHashesWithCDTB)
        {
            var directoriesCreator = new DirectoriesCreator();
            var httpClient = new HttpClient();
            var requests = new Requests(httpClient, directoriesCreator);

            bool isUpdated = await IsUpdatedAsync();
            if (isUpdated)
            {
                AppendLog("Server updated. Starting hash synchronization...");
                await requests.SyncHashesIfEnabledAsync(syncHashesWithCDTB, AppendLog);
            }
            else
            {
                CheckForUpdates(isUpdated);
            }
        }
        
        public async Task<bool> IsUpdatedAsync()
        {
            try
            {
                AppendLog("Getting update sizes from server...");
                var serverSizes = await GetRemoteHashesSizesAsync();
                var localSizes = ReadLocalHashesSizes();
                bool updated = false;

                // Verificar si hay alguna diferencia en los tamaños
                updated |= UpdateHashSizeIfDifferent(serverSizes, localSizes, GAME_HASHES_FILENAME);
                updated |= UpdateHashSizeIfDifferent(serverSizes, localSizes, LCPU_HASHES_FILENAME);

                if (updated)
                {
                    SaveHashesSizes(localSizes);
                    return true;
                }
            }
            catch (Exception ex)
            {
                CurrentStatus = $"Error checking for updates: {ex.Message}";
                AppendLog(CurrentStatus);
            }

            return false;
        }

        private bool UpdateHashSizeIfDifferent(Dictionary<string, long> serverSizes, Dictionary<string, long> localSizes, string filename)
        {
            long serverSize = serverSizes.GetValueOrDefault(filename, 0);
            long localSize = localSizes.GetValueOrDefault(filename, 0);

            if (serverSize != localSize)
            {
                localSizes[filename] = serverSize;
                return true;
            }

            return false;
        }

        private async Task<Dictionary<string, long>> GetRemoteHashesSizesAsync()
        {
            var result = new Dictionary<string, long>();

            string html = await client.GetStringAsync(statusUrl);
            var lines = html.Split('\n');

            foreach (var line in lines)
            {
                // Solo buscar hashes.game.txt o hashes.lcu.txt
                var match = Regex.Match(line, @"href=""(?<filename>hashes\.(game|lcu)\.txt)"".*?\s(?<size>\d+)\s*$");
                if (match.Success)
                {
                    string filename = match.Groups["filename"].Value;
                    long size = long.Parse(match.Groups["size"].Value);
                    result[filename] = size;
                }
            }

            return result;
        }


        private Dictionary<string, long> ReadLocalHashesSizes()
        {
            return ReadSettings()["HashesSizes"]?.ToObject<Dictionary<string, long>>() ?? new Dictionary<string, long>();
        }

        private void SaveHashesSizes(Dictionary<string, long> sizes)
        {
            var settings = ReadSettings();
            settings["HashesSizes"] = JObject.FromObject(sizes);
            WriteSettings(settings);
        }

        private JObject ReadSettings()
        {
            if (File.Exists(configFilePath))
            {
                string json = File.ReadAllText(configFilePath);
                return JObject.Parse(json);
            }
            return new JObject(); // Si el archivo no existe, devolver un JObject vacío
        }

        private void WriteSettings(JObject settings)
        {
            File.WriteAllText(configFilePath, settings.ToString());
        }

        public void CheckForUpdates(bool isUpdated)
        {
            if (!isUpdated)
            {
                AppendLog("No server updates found.");
            }
        }

        private void AppendLog(string message)
        {
            _logAction?.Invoke(message);
        }
    }
}
