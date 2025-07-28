// PBE_AssetsDownloader/Services/Status.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq; // Mantener si JObject se usa en algún lado para HashesSizes (aunque refactorizaremos)
using PBE_AssetsDownloader.Utils; // Necesario para AppSettings y DirectoriesCreator
using Serilog;
using PBE_AssetsDownloader.Services; // Para LogService y Requests

namespace PBE_AssetsDownloader.Services
{
    public class Status
    {
        private readonly string statusUrl = "https://raw.communitydragon.org/data/hashes/lol/";
        private const string GAME_HASHES_FILENAME = "hashes.game.txt";
        private const string LCPU_HASHES_FILENAME = "hashes.lcu.txt";
        private const string JSON_DATA_URL = "https://raw.communitydragon.org/pbe/plugins/rcp-be-lol-game-data/global/default/v1/";

        public string CurrentStatus { get; private set; }

        // Campos para las dependencias inyectadas
        private readonly LogService _logService;
        private readonly HttpClient _httpClient;
        private readonly Requests _requests;
        private readonly AppSettings _appSettings; // Añadimos la instancia de AppSettings

        // Constructor: Ahora recibe TODAS las dependencias que necesita
        public Status(
            LogService logService,
            HttpClient httpClient,
            Requests requests,
            AppSettings appSettings) // Recibimos AppSettings
        {
            _logService = logService;
            _httpClient = httpClient;
            _requests = requests;
            _appSettings = appSettings; // Asignamos la instancia
        }

        // Handles whether to update hashes from the server
        public async Task SyncHashesIfNeeds(bool syncHashesWithCDTB)
        {
            bool isUpdated = await IsUpdatedAsync();
            if (isUpdated)
            {
                _logService.Log("Server updated. Starting hash synchronization...");
                await _requests.SyncHashesIfEnabledAsync(syncHashesWithCDTB);
                _logService.LogSuccess("Synchronization completed.");
            }
            else
            {
                CheckForUpdates(isUpdated); // Esto logueará "No server updates found."
            }
        }

        public async Task<bool> IsUpdatedAsync()
        {
            try
            {
                _logService.Log("Getting update sizes from server...");
                var serverSizes = await GetRemoteHashesSizesAsync();

                if (serverSizes == null || serverSizes.Count == 0)
                {
                    _logService.LogWarning("Could not retrieve remote hash sizes or received an empty list. Skipping update check.");
                    return false; // No podemos determinar si hay actualización sin tamaños remotos
                }

                // Usamos la instancia inyectada de AppSettings
                var localSizes = _appSettings.HashesSizes ?? new Dictionary<string, long>();

                bool updated = false;

                updated |= UpdateHashSizeIfDifferent(serverSizes, localSizes, GAME_HASHES_FILENAME);
                updated |= UpdateHashSizeIfDifferent(serverSizes, localSizes, LCPU_HASHES_FILENAME);

                if (updated)
                {
                    // Actualizamos la instancia de AppSettings y la guardamos ---
                    _appSettings.HashesSizes = localSizes; // Asigna los tamaños actualizados al objeto cargado
                    AppSettings.SaveSettings(_appSettings); // Guarda el objeto AppSettings completo
                    return true;
                }
                else
                {
                    return false; // No hay actualización necesaria
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error checking for updates.");
                return false; // Hubo un error, no está actualizado
            }
        }

        private bool UpdateHashSizeIfDifferent(Dictionary<string, long> serverSizes, Dictionary<string, long> localSizes, string filename)
        {
            long serverSize = serverSizes.GetValueOrDefault(filename, 0);
            long localSize = localSizes.GetValueOrDefault(filename, 0);

            if (serverSize != localSize)
            {
                localSizes[filename] = serverSize; // Actualiza el diccionario 'localSizes'
                return true; // Indica que se encontró una diferencia y se actualizó
            }
            return false;
        }

        private async Task<Dictionary<string, long>> GetRemoteHashesSizesAsync()
        {
            var result = new Dictionary<string, long>();

            if (_httpClient == null)
            {
                _logService.LogError("HttpClient is null. Cannot fetch remote sizes.");
                return result;
            }

            if (string.IsNullOrEmpty(statusUrl))
            {
                _logService.LogError("statusUrl is null or empty. Cannot fetch remote sizes.");
                return result;
            }

            string html;
            try
            {
                html = await _httpClient.GetStringAsync(statusUrl);
            }
            catch (HttpRequestException httpEx)
            {
                _logService.LogError($"HTTP request failed for '{statusUrl}': {httpEx.Message}. Check internet connection or URL.");
                return result;
            }
            catch (Exception ex)
            {
                _logService.LogError($"An unexpected exception occurred fetching URL '{statusUrl}': {ex.Message}");
                return result;
            }

            if (string.IsNullOrEmpty(html))
            {
                _logService.LogError("Received empty response from statusUrl.");
                return result;
            }

            // <a href="hashes.game.txt">hashes.game.txt</a>        15-Jun-2023 10:00    12345
            // <a href="hashes.lcu.txt">hashes.lcu.txt</a>          15-Jun-2023 10:00    67890
            var regex = new Regex(@"href=""(?<filename>hashes\.(game|lcu)\.txt)"".*?\s+(?<size>\d+)\s*$", RegexOptions.Multiline);

            foreach (Match match in regex.Matches(html))
            {
                string filename = match.Groups["filename"].Value;
                string sizeStr = match.Groups["size"].Value;

                if (long.TryParse(sizeStr, out long size))
                {
                    result[filename] = size;
                }
                else
                {
                    _logService.LogError($"Invalid size format '{sizeStr}' for file '{filename}'.");
                }
            }
            if (result.Count == 0)
            {
                 _logService.LogWarning("No hash files hashes.game or hashes.lcu found in the remote directory listing.");
            }
            else
            {
                // _logService.Log($"Successfully parsed {result.Count} remote hash file sizes.");
            }
            return result;
        }

        public void CheckForUpdates(bool isUpdated)
        {
            if (!isUpdated)
            {
                _logService.Log("No server updates found. Local hashes are up-to-date.");
            }
        }

        public async Task CheckJsonDataUpdatesAsync()
        {
            var appSettings = _appSettings; // Usamos la instancia inyectada
            if (!appSettings.CheckJsonDataUpdates)
            {
                return; // La opción no está activada, no hacemos nada.
            }

            _logService.Log("Checking for files json updates...");

            try
            {
                string html = await _httpClient.GetStringAsync(JSON_DATA_URL);
                var serverJsonDataSizes = new Dictionary<string, long>();

                // Regex mejorada para capturar rutas completas y tamaños con unidades
                var regex = new Regex(@"<a href=""(?<filename>[^""]+\.json)""[^>]*>.*?<\/a><\/td><td class=""size"">(?<size>[^<]+)<\/td>", RegexOptions.Singleline);

                foreach (Match match in regex.Matches(html))
                {
                    string filename = match.Groups["filename"].Value;
                    string sizeStr = match.Groups["size"].Value;
                    long parsedSize = ParseSize(sizeStr); // Usa la función ParseSize mejorada
                    
                    serverJsonDataSizes[filename] = parsedSize;
                }

                if (serverJsonDataSizes.Count == 0)
                {
                    _logService.LogWarning("No .json files found at the specified URL. The parsing logic might need an update or the directory is empty.");
                    return;
                }

                var localJsonDataSizes = appSettings.JsonDataSizes ?? new Dictionary<string, long>();
                bool updated = false;

                // Comprobar archivos nuevos o actualizados en el servidor
                foreach (var serverFile in serverJsonDataSizes)
                {
                    if (!localJsonDataSizes.ContainsKey(serverFile.Key) || localJsonDataSizes[serverFile.Key] != serverFile.Value)
                    {
                        _logService.Log($"Game data file updated or new: {serverFile.Key} (Server Size: {FormatBytes(serverFile.Value)})");
                        localJsonDataSizes[serverFile.Key] = serverFile.Value;
                        updated = true;
                    }
                }

                if (updated)
                {
                    appSettings.JsonDataSizes = localJsonDataSizes;
                    AppSettings.SaveSettings(appSettings);
                    _logService.LogSuccess("Local game data sizes updated.");
                }
                else
                {
                    _logService.Log("Files are up-to-date.");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error checking for JSON data updates.");
            }
        }

        private long ParseSize(string sizeStr)
        {
            var culture = CultureInfo.InvariantCulture;
            sizeStr = sizeStr.Trim(); // Eliminar espacios en blanco al inicio/final

            // Extraer la parte numérica, que puede incluir un punto decimal
            Match numericMatch = Regex.Match(sizeStr, @"([\d\.]+)");
            if (!numericMatch.Success || !double.TryParse(numericMatch.Groups[1].Value, NumberStyles.Any, culture, out double size))
            {
                _logService.LogWarning($"Could not parse numeric part of size string: '{sizeStr}'. Defaulting to 0 bytes.");
                return 0; // Si no se puede parsear la parte numérica, retornamos 0
            }

            // Unidades BINARIAS (KiB, MiB, GiB, TiB)
            if (sizeStr.EndsWith("KiB", StringComparison.OrdinalIgnoreCase)) return (long)(size * 1024);
            if (sizeStr.EndsWith("MiB", StringComparison.OrdinalIgnoreCase)) return (long)(size * 1024 * 1024);
            if (sizeStr.EndsWith("GiB", StringComparison.OrdinalIgnoreCase)) return (long)(size * 1024 * 1024 * 1024);
            if (sizeStr.EndsWith("TiB", StringComparison.OrdinalIgnoreCase)) return (long)(size * 1024L * 1024L * 1024L * 1024L);

            // Unidades DECIMALES (KB, MB, GB) - Menos comunes en listados de directorios web, pero incluidos
            if (sizeStr.EndsWith("KB", StringComparison.OrdinalIgnoreCase)) return (long)(size * 1000);
            if (sizeStr.EndsWith("MB", StringComparison.OrdinalIgnoreCase)) return (long)(size * 1000 * 1000);
            if (sizeStr.EndsWith("GB", StringComparison.OrdinalIgnoreCase)) return (long)(size * 1000 * 1000 * 1000);

            // Si termina con 'B' o no tiene unidad, asumimos bytes
            if (sizeStr.EndsWith("B", StringComparison.OrdinalIgnoreCase)) return (long)size;

            return (long)size; // Por defecto, si no hay unidad o no se reconoce, asumimos que ya está en bytes
        }
        
        private string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KiB", "MiB", "GiB", "TiB" };
            int i = 0;
            double dblSByte = bytes;

            while (Math.Round(dblSByte / 1024) >= 1)
            {
                dblSByte /= 1024;
                i++;
            }

            return string.Format(CultureInfo.InvariantCulture, "{0:n1} {1}", dblSByte, Suffix[i]);
        }   
    }
}