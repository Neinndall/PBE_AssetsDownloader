using Microsoft.WindowsAPICodePack.Dialogs;
using AssetsManager.Services.Core;
using AssetsManager.Services.Downloads;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AssetsManager.Views.Controls.Export
{
    public partial class DirectoryConfigControl : UserControl
    {
        public LogService LogService { get; set; }
        public ExportService ExportService { get; set; }

        public DirectoryConfigControl()
        {
            InitializeComponent();
        }

        private void BtnBrowseDownloadTargetPath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = "Select Download Target Folder";

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    txtDownloadTargetPath.Text = dialog.FileName;
                    if (ExportService != null) ExportService.DownloadTargetPath = dialog.FileName;
                    LogService.LogDebug($"Download Target Path selected: {dialog.FileName}");
                }
            }
        }

        private void BtnBrowseDifferencesPath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = "Select Differences Files Folder";

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    txtDifferencesPath.Text = dialog.FileName;
                    if (ExportService != null) ExportService.DifferencesPath = dialog.FileName;
                    LogService.LogDebug($"Differences Files Path selected: {dialog.FileName}");
                }
            }
        }
    }
}