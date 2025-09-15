using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using PBE_AssetsManager.Services.Comparator;
using PBE_AssetsManager.Services.Hashes;
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Services.Explorer;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Models;

namespace PBE_AssetsManager.Views.Controls.Explorer
{
    public partial class FileExplorerControl : UserControl
    {
        public event RoutedPropertyChangedEventHandler<object> FileSelected;

        public LogService LogService { get; set; }
        public CustomMessageBoxService CustomMessageBoxService { get; set; }
        public HashResolverService HashResolverService { get; set; }
        public WadNodeLoaderService WadNodeLoaderService { get; set; }
        public WadExtractionService WadExtractionService { get; set; }
        public WadSearchBoxService WadSearchBoxService { get; set; }

        public ObservableCollection<FileSystemNodeModel> RootNodes { get; set; }
        private readonly DispatcherTimer _searchTimer;

        public FileExplorerControl()
        {
            InitializeComponent();
            RootNodes = new ObservableCollection<FileSystemNodeModel>();
            DataContext = this;
            this.Loaded += FileExplorerControl_Loaded;

            _searchTimer = new DispatcherTimer();
            _searchTimer.Interval = TimeSpan.FromMilliseconds(300);
            _searchTimer.Tick += SearchTimer_Tick;
        }

        private async void FileExplorerControl_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = AppSettings.LoadSettings();
            if (!string.IsNullOrEmpty(settings.LolDirectory) && Directory.Exists(settings.LolDirectory))
            {
                await BuildInitialTree(settings.LolDirectory);
            }
            else
            {
                FileTreeView.Visibility = Visibility.Collapsed;
                NoDirectoryMessage.Visibility = Visibility.Visible;
            }
        }

        private async void ExtractSelected_Click(object sender, RoutedEventArgs e)
        {
            if (WadExtractionService == null)
            {
                CustomMessageBoxService.ShowError("Error", "Extraction Service is not available.", Window.GetWindow(this));
                return;
            }

            if (FileTreeView.SelectedItem is not FileSystemNodeModel selectedNode)
            {
                CustomMessageBoxService.ShowInfo("Info", "Please select a file or folder to extract.", Window.GetWindow(this));
                return;
            }

            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Destination Folder"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string destinationPath = dialog.FileName;
                try
                {
                    LogService.Log("Extracting selected files...");
                    await WadExtractionService.ExtractNodeAsync(selectedNode, destinationPath);
                    LogService.LogInteractiveSuccess($"Successfully extracted '{selectedNode.Name}' to '{destinationPath}'.", destinationPath);
                }
                catch (Exception ex)
                {
                    LogService.LogError(ex, $"Failed to extract '{selectedNode.Name}'.");
                    CustomMessageBoxService.ShowError("Error", $"An error occurred during extraction: {ex.Message}", Window.GetWindow(this));
                }
            }
        }

        private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }

        static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        private async void SelectLolDirButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select the League of Legends Directory",
                Filter = "All files (*.*)|*.*",
                CheckFileExists = false,
                ValidateNames = false,
                FileName = "Folder Selection."
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string lolDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                if (Directory.Exists(lolDirectory))
                {
                    await BuildInitialTree(lolDirectory);
                }
                else
                {
                    CustomMessageBoxService.ShowError("Error", "Invalid directory selected.", Window.GetWindow(this));
                }
            }
        }

        private async Task BuildInitialTree(string rootPath)
        {
            NoDirectoryMessage.Visibility = Visibility.Collapsed;
            FileTreeView.Visibility = Visibility.Collapsed;
            LoadingIndicator.Visibility = Visibility.Visible;

            try
            {
                await HashResolverService.LoadHashesAsync();
                await HashResolverService.LoadBinHashesAsync();

                RootNodes.Clear();

                string gamePath = Path.Combine(rootPath, "Game");
                if (Directory.Exists(gamePath))
                {
                    var gameNode = new FileSystemNodeModel(gamePath);
                    RootNodes.Add(gameNode);
                    await LoadAllChildren(gameNode);
                }

                string pluginsPath = Path.Combine(rootPath, "Plugins");
                if (Directory.Exists(pluginsPath))
                {
                    var pluginsNode = new FileSystemNodeModel(pluginsPath);
                    RootNodes.Add(pluginsNode);
                    await LoadAllChildren(pluginsNode);
                }

                if (RootNodes.Count == 0)
                {
                    CustomMessageBoxService.ShowError("Error", "Could not find 'Game' or 'Plugins' subdirectories in the selected path.", Window.GetWindow(this));
                    NoDirectoryMessage.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Failed to build initial tree.");
                CustomMessageBoxService.ShowError("Error", "Could not load the directory. Please check the logs.", Window.GetWindow(this));
                NoDirectoryMessage.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
                FileTreeView.Visibility = Visibility.Visible;
            }
        }

        private async Task LoadAllChildren(FileSystemNodeModel node)
        {
            if (node.Children.Count == 1 && node.Children[0].Name == "Loading...")
            {
                node.Children.Clear();
            }

            if (node.Type == NodeType.WadFile)
            {
                var children = await WadNodeLoaderService.LoadChildrenAsync(node);
                foreach (var child in children)
                {
                    node.Children.Add(child);
                }
                return;
            }

            if (node.Type == NodeType.RealDirectory)
            {
                try
                {
                    var directories = Directory.GetDirectories(node.FullPath);
                    foreach (var dir in directories.OrderBy(d => d))
                    {
                        var childNode = new FileSystemNodeModel(dir);
                        node.Children.Add(childNode);
                        await LoadAllChildren(childNode);
                    }

                    var files = Directory.GetFiles(node.FullPath);
                    foreach (var file in files.OrderBy(f => f))
                    {
                        var childNode = new FileSystemNodeModel(file);
                        node.Children.Add(childNode);
                        await LoadAllChildren(childNode); // This will handle WAD files
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    LogService.LogWarning($"Access denied to: {node.FullPath}");
                }
            }
        }

        private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            FileSelected?.Invoke(this, e);
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
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private async void SearchTimer_Tick(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            string searchText = txtSearchExplorer.Text;

            var selectedNode = FileTreeView.SelectedItem as FileSystemNodeModel;

            await WadSearchBoxService.FilterTreeAsync(RootNodes, searchText);

            if (string.IsNullOrEmpty(searchText) && selectedNode != null)
            {
                // Use the dispatcher to give the UI time to update after the filter is cleared.
                _ = Dispatcher.BeginInvoke(new Action(() =>
                {
                    SelectAndFocusNode(selectedNode);
                }), DispatcherPriority.ContextIdle);
            }
        }

        private void SelectAndFocusNode(FileSystemNodeModel node)
        {
            var path = FindNodePath(RootNodes, node);
            if (path == null) return;

            var container = (ItemsControl)FileTreeView;
            TreeViewItem itemContainer = null;

            // Expand all parent nodes.
            foreach (var parentNode in path)
            {
                if (parentNode == node) break;

                itemContainer = (TreeViewItem)container.ItemContainerGenerator.ContainerFromItem(parentNode);
                if (itemContainer == null)
                {
                    container.UpdateLayout(); // Force the container to be generated
                    itemContainer = (TreeViewItem)container.ItemContainerGenerator.ContainerFromItem(parentNode);
                }

                if (itemContainer == null) return; // Could not create container

                if (!itemContainer.IsExpanded)
                {
                    itemContainer.IsExpanded = true;
                }
                container = itemContainer;
            }

            // Select the final node.
            itemContainer = (TreeViewItem)container.ItemContainerGenerator.ContainerFromItem(node);
            if (itemContainer == null)
            {
                container.UpdateLayout();
                itemContainer = (TreeViewItem)container.ItemContainerGenerator.ContainerFromItem(node);
            }

            if (itemContainer != null)
            {
                itemContainer.IsSelected = true;
                itemContainer.BringIntoView();
                itemContainer.Focus();
            }
        }

        private List<FileSystemNodeModel> FindNodePath(IEnumerable<FileSystemNodeModel> nodes, FileSystemNodeModel nodeToFind)
        {
            foreach (var n in nodes)
            {
                if (n == nodeToFind)
                {
                    return new List<FileSystemNodeModel> { n };
                }

                if (n.Children != null)
                {
                    var path = FindNodePath(n.Children, nodeToFind);
                    if (path != null)
                    {
                        path.Insert(0, n);
                        return path;
                    }
                }
            }
            return null;
        }
    }
}