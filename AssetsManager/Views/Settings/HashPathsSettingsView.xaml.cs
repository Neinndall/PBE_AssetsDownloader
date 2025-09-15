using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using AssetsManager.Services.Core;
using AssetsManager.Utils;

namespace AssetsManager.Views.Settings
{
    public partial class HashPathsSettingsView : UserControl
    {
        private AppSettings _appSettings;
        private readonly LogService _logService;

        public HashPathsSettingsView(LogService logService)
        {
            InitializeComponent();
            _logService = logService;
        }
        
        public void ApplySettingsToUI(AppSettings appSettings)
        {
            _appSettings = appSettings;
            textBoxNewHashPath.Text = _appSettings.NewHashesPath;
            textBoxOldHashPath.Text = _appSettings.OldHashesPath;
            textBoxLolPath.Text = _appSettings.LolDirectory;
        }
        
        public void SaveSettings()
        {
            if (_appSettings == null) return;
            _appSettings.NewHashesPath = textBoxNewHashPath.Text;
            _appSettings.OldHashesPath = textBoxOldHashPath.Text;
            _appSettings.LolDirectory = textBoxLolPath.Text;
        }

        private void btnBrowseNew_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select New Hashes Folder";

                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    textBoxNewHashPath.Text = folderBrowserDialog.FileName;
                }
            }
        }

        private void btnBrowseOld_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select Old Hashes Folder";

                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    textBoxOldHashPath.Text = folderBrowserDialog.FileName;
                }
            }
        }

        private void btnBrowseLol_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select LoL Directory";

                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    textBoxLolPath.Text = folderBrowserDialog.FileName;
                }
            }
        }
    }
}