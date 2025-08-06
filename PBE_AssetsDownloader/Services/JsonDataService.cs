using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PBE_AssetsDownloader.UI;
using PBE_AssetsDownloader.Utils;

namespace PBE_AssetsDownloader.Services
{
    public class JsonDataService
    {
        private readonly LogService _logService;
        private readonly HttpClient _httpClient;
        private readonly AppSettings _appSettings;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly Requests _requests;
        private readonly string statusUrl = "https://raw.communitydragon.org/data/hashes/lol/";

        public JsonDataService(LogService logService, HttpClient httpClient, AppSettings appSettings, DirectoriesCreator directoriesCreator, Requests requests)
        {
            _logService = logService;
            _httpClient = httpClient;
            _appSettings = appSettings;
            _directoriesCreator = directoriesCreator;
            _requests = requests;
        }

        public async Task<Dictionary<string, long>> GetRemoteHashesSizesAsync()
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
            return result;
        }

        public async Task CheckJsonDataUpdatesAsync()
        {
            if (!_appSettings.CheckJsonDataUpdates || (_appSettings.MonitoredJsonDirectories == null && _appSettings.MonitoredJsonFiles == null))
            {
                return; // La opción no está activada o ninguna lista de archivos/directorios está configurada.
            }

            // Creamos directorios necesarios + Mensaje de creacion
            await _directoriesCreator.CreateDirJsonCacheNewAsync();
            await _directoriesCreator.CreateDirJsonCacheOldAsync();

            _logService.Log("Checking for JSON file updates...");
            var serverJsonDataEntries = new Dictionary<string, (DateTime Date, string FullUrl)>();
            bool anyUrlProcessed = false;

            // Procesar directorios monitoreados
            if (_appSettings.MonitoredJsonDirectories != null)
            {
                foreach (var url in _appSettings.MonitoredJsonDirectories)
                {
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        continue;
                    }

                    try
                    {
                        string html = await _httpClient.GetStringAsync(url);
                        var regex = new Regex(@"<a href=""(?<filename>[^""]+\.json)""[^>]*>.*?<\/a><\/td><td class=""size"">.*?<\/td><td class=""date"">(?<date>[^<]+)<\/td>", RegexOptions.Singleline);
                        foreach (Match match in regex.Matches(html))
                        {
                            string filename = match.Groups["filename"].Value;
                            string dateStr = match.Groups["date"].Value.Trim();
                            if (ParseDate(dateStr, out DateTime parsedDate))
                            {
                                string fullFileUrl = url + filename; // Construir la URL completa
                                serverJsonDataEntries[filename] = (parsedDate, fullFileUrl); // Usar el nombre del archivo como clave
                                anyUrlProcessed = true;
                            }
                            else
                            {
                                _logService.LogWarning($"Could not parse date for {filename}: {dateStr}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError(ex, $"Error processing monitored directory URL: {url}");
                    }
                }
            }

            // Procesar archivos individuales monitoreados
            if (_appSettings.MonitoredJsonFiles != null)
            {
                foreach (var url in _appSettings.MonitoredJsonFiles)
                {
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        continue;
                    }

                    string parentDirectoryUrl = string.Empty;
                    try
                    {
                        // Extraer la URL del directorio padre
                        Uri fileUri = new Uri(url);
                        parentDirectoryUrl = fileUri.GetLeftPart(UriPartial.Path);
                        parentDirectoryUrl = parentDirectoryUrl.Substring(0, parentDirectoryUrl.LastIndexOf('/') + 1); // Asegurarse de que termine con /

                        string html = await _httpClient.GetStringAsync(parentDirectoryUrl);
                        // Reutilizar la regex para parsear listados de directorios
                        var regex = new Regex(@"<a href=""(?<filename>[^""]+\.json)""[^>]*>.*?<\/a><\/td><td class=""size"">.*?<\/td><td class=""date"">(?<date>[^<]+)<\/td>", RegexOptions.Singleline);
                        bool foundInParent = false;
                        foreach (Match match in regex.Matches(html))
                        {
                            string filenameInParent = match.Groups["filename"].Value;
                            if (url.EndsWith(filenameInParent)) // Comprobar si es nuestro archivo
                            {
                                string dateStr = match.Groups["date"].Value.Trim();
                                if (ParseDate(dateStr, out DateTime parsedDate))
                                {
                                    serverJsonDataEntries[filenameInParent] = (parsedDate, url); // Usar el nombre del archivo como clave
                                    anyUrlProcessed = true;
                                    foundInParent = true;
                                    _logService.LogDebug($"Found {url} in parent directory listing. Date: {parsedDate}");
                                    break; // Encontramos nuestro archivo, no es necesario continuar el bucle
                                }
                                else
                                {
                                    _logService.LogWarning($"Could not parse date for {url}: {dateStr}");
                                }
                            }
                        }

                        if (!foundInParent)
                        {
                            _logService.LogWarning($"Could not find {url} in its parent directory listing: {parentDirectoryUrl}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError(ex, $"Error fetching or parsing parent directory {parentDirectoryUrl} for file {url}");
                    }
                }
            }

            if (!anyUrlProcessed)
            {
                _logService.LogWarning("No JSON files could be processed from the configured URLs.");
                return;
            }

            var localJsonDataDates = _appSettings.JsonDataModificationDates ?? new Dictionary<string, DateTime>();
            bool updated = false;

            foreach (var serverEntry in serverJsonDataEntries)
            {
                string filename = serverEntry.Key;
                DateTime serverDate = serverEntry.Value.Date;
                string fullUrl = serverEntry.Value.FullUrl;

                if (!localJsonDataDates.ContainsKey(filename) || localJsonDataDates[filename] != serverDate)
                {
                    string message = $"File updated or new: {filename} (Server Date: {serverDate.ToString("yyyy-MMM-dd HH:mm", CultureInfo.InvariantCulture)})";
                    localJsonDataDates[filename] = serverDate;
                    updated = true;

                    string jsonFileName = filename;
                    string oldFilePath = Path.Combine(_directoriesCreator.JsonCacheOldPath, jsonFileName);
                    string newFilePath = Path.Combine(_directoriesCreator.JsonCacheNewPath, jsonFileName);

                    try
                    {
                        if (File.Exists(newFilePath))
                        {
                            File.Copy(newFilePath, oldFilePath, true);
                        }

                        // Usar el nuevo método de Requests para descargar y guardar el JSON
                        bool downloadSuccess = await _requests.DownloadJsonContentAsync(fullUrl, newFilePath);

                        if (downloadSuccess)
                        {
                            _logService.LogDebug($"Saved new JSON content to {newFilePath}");
                            // Log interactive message
                            _logService.LogInteractive(
                                message,
                                "View Diff",
                                () =>
                                {
                                    string oldContent = File.Exists(oldFilePath) ? File.ReadAllText(oldFilePath) : "";
                                    string newContent = File.Exists(newFilePath) ? File.ReadAllText(newFilePath) : "";
                                    var diffWindow = new JsonDiffWindow(oldContent, newContent);
                                    diffWindow.Show();
                                }
                            );
                        }
                        else
                        {
                            // Si la descarga falla, loguear el error y no marcar como actualizado
                            _logService.LogError($"Failed to download and save JSON content for {fullUrl}. Check logs for details.");
                            updated = false; // No se actualizó correctamente
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError(ex, $"Error processing JSON content for {fullUrl}");
                        updated = false; // Hubo un error, no se actualizó correctamente
                    }
                }
            }

            if (updated)
            {
                _appSettings.JsonDataModificationDates = localJsonDataDates;
                AppSettings.SaveSettings(_appSettings);
                _logService.LogSuccess("Local game data dates updated.");
            }
            else
            {
                _logService.Log("JSON files are up-to-date.");
            }
        }

        private bool ParseDate(string dateStr, out DateTime date)
        {
            // Example format: 2025-Jul-29 21:18
            string format = "yyyy-MMM-dd HH:mm";
            return DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
    }
}
