using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Models;
using Microsoft.Web.WebView2.Core;

namespace PBE_AssetsManager.Views
{
    public partial class ExplorerWindow : UserControl
    {
        private readonly LogService _logService;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private readonly HashResolverService _hashResolverService;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly ExplorerPreviewService _explorerPreviewService;
        private readonly WadNodeLoaderService _wadNodeLoaderService;

        public ObservableCollection<FileSystemNodeModel> RootNodes { get; set; }
        public ExplorerWindow(LogService logService, CustomMessageBoxService customMessageBoxService, HashResolverService hashResolverService, DirectoriesCreator directoriesCreator, WadNodeLoaderService wadNodeLoaderService, ExplorerPreviewService explorerPreviewService)
        {
            InitializeComponent();
            _logService = logService;
            _customMessageBoxService = customMessageBoxService;
            _hashResolverService = hashResolverService;
            _directoriesCreator = directoriesCreator;
            _wadNodeLoaderService = wadNodeLoaderService;
            _explorerPreviewService = explorerPreviewService;

            _explorerPreviewService.Initialize(
                ImagePreview,
                WebView2Preview,
                PreviewPlaceholder,
                SelectFileMessagePanel,
                UnsupportedFileMessagePanel,
                UnsupportedFileMessage
            );

            RootNodes = new ObservableCollection<FileSystemNodeModel>();
            DataContext = this;
            this.Loaded += ExplorerWindow_Loaded;
            this.Unloaded += ExplorerWindow_Unloaded;
        }

        private async void ExplorerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebView2();

            var settings = AppSettings.LoadSettings();
            if (!string.IsNullOrEmpty(settings.PbeDirectory) && Directory.Exists(settings.PbeDirectory))
            {
                LoadInitialDirectory(settings.PbeDirectory);
            }
            else
            {
                FileTreeView.Visibility = Visibility.Collapsed;
                NoDirectoryMessage.Visibility = Visibility.Visible;
            }
        }

        private void ExplorerWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            WebView2Preview.CoreWebView2?.Navigate("about:blank");
        }

        private void SelectPbeDirButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select the PBE Directory",
                Filter = "All files (*.*)|*.*",
                CheckFileExists = false,
                ValidateNames = false,
                FileName = "Folder Selection."
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string pbeDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                if (Directory.Exists(pbeDirectory))
                {
                    LoadInitialDirectory(pbeDirectory);

                    var settings = AppSettings.LoadSettings();
                    settings.PbeDirectory = pbeDirectory;
                    AppSettings.SaveSettings(settings);
                }
                else
                {
                    _customMessageBoxService.ShowError("Error", "Invalid directory selected.", Window.GetWindow(this));
                }
            }
        }

        private async void LoadInitialDirectory(string rootPath)
        {
            NoDirectoryMessage.Visibility = Visibility.Collapsed;
            FileTreeView.Visibility = Visibility.Collapsed;
            LoadingIndicator.Visibility = Visibility.Visible;

            try
            {
                await _hashResolverService.LoadHashesAsync();
                await _hashResolverService.LoadBinHashesAsync();

                RootNodes.Clear();
                var rootNode = new FileSystemNodeModel(rootPath);
                RootNodes.Add(rootNode);

                FileTreeView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Failed to load initial directory or hashes.");
                _customMessageBoxService.ShowError("Error", "Could not load the directory. Please check the logs.", Window.GetWindow(this));
                FileTreeView.Visibility = Visibility.Collapsed;
                NoDirectoryMessage.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            var item = sender as TreeViewItem;
            if (item?.DataContext is not FileSystemNodeModel node) return;

            if (node.Children.Count != 1 || node.Children[0].Name != "Loading...") return;

            item.IsEnabled = false;
            try
            {
                node.Children.Clear();
                switch (node.Type)
                {
                    case NodeType.RealDirectory:
                        LoadRealDirectoryChildren(node);
                        break;
                    case NodeType.WadFile:
                        var children = await _wadNodeLoaderService.LoadChildrenAsync(node);
                        foreach (var child in children)
                        {
                            node.Children.Add(child);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Failed to expand node: {node.FullPath}");
            }
            finally
            {
                item.IsEnabled = true;
            }
        }

        private void LoadRealDirectoryChildren(FileSystemNodeModel parent)
        {
            try
            {
                var directories = Directory.GetDirectories(parent.FullPath);
                foreach (var dir in directories.OrderBy(d => d))
                {
                    parent.Children.Add(new FileSystemNodeModel(dir));
                }

                var files = Directory.GetFiles(parent.FullPath);
                foreach (var file in files.OrderBy(f => f))
                {
                    parent.Children.Add(new FileSystemNodeModel(file));
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logService.LogWarning($"Access denied to: {parent.FullPath}");
            }
        }

        private async void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedNode = e.NewValue as FileSystemNodeModel;
            await _explorerPreviewService.ShowPreviewAsync(selectedNode);
        }

        private async Task InitializeWebView2()
        {
            try
            {
                var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: _directoriesCreator.WebView2DataPath);
                await WebView2Preview.EnsureCoreWebView2Async(environment);
                WebView2Preview.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                
                WebView2Preview.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "preview.assets",
                    _directoriesCreator.TempPreviewPath,
                    CoreWebView2HostResourceAccessKind.Allow
                );
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "WebView2 initialization failed. Previews will be affected.");
                _customMessageBoxService.ShowError(
                    "Error",
                    "Could not initialize content viewer. Some previews may not work correctly.",
                    Window.GetWindow(this)
                );
            }
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

        private void txtSearchExplorer_TextChanged(object sender, TextChangedEventArgs e)
        {
            // TODO: Implement search logic for the virtual tree
        }
    }
}
