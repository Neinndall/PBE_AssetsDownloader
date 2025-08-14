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
using Microsoft.Web.WebView2.Core;
using System.Linq;
using System.Collections.Generic;

namespace PBE_AssetsDownloader.UI
{
    public partial class ExplorerWindow : UserControl
    {
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly LogService _logService;
        private readonly CustomMessageBoxService _customMessageBoxService;

        public ObservableCollection<FileSystemNodeModel> RootNodes { get; set; }
        private readonly List<FileSystemNodeModel> _fullTree = new List<FileSystemNodeModel>();

        public ExplorerWindow(DirectoriesCreator directoriesCreator, LogService logService, CustomMessageBoxService customMessageBoxService)
        {
            InitializeComponent();
            _directoriesCreator = directoriesCreator;
            _logService = logService;
            _customMessageBoxService = customMessageBoxService;
            RootNodes = new ObservableCollection<FileSystemNodeModel>();

            DataContext = this;
            this.Loaded += ExplorerWindow_Loaded;
            this.Unloaded += ExplorerWindow_Unloaded;
            InitializeWebView2();
        }

        private void Info_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is FileSystemNodeModel node)
            {
                ShowFileInfo(node.FullPath);
            }
        }

        private void Delete_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is FileSystemNodeModel node)
            {
                DeletePath(node);
            }
        }

        private async void InitializeWebView2()
        {
            try
            {
                var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PBE_AssetsDownloader", "WebView2Data");
                var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: appDataPath);
                await WebView2Preview.EnsureCoreWebView2Async(environment);
            }
            catch (Exception ex)
            {
                _logService.LogError("WebView2 initialization failed. Previews will be affected. See application_errors.log for details.");
                _logService.LogCritical(ex, "WebView2 Initialization Failed");
                _customMessageBoxService.ShowError("Error", "Could not initialize content viewer. Some previews may not work correctly.", Window.GetWindow(this), CustomMessageBoxIcon.Error);
            }
        }

        private void ExplorerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDirectory();
        }

        private void ExplorerWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            WebView2Preview.CoreWebView2?.Navigate("about:blank");
        }

        private void LoadDirectory()
        {
            try
            {
                var rootDirectory = Path.Combine(_directoriesCreator.AppDirectory, "AssetsDownloaded");
                if (!Directory.Exists(rootDirectory))
                {
                    FileTreeView.Visibility = Visibility.Collapsed;
                    NoDirectoryMessage.Visibility = Visibility.Visible;
                    return;
                }

                FileTreeView.Visibility = Visibility.Visible;
                NoDirectoryMessage.Visibility = Visibility.Collapsed;

                _fullTree.Clear();
                RootNodes.Clear();

                var rootNode = new FileSystemNodeModel(rootDirectory);
                _fullTree.Add(rootNode);

                foreach (var node in _fullTree)
                {
                    RootNodes.Add(node);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Failed to load asset directory. See application_errors.log for details.");
                _logService.LogCritical(ex, "Failed to load asset directory.");
            }
        }

        private void txtSearchExplorer_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearchExplorer.Text;
            txtSearchExplorerPlaceholder.Visibility = string.IsNullOrEmpty(searchText) ? Visibility.Visible : Visibility.Collapsed;

            RootNodes.Clear();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                foreach (var node in _fullTree)
                {
                    RootNodes.Add(node);
                }
            }
            else
            {
                foreach (var rootNode in _fullTree)
                {
                    var filteredNode = FilterNode(rootNode, searchText);
                    if (filteredNode != null)
                    {
                        RootNodes.Add(filteredNode);
                    }
                }
            }
        }

        private FileSystemNodeModel FilterNode(FileSystemNodeModel node, string searchText)
        {
            bool selfMatches = node.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;

            if (node.IsDirectory)
            {
                if (selfMatches)
                {
                    var fullNode = new FileSystemNodeModel(node.FullPath);
                    fullNode.IsExpanded = true;
                    return fullNode;
                }

                var filteredChildren = new ObservableCollection<FileSystemNodeModel>();
                foreach (var child in node.Children)
                {
                    var filteredChild = FilterNode(child, searchText);
                    if (filteredChild != null)
                    {
                        filteredChildren.Add(filteredChild);
                    }
                }

                if (filteredChildren.Any())
                {
                    var newNode = new FileSystemNodeModel(node.FullPath, isDirectory: true, children: filteredChildren);
                    newNode.IsExpanded = true;
                    return newNode;
                }
            }
            else if (selfMatches)
            {
                return new FileSystemNodeModel(node.FullPath, isDirectory: false);
            }

            return null;
        }

        private void txtSearchExplorer_GotFocus(object sender, RoutedEventArgs e)
        {
            txtSearchExplorerPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void txtSearchExplorer_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtSearchExplorer.Text))
            {
                txtSearchExplorerPlaceholder.Visibility = Visibility.Visible;
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
                else if (IsPreviewableInWebView(selectedNode.Extension))
                {
                    ShowWebViewPreview(selectedNode.FullPath);
                }
                else
                {
                    ShowUnsupportedPreview(selectedNode.Extension);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Failed to preview file '{{selectedNode.FullPath}}'. See application_errors.log for details.");
                _logService.LogCritical(ex, $"Failed to preview file '{{selectedNode.FullPath}}'.");
                ResetPreview();
            }
        }

        private void ShowImagePreview(string path)
        {
            ResetPreview();
            ImagePreview.Visibility = Visibility.Visible;
            PreviewPlaceholder.Visibility = Visibility.Collapsed;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            ImagePreview.Source = bitmap;
        }

        private void ShowWebViewPreview(string path)
        {
            ResetPreview();
            WebView2Preview.Visibility = Visibility.Visible;
            PreviewPlaceholder.Visibility = Visibility.Collapsed;
            WebView2Preview.CoreWebView2.Navigate(new Uri(path).AbsoluteUri);
        }

        private void ShowUnsupportedPreview(string extension)
        {
            ResetPreview();
            PreviewPlaceholder.Visibility = Visibility.Visible;
            SelectFileMessage.Visibility = Visibility.Collapsed;
            SelectFileSubMessage.Visibility = Visibility.Collapsed;
            UnsupportedFileMessage.Visibility = Visibility.Visible;
            UnsupportedFileMessage.Text = $"Preview not available for '{{extension}}' files.";
        }

        private void ResetPreview()
        {
            PreviewPlaceholder.Visibility = Visibility.Visible;
            SelectFileMessage.Visibility = Visibility.Visible;
            SelectFileSubMessage.Visibility = Visibility.Visible;
            UnsupportedFileMessage.Visibility = Visibility.Collapsed;

            ImagePreview.Visibility = Visibility.Collapsed;
            TextPreview.Visibility = Visibility.Collapsed;
            WebView2Preview.Visibility = Visibility.Collapsed;
            
            if (WebView2Preview != null && WebView2Preview.CoreWebView2 != null)
            {
                WebView2Preview.CoreWebView2.Navigate("about:blank");
            }
        }

        private bool IsImageExtension(string extension)
        {
            return extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".bmp" || extension == ".gif";
        }

        private bool IsPreviewableInWebView(string extension)
        {
            return extension == ".svg" ||
                   extension == ".txt" || extension == ".json" || extension == ".log" ||
                   extension == ".webm" || extension == ".ogg" || extension == ".mp4" ||
                   extension == ".mp3" || extension == ".wav";
        }

        private void DeletePath(FileSystemNodeModel node)
        {
            var message = node.IsDirectory
                ? $"Are you sure you want to delete the directory '{node.Name}' and all its contents?"
                : $"Are you sure you want to delete the file '{node.Name}'";

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
                    LoadDirectory();
                    _logService.Log($"Deleted: {{node.FullPath}}");
                }
                catch (Exception ex)
                {
                    _logService.LogError($"Failed to delete '{{node.FullPath}}'. See application_errors.log for details.");
                    _logService.LogCritical(ex, $"Failed to delete '{{node.FullPath}}'.");
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
                    var message = $"File: {{fileInfo.Name}}\n" +
                                  $"Size: {{fileInfo.Length / 1024:N0}} KB\n" +
                                  $"Created: {{fileInfo.CreationTime}}\n" +
                                  $"Modified: {{fileInfo.LastWriteTime}}";
                    _customMessageBoxService.ShowInfo("File Information", message, Window.GetWindow(this), CustomMessageBoxIcon.Info);
                }
                else if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    var message = $"Directory: {{dirInfo.Name}}\n" +
                                  $"Created: {{dirInfo.CreationTime}}\n" +
                                  $"Last Modified: {{dirInfo.LastWriteTime}}";
                    _customMessageBoxService.ShowInfo("Directory Information", message, Window.GetWindow(this), CustomMessageBoxIcon.Info);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Failed to get info for '{{path}}'. See application_errors.log for details.");
                _logService.LogCritical(ex, $"Failed to get info for '{{path}}'.");
                _customMessageBoxService.ShowError("Error", $"Could not get information for '{Path.GetFileName(path)}'.", Window.GetWindow(this), CustomMessageBoxIcon.Error);
            }
        }
    }
}