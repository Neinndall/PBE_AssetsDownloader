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
    private readonly JsonDataService _jsonDataService;

    public Status(
        LogService logService,
        HttpClient httpClient,
        Requests requests,
        AppSettings appSettings,
        JsonDataService jsonDataService)
    {
      _logService = logService;
      _httpClient = httpClient;
      _requests = requests;
      _appSettings = appSettings;
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
  }
}