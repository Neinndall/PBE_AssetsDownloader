using PBE_AssetsManager.Services;
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Services.Downloads;
using PBE_AssetsManager.Services.Monitor;
using PBE_AssetsManager.Views.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows;

using System.Threading;
using Microsoft.Win32;

namespace PBE_AssetsManager.Views.Controls.Monitor
{
    public partial class AssetTrackerControl : UserControl
    {
        // Public properties for dependency injection from the container
        public MonitorService MonitorService { get; set; }
        public AssetDownloader AssetDownloader { get; set; }
        public LogService LogService { get; set; }
        public CustomMessageBoxService CustomMessageBoxService { get; set; }

        // Internal state properties
        public ObservableCollection<AssetCategory> Categories { get; set; }
        public AssetCategory SelectedCategory { get; set; }
        public ObservableCollection<TrackedAsset> Assets { get; set; }

        public AssetTrackerControl()
        {
            InitializeComponent();
            this.Loaded += AssetTrackerControl_Loaded;
            this.Unloaded += AssetTrackerControl_Unloaded;
            Categories = new ObservableCollection<AssetCategory>();
            Assets = new ObservableCollection<TrackedAsset>();
        }

        private void AssetTrackerControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (MonitorService == null) return;

            MonitorService.CategoryCheckStarted += OnCategoryCheckStarted;
            MonitorService.CategoryCheckCompleted += OnCategoryCheckCompleted;

            MonitorService.LoadAssetCategories();

            Categories.Clear();
            foreach (var category in MonitorService.AssetCategories)
            {
                Categories.Add(category);
            }

            CategoryComboBox.ItemsSource = Categories;

            if (Categories.Any())
            {
                CategoryComboBox.SelectedItem = Categories.First();
            }
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update the internal SelectedCategory property from the ComboBox's selection
            SelectedCategory = CategoryComboBox.SelectedItem as AssetCategory;
            RefreshAssetList();
        }

        private void RefreshAssetList()
        {
            Assets.Clear();
            if (SelectedCategory == null || MonitorService == null) return;

            var assetsFromService = MonitorService.GetAssetListForCategory(SelectedCategory);
            foreach (var asset in assetsFromService)
            {
                Assets.Add(asset);
            }
            AssetsItemsControl.ItemsSource = Assets;


        }

        private void DownloadButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Logic to be implemented
        }

        private void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (MonitorService == null || SelectedCategory == null) return;

            LoadMoreButton.IsEnabled = false;
            try
            {
                var newAssets = MonitorService.GenerateMoreAssets(Assets, SelectedCategory, 5); // Generate 5 more
                foreach (var asset in newAssets)
                {
                    Assets.Add(asset);
                }
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "An error occurred while loading more assets.");
                CustomMessageBoxService?.ShowError("Error", "An error occurred while loading more assets. Please check the logs.");
            }
            finally
            {
                LoadMoreButton.IsEnabled = true;
            }
        }

        private async void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            if (MonitorService == null || SelectedCategory == null || !Assets.Any()) return;

            var assetsToCheck = Assets.Where(a => a.Status == "Pending").ToList();
            if (!assetsToCheck.Any())
            {
                CustomMessageBoxService?.ShowInfo("Info", "No pending assets to check.");
                return;
            }

            CheckButton.IsEnabled = false;
            LoadMoreButton.IsEnabled = false;

            // Instantly update UI to show "Checking"
            foreach (var asset in assetsToCheck)
            {
                asset.Status = "Checking";
            }

            try
            {
                // Process the assets in the background. The UI will update live thanks to INotifyPropertyChanged.
                await MonitorService.CheckAssetsAsync(assetsToCheck, SelectedCategory, CancellationToken.None);

                // Optional: Show a summary message after completion
                var foundCount = assetsToCheck.Count(a => a.Status == "OK");
                CustomMessageBoxService?.ShowInfo("Info", $"Check finished. Found {foundCount} new assets out of {assetsToCheck.Count} checked.");
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "An error occurred during asset check.");
                CustomMessageBoxService?.ShowError("Error", "An error occurred during asset check. Please check the logs.");
            }
            finally
            {
                CheckButton.IsEnabled = true;
                LoadMoreButton.IsEnabled = true;
            }
        }

        private void AssetTrackerControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (MonitorService != null)
            {
                MonitorService.CategoryCheckStarted -= OnCategoryCheckStarted;
                MonitorService.CategoryCheckCompleted -= OnCategoryCheckCompleted;
            }
        }

        private void OnCategoryCheckStarted(AssetCategory category)
        {
            if (category == SelectedCategory)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var asset in Assets.Where(a => a.Status == "Pending"))
                    {
                        asset.Status = "Checking";
                    }
                });
            }
        }

        private void OnCategoryCheckCompleted(AssetCategory category)
        {
            if (category == SelectedCategory)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MonitorService.InvalidateAssetCacheForCategory(category);
                    RefreshAssetList();
                });
            }
        }

        private async void SaveAssetButton_Click(object sender, RoutedEventArgs e)
        {
            if (AssetDownloader == null) return;

            var button = sender as Button;
            var asset = button?.Tag as TrackedAsset;
            if (asset == null) return;

            string extension = Path.GetExtension(asset.Url);

            var saveFileDialog = new SaveFileDialog
            {
                FileName = asset.DisplayName
            };

            if (!string.IsNullOrEmpty(extension))
            {
                saveFileDialog.Filter = $"Asset File (*{extension})|*{extension}|All files (*.*)|*.*";
                saveFileDialog.DefaultExt = extension;
            }
            else
            {
                saveFileDialog.Filter = "All files (*.*)|*.*";
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    await AssetDownloader.DownloadAssetToCustomPathAsync(asset.Url, saveFileDialog.FileName);
                    CustomMessageBoxService?.ShowSuccess("Success", $"Asset '{asset.DisplayName}' saved successfully.");
                }
                catch (Exception ex)
                {
                    LogService.LogError(ex, $"Failed to save asset '{asset.DisplayName}'.");
                    CustomMessageBoxService?.ShowError("Error", $"Failed to save asset. Check logs for details.");
                }
            }
        }

        private void RemoveAssetButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var assetToRemove = button?.Tag as TrackedAsset;
            if (assetToRemove == null || SelectedCategory == null) return;

            var result = CustomMessageBoxService.ShowYesNo("Remove Asset", $"Are you sure you want to remove '{assetToRemove.DisplayName}'? This will mark it as 'Not Found' and it won't be shown as a valid asset anymore.");
            if (result == true)
            {
                Assets.Remove(assetToRemove);
                MonitorService.RemoveAsset(SelectedCategory, assetToRemove);
            }
        }
    }
}