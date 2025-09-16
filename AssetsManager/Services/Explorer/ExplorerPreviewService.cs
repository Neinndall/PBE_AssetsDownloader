using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using LeagueToolkit.Core.Wad;
using Microsoft.Web.WebView2.Wpf;
using LeagueToolkit.Core.Renderer;
using BCnEncoder.Shared;
using System.Runtime.InteropServices;
using System.Windows;
using LeagueToolkit.Core.Meta;
using LeagueToolkit.Core.Meta.Properties;
using System.Text.Json;
using AssetsManager.Views.Models;
using AssetsManager.Utils;
using AssetsManager.Services.Hashes;
using AssetsManager.Views.Helpers;
using AssetsManager.Services.Core;
using AssetsManager.Services.Monitor;
using System.Reflection;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;

namespace AssetsManager.Services.Explorer
{
    public class ExplorerPreviewService
    {
        private enum Previewer { None, Image, WebView, AvalonEdit, Placeholder }
        private Previewer _activePreviewer = Previewer.None;

        private Image _imagePreview;
        private WebView2 _webView2Preview;
        private TextEditor _textEditorPreview;
        private Panel _previewPlaceholder;
        private Panel _selectFileMessagePanel;
        private Panel _unsupportedFileMessagePanel;
        private TextBlock _unsupportedFileTextBlock;

        private readonly LogService _logService;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly HashResolverService _hashResolverService;
        private readonly JsBeautifierService _jsBeautifierService;

        // Constructor for DI
        public ExplorerPreviewService(LogService logService, DirectoriesCreator directoriesCreator, HashResolverService hashResolverService, JsBeautifierService jsBeautifierService)
        {
            _logService = logService;
            _directoriesCreator = directoriesCreator;
            _hashResolverService = hashResolverService;
            _jsBeautifierService = jsBeautifierService;
        }

        // Method to initialize UI-dependent components
        public void Initialize(Image imagePreview, WebView2 webView2Preview, TextEditor textEditor, Panel placeholder, Panel selectFileMessage, Panel unsupportedFileMessage, TextBlock unsupportedFileTextBlock)
        {
            _imagePreview = imagePreview;
            _webView2Preview = webView2Preview;
            _textEditorPreview = textEditor;
            _previewPlaceholder = placeholder;
            _selectFileMessagePanel = selectFileMessage;
            _unsupportedFileMessagePanel = unsupportedFileMessage;
            _unsupportedFileTextBlock = unsupportedFileTextBlock;
        }

        public async Task ShowPreviewAsync(FileSystemNodeModel node)
        {
            if (node == null || node.Type == NodeType.RealDirectory || node.Type == NodeType.VirtualDirectory || node.Type == NodeType.WadFile)
            {
                await ResetPreviewAsync();
                return;
            }

            try
            {
                if (node.Type == NodeType.VirtualFile)
                {
                    await PreviewWadFile(node);
                }
                else if (node.Type == NodeType.RealFile)
                {
                    await PreviewRealFile(node);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Failed to preview file '{node.FullPath}'.");
                await ShowUnsupportedPreviewAsync(node.Extension);
            }
        }

        public async Task ResetPreviewAsync()
        {
            SetPreviewer(Previewer.Placeholder);
            _selectFileMessagePanel.Visibility = Visibility.Visible;
            _unsupportedFileMessagePanel.Visibility = Visibility.Collapsed;
            await Task.CompletedTask;
        }

        private async Task PreviewRealFile(FileSystemNodeModel node)
        {
            if (!File.Exists(node.FullPath))
            {
                await ShowUnsupportedPreviewAsync("File not found");
                return;
            }

            byte[] fileData = await File.ReadAllBytesAsync(node.FullPath);
            await DispatchPreview(fileData, node.Extension);
        }

        private async Task PreviewWadFile(FileSystemNodeModel node)
        {
            if (string.IsNullOrEmpty(node.SourceWadPath) || node.SourceChunkPathHash == 0)
            {
                await ShowUnsupportedPreviewAsync(node.Extension);
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
                await ShowUnsupportedPreviewAsync(node.Extension);
                return;
            }

            await DispatchPreview(decompressedData, node.Extension);
        }

        private async Task DispatchPreview(byte[] data, string extension)
        {
            if (IsImageExtension(extension)) { await ShowImagePreviewAsync(data); }
            else if (IsTextureExtension(extension)) { await ShowTexturePreviewAsync(data); }
            else if (IsVectorImageExtension(extension)) { await ShowSvgPreviewAsync(data); }
            else if (IsJavaScriptExtension(extension)) { await ShowAvalonEditTextPreviewAsync(data, extension); }
            else if (IsTextExtension(extension) || IsBinExtension(extension)) { await ShowAvalonEditTextPreviewAsync(data, extension); }
            else if (IsMediaExtension(extension)) { await ShowAudioVideoPreviewAsync(data, extension); }
            else { await ShowUnsupportedPreviewAsync(extension); }
        }

        private async Task ShowAvalonEditTextPreviewAsync(byte[] data, string extension)
        {
            SetPreviewer(Previewer.AvalonEdit);
            try
            {
                string textContent = string.Empty;

                if (IsBinExtension(extension))
                {
                    using var stream = new MemoryStream(data);
                    var binTree = new BinTree(stream);
                    var binDict = BinUtils.ConvertBinTreeToDictionary(binTree, _hashResolverService);
                    textContent = await JsonDiffHelper.FormatJsonAsync(binDict);
                }
                else
                {
                    textContent = Encoding.UTF8.GetString(data);
                    if (extension == ".js")
                    {
                        textContent = await _jsBeautifierService.BeautifyAsync(textContent);
                    }
                    else
                    {
                        textContent = await JsonDiffHelper.FormatJsonAsync(textContent);
                    }
                }

                if (extension == ".json" || IsBinExtension(extension) || IsJavaScriptExtension(extension))
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = "AssetsManager.Resources.JsonSyntaxHighlighting.xshd";
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            using (var reader = new XmlTextReader(stream))
                            {
                                _textEditorPreview.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                            }
                        }
                        else
                        {
                            _textEditorPreview.SyntaxHighlighting = null;
                        }
                    }
                }
                else
                {
                    _textEditorPreview.SyntaxHighlighting = null;
                }

                _textEditorPreview.Text = textContent;
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Failed to show text preview for extension {extension}");
                _textEditorPreview.Text = $"Error showing {extension} file.";
            }
        }

        private void SetPreviewer(Previewer previewer)
        {
            if (_activePreviewer == previewer) return;

            if (_activePreviewer == Previewer.WebView && previewer != Previewer.WebView && _webView2Preview?.CoreWebView2 != null)
            {
                _webView2Preview.Visibility = Visibility.Collapsed;
            }

            _imagePreview.Visibility = previewer == Previewer.Image ? Visibility.Visible : Visibility.Collapsed;
            _webView2Preview.Visibility = previewer == Previewer.WebView ? Visibility.Visible : Visibility.Collapsed;
            _textEditorPreview.Visibility = previewer == Previewer.AvalonEdit ? Visibility.Visible : Visibility.Collapsed;
            _previewPlaceholder.Visibility = previewer == Previewer.Placeholder ? Visibility.Visible : Visibility.Collapsed;

            _activePreviewer = previewer;
        }

        private async Task ShowImagePreviewAsync(byte[] data)
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
            _imagePreview.Source = bitmap;
        }

        private async Task ShowTexturePreviewAsync(byte[] data)
        {
            SetPreviewer(Previewer.Image);
            var bitmapSource = await Task.Run(() =>
            {
                using var stream = new MemoryStream(data);
                var texture = Texture.Load(stream);
                if (texture.Mips.Length > 0)
                {
                    var mainMip = texture.Mips[0];
                    var width = mainMip.Width;
                    var height = mainMip.Height;
                    if (mainMip.Span.TryGetSpan(out Span<ColorRgba32> pixelSpan))
                    {
                        var pixelBytes = MemoryMarshal.AsBytes(pixelSpan).ToArray();
                        var bmp = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixelBytes, width * 4);
                        bmp.Freeze();
                        return bmp;
                    }
                }
                return null;
            });

            if (bitmapSource != null)
            {
                _imagePreview.Source = bitmapSource;
            }
            else
            {
                await ShowUnsupportedPreviewAsync(".tex/.dds");
            }
        }

        private async Task ShowSvgPreviewAsync(byte[] data)
        {
            try
            {
                string svgContent = Encoding.UTF8.GetString(data);
                var htmlContent = $"<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><style>body {{ background-color: transparent; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; padding: 20px; box-sizing: border-box; }} svg {{ width: 90%; height: 90%; object-fit: contain; }}</style></head><body>{svgContent}</body></html>";
                
                await _webView2Preview.EnsureCoreWebView2Async();
                SetPreviewer(Previewer.WebView);
                _webView2Preview.CoreWebView2.NavigateToString(htmlContent);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Failed to show SVG preview.");
                await ShowUnsupportedPreviewAsync(".svg");
            }
        }

        private async Task ShowAudioVideoPreviewAsync(byte[] data, string extension)
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
                var htmlContent = $"<body style=\"background-color: transparent; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0;\"><{tag} controls autoplay {extraAttributes} style=\"width:{(tag == "audio" ? "300px" : "100%")}; height:{(tag == "audio" ? "80px" : "100%")};\"><source src=\"{fileUrl}\" type=\"{mimeType}\"></{tag}></body>";

                await _webView2Preview.EnsureCoreWebView2Async();
                _webView2Preview.CoreWebView2.NavigateToString(htmlContent);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Failed to create and show preview for {extension} file.");
                await ShowUnsupportedPreviewAsync(extension);
            }
        }

        private async Task ShowUnsupportedPreviewAsync(string extension)
        {
            SetPreviewer(Previewer.Placeholder);
            _selectFileMessagePanel.Visibility = Visibility.Collapsed;
            _unsupportedFileMessagePanel.Visibility = Visibility.Visible;
            _unsupportedFileTextBlock.Text = $"Preview not available for '{extension}' files.";
            await Task.CompletedTask;
        }

        // Método auxiliar para limpiar WebView2 cuando sea necesario
        public void CleanupWebView()
        {
            if (_webView2Preview?.CoreWebView2 != null)
            {
                try
                {
                    _webView2Preview.CoreWebView2.NavigateToString("about:blank");
                }
                catch { /* Ignorar errores de limpieza */ }
            }
        }

        private bool IsImageExtension(string extension) => extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".bmp" || extension == ".gif";
        private bool IsVectorImageExtension(string extension) => extension == ".svg";
        private bool IsTextureExtension(string extension) => extension == ".tex" || extension == ".dds";
        private bool IsTextExtension(string extension) => extension == ".json" || extension == ".lua" || extension == ".xml" || extension == ".yml" || extension == ".yaml" || extension == ".ini" || extension == ".log" || extension == ".txt";
        private bool IsJavaScriptExtension(string extension) => extension == ".js";
        private bool IsMediaExtension(string extension) => extension == ".ogg" || extension == ".wav" || extension == ".webm";
        private bool IsBinExtension(string extension) => extension == ".bin";
    }
}