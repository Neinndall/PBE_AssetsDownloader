using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LeagueToolkit.Core.Wad;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Helpers;
using PBE_AssetsManager.Views.Models;

namespace PBE_AssetsManager.Views
{
    public partial class ExplorerWindow : UserControl
    {
        private readonly LogService _logService;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private readonly HashResolverService _hashResolverService;
        private readonly DirectoriesCreator _directoriesCreator;

        private enum Previewer { None, Image, WebView, Placeholder }
        private Previewer _activePreviewer = Previewer.None;

        public ObservableCollection<FileSystemNodeModel> RootNodes { get; set; }

        public ExplorerWindow(LogService logService, CustomMessageBoxService customMessageBoxService, HashResolverService hashResolverService, DirectoriesCreator directoriesCreator)
        {
            InitializeComponent();
            _logService = logService;
            _customMessageBoxService = customMessageBoxService;
            _hashResolverService = hashResolverService;
            _directoriesCreator = directoriesCreator;
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

        private async void LoadInitialDirectory(string rootPath)
        {
            NoDirectoryMessage.Visibility = Visibility.Collapsed;
            FileTreeView.Visibility = Visibility.Visible;

            // Pre-load hashes for this explorer session
            await _hashResolverService.LoadHashesAsync();

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
            var childrenToAdd = await Task.Run(() =>
            {
                var rootVirtualNode = new FileSystemNodeModel(wadNode.Name, true, wadNode.FullPath, wadNode.FullPath);
                using (var wadFile = new WadFile(wadNode.FullPath))
                {
                    foreach (var chunk in wadFile.Chunks.Values)
                    {
                        string virtualPath = _hashResolverService.ResolveHash(chunk.PathHash);
                        if (!string.IsNullOrEmpty(virtualPath) && virtualPath != chunk.PathHash.ToString("x16"))
                        {
                            AddNodeToVirtualTree(rootVirtualNode, virtualPath, wadNode.FullPath, chunk.PathHash);
                        }
                    }
                }
                return rootVirtualNode.Children
                    .OrderBy(c => c.Type == NodeType.VirtualDirectory ? 0 : 1)
                    .ThenBy(c => c.Name)
                    .ToList();
            });

            foreach (var child in childrenToAdd)
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
                await ResetPreview();
                return;
            }

            try
            {
                if (selectedNode.Type == NodeType.VirtualFile)
                {
                    await PreviewWadFile(selectedNode);
                }
                else if (selectedNode.Type == NodeType.RealFile)
                {
                    await PreviewRealFile(selectedNode);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Failed to preview file '{selectedNode.FullPath}'.");
                await ShowUnsupportedPreview(selectedNode.Extension);
            }
        }

        private async Task PreviewRealFile(FileSystemNodeModel node)
        {
            if (!File.Exists(node.FullPath))
            {
                await ShowUnsupportedPreview("File not found");
                return;
            }

            byte[] fileData = await File.ReadAllBytesAsync(node.FullPath);
            var extension = node.Extension;

            if (IsImageExtension(extension)) { await ShowImagePreview(fileData); }
            else if (IsTextureExtension(extension)) { await ShowTexturePreview(fileData); }
            else if (IsVectorImageExtension(extension)) { await ShowSvgPreview(fileData); }
            else if (IsTextExtension(extension)) { await ShowTextPreview(fileData); }
            else if (IsMediaExtension(extension)) { await ShowAudioVideoPreview(fileData, extension); }
            else { await ShowUnsupportedPreview(extension); }
        }

        private async Task PreviewWadFile(FileSystemNodeModel node)
        {
            if (string.IsNullOrEmpty(node.SourceWadPath) || node.SourceChunkPathHash == 0)
            {
                await ShowUnsupportedPreview(node.Extension);
                return;
            }

            byte[] decompressedData;
            try
            {
                decompressedData = await Task.Run(() =>
                {
                    using var wadFile = new WadFile(node.SourceWadPath);
                    var chunk = wadFile.FindChunk(node.SourceChunkPathHash);
                    using var decompressedOwner = wadFile.LoadChunkDecompressed(chunk);
                    return decompressedOwner.Span.ToArray();
                });
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Failed to decompress chunk for preview: {node.FullPath}");
                await ShowUnsupportedPreview(node.Extension);
                return;
            }

            var extension = node.Extension;

            if (IsImageExtension(extension)) { await ShowImagePreview(decompressedData); }
            else if (IsTextureExtension(extension)) { await ShowTexturePreview(decompressedData); }
            else if (IsVectorImageExtension(extension)) { await ShowSvgPreview(decompressedData); }
            else if (IsTextExtension(extension)) { await ShowTextPreview(decompressedData); }
            else if (IsMediaExtension(extension)) { await ShowAudioVideoPreview(decompressedData, extension); }
            else { await ShowUnsupportedPreview(extension); }
        }

        private async Task ShowImagePreview(byte[] data)
        {
            SetPreviewer(Previewer.Image);

            var bitmap = await Task.Run(() =>
            {
                using var stream = new MemoryStream(data);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = stream;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            });
            ImagePreview.Source = bitmap;
        }

        private async Task ShowTexturePreview(byte[] data)
        {
            SetPreviewer(Previewer.Image);

            var bitmapSource = await Task.Run(() =>
            {
                using var stream = new MemoryStream(data);
                var texture = LeagueToolkit.Core.Renderer.Texture.Load(stream);
                if (texture.Mips.Length > 0)
                {
                    var mainMip = texture.Mips[0];
                    var width = mainMip.Width;
                    var height = mainMip.Height;
                    if (mainMip.Span.TryGetSpan(out Span<BCnEncoder.Shared.ColorRgba32> pixelSpan))
                    {
                        var pixelBytes = System.Runtime.InteropServices.MemoryMarshal.AsBytes(pixelSpan).ToArray();
                        var bmp = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixelBytes, width * 4);
                        bmp.Freeze();
                        return bmp;
                    }
                }
                return null;
            });

            if (bitmapSource != null)
            {
                ImagePreview.Source = bitmapSource;
            }
            else
            {
                await ShowUnsupportedPreview(".tex/.dds");
            }
        }

        private async Task ShowSvgPreview(byte[] data)
        {
            SetPreviewer(Previewer.WebView);

            string svgContent = Encoding.UTF8.GetString(data);

            var htmlContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""UTF-8"">
                    <style>
                        body {{ 
                            background-color: #2D2D30; 
                            display: flex; 
                            justify-content: center; 
                            align-items: center; 
                            height: 100vh; 
                            margin: 0; 
                            padding: 20px;
                            box-sizing: border-box;
                        }}
                        svg {{ 
                            max-width: 100%; 
                            max-height: 100%;
                        }}
                    </style>
                </head>
                <body>
                    {svgContent}
                </body>
                </html>
            ";

            try
            {
                await WebView2Preview.EnsureCoreWebView2Async();
                WebView2Preview.CoreWebView2.NavigateToString(htmlContent);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Failed to show SVG preview.");
                await ShowUnsupportedPreview(".svg");
            }
        }

        private async Task ShowTextPreview(byte[] data)
        {
            SetPreviewer(Previewer.WebView);

            string textContent = await Task.Run(() =>
            {
                var rawText = Encoding.UTF8.GetString(data);
                var cleanText = new string(rawText.Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t').ToArray());
                
                const int MaxLength = 500_000;
                if (cleanText.Length > MaxLength)
                {
                    cleanText = cleanText.Substring(0, MaxLength) + "\n\n--- LOG TRUNCATED ---";
                }
                return cleanText;
            });
            
            bool isJson = textContent.TrimStart().StartsWith("{ ") || textContent.TrimStart().StartsWith("[");
            string formattedText = isJson ? JsonDiffHelper.FormatJson(textContent) : textContent;
            string escapedHtml = System.Net.WebUtility.HtmlEncode(formattedText);

            var htmlContent = @$"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""UTF-8"">
                    <style>
                        body {{ background-color: #2D2D30; color: #abb2bf; font-family: Consolas, 'Courier New', monospace; font-size: 14px; }}
                        pre {{ margin: 0; word-wrap: break-word; white-space: pre-wrap; }}
                    </style>
                </head>
                <body>
                    <pre>{escapedHtml}</pre>
                </body>
                </html>
            ";

            await WebView2Preview.EnsureCoreWebView2Async();
            WebView2Preview.CoreWebView2.NavigateToString(htmlContent);
        }

        private async Task ShowAudioVideoPreview(byte[] data, string extension)
        {
            SetPreviewer(Previewer.WebView);

            try
            {
                await Task.Run(() =>
                {
                    foreach (var file in Directory.GetFiles(_directoriesCreator.TempPreviewPath))
                    {
                        File.Delete(file);
                    }
                });

                var tempFileName = "preview" + extension;
                var tempFilePath = Path.Combine(_directoriesCreator.TempPreviewPath, tempFileName);
                await File.WriteAllBytesAsync(tempFilePath, data);

                var mimeType = extension switch
                {
                    ".ogg" => "audio/ogg",
                    ".wav" => "audio/wav",
                    ".webm" => "video/webm",
                    _ => "application/octet-stream"
                };
                string tag = mimeType.StartsWith("video/") ? "video" : "audio";
                string extraAttributes = tag == "video" ? "muted" : "";

                var fileUrl = $"https://preview.assets/{tempFileName}";
                var htmlContent = $"<body style=\"background-color: #2D2D30; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0;\"><{tag} controls autoplay {extraAttributes} style=\"width: 100%; max-height: 100%;\"><source src=\"{fileUrl}\" type=\"{mimeType}\"></{tag}>";
                
                await WebView2Preview.EnsureCoreWebView2Async();
                WebView2Preview.CoreWebView2.NavigateToString(htmlContent);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Failed to create and show preview for {extension} file.");
                await ShowUnsupportedPreview(extension);
            }
        }

        private async Task ShowUnsupportedPreview(string extension)
        {
            SetPreviewer(Previewer.Placeholder);
            SelectFileMessagePanel.Visibility = Visibility.Collapsed;
            UnsupportedFileMessagePanel.Visibility = Visibility.Visible;
            UnsupportedFileMessage.Text = $"Preview not available for '{extension}' files.";
            await Task.CompletedTask;
        }

        private void SetPreviewer(Previewer previewer)
        {
            if (_activePreviewer == previewer) return;

            if (_activePreviewer == Previewer.WebView && previewer != Previewer.WebView)
            {
                if (WebView2Preview != null && WebView2Preview.CoreWebView2 != null)
                {
                    WebView2Preview.CoreWebView2.NavigateToString("about:blank");
                }
            }

            ImagePreview.Visibility = previewer == Previewer.Image ? Visibility.Visible : Visibility.Collapsed;
            WebView2Preview.Visibility = previewer == Previewer.WebView ? Visibility.Visible : Visibility.Collapsed;
            PreviewPlaceholder.Visibility = previewer == Previewer.Placeholder ? Visibility.Visible : Visibility.Collapsed;

            _activePreviewer = previewer;
        }

        private async Task ResetPreview()
        {
            SetPreviewer(Previewer.Placeholder);
            SelectFileMessagePanel.Visibility = Visibility.Visible;
            UnsupportedFileMessagePanel.Visibility = Visibility.Collapsed;
            await Task.CompletedTask;
        }

        private bool IsImageExtension(string extension) => extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".bmp" || extension == ".gif";
        private bool IsVectorImageExtension(string extension) => extension == ".svg";
        private bool IsTextureExtension(string extension) => extension == ".tex" || extension == ".dds";
        private bool IsTextExtension(string extension) => extension == ".json" || extension == ".lua" || extension == ".xml" || extension == ".yml" || extension == ".yaml" || extension == ".ini" || extension == ".log" || extension == ".txt";
        private bool IsMediaExtension(string extension) => extension == ".ogg" || extension == ".wav" || extension == ".webm";

        private async Task InitializeWebView2()
        {
            try
            {
                // Crea un entorno personalizado (si necesitas almacenar datos)
                var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: _directoriesCreator.WebView2DataPath);

                // Inicializa WebView2
                await WebView2Preview.EnsureCoreWebView2Async(environment);
                WebView2Preview.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                
                // Configura host virtual para recursos locales
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