using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PBE_AssetsManager.Services
{
    public class MonitorService
    {
        private readonly AppSettings _appSettings;
        private readonly JsonDataService _jsonDataService;
        private readonly LogService _logService;
        private readonly DiffViewService _diffViewService;
        private readonly CustomMessageBoxService _customMessageBoxService;

        public ObservableCollection<MonitoredUrl> MonitoredItems { get; } = new ObservableCollection<MonitoredUrl>();

        public MonitorService(AppSettings appSettings, JsonDataService jsonDataService, LogService logService, DiffViewService diffViewService, CustomMessageBoxService customMessageBoxService)
        {
            _appSettings = appSettings;
            _jsonDataService = jsonDataService;
            _logService = logService;
            _diffViewService = diffViewService;
            _customMessageBoxService = customMessageBoxService;

            LoadMonitoredUrls();

            _jsonDataService.FileUpdated += OnFileUpdated;
        }

        public void LoadMonitoredUrls()
        {
            MonitoredItems.Clear();
            foreach (var url in _appSettings.MonitoredJsonFiles)
            {
                _appSettings.JsonDataModificationDates.TryGetValue(url, out DateTime lastUpdated);

                string statusText = "Pending check...";
                string lastChecked = "Never";
                Brush statusColor = new SolidColorBrush(Colors.Gray);

                if (lastUpdated != DateTime.MinValue)
                {
                    statusText = "Up-to-date";
                    lastChecked = $"Last Update: {lastUpdated:yyyy-MMM-dd HH:mm}";
                    statusColor = new SolidColorBrush(Colors.MediumSeaGreen);
                }

                MonitoredItems.Add(new MonitoredUrl
                {
                    Alias = GetAliasForUrl(url),
                    Url = url,
                    StatusText = statusText,
                    StatusColor = statusColor,
                    LastChecked = lastChecked,
                    HasChanges = false
                });
            }
        }

        private void OnFileUpdated(FileUpdateInfo fileUpdateInfo)
        {
            _logService.LogDebug($"MonitorService: FileUpdated event received for URL: {fileUpdateInfo.FullUrl}");
            var item = MonitoredItems.FirstOrDefault(x => x.Url == fileUpdateInfo.FullUrl);

            if (item != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _logService.LogDebug($"MonitorService: Found matching item with URL: {item.Url}. Updating status on UI thread.");
                    item.StatusText = "Updated";
                    item.StatusColor = new SolidColorBrush(Colors.DodgerBlue);
                    item.LastChecked = $"Last Update: {fileUpdateInfo.Timestamp:yyyy-MMM-dd HH:mm}";
                    item.HasChanges = true;
                    item.OldFilePath = fileUpdateInfo.OldFilePath;
                    item.NewFilePath = fileUpdateInfo.NewFilePath;
                });

                // Persisting settings can still happen on the background thread.
                _appSettings.JsonDataModificationDates[fileUpdateInfo.FullUrl] = fileUpdateInfo.Timestamp;
                AppSettings.SaveSettings(_appSettings);
            }
            else
            {
                _logService.LogWarning($"MonitorService: Could not find a matching monitored item for URL: {fileUpdateInfo.FullUrl}");
                _logService.LogDebug("MonitorService: Currently monitored URLs:");
                foreach (var monitoredItem in MonitoredItems)
                {
                    _logService.LogDebug($"- {monitoredItem.Url}");
                }
            }
        }

        private string GetAliasForUrl(string url)
        {
            // Simple alias generation, can be improved
            try
            {
                var uri = new Uri(url);
                return uri.Segments.Last();
            }
            catch
            {
                return url;
            }
        }
    }
}