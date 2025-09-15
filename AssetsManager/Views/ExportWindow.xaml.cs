using AssetsManager.Services.Downloads;
using AssetsManager.Services;
using AssetsManager.Services.Core;
using AssetsManager.Views.Controls.Export;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AssetsManager.Views
{
    public partial class ExportWindow : UserControl
    {
        public event EventHandler<PreviewRequestedEventArgs> PreviewRequested;

        public ExportWindow(
            LogService logService,
            AssetDownloader assetDownloader,
            CustomMessageBoxService customMessageBoxService
            )
        {
            InitializeComponent();
            FilterConfig.PreviewRequested += (sender, args) => PreviewRequested?.Invoke(this, args);
            ExportActions.PreviewAssetsRequested += btnPreviewAssets_Click;
            ExportActions.DownloadSelectedAssetsRequested += BtnDownloadSelectedAssets_Click;

            DirectoryConfig.LogService = logService;

            FilterConfig.LogService = logService;
            FilterConfig.AssetDownloader = assetDownloader;
            FilterConfig.CustomMessageBoxService = customMessageBoxService;
        }

        private void btnPreviewAssets_Click(object sender, EventArgs e)
        {
            string differencesPath = DirectoryConfig.DifferencesPath;
            FilterConfig.DoPreview(differencesPath);
        }

        private async void BtnDownloadSelectedAssets_Click(object sender, EventArgs e)
        {
            string differencesPath = DirectoryConfig.DifferencesPath;
            string downloadPath = DirectoryConfig.DownloadTargetPath;
            await FilterConfig.DoDownload(differencesPath, downloadPath);
        }
    }
}