using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace PBE_AssetsDownloader.Services
{
    public class Status
    {
        private readonly string statusUrl = "https://raw.communitydragon.org/data/hashes/lol/"; // URL base
        private const string GAME_HASHES_FILENAME = "hashes.game.txt";
        private const string LCU_HASHES_FILENAME = "hashes.lcu.txt";
    
        private readonly string configFilePath = "config.json"; // Archivo de configuración
        public string CurrentStatus { get; private set; } // Para almacenar el estado actual
        private Action<string> _logAction; // Acción para el log

        public Status(Action<string> logAction) // Constructor modificado
        {
            _logAction = logAction;
        }

        // Método para verificar si el servidor ha sido actualizado
        public async Task<bool>IsUpdatedAsync ()
        {
            try
            {
                Log("Getting update date from server...");

                // Obtener fechas de actualización para ambos archivos
                DateTime gameHashesDate = await GetServerUpdateDate(GAME_HASHES_FILENAME);
                DateTime lcuHashesDate = await GetServerUpdateDate(LCU_HASHES_FILENAME);

                string lastUpdateDateString = ReadLastUpdateDate();

                // Convertir la fecha guardada a DateTime
                DateTime lastUpdateDate = string.IsNullOrEmpty(lastUpdateDateString)
                    ? DateTime.MinValue
                    : DateTime.ParseExact(lastUpdateDateString, "dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture);

                // Comparar fechas
                if (gameHashesDate > lastUpdateDate || lcuHashesDate > lastUpdateDate)
                {
                    SaveLastUpdateDate(gameHashesDate); // Pasar el DateTime correcto
                    // Actualiza lastUpdateHashes en la configuración
                    //  SaveSettings(_syncHashesWithCDTB, gameHashesDate.ToString("dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture)); 
                    return true; // Se ha actualizado
                }
            }
            catch (Exception ex)
            {
                CurrentStatus = $"Error checking for updates: {ex.Message}";
                Log(CurrentStatus); // Log de error
            }

            return false; // No se ha actualizado
        }

        // Método para obtener la última fecha de actualización del archivo JSON
        public string ReadLastUpdateDate()
        {
            if (File.Exists(configFilePath))
            {
                // Leer el archivo JSON
                string json = File.ReadAllText(configFilePath);
                var settings = JObject.Parse(json);
                
                // Leer y devolver la fecha en el formato correcto
                return settings["lastUpdateHashes"]?.ToString() ?? string.Empty; // Devolver la fecha o cadena vacía si no existe
            }
            else
            {
                // Crear el archivo y establecer un valor predeterminado
                DateTime now = DateTime.UtcNow;
                SaveLastUpdateDate(now); // Guardar la fecha actual
                return string.Empty; // Si no existe el archivo, devolvemos cadena vacía
            }
        }

        // Método para analizar el tiempo de actualización del servidor
        private static DateTime ParseServerUpdateTime(string html, string fileName)
        {
            // Actualizamos la expresión regular para capturar la fecha y hora correctamente
            var match = Regex.Match(html, $@"<a href=""{fileName}"">.*?(\d{{1,2}}-\w{{3}}-\d{{4}})\s+(\d{{1,2}}:\d{{2}})\s+");
            if (!match.Success) throw new Exception($"Failed to find entry for file {fileName}");

            // Parseamos la fecha y hora
            var date = DateTime.ParseExact(match.Groups[1].Value, "dd-MMM-yyyy", CultureInfo.InvariantCulture);
            var time = TimeOnly.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            return date.Date.Add(time.ToTimeSpan());
        }

        // Método para obtener la última fecha de actualización del servidor
        private async Task<DateTime> GetServerUpdateDate(string fileName)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Realiza la solicitud HTTP para obtener el HTML del servidor
                    string html = await client.GetStringAsync(statusUrl);
                    // Llama a ParseServerUpdateTime para obtener la fecha del archivo específico
                    return ParseServerUpdateTime(html, fileName);
                }
                catch (HttpRequestException ex)
                {
                    CurrentStatus = $"Error accessing server: {ex.Message}";
                    Log(CurrentStatus); // Log de error
                    throw; // Lanza de nuevo la excepción para que el llamador pueda manejarla
                }
            }
        }

        // Método para comprobar si hay updates en el servidor sino devolver mensaje
        public void CheckForUpdates(bool isUpdated)
        {
            if (!isUpdated)
            {
                SetSyncStatus("No server updates found.");
            }
        }

        // Método para manejar errores
        public void HandleError(Exception ex)
        {
            string message = $"Error during download: {ex.Message}";
            SetSyncStatus(message); // Establecer el estado con el mensaje de error
        }

        // Método para guardar la fecha de actualización en el archivo JSON
        private void SaveLastUpdateDate(DateTime serverDateTime)
        {
            JObject settings;

            // Si el archivo existe, lo leemos y actualizamos la fecha
            if (File.Exists(configFilePath))
            {
                string json = File.ReadAllText(configFilePath);
                settings = JObject.Parse(json);
            }
            else
            {
                settings = new JObject(); // Creamos un nuevo objeto JSON si no existe el archivo
            }

            // Guardamos la fecha en el formato exacto que aparece en el servidor
            settings["lastUpdateHashes"] = serverDateTime.ToString("dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture);

            // Guardar el archivo actualizado
            File.WriteAllText(configFilePath, settings.ToString());
        }

        // Método para establecer el estado
        public void SetSyncStatus(string statusMessage)
        {
            CurrentStatus = statusMessage;
            Log(statusMessage); // También loguear el mensaje en la UI
        }

        // Método para registrar mensajes en la UI
        private void Log(string message)
        {
            _logAction?.Invoke(message); // Invocar la acción para registrar el mensaje
        }
    }
}
