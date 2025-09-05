using PBE_AssetsManager.Services.Downloads;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Views.Controls.Export;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views
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