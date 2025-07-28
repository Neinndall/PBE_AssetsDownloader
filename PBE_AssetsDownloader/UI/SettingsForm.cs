using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;

using System.Reflection;
using System.Drawing;

using PBE_AssetsDownloader.Info;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader.UI
{
    public partial class SettingsForm : Form
    {
        private Requests _requests;
        private DirectoriesCreator _directoriesCreator;
        private Status _status;

        // Rutas de los directorios
        public string NewHashesPath { get; set; }
        public string OldHashesPath { get; set; }
        
        // Convertir las variables públicas a propiedades con getters y setters
        public bool SyncHashesWithCDTB { get; set; }
        public bool AutoCopyHashes { get; set; }
        public bool CreateBackUpOldHashes { get; set; }
        public bool OnlyCheckDifferences { get; set; }
        
        public event EventHandler SettingsChanged;
        public event EventHandler PathsChanged;

        public SettingsForm(bool syncHashesWithCDTB, bool autoCopyHashes, bool createBackUpOldHashes, bool onlyCheckDifferences, Status status)
        {
            InitializeComponent();
            BackgroundHelper.SetBackgroundImage(this);
            ApplicationInfos.SetIcon(this);

            var httpClient = new HttpClient();
            _directoriesCreator = new DirectoriesCreator();
            _requests = new Requests(httpClient, _directoriesCreator);
            _status = new Status(AppendLog);

            // Cargar la configuración al iniciar el formulario
            var settings = AppSettings.LoadSettings();

            SyncHashesWithCDTB = settings.SyncHashesWithCDTB;
            checkBoxSyncHashes.Checked = SyncHashesWithCDTB;
                        
            AutoCopyHashes = settings.AutoCopyHashes;
            checkBoxAutoCopy.Checked = AutoCopyHashes;
                        
            CreateBackUpOldHashes = settings.CreateBackUpOldHashes;
            CheckBoxCreateBackUp.Checked = CreateBackUpOldHashes;
            
            OnlyCheckDifferences = settings.OnlyCheckDifferences;
            checkBoxOnlyCheckDifferences.Checked = OnlyCheckDifferences;

            // Cargar las rutas de los directorios
            NewHashesPath = settings.NewHashesPath;
            OldHashesPath = settings.OldHashesPath;
            
            // Asigna la ruta al TextBox para que se muestre
            textBoxNewHashPath.Text = FormatPath(NewHashesPath);  // Aplicar FormatPath
            textBoxOldHashPath.Text = FormatPath(OldHashesPath);  // Aplicar FormatPath
        }

        private void BtnResetDefaults_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to reset all settings to default values?", "Confirm Reset",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                // Crear una nueva instancia con los valores por defecto
                var currentSettings = AppSettings.LoadSettings(); // Carga la configuración actual
                var defaultSettings = AppSettings.GetDefaultSettings();

                // Conserva los tamaños de hash actuales
                defaultSettings.HashesSizes = currentSettings.HashesSizes;

                // Guarda los valores por defecto, manteniendo HashesSizes
                AppSettings.SaveSettings(defaultSettings);

                // Actualizar controles en el formulario para reflejar los valores por defecto
                checkBoxSyncHashes.Checked = defaultSettings.SyncHashesWithCDTB;
                checkBoxAutoCopy.Checked = defaultSettings.AutoCopyHashes;
                CheckBoxCreateBackUp.Checked = defaultSettings.CreateBackUpOldHashes;
                checkBoxOnlyCheckDifferences.Checked = defaultSettings.OnlyCheckDifferences;

                // Actualizar las rutas en los TextBox aplicando FormatPath
                textBoxNewHashPath.Text = FormatPath(defaultSettings.NewHashesPath);
                textBoxOldHashPath.Text = FormatPath(defaultSettings.OldHashesPath);

                MessageBox.Show("Settings have been reset to default values.", "Reset Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Método para recortar y mostrar solo una parte de la dirección, agregando "..." en el medio
        private string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";  // Si la ruta es null o vacía, no mostramos nada

            int maxLength = 40; // Longitud máxima para mostrar
            if (path.Length > maxLength)
            {
                string start = path.Substring(0, 20); // Toma los primeros 20 caracteres
                string end = path.Substring(path.Length - 15); // Toma los últimos 15 caracteres
                return start + "..." + end; // Junta los dos fragmentos con "..."
            }
            return path; // Si es más corto que maxLength, muestra la ruta completa
        }

        private void btnBrowseNew_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    NewHashesPath = folderDialog.SelectedPath;  // Actualiza la propiedad NewHashesPath
                    textBoxNewHashPath.Text = FormatPath(NewHashesPath);  // Aplica FormatPath
                }
            }
        }

        private void btnBrowseOld_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    OldHashesPath = folderDialog.SelectedPath;  // Actualiza la propiedad OldHashesPath
                    textBoxOldHashPath.Text = FormatPath(OldHashesPath);  // Aplica FormatPath
                }
            }
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            // Guardar los valores del formulario en las propiedades
            bool WasSyncHashesWithCDTB = SyncHashesWithCDTB;
            SyncHashesWithCDTB = checkBoxSyncHashes.Checked;
            AutoCopyHashes = checkBoxAutoCopy.Checked;
            CreateBackUpOldHashes = CheckBoxCreateBackUp.Checked;
            OnlyCheckDifferences = checkBoxOnlyCheckDifferences.Checked;

            // Cargar los ajustes actuales
            var settings = AppSettings.LoadSettings(); // Llamamos a AppSettings para cargar los settings
            settings.SyncHashesWithCDTB = SyncHashesWithCDTB;
            settings.AutoCopyHashes = AutoCopyHashes;
            settings.CreateBackUpOldHashes = CreateBackUpOldHashes;
            settings.OnlyCheckDifferences = OnlyCheckDifferences;
            
            // Guardar las rutas de los directorios
            settings.NewHashesPath = NewHashesPath;
            settings.OldHashesPath = OldHashesPath;

            // Guardar los ajustes
            SaveSettings(SyncHashesWithCDTB, settings.HashesSizes);  // Usamos HashesSizes aquí

            // Si la opción de sincronizar hashes con CDTB fue activada y antes no estaba activada
            if (SyncHashesWithCDTB && !WasSyncHashesWithCDTB)
            {
                var result = MessageBox.Show("Do you want to sync files now?", "Synchronization Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    AppendLog("Checking for updates on the server...");
                    bool isUpdated = await _status.IsUpdatedAsync();

                    if (!isUpdated)
                    {
                        _status.CheckForUpdates(isUpdated);
                        MessageBox.Show("Settings updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        AppendLog("Settings updated.");
                        return;
                    }

                    AppendLog("Starting sync...");
                    await DownloadFiles(SyncHashesWithCDTB, settings.HashesSizes);  // Usamos HashesSizes directamente aquí
                    AppendLog("Synchronization completed.");
                }
            }

            // Muestra "Settings updated" solo cuando haya cambios en los ajustes
            MessageBox.Show("Settings updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            AppendLog("Settings updated.");
            SettingsChanged?.Invoke(this, EventArgs.Empty);
            PathsChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task DownloadFiles(bool syncHashesWithCDTB, Dictionary<string, long> hashesSizes)
        {
            string downloadDirectory = _directoriesCreator.GetHashesNewsDirectoryPath();

            try
            {
                await _requests.DownloadHashesFilesAsync(downloadDirectory, logMessage =>
                {
                    AppendLog(logMessage);
                });

                SaveSettings(syncHashesWithCDTB, hashesSizes);  // Usamos HashesSizes directamente
            }
            catch (Exception ex)
            {
                // Manejo de error directamente aquí
                AppendLog($"Error during download: {ex.Message}");
            }
        }

        private void SaveSettings(bool SyncHashesWithCDTB, Dictionary<string, long> hashSizes = null)
        {
            var settings = AppSettings.LoadSettings(); // Llamamos a AppSettings para cargar los settings
            settings.SyncHashesWithCDTB = SyncHashesWithCDTB;
            settings.AutoCopyHashes = AutoCopyHashes;
            settings.CreateBackUpOldHashes = CreateBackUpOldHashes;
            settings.OnlyCheckDifferences = OnlyCheckDifferences;

            // Guardar las rutas de los directorios
            settings.NewHashesPath = NewHashesPath;
            settings.OldHashesPath = OldHashesPath;
    
            if (hashSizes != null && hashSizes.Count > 0)
            {
                // Guardamos los tamaños de los hashes
                settings.HashesSizes = hashSizes;
            }

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText("config.json", json);
        }

        public void AppendLog(string message)
        {
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
