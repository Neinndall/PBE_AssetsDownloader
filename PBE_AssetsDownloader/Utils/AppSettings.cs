using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

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

        private const string ConfigFilePath = "config.json";

        public static AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    var defaultSettings = GetDefaultSettings();
                    SaveSettings(defaultSettings); // Guardamos el archivo con valores predeterminados
                    return defaultSettings;
                }

                var json = File.ReadAllText(ConfigFilePath);
                var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                
                if (settings == null)
                {
                    Serilog.Log.Error("Error: Configuration file is invalid, loading default settings.");
                    return GetDefaultSettings();
                }

                return settings;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al cargar la configuración.");
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
                JsonDataSizes = new Dictionary<string, long>() // Inicializamos el nuevo diccionario
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
