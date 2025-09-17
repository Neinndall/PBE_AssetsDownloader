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
using AssetsManager.Services.Comparator;
using AssetsManager.Services.Hashes;
using AssetsManager.Services.Core;
using AssetsManager.Services.Explorer;
using AssetsManager.Utils;
using AssetsManager.Views.Models;

namespace AssetsManager.Views.Controls.Explorer
{
    public partial class FileExplorerControl : UserControl
    {
        public event RoutedPropertyChangedEventHandler<object> FileSelected;

        public FilePreviewerControl FilePreviewer { get; set; }

        public MenuItem PinMenuItem => (this.FindResource("ExplorerContextMenu") as ContextMenu)?.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "PinMenuItem");

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
            Toolbar.SearchTextChanged += Toolbar_SearchTextChanged;
            Toolbar.CollapseToContainerClicked += Toolbar_CollapseToContainerClicked;
            Toolbar.FileExplorerControl = this;

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

        private void PinSelected_Click(object sender, RoutedEventArgs e)
        {
            if (FileTreeView.SelectedItem is FileSystemNodeModel selectedNode)
            {
                FilePreviewer?.ViewModel.PinFile(selectedNode);
            }
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (PinMenuItem is not null && FileTreeView.SelectedItem is FileSystemNodeModel selectedNode)
            {
                PinMenuItem.IsEnabled = selectedNode.Type != NodeType.RealDirectory && selectedNode.Type != NodeType.VirtualDirectory;
            }
        }

        private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (SafeVisualUpwardSearch(e.OriginalSource as DependencyObject) is TreeViewItem treeViewItem)
            {
                treeViewItem.IsSelected = true;
                e.Handled = true;
            }
        }

        private static TreeViewItem SafeVisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
            {
                if (source is Visual || source is System.Windows.Media.Media3D.Visual3D)
                {
                    source = VisualTreeHelper.GetParent(source);
                }
                else
                {
                    source = LogicalTreeHelper.GetParent(source);
                }
            }
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
                Toolbar.Visibility = Visibility.Visible;
                ToolbarSeparator.Visibility = Visibility.Visible;
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

        private void Toolbar_SearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void Toolbar_CollapseToContainerClicked(object sender, RoutedEventArgs e)
        {
            var selectedNode = FileTreeView.SelectedItem as FileSystemNodeModel;
            if (selectedNode == null) return;

            var path = FindNodePath(RootNodes, selectedNode);
            if (path == null) return;

            FileSystemNodeModel containerNode = null;
            for (int i = path.Count - 1; i >= 0; i--)
            {
                if (path[i].Type == NodeType.WadFile)
                {
                    containerNode = path[i];
                    break;
                }
            }

            if (containerNode != null)
            {
                containerNode.IsExpanded = false;

                _ = Dispatcher.BeginInvoke(new Action(() =>
                {
                    SelectAndFocusNode(containerNode, false);
                }), DispatcherPriority.ContextIdle);
            }
        }

        private void CollapseAll(FileSystemNodeModel node)
        {
            node.IsExpanded = false;
            if (node.Children == null) return;
            foreach (var child in node.Children)
            {
                CollapseAll(child);
            }
        }

        private async void SearchTimer_Tick(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            string searchText = Toolbar.SearchText;

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

        private void SelectAndFocusNode(FileSystemNodeModel node, bool focusAndSelect = true)
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
                itemContainer.BringIntoView();
                if (focusAndSelect)
                {
                    itemContainer.IsSelected = true;
                    itemContainer.Focus();
                }
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

        public async Task ExpandToPath(string path)
        {
            path = path.Replace("\\", "/").Trim('/'); // Normalize and remove leading/trailing slashes
            if (string.IsNullOrEmpty(path)) return;

            string[] pathComponents = path.Split('/');

            LoadingIndicator.Visibility = Visibility.Visible;

            var targetNode = await FindNodeByPathSuffixAsync(RootNodes, pathComponents);

            LoadingIndicator.Visibility = Visibility.Collapsed;

            if (targetNode != null)
            {
                _ = Dispatcher.BeginInvoke(new Action(() =>
                {
                    SelectAndFocusNode(targetNode, true);
                }), DispatcherPriority.ContextIdle);
            }
            else
            {
                string message = $"Could not find any item matching the path: '{path}'";
                CustomMessageBoxService.ShowWarning("Path Not Found", message, Window.GetWindow(this));
            }
        }

        private async Task<FileSystemNodeModel> FindNodeByPathSuffixAsync(IEnumerable<FileSystemNodeModel> nodes, string[] pathSuffix)
        {
            foreach (var node in nodes)
            {
                // Check if the current node is the start of a potential match.
                if (node.Name.Equals(pathSuffix[0], StringComparison.OrdinalIgnoreCase))
                {
                    FileSystemNodeModel potentialMatch = node;
                    bool match = true;

                    // Look ahead to see if the rest of the path matches.
                    for (int i = 1; i < pathSuffix.Length; i++)
                    {
                        // Ensure children are loaded before checking them.
                        if (potentialMatch.Type == NodeType.WadFile || potentialMatch.Type == NodeType.RealDirectory || potentialMatch.Type == NodeType.VirtualDirectory)
                        {
                            if (potentialMatch.Children.Count == 0 || (potentialMatch.Children.Count == 1 && potentialMatch.Children[0].Name == "Loading..."))
                            {
                                await LoadAllChildren(potentialMatch);
                            }
                        }

                        var nextNode = potentialMatch.Children.FirstOrDefault(c => c.Name.Equals(pathSuffix[i], StringComparison.OrdinalIgnoreCase));
                        if (nextNode != null)
                        {
                            potentialMatch = nextNode;
                        }
                        else
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        return potentialMatch; // Found the full suffix path.
                    }
                }

                // ALWAYS try to search deeper, regardless of the check above.
                if (node.Type == NodeType.WadFile || node.Type == NodeType.RealDirectory || node.Type == NodeType.VirtualDirectory)
                {
                    if (node.Children.Count == 0 || (node.Children.Count == 1 && node.Children[0].Name == "Loading..."))
                    {
                        await LoadAllChildren(node);
                    }
                    var foundInChild = await FindNodeByPathSuffixAsync(node.Children, pathSuffix);
                    if (foundInChild != null)
                    {
                        return foundInChild; // Found it in a descendant.
                    }
                }
            }
            return null;
        }
    }
}