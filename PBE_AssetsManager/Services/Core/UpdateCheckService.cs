using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Services.Downloads;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using PBE_AssetsManager.Services.Monitor;

namespace PBE_AssetsManager.Services.Core
{
    public class UpdateCheckService
    {
        private readonly AppSettings _appSettings;
        private readonly Status _status;
        private readonly JsonDataService _jsonDataService;
        private readonly UpdateManager _updateManager;
        private readonly LogService _logService;
        private readonly MonitorService _monitorService;
        private Timer _updateTimer;
        private Timer _assetTrackerTimer;

        public event Action<string, string> UpdatesFound;

        public UpdateCheckService(AppSettings appSettings, Status status, JsonDataService jsonDataService, UpdateManager updateManager, LogService logService, MonitorService monitorService)
        {
            _appSettings = appSettings;
            _status = status;
            _jsonDataService = jsonDataService;
            _updateManager = updateManager;
            _logService = logService;
            _monitorService = monitorService;
        }

        public void Start()
        {
            // Start general updates timer
            if (_appSettings.BackgroundUpdates)
            {
                if (_updateTimer == null)
                {
                    _updateTimer = new Timer();
                    _updateTimer.Elapsed += async (sender, e) => await CheckForGeneralUpdatesAsync(true);
                    _updateTimer.AutoReset = true;
                }
                _updateTimer.Interval = _appSettings.UpdateCheckFrequency * 60 * 1000;
                _updateTimer.Enabled = true;
                _logService.LogDebug($"Background update timer started. Frequency: {_appSettings.UpdateCheckFrequency} minutes.");
            }

            // Start Asset Tracker timer
            if (_appSettings.CheckAssetUpdates && _appSettings.AssetTrackerFrequency > 0)
            {
                if (_assetTrackerTimer == null)
                {
                    _assetTrackerTimer = new Timer();
                    _assetTrackerTimer.Elapsed += async (sender, e) => await CheckForAssetsAsync();
                    _assetTrackerTimer.AutoReset = true;
                }
                _assetTrackerTimer.Interval = _appSettings.AssetTrackerFrequency * 60 * 1000;
                _assetTrackerTimer.Enabled = true;
                _logService.LogDebug($"Asset Tracker timer started. Frequency: {_appSettings.AssetTrackerFrequency} minutes.");
            }
        }

        public void Stop()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Enabled = false;
                _logService.LogDebug("Background update timer stopped.");
            }
            if (_assetTrackerTimer != null)
            {
                _assetTrackerTimer.Enabled = false;
                _logService.LogDebug("Asset Tracker timer stopped.");
            }
        }

        private async Task CheckForAssetsAsync()
        {
            bool assetsUpdated = await _monitorService.CheckAllAssetCategoriesAsync(true);
            if (assetsUpdated)
            {
                UpdatesFound?.Invoke("New assets have been found.", null);
            }
        }

        public async Task CheckForGeneralUpdatesAsync(bool silent = false)
        {
            bool hashesUpdated = _appSettings.SyncHashesWithCDTB && await _status.SyncHashesIfNeeds(_appSettings.SyncHashesWithCDTB, silent);
            bool jsonUpdated = _appSettings.CheckJsonDataUpdates && await _jsonDataService.CheckJsonDataUpdatesAsync(silent);
            var (appUpdateAvailable, newVersion) = await _updateManager.IsNewVersionAvailableAsync();

            string latestVersion = appUpdateAvailable ? newVersion : null;

            if (appUpdateAvailable || jsonUpdated || (hashesUpdated && silent))
            {
                var messages = new List<string>();
                if (appUpdateAvailable) messages.Add($"Version {newVersion} is available!");
                if (hashesUpdated && silent) messages.Add("New hashes are available.");
                if (jsonUpdated) messages.Add("JSON files have been updated.");
                if (messages.Count > 0)
                {
                    UpdatesFound?.Invoke(string.Join(" | ", messages), latestVersion);
                }
            }
        }

        // This method can be called for a manual, one-time check of everything.
        public async Task CheckForAllUpdatesAsync(bool silent = false)
        {
            await CheckForGeneralUpdatesAsync(silent);
            if (_appSettings.CheckAssetUpdates)
            {
                await CheckForAssetsAsync();
            }
        }
    }
}