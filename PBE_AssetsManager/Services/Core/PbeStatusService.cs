using System;
using System.Linq;
using System.Net.Http;
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
                
                return enTranslation?["content"]?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Failed to parse PBE status JSON.");
                return "Failed to parse PBE status information.";
            }
        }
    }
}
