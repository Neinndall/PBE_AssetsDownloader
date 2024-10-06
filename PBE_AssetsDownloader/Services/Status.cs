using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PBE_NewFileExtractor.Services
{
    public class Status
    {
        private readonly string statusUrl = "https://raw.communitydragon.org/status.pbe.txt";
        private readonly string configFilePath = "config.json"; // Archivo de configuración
        public string CurrentStatus { get; private set; } // Para almacenar el estado actual
        private Action<string> _logAction; // Acción para el log

        public Status(Action<string> logAction) // Constructor modificado
        {
            _logAction = logAction;
        }

        // Método para verificar si el servidor ha sido actualizado
        public async Task<bool> IsUpdatedAsync()
        {
            try
            {
                Log("Getting update date from server...");
                string serverDate = await GetServerUpdateDate();
                string lastUpdateDate = ReadLastUpdateDate();

                // Verificar si la fecha del servidor es diferente a la guardada
                if (serverDate != lastUpdateDate)
                {
                    SaveLastUpdateDate(serverDate);
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

        // Método para obtener la fecha de la última actualización del servidor
        private async Task<string> GetServerUpdateDate()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    return await client.GetStringAsync(statusUrl);
                }
                catch (HttpRequestException ex)
                {
                    CurrentStatus = $"Error accessing server: {ex.Message}";
                    Log(CurrentStatus); // Log de error
                    return string.Empty;
                }
            }
        }

        // Método para leer la última fecha de actualización desde el archivo JSON
        public string ReadLastUpdateDate()
        {
            if (File.Exists(configFilePath))
            {
                // Leer el archivo JSON
                string json = File.ReadAllText(configFilePath);
                var settings = JObject.Parse(json);
                return settings["lastUpdateFile"]?.ToString() ?? string.Empty; // Devolver la fecha, o una cadena vacía si no existe
            }
            else
            {
                // Crear el archivo y establecer un valor predeterminado para lastUpdateFile
                SaveLastUpdateDate(DateTime.UtcNow.ToString("o")); // Usar el formato ISO 8601
                return string.Empty; // Si no existe el archivo, devolvemos una cadena vacía
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
        private void SaveLastUpdateDate(string date)
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

            // Actualizamos la fecha de la última actualización
            settings["lastUpdateFile"] = date;

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
