using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PBE_AssetsDownloader.Utils;

namespace PBE_AssetsDownloader.Services
{
  public class Status
  {
    private const string GAME_HASHES_FILENAME = "hashes.game.txt";
    private const string LCPU_HASHES_FILENAME = "hashes.lcu.txt";

    private readonly LogService _logService;
    private readonly Requests _requests;
    private readonly AppSettings _appSettings;
    private readonly JsonDataService _jsonDataService;

    public Status(
        LogService logService,
        Requests requests,
        AppSettings appSettings,
        JsonDataService jsonDataService)
    {
      _logService = logService;
      _requests = requests;
      _appSettings = appSettings;
      _jsonDataService = jsonDataService;
    }

    public async Task<bool> SyncHashesIfNeeds(bool syncHashesWithCDTB, bool silent = false)
    {
      bool isUpdated = await IsUpdatedAsync(silent);
      if (isUpdated)
      {
        if (!silent) _logService.Log("Server updated. Starting hash synchronization...");
        await _requests.SyncHashesIfEnabledAsync(syncHashesWithCDTB);
        if (!silent) _logService.LogSuccess("Synchronization completed.");
        return true;
      }
      else
      {
        if (!silent) _logService.Log("No server updates found. Local hashes are up-to-date.");
        return false;
      }
    }

    public async Task<bool> IsUpdatedAsync(bool silent = false)
    {
      try
      {
        if (!silent) _logService.Log("Getting update sizes from server...");
        var serverSizes = await _jsonDataService.GetRemoteHashesSizesAsync();

        if (serverSizes == null || serverSizes.Count == 0)
        {
          _logService.LogWarning("Could not retrieve remote hash sizes or received an empty list. Skipping update check.");
          return false;
        }

        var localSizes = _appSettings.HashesSizes ?? new Dictionary<string, long>();
        bool updated = false;

        updated |= UpdateHashSizeIfDifferent(serverSizes, localSizes, GAME_HASHES_FILENAME);
        updated |= UpdateHashSizeIfDifferent(serverSizes, localSizes, LCPU_HASHES_FILENAME);

        if (updated)
        {
          _appSettings.HashesSizes = localSizes;
          AppSettings.SaveSettings(_appSettings);
          return true;
        }
        else
        {
          return false;
        }
      }
      catch (Exception ex)
      {
        _logService.LogError("Error checking for updates. See application_errors.log for details.");
        _logService.LogCritical(ex, "Status.IsUpdatedAsync Exception");
        return false;
      }
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
  }
}
