using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PBE_AssetsDownloader.Services
{
    public class JsonDataService
    {
        private readonly LogService _logService;
        private readonly HttpClient _httpClient;
        private readonly string statusUrl = "https://raw.communitydragon.org/data/hashes/lol/";

        public JsonDataService(LogService logService, HttpClient httpClient)
        {
            _logService = logService;
            _httpClient = httpClient;
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
                _logService.LogError($"HTTP request failed for '{statusUrl}': {httpEx.Message}. Check internet connection or URL.");
                return result;
            }
            catch (Exception ex)
            {
                _logService.LogError($"An unexpected exception occurred fetching URL '{statusUrl}': {ex.Message}");
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

        public long ParseSize(string sizeStr)
        {
            var culture = CultureInfo.InvariantCulture;
            sizeStr = sizeStr.Trim();

            Match numericMatch = Regex.Match(sizeStr, @"([\d\.]+)");
            if (!numericMatch.Success || !double.TryParse(numericMatch.Groups[1].Value, NumberStyles.Any, culture, out double size))
            {
                _logService.LogWarning($"Could not parse numeric part of size string: '{sizeStr}'. Defaulting to 0 bytes.");
                return 0;
            }

            if (sizeStr.EndsWith("KiB", StringComparison.OrdinalIgnoreCase)) return (long)(size * 1024);
            if (sizeStr.EndsWith("MiB", StringComparison.OrdinalIgnoreCase)) return (long)(size * 1024 * 1024);
            if (sizeStr.EndsWith("GiB", StringComparison.OrdinalIgnoreCase)) return (long)(size * 1024 * 1024 * 1024);
            if (sizeStr.EndsWith("TiB", StringComparison.OrdinalIgnoreCase)) return (long)(size * 1024L * 1024L * 1024L * 1024L);

            if (sizeStr.EndsWith("KB", StringComparison.OrdinalIgnoreCase)) return (long)(size * 1000);
            if (sizeStr.EndsWith("MB", StringComparison.OrdinalIgnoreCase)) return (long)(size * 1000 * 1000);
            if (sizeStr.EndsWith("GB", StringComparison.OrdinalIgnoreCase)) return (long)(size * 1000 * 1000 * 1000);

            if (sizeStr.EndsWith("B", StringComparison.OrdinalIgnoreCase)) return (long)size;

            return (long)size;
        }

        public string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KiB", "MiB", "GiB", "TiB" };
            int i = 0;
            double dblSByte = bytes;

            while (Math.Round(dblSByte / 1024) >= 1)
            {
                dblSByte /= 1024;
                i++;
            }

            return string.Format(CultureInfo.InvariantCulture, "{0:n1} {1}", dblSByte, Suffix[i]);
        }
    }
}
