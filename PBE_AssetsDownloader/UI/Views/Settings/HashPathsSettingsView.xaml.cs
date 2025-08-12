using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.Utils;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace PBE_AssetsDownloader.UI.Views.Settings
{
    public partial class HashPathsSettingsView : UserControl, ISettingsView
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
        }

        public void SaveSettings()
        {
            if (_appSettings == null) return;
            _appSettings.NewHashesPath = textBoxNewHashPath.Text;
            _appSettings.OldHashesPath = textBoxOldHashPath.Text;
        }

        private void btnBrowseNew_Click(object sender, System.Windows.RoutedEventArgs e)
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

        private void btnBrowseOld_Click(object sender, System.Windows.RoutedEventArgs e)
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
        
        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveSettings();
        }
    }
}
