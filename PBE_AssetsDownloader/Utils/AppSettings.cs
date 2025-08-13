using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using PBE_AssetsDownloader.Info;

namespace PBE_AssetsDownloader.Utils
{
  public class AppSettings
  {
    public bool SyncHashesWithCDTB { get; set; }
    public bool AutoCopyHashes { get; set; }
    public bool CreateBackUpOldHashes { get; set; }
    public bool OnlyCheckDifferences { get; set; }
    public bool CheckJsonDataUpdates { get; set; }
    public bool SaveDiffHistory { get; set; }
    public bool BackgroundUpdates { get; set; }
    public int UpdateCheckFrequency { get; set; }
    
    public string NewHashesPath { get; set; }
    public string OldHashesPath { get; set; }
    public Dictionary<string, long> HashesSizes { get; set; }

    public Dictionary<string, DateTime> JsonDataModificationDates { get; set; }
    public List<string> MonitoredJsonDirectories { get; set; }
    public List<string> MonitoredJsonFiles { get; set; }
    public List<JsonDiffHistoryEntry> DiffHistory { get; set; }

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
        var jObject = Newtonsoft.Json.Linq.JObject.Parse(json);

        if (jObject.TryGetValue("JsonFiles", out var jsonFilesToken) && jsonFilesToken is Newtonsoft.Json.Linq.JArray jsonFilesArray)
        {
            var monitoredDirectories = (jObject["MonitoredJsonDirectories"] as Newtonsoft.Json.Linq.JArray) ?? new Newtonsoft.Json.Linq.JArray();
            var monitoredFiles = (jObject["MonitoredJsonFiles"] as Newtonsoft.Json.Linq.JArray) ?? new Newtonsoft.Json.Linq.JArray();

            foreach (var urlToken in jsonFilesArray)
            {
                string url = urlToken.ToString(); // Correct way to get the string value
                if (!string.IsNullOrEmpty(url))
                {
                    if (url.EndsWith("/"))
                    {
                        if (!monitoredDirectories.Any(x => x.ToString() == url)) monitoredDirectories.Add(url);
                    }
                    else
                    {
                        if (!monitoredFiles.Any(x => x.ToString() == url)) monitoredFiles.Add(url);
                    }
                }
            }

            jObject["MonitoredJsonDirectories"] = monitoredDirectories;
            jObject["MonitoredJsonFiles"] = monitoredFiles;
            jObject.Remove("JsonFiles");
        }

        var settings = jObject.ToObject<AppSettings>();

        if (settings == null)
        {
            return GetDefaultSettings();
        }
        
        // Ensure lists are not null if they are missing from the JSON
        if (settings.MonitoredJsonDirectories == null) settings.MonitoredJsonDirectories = new List<string>();
        if (settings.MonitoredJsonFiles == null) settings.MonitoredJsonFiles = new List<string>();
        if (settings.DiffHistory == null) settings.DiffHistory = new List<JsonDiffHistoryEntry>();

        // Handle backward compatibility for JsonDataSizes (old format with full URLs as keys)
        if (jObject.TryGetValue("JsonDataSizes", out var jsonDataSizesToken) && jsonDataSizesToken.Type == Newtonsoft.Json.Linq.JTokenType.Object)
        {
            var oldSizes = jsonDataSizesToken.ToObject<Dictionary<string, long>>();
            if (oldSizes != null)
            {
                settings.JsonDataModificationDates = oldSizes.ToDictionary(kvp => Path.GetFileName(kvp.Key), kvp => DateTime.MinValue);
            }
        }
        // Handle loading JsonDataModificationDates (new format with filenames as keys)
        else if (jObject.TryGetValue("JsonDataModificationDates", out var jsonDataDatesToken) && jsonDataDatesToken.Type == Newtonsoft.Json.Linq.JTokenType.Object)
        {
            settings.JsonDataModificationDates = jsonDataDatesToken.ToObject<Dictionary<string, DateTime>>();
        }
        else
        {
            settings.JsonDataModificationDates = new Dictionary<string, DateTime>();
        }

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
        SaveDiffHistory = false,
        BackgroundUpdates = false,
        UpdateCheckFrequency = 10, // Default to 10 minutes
        
        NewHashesPath = null,
        OldHashesPath = null,
        HashesSizes = new Dictionary<string, long>(),

        JsonDataModificationDates = new Dictionary<string, DateTime>(),
        MonitoredJsonDirectories = new List<string>(),
        MonitoredJsonFiles = new List<string>(),
        DiffHistory = new List<JsonDiffHistoryEntry>()
      };
    }

    public static void SaveSettings(AppSettings settings)
    {
        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        File.WriteAllText(ConfigFilePath, json);
    }
  }
}