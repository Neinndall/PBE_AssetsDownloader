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
        private const string BaseUrl = "https://raw.communitydragon.org/data/hashes/lol/";

        public Requests(HttpClient httpClient, DirectoriesCreator directoriesCreator)
        {
            _httpClient = httpClient;
            _directoriesCreator = directoriesCreator;
        }

        public async Task DownloadHashesAsync(string fileName, string downloadDirectory, Action<string> logAction)
        {
            var url = $"{BaseUrl}/{fileName}";

            try
            {
                var filePath = Path.Combine(downloadDirectory, fileName);
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // Asegúrate de que el directorio existe antes de guardar el archivo
                    await _directoriesCreator.CreateHashesNewDirectoryAsync(); // Crea el directorio hashes/new si no existe

                    await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }

                    logAction($"Download: {fileName}");
                }
                else
                {
                    logAction($"Error downloading {fileName}");
                }
            }
            catch (Exception ex)
            {
                logAction($"Error trying to download {fileName}: {ex.Message}");
            }
        }

        public async Task DownloadHashesFilesAsync(string downloadDirectory, Action<string> logAction)
        {
            await DownloadHashesAsync("hashes.game.txt", downloadDirectory, logAction);
            await DownloadHashesAsync("hashes.lcu.txt", downloadDirectory, logAction);
        }

        public async Task SyncHashesIfEnabledAsync(bool syncHashesWithCDTB, Action<string> logAction)
        {
            if (syncHashesWithCDTB)
            {
                var downloadDirectory = _directoriesCreator.GetHashesNewsDirectoryPath(); // Obtener el directorio de descarga
                await DownloadHashesFilesAsync(downloadDirectory, logAction);
            }
        }
    }
}