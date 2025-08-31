using Microsoft.WindowsAPICodePack.Dialogs;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Utils;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views.Controls.Home
{
    public partial class DirectorySelectionControl : UserControl
    {
        public LogService LogService { get; set; }
        public AppSettings AppSettings { get; set; }

        public string NewHashesPath => newHashesTextBox.Text;
        public string OldHashesPath => oldHashesTextBox.Text;

        public DirectorySelectionControl()
        {
            InitializeComponent();
            this.Loaded += DirectorySelectionControl_Loaded;
        }

        private void DirectorySelectionControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize paths from saved settings
            newHashesTextBox.Text = AppSettings.NewHashesPath ?? "";
            oldHashesTextBox.Text = AppSettings.OldHashesPath ?? "";

            // Store the initial loaded path in the Tag property for later comparison
            newHashesTextBox.Tag = newHashesTextBox.Text;
            oldHashesTextBox.Tag = oldHashesTextBox.Text;
        }

        public void UpdateSettings(AppSettings newSettings, bool wasResetToDefaults)
        {
            AppSettings = newSettings;

            UpdatePathTextBox(newHashesTextBox, AppSettings.NewHashesPath, wasResetToDefaults);
            UpdatePathTextBox(oldHashesTextBox, AppSettings.OldHashesPath, wasResetToDefaults);
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
                    LogService.LogDebug($"New Hashes Directory selected: {folderBrowserDialog.FileName}");
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
                    LogService.LogDebug($"Old Hashes Directory selected: {folderBrowserDialog.FileName}");
                }
            }
        }
    }
}
