using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.UI.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using PBE_AssetsDownloader.UI.Dialogs;

namespace PBE_AssetsDownloader.UI
{
    public partial class ExplorerWindow : UserControl
    {
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly LogService _logService;
        private readonly CustomMessageBoxService _customMessageBoxService;

        public ObservableCollection<FileSystemNodeModel> RootNodes { get; set; }

        public ExplorerWindow(DirectoriesCreator directoriesCreator, LogService logService, CustomMessageBoxService customMessageBoxService)
        {
            InitializeComponent();
            _directoriesCreator = directoriesCreator;
            _logService = logService;
            _customMessageBoxService = customMessageBoxService;
            RootNodes = new ObservableCollection<FileSystemNodeModel>();
            DataContext = this;
            LoadDirectory();
            FileTreeView.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(MenuItem_Click));
        }

        private void LoadDirectory()
        {
            try
            {
                var rootDirectory = Path.Combine(_directoriesCreator.AppDirectory, "AssetsDownloaded");
                if (!Directory.Exists(rootDirectory))
                {
                    _logService.LogWarning($"The directory '{rootDirectory}' does not exist. No assets to explore.");
                    FileTreeView.Visibility = Visibility.Collapsed;
                    NoDirectoryMessage.Visibility = Visibility.Visible;
                    return;
                }

                FileTreeView.Visibility = Visibility.Visible;
                NoDirectoryMessage.Visibility = Visibility.Collapsed;

                var rootNode = new FileSystemNodeModel(rootDirectory);
                RootNodes.Add(rootNode);
            }
            catch (Exception ex)
            {
                _logService.LogError($"Failed to load asset directory. See application_errors.log for details.");
                _logService.LogCritical(ex, "Failed to load asset directory.");
            }
        }

        private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedNode = e.NewValue as FileSystemNodeModel;
            if (selectedNode == null || selectedNode.IsDirectory)
            {
                ResetPreview();
                return;
            }

            try
            {
                if (IsImageExtension(selectedNode.Extension))
                {
                    ShowImagePreview(selectedNode.FullPath);
                }
                else if (IsTextExtension(selectedNode.Extension))
                {
                    ShowTextPreview(selectedNode.FullPath);
                }
                else
                {
                    ResetPreview();
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Failed to preview file '{selectedNode.FullPath}'. See application_errors.log for details.");
                _logService.LogCritical(ex, $"Failed to preview file '{selectedNode.FullPath}'.");
                ResetPreview();
            }
        }

        private void ShowImagePreview(string path)
        {
            TextPreview.Visibility = Visibility.Collapsed;
            PreviewPlaceholder.Visibility = Visibility.Collapsed;
            ImagePreview.Visibility = Visibility.Visible;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            ImagePreview.Source = bitmap;
        }

        private void ShowTextPreview(string path)
        {
            ImagePreview.Visibility = Visibility.Collapsed;
            PreviewPlaceholder.Visibility = Visibility.Collapsed;
            TextPreview.Visibility = Visibility.Visible;

            TextPreview.Text = File.ReadAllText(path);
        }

        private void ResetPreview()
        {
            ImagePreview.Visibility = Visibility.Collapsed;
            TextPreview.Visibility = Visibility.Collapsed;
            PreviewPlaceholder.Visibility = Visibility.Visible;
        }

        private bool IsImageExtension(string extension)
        {
            return extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".bmp" || extension == ".gif";
        }

        private bool IsTextExtension(string extension)
        {
            return extension == ".txt" || extension == ".json" || extension == ".lua" || extension == ".log";
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = e.OriginalSource as MenuItem;
            if (menuItem == null) return;

            var selectedNode = FileTreeView.SelectedItem as FileSystemNodeModel;
            if (selectedNode == null) return;

            var tag = menuItem.Tag as string;
            if (tag == "Delete")
            {
                DeletePath(selectedNode);
            }
            else if (tag == "Info")
            {
                ShowFileInfo(selectedNode.FullPath);
            }
        }

        private void DeletePath(FileSystemNodeModel node)
        {
            var message = node.IsDirectory
                ? $"Are you sure you want to delete the directory '{node.Name}' and all its contents?"
                : $"Are you sure you want to delete the file '{node.Name}'?";

            var result = _customMessageBoxService.ShowYesNo("Confirm Deletion", message, Window.GetWindow(this), CustomMessageBoxIcon.Warning);

            if (result == true)
            {
                try
                {
                    if (node.IsDirectory)
                    {
                        Directory.Delete(node.FullPath, true);
                    }
                    else
                    {
                        File.Delete(node.FullPath);
                    }
                    // This is tricky, need to find the parent and remove it from the collection
                    // For now, just reload the whole tree
                    RootNodes.Clear();
                    LoadDirectory();
                    _logService.Log($"Deleted: {node.FullPath}");
                }
                catch (Exception ex)
                {
                    _logService.LogError($"Failed to delete '{node.FullPath}'. See application_errors.log for details.");
                    _logService.LogCritical(ex, $"Failed to delete '{node.FullPath}'.");
                    _customMessageBoxService.ShowError("Error", $"Could not delete '{node.Name}'.", Window.GetWindow(this), CustomMessageBoxIcon.Error);
                }
            }
        }

        private void ShowFileInfo(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    var message = $"File: {fileInfo.Name}\n" +
                                  $"Size: {fileInfo.Length / 1024:N0} KB\n" +
                                  $"Created: {fileInfo.CreationTime}\n" +
                                  $"Modified: {fileInfo.LastWriteTime}";
                    _customMessageBoxService.ShowInfo("File Information", message, Window.GetWindow(this), CustomMessageBoxIcon.Info);
                }
                else if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    var message = $"Directory: {dirInfo.Name}\n" +
                                  $"Created: {dirInfo.CreationTime}\n" +
                                  $"Last Modified: {dirInfo.LastWriteTime}";
                    _customMessageBoxService.ShowInfo("Directory Information", message, Window.GetWindow(this), CustomMessageBoxIcon.Info);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Failed to get info for '{path}'. See application_errors.log for details.");
                _logService.LogCritical(ex, $"Failed to get info for '{path}'.");
                _customMessageBoxService.ShowError("Error", $"Could not get information for '{Path.GetFileName(path)}'.", Window.GetWindow(this), CustomMessageBoxIcon.Error);
            }
        }
    }
}
