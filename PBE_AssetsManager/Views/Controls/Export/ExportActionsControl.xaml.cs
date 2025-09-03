using System;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views.Controls.Export
{
    public partial class ExportActionsControl : UserControl
    {
        public event EventHandler PreviewAssetsRequested;
        public event EventHandler DownloadSelectedAssetsRequested;

        public ExportActionsControl()
        {
            InitializeComponent();
        }

        private void btnPreviewAssets_Click(object sender, RoutedEventArgs e)
        {
            PreviewAssetsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnDownloadSelectedAssets_Click(object sender, RoutedEventArgs e)
        {
            DownloadSelectedAssetsRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
