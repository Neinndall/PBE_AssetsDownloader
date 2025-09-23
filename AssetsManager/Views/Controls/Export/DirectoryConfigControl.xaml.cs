using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using AssetsManager.Services.Core;
using AssetsManager.Services.Downloads;
using AssetsManager.Utils;

namespace AssetsManager.Views.Controls.Export
{
    public partial class DirectoryConfigControl : UserControl
    {
        public LogService LogService { get; set; }
        public ExportService ExportService { get; set; }
        public DirectoriesCreator DirectoriesCreator { get; set; }
        
        public DirectoryConfigControl()
        {
            InitializeComponent();
        }

        private void BtnBrowseDownloadTargetPath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Download Target Folder"
            })
            {
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    txtDownloadTargetPath.Text = dialog.FileName;
                    if (ExportService != null) ExportService.DownloadTargetPath = dialog.FileName;
                }
            }
        }

        private void BtnBrowseDifferencesPath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Differences Files Folder"
            })
            {
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    txtDifferencesPath.Text = dialog.FileName;
                    if (ExportService != null) ExportService.DifferencesPath = dialog.FileName;
                }
            }
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