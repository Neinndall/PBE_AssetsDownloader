using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PBE_AssetsManager.Services.Comparator;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using PBE_AssetsManager.Views.Models;
using PBE_AssetsManager.Views.Dialogs;
using PBE_AssetsManager.Views.Helpers;
using PBE_AssetsManager.Services.Core;

namespace PBE_AssetsManager.Services.Monitor
{
    public class DiffViewService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly WadDifferenceService _wadDifferenceService;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private readonly LogService _logService;

        private static readonly string[] SupportedImageExtensions = { ".png", ".dds", ".tga", ".jpg", ".jpeg", ".bmp", ".gif", ".ico", ".tif", ".tiff", ".webp", ".tex" };
        private static readonly string[] SupportedTextExtensions = { ".json", ".js", ".txt", ".xml", ".yaml", ".html", ".ini", ".log", ".glsl", ".vert", ".frag", ".tes", ".bak", ".py", ".lua", ".scd", ".skl", ".wgeo", ".sco", ".ann", ".map" };

        public DiffViewService(IServiceProvider serviceProvider, WadDifferenceService wadDifferenceService, CustomMessageBoxService customMessageBoxService, LogService logService)
        {
            _serviceProvider = serviceProvider;
            _wadDifferenceService = wadDifferenceService;
            _customMessageBoxService = customMessageBoxService;
            _logService = logService;
        }

        public async Task ShowWadDiffAsync(SerializableChunkDiff diff, string oldPbePath, string newPbePath, System.Windows.Window owner)
        {
            if (diff == null) return;

            var pathForCheck = diff.NewPath ?? diff.OldPath;
            if (!IsDiffSupported(pathForCheck))
            {
                _customMessageBoxService.ShowInfo("Info", "This file type cannot be displayed in the difference viewer.", owner);
                return;
            }

            string extension = Path.GetExtension(pathForCheck).ToLowerInvariant();
            if (SupportedImageExtensions.Contains(extension))
            {
                await HandleImageDiffAsync(diff, oldPbePath, newPbePath, owner);
                return;
            }

            try
            {
                var (dataType, oldData, newData, oldPath, newPath) = await _wadDifferenceService.PrepareDifferenceDataAsync(diff, oldPbePath, newPbePath);
                var (oldText, newText) = await ProcessDataAsync(dataType, oldData, newData);

                if (oldText == newText)
                {
                    _customMessageBoxService.ShowInfo("Info", "No differences found. The two files are identical.", owner);
                    return;
                }

                var diffWindow = _serviceProvider.GetRequiredService<JsonDiffWindow>();
                diffWindow.Owner = owner;
                await diffWindow.LoadAndDisplayDiffAsync(oldText, newText, oldPath, newPath);
                diffWindow.Show();
            }
            catch (Exception ex)
            {
                _customMessageBoxService.ShowError("Comparison Error", $"An unexpected error occurred while preparing the file for comparison. Details: {ex.Message}", owner);
                _logService.LogError(ex, "Error showing WAD diff");
            }
        }
        
        private async Task HandleImageDiffAsync(SerializableChunkDiff diff, string oldPbePath, string newPbePath, System.Windows.Window owner)
        {
            try
            {
                var (dataType, oldData, newData, oldPath, newPath) = await _wadDifferenceService.PrepareDifferenceDataAsync(diff, oldPbePath, newPbePath);
                if (dataType == "image")
                {
                    var imageDiffWindow = new ImageDiffWindow((BitmapSource)oldData, (BitmapSource)newData, oldPath, newPath) { Owner = owner };
                    imageDiffWindow.Show();
                }
                else
                {
                    _customMessageBoxService.ShowError("Error", "Expected an image but received a different file type.", owner);
                }
            }
            catch (Exception ex)
            {
                _customMessageBoxService.ShowError("Image Comparison Error", $"An unexpected error occurred while preparing the image for comparison. Details: {ex.Message}", owner);
            }
        }

        public async Task ShowFileDiffAsync(string oldFilePath, string newFilePath, System.Windows.Window owner)
        {
            if (!File.Exists(oldFilePath) && !File.Exists(newFilePath))
            {
                _customMessageBoxService.ShowError("Error", "Neither of the files to compare exist.", owner);
                return;
            }

            string extension = Path.GetExtension(newFilePath ?? oldFilePath).ToLowerInvariant();
            if (SupportedImageExtensions.Contains(extension))
            {
                _customMessageBoxService.ShowInfo("Info", "Image comparison for local files is not implemented yet.", owner);
                return;
            }

            try
            {
                var (dataType, oldData, newData) = await _wadDifferenceService.PrepareFileDifferenceDataAsync(oldFilePath, newFilePath);
                var (oldText, newText) = await ProcessDataAsync(dataType, oldData, newData);

                if (oldText == newText)
                {
                    _customMessageBoxService.ShowInfo("Info", "No differences found. The two files are identical.", owner);
                    return;
                }
                
                var diffWindow = _serviceProvider.GetRequiredService<JsonDiffWindow>();
                diffWindow.Owner = owner;
                await diffWindow.LoadAndDisplayDiffAsync(oldText, newText, Path.GetFileName(oldFilePath), Path.GetFileName(newFilePath));
                diffWindow.Show();
            }
            catch (Exception ex)
            {
                _customMessageBoxService.ShowError("Comparison Error", $"An unexpected error occurred while preparing the file for comparison. Details: {ex.Message}", owner);
                _logService.LogError(ex, "Error showing file diff");
            }
        }

        private async Task<(string oldText, string newText)> ProcessDataAsync(string dataType, object oldData, object newData)
        {
            string oldText = string.Empty;
            string newText = string.Empty;

            switch (dataType)
            {
                case "bin":
                    if (oldData != null) oldText = await JsonDiffHelper.FormatJsonAsync(oldData);
                    if (newData != null) newText = await JsonDiffHelper.FormatJsonAsync(newData);
                    break;
                case "js":
                    var jsBeautifier = _serviceProvider.GetRequiredService<JsBeautifierService>();
                    try
                    {
                        if (oldData != null) oldText = await jsBeautifier.BeautifyAsync((string)oldData);
                        if (newData != null) newText = await jsBeautifier.BeautifyAsync((string)newData);
                    }
                    catch (Exception ex)
                    {
                        _logService.LogWarning($"JS Beautifier failed: {ex.Message}");
                        oldText = (string)oldData ?? string.Empty;
                        newText = (string)newData ?? string.Empty;
                    }
                    break;
                case "json":
                    if (oldData != null) oldText = await JsonDiffHelper.FormatJsonAsync(oldData);
                    if (newData != null) newText = await JsonDiffHelper.FormatJsonAsync(newData);
                    break;
                case "text":
                    oldText = (string)oldData ?? string.Empty;
                    newText = (string)newData ?? string.Empty;
                    break;
                default:
                    oldText = oldData?.ToString() ?? string.Empty;
                    newText = newData?.ToString() ?? string.Empty;
                    break;
            }
            return (oldText, newText);
        }

        private bool IsDiffSupported(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (SupportedImageExtensions.Contains(extension)) return true;
            if (SupportedTextExtensions.Contains(extension)) return true;
            if (extension == ".bin") return true;

            return false;
        }
    }
}
