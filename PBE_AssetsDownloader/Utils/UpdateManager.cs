using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace PBE_AssetsDownloader.Utils
{
    public static class UpdateManager
    {
        public static async Task CheckForUpdatesAsync(bool showNoUpdatesMessage = true)
        {
            // Versión actual de la aplicación (la que está corriendo)
            string currentVersionRaw = Application.ProductVersion;
            string apiUrl = "https://api.github.com/repos/Neinndall/PBE_AssetsDownloader/releases/latest";
            string downloadUrl = "";   // URL del archivo ZIP de la nueva versión
            long totalBytes = 0;       // Tamaño del archivo a descargar

            // Carpeta de descargas del usuario
            string userDownloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            using (HttpClient client = new HttpClient())
            {
                // GitHub requiere que se especifique un User-Agent
                client.DefaultRequestHeaders.Add("User-Agent", "PBE_AssetsDownloader");

                try
                {
                    // Obtenemos la información de la última release publicada
                    var response = await client.GetStringAsync(apiUrl);
                    var releaseData = JsonConvert.DeserializeObject<dynamic>(response);

                    // Extraemos versión y URL de descarga del ZIP
                    string latestVersionRaw = releaseData.tag_name;
                    downloadUrl = releaseData.assets[0].browser_download_url;
                    totalBytes = releaseData.assets[0].size;

                    // Limpiamos ambas versiones para comparar solo números
                    string parsedCurrentVersion = Regex.Match(currentVersionRaw, @"\d+(\.\d+){1,3}").Value;
                    string parsedLatestVersion = Regex.Match(latestVersionRaw.ToString(), @"\d+(\.\d+){1,3}").Value;

                    // Verificamos si el parseo fue exitoso
                    if (string.IsNullOrEmpty(parsedCurrentVersion) || string.IsNullOrEmpty(parsedLatestVersion))
                    {
                        MessageBox.Show("Could not parse version numbers.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Convertimos las cadenas de versión en objetos Version para comparar
                    Version currentVer = new Version(parsedCurrentVersion);
                    Version latestVer = new Version(parsedLatestVersion);

                    // Si hay una versión más nueva disponible
                    if (latestVer.CompareTo(currentVer) > 0)
                    {
                        DialogResult result = MessageBox.Show($"New version available ({latestVersionRaw}). Do you want to download it?",
                                                               "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                        if (result == DialogResult.Yes)
                        {
                            // Mostramos el formulario de progreso
                            ProgressForm progressForm = new ProgressForm();
                            progressForm.Show();

                            // Ruta de descarga local
                            string fileName = $"PBE_AssetsDownloader_{latestVersionRaw}.zip";
                            string downloadPath = Path.Combine(userDownloadsFolder, fileName);

                            // Mostramos el tamaño total antes de iniciar la descarga
                            string downloadSize = $"{(totalBytes / 1024.0 / 1024.0):0.00} MB";
                            progressForm.SetProgress(0, $"Downloading {downloadSize}...");
                            await Task.Delay(500); // Breve espera para que la UI actualice

                            // Iniciamos la descarga del archivo ZIP
                            using (var responseDownload = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                            {
                                responseDownload.EnsureSuccessStatusCode();
                                long bytesDownloaded = 0;

                                using (var fs = new FileStream(downloadPath, FileMode.Create))
                                {
                                    byte[] buffer = new byte[8192];
                                    int bytesRead;

                                    using (var stream = await responseDownload.Content.ReadAsStreamAsync())
                                    {
                                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                        {
                                            await fs.WriteAsync(buffer, 0, bytesRead);
                                            bytesDownloaded += bytesRead;

                                            // Actualizamos el progreso durante la descarga
                                            if (totalBytes > 0)
                                            {
                                                int progressPercentage = (int)((double)bytesDownloaded / totalBytes * 100);
                                                progressForm.SetProgress(progressPercentage, 
                                                    $"Downloading... {(bytesDownloaded / 1024.0 / 1024.0):0.00} MB / {downloadSize}");
                                            }
                                        }
                                    }
                                }
                                await Task.Delay(1200); // Breve espera para que la UI actualice
                                
                                // Cerramos el formulario de progreso de forma segura
                                if (progressForm.IsHandleCreated)
                                    progressForm.Invoke(() => { progressForm.Close(); progressForm.Dispose(); });

                                MessageBox.Show("Update downloaded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                    else if (showNoUpdatesMessage)
                    {
                        // Si no hay nueva versión y se pidió mostrar el mensaje
                        MessageBox.Show("No updates available.", "Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    // Captura y muestra cualquier error que ocurra en el proceso
                    MessageBox.Show("Error checking for updates:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}