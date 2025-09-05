using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using PBE_AssetsManager.Views.Models;
using Newtonsoft.Json.Linq;

namespace PBE_AssetsManager.Utils
{
    public class AppSettings
    {
        public bool SyncHashesWithCDTB { get; set; }
        public bool AutoCopyHashes { get; set; }
        public bool CreateBackUpOldHashes { get; set; }
        public bool OnlyCheckDifferences { get; set; }
        public bool CheckJsonDataUpdates { get; set; }
        public bool CheckAssetUpdates { get; set; }
        public bool SaveDiffHistory { get; set; }
        public bool BackgroundUpdates { get; set; }
        public int UpdateCheckFrequency { get; set; }

        public string NewHashesPath { get; set; }
        public string OldHashesPath { get; set; }
        public string PbeDirectory { get; set; }
        public Dictionary<string, long> HashesSizes { get; set; }

        // This will become redundant after migration
        public Dictionary<string, DateTime> JsonDataModificationDates { get; set; }

        // New structure for monitored files and directories
        public List<string> MonitoredJsonFiles { get; set; }
        public List<JsonDiffHistoryEntry> DiffHistory { get; set; }
        public Dictionary<string, long> AssetTrackerProgress { get; set; }
        public Dictionary<string, List<long>> AssetTrackerFailedIds { get; set; }
        public Dictionary<string, List<long>> AssetTrackerFoundIds { get; set; }

        private const string ConfigFilePath = "config.json";

        public static AppSettings LoadSettings()
        {
            if (!File.Exists(ConfigFilePath))
            {
                var defaultSettings = GetDefaultSettings();
                SaveSettings(defaultSettings);
                return defaultSettings;
            }

            var json = File.ReadAllText(ConfigFilePath);
            var settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? GetDefaultSettings();

            // Migration block to handle old formats.
            var jObject = JObject.Parse(json);
            bool needsResave = false;

            if (jObject.TryGetValue("MonitoredJsonFiles", out var monitoredFilesToken) &&
                monitoredFilesToken is JArray monitoredFilesArray)
            {
                var newUrls = new List<string>();
                var newDates = settings.JsonDataModificationDates ?? new Dictionary<string, DateTime>();

                foreach (var token in monitoredFilesArray)
                {
                    if (token is JObject itemObject &&
                        itemObject.TryGetValue("Url", StringComparison.OrdinalIgnoreCase, out var urlToken))
                    {
                        // Old object format: {"Url": "...", "LastUpdated": "..."}
                        string url = urlToken.ToString();
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            newUrls.Add(url);

                            if (itemObject.TryGetValue("LastUpdated", StringComparison.OrdinalIgnoreCase, out var dateToken) &&
                                DateTime.TryParse(dateToken.ToString(), out var date))
                            {
                                newDates[url] = date;
                            }

                            needsResave = true; // Mark for resave to migrate format
                        }
                    }
                    else if (token.Type == JTokenType.String)
                    {
                        // Current/new format: "...url..."
                        newUrls.Add(token.ToString());
                    }
                }

                settings.MonitoredJsonFiles = newUrls.Distinct().ToList();
                settings.JsonDataModificationDates = newDates;
            }

            if (needsResave)
            {
                SaveSettings(settings);
            }

            // Ensure lists are not null
            settings.MonitoredJsonFiles ??= new List<string>();
            settings.JsonDataModificationDates ??= new Dictionary<string, DateTime>();
            settings.DiffHistory ??= new List<JsonDiffHistoryEntry>();
            settings.AssetTrackerProgress ??= new Dictionary<string, long>();
            settings.AssetTrackerFailedIds ??= new Dictionary<string, List<long>>();
            settings.AssetTrackerFoundIds ??= new Dictionary<string, List<long>>();

            return settings;
        }

        public static AppSettings GetDefaultSettings()
        {
            return new AppSettings
            {
                SyncHashesWithCDTB = true,
                AutoCopyHashes = false,
                CreateBackUpOldHashes = false,
                OnlyCheckDifferences = false,
                CheckJsonDataUpdates = false,
                CheckAssetUpdates = false,
                SaveDiffHistory = false,
                BackgroundUpdates = false,
                UpdateCheckFrequency = 10, // Default to 10 minutes

                NewHashesPath = null,
                OldHashesPath = null,
                PbeDirectory = null,
                HashesSizes = new Dictionary<string, long>(),

                JsonDataModificationDates = new Dictionary<string, DateTime>(),
                MonitoredJsonFiles = new List<string>(),
                DiffHistory = new List<JsonDiffHistoryEntry>(),
                AssetTrackerProgress = new Dictionary<string, long>(),
                AssetTrackerFailedIds = new Dictionary<string, List<long>>(),
                AssetTrackerFoundIds = new Dictionary<string, List<long>>()
            };
        }

        public static void SaveSettings(AppSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }
    }
}
