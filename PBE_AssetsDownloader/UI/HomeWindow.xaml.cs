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
    public partial class HomeWindow : UserControl
    {
        private readonly LogService _logService;
        private readonly HttpClient _httpClient;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly Requests _requests;
        private readonly Status _status;
        private readonly AssetDownloader _assetDownloader;
        private AppSettings _appSettings;

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
            _logService = logService;
            _httpClient = httpClient;
            _directoriesCreator = directoriesCreator;
            _requests = requests;
            _status = status;
            _assetDownloader = assetDownloader;
            _appSettings = appSettings;

            // Initialize paths from saved settings
            newHashesTextBox.Text = _appSettings.NewHashesPath ?? "";
            oldHashesTextBox.Text = _appSettings.OldHashesPath ?? "";

            // Store the initial loaded path in the Tag property for later comparison
            newHashesTextBox.Tag = newHashesTextBox.Text;
            oldHashesTextBox.Tag = oldHashesTextBox.Text;
        }

        /// <summary>
        /// Updates the UI and session paths when settings are saved from the SettingsWindow.
        /// This logic respects user input in the current session by comparing Text with Tag.
        /// </summary>
        public void UpdateSettings(AppSettings newSettings, bool wasResetToDefaults)
        {
            _appSettings = newSettings;

            UpdatePathTextBox(newHashesTextBox, _appSettings.NewHashesPath, wasResetToDefaults);
            UpdatePathTextBox(oldHashesTextBox, _appSettings.OldHashesPath, wasResetToDefaults);
        }

        /// <summary>
        /// Helper method to update a single path TextBox and its associated session variable.
        /// </summary>
        private void UpdatePathTextBox(TextBox textBox, string newSettingPath, bool wasResetToDefaults)
        {
            // Determina si la ruta ha sido modificada por el usuario durante la sesión actual.
            // Compara el texto actual (.Text) con el valor original que se cargó al inicio (almacenado en .Tag).
            bool isPathChangedInSession = (textBox.Text != (textBox.Tag as string));

            // Si se han reseteado los ajustes a sus valores por defecto,
            // O si es un guardado normal y el usuario NO ha modificado la ruta en esta sesión:
            if (wasResetToDefaults || !isPathChangedInSession)
            {
                // Actualiza el texto del TextBox y el Tag con el nuevo valor de la configuración.
                textBox.Text = newSettingPath ?? "";
                textBox.Tag = textBox.Text;
            }
            // else: Si es un guardado normal Y el usuario SÍ ha modificado la ruta manualmente,
            // no hacemos nada, respetando la entrada del usuario en el TextBox.
        }

        private void btnSelectNewHashesDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select New Hashes Directory";
                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    newHashesTextBox.Text = folderBrowserDialog.FileName;
                    _logService.LogDebug($"New Hashes Directory selected: {folderBrowserDialog.FileName}");
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
                    oldHashesTextBox.Text = folderBrowserDialog.FileName;
                    _logService.LogDebug($"Old Hashes Directory selected: {folderBrowserDialog.FileName}");
                }
            }
        }

        private async void startButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(oldHashesTextBox.Text) || string.IsNullOrEmpty(newHashesTextBox.Text))
            {
                _logService.LogWarning("Please select both hash directories.");
                MessageBox.Show("Please select both hash directories.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _logService.Log("Starting asset extraction process...");
            await App.RunExtraction(
                _logService,
                _httpClient,
                _directoriesCreator,
                _assetDownloader,
                _requests,
                newHashesTextBox.Text, // Usar Text directamente
                oldHashesTextBox.Text, // Usar Text directamente
                _appSettings.SyncHashesWithCDTB,
                _appSettings.AutoCopyHashes,
                _appSettings.CreateBackUpOldHashes,
                _appSettings.OnlyCheckDifferences,
                _appSettings.CheckJsonDataUpdates
            );
        }
    }
}