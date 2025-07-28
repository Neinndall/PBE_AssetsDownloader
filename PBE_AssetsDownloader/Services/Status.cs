// PBE_AssetsDownloader/Services/Status.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PBE_AssetsDownloader.Utils;
using Serilog;
using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.UI; // Added for JsonDiffWindow

namespace PBE_AssetsDownloader.Services
{
  public class Status
  {
    private const string GAME_HASHES_FILENAME = "hashes.game.txt";
    private const string LCPU_HASHES_FILENAME = "hashes.lcu.txt";

    public string CurrentStatus { get; private set; }

    // Campos para las dependencias inyectadas
    private readonly LogService _logService;
    private readonly HttpClient _httpClient;
    private readonly Requests _requests;
    private readonly AppSettings _appSettings;
    private readonly DirectoriesCreator _directoriesCreator;
    private readonly JsonDataService _jsonDataService;

    public Status(
        LogService logService,
        HttpClient httpClient,
        Requests requests,
        AppSettings appSettings,
        DirectoriesCreator directoriesCreator,
        JsonDataService jsonDataService)
    {
      _logService = logService;
      _httpClient = httpClient;
      _requests = requests;
      _appSettings = appSettings;
      _directoriesCreator = directoriesCreator;
      _jsonDataService = jsonDataService;
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
        // This method is now handled by JsonDataService or is no longer needed.
        _logService.Log("No server updates found. Local hashes are up-to-date.");
      }
    }

    public async Task<bool> IsUpdatedAsync()
    {
      try
      {
        _logService.Log("Getting update sizes from server...");
        var serverSizes = await _jsonDataService.GetRemoteHashesSizesAsync();

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
        var serverJsonDataSizes = new Dictionary<string, long>();
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
                    var regex = new Regex(@"<a href=""(?<filename>[^""]+\.json)""[^>]*>.*?<\/a><\/td><td class=""size"">(?<size>[^<]+)<\/td>", RegexOptions.Singleline);
                    foreach (Match match in regex.Matches(html))
                    {
                        string filename = match.Groups["filename"].Value;
                        long parsedSize = _jsonDataService.ParseSize(match.Groups["size"].Value);
                        string fullFileUrl = url + filename; // Construir la URL completa
                        serverJsonDataSizes[fullFileUrl] = parsedSize; // Usar la URL completa como clave
                        anyUrlProcessed = true;
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
                    var regex = new Regex(@"<a href=""(?<filename>[^""]+\.json)""[^>]*>.*?<\/a><\/td><td class=""size"">(?<size>[^<]+)<\/td>", RegexOptions.Singleline);
                    bool foundInParent = false;
                    foreach (Match match in regex.Matches(html))
                    {
                        string filenameInParent = match.Groups["filename"].Value;
                        if (url.EndsWith(filenameInParent)) // Comprobar si es nuestro archivo
                        {
                            long parsedSize = _jsonDataService.ParseSize(match.Groups["size"].Value);
                            serverJsonDataSizes[url] = parsedSize; // Usar la URL completa como clave
                            anyUrlProcessed = true;
                            foundInParent = true;
                            _logService.LogDebug($"Found {url} in parent directory listing. Size: {parsedSize}");
                            break; // Encontramos nuestro archivo, no es necesario continuar el bucle
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

        var localJsonDataSizes = _appSettings.JsonDataSizes ?? new Dictionary<string, long>();
        bool updated = false;

        foreach (var serverFile in serverJsonDataSizes)
        {
            if (!localJsonDataSizes.ContainsKey(serverFile.Key) || localJsonDataSizes[serverFile.Key] != serverFile.Value)
            {
                string message = $"File updated or new: {Path.GetFileName(serverFile.Key)} (Server Size: {_jsonDataService.FormatBytes(serverFile.Value)})";
                localJsonDataSizes[serverFile.Key] = serverFile.Value;
                updated = true;

                string jsonFileName = Path.GetFileName(serverFile.Key);
                string oldFilePath = Path.Combine(_directoriesCreator.JsonCacheOldPath, jsonFileName);
                string newFilePath = Path.Combine(_directoriesCreator.JsonCacheNewPath, jsonFileName);

                try
                {
                    if (File.Exists(newFilePath))
                    {
                        File.Copy(newFilePath, oldFilePath, true);
                    }

                    // Usar el nuevo método de Requests para descargar y guardar el JSON
                    bool downloadSuccess = await _requests.DownloadJsonContentAsync(serverFile.Key, newFilePath);

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
                        _logService.LogError($"Failed to download and save JSON content for {serverFile.Key}. Check logs for details.");
                        updated = false; // No se actualizó correctamente
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, $"Error processing JSON content for {serverFile.Key}");
                    updated = false; // Hubo un error, no se actualizó correctamente
                }
            }
        }

        if (updated)
        {
            _appSettings.JsonDataSizes = localJsonDataSizes;
            AppSettings.SaveSettings(_appSettings);
            _logService.LogSuccess("Local game data sizes updated.");
        }
        else
        {
            _logService.Log("JSON files are up-to-date.");
        }
    }
  }
}