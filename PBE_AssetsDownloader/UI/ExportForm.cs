using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Info;

namespace PBE_AssetsDownloader.UI
{
    public partial class ExportForm : Form
    {
        private readonly HttpClient _httpClient;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly AssetDownloader _assetDownloader;

        public ExportForm()
        {
            InitializeComponent();
            BackgroundHelper.SetBackgroundImage(this);
            ApplicationInfos.SetIcon(this);

            _httpClient = new HttpClient();
            _directoriesCreator = new DirectoriesCreator();
            _assetDownloader = new AssetDownloader(_httpClient, _directoriesCreator); 
        }

        private void btnPreviewAssets_Click(object sender, EventArgs e)
        {
            string inputFolder = txtDifferencesPath.Text;

            if (string.IsNullOrWhiteSpace(inputFolder) || !Directory.Exists(inputFolder))
            {
                MessageBox.Show("Select a valid folder that contains the differences_game and differences_lcu files.", "Invalid path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedAssetTypes = clbAssets.CheckedItems.Cast<string>().ToList();

            if (!selectedAssetTypes.Any())
            {
                MessageBox.Show("Select at least one type for preview.", "Type not selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Abrir el formulario PreviewAssetsForm y pasar los datos, incluyendo la función FilterAssetsByType
            var previewForm = new PreviewAssetsForm(inputFolder, selectedAssetTypes, FilterAssetsByType);
            previewForm.ShowDialog();
        }

        private async void BtnDownloadSelectedAssets_Click(object sender, EventArgs e)
        {
            string inputFolder = txtDifferencesPath.Text;
            string downloadFolder = txtDownloadTargetPath.Text;

            if (string.IsNullOrWhiteSpace(inputFolder) || !Directory.Exists(inputFolder))
            {
                MessageBox.Show("Select a valid folder that contains the differences_game and differences_lcu files.", "Invalid path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(downloadFolder) || !Directory.Exists(downloadFolder))
            {
                MessageBox.Show("Select a valid folder to save the exported assets.", "Invalid Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedAssetTypes = clbAssets.CheckedItems.Cast<string>().ToList();

            if (!selectedAssetTypes.Any())
            {
                MessageBox.Show("Select at least one type for preview.", "Type not selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var differencesGamePath = Path.Combine(inputFolder, "differences_game.txt");
            var differencesLcuPath = Path.Combine(inputFolder, "differences_lcu.txt");

            var gameLines = File.Exists(differencesGamePath) ? await File.ReadAllLinesAsync(differencesGamePath) : Array.Empty<string>();
            var lcuLines = File.Exists(differencesLcuPath) ? await File.ReadAllLinesAsync(differencesLcuPath) : Array.Empty<string>();

            if (!gameLines.Any() && !lcuLines.Any())
            {
                MessageBox.Show("No assets were found with the provided differences.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AppendLog("Starting download of assets...");

            var notFoundAssets = new List<string>();
            var gameAssets = FilterAssetsByType(gameLines, selectedAssetTypes);
            var lcuAssets = FilterAssetsByType(lcuLines, selectedAssetTypes);

            AppendLog($"Total GAME assets to download: {gameAssets.Count}");
            var notFoundGame = await _assetDownloader.DownloadAssets(
                gameAssets,
                "https://raw.communitydragon.org/pbe/game/",
                downloadFolder,
                AppendLog,
                notFoundAssets
            );
            notFoundAssets.AddRange(notFoundGame);

            AppendLog($"Total LCU assets to download: {lcuAssets.Count}");
            var notFoundLcu = await _assetDownloader.DownloadAssets(
                lcuAssets,
                "https://raw.communitydragon.org/pbe/",
                downloadFolder,
                AppendLog,
                notFoundAssets
            );
            notFoundAssets.AddRange(notFoundLcu);

            if (notFoundAssets.Any())
            {
                // Ruta para guardar el archivo .txt (en el directorio de descarga)
                string notFoundFilePath = Path.Combine(downloadFolder, "NotFoundAssets.txt");

                // Guardar los activos no encontrados en un archivo .txt
                File.WriteAllLines(notFoundFilePath, notFoundAssets);

                // Opcional: Mostrar un mensaje indicando que el archivo fue guardado
                MessageBox.Show($"Some assets could not be downloaded. A list of missing assets has been saved to: {notFoundFilePath}", 
                                "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("Download completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private List<string> FilterAssetsByType(IEnumerable<string> lines, List<string> selectedTypes)
        {
            return lines
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split(' ').Skip(1).First()) // Elimina el hash y deja solo el path
                .Where(path =>
                {
                    if (selectedTypes.Any(type => type.Equals("All", StringComparison.OrdinalIgnoreCase)))
                        return true;

                    foreach (var type in selectedTypes)
                    {
                        if (type.Equals("Images", StringComparison.OrdinalIgnoreCase) &&
                            (path.EndsWith(".tex", StringComparison.OrdinalIgnoreCase) ||
                             path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                             path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                             path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))) return true;

                        if (type.Equals("Audios", StringComparison.OrdinalIgnoreCase) && path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)) return true;
                        if (type.Equals("Plugins", StringComparison.OrdinalIgnoreCase) && path.StartsWith("plugins/", StringComparison.OrdinalIgnoreCase)) return true;
                        if (type.Equals("Game", StringComparison.OrdinalIgnoreCase) && path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase)) return true;
                    }
                    return false;
                })
                .Distinct()
                .ToList();
        }

        private void BtnBrowseDownloadTargetPath_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtDownloadTargetPath.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void BtnBrowseDifferencesPath_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtDifferencesPath.Text = folderBrowserDialog.SelectedPath;
            }
        }

        public void AppendLog(string message)
        {
            // Comprobación de si el formulario está desechado o cerrado
            if (this.IsDisposed || !this.IsHandleCreated)
                return;

            string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Action appendMessage = () =>
            {
                richTextBoxLogs.SelectionStart = richTextBoxLogs.TextLength;
                richTextBoxLogs.SelectionIndent = 4;
                richTextBoxLogs.AppendText(timestampedMessage + Environment.NewLine);
                richTextBoxLogs.SelectionStart = richTextBoxLogs.TextLength;
                richTextBoxLogs.ScrollToCaret();
            };

            if (richTextBoxLogs.InvokeRequired)
                richTextBoxLogs.Invoke(appendMessage);
            else
                appendMessage();
        }
    }
}