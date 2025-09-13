using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Services.Monitor;

namespace PBE_AssetsManager.Services.Downloads
{
    public class Status
    {
        // Game Hashes
        private const string GAME_HASHES_FILENAME = "hashes.game.txt";
        private const string LCU_HASHES_FILENAME = "hashes.lcu.txt";
        
        // Bin Hashes
        private const string HASHES_BINENTRIES = "hashes.binentries.txt";
        private const string HASHES_BINFIELDS = "hashes.binfields.txt";
        private const string HASHES_BINHASHES = "hashes.binhashes.txt";
        private const string HASHES_BINTYPES = "hashes.bintypes.txt";

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

        public async Task<bool> SyncHashesIfNeeds(bool syncHashesWithCDTB, bool silent = false, Action onUpdateFound = null)
        {
            bool isUpdated = await IsUpdatedAsync(silent, onUpdateFound);
            if (isUpdated)
            {
                if (!silent) _logService.Log("Server updated. Starting hash synchronization...");
                await _requests.SyncHashesIfEnabledAsync(syncHashesWithCDTB);
                if (!silent) _logService.LogSuccess("Synchronization completed.");
                return true;
            }

            if (!silent) _logService.Log("No server updates found. Local hashes are up-to-date.");
            return false;
        }

        public async Task<bool> IsUpdatedAsync(bool silent = false, Action onUpdateFound = null)
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
                bool notificationSent = false;

                void CheckAndUpdate(string filename)
                {
                    if (UpdateHashSizeIfDifferent(serverSizes, localSizes, filename))
                    {
                        updated = true;
                        if (!notificationSent)
                        {
                            onUpdateFound?.Invoke();
                            notificationSent = true;
                        }
                    }
                }

                CheckAndUpdate(GAME_HASHES_FILENAME);
                CheckAndUpdate(LCU_HASHES_FILENAME);
                CheckAndUpdate(HASHES_BINENTRIES);
                CheckAndUpdate(HASHES_BINFIELDS);
                CheckAndUpdate(HASHES_BINHASHES);
                CheckAndUpdate(HASHES_BINTYPES);

                if (updated)
                {
                    _appSettings.HashesSizes = localSizes;
                    AppSettings.SaveSettings(_appSettings);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error checking for updates.");
                return false;
            }
        }

        private bool UpdateHashSizeIfDifferent(
            Dictionary<string, long> serverSizes,
            Dictionary<string, long> localSizes,
            string filename)
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