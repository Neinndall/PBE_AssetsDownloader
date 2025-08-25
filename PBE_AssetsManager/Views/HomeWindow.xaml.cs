using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Dialogs;

namespace PBE_AssetsManager.Views
{
    public partial class HomeWindow : UserControl
    {
        private readonly LogService _logService;
        private readonly ExtractionService _extractionService;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private AppSettings _appSettings;

        public HomeWindow(
            LogService logService,
            ExtractionService extractionService,
            AppSettings appSettings,
            CustomMessageBoxService customMessageBoxService)
        {
            InitializeComponent();
            _logService = logService;
            _extractionService = extractionService;
            _appSettings = appSettings;
            _customMessageBoxService = customMessageBoxService;

            // Initialize paths from saved settings
            newHashesTextBox.Text = _appSettings.NewHashesPath ?? "";
            oldHashesTextBox.Text = _appSettings.OldHashesPath ?? "";

            // Store the initial loaded path in the Tag property for later comparison
            newHashesTextBox.Tag = newHashesTextBox.Text;
            oldHashesTextBox.Tag = oldHashesTextBox.Text;
        }

        public void UpdateSettings(AppSettings newSettings, bool wasResetToDefaults)
        {
            _appSettings = newSettings;

            UpdatePathTextBox(newHashesTextBox, _appSettings.NewHashesPath, wasResetToDefaults);
            UpdatePathTextBox(oldHashesTextBox, _appSettings.OldHashesPath, wasResetToDefaults);
        }

        private void UpdatePathTextBox(TextBox textBox, string newSettingPath, bool wasResetToDefaults)
        {
            bool isPathChangedInSession = (textBox.Text != (textBox.Tag as string));

            if (wasResetToDefaults || !isPathChangedInSession)
            {
                textBox.Text = newSettingPath ?? "";
                textBox.Tag = textBox.Text;
            }
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
                _customMessageBoxService.ShowWarning("Warning", "Please select both hash directories.", Window.GetWindow(this));
                return;
            }
            
            _appSettings.NewHashesPath = newHashesTextBox.Text;
            _appSettings.OldHashesPath = oldHashesTextBox.Text;
            
            await _extractionService.ExecuteAsync();
        }
    }
}