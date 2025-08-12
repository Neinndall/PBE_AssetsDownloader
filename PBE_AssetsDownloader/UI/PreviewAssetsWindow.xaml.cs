using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PBE_AssetsDownloader.Info;
using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.UI.Dialogs;

namespace PBE_AssetsDownloader.UI
{
    public partial class PreviewAssetsWindow : Window
    {
        #region Fields
        private string _inputFolder;
        private List<string> _selectedAssetTypes;
        private readonly LogService _logService;
        private readonly AssetDownloader _assetDownloader;
        private readonly AssetsPreview _assetsPreview;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private List<AssetInfo> _allAssetsToPreview;
        public ObservableCollection<object> DisplayedAssets { get; set; }
        #endregion

        #region Constructor
        public PreviewAssetsWindow(LogService logService, AssetDownloader assetDownloader, AssetsPreview assetsPreview, CustomMessageBoxService customMessageBoxService)
        {
            InitializeComponent();
            DataContext = this;

            _logService = logService;
            _assetDownloader = assetDownloader;
            _assetsPreview = assetsPreview;
            _customMessageBoxService = customMessageBoxService;

            _allAssetsToPreview = new List<AssetInfo>();
            DisplayedAssets = new ObservableCollection<object>();
        }
        #endregion

        public void InitializeData(string inputFolder, List<string> selectedAssetTypes, Func<IEnumerable<string>, List<string>, List<string>> filterAssetsByType)
        {
            _inputFolder = inputFolder;
            _selectedAssetTypes = selectedAssetTypes;
            LoadAndDisplayAssets(filterAssetsByType);
        }

        #region Event Handlers
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var searchText = txtSearch.Text.ToLower();
                var filteredAssets = _allAssetsToPreview
                    .Where(a => a.Name.ToLower().Contains(searchText))
                    .ToList();
                PopulateAssetsList(filteredAssets);
            }
            catch (Exception ex)
            {
                _logService.LogError("An error occurred while searching assets. See application_errors.log for details.");
                _logService.LogCritical(ex, "PreviewAssetsWindow.TxtSearch_TextChanged Exception");
            }
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            txtSearchPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtSearch.Text))
            {
                txtSearchPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private async void listBoxAssets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            textBlockNoData.Visibility = Visibility.Collapsed;
            borderMediaPlayer.Child = null;

            if (listBoxAssets.SelectedItem is not AssetInfo selectedAsset)
            {
                return;
            }

            PreviewData previewData = await _assetsPreview.GetPreviewData(selectedAsset.Url, selectedAsset.Name);

            DisplayAssetInUI(previewData);
            textBlockNoData.Visibility = Visibility.Collapsed;
        }

        private void PreviewAssetsWindow_Closed(object sender, EventArgs e)
        {
            if (borderMediaPlayer.Child is MediaElement mediaElement)
            {
                mediaElement.Stop();
                mediaElement.Close();
            }
        }
        #endregion

        #region Private Methods
        private void LoadAndDisplayAssets(Func<IEnumerable<string>, List<string>, List<string>> filterAssetsByType)
        {
            try
            {
                _allAssetsToPreview = _assetsPreview.GetAssetsForPreview(_inputFolder, _selectedAssetTypes, filterAssetsByType);

                if (_allAssetsToPreview == null)
                {
                    throw new InvalidOperationException("GetAssetsForPreview returned a null list, indicating a critical bug.");
                }

                if (!_allAssetsToPreview.Any())
                {
                    _logService.LogWarning("No assets to display after filtering because the list is empty.");
                }
                else
                {
                    var gameAssetsCount = _allAssetsToPreview.Count(a => a.Url.StartsWith("https://raw.communitydragon.org/pbe/game/"));
                    var lcuAssetsCount = _allAssetsToPreview.Count - gameAssetsCount;
                    _logService.Log($"Total of {_allAssetsToPreview.Count} assets for display [{gameAssetsCount} Game Assets and {lcuAssetsCount} LCU Assets].");
                }

                PopulateAssetsList(_allAssetsToPreview);
            }
            catch (Exception ex)
            {
                _logService.LogError("A critical error occurred while trying to load assets for preview. See application_errors.log for details.");
                _logService.LogCritical(ex, "PreviewAssetsWindow.LoadAndDisplayAssets Exception");
                _customMessageBoxService.ShowError("Preview Error", "An unexpected error occurred while preparing the asset preview. Please check the logs for more details.", this, CustomMessageBoxIcon.Error);
            }
        }

        private void PopulateAssetsList(List<AssetInfo> assetsToDisplay)
        {
            DisplayedAssets.Clear();

            if (!assetsToDisplay.Any())
            {
                DisplayedAssets.Add("No assets were found." + Environment.NewLine + "Select another resources folder.");
                _logService.Log("PreviewAssetsWindow: Displaying 'No assets found' message in ListBox.");
                return;
            }

            var gameAssets = assetsToDisplay.Where(a => a.Url.StartsWith("https://raw.communitydragon.org/pbe/game/")).ToList();
            var lcuAssets = assetsToDisplay.Except(gameAssets).ToList();

            DisplayedAssets.Add("🎮 GAME ASSETS");
            if (gameAssets.Any())
            {
                foreach (var asset in gameAssets)
                    DisplayedAssets.Add(asset);
            }
            else
            {
                DisplayedAssets.Add("  No game assets found.");
            }

            DisplayedAssets.Add("");
            DisplayedAssets.Add("💻 LCU ASSETS");
            if (lcuAssets.Any())
            {
                foreach (var asset in lcuAssets)
                    DisplayedAssets.Add(asset);
            }
            else
            {
                DisplayedAssets.Add("  No lcu assets found.");
            }
        }

        private void DisplayAssetInUI(PreviewData previewData)
        {
            borderMediaPlayer.Child = null;

            switch (previewData.ContentType)
            {
                case AssetContentType.Image:
                    DisplayImage(previewData.AssetUrl);
                    break;
                case AssetContentType.Audio:
                    DisplayAudioExternal(previewData.LocalFilePath);
                    break;
                case AssetContentType.Video:
                    DisplayVideoExternal(previewData.LocalFilePath);
                    break;
                case AssetContentType.Text:
                    DisplayText(previewData.TextContent);
                    break;
                case AssetContentType.ExternalProgram:
                    DisplayExternalProgram(previewData);
                    break;
                case AssetContentType.NotFound:
                case AssetContentType.Unsupported:
                default:
                    DisplayUnsupported(previewData.Message);
                    break;
            }
        }

        private void DisplayImage(string url)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(url, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.DownloadFailed += (s, e) => ShowInfoMessage("Image not available for preview. It may have been removed from the server.");

                Image image = new Image { Source = bitmap, Stretch = Stretch.Uniform };
                borderMediaPlayer.Child = image;
            }
            catch (Exception ex)
            {
                _logService.LogError($"An unexpected error occurred while creating the image display for {url}. See application_errors.log for details.");
                _logService.LogCritical(ex, $"PreviewAssetsWindow.DisplayImage Exception for URL: {url}");
                ShowInfoMessage("An error occurred while trying to display the image.");
            }
        }

        private void MenuItem_Click_SaveAs(object sender, RoutedEventArgs e)
        {
            if (borderMediaPlayer.Child is Image image && image.Source is BitmapSource bitmapSource)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = "PNG Image|*.png" };
                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        using (FileStream stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                        {
                            encoder.Save(stream);
                        }
                        _logService.LogSuccess($"Image saved successfully to {saveFileDialog.FileName}");
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError($"Failed to save image to {saveFileDialog.FileName}. See application_errors.log for details.");
                        _logService.LogCritical(ex, $"PreviewAssetsWindow.MenuItem_Click_SaveAs Exception for file: {saveFileDialog.FileName}");
                        _customMessageBoxService.ShowInfo("Error", "Failed to save image: {ex.Message}", this, CustomMessageBoxIcon.Error);
                    }
                }
            }
        }

        private void DisplayAudioExternal(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                _logService.LogDebug($"PreviewAssetsWindow: Opening audio '{{Path.GetFileName(filePath)}}' in external application.");
                ShowInfoMessage($"Opening audio '{{Path.GetFileName(filePath)}}' in external application.");
            }
            catch (Exception ex)
            {
                _logService.LogError($"Could not open audio: '{{filePath}}' externally. See application_errors.log for details.");
                _logService.LogCritical(ex, $"PreviewAssetsWindow.DisplayAudioExternal Exception for file: {{filePath}}");
            }
        }

        private void DisplayVideoExternal(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                _logService.LogDebug($"PreviewAssetsWindow: Opening video file '{{Path.GetFileName(filePath)}}' in external application.");
                ShowInfoMessage($"Opening video '{{Path.GetFileName(filePath)}}' with external program.");
            }
            catch (Exception ex)
            {
                _logService.LogError($"Could not open video '{{filePath}}' externally. See application_errors.log for details.");
                _logService.LogCritical(ex, $"PreviewAssetsWindow.DisplayVideoExternal Exception for file: {{filePath}}");
            }
        }

        private void DisplayText(string textContent)
        {
            ScrollViewer scrollViewer = new ScrollViewer();
            TextBlock textBlock = new TextBlock
            {
                Text = textContent,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };
            scrollViewer.Content = textBlock;
            borderMediaPlayer.Child = scrollViewer;
        }

        private void DisplayExternalProgram(PreviewData previewData)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(previewData.LocalFilePath) { UseShellExecute = true });
                _logService.LogDebug($"PreviewAssetsWindow: Opening {previewData.LocalFilePath} with external program.");
                ShowInfoMessage($"Opening '{{Path.GetFileName(previewData.LocalFilePath)}}' in an external application.\n{previewData.Message}");
            }
            catch (Exception ex)
            {
                _logService.LogError($"Failed to open {previewData.LocalFilePath} with external program. See application_errors.log for details.");
                _logService.LogCritical(ex, $"PreviewAssetsWindow.DisplayExternalProgram Exception for file: {previewData.LocalFilePath}");
            }
        }

        private void DisplayUnsupported(string message)
        {
            ShowInfoMessage(message ?? "Preview not available for this file type.");
        }

        private void ShowInfoMessage(string message)
        {
            borderMediaPlayer.Child = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
        }
        #endregion
    }
}
