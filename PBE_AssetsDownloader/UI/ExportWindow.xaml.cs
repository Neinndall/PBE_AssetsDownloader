using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;

using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Info;

namespace PBE_AssetsDownloader.UI
{
  public partial class ExportWindow : UserControl
  {
    private readonly LogService _logService;
    private readonly HttpClient _httpClient;
    private readonly DirectoriesCreator _directoriesCreator;
    private readonly AssetDownloader _assetDownloader;

    public ExportWindow(
        LogService logService,
        HttpClient httpClient,
        DirectoriesCreator directoriesCreator,
        AssetDownloader assetDownloader)
    {
      InitializeComponent();

      _logService = logService;
      _httpClient = httpClient;
      _directoriesCreator = directoriesCreator;
      _assetDownloader = assetDownloader;

      SetupCheckboxEvents();
    }

    private void SetupCheckboxEvents()
    {
      chkAll.Checked += ChkAll_Checked;
      chkAll.Unchecked += ChkAll_Unchecked;

      var individualCheckboxes = new[] { chkImages, chkAudios, chkPlugins, chkGame };
      foreach (var checkbox in individualCheckboxes)
      {
        checkbox.Checked += IndividualCheckBox_Changed;
        checkbox.Unchecked += IndividualCheckBox_Changed;
      }
    }

    private void ChkAll_Checked(object sender, RoutedEventArgs e)
    {
      SetIndividualCheckboxes(false);
    }

    private void ChkAll_Unchecked(object sender, RoutedEventArgs e)
    {
      if (!HasAnyIndividualCheckboxSelected())
        chkAll.IsChecked = true;
    }

    private void IndividualCheckBox_Changed(object sender, RoutedEventArgs e)
    {
      if (sender is CheckBox checkBox && checkBox.IsChecked.GetValueOrDefault())
      {
        chkAll.IsChecked = false;
      }
      else if (!HasAnyIndividualCheckboxSelected())
      {
        chkAll.IsChecked = true;
      }
    }

    private void SetIndividualCheckboxes(bool value)
    {
      chkImages.IsChecked = value;
      chkAudios.IsChecked = value;
      chkPlugins.IsChecked = value;
      chkGame.IsChecked = value;
    }

    private bool HasAnyIndividualCheckboxSelected()
    {
      return chkImages.IsChecked.GetValueOrDefault() ||
             chkAudios.IsChecked.GetValueOrDefault() ||
             chkPlugins.IsChecked.GetValueOrDefault() ||
             chkGame.IsChecked.GetValueOrDefault();
    }

    private List<string> GetSelectedAssetTypes()
    {
      var selectedTypes = new List<string>();

      if (chkAll.IsChecked.GetValueOrDefault())
      {
        selectedTypes.Add("All");
      }
      else
      {
        var typeMap = new Dictionary<CheckBox, string>
                {
                    { chkImages, "Images" },
                    { chkAudios, "Audios" },
                    { chkPlugins, "Plugins" },
                    { chkGame, "Game" }
                };

        selectedTypes.AddRange(
            typeMap.Where(kvp => kvp.Key.IsChecked.GetValueOrDefault())
                   .Select(kvp => kvp.Value)
        );
      }

      return selectedTypes;
    }

    private void btnPreviewAssets_Click(object sender, RoutedEventArgs e)
    {
      if (!ValidateInputPath()) return;

      var selectedAssetTypes = GetSelectedAssetTypes();
      LogSelectedTypes(selectedAssetTypes);

      if (!selectedAssetTypes.Any())
      {
        ShowWarning("Select at least one type for preview.", "Type not selected");
        return;
      }

      var previewWindow = new PreviewAssetsWindow(
          txtDifferencesPath.Text,
          selectedAssetTypes,
          FilterAssetsByType,
          _httpClient,
          _directoriesCreator,
          _logService,
          _assetDownloader
      );
      previewWindow.ShowDialog();
    }

    private async void BtnDownloadSelectedAssets_Click(object sender, RoutedEventArgs e)
    {
      if (!ValidateInputPath() || !ValidateDownloadPath()) return;

      var selectedAssetTypes = GetSelectedAssetTypes();
      if (!selectedAssetTypes.Any())
      {
        ShowWarning("Select at least one type for download.", "Type not selected");
        return;
      }

      var (gameLines, lcuLines) = await ReadDifferenceFiles();
      if (!gameLines.Any() && !lcuLines.Any())
      {
        ShowWarning("No assets were found with the provided differences.", "Warning");
        return;
      }

      await DownloadAssets(gameLines, lcuLines, selectedAssetTypes);
    }

    private bool ValidateInputPath()
    {
      string inputFolder = txtDifferencesPath.Text;
      if (string.IsNullOrWhiteSpace(inputFolder) || !Directory.Exists(inputFolder))
      {
        ShowWarning("Select a valid folder that contains the differences_game and differences_lcu files.", "Invalid path");
        return false;
      }
      return true;
    }

    private bool ValidateDownloadPath()
    {
      string downloadFolder = txtDownloadTargetPath.Text;
      if (string.IsNullOrWhiteSpace(downloadFolder) || !Directory.Exists(downloadFolder))
      {
        ShowWarning("Select a valid folder to save the exported assets.", "Invalid Folder");
        return false;
      }
      return true;
    }

    private void ShowWarning(string message, string title)
    {
      _logService.LogWarning(message);
      MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void LogSelectedTypes(List<string> selectedAssetTypes)
    {
      if (selectedAssetTypes.Any())
        _logService.LogDebug($"ExportWindow: Selected asset types: {string.Join(", ", selectedAssetTypes)}");
      else
        _logService.LogDebug("ExportWindow: No asset types were selected.");
    }

    private async Task<(string[] gameLines, string[] lcuLines)> ReadDifferenceFiles()
    {
      var differencesGamePath = Path.Combine(txtDifferencesPath.Text, "differences_game.txt");
      var differencesLcuPath = Path.Combine(txtDifferencesPath.Text, "differences_lcu.txt");

      var gameLines = File.Exists(differencesGamePath) ? await File.ReadAllLinesAsync(differencesGamePath) : Array.Empty<string>();
      var lcuLines = File.Exists(differencesLcuPath) ? await File.ReadAllLinesAsync(differencesLcuPath) : Array.Empty<string>();

      return (gameLines, lcuLines);
    }

    private async Task DownloadAssets(string[] gameLines, string[] lcuLines, List<string> selectedAssetTypes)
    {
      _logService.Log("Starting download of assets ...");

      var notFoundAssets = new List<string>();
      var gameAssetsList = FilterAssetsByType(gameLines, selectedAssetTypes);
      var lcuAssetsList = FilterAssetsByType(lcuLines, selectedAssetTypes);

      var gameAssets = gameAssetsList.Select(asset => (asset, "https://raw.communitydragon.org/pbe/game/"));
      var lcuAssets = lcuAssetsList.Select(asset => (asset, "https://raw.communitydragon.org/pbe/"));

      var allAssets = gameAssets.Concat(lcuAssets).ToList();
      int overallTotalFiles = allAssets.Count;

      _assetDownloader.NotifyDownloadStarted(overallTotalFiles);

      _logService.Log($"Total GAME assets to download: {gameAssetsList.Count} and Total LCU assets to download: {lcuAssetsList.Count}");
      await _assetDownloader.DownloadAssets(
          allAssets,
          txtDownloadTargetPath.Text,
          notFoundAssets,
          overallTotalFiles,
          0
      );

      HandleDownloadCompletion(notFoundAssets);
      _assetDownloader.NotifyDownloadCompleted(); // Notify completion after all downloads
    }

    private void HandleDownloadCompletion(List<string> notFoundAssets)
    {
      if (notFoundAssets.Any())
      {
        string notFoundFilePath = Path.Combine(txtDownloadTargetPath.Text, "NotFoundAssets.txt");
        File.WriteAllLines(notFoundFilePath, notFoundAssets);

        string message = $"Some assets could not be downloaded. A list of missing assets has been saved to NotFoundAssets.txt";
        _logService.LogWarning(message);
      }
      else
      {
        _logService.LogSuccess("Download completed successfully!");
        MessageBox.Show("Download completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
      }
    }

    private List<string> FilterAssetsByType(IEnumerable<string> lines, List<string> selectedTypes)
    {
      var filteredAndParsedLines = lines
          .Select(line => line.Trim())
          .Where(line => !string.IsNullOrWhiteSpace(line))
          .Select(line => line.Split(' ').Skip(1).FirstOrDefault())
          .Where(path => path != null)
          .ToList();

      _logService.LogDebug($"FilterAssetsByType: Total parsed lines (before type filter): {filteredAndParsedLines.Count}");

      if (selectedTypes.Any(type => type.Equals("All", StringComparison.OrdinalIgnoreCase)))
        return filteredAndParsedLines.Distinct().ToList();

      var finalFilteredAssets = filteredAndParsedLines
          .Where(path => IsPathMatchingSelectedTypes(path, selectedTypes))
          .Distinct()
          .ToList();

      _logService.LogDebug($"FilterAssetsByType: Total assets after type filter and distinct: {finalFilteredAssets.Count}");
      return finalFilteredAssets;
    }

    private bool IsPathMatchingSelectedTypes(string path, List<string> selectedTypes)
    {
      var typeCheckers = new Dictionary<string, Func<string, bool>>
            {
                { "Images", p => p.EndsWith(".tex", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) },
                { "Audios", p => p.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) },
                { "Plugins", p => p.StartsWith("plugins/", StringComparison.OrdinalIgnoreCase) },
                { "Game", p => p.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) }
            };

      foreach (var type in selectedTypes)
      {
        if (typeCheckers.TryGetValue(type, out var checker) && checker(path))
        {
          _logService.LogDebug($"FilterAssetsByType: Path '{path}' matched '{type}'.");
          return true;
        }
      }
      return false;
    }

    private void BtnBrowseDownloadTargetPath_Click(object sender, RoutedEventArgs e)
    {
      BrowseFolder("Select Download Target Folder", folder => txtDownloadTargetPath.Text = folder, "Download Target Path");
    }

    private void BtnBrowseDifferencesPath_Click(object sender, RoutedEventArgs e)
    {
      BrowseFolder("Select Differences Files Folder", folder => txtDifferencesPath.Text = folder, "Differences Files Path");
    }

    private void BrowseFolder(string title, Action<string> onSuccess, string logPrefix)
    {
      using (var dialog = new CommonOpenFileDialog())
      {
        dialog.IsFolderPicker = true;
        dialog.Title = title;

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
          onSuccess(dialog.FileName);
          _logService.LogDebug($"{logPrefix} selected: {dialog.FileName}");
        }
      }
    }
  }
}