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
using PBE_AssetsManager.Views.Models;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Helpers;

namespace PBE_AssetsManager.Services
{
    public class ExplorerPreviewService
    {
        private enum Previewer { None, Image, WebView, Placeholder }
        private Previewer _activePreviewer = Previewer.None;

        private Image _imagePreview;
        private WebView2 _webView2Preview;
        private Panel _previewPlaceholder;
        private Panel _selectFileMessagePanel;
        private Panel _unsupportedFileMessagePanel;
        private TextBlock _unsupportedFileMessage;

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
        public void Initialize(
            Image imagePreview,
            WebView2 webView2Preview,
            Panel previewPlaceholder,
            Panel selectFileMessagePanel,
            Panel unsupportedFileMessagePanel,
            TextBlock unsupportedFileMessage)
        {
            _imagePreview = imagePreview;
            _webView2Preview = webView2Preview;
            _previewPlaceholder = previewPlaceholder;
            _selectFileMessagePanel = selectFileMessagePanel;
            _unsupportedFileMessagePanel = unsupportedFileMessagePanel;
            _unsupportedFileMessage = unsupportedFileMessage;
        }

        public async Task ShowPreviewAsync(FileSystemNodeModel node)
        {
            if (node == null || node.Type == NodeType.RealDirectory || node.Type == NodeType.VirtualDirectory || node.Type == NodeType.WadFile)
            {
                await ResetPreviewAsync(); // This will show _selectFileMessagePanel
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
                await ShowUnsupportedPreviewAsync(node.Extension); // This will show _unsupportedFileMessagePanel
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
            else if (IsTextExtension(extension)) { await ShowTextPreviewAsync(data, extension); }
            else if (IsMediaExtension(extension)) { await ShowAudioVideoPreviewAsync(data, extension); }
            else if (IsBinExtension(extension)) { await ShowBinPreviewAsync(data); }
            else { await ShowUnsupportedPreviewAsync(extension); }
        }

        private const string CustomScrollbarCss = @"
            ::-webkit-scrollbar {
                width: 12px;
            }
            ::-webkit-scrollbar-track {
                background: #252526;
            }
            ::-webkit-scrollbar-thumb {
                background-color: #505050;
                border-radius: 6px;
                border: 3px solid #252526;
            }
            ::-webkit-scrollbar-thumb:hover {
                background-color: #707070;
            }";

        private async Task ShowBinPreviewAsync(byte[] data)
        {
            try
            {
                string htmlContent = await Task.Run(async () =>
                {
                    try
                    {
                        using var stream = new MemoryStream(data);
                        var binTree = new BinTree(stream);
                        var binDict = BinUtils.ConvertBinTreeToDictionary(binTree, _hashResolverService);
                        var formattedJson = await JsonDiffHelper.FormatJsonAsync(binDict);
                        
                        var escapedHtml = System.Net.WebUtility.HtmlEncode(formattedJson);
                        return $"<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><style>body {{ background-color: #252526; color: #abb2bf; font-family: Consolas, 'Courier New', monospace; font-size: 14px; margin: 0; padding: 10px; }} pre {{ margin: 0; white-space: pre-wrap; word-wrap: break-word; overflow-wrap: break-word; line-height: 1.4; }} {CustomScrollbarCss}</style></head><body><pre>{escapedHtml}</pre></body></html>";
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError(ex, "Failed to deserialize .bin file.");
                        return "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><style>body { background-color: #252526; color: #f44747; font-family: Consolas; padding: 20px; }</style></head><body>Error: Could not deserialize .bin file. It may be corrupt or of an unsupported version.</body></html>";
                    }
                });

                var tempFileName = "preview.html";
                var tempFilePath = Path.Combine(_directoriesCreator.TempPreviewPath, tempFileName);
                await File.WriteAllTextAsync(tempFilePath, htmlContent, Encoding.UTF8);

                await _webView2Preview.EnsureCoreWebView2Async();
                SetPreviewer(Previewer.WebView);
                
                var fileUrl = new Uri(tempFilePath).AbsoluteUri;
                _webView2Preview.CoreWebView2.Navigate(fileUrl);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Failed to show .bin preview.");
                await ShowUnsupportedPreviewAsync(".bin");
            }
        }

        private void SetPreviewer(Previewer previewer)
        {
            if (_activePreviewer == previewer) return;

            // Solo ocultar WebView2 si cambiamos DESDE WebView a otro previewer
            if (_activePreviewer == Previewer.WebView && previewer != Previewer.WebView && _webView2Preview?.CoreWebView2 != null)
            {
                _webView2Preview.Visibility = Visibility.Collapsed;
            }

            _imagePreview.Visibility = previewer == Previewer.Image ? Visibility.Visible : Visibility.Collapsed;
            _webView2Preview.Visibility = previewer == Previewer.WebView ? Visibility.Visible : Visibility.Collapsed;
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
                var htmlContent = $"<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><style>body {{ background-color: #252526; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; padding: 20px; box-sizing: border-box; }} svg {{ width: 90%; height: 90%; object-fit: contain; }} {CustomScrollbarCss}</style></head><body>{svgContent}</body></html>";
                
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

        private async Task ShowTextPreviewAsync(byte[] data, string extension)
        {
            try
            {
                string htmlContent = await Task.Run(async () =>
                {
                    try
                    {
                        string textContent = Encoding.UTF8.GetString(data);
                        string formattedText = textContent;

                        if (extension == ".js")
                        {
                            formattedText = await _jsBeautifierService.BeautifyAsync(textContent);
                        }
                        else
                        {
                            formattedText = await JsonDiffHelper.FormatJsonAsync(textContent);
                        }

                        var escapedHtml = System.Net.WebUtility.HtmlEncode(formattedText);
                        return $"<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><style>body {{ background-color: #252526; color: #abb2bf; font-family: Consolas, 'Courier New', monospace; font-size: 14px; margin: 0; padding: 10px; }} pre {{ margin: 0; word-wrap: break-word; white-space: pre-wrap; line-height: 1.4; }} {CustomScrollbarCss}</style></head><body><pre>{escapedHtml}</pre></body></html>";
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError(ex, $"Failed to process text for extension {extension}");
                        return $"<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><style>body {{ background-color: #252526; color: #f44747; font-family: Consolas; padding: 20px; }}</style></head><body>Error processing {extension} file.</body></html>";
                    }
                });

                var tempFileName = "preview.html";
                var tempFilePath = Path.Combine(_directoriesCreator.TempPreviewPath, tempFileName);
                await File.WriteAllTextAsync(tempFilePath, htmlContent, Encoding.UTF8);

                await _webView2Preview.EnsureCoreWebView2Async();
                SetPreviewer(Previewer.WebView);
                
                var fileUrl = new Uri(tempFilePath).AbsoluteUri;
                _webView2Preview.CoreWebView2.Navigate(fileUrl);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Failed to show text preview for extension {extension}");
                await ShowUnsupportedPreviewAsync(extension);
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
                var htmlContent = $"<body style=\"background-color: #252526; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0;\"><{tag} controls autoplay {extraAttributes} style=\"width:{(tag == "audio" ? "300px" : "100%")}; height:{(tag == "audio" ? "80px" : "100%")};\"><source src=\"{fileUrl}\" type=\"{mimeType}\"></{tag}></body>";

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
            _unsupportedFileMessage.Text = $"Preview not available for '{extension}' files.";
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
        private bool IsTextExtension(string extension) => extension == ".json" || extension == ".lua" || extension == ".xml" || extension == ".yml" || extension == ".yaml" || extension == ".ini" || extension == ".log" || extension == ".txt" || extension == ".js";
        private bool IsMediaExtension(string extension) => extension == ".ogg" || extension == ".wav" || extension == ".webm";
        private bool IsBinExtension(string extension) => extension == ".bin";
    }
}