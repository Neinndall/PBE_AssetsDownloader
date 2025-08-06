// PBE_AssetsDownloader/Services/Requests.cs
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using PBE_AssetsDownloader.Utils;

namespace PBE_AssetsDownloader.Services
{
  public class Requests
  {
    private readonly HttpClient _httpClient;
    private readonly DirectoriesCreator _directoriesCreator;
    private readonly LogService _logService;
    private const string BaseUrl = "https://raw.communitydragon.org/data/hashes/lol/";

    public Requests(HttpClient httpClient, DirectoriesCreator directoriesCreator, LogService logService)
    {
      _httpClient = httpClient;
      _directoriesCreator = directoriesCreator;
      _logService = logService;
    }

    public async Task DownloadHashesAsync(string fileName, string downloadDirectory)
    {
      var url = $"{BaseUrl}/{fileName}";
      try
      {
        var filePath = Path.Combine(downloadDirectory, fileName);
        var response = await _httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
          await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
          await response.Content.CopyToAsync(fileStream);
          _logService.Log($"Download: {fileName}");
        }
        else
        {
          _logService.LogError($"Error downloading {fileName}. Status code: {response.StatusCode}");
        }
      }
      catch (Exception ex)
      {
        _logService.LogError($"Exception downloading {fileName}: {ex.Message}");
      }
    }

    public async Task DownloadHashesFilesAsync(string downloadDirectory)
    {
      await DownloadHashesAsync("hashes.game.txt", downloadDirectory);
      await DownloadHashesAsync("hashes.lcu.txt", downloadDirectory);
    }

    public async Task SyncHashesIfEnabledAsync(bool syncHashesWithCDTB)
    {
      if (syncHashesWithCDTB)
      {
        // Llamamos al directorio de Hashes New
        var downloadDirectory = _directoriesCreator.HashesNewPath;
        await DownloadHashesFilesAsync(downloadDirectory);
      }
    }

    public async Task<bool> DownloadJsonContentAsync(string url, string filePath)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                await File.WriteAllTextAsync(filePath, await response.Content.ReadAsStringAsync());
                _logService.LogDebug($"Downloaded and saved JSON from {url} to {filePath}");
                return true;
            }
            else
            {
                _logService.LogError($"Failed to download JSON from {url}. Status: {response.StatusCode} - {response.ReasonPhrase}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logService.LogError(ex, $"Error downloading JSON from {url}");
            return false;
        }
    }
  }
}
