using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PBE_AssetsDownloader.Utils;

namespace PBE_AssetsDownloader.Services
{
    public class AssetDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly DirectoriesCreator _directoriesCreator; // Crea una instancia de DirectoriesCreator

        // Lista de extensiones excluidas
        private readonly List<string> _excludedExtensions = new()
        {
            ".luabin", ".luabin64", ".preload", ".scb",
            ".sco", ".skl", ".mapgeo", ".subchunktoc", ".stringtable",
            ".anm", ".dat", ".bnk", ".wpk"
        };

        public AssetDownloader(HttpClient httpClient, DirectoriesCreator directoriesCreator)
        {
            _httpClient = httpClient;
            _directoriesCreator = directoriesCreator; // Inicializar el creador de directorios
        }

        private string AdjustUrlBasedOnRules(string url)
        {
            // Cambiar extensión o ignorar según las reglas específicas
            if (url.EndsWith(".dds") &&
                (url.Contains("/loot/companions/") || 
                 url.Contains("2x_") || 
                 url.Contains("4x_") || 
                 url.Contains("tx_cm") || 
                 url.Contains("/particles/") || 
                 url.Contains("/clash/") ||
                 url.Contains("/skins/") || 
                 url.Contains("uiautoatlas") || 
                 url.Contains("/summonerbanners/") || 
                 url.Contains("/hud/") || 
                 url.Contains("/regalia/")))
            {
                // Si contiene "/hud/" y termina en ".png", ignorar la URL
                if (url.Contains("/hud/") && url.EndsWith(".png"))
                {
                    return null; // Ignorar
                }

                url = Path.ChangeExtension(url, ".png"); // Cambiar la extensión a ".png"
            }
            // Si contiene "/loot/companions/" y termina en ".png", ignorar la URL
            if (url.Contains("/loot/companions/") && url.EndsWith(".png"))
            {
                return null; // Ignorar
            }
            // Si contiene "_le." y termina en ".dds", ignorar la URL
            if (url.Contains("_le.") && url.EndsWith(".dds"))
            {
                return null; // Ignorar
            }
            // Si contiene "/summonericons/" y termina en ".dds"
            if (url.Contains("/summonericons/") && url.EndsWith(".dds"))
            {   
                // Si también contiene ".accessories_", procesarlo cambiando la extensión a .png
                if (url.Contains(".accessories_"))
                {
                    url = Path.ChangeExtension(url, ".png"); // Cambiar la extensión a ".png"
                }
                else
                {
                    return null; // Ignorar el resto
                }
            }
            // Si la url termina en .tex cambiar la extensión a png y descargar
            if (url.EndsWith(".tex"))
            {
                url = Path.ChangeExtension(url, ".png"); // Cambiar la extensión a ".png"
            }

            return url;
        }
        
        public async Task<List<string>> DownloadAssets(IEnumerable<string> differences, string baseUrl, string downloadDirectory, Action<string> logAction, List<string> notFoundAssets)
        {
            // Lista para URLs no encontradas que se retorna al final
            var NotFoundAssets = new List<string>();

            // Asegurarse de que el directorio de descarga exista
            if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
            }

            foreach (var line in differences)
            {
                var url = baseUrl + line.Split(' ').Skip(1).First();
                var originalUrl = url;

                var extension = Path.GetExtension(url);
                
                // Excluir extensiones para los assets (por si acaso no fue filtrado antes)
                if (_excludedExtensions.Contains(extension))
                {
                    continue;
                }

                // Funcion para ajustar los url con la extension adecuada segun criterios
                url = AdjustUrlBasedOnRules(url);

                if (string.IsNullOrEmpty(url)) {
                    continue;
                }

                var result = await DownloadFileAsync(url, downloadDirectory, logAction, NotFoundAssets, originalUrl);
                if (!result) {
                    NotFoundAssets.Add(originalUrl);
                }
            }

            return NotFoundAssets;
        }

        private async Task<bool> DownloadFileAsync(string url, string downloadDirectory, Action<string> logAction, List<string> notFoundAssets, string originalUrl)
        {
            try
            {
                var fileName = Path.GetFileName(url);
                var finalExtension = Path.GetExtension(fileName);

                string extensionFolder = _directoriesCreator.CreateAssetTypeDirectory(finalExtension, fileName, url);
                var filePath = Path.Combine(extensionFolder, fileName);

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                    logAction($"Download: {fileName}");
                    return true;
                }
                else
                {
                    notFoundAssets.Add(originalUrl);
                    return false;
                }
            }
            catch (Exception)
            {
                notFoundAssets.Add(originalUrl);
                return false;
            }
        }
    }
}