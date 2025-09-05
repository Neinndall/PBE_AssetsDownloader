using PBE_AssetsManager.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace PBE_AssetsManager.Services
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
            if (_appSettings.BackgroundUpdates)
            {
                if (_updateTimer == null)
                {
                    _updateTimer = new Timer();
                    _updateTimer.Elapsed += async (sender, e) => await CheckForAllUpdatesAsync(true);
                    _updateTimer.AutoReset = true;
                }
                _updateTimer.Interval = _appSettings.UpdateCheckFrequency * 60 * 1000;
                _updateTimer.Enabled = true;
                _logService.LogDebug($"Background update timer started. Frequency: {_appSettings.UpdateCheckFrequency} minutes.");
            }
        }

        public void Stop()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Enabled = false;
                _logService.LogDebug("Background update timer stopped.");
            }
        }

        public async Task CheckForAllUpdatesAsync(bool silent = false)
        {
            bool hashesUpdated = _appSettings.SyncHashesWithCDTB && await _status.SyncHashesIfNeeds(_appSettings.SyncHashesWithCDTB, silent);
            bool jsonUpdated = _appSettings.CheckJsonDataUpdates && await _jsonDataService.CheckJsonDataUpdatesAsync(silent);
            bool assetsUpdated = _appSettings.CheckAssetUpdates && await _monitorService.CheckAllAssetCategoriesAsync(silent);
            var (appUpdateAvailable, newVersion) = await _updateManager.IsNewVersionAvailableAsync();

            string latestVersion = appUpdateAvailable ? newVersion : null;

            if (appUpdateAvailable || jsonUpdated || (hashesUpdated && silent) || assetsUpdated)
            {
                var messages = new List<string>();
                if (appUpdateAvailable) messages.Add($"Version {newVersion} is available!");
                if (hashesUpdated && silent) messages.Add("New hashes are available.");
                if (jsonUpdated) messages.Add("JSON files have been updated.");
                if (assetsUpdated) messages.Add("New assets have been found.");
                if (messages.Count > 0)
                {
                    UpdatesFound?.Invoke(string.Join(" | ", messages), latestVersion);
                }
            }
        }
    }
}
