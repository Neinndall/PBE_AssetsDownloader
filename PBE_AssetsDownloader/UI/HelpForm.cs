using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;

using System.Linq;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Info;

namespace PBE_AssetsDownloader.UI
{
    public partial class HelpForm : Form
    {
        private const string EmbeddedChangelogResource = "PBE_AssetsDownloader.changelogs.txt";

        public HelpForm()
        {
            InitializeComponent();
            BackgroundHelper.SetBackgroundImage(this);
            ApplicationInfos.SetIcon(this);
            this.Load += HelpForm_Load;
        }

        private void HelpForm_Load(object sender, EventArgs e)
        {
            tabControl.SelectedIndexChanged += tabControl_SelectedIndexChanged;
            tabControl.SelectedIndex = 0;

            // Asegurarse de que el contenido "About" se cargue al abrir el formulario
            FormatRichText(richTextBoxAbout, GetAboutText());
        }


        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedIndex == 0)
                FormatRichText(richTextBoxAbout, GetAboutText());
            else if (tabControl.SelectedIndex == 1)
                FormatRichText(richTextBoxChangelogs, LoadEmbeddedChangelog());
        }

        private string LoadEmbeddedChangelog()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using Stream stream = assembly.GetManifestResourceStream(EmbeddedChangelogResource);
                if (stream == null) return "Changelog resource not found.";
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                return $"Error reading changelog resource: {ex.Message}";
            }
        }

        private void FormatRichText(RichTextBox box, string content)
        {
            box.Clear();
            box.SelectionIndent = 4;
            box.AppendText(Environment.NewLine);

            foreach (var rawLine in content.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                var line = rawLine.TrimStart();
                bool isTitle = line.EndsWith(":");
                bool isHeader = line.StartsWith("PBE_AssetsDownloader");

                if (line.Contains("[NEW]") || line.Contains("[IMPROVEMENTS]") || line.Contains("[BUG FIXES]"))
                    ApplyStyle(box, FontStyle.Bold, Color.Black, bullet: false);
                else if (isHeader)
                    ApplyStyle(box, FontStyle.Bold, Color.DarkBlue, bullet: false);
                else if (isTitle)
                    ApplyStyle(box, FontStyle.Bold, Color.Black, bullet: false);
                else if (line.StartsWith("•"))
                    ApplyStyle(box, FontStyle.Regular, Color.Black, bullet: true);
                else if (char.IsDigit(line.FirstOrDefault()) && line.Contains("."))
                    ApplyStyle(box, FontStyle.Regular, Color.DarkSlateGray, bullet: false);
                else
                    ApplyStyle(box, FontStyle.Regular, Color.Black, bullet: false);

                box.AppendText(line + Environment.NewLine);
                box.SelectionBullet = false;
            }
        }

        private void ApplyStyle(RichTextBox box, FontStyle style, Color color, bool bullet)
        {
            box.SelectionFont = new Font(box.Font, style);
            box.SelectionColor = color;
            box.SelectionBullet = bullet;
        }

        private string GetAboutText()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "Description:",
                "This app is designed to automatically download and manage new assets from League of Legends PBE server updates. " +
                "It helps content creators and developers stay up-to-date with the latest changes and additions to the game.",
                "",
                "Key Features:",
                "1. Automatic detection of new PBE updates.",
                "2. Downloads and organizes new game assets.",
                "3. Supports synchronization with CDTB.",
                "4. Auto-copy functionality for hash files.",
                "5. Back-Ups for hash files.",
                "6. Manually download assets from others days with differences text files.",
                "",
                "How to Use:",
                "1. Configure your desired settings in the Settings menu",
                "2. Select the directories for the old and new hashes files.",
                "3. Enable auto-copy if needed.",
                "4. The tool will automatically check for and download new PBE assets.",
                "",
                "For more information and updates, check the Changelogs section."
            });
        }
        
        private void buttonReportBug_Click(object sender, EventArgs e)
        {
            // Puedes abrir un enlace o mostrar una ventana
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/Neinndall/PBE_AssetsDownloader/issues", // o un formulario
                UseShellExecute = true
            });
        }
        
        private async void buttonCheckUpdates_Click(object sender, EventArgs e)
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
                                {
                                    progressForm.Invoke((Action)(() => progressForm.Close()));
                                }

                                MessageBox.Show("Update downloaded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                    else
                    {
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
