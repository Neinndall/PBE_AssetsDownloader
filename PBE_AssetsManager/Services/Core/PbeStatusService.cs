using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PBE_AssetsManager.Services.Core
{
    public class PbeStatusService
    {
        private readonly HttpClient _httpClient;
        private readonly LogService _logService;
        private string _lastStatusMessage = "";
        private const string PbeStatusUrl = "https://lol.secure.dyn.riotcdn.net/channels/public/x/status/pbe.json";

        // Dictionary to map common timezone abbreviations to their UTC offsets.
        private static readonly Dictionary<string, TimeSpan> TimeZoneAbbreviations = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase)
        {
            { "PDT", TimeSpan.FromHours(-7) },
            { "PST", TimeSpan.FromHours(-8) },
            { "UTC", TimeSpan.FromHours(0) },
            // Add more as needed
        };

        public PbeStatusService(LogService logService)
        {
            _httpClient = new HttpClient();
            _logService = logService;
        }

        public async Task<string> CheckPbeStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(PbeStatusUrl);
                string currentStatus = ExtractStatus(response);

                if (!string.IsNullOrEmpty(currentStatus) && currentStatus != _lastStatusMessage)
                {
                    _lastStatusMessage = currentStatus;
                    return $"PBE Status: {currentStatus}"; // Return the message
                }
                else if (string.IsNullOrEmpty(currentStatus) && !string.IsNullOrEmpty(_lastStatusMessage))
                {
                    _lastStatusMessage = "";
                    return "PBE Status: Maintenance ended."; // Return the message
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Failed to check PBE status.");
            }
            return null; // No change or error
        }

        private string ExtractStatus(string jsonContent)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonContent)) return string.Empty;

                var data = JObject.Parse(jsonContent);
                var maintenances = data["maintenances"] as JArray;

                if (maintenances == null || maintenances.Count == 0) return string.Empty;

                var firstMaintenance = maintenances[0];
                var updates = firstMaintenance?["updates"] as JArray;

                if (updates == null || updates.Count == 0) return string.Empty;

                var latestUpdate = updates[0];
                var translations = latestUpdate?["translations"] as JArray;

                if (translations == null) return string.Empty;

                var enTranslation = translations.FirstOrDefault(t => t["locale"]?.ToString() == "en_US");
                string originalContent = enTranslation?["content"]?.ToString();

                if (string.IsNullOrEmpty(originalContent)) return string.Empty;

                // Regex to find a date, time, and timezone abbreviation.
                var match = Regex.Match(originalContent, @"(\d{2}/\d{2}/\d{4})\s*(\d{1,2}:\d{2})\s*([A-Z]{3})", RegexOptions.IgnoreCase);

                if (!match.Success) return originalContent; // Return original message if no time is found

                string dateStr = match.Groups[1].Value;
                string timeStr = match.Groups[2].Value;
                string tzAbbr = match.Groups[3].Value;

                if (!DateTime.TryParseExact($"{dateStr} {timeStr}", "MM/dd/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var maintenanceDateTime))
                {
                    return originalContent; // Return original if parsing fails
                }

                if (!TimeZoneAbbreviations.TryGetValue(tzAbbr, out var offset))
                {
                    return originalContent; // Return original if timezone is unknown
                }

                var maintenanceTime = new DateTimeOffset(maintenanceDateTime, offset);

                // If maintenance is in the past, ignore it.
                if (maintenanceTime.ToUniversalTime() < DateTime.UtcNow)
                {
                    return string.Empty;
                }

                // Convert to user's local time for display.
                var localMaintenanceTime = maintenanceTime.ToLocalTime();

                return $"Maintenance at {match.Value} (your time: {localMaintenanceTime:HH:mm}). {originalContent}";
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Failed to parse PBE status JSON.");
                return "Failed to parse PBE status information.";
            }
        }
    }
}
