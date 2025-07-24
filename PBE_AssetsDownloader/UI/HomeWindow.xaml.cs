// PBE_AssetsDownloader/UI/HomeWindow.xaml.cs
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs; // Para el explorador de carpetas
using PBE_AssetsDownloader.Info;
using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.Utils;

namespace PBE_AssetsDownloader.UI
{
    /// <summary>
    /// Lógica de interacción para HomeWindow.xaml
    /// </summary>
    public partial class HomeWindow : UserControl
    {
        // Declara las dependencias que este UserControl necesita
        private readonly LogService _logService;
        private readonly HttpClient _httpClient; // Si los métodos de App.RunExtraction lo requieren
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly Requests _requests;
        private readonly Status _status;
        private readonly AssetDownloader _assetDownloader;

        private AppSettings _appSettings; // La instancia de AppSettings para que esta vista pueda leer/escribir rutas
        private string _sessionNewHashesPath; // Ruta de New Hashes para la sesión actual
        private string _sessionOldHashesPath; // Ruta de Old Hashes para la sesión actual
        public event EventHandler PathsChanged; // Define eventos que MainWindow puede escuchar si HomeWindow cambia paths

        // Constructor del HomeWindow - Recibe las mismas dependencias que MainWindow, más AppSettings
        public HomeWindow(
            LogService logService,
            HttpClient httpClient,
            DirectoriesCreator directoriesCreator,
            Requests requests,
            Status status,
            AssetDownloader assetDownloader,
            AppSettings appSettings)
            
        {
            InitializeComponent();

            // Asigna las dependencias a los campos readonly
            _logService = logService;
            _httpClient = httpClient;
            _directoriesCreator = directoriesCreator;
            _requests = requests;
            _status = status;
            _assetDownloader = assetDownloader;

            // Asigna la instancia de AppSettings
            _appSettings = appSettings;

            // Inicializa las variables de sesión con los paths guardados en AppSettings al cargar la vista
            _sessionNewHashesPath = _appSettings.NewHashesPath ?? "";
            _sessionOldHashesPath = _appSettings.OldHashesPath ?? "";

            // Inicializa los TextBoxes con los paths de la sesión
            newHashesTextBox.Text = _sessionNewHashesPath;
            oldHashesTextBox.Text = _sessionOldHashesPath;
        }

        /// <summary>
        /// Método para actualizar los TextBoxes de rutas cuando los AppSettings cambian
        /// (por ejemplo, si se modifican desde la SettingsWindow).
        /// </summary>
        /// <param name="newSettings">La instancia actualizada de AppSettings.</param>
        public void UpdateSettings(AppSettings newSettings)
        {
            _appSettings = newSettings; // Actualiza la referencia local
            _sessionNewHashesPath = _appSettings.NewHashesPath ?? "";
            _sessionOldHashesPath = _appSettings.OldHashesPath ?? "";
            newHashesTextBox.Text = _sessionNewHashesPath;
            oldHashesTextBox.Text = _sessionOldHashesPath;
        }

        private void btnSelectNewHashesDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select New Hashes Directory";

                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    _sessionNewHashesPath = folderBrowserDialog.FileName; // Actualiza la variable de sesión
                    newHashesTextBox.Text = _sessionNewHashesPath; // Actualiza el TextBox
                    _logService.Log($"New Hashes Directory selected: {folderBrowserDialog.FileName}");
                    // No se guarda en AppSettings ni en config.json desde aquí
                    PathsChanged?.Invoke(this, EventArgs.Empty); // Notifica a MainWindow que los paths han cambiado
                }
            }
        }

        private void btnSelectOldHashesDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select Old Hashes Directory";

                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    _sessionOldHashesPath = folderBrowserDialog.FileName; // Actualiza la variable de sesión
                    oldHashesTextBox.Text = _sessionOldHashesPath; // Actualiza el TextBox
                    _logService.Log($"Old Hashes Directory selected: {folderBrowserDialog.FileName}");
                    // No se guarda en AppSettings ni en config.json desde aquí
                    PathsChanged?.Invoke(this, EventArgs.Empty); // Notifica a MainWindow que los paths han cambiado
                }
            }
        }

        private async void startButton_Click(object sender, RoutedEventArgs e)
        {
            // Validar usando las variables de sesión
            if (string.IsNullOrEmpty(_sessionOldHashesPath) || string.IsNullOrEmpty(_sessionNewHashesPath))
            {
                _logService.LogWarning("Please select both hash directories.");
                MessageBox.Show("Please select both hash directories.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _logService.Log("Starting asset extraction process...");

            // Llama a RunExtraction de la clase App, pasando las rutas de la sesión
            await App.RunExtraction(
                _logService,
                _httpClient,
                _directoriesCreator,
                _assetDownloader,
                _requests,
                _sessionNewHashesPath, // Usar la ruta de la sesión
                _sessionOldHashesPath, // Usar la ruta de la sesión
                _appSettings.SyncHashesWithCDTB,
                _appSettings.AutoCopyHashes,
                _appSettings.CreateBackUpOldHashes,
                _appSettings.OnlyCheckDifferences,
                _appSettings.CheckJsonDataUpdates
            );
    }
    }
}