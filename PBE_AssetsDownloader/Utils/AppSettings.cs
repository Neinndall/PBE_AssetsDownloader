using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace PBE_AssetsDownloader.Utils
{
  public class AppSettings
  {
    public bool SyncHashesWithCDTB { get; set; }
    public bool AutoCopyHashes { get; set; }
    public bool CreateBackUpOldHashes { get; set; }
    public bool OnlyCheckDifferences { get; set; }
    public string NewHashesPath { get; set; }
    public string OldHashesPath { get; set; }
    public Dictionary<string, long> HashesSizes { get; set; }
    public bool CheckJsonDataUpdates { get; set; }
    public Dictionary<string, long> JsonDataSizes { get; set; }
    public List<string> MonitoredJsonDirectories { get; set; }
    public List<string> MonitoredJsonFiles { get; set; }

    private const string ConfigFilePath = "config.json";

    public static AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                var defaultSettings = GetDefaultSettings();
                SaveSettings(defaultSettings);
                return defaultSettings;
            }

            var json = File.ReadAllText(ConfigFilePath);
            var jObject = Newtonsoft.Json.Linq.JObject.Parse(json);

            // Backward compatibility: Migrate from old "JsonFiles" to new lists
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
                Serilog.Log.Error("Error: Configuration file is invalid, loading default settings.");
                return GetDefaultSettings();
            }

            // Ensure lists are not null if they are missing from the JSON
            if (settings.MonitoredJsonDirectories == null) settings.MonitoredJsonDirectories = new List<string>();
            if (settings.MonitoredJsonFiles == null) settings.MonitoredJsonFiles = new List<string>();

            return settings;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error loading settings.");
        }

        return GetDefaultSettings();
    }

    // Obtener valores predeterminados
    public static AppSettings GetDefaultSettings()
    {
      return new AppSettings
      {
        SyncHashesWithCDTB = true,
        AutoCopyHashes = false,
        CreateBackUpOldHashes = false,
        OnlyCheckDifferences = false,
        HashesSizes = new Dictionary<string, long>(), // Inicializamos HashesSizes
        CheckJsonDataUpdates = false, // Por defecto, esta nueva opción estará desactivada
        JsonDataSizes = new Dictionary<string, long>(), // Inicializamos el nuevo diccionario
        MonitoredJsonDirectories = new List<string>
        {
          "https://raw.communitydragon.org/pbe/plugins/rcp-be-lol-game-data/global/default/v1/"
        },
        MonitoredJsonFiles = new List<string>
        {
          "https://raw.communitydragon.org/pbe/game/en_us/data/menu/en_us/lol.stringtable.json"
        }
      };
    }

    // Guardar configuración en el archivo
    public static void SaveSettings(AppSettings settings)
    {
      try
      {
        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        File.WriteAllText(ConfigFilePath, json);
      }
      catch (Exception ex)
      {
        // Maneja el error de guardado si es necesario
        Serilog.Log.Error(ex, "Error al guardar la configuración.");
      }
    }
  }
}
