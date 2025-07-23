using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PBE_AssetsDownloader.Info;
using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.Utils;

namespace PBE_AssetsDownloader.UI
{
    public partial class PreviewAssetsWindow : Window
    {
        #region Fields
        private readonly string _inputFolder;
        private readonly List<string> _selectedAssetTypes;
        private readonly LogService _logService;
        private readonly Func<IEnumerable<string>, List<string>, List<string>> _filterAssetsByType;
        private readonly HttpClient _httpClient;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly AssetDownloader _assetDownloader;
        private readonly AssetsPreview _assetsPreview;
        private List<AssetInfo> _allAssetsToPreview;
        #endregion

        #region Constructor
        public PreviewAssetsWindow(
            string inputFolder, 
            List<string> selectedAssetTypes, 
            Func<IEnumerable<string>, List<string>, List<string>> filterAssetsByType, 
            HttpClient httpClient, 
            DirectoriesCreator directoriesCreator,
            LogService logService,
            AssetDownloader assetDownloader)
        {
            InitializeComponent();
           
            _logService = logService;
            _assetDownloader = assetDownloader;
            _inputFolder = inputFolder;
            _selectedAssetTypes = selectedAssetTypes;
            _filterAssetsByType = filterAssetsByType;
            _httpClient = httpClient;
            _directoriesCreator = directoriesCreator;

            _assetsPreview = new AssetsPreview(_assetDownloader, _filterAssetsByType, _logService);
            _allAssetsToPreview = new List<AssetInfo>();
        }
        #endregion

        #region Event Handlers
        private void LoadAndDisplayAssets_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAndDisplayAssets();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var searchText = txtSearch.Text.ToLower();
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    PopulateAssetsList(_allAssetsToPreview); // Muestra todos si no hay texto
                }
                else
                {
                    var filteredAssets = _allAssetsToPreview
                        .Where(a => a.Name.ToLower().Contains(searchText))
                        .ToList();
                    PopulateAssetsList(filteredAssets); // Muestra solo los filtrados
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"An error occurred while searching assets: {ex.Message}");
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

            if (listBoxAssets.SelectedItem == null) return;

            string selectedAssetName = listBoxAssets.SelectedItem.ToString().Trim();

            if (IsHeaderOrSeparator(selectedAssetName))
            {
                textBlockNoData.Visibility = Visibility.Visible;
                return;
            }

            AssetInfo selectedAsset = _allAssetsToPreview.FirstOrDefault(a => a.Name == selectedAssetName);

            if (selectedAsset != null)
            {
                PreviewData previewData = await _assetsPreview.GetPreviewData(selectedAsset.Url, selectedAsset.Name);

                DisplayAssetInUI(previewData);
                textBlockNoData.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("Asset information not found for preview.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                textBlockNoData.Visibility = Visibility.Visible;
            }
        }

        private void PreviewAssetsWindow_Closed(object sender, EventArgs e)
        {
            if (borderMediaPlayer.Child is MediaElement mediaElement)
            {
                mediaElement.Stop();
                mediaElement.Close();
            }
            // _logService.ClearLog(); // Clear the logs
        }
        #endregion

        #region Private Methods
        private void LoadAndDisplayAssets()
        {
            _logService.Log("PreviewAssetsWindow: Attempting to load and display assets.");

            _allAssetsToPreview = _assetsPreview.GetAssetsForPreview(_inputFolder, _selectedAssetTypes);

            if (_allAssetsToPreview != null)
            {
                if (!_allAssetsToPreview.Any())
                {
                    _logService.Log("PreviewAssetsWindow: No assets to display after filtering (list is empty).");
                }
                else
                {
                    var gameAssetsCount = _allAssetsToPreview.Count(a => a.Url.StartsWith("https://raw.communitydragon.org/pbe/game/"));
                    var lcuAssetsCount = _allAssetsToPreview.Count - gameAssetsCount; // LCU assets are all others
                    _logService.Log($"PreviewAssetsWindow: Received {_allAssetsToPreview.Count} assets for display [{gameAssetsCount} Game Assets and {lcuAssetsCount} LCU Assets].");
                }
            }
            else
            {
                _logService.LogWarning("PreviewAssetsWindow: _allAssetsToPreview list is null after GetAssetsForPreview. This should not happen if GetAssetsForPreview always returns a List.");
            }

            PopulateAssetsList(_allAssetsToPreview);
            _logService.Log("PreviewAssetsWindow: Finished loading and displaying assets.");
        }

        private void PopulateAssetsList(List<AssetInfo> assetsToDisplay)
        {
            listBoxAssets.Items.Clear();

            if (!assetsToDisplay.Any())
            {
                listBoxAssets.Items.Add("No assets were found." + Environment.NewLine + "Select another resources folder.");
                _logService.Log("PreviewAssetsWindow: Displaying 'No assets found' message in ListBox.");
                return;
            }

            var gameAssets = assetsToDisplay.Where(a => a.Url.StartsWith("https://raw.communitydragon.org/pbe/game/")).ToList();
            var lcuAssets = assetsToDisplay.Except(gameAssets).ToList();

            // _logService.Log($"PreviewAssetsWindow: Found {gameAssets.Count} Game Assets and {lcuAssets.Count} LCU Assets.");

            // Game Assets Section
            listBoxAssets.Items.Add("🎮 GAME ASSETS");
            if (gameAssets.Any())
            {
                foreach (var asset in gameAssets)
                    listBoxAssets.Items.Add($"  {asset.Name}");
            }
            else
            {
                listBoxAssets.Items.Add("  No game assets found.");
            }

            // LCU Assets Section
            listBoxAssets.Items.Add("");
            listBoxAssets.Items.Add("💻 LCU ASSETS");
            if (lcuAssets.Any())
            {
                foreach (var asset in lcuAssets)
                    listBoxAssets.Items.Add($"  {asset.Name}");
            }
            else
            {
                listBoxAssets.Items.Add("  No lcu assets found.");
            }
        }

        private bool IsHeaderOrSeparator(string selectedAssetName)
        {
            return selectedAssetName.Contains("ASSETS") ||
                   selectedAssetName.Contains("No game assets found") ||
                   selectedAssetName.Contains("No LCU assets found") ||
                   selectedAssetName.Contains("No assets were found") ||
                   string.IsNullOrWhiteSpace(selectedAssetName) ||
                   selectedAssetName.StartsWith("🎮") ||
                   selectedAssetName.StartsWith("💻") ||
                   selectedAssetName == "";
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

                // Manejar el evento de fallo de descarga
                bitmap.DownloadFailed += (s, e) =>
                {
                    // No es necesario un log aquí, solo mostrar el mensaje en la UI.
                    ShowInfoMessage("Image not available for preview. It may have been removed from the server.");
                };

                Image image = new Image();
                image.Source = bitmap;
                image.Stretch = Stretch.Uniform;
                borderMediaPlayer.Child = image;
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"An unexpected error occurred while creating the image display for {url}.");
                ShowInfoMessage("An error occurred while trying to display the image.");
            }
        }

        private void DisplayAudioExternal(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                _logService.LogDebug($"PreviewAssetsWindow: Opening audio '{Path.GetFileName(filePath)}' in external application.");
                ShowInfoMessage($"Opening audio '{Path.GetFileName(filePath)}' in external application.");
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"PreviewAssetsWindow: Could not open audio: '{filePath}' externally.");
            }
        }

        private void DisplayVideoExternal(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                _logService.LogDebug($"PreviewAssetsWindow: Opening video file '{Path.GetFileName(filePath)}' in external application.");
                ShowInfoMessage($"Opening video '{Path.GetFileName(filePath)}' with external program.");
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"PreviewAssetsWindow: Could not open video '{filePath}' externally.");
            }
        }

        private void DisplayText(string textContent)
        {
            ScrollViewer scrollViewer = new ScrollViewer();
            TextBlock textBlock = new TextBlock();
            textBlock.Text = textContent;
            textBlock.Foreground = Brushes.White;
            textBlock.TextWrapping = TextWrapping.Wrap;
            scrollViewer.Content = textBlock;
            borderMediaPlayer.Child = scrollViewer;
        }

        private void DisplayExternalProgram(PreviewData previewData)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(previewData.LocalFilePath) { UseShellExecute = true });
                _logService.LogDebug($"PreviewAssetsWindow: Opening {previewData.LocalFilePath} with external program.");
                ShowInfoMessage($"Opening '{Path.GetFileName(previewData.LocalFilePath)}' in an external application.\n{previewData.Message}");
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"PreviewAssetsWindow: Failed to open {previewData.LocalFilePath} with external program.");
            }
        }

        private void DisplayUnsupported(string message)
        {
            ShowInfoMessage(message ?? "Preview not available for this file type.");
        }

        private TextBlock CreateTextBlock(string text, Brush foreground)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = foreground,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
        }

        private void ShowInfoMessage(string message)
        {
            borderMediaPlayer.Child = CreateTextBlock(message, Brushes.White);
        }
        #endregion
    }
}