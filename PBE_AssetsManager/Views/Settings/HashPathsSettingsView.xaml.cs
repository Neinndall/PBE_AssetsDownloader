using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Utils;

namespace PBE_AssetsManager.Views.Settings
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
            textBoxPbePath.Text = _appSettings.PbeDirectory;
        }

        public void SaveSettings()
        {
            if (_appSettings == null) return;
            _appSettings.NewHashesPath = textBoxNewHashPath.Text;
            _appSettings.OldHashesPath = textBoxOldHashPath.Text;
            _appSettings.PbeDirectory = textBoxPbePath.Text;
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
        
        
    private void btnBrowsePbe_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new CommonOpenFileDialog())
            {
                folderBrowserDialog.IsFolderPicker = true;
                folderBrowserDialog.Title = "Select PBE Directory for Explorer";

                if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    textBoxPbePath.Text = folderBrowserDialog.FileName;
                }
            }
        }
    }
}
