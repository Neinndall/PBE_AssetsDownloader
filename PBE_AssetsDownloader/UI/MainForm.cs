using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Net.Http;
using System.Drawing;
using PBE_NewFileExtractor.Info;
using PBE_NewFileExtractor.Utils;
using PBE_NewFileExtractor.Services;

namespace PBE_NewFileExtractor.UI
{
    public partial class MainForm : Form
    {
        private bool _syncHashesWithCDTB; // Indica si se debe sincronizar con CDTB
        private Status _status; // Instancia para manejar el estado del servidor

        public MainForm()
        {
            InitializeComponent();
            // Llama a ApplicationInfos para configurar el icono y el título del formulario
            ApplicationInfos.SetInfo(this);

            // Inicializa la clase Status
            _status = new Status(AppendLog);
            
            // Cargar configuración al iniciar
            var settings = LoadSettings();
            _syncHashesWithCDTB = settings.syncHashesWithCDTB;

            // Sincronizar solo al iniciar si la opción está habilitada
            if (_syncHashesWithCDTB)
            {
                AppendLog("Sync enabled on startup.");
                _ = SyncHashesOnly(); // Inicia la sincronización de hashes si está habilitada
            }
        }

        // Selecciona el directorio para los nuevos hashes
        private void btnSelectNewHashesDirectory_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                newHashesTextBox.Text = folderBrowserDialog.SelectedPath;
            }
        }
        
        // Selecciona el directorio para los hashes antiguos
        private void btnSelectOldHashesDirectory_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                oldHashesTextBox.Text = folderBrowserDialog.SelectedPath;
            }
        }

        // Al hacer clic en el botón de inicio, se ejecuta la extracción de archivos
        private async void startButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(oldHashesTextBox.Text) || string.IsNullOrEmpty(newHashesTextBox.Text))
            {
                MessageBox.Show("Please select both hash directories.");
                return;
            }

            // Ejecuta el proceso de extracción de hashes y actualiza los logs
            await Program.RunExtraction(newHashesTextBox.Text, oldHashesTextBox.Text, _syncHashesWithCDTB, logMessage =>
            {
                if (textBoxLogs.InvokeRequired)
                {
                    textBoxLogs.Invoke(new Action(() => textBoxLogs.AppendText(logMessage + Environment.NewLine)));
                }
                else
                {
                    textBoxLogs.AppendText(logMessage + Environment.NewLine);
                }
            });
        }

        // Abre la ventana de ayuda
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
            using (var settingsForm = new SettingsForm(_syncHashesWithCDTB, _status)) // Pasar _status
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    bool newsyncHashesWithCDTB = settingsForm.syncHashesWithCDTB;
                    if (newsyncHashesWithCDTB != _syncHashesWithCDTB)
                    {
                        _syncHashesWithCDTB = newsyncHashesWithCDTB;
                        SaveSettings(_syncHashesWithCDTB);
                        MessageBox.Show(_syncHashesWithCDTB ? "Sync enabled." : "Sync disabled.");
                    }
                }
            }
        }

        //private void pbSettings_Click(object sender, EventArgs e)
        //{
        //    // Lógica para abrir configuración
        //}
        //
        //// private void pbSettings_MouseEnter(object sender, EventArgs e)
        //// {
        ////     this.pbSettings.BackColor = Color.LightGray;
        //// }
        //
        //private void pbSettings_MouseEnter(object sender, EventArgs e)
        //{
        //    // Crear un nuevo gráfico con un rectángulo que define el área del círculo
        //    System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
        //    int diameter = pbSettings.Width + 20; // Asumimos que el PictureBox es cuadrado
        //    path.AddEllipse(-10, -10, diameter, diameter);
        //    
        //    // Establecer la región del PictureBox para que sea circular
        //    pbSettings.Region = new Region(path);
        //    
        //    // Cambiar el color de fondo a gris
        //    pbSettings.BackColor = Color.LightGray;
        //
        //    // Redibujar el PictureBox
        //    pbSettings.Invalidate();
        //}
        //
        //private void pbSettings_MouseLeave(object sender, EventArgs e)
        //{
        //    pbSettings.Region = null;
        //    pbSettings.BackColor = Color.Transparent; // O el color por defecto que quieras
        //    pbSettings.Invalidate();
        //}
    
        // Sincroniza solo los hashes si la opción está activada
        private async Task SyncHashesOnly()
        {
            var directoriesCreator = new DirectoriesCreator(); // Inicializa correctamente
            var httpClient = new HttpClient();
            var requests = new Requests(httpClient, directoriesCreator);

            // Verifica si el servidor ha sido actualizado
            bool isUpdated = await _status.IsUpdatedAsync();
            if (isUpdated)
            {
                AppendLog("Server updated. Starting hash synchronization...");
                // Llama a SyncHashesIfEnabledAsync pasando el argumento
                await requests.SyncHashesIfEnabledAsync(_syncHashesWithCDTB, AppendLog);
            }
            else
            {
                _status.CheckForUpdates(isUpdated); // Muestra el mensaje de no actualización
            }
        }

        // Guarda la configuración en un archivo JSON
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

        // Carga la configuración desde un archivo JSON
        private AppSettings LoadSettings()
        {
            const string configFilePath = "config.json";

            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }

            return new AppSettings { syncHashesWithCDTB = false };
        }

        // Clase para almacenar la configuración de la aplicación
        public class AppSettings
        {
            public bool syncHashesWithCDTB { get; set; }
            public string lastUpdateFile { get; set; }
        }
    }
}
