using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Models;
using Microsoft.Win32;
using LeagueToolkit.Core.Wad;
using System.Text;

namespace PBE_AssetsManager.Views
{
    public partial class ExplorerWindow : UserControl
    {
        private readonly LogService _logService;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private readonly HashResolverService _hashResolverService;

        public ObservableCollection<FileSystemNodeModel> RootNodes { get; set; }

        public ExplorerWindow(LogService logService, CustomMessageBoxService customMessageBoxService, HashResolverService hashResolverService)
        {
            InitializeComponent();
            _logService = logService;
            _customMessageBoxService = customMessageBoxService;
            _hashResolverService = hashResolverService;
            RootNodes = new ObservableCollection<FileSystemNodeModel>();
            DataContext = this;
            this.Loaded += ExplorerWindow_Loaded;
            this.Unloaded += ExplorerWindow_Unloaded;
            InitializeWebView2();
        }

        private void ExplorerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FileTreeView.Visibility = Visibility.Collapsed;
            NoDirectoryMessage.Visibility = Visibility.Visible;
        }

        private void ExplorerWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            WebView2Preview.CoreWebView2?.Navigate("about:blank");
        }

        private void SelectPbeDirButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select any file inside the PBE directory",
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
                }
                else
                {
                    _customMessageBoxService.ShowError("Error", "Invalid directory selected.", Window.GetWindow(this));
                }
            }
        }

        private void LoadInitialDirectory(string rootPath)
        {
            NoDirectoryMessage.Visibility = Visibility.Collapsed;
            FileTreeView.Visibility = Visibility.Visible;
            RootNodes.Clear();
            var rootNode = new FileSystemNodeModel(rootPath);
            RootNodes.Add(rootNode);
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
                        await LoadWadFileChildren(node);
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

        private async Task LoadWadFileChildren(FileSystemNodeModel wadNode)
        {
            await _hashResolverService.LoadHashesAsync();
            var rootVirtualNode = new FileSystemNodeModel(wadNode.Name, true, wadNode.FullPath, wadNode.FullPath);

            using var wadFile = new WadFile(wadNode.FullPath);
            foreach (var chunk in wadFile.Chunks.Values)
            {
                string virtualPath = _hashResolverService.ResolveHash(chunk.PathHash);
                if (!string.IsNullOrEmpty(virtualPath) && virtualPath != chunk.PathHash.ToString("x16"))
                {
                    AddNodeToVirtualTree(rootVirtualNode, virtualPath, wadNode.FullPath, chunk.PathHash);
                }
            }

            foreach (var child in rootVirtualNode.Children.OrderBy(c => c.Type == NodeType.VirtualDirectory ? 0 : 1).ThenBy(c => c.Name))
            {
                wadNode.Children.Add(child);
            }
        }

        private void AddNodeToVirtualTree(FileSystemNodeModel root, string virtualPath, string wadPath, ulong chunkHash)
        {
            string[] parts = virtualPath.Replace('\\', '/').Split('/');
            var currentNode = root;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                var dirName = parts[i];
                var childDir = currentNode.Children.FirstOrDefault(c => c.Name.Equals(dirName, StringComparison.OrdinalIgnoreCase) && c.Type == NodeType.VirtualDirectory);
                if (childDir == null)
                {
                    var newVirtualPath = string.Join("/", parts.Take(i + 1));
                    childDir = new FileSystemNodeModel(dirName, true, newVirtualPath, wadPath);
                    currentNode.Children.Add(childDir);
                }
                currentNode = childDir;
            }

            var fileNode = new FileSystemNodeModel(parts.Last(), false, virtualPath, wadPath)
            {
                SourceChunkPathHash = chunkHash
            };
            currentNode.Children.Add(fileNode);
        }

        private async void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedNode = e.NewValue as FileSystemNodeModel;
            if (selectedNode == null || selectedNode.Type == NodeType.RealDirectory || selectedNode.Type == NodeType.VirtualDirectory || selectedNode.Type == NodeType.WadFile)
            {
                ResetPreview();
                return;
            }

            try
            {
                await PreviewWadFile(selectedNode);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Failed to preview file '{selectedNode.FullPath}'.");
                ShowUnsupportedPreview(selectedNode.Extension);
            }
        }

        private async Task PreviewWadFile(FileSystemNodeModel node)
        {
            if (string.IsNullOrEmpty(node.SourceWadPath) || node.SourceChunkPathHash == 0)
            {
                ShowUnsupportedPreview(node.Extension);
                return;
            }

            byte[] decompressedData;
            try
            {
                using var wadFile = new WadFile(node.SourceWadPath);
                var chunk = wadFile.FindChunk(node.SourceChunkPathHash);
                using var decompressedOwner = wadFile.LoadChunkDecompressed(chunk);
                decompressedData = decompressedOwner.Span.ToArray();
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Failed to decompress chunk for preview: {node.FullPath}");
                ShowUnsupportedPreview(node.Extension);
                return;
            }

            var extension = node.Extension;

            if (IsImageExtension(extension)) { ShowImagePreview(decompressedData); }
            else if (IsTextureExtension(extension)) { ShowTexturePreview(decompressedData); }
            else if (IsTextExtension(extension)) { ShowTextPreview(decompressedData); }
            else if (IsAudioExtension(extension)) { ShowAudioVideoPreview(decompressedData, extension); }
            else { ShowUnsupportedPreview(extension); }
        }

        private void ShowImagePreview(byte[] data)
        {
            ResetPreview();
            ImagePreview.Visibility = Visibility.Visible;
            PreviewPlaceholder.Visibility = Visibility.Collapsed;

            using var stream = new MemoryStream(data);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            ImagePreview.Source = bitmap;
        }

        private void ShowTexturePreview(byte[] data)
        {
            ResetPreview();
            ImagePreview.Visibility = Visibility.Visible;
            PreviewPlaceholder.Visibility = Visibility.Collapsed;

            using var stream = new MemoryStream(data);
            var texture = LeagueToolkit.Core.Renderer.Texture.Load(stream);
            if (texture.Mips.Length > 0)
            {
                var mainMip = texture.Mips[0];
                var width = mainMip.Width;
                var height = mainMip.Height;
                if (mainMip.Span.TryGetSpan(out Span<BCnEncoder.Shared.ColorRgba32> pixelSpan))
                {
                    var pixelBytes = System.Runtime.InteropServices.MemoryMarshal.AsBytes(pixelSpan);
                    ImagePreview.Source = BitmapSource.Create(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixelBytes.ToArray(), width * 4);
                }
            }
        }

        private void ShowTextPreview(byte[] data)
        {
            ResetPreview();
            TextPreview.Visibility = Visibility.Visible;
            PreviewPlaceholder.Visibility = Visibility.Collapsed;
            TextPreview.Text = Encoding.UTF8.GetString(data);
        }

        private void ShowAudioVideoPreview(byte[] data, string extension)
        {
            ResetPreview();
            WebView2Preview.Visibility = Visibility.Visible;
            PreviewPlaceholder.Visibility = Visibility.Collapsed;

            var base64Data = Convert.ToBase64String(data);
            var mimeType = extension switch
            {
                ".ogg" => "audio/ogg",
                ".wav" => "audio/wav",
                _ => "application/octet-stream"
            };

            var htmlContent = $"<body style=\"background-color: #282c34; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0;\"><audio controls autoplay style=\"width: 80%;\"><source src=\"data:{mimeType};base64,{base64Data}\" type=\"{mimeType}\"></audio></body>";
            WebView2Preview.CoreWebView2.NavigateToString(htmlContent);
        }

        private void ShowUnsupportedPreview(string extension)
        {
            ResetPreview();
            PreviewPlaceholder.Visibility = Visibility.Visible;
            SelectFileMessagePanel.Visibility = Visibility.Collapsed;
            UnsupportedFileMessagePanel.Visibility = Visibility.Visible;
            UnsupportedFileMessage.Text = $"Preview not available for '{extension}' files.";
        }

        private void ResetPreview()
        {
            PreviewPlaceholder.Visibility = Visibility.Visible;
            SelectFileMessagePanel.Visibility = Visibility.Visible;
            UnsupportedFileMessagePanel.Visibility = Visibility.Collapsed;

            ImagePreview.Visibility = Visibility.Collapsed;
            TextPreview.Visibility = Visibility.Collapsed;
            WebView2Preview.Visibility = Visibility.Collapsed;

            if (WebView2Preview != null && WebView2Preview.CoreWebView2 != null)
            {
                WebView2Preview.CoreWebView2.Navigate("about:blank");
            }
        }

        private bool IsImageExtension(string extension) => extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".bmp" || extension == ".gif";
        private bool IsTextureExtension(string extension) => extension == ".tex" || extension == ".dds";
        private bool IsTextExtension(string extension) => extension == ".json" || extension == ".lua" || extension == ".xml" || extension == ".yml" || extension == ".ini" || extension == ".log" || extension == ".txt";
        private bool IsAudioExtension(string extension) => extension == ".ogg" || extension == ".wav";

        private async void InitializeWebView2()
        {
            try
            {
                var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PBE_AssetsManager", "WebView2Data");
                var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: appDataPath);
                await WebView2Preview.EnsureCoreWebView2Async(environment);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"WebView2 initialization failed. Previews will be affected.");
                _customMessageBoxService.ShowError("Error", "Could not initialize content viewer. Some previews may not work correctly.", Window.GetWindow(this));
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
