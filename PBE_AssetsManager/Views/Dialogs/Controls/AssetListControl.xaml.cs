using PBE_AssetsManager.Info;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views.Dialogs.Controls
{
    public partial class AssetListControl : UserControl
    {
        private List<AssetInfo> _allAssets;
        public ObservableCollection<object> DisplayedAssets { get; set; }
        public event SelectionChangedEventHandler SelectionChanged;

        public AssetListControl()
        {
            InitializeComponent();
            DisplayedAssets = new ObservableCollection<object>();
            listBoxAssets.ItemsSource = DisplayedAssets;
        }

        public void SetAssets(List<AssetInfo> assets)
        {
            _allAssets = assets;
            PopulateAssetsList(_allAssets);
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = txtSearch.Text.ToLower();
            var filteredAssets = _allAssets
                .Where(a => a.Name.ToLower().Contains(searchText))
                .ToList();
            PopulateAssetsList(filteredAssets);
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

        private void listBoxAssets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        private void PopulateAssetsList(List<AssetInfo> assetsToDisplay)
        {
            DisplayedAssets.Clear();

            if (!assetsToDisplay.Any())
            {
                DisplayedAssets.Add("No assets were found.");
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
    }
}
