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
using System.Windows.Media.Imaging;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Models;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using PBE_AssetsManager.Services.Core;

namespace PBE_AssetsManager.Services.Monitor
{
    public class MonitorService
    {
        private readonly AppSettings _appSettings;
        private readonly JsonDataService _jsonDataService;
        private readonly LogService _logService;
        private readonly DiffViewService _diffViewService;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private readonly HttpClient _httpClient;

        public ObservableCollection<MonitoredUrl> MonitoredItems { get; } = new ObservableCollection<MonitoredUrl>();

        public MonitorService(AppSettings appSettings, JsonDataService jsonDataService, LogService logService, DiffViewService diffViewService, CustomMessageBoxService customMessageBoxService, HttpClient httpClient)
        {
            _appSettings = appSettings;
            _jsonDataService = jsonDataService;
            _logService = logService;
            _diffViewService = diffViewService;
            _customMessageBoxService = customMessageBoxService;
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

        #region Asset Tracker

        private readonly Dictionary<string, ObservableCollection<TrackedAsset>> _assetCache = new Dictionary<string, ObservableCollection<TrackedAsset>>();
        public List<AssetCategory> AssetCategories { get; private set; } = new List<AssetCategory>();

        public void LoadAssetCategories()
        {
            AssetCategories = DefaultCategories.Get();
            foreach (var category in AssetCategories)
            {
                if (_appSettings.AssetTrackerProgress.TryGetValue(category.Id, out long lastValid))
                {
                    category.LastValid = lastValid;
                }
                if (_appSettings.AssetTrackerFailedIds.TryGetValue(category.Id, out var failedIds))
                {
                    category.FailedUrls = new List<long>(failedIds); // Create a copy
                }
                if (_appSettings.AssetTrackerFoundIds.TryGetValue(category.Id, out var foundIds))
                {
                    category.FoundUrls = new List<long>(foundIds); // Create a copy
                }
            }
        }

        public ObservableCollection<TrackedAsset> GetAssetListForCategory(AssetCategory category)
        {
            if (category == null) return new ObservableCollection<TrackedAsset>();

            if (_assetCache.TryGetValue(category.Id, out var cachedList))
            {
                return cachedList;
            }

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
                var url = $"{category.BaseUrl}{currentNumber}.{category.Extension}";

                string initialStatus;
                if (foundIds.Contains(currentNumber) || currentNumber <= category.LastValid)
                {
                    initialStatus = "OK";
                }
                else if (failedIds.Contains(currentNumber))
                {
                    initialStatus = "Not Found";
                }
                else
                {
                    initialStatus = "Pending";
                }

                assets.Add(new TrackedAsset
                {
                    Url = url,
                    DisplayName = Path.GetFileName(new Uri(url).AbsolutePath),
                    Status = initialStatus,
                    Thumbnail = initialStatus == "OK" ? url : null
                });
            }

            return assets;
        }

        public List<TrackedAsset> GenerateMoreAssets(ObservableCollection<TrackedAsset> currentAssets, AssetCategory category, int amountToAdd)
        {
            var newAssets = new List<TrackedAsset>();
            if (category == null) return newAssets;

            long lastNumber = 0;
            if (currentAssets.Any())
            {
                var lastUrl = currentAssets.Last().Url;
                lastNumber = GetAssetIdFromUrl(lastUrl) ?? 0;
            }
            else
            {
                lastNumber = category.Start - 1;
            }

            for (int i = 1; i <= amountToAdd; i++)
            {
                long currentNumber = lastNumber + i;
                var url = $"{category.BaseUrl}{currentNumber}.{category.Extension}";
                newAssets.Add(new TrackedAsset
                {
                    Url = url,
                    DisplayName = Path.GetFileName(new Uri(url).AbsolutePath),
                    Status = "Pending"
                });
            }

            return newAssets;
        }

        private long? GetAssetIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            var match = Regex.Match(url, @"(\d+)(?!.*\d)");
            if (match.Success && long.TryParse(match.Value, out long assetId))
            {
                return assetId;
            }
            return null;
        }

        public async Task CheckAssetsAsync(List<TrackedAsset> assetsToCheck, AssetCategory category, CancellationToken cancellationToken)
        {
            bool stateChanged = false;

            foreach (var asset in assetsToCheck)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Head, asset.Url))
                    {
                        var response = await _httpClient.SendAsync(request, cancellationToken);
                        long? assetId = GetAssetIdFromUrl(asset.Url);

                        if (response.IsSuccessStatusCode)
                        {
                            asset.Status = "OK";
                            asset.Thumbnail = asset.Url;

                            if (assetId.HasValue)
                            {
                                if (!category.FoundUrls.Contains(assetId.Value))
                                {
                                    category.FoundUrls.Add(assetId.Value);
                                    stateChanged = true;
                                }

                                if (category.FailedUrls.Remove(assetId.Value))
                                {
                                    stateChanged = true;
                                }
                            }
                        }
                        else
                        {
                            asset.Status = "Not Found";
                            if (assetId.HasValue)
                            {
                                if (!category.FailedUrls.Contains(assetId.Value))
                                {
                                    category.FailedUrls.Add(assetId.Value);
                                    stateChanged = true;
                                }
                            }
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    asset.Status = "Not Found";
                }
            }

            if (stateChanged)
            {
                if (category.FoundUrls.Any())
                {
                    category.LastValid = category.FoundUrls.Max();
                }
                _appSettings.AssetTrackerProgress[category.Id] = category.LastValid;
                _appSettings.AssetTrackerFailedIds[category.Id] = category.FailedUrls;
                _appSettings.AssetTrackerFoundIds[category.Id] = category.FoundUrls;

                AppSettings.SaveSettings(_appSettings);
            }
        }



        public async Task<bool> CheckSingleCategoryAsync(AssetCategory category, bool silent)
        {
            if (!AssetCategories.Contains(category))
            {
                AssetCategories.Add(category);
            }
            return await _CheckCategoryAsync(category, silent);
        }

        public async Task<bool> CheckAllAssetCategoriesAsync(bool silent)
        {
            bool anyNewAssetFound = false;

            if (!AssetCategories.Any())
            {
                LoadAssetCategories();
            }

            foreach (var category in AssetCategories)
            {
                if(await _CheckCategoryAsync(category, silent))
                {
                    anyNewAssetFound = true;
                }
            }

            if (anyNewAssetFound && !silent)
            {
                // Here we could trigger a more direct notification if needed
            }

            return anyNewAssetFound;
        }

        private async Task<bool> _CheckCategoryAsync(AssetCategory category, bool silent)
        {
            bool anyNewAssetFound = false;
            bool progressChanged = false; // New flag
            _logService.LogDebug($"Checking category: {category.Name}");

            // Phase 1: Re-check previously failed URLs (the "gaps")
            if (category.FailedUrls.Any())
            {
                _logService.LogDebug($"Re-checking {category.FailedUrls.Count} previously failed URLs for {category.Name}.");
                var tasks = category.FailedUrls.Select(async id =>
                {
                    var url = $"{category.BaseUrl}{id}.{category.Extension}";
                    using var request = new HttpRequestMessage(HttpMethod.Head, url);
                    var response = await _httpClient.SendAsync(request);
                    return new { Id = id, IsSuccess = response.IsSuccessStatusCode, Url = url };
                }).ToList();

                var results = await Task.WhenAll(tasks);

                var newlyFoundIds = results.Where(r => r.IsSuccess).Select(r => r.Id).ToList();
                if (newlyFoundIds.Any())
                {
                    _logService.LogDebug($"Found {newlyFoundIds.Count} new assets in previously failed gaps for {category.Name}.");
                    anyNewAssetFound = true;
                    progressChanged = true; // A change occurred
                    category.HasNewAssets = true;
                    foreach (var id in newlyFoundIds)
                    {
                        category.FoundUrls.Add(id);
                        category.FailedUrls.Remove(id);
                        _logService.LogDebug($"Filled gap for category '{category.Name}': {category.BaseUrl}{id}.{category.Extension}");
                    }
                }
            }

            // Phase 2: Search for new assets beyond the highest known ID
            const int maxConsecutiveErrors = 5;
            int consecutiveErrors = 0;
            long highestKnownId = 0;
            if (category.FoundUrls.Any()) highestKnownId = Math.Max(highestKnownId, category.FoundUrls.Max());
            if (category.FailedUrls.Any()) highestKnownId = Math.Max(highestKnownId, category.FailedUrls.Max());

            long currentNumber = highestKnownId > 0 ? highestKnownId + 1 : category.Start;

            _logService.LogDebug($"Starting new asset search for {category.Name} from ID {currentNumber}.");

            while (consecutiveErrors < maxConsecutiveErrors)
            {
                const int batchSize = 5;
                var batchIds = Enumerable.Range(0, batchSize).Select(i => Convert.ToInt64(currentNumber) + i).ToList();

                _logService.LogDebug($"Checking batch of {batchSize} from {batchIds.First()} to {batchIds.Last()}");

                var tasks = batchIds.Select(async id =>
                {
                    var url = $"{category.BaseUrl}{id}.{category.Extension}";
                    try
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Head, url);
                        var response = await _httpClient.SendAsync(request);
                        return new { Id = id, IsSuccess = response.IsSuccessStatusCode, Url = url };
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError(ex, $"Error checking asset URL: {url}");
                        return new { Id = id, IsSuccess = false, Url = url };
                    }
                }).ToList();

                var results = await Task.WhenAll(tasks);

                // Process results in order to correctly calculate consecutive errors
                foreach (var result in results.OrderBy(r => r.Id))
                {
                    if (consecutiveErrors >= maxConsecutiveErrors)
                    {
                        break; // Stop processing batch if limit was reached by a previous item in this same batch
                    }

                    if (result.IsSuccess)
                    {
                        if (!category.FoundUrls.Contains(result.Id))
                        {
                            category.FoundUrls.Add(result.Id);
                            category.HasNewAssets = true;
                            anyNewAssetFound = true;
                            progressChanged = true; // A change occurred
                            _logService.LogDebug($"New asset found for category '{category.Name}': {result.Url}");
                        }
                        consecutiveErrors = 0; // Reset on success
                    }
                    else
                    {
                        if (!category.FailedUrls.Contains(result.Id))
                        {
                            category.FailedUrls.Add(result.Id);
                            progressChanged = true; // A change occurred
                        }
                        consecutiveErrors++;
                    }
                }

                currentNumber += batchSize;
            }

            // Phase 3: Save the complete progress
            if (progressChanged) // Use the new flag here
            {
                if (category.FoundUrls.Any())
                {
                    category.LastValid = category.FoundUrls.Max();
                }
                _appSettings.AssetTrackerProgress[category.Id] = category.LastValid;
                _appSettings.AssetTrackerFailedIds[category.Id] = category.FailedUrls;
                _appSettings.AssetTrackerFoundIds[category.Id] = category.FoundUrls;

                AppSettings.SaveSettings(_appSettings);
            }

            return anyNewAssetFound;
        }

        #endregion
    }
}