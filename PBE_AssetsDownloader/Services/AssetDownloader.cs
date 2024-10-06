using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PBE_NewFileExtractor.Services
{
    public class AssetDownloader
    {
        private readonly HttpClient _httpClient;

        public AssetDownloader(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<string>> DownloadAssets(IEnumerable<string> differences, string baseUrl, string downloadDirectory, Action<string> logAction)
        {
            var notFoundUrls = new List<string>();

            foreach (var line in differences)
            {
                var url = baseUrl + line.Split(' ').Skip(1).First();

                // Si la URL termina en ".dds" y contiene patrones específicos, cambiar la extensión a ".png"
                if (url.EndsWith(".dds") && 
                    (url.Contains("/loot/companions/") || 
                    url.Contains("2x_") || 
                    url.Contains("4x_") || 
                    url.Contains("tx_cm") || 
                    url.Contains("/particles/") || 
                    url.Contains("uiautoatlas") ||
                    url.Contains("/hud/")))
                {
                    // Si contiene "/hud/" y termina en ".png", ignorar la URL
                    if (url.Contains("/hud/") && url.EndsWith(".png"))
                    {
                        logAction($"Ignored: {url}");
                        continue;
                    }

                    // Si pasa las condiciones, cambiar la extensión a ".png"
                    url = Path.ChangeExtension(url, ".png");
                }

                // Si la URL contiene "_le." y termina en ".jpg", permitir la descarga
                if (url.Contains("_le.") && url.EndsWith(".jpg"))
                {
                    // Proceder con la descarga normal
                }
                
                // Si la URL contiene "_le." y termina en ".dds", no descargar
                if (url.Contains("_le.") && url.EndsWith(".dds"))
                {
                    logAction($"Ignored: {url}");
                    continue;
                }

                // Si la URL acaba en ".tex" sustituir por ".png"
                if (url.EndsWith(".tex"))
                {
                    // Modificar la extensión si termina en ".tex"
                    url = Path.ChangeExtension(url, ".png");
                }

                // Si la URL contiene "/loot/companions/" y termina en ".png", no se descargará
                if (url.Contains("/loot/companions/") && url.EndsWith(".png"))
                {
                    logAction($"Ignored: {url}");
                    continue;
                }

                
                try
                {
                    var fileName = Path.GetFileName(url);
                    var filePath = Path.Combine(downloadDirectory, fileName);

                    var response = await _httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }

                        logAction($"Download: {fileName}");
                    }
                    else
                    {
                        logAction($"The asset was not found in: {url}");
                        notFoundUrls.Add(url);
                    }
                }
                catch (Exception ex)
                {
                    logAction($"Error trying to download asset from {url}: {ex.Message}");
                    notFoundUrls.Add(url);
                }
            }

            return notFoundUrls;
        }
    }
}
