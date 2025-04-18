using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Net.Http;
using PBE_AssetsDownloader.Info;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader.UI
{
    public partial class MainForm : Form
    {
        private bool _syncHashesWithCDTB; // Indica si se debe sincronizar con CDTB
        private bool _autoCopyHashes; // Indica si se debe copiar automáticamente los hashes
        private bool _createBackUpOldHashes; // Indica si se debe crear una copia de seguridad de los hashes antiguos
        private Status _status; // Instancia para manejar el estado del servidor

        public MainForm()
        {
            InitializeComponent();
            ApplicationInfos.SetInfo(this);

            _status = new Status(AppendLog);

            var settings = LoadSettings();
            _syncHashesWithCDTB = settings.syncHashesWithCDTB;
            _autoCopyHashes = settings.AutoCopyHashes;
            _createBackUpOldHashes = settings.CreateBackUpOldHashes;

            if (_syncHashesWithCDTB)
            {
                AppendLog("Sync enabled on startup.");
                _ = SyncHashesOnly();
            }
            if (_autoCopyHashes)
            {
                AppendLog("Automatically replace old hashes enabled.");
            }
            if (_createBackUpOldHashes)
            {
                AppendLog("Backup old hashes enabled.");
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            newHashesTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            oldHashesTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            richTextBoxLogs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            startButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnSelectNewHashesDirectory.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnSelectOldHashesDirectory.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            richTextBoxLogs.Dock = DockStyle.Fill;
            richTextBoxLogs.Text = Environment.NewLine; // Añadir una línea en blanco al inicio
        }

        private void btnSelectNewHashesDirectory_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                newHashesTextBox.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void btnSelectOldHashesDirectory_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                oldHashesTextBox.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private async void startButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(oldHashesTextBox.Text) || string.IsNullOrEmpty(newHashesTextBox.Text))
            {
                MessageBox.Show("Please select both hash directories.", "Warning");
                return;
            }

            // Llamada sin esperar un retorno (ahora el método solo se ejecuta y se loguea la salida)
            await Program.RunExtraction(newHashesTextBox.Text, oldHashesTextBox.Text, _syncHashesWithCDTB, _autoCopyHashes, _createBackUpOldHashes, logMessage =>
            {
                AppendLog(logMessage); // Utilizar AppendLog para agregar el mensaje con margen
            });
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            using (var helpForm = new HelpForm())
            {
                helpForm.ShowDialog();
            }
        }

        // Abre la ventana de configuración
        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm(_syncHashesWithCDTB, _autoCopyHashes, _createBackUpOldHashes, _status))
            {
                // Suscribirse al evento SettingsChanged
                settingsForm.SettingsChanged += OnSettingsChanged;
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    bool newsyncHashesWithCDTB = settingsForm.syncHashesWithCDTB;
                    bool newAutoCopyHashes = settingsForm.AutoCopyHashes;
                    bool newCreateBackUpOldHashes = settingsForm.CreateBackUpOldHashes;

                    if (newsyncHashesWithCDTB != _syncHashesWithCDTB || newAutoCopyHashes != _autoCopyHashes || newCreateBackUpOldHashes != _createBackUpOldHashes)
                    {
                        _syncHashesWithCDTB = newsyncHashesWithCDTB;
                        _autoCopyHashes = newAutoCopyHashes;
                        _createBackUpOldHashes = newCreateBackUpOldHashes;
                        SaveSettings(_syncHashesWithCDTB, _autoCopyHashes, _createBackUpOldHashes);
                        AppendLog("Settings updated.");
                    }
                }
                settingsForm.SettingsChanged -= OnSettingsChanged;
            }
        }

        // Maneja los cambios en la configuración
        private void OnSettingsChanged(object sender, EventArgs e)
        {
            var settings = LoadSettings();
            _syncHashesWithCDTB = settings.syncHashesWithCDTB;
            _autoCopyHashes = settings.AutoCopyHashes;
            _createBackUpOldHashes = settings.CreateBackUpOldHashes;
        }

        private async Task SyncHashesOnly()
        {
            var directoriesCreator = new DirectoriesCreator();
            var httpClient = new HttpClient();
            var requests = new Requests(httpClient, directoriesCreator);

            bool isUpdated = await _status.IsUpdatedAsync();
            if (isUpdated)
            {
                AppendLog("Server updated. Starting hash synchronization...");
                await requests.SyncHashesIfEnabledAsync(_syncHashesWithCDTB, AppendLog);
            }
            else
            {
                _status.CheckForUpdates(isUpdated);
            }
        }

        private void SaveSettings(bool syncHashesWithCDTB, bool autoCopyHashes, bool createBackUpOldHashes, string lastUpdateHashes = null)
        {
            var settings = LoadSettings();
            settings.syncHashesWithCDTB = syncHashesWithCDTB;
            settings.AutoCopyHashes = autoCopyHashes;
            settings.CreateBackUpOldHashes = createBackUpOldHashes;

            if (!string.IsNullOrEmpty(lastUpdateHashes))
            {
                settings.lastUpdateHashes = lastUpdateHashes;
            }

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText("config.json", json);
        }

        private void AppendLog(string message)
        {
            if (richTextBoxLogs.InvokeRequired) 
            {
                richTextBoxLogs.Invoke(new Action(() => 
                {
                    string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
                    richTextBoxLogs.AppendText(timestampedMessage + Environment.NewLine);
                    richTextBoxLogs.SelectionStart = richTextBoxLogs.TextLength;
                    richTextBoxLogs.ScrollToCaret();
                }));
            }
            else
            {
                string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
                richTextBoxLogs.AppendText(timestampedMessage + Environment.NewLine);
                richTextBoxLogs.SelectionStart = richTextBoxLogs.TextLength;
                richTextBoxLogs.ScrollToCaret();
            }
        }

        private AppSettings LoadSettings()
        {
            const string configFilePath = "config.json";

            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }

            return new AppSettings { syncHashesWithCDTB = false, AutoCopyHashes = false, CreateBackUpOldHashes = false };
        }

        public class AppSettings
        {
            public bool syncHashesWithCDTB { get; set; }
            public string lastUpdateHashes { get; set; }
            public bool AutoCopyHashes { get; set; }
            public bool CreateBackUpOldHashes { get; set; }
        }
    }
}
