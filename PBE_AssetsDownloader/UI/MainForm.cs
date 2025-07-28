using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Net.Http;
using System.Reflection;
using System.Drawing;

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
        private bool _onlyCheckDifferences; // Indica si unicamente chckeará las diferencias
        private Status _status; // Instancia para manejar el estado del servidor

        public MainForm()
        {
            InitializeComponent();

            BackgroundHelper.SetBackgroundImage(this);
            DoubleBuffered = true;
            ApplicationInfos.SetInfo(this);
            _status = new Status(AppendLog);

            var settings = AppSettings.LoadSettings();
            _syncHashesWithCDTB = settings.SyncHashesWithCDTB;
            _autoCopyHashes = settings.AutoCopyHashes;
            _createBackUpOldHashes = settings.CreateBackUpOldHashes;
            _onlyCheckDifferences = settings.OnlyCheckDifferences;

            // Diccionario de configuraciones y sus mensajes
            var configLogs = new (bool enabled, string message)[]
            {
                (_syncHashesWithCDTB, "Sync enabled on startup."),
                (_autoCopyHashes, "Automatically replace old hashes enabled."),
                (_createBackUpOldHashes, "Backup old hashes enabled."),
                (_onlyCheckDifferences, "Check only differences enabled.")
            };

            // Mostrar los mensajes de configuración activados
            foreach (var (enabled, message) in configLogs) {
                if (enabled) AppendLog(message);
            }

            // Llamada al sync después de mostrar configuraciones
            if (_syncHashesWithCDTB) { 
                _ = _status.SyncHashesIfNeeds(_syncHashesWithCDTB);
            }
            
            // Cargar las rutas de los directorios desde la configuración al arrancar el programa
            newHashesTextBox.Text = settings.NewHashesPath ?? "";
            oldHashesTextBox.Text = settings.OldHashesPath ?? "";
            
            // Llamar al verificador de actualizaciones
            _ = UpdateManager.CheckForUpdatesAsync(false);
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
                MessageBox.Show("Please select both hash directories.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Llamada al método de extracción con el logger apuntando a AppendLog
            await Program.RunExtraction(newHashesTextBox.Text, oldHashesTextBox.Text, _syncHashesWithCDTB, _autoCopyHashes, _createBackUpOldHashes, _onlyCheckDifferences, AppendLog);
        }
        
        // Abre la ventana de export
        private void btnExport_Click(object sender, EventArgs e)
        {
            using (var exportForm = new ExportForm())
            {
                exportForm.ShowDialog();
            }
        }
        
        // Abre la ventana de help
        private void btnHelp_Click(object sender, EventArgs e)
        {
            using (var helpForm = new HelpForm())
            {
                helpForm.ShowDialog();
            }
        }

        // Abre la ventana de settings
        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm(_syncHashesWithCDTB, _autoCopyHashes, _createBackUpOldHashes, _onlyCheckDifferences, _status))
            {
                // Suscribirse al evento SettingsChanged y PathsChanged
                settingsForm.SettingsChanged += OnSettingsChanged;
                settingsForm.PathsChanged += OnPathsChanged;
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    bool newsyncHashesWithCDTB = settingsForm.SyncHashesWithCDTB;
                    bool newAutoCopyHashes = settingsForm.AutoCopyHashes;
                    bool newCreateBackUpOldHashes = settingsForm.CreateBackUpOldHashes;
                    bool newOnlyCheckDifferences = settingsForm.OnlyCheckDifferences;

                    if (newsyncHashesWithCDTB != _syncHashesWithCDTB || newAutoCopyHashes != _autoCopyHashes || newCreateBackUpOldHashes != _createBackUpOldHashes || newOnlyCheckDifferences != _onlyCheckDifferences)
                    {
                        _syncHashesWithCDTB = newsyncHashesWithCDTB;
                        _autoCopyHashes = newAutoCopyHashes;
                        _createBackUpOldHashes = newCreateBackUpOldHashes;
                        _onlyCheckDifferences = newOnlyCheckDifferences;
                        SaveSettings(_syncHashesWithCDTB, _autoCopyHashes, _createBackUpOldHashes, _onlyCheckDifferences);
                    }
                }
                settingsForm.SettingsChanged -= OnSettingsChanged;
                settingsForm.PathsChanged -= OnPathsChanged;
            }
        }

        // Maneja los cambios en la configuración
        private void OnSettingsChanged(object sender, EventArgs e)
        {
            var settings = AppSettings.LoadSettings(); // Llamamos a AppSettings para cargar los settings
            _syncHashesWithCDTB = settings.SyncHashesWithCDTB;
            _autoCopyHashes = settings.AutoCopyHashes;
            _createBackUpOldHashes = settings.CreateBackUpOldHashes;
            _onlyCheckDifferences = settings.OnlyCheckDifferences;
        }
        
        private void OnPathsChanged(object sender, EventArgs e)
        {
            var settings = AppSettings.LoadSettings();
            newHashesTextBox.Text = settings.NewHashesPath ?? "";
            oldHashesTextBox.Text = settings.OldHashesPath ?? "";
        }

        private void SaveSettings(bool syncHashesWithCDTB, bool autoCopyHashes, bool createBackUpOldHashes, bool onlyCheckDifferences)
        {
            var settings = AppSettings.LoadSettings(); // Llamamos a AppSettings para cargar los settings
            settings.SyncHashesWithCDTB = syncHashesWithCDTB;
            settings.AutoCopyHashes = autoCopyHashes;
            settings.CreateBackUpOldHashes = createBackUpOldHashes;
            settings.OnlyCheckDifferences = onlyCheckDifferences;

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText("config.json", json);
        }

        public void AppendLog(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Action appendMessage = () =>
            {
                richTextBoxLogs.SelectionIndent = 4;
                richTextBoxLogs.AppendText(timestampedMessage + Environment.NewLine);
                richTextBoxLogs.SelectionStart = richTextBoxLogs.TextLength;
                richTextBoxLogs.ScrollToCaret();
            };

            if (richTextBoxLogs.InvokeRequired) richTextBoxLogs.Invoke(appendMessage);
            else appendMessage();
        }
    }
}