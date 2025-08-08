using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.Utils;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace PBE_AssetsDownloader.UI.Views.SettingsViews
{
    public partial class HashPathsSettingsView : UserControl
    {
        private AppSettings _appSettings;
        private LogService _logService;

        public HashPathsSettingsView() 
        {
            InitializeComponent();
        }

        public void ApplySettingsToUI(AppSettings appSettings, LogService logService)
        {
            _appSettings = appSettings;
            _logService = logService;
            textBoxNewHashPath.Text = _appSettings.NewHashesPath;
            textBoxOldHashPath.Text = _appSettings.OldHashesPath;
        }

        public void SaveSettings()
        {
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
    }
}