using PBE_AssetsManager.Views.Models;
using PBE_AssetsManager.Services.Downloads;
using PBE_AssetsManager.Services;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;

namespace PBE_AssetsManager.Views.Dialogs
{
    public partial class PreviewAssetsWindow : Window
    {
        private readonly AssetsPreview _assetsPreview;
        private readonly LogService _logService;

        public PreviewAssetsWindow(LogService logService, AssetsPreview assetsPreview, CustomMessageBoxService customMessageBoxService)
        {
            InitializeComponent();
            _logService = logService;
            _assetsPreview = assetsPreview;

            // Pass services down to the child control
            AssetPreview.LogService = logService;
            AssetPreview.AssetsPreview = assetsPreview;
            AssetPreview.CustomMessageBoxService = customMessageBoxService;
        }

        public void InitializeData(string inputFolder, List<string> selectedAssetTypes, Func<IEnumerable<string>, List<string>, List<string>> filterAssetsByType)
        {
            try
            {
                var allAssets = _assetsPreview.GetAssetsForPreview(inputFolder, selectedAssetTypes, filterAssetsByType);
                AssetList.SetAssets(allAssets);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "A critical error occurred while trying to load assets for preview.");
            }
        }

        private async void AssetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is AssetInfo selectedAsset)
            {
                await AssetPreview.ShowPreviewAsync(selectedAsset);
            }
        }
    }
}
