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
        private readonly List<string> _excludedExtensions = new() // Lista de extensiones excluidas
        {
            ".luabin", ".luabin64", ".preload", ".scb",
            ".sco", ".skl", ".mapgeo", ".subchunktoc", ".stringtable",
            ".anm", ".dat", ".bnk", ".wpk", 
            ".cfg", ".cfgbin"
        };

        public List<string> ExcludedExtensions => _excludedExtensions;
        
        public AssetDownloader(HttpClient httpClient, DirectoriesCreator directoriesCreator)
        {
            _httpClient = httpClient;
            _directoriesCreator = directoriesCreator; // Inicializar el creador de directorios
        }

        public async Task<List<string>> DownloadAssets(IEnumerable<string> differences, string baseUrl, string downloadDirectory, Action<string> logAction, List<string> notFoundAssets)
        {
            var NotFoundAssets = new List<string>();

            if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
            }

            foreach (var line in differences)
            {
                string[] parts = line.Split(' ');
                string relativePath = parts.Length >= 2 ? parts[1] : parts[0]; // Evitar errores con hash + path

                string url = baseUrl + relativePath;
                string originalUrl = url;

                url = AssetUrlRules.Adjust(url);

                // Si la URL es null (es decir, ha sido ignorada), no continuar con la descarga
                if (string.IsNullOrEmpty(url))
                {
                    continue; // No añadir a NotFounds.txt si fue ignorada
                }

                var result = await DownloadFileAsync(url, downloadDirectory, logAction, originalUrl);
                if (!result)
                {
                    NotFoundAssets.Add(originalUrl); // Añadir solo si no se descargó correctamente
                }
            }
            
            return NotFoundAssets;
        }

        private async Task<bool> DownloadFileAsync(string url, string downloadDirectory, Action<string> logAction, string originalUrl)
        {
            try
            {
                // Obtener el nombre del archivo y la extensión
                var fileName = Path.GetFileName(url);
                var finalExtension = Path.GetExtension(fileName);

                // Crear el directorio de destino basado en la URL
                string extensionFolder = _directoriesCreator.CreateAssetDirectoryFromPath(url, downloadDirectory); // Organizar por URL
                var filePath = Path.Combine(extensionFolder, fileName);

                // Comprobar si el archivo ya existe
                int counter = 1;
                while (File.Exists(filePath))
                {
                    // Si el archivo ya existe, añadir un sufijo para evitar sobrescribir
                    string newFileName = Path.GetFileNameWithoutExtension(fileName) + $"_{counter}{finalExtension}";
                    filePath = Path.Combine(extensionFolder, newFileName);
                    counter++;
                }

                // Realizar la solicitud HTTP para obtener el archivo
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    // Descargar el archivo y guardarlo en el directorio correspondiente
                    await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }

                    // Si necesitas hacer algo con el log, hazlo aquí directamente
                    logAction($"Downloaded: {fileName}");
                    return true;
                }
                else
                {
                    return false; // No volver a agregar aquí
                }
            }
            catch (Exception)
            {
                return false; // Tampoco volver a agregar aquí
            }
        }
    }
}
