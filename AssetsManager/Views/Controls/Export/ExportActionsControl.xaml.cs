using AssetsManager.Services.Downloads;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AssetsManager.Views.Controls.Export
{
    public partial class ExportActionsControl : UserControl
    {
        public ExportService ExportService { get; set; }

        public ExportActionsControl()
        {
            InitializeComponent();
        }

        private void btnPreviewAssets_Click(object sender, RoutedEventArgs e)
        {
            ExportService.DoPreview();
        }

        private async void BtnDownloadSelectedAssets_Click(object sender, RoutedEventArgs e)
        {
            await ExportService.DoDownload();
        }
    }
}
