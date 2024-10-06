using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using Newtonsoft.Json;
using PBE_NewFileExtractor.Info;
using PBE_NewFileExtractor.Utils;
using PBE_NewFileExtractor.Services;

namespace PBE_NewFileExtractor.UI
{
    public partial class SettingsForm : Form
    {
        private Requests _requests;
        private DirectoriesCreator _directoriesCreator;
        private Status _status;

        public bool syncHashesWithCDTB { get; private set; }

        public SettingsForm(bool syncHashesWithCDTB, Status status)
        {
            InitializeComponent();
            // LLamamos al Icono en la pestaña de Settings
            ApplicationInfos.SetIcon(this);

            // Cargar configuración y establecer el estado del checkbox
            var settings = LoadSettings();
            syncHashesWithCDTB = settings.syncHashesWithCDTB; // Actualizar la propiedad
            checkBoxSyncHashes.Checked = syncHashesWithCDTB; // Usar la propiedad aquí

            var httpClient = new HttpClient();
            _directoriesCreator = new DirectoriesCreator();
            _requests = new Requests(httpClient, _directoriesCreator);
            _status = new Status(AppendLog);
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            // Actualizar la propiedad syncHashesWithCDTB desde el checkbox
            syncHashesWithCDTB = checkBoxSyncHashes.Checked;

            var settings = LoadSettings(); // Cargar configuraciones existentes
            settings.syncHashesWithCDTB = this.syncHashesWithCDTB; // Actualizar solo el campo de sincronización

            var result = MessageBox.Show("Do you want to sync files now?", "Confirm synchronization", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                AppendLog("Checking for updates on the server...");
                bool isUpdated = await _status.IsUpdatedAsync();

                if (!isUpdated)
                {
                    _status.CheckForUpdates(isUpdated);
                    SaveSettings(settings.syncHashesWithCDTB, settings.lastUpdateFile); // Mantener lastUpdateFile
                    return; // No proceder con la descarga
                }

                // Solo ejecutar la sincronización si se ha marcado la opción
                AppendLog("Starting sync...");
                string lastUpdateFile = DateTime.UtcNow.ToString("o"); // O el valor que obtengas de la sincronización
                await DownloadFiles(this.syncHashesWithCDTB, lastUpdateFile);
                MessageBox.Show("Synchronization completed."); // Mensaje de éxito
            }
            else
            {
                SaveSettings(settings.syncHashesWithCDTB, settings.lastUpdateFile); // Mantener lastUpdateFile
                this.DialogResult = DialogResult.OK; // Cerrar formulario si no se desea sincronizar
                this.Close();
            }
        }

        private async Task DownloadFiles(bool syncHashesWithCDTB, string lastUpdateFile)
        {
            string downloadDirectory = _directoriesCreator.GetHashesNewsDirectoryPath();

            try
            {
                // Descargar archivos usando Requests
                await _requests.DownloadHashesFilesAsync(downloadDirectory, logMessage =>
                {
                    // Muestra el mensaje en el cuadro de texto de logs si es necesario
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => textBoxLogs.AppendText(logMessage + Environment.NewLine)));
                    }
                    else
                    {
                        textBoxLogs.AppendText(logMessage + Environment.NewLine);
                    }
                });

                // Actualizar lastUpdateFile tras la sincronización
                SaveSettings(syncHashesWithCDTB, lastUpdateFile);
            }
            catch (Exception ex)
            {
                // Manejo de errores
                _status.HandleError(ex); // Usar el método en Status
            }
        }

        private AppSettings LoadSettings()
        {
            const string configFilePath = "config.json";

            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                return JsonConvert.DeserializeObject<AppSettings>(json);
            }

            return new AppSettings { syncHashesWithCDTB = false }; // Valor por defecto
        }

        private void SaveSettings(bool syncHashesWithCDTB, string lastUpdateFile = null)
        {
            var settings = LoadSettings(); // Cargar la configuración existente

            settings.syncHashesWithCDTB = syncHashesWithCDTB;

            // Solo actualizar lastUpdateFile si se proporciona un nuevo valor
            if (!string.IsNullOrEmpty(lastUpdateFile))
            {
                settings.lastUpdateFile = lastUpdateFile;
            }

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText("config.json", json);
        }

        // Agrega mensajes al log en la interfaz de usuario
        public void AppendLog(string message)
        {
            if (textBoxLogs.InvokeRequired)
            {
                textBoxLogs.Invoke(new Action(() => textBoxLogs.AppendText(message + Environment.NewLine)));
            }
            else
            {
                textBoxLogs.AppendText(message + Environment.NewLine);
            }
        }

        public class AppSettings
        {
            public bool syncHashesWithCDTB { get; set; }
            public string lastUpdateFile { get; set; } // Asegúrate de que esta propiedad esté presente
        }
    }
}
