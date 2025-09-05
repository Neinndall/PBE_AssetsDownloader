using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PBE_AssetsManager.Services.Comparator;
using PBE_AssetsManager.Services.Hashes;
using PBE_AssetsManager.Services;
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

        public ObservableCollection<FileSystemNodeModel> RootNodes { get; set; }

        public FileExplorerControl()
        {
            InitializeComponent();
            RootNodes = new ObservableCollection<FileSystemNodeModel>();
            DataContext = this;
            this.Loaded += FileExplorerControl_Loaded;
        }

        private void FileExplorerControl_Loaded(object sender, RoutedEventArgs e)
        {
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
                    CustomMessageBoxService.ShowError("Error", "Invalid directory selected.", Window.GetWindow(this));
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
                await HashResolverService.LoadHashesAsync();
                await HashResolverService.LoadBinHashesAsync();

                RootNodes.Clear();

                string gamePath = Path.Combine(rootPath, "Game");
                string pluginsPath = Path.Combine(rootPath, "Plugins");

                bool directoryFound = false;
                if (Directory.Exists(gamePath))
                {
                    RootNodes.Add(new FileSystemNodeModel(gamePath));
                    directoryFound = true;
                }
                if (Directory.Exists(pluginsPath))
                {
                    RootNodes.Add(new FileSystemNodeModel(pluginsPath));
                    directoryFound = true;
                }

                if (!directoryFound)
                {
                    CustomMessageBoxService.ShowError("Error", "Could not find 'Game' or 'Plugins' subdirectories in the selected path.", Window.GetWindow(this));
                    NoDirectoryMessage.Visibility = Visibility.Visible;
                }
                else
                {
                    FileTreeView.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Failed to load initial directory or hashes.");
                CustomMessageBoxService.ShowError("Error", "Could not load the directory. Please check the logs.", Window.GetWindow(this));
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
                        var children = await WadNodeLoaderService.LoadChildrenAsync(node);
                        foreach (var child in children)
                        {
                            node.Children.Add(child);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, $"Failed to expand node: {node.FullPath}");
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
                LogService.LogWarning($"Access denied to: {parent.FullPath}");
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
            string searchText = txtSearchExplorer.Text;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                SetVisibility(RootNodes, true);
                return;
            }

            FilterTree(RootNodes, searchText, false);
        }

        private bool FilterTree(IEnumerable<FileSystemNodeModel> nodes, string searchText, bool parentMatched)
        {
            bool somethingVisibleInThisLevel = false;
            if (nodes == null) return false;

            foreach (var node in nodes)
            {
                if (node.Name == "Loading...")
                {
                    if (parentMatched)
                    {
                        node.IsVisible = true;
                        somethingVisibleInThisLevel = true;
                    }
                    continue;
                }

                bool selfMatches = node.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;

                if (parentMatched || selfMatches)
                {
                    node.IsVisible = true;
                    somethingVisibleInThisLevel = true;
                    FilterTree(node.Children, searchText, true);
                }
                else
                {
                    bool childMatches = FilterTree(node.Children, searchText, false);
                    node.IsVisible = childMatches;
                    if (childMatches)
                    { 
                        node.IsExpanded = true;
                        somethingVisibleInThisLevel = true;
                    }
                }
            }
            return somethingVisibleInThisLevel;
        }

        private void SetVisibility(IEnumerable<FileSystemNodeModel> nodes, bool isVisible)
        {
            foreach (var node in nodes)
            {
                node.IsVisible = isVisible;
                SetVisibility(node.Children, isVisible);
            }
        }
    }
}