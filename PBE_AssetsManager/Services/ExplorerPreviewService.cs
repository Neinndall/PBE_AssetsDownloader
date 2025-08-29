using PBE_AssetsManager.Views.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using LeagueToolkit.Core.Wad;
using PBE_AssetsManager.Views.Helpers;
using Microsoft.Web.WebView2.Wpf;
using PBE_AssetsManager.Utils;
using LeagueToolkit.Core.Renderer;
using BCnEncoder.Shared;
using System.Runtime.InteropServices;
using System.Windows;
using LeagueToolkit.Core.Meta;
using LeagueToolkit.Core.Meta.Properties;
using System.Text.Json;

namespace PBE_AssetsManager.Services
{
    public class ExplorerPreviewService
    {
        private enum Previewer { None, Image, WebView, Placeholder }
        private Previewer _activePreviewer = Previewer.None;

        // UI Controls - to be initialized via Initialize method
        private Image _imagePreview;
        private WebView2 _webView2Preview;
        private Panel _previewPlaceholder;
        private Panel _selectFileMessagePanel;
        private Panel _unsupportedFileMessagePanel;
        private TextBlock _unsupportedFileMessage;

        // Injected Services
        private readonly LogService _logService;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly HashResolverService _hashResolverService;

        // Constructor for DI
        public ExplorerPreviewService(LogService logService, DirectoriesCreator directoriesCreator, HashResolverService hashResolverService)
        {
            _logService = logService;
            _directoriesCreator = directoriesCreator;
            _hashResolverService = hashResolverService;
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
            else if (IsTextExtension(extension)) { await ShowTextPreviewAsync(data); }
            else if (IsMediaExtension(extension)) { await ShowAudioVideoPreviewAsync(data, extension); }
            else if (IsBinExtension(extension)) { await ShowBinPreviewAsync(data); }
            else { await ShowUnsupportedPreviewAsync(extension); }
        }

        private async Task ShowBinPreviewAsync(byte[] data)
        {
            SetPreviewer(Previewer.WebView);

            try
            {
                string textContent = await Task.Run(() =>
                {
                    try
                    {
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        using var stream = new MemoryStream(data);
                        var binTree = new BinTree(stream);
                        var binDict = BinUtils.ConvertBinTreeToDictionary(binTree, _hashResolverService);
                        return JsonSerializer.Serialize(binDict, options);
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError(ex, "Failed to deserialize .bin file.");
                        return "Error: Could not deserialize .bin file. It may be corrupt or of an unsupported version.";
                    }
                });

                string formattedText = JsonDiffHelper.FormatJson(textContent);
                
                string escapedHtml = System.Net.WebUtility.HtmlEncode(formattedText);
                var htmlPageContent = $@"<!DOCTYPE html><html><head><meta charset=""UTF-8""><style>body {{ background-color: #2D2D30; color: #abb2bf; font-family: Consolas, 'Courier New', monospace; font-size: 14px; margin: 0; }} pre {{ margin: 0; white-space: nowrap; overflow-x: auto; }}</style></head><body><pre>{escapedHtml}</pre></body></html>";

                var tempFileName = "preview.html";
                var tempFilePath = Path.Combine(_directoriesCreator.TempPreviewPath, tempFileName);
                await File.WriteAllTextAsync(tempFilePath, htmlPageContent, Encoding.UTF8);

                var fileUrl = $"https://preview.assets/{tempFileName}";
                await _webView2Preview.EnsureCoreWebView2Async();
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

            if (_activePreviewer == Previewer.WebView && previewer != Previewer.WebView)
            {
                if (_webView2Preview != null && _webView2Preview.CoreWebView2 != null)
                {
                    _webView2Preview.CoreWebView2.NavigateToString("about:blank");
                }
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
            SetPreviewer(Previewer.WebView);
            string svgContent = Encoding.UTF8.GetString(data);
            var htmlContent = $"<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><style>body {{ background-color: #2D2D30; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; padding: 20px; box-sizing: border-box; }} svg {{ max-width: 100%; max-height: 100%; }}</style></head><body>{svgContent}</body></html>";

            try
            {
                await _webView2Preview.EnsureCoreWebView2Async();
                _webView2Preview.CoreWebView2.NavigateToString(htmlContent);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Failed to show SVG preview.");
                await ShowUnsupportedPreviewAsync(".svg");
            }
        }

        private async Task ShowTextPreviewAsync(byte[] data)
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
            var htmlContent = $"<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><style>body {{ background-color: #2D2D30; color: #abb2bf; font-family: Consolas, 'Courier New', monospace; font-size: 14px; }} pre {{ margin: 0; word-wrap: break-word; white-space: pre-wrap; }}</style></head><body><pre>{escapedHtml}</pre></body></html>";

            await _webView2Preview.EnsureCoreWebView2Async();
            _webView2Preview.CoreWebView2.NavigateToString(htmlContent);
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
                var htmlContent = $"<body style=\"background-color: #2D2D30; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0;\"><{{tag}} controls autoplay {extraAttributes} style=\"width: 100%; max-height: 100%;\"><source src=\"{fileUrl}\" type=\"{mimeType}\"></{{tag}}></body>";

                await _webView2Preview.EnsureCoreWebView2Async();
                _webView2Preview.CoreWebView2.NavigateToString(htmlContent);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Failed to create and show preview for {{extension}} file.");
                await ShowUnsupportedPreviewAsync(extension);
            }
        }

        private async Task ShowUnsupportedPreviewAsync(string extension)
        {
            SetPreviewer(Previewer.Placeholder);
            _selectFileMessagePanel.Visibility = Visibility.Visible;
            _unsupportedFileMessagePanel.Visibility = Visibility.Visible;
            _unsupportedFileMessage.Text = $"Preview not available for '{extension}' files.";
            await Task.CompletedTask;
        }

        private bool IsImageExtension(string extension) => extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".bmp" || extension == ".gif";
        private bool IsVectorImageExtension(string extension) => extension == ".svg";
        private bool IsTextureExtension(string extension) => extension == ".tex" || extension == ".dds";
        private bool IsTextExtension(string extension) => extension == ".json" || extension == ".lua" || extension == ".xml" || extension == ".yml" || extension == ".yaml" || extension == ".ini" || extension == ".log" || extension == ".txt";
        private bool IsMediaExtension(string extension) => extension == ".ogg" || extension == ".wav" || extension == ".webm";
        private bool IsBinExtension(string extension) => extension == ".bin";
    }
}