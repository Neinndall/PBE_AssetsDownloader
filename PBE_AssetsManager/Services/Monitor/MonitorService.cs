using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Models;
using System.Text.RegularExpressions;
using PBE_AssetsManager.Services.Core;

namespace PBE_AssetsManager.Services.Monitor
{
    public class MonitorService
    {
        private readonly AppSettings _appSettings;
        private readonly JsonDataService _jsonDataService;
        private readonly LogService _logService;
        private readonly HttpClient _httpClient;

        public ObservableCollection<MonitoredUrl> MonitoredItems { get; } = new ObservableCollection<MonitoredUrl>();

        public event Action<AssetCategory> CategoryCheckStarted;
        public event Action<AssetCategory> CategoryCheckCompleted;

        public MonitorService(AppSettings appSettings, JsonDataService jsonDataService, LogService logService, HttpClient httpClient)
        {
            _appSettings = appSettings;
            _jsonDataService = jsonDataService;
            _logService = logService;
            _httpClient = httpClient;

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
            var item = MonitoredItems.FirstOrDefault(x => x.Url == fileUpdateInfo.FullUrl);
            if (item != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    item.StatusText = "Updated";
                    item.StatusColor = new SolidColorBrush(Colors.DodgerBlue);
                    item.LastChecked = $"Last Update: {fileUpdateInfo.Timestamp:yyyy-MMM-dd HH:mm}";
                    item.HasChanges = true;
                    item.OldFilePath = fileUpdateInfo.OldFilePath;
                    item.NewFilePath = fileUpdateInfo.NewFilePath;
                });
                _appSettings.JsonDataModificationDates[fileUpdateInfo.FullUrl] = fileUpdateInfo.Timestamp;
                AppSettings.SaveSettings(_appSettings);
            }
        }

        private string GetAliasForUrl(string url)
        {
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

        #region Asset Tracker

        private readonly Dictionary<string, ObservableCollection<TrackedAsset>> _assetCache = new Dictionary<string, ObservableCollection<TrackedAsset>>();
        public List<AssetCategory> AssetCategories { get; private set; } = new List<AssetCategory>();

        public void InvalidateAssetCacheForCategory(AssetCategory category)
        {
            if (category != null) _assetCache.Remove(category.Id);
        }

        public void LoadAssetCategories()
        {
            AssetCategories = DefaultCategories.Get();
            foreach (var category in AssetCategories)
            {
                if (_appSettings.AssetTrackerProgress.TryGetValue(category.Id, out long lastValid)) category.LastValid = lastValid;
                if (_appSettings.AssetTrackerFailedIds.TryGetValue(category.Id, out var failedIds)) category.FailedUrls = new List<long>(failedIds);
                if (_appSettings.AssetTrackerFoundIds.TryGetValue(category.Id, out var foundIds)) category.FoundUrls = new List<long>(foundIds);
                if (_appSettings.AssetTrackerUrlOverrides.TryGetValue(category.Id, out var overrides)) category.FoundUrlOverrides = new Dictionary<long, string>(overrides);
            }
        }

        public ObservableCollection<TrackedAsset> GetAssetListForCategory(AssetCategory category)
        {
            if (category == null) return new ObservableCollection<TrackedAsset>();
            if (_assetCache.TryGetValue(category.Id, out var cachedList)) return cachedList;

            var initialAssets = GenerateNewAssetList(category);
            var newList = new ObservableCollection<TrackedAsset>(initialAssets);
            _assetCache[category.Id] = newList;
            return newList;
        }

        private List<TrackedAsset> GenerateNewAssetList(AssetCategory category)
        {
            var assets = new List<TrackedAsset>();
            if (category == null) return assets;

            var foundIds = new HashSet<long>(category.FoundUrls);
            var failedIds = new HashSet<long>(category.FailedUrls);
            long startNumber = category.LastValid > 0 ? Math.Max(category.Start, category.LastValid - 5) : category.Start;

            for (int i = 0; i < 10; i++)
            {
                long currentNumber = startNumber + i;
                string url = category.FoundUrlOverrides.TryGetValue(currentNumber, out var overrideUrl) ? overrideUrl : $"{category.BaseUrl}{currentNumber}.{category.Extension}";

                try
                {
                    string status = foundIds.Contains(currentNumber) ? "OK" : failedIds.Contains(currentNumber) ? "Not Found" : "Pending";
                    string displayName = Path.GetFileName(new Uri(url).AbsolutePath);
                    if (string.IsNullOrEmpty(displayName)) displayName = $"Asset ID: {currentNumber}";

                    assets.Add(new TrackedAsset
                    {
                        Url = url,
                        DisplayName = displayName,
                        Status = status,
                        Thumbnail = status == "OK" ? url : null
                    });
                }
                catch (UriFormatException)
                {
                    assets.Add(new TrackedAsset { Url = url, DisplayName = "Invalid URL format", Status = "Error" });
                }
            }
            return assets;
        }

        public List<TrackedAsset> GenerateMoreAssets(ObservableCollection<TrackedAsset> currentAssets, AssetCategory category, int amountToAdd)
        {
            var newAssets = new List<TrackedAsset>();
            if (category == null) return newAssets;

            long lastNumber = currentAssets.Any() ? GetAssetIdFromUrl(currentAssets.Last().Url) ?? 0 : category.Start - 1;

            for (int i = 1; i <= amountToAdd; i++)
            {
                long currentNumber = lastNumber + i;
                var url = $"{category.BaseUrl}{currentNumber}.{category.Extension}";
                newAssets.Add(new TrackedAsset { Url = url, DisplayName = Path.GetFileName(new Uri(url).AbsolutePath), Status = "Pending" });
            }
            return newAssets;
        }

        private long? GetAssetIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            var match = Regex.Match(url, @"(\d+)(?!.*\d)");
            return match.Success && long.TryParse(match.Value, out long assetId) ? assetId : null;
        }

        private async Task<(bool IsSuccess, string FoundUrl)> PerformCheckAsync(long id, AssetCategory category)
        {
            string primaryUrl = $"{category.BaseUrl}{id}.{category.Extension}";
            using var request = new HttpRequestMessage(HttpMethod.Head, primaryUrl);
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode) return (true, primaryUrl);

            if (category.Id == "3" || category.Id == "11")
            {
                string fallbackUrl = $"{category.BaseUrl}{id}.png";
                using var fallbackRequest = new HttpRequestMessage(HttpMethod.Head, fallbackUrl);
                var fallbackResponse = await _httpClient.SendAsync(fallbackRequest);
                if (fallbackResponse.IsSuccessStatusCode) return (true, fallbackUrl);
            }

            return (false, null);
        }

        public async Task CheckAssetsAsync(List<TrackedAsset> assetsToCheck, AssetCategory category, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() => category.Status = CategoryStatus.Checking);
            try
            {
                bool progressChanged = false;
                foreach (var asset in assetsToCheck)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    long? assetId = GetAssetIdFromUrl(asset.Url);
                    if (!assetId.HasValue) continue;

                    var (isSuccess, foundUrl) = await PerformCheckAsync(assetId.Value, category);

                    if (isSuccess)
                    {
                        asset.Status = "OK";
                        asset.Thumbnail = foundUrl;
                        if (!category.FoundUrls.Contains(assetId.Value)) { category.FoundUrls.Add(assetId.Value); progressChanged = true; }
                        if (foundUrl != asset.Url) { category.FoundUrlOverrides[assetId.Value] = foundUrl; progressChanged = true; }
                        if (category.FailedUrls.Remove(assetId.Value)) progressChanged = true;
                    }
                    else
                    {
                        asset.Status = "Not Found";
                        if (!category.FailedUrls.Contains(assetId.Value)) { category.FailedUrls.Add(assetId.Value); progressChanged = true; }
                    }
                }

                if (progressChanged) SaveCategoryProgress(category);
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    category.Status = CategoryStatus.CompletedSuccess;
                    await Task.Delay(3000);
                    category.Status = CategoryStatus.Idle;
                });
            }
        }

        public async Task<bool> CheckAllAssetCategoriesAsync(bool silent)
        {
            if (!AssetCategories.Any()) LoadAssetCategories();
            bool anyNewAssetFound = false;
            foreach (var category in AssetCategories)
            {
                if (await _CheckCategoryAsync(category, silent)) anyNewAssetFound = true;
            }
            return anyNewAssetFound;
        }

        private async Task<bool> _CheckCategoryAsync(AssetCategory category, bool silent)
        {
            Application.Current.Dispatcher.Invoke(() => category.Status = CategoryStatus.Checking);
            try
            {
                CategoryCheckStarted?.Invoke(category);
                bool anyNewAssetFound = false;
                bool progressChanged = false;

                var idsToCheck = new HashSet<long>(category.FailedUrls);
                long highestKnownId = 0;
                if (category.FoundUrls.Any()) highestKnownId = Math.Max(highestKnownId, category.FoundUrls.Max());
                if (category.FailedUrls.Any()) highestKnownId = Math.Max(highestKnownId, category.FailedUrls.Max());
                long startNumber = highestKnownId > 0 ? highestKnownId + 1 : category.Start;

                for (int i = 0; i < 10; i++) idsToCheck.Add(startNumber + i);

                foreach (long id in idsToCheck.OrderBy(i => i))
                {
                    var (isSuccess, foundUrl) = await PerformCheckAsync(id, category);
                    if (isSuccess)
                    {
                        if (!category.FoundUrls.Contains(id)) { category.FoundUrls.Add(id); anyNewAssetFound = true; progressChanged = true; }
                        string primaryUrl = $"{category.BaseUrl}{id}.{category.Extension}";
                        if (foundUrl != primaryUrl) { category.FoundUrlOverrides[id] = foundUrl; progressChanged = true; }
                        if (category.FailedUrls.Remove(id)) progressChanged = true;
                    }
                    else
                    {
                        if (!category.FailedUrls.Contains(id)) { category.FailedUrls.Add(id); progressChanged = true; }
                    }
                }

                if (progressChanged) SaveCategoryProgress(category);
                CategoryCheckCompleted?.Invoke(category);
                return anyNewAssetFound;
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    category.Status = CategoryStatus.CompletedSuccess;
                    await Task.Delay(3000);
                    category.Status = CategoryStatus.Idle;
                });
            }
        }

        private void SaveCategoryProgress(AssetCategory category)
        {
            if (category.FoundUrls.Any()) category.LastValid = category.FoundUrls.Max();
            _appSettings.AssetTrackerProgress[category.Id] = category.LastValid;
            _appSettings.AssetTrackerFailedIds[category.Id] = category.FailedUrls;
            _appSettings.AssetTrackerFoundIds[category.Id] = category.FoundUrls;
            _appSettings.AssetTrackerUrlOverrides[category.Id] = category.FoundUrlOverrides;
            AppSettings.SaveSettings(_appSettings);
        }

        #endregion
    }
}