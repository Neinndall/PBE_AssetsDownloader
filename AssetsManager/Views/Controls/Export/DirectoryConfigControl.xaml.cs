using Microsoft.WindowsAPICodePack.Dialogs;
using AssetsManager.Services;
using AssetsManager.Services.Core;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AssetsManager.Views.Controls.Export
{
    public partial class DirectoryConfigControl : UserControl
    {
        public LogService LogService { get; set; }

        public string DifferencesPath => txtDifferencesPath.Text;
        public string DownloadTargetPath => txtDownloadTargetPath.Text;

        public DirectoryConfigControl()
        {
            InitializeComponent();
        }

        private void BtnBrowseDownloadTargetPath_Click(object sender, RoutedEventArgs e)
        {
            BrowseFolder("Select Download Target Folder", folder => txtDownloadTargetPath.Text = folder, "Download Target Path");
        }

        private void BtnBrowseDifferencesPath_Click(object sender, RoutedEventArgs e)
        {
            BrowseFolder("Select Differences Files Folder", folder => txtDifferencesPath.Text = folder, "Differences Files Path");
        }

        private void BrowseFolder(string title, Action<string> onSuccess, string logPrefix)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = title;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    onSuccess(dialog.FileName);
                    LogService.LogDebug($"{logPrefix} selected: {dialog.FileName}");
                }
            }
        }
    }
}