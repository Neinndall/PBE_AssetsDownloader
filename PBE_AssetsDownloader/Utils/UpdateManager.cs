using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace PBE_AssetsDownloader.Utils
{
    public static class UpdateManager
    {
        // Método modificado para aceptar un parámetro que determine si mostrar el mensaje de "No actualizaciones"
        public static async Task CheckForUpdatesAsync(bool showNoUpdatesMessage = true)
        {
            string currentVersion = Application.ProductVersion;
            string apiUrl = "https://api.github.com/repos/Neinndall/PBE_AssetsDownloader/releases/latest";
            string downloadUrl = "";
            long totalBytes = 0;

            // Obtenemos la ruta del directorio "Downloads" del usuario actual
            string userDownloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "PBE_AssetsDownloader");

                try
                {
                    var response = await client.GetStringAsync(apiUrl);
                    var releaseData = JsonConvert.DeserializeObject<dynamic>(response);

                    string latestVersion = releaseData.tag_name;
                    downloadUrl = releaseData.assets[0].browser_download_url;
                    totalBytes = releaseData.assets[0].size;

                    string cleanedVersion = latestVersion.Replace("v", "").Split('-')[0];

                    Version currentVer = new Version(currentVersion);
                    Version latestVer = new Version(cleanedVersion);

                    if (latestVer.CompareTo(currentVer) > 0)
                    {
                        DialogResult result = MessageBox.Show($"New version available ({latestVersion}). Do you want to download it?",
                                                               "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (result == DialogResult.Yes)
                        {
                            ProgressForm progressForm = new ProgressForm();
                            progressForm.Show();

                            string fileName = $"PBE_AssetsDownloader_{latestVersion}.zip";
                            string downloadPath = Path.Combine(userDownloadsFolder, fileName);

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

                                            if (totalBytes > 0)
                                            {
                                                int progressPercentage = (int)((double)bytesDownloaded / totalBytes * 100);
                                                progressForm.SetProgress(progressPercentage, 
                                                    $"{bytesDownloaded / 1024 / 1024} MB / {totalBytes / 1024 / 1024} MB");
                                            }
                                        }
                                    }
                                }

                                // Mostramos el progreso final al 100%
                                progressForm.SetProgress(100, 
                                    $"{(totalBytes / 1024.0 / 1024.0):0.00} MB / {(totalBytes / 1024.0 / 1024.0):0.00} MB");

                                // Aseguramos que la barra de progreso llegue al 100% al final de la descarga
                                progressForm.SetProgress(100, "Download completed. Processing...");

                                // Esperamos brevemente para garantizar que la UI procese las actualizaciones
                                await Task.Delay(1000);

                                // Cerramos el formulario de manera segura
                                if (progressForm.IsHandleCreated)
                                    progressForm.Invoke(() => { progressForm.Close(); progressForm.Dispose(); });

                                MessageBox.Show("Update downloaded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                    else if (showNoUpdatesMessage)
                    {
                        // Solo mostramos el mensaje de no actualizaciones si showNoUpdatesMessage es true
                        MessageBox.Show("No updates available.", "Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error checking for updates:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
