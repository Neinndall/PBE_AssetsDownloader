using PBE_AssetsManager.Info;
using PBE_AssetsManager.Services;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

namespace PBE_AssetsManager.Views.Dialogs.Controls
{
    public partial class AssetPreviewControl : UserControl
    {
        public LogService LogService { get; set; }
        public AssetsPreview AssetsPreview { get; set; }
        public CustomMessageBoxService CustomMessageBoxService { get; set; }

        public AssetPreviewControl()
        {
            InitializeComponent();
            this.Unloaded += AssetPreviewControl_Unloaded;
        }

        private void AssetPreviewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (borderMediaPlayer.Child is MediaElement mediaElement)
            {
                mediaElement.Stop();
                mediaElement.Close();
            }
        }

        public async Task ShowPreviewAsync(AssetInfo selectedAsset)
        {
            noDataPanel.Visibility = Visibility.Collapsed;
            borderMediaPlayer.Child = null;

            if (selectedAsset == null)
            {
                noDataPanel.Visibility = Visibility.Visible;
                return;
            }
            
            noDataPanel.Visibility = Visibility.Collapsed;

            PreviewData previewData = await AssetsPreview.GetPreviewData(selectedAsset.Url, selectedAsset.Name);

            DisplayAssetInUI(previewData);
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
                LogService.LogError(ex, $"An unexpected error occurred while creating the image display for {url}.");
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
                        LogService.LogSuccess($"Image saved successfully to {saveFileDialog.FileName}");
                    }
                    catch (Exception ex)
                    {
                        LogService.LogError(ex, $"Failed to save image to {saveFileDialog.FileName}.");
                        CustomMessageBoxService.ShowError("Error", $"Failed to save image: {ex.Message}", Window.GetWindow(this));
                    }
                }
            }
        }

        private void DisplayAudioExternal(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                LogService.LogDebug($"PreviewAssetsWindow: Opening audio '{{Path.GetFileName(filePath)}}' in external application.");
                ShowInfoMessage($"Opening audio '{{Path.GetFileName(filePath)}}' in external application.");
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, $"Could not open audio: '{filePath}' externally.");
            }
        }

        private void DisplayVideoExternal(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                LogService.LogDebug($"PreviewAssetsWindow: Opening video file '{{Path.GetFileName(filePath)}}' in external application.");
                ShowInfoMessage($"Opening video '{{Path.GetFileName(filePath)}}' with external program.");
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, $"Could not open video '{{filePath}}' externally.");
                LogService.LogCritical(ex, $"PreviewAssetsWindow.DisplayVideoExternal Exception for file: {{filePath}}");
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
                LogService.LogDebug($"PreviewAssetsWindow: Opening {previewData.LocalFilePath} with external program.");
                ShowInfoMessage($"Opening '{{Path.GetFileName(previewData.LocalFilePath)}}' in an external application.\n{previewData.Message}");
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, $"Failed to open {previewData.LocalFilePath} with external program.");
                LogService.LogCritical(ex, $"PreviewAssetsWindow.DisplayExternalProgram Exception for file: {previewData.LocalFilePath}");
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
    }
}