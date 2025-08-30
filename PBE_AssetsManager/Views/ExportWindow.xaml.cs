using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views
{
    public partial class ExportWindow : UserControl
    {
        public ExportWindow()
        {
            InitializeComponent();
        }

        private void btnPreviewAssets_Click(object sender, RoutedEventArgs e)
        {
            string differencesPath = DirectoryConfig.DifferencesPath;
            FilterConfig.DoPreview(differencesPath);
        }

        private async void BtnDownloadSelectedAssets_Click(object sender, RoutedEventArgs e)
        {
            string differencesPath = DirectoryConfig.DifferencesPath;
            string downloadPath = DirectoryConfig.DownloadTargetPath;
            await FilterConfig.DoDownload(differencesPath, downloadPath);
        }
    }
}
