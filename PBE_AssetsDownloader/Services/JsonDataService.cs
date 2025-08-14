using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PBE_AssetsDownloader.Info;
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
        private readonly IServiceProvider _serviceProvider;
        private readonly string statusUrl = "https://raw.communitydragon.org/data/hashes/lol/";

        private readonly HashSet<string> _filesRequiringUniquePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "trans.json",
        };

        public JsonDataService(LogService logService, HttpClient httpClient, AppSettings appSettings, DirectoriesCreator directoriesCreator, Requests requests, IServiceProvider serviceProvider)
        {
            _logService = logService;
            _httpClient = httpClient;
            _appSettings = appSettings;
            _directoriesCreator = directoriesCreator;
            _requests = requests;
            _serviceProvider = serviceProvider;
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
                _logService.LogError($"HTTP request failed for '{statusUrl}'. See application_errors.log for details.");
                _logService.LogCritical(httpEx, $"JsonDataService.GetRemoteHashesSizesAsync HttpRequestException for URL: {statusUrl}");
                return result;
            }
            catch (Exception ex)
            {
                _logService.LogError($"An unexpected exception occurred fetching URL '{statusUrl}'. See application_errors.log for details.");
                _logService.LogCritical(ex, $"JsonDataService.GetRemoteHashesSizesAsync Exception for URL: {statusUrl}");
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

        public async Task<bool> CheckJsonDataUpdatesAsync(bool silent = false)
        {
            if (!_appSettings.CheckJsonDataUpdates || (_appSettings.MonitoredJsonDirectories == null && _appSettings.MonitoredJsonFiles == null))
            {
                return false;
            }

            if (!silent) _logService.Log("Checking for JSON file updates...");
            var serverJsonDataEntries = new Dictionary<string, (DateTime Date, string FullUrl)>();
            bool anyUrlProcessed = false;

            if (_appSettings.MonitoredJsonDirectories != null)
            {
                foreach (var url in _appSettings.MonitoredJsonDirectories)
                {
                    if (string.IsNullOrWhiteSpace(url)) continue;
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
                                string fullFileUrl = url + filename;
                                string key = _filesRequiringUniquePaths.Contains(filename) ? PathUtils.GetUniqueLocalPathFromJsonUrl(fullFileUrl) : filename;
                                serverJsonDataEntries[key] = (parsedDate, fullFileUrl);
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
                        _logService.LogError($"Error processing monitored directory URL: {url}. See application_errors.log for details.");
                        _logService.LogCritical(ex, $"JsonDataService.CheckJsonDataUpdates Exception for directory URL: {url}");
                    }
                }
            }

            if (_appSettings.MonitoredJsonFiles != null)
            {
                foreach (var url in _appSettings.MonitoredJsonFiles)
                {
                    if (string.IsNullOrWhiteSpace(url)) continue;
                    string parentDirectoryUrl = string.Empty;
                    try
                    {
                        Uri fileUri = new Uri(url);
                        parentDirectoryUrl = new Uri(fileUri, ".").ToString();
                        string html = await _httpClient.GetStringAsync(parentDirectoryUrl);
                        var regex = new Regex(@"<a href=""(?<filename>[^""]+\.json)""[^>]*>.*?<\/a><\/td><td class=""size"">.*?<\/td><td class=""date"">(?<date>[^<]+)<\/td>", RegexOptions.Singleline);
                        bool foundInParent = false;
                        foreach (Match match in regex.Matches(html))
                        {
                            string filenameInParent = match.Groups["filename"].Value;
                            if (url.EndsWith(filenameInParent))
                            {
                                string dateStr = match.Groups["date"].Value.Trim();
                                if (ParseDate(dateStr, out DateTime parsedDate))
                                {
                                    string key = _filesRequiringUniquePaths.Contains(filenameInParent) ? PathUtils.GetUniqueLocalPathFromJsonUrl(url) : filenameInParent;
                                    serverJsonDataEntries[key] = (parsedDate, url);
                                    anyUrlProcessed = true;
                                    foundInParent = true;
                                    _logService.LogDebug($"Found {url} in parent directory listing. Date: {parsedDate}");
                                    break;
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
                        _logService.LogError($"Error fetching or parsing parent directory {parentDirectoryUrl} for file {url}. See application_errors.log for details.");
                        _logService.LogCritical(ex, $"JsonDataService.CheckJsonDataUpdates Exception for file URL: {url}");
                    }
                }
            }

            if (!anyUrlProcessed)
            {
                _logService.LogWarning("No JSON files could be processed from the configured URLs.");
                return false;
            }

            var localJsonDataDates = _appSettings.JsonDataModificationDates ?? new Dictionary<string, DateTime>();
            bool wasUpdated = false;

            foreach (var serverEntry in serverJsonDataEntries)
            {
                string key = serverEntry.Key;
                DateTime serverDate = serverEntry.Value.Date;
                string fullUrl = serverEntry.Value.FullUrl;

                if (!localJsonDataDates.ContainsKey(key) || localJsonDataDates[key] != serverDate)
                {
                    localJsonDataDates[key] = serverDate;
                    wasUpdated = true;

                    string oldFilePath = Path.Combine(_directoriesCreator.JsonCacheOldPath, key);
                    string newFilePath = Path.Combine(_directoriesCreator.JsonCacheNewPath, key);

                    Directory.CreateDirectory(Path.GetDirectoryName(oldFilePath));
                    Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));

                    try
                    {
                        if (File.Exists(newFilePath))
                        {
                            File.Copy(newFilePath, oldFilePath, true);
                        }

                        bool downloadSuccess = await _requests.DownloadJsonContentAsync(fullUrl, newFilePath);

                        if (downloadSuccess)
                        {
                            if (_appSettings.SaveDiffHistory && File.Exists(oldFilePath))
                            {
                                // --- START: New robust history saving logic ---
                                
                                // 1. Create a unique directory for this specific change based on timestamp
                                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                                string fileHistoryBasePath = Path.Combine(_directoriesCreator.JsonCacheHistoryPath, key);
                                string changeInstancePath = Path.Combine(fileHistoryBasePath, timestamp);
                                Directory.CreateDirectory(changeInstancePath);

                                // 2. Define static paths for the old and new versions inside the unique directory
                                string historyOldFilePath = Path.Combine(changeInstancePath, $"old_{Path.GetFileName(key)}");
                                string historyNewFilePath = Path.Combine(changeInstancePath, $"new_{Path.GetFileName(key)}");

                                // 3. Copy both files to the new location to make them immutable
                                File.Copy(oldFilePath, historyOldFilePath, true);
                                File.Copy(newFilePath, historyNewFilePath, true);

                                // 4. Add an entry to the history pointing to these static, immutable files
                                _appSettings.DiffHistory.Add(new JsonDiffHistoryEntry
                                {
                                    FileName = key, // The key is the unique relative path
                                    OldFilePath = historyOldFilePath,
                                    NewFilePath = historyNewFilePath,
                                    Timestamp = DateTime.Now
                                });
                                
                                // --- END: New robust history saving logic ---
                            }

                            _logService.LogDebug($"Saved new JSON content to {newFilePath}");
                            _logService.LogInteractive(
                                $"Updated: {key} ({serverDate:yyyy-MMM-dd HH:mm})",
                                "View Diff",
                                () =>
                                {
                                    string oldContent = File.Exists(oldFilePath) ? File.ReadAllText(oldFilePath) : "";
                                    string newContent = File.Exists(newFilePath) ? File.ReadAllText(newFilePath) : "";
                                    var diffWindow = _serviceProvider.GetRequiredService<JsonDiffWindow>();
                                    diffWindow.LoadDiff(oldContent, newContent);
                                    diffWindow.Show();
                                }
                            );
                        }
                        else
                        {
                            _logService.LogError($"Failed to download and save JSON content for {fullUrl}.");
                            wasUpdated = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError($"Error processing JSON content for {fullUrl}. See application_errors.log for details.");
                        _logService.LogCritical(ex, $"JsonDataService.CheckJsonDataUpdates Exception for URL: {fullUrl}");
                        wasUpdated = false;
                    }
                }
            }

            if (wasUpdated)
            {
                _appSettings.JsonDataModificationDates = localJsonDataDates;
                AppSettings.SaveSettings(_appSettings);
                if (!silent) _logService.LogSuccess("Local game data dates updated.");
            }
            else
            {
                if (!silent) _logService.Log("JSON files are up-to-date.");
            }
            return wasUpdated;
        }

        private bool ParseDate(string dateStr, out DateTime date)
        {
            return DateTime.TryParseExact(dateStr, "yyyy-MMM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
    }
}
