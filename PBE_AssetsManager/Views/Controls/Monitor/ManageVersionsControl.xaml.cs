
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Services.Versions;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views.Controls.Monitor
{
    public partial class ManageVersionsControl : UserControl
    {
        public VersionService VersionService { get; set; }
        public LogService LogService { get; set; }
        public AppSettings AppSettings { get; set; }
        public CustomMessageBoxService CustomMessageBoxService { get; set; }
        private ManageVersions _viewModel;

        public ManageVersionsControl()
        {
            InitializeComponent();
            this.Loaded += ManageVersionsControl_Loaded;
            LeagueClientVersionsListView.SelectionChanged += ListView_SelectionChanged;
            LoLGameClientVersionsListView.SelectionChanged += ListView_SelectionChanged;
        }

        private async void ManageVersionsControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null && VersionService != null && LogService != null)
            {
                _viewModel = new ManageVersions(VersionService, LogService);
                this.DataContext = _viewModel;
                await _viewModel.LoadVersionFilesAsync();
            }
        }

        private async void FetchVersions_Click(object sender, RoutedEventArgs e)
        {
            if (VersionService != null && LogService != null)
            {
                LogService.Log("User initiated version fetch.");
                await VersionService.FetchAllVersionsAsync();
                if (_viewModel != null)
                {
                    await _viewModel.LoadVersionFilesAsync();
                }
            }
            else
            {
                CustomMessageBoxService.ShowError("Error", "Services not initialized.");
            }
        }

        private async void GetLeagueClient_Click(object sender, RoutedEventArgs e)
        {
            var selectedVersion = _viewModel?.LeagueClientVersions.FirstOrDefault(v => v.IsSelected);
            if (selectedVersion == null)
            {
                CustomMessageBoxService.ShowWarning("No Version Selected", "Please select a League Client version from the list first.");
                return;
            }

            if (string.IsNullOrEmpty(AppSettings.LolDirectory))
            {
                CustomMessageBoxService.ShowError("Directory Not Found", "League of Legends directory is not configured. Please set it in Settings > Default Paths.");
                return;
            }

            var locales = new List<string>();
            if (_viewModel.IsEsEsSelected) locales.Add("es_ES");
            if (_viewModel.IsEsMxSelected) locales.Add("es_MX");
            if (_viewModel.IsEnUsSelected) locales.Add("en_US");

            if (locales.Count == 0)
            {
                CustomMessageBoxService.ShowWarning("No Locales Selected", "Please select at least one locale to download.");
                return;
            }

            await VersionService.DownloadPluginsAsync(selectedVersion.Content, AppSettings.LolDirectory, locales);
        }

        private async void GetLoLGameClient_Click(object sender, RoutedEventArgs e)
        {
            var selectedVersion = _viewModel?.LoLGameClientVersions.FirstOrDefault(v => v.IsSelected);
            if (selectedVersion == null)
            {
                CustomMessageBoxService.ShowWarning("No Version Selected", "Please select a LoL Game Client version from the list first.");
                return;
            }

            if (string.IsNullOrEmpty(AppSettings.LolDirectory))
            {
                CustomMessageBoxService.ShowError("Directory Not Found", "League of Legends directory is not configured. Please set it in Settings > Default Paths.");
                return;
            }

            var locales = new List<string>();
            if (_viewModel.IsEsEsSelected) locales.Add("es_ES");
            if (_viewModel.IsEsMxSelected) locales.Add("es_MX");
            if (_viewModel.IsEnUsSelected) locales.Add("en_US");

            if (locales.Count == 0)
            {
                CustomMessageBoxService.ShowWarning("No Locales Selected", "Please select at least one locale to download.");
                return;
            }

            await VersionService.DownloadGameClientAsync(selectedVersion.Content, AppSettings.LolDirectory, locales);
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selectedItem = e.AddedItems[0] as VersionFileInfo;
                if (selectedItem != null)
                {
                    // Deselect all other items in both lists
                    foreach (var item in _viewModel.LeagueClientVersions.Where(i => i != selectedItem))
                    {
                        item.IsSelected = false;
                    }
                    foreach (var item in _viewModel.LoLGameClientVersions.Where(i => i != selectedItem))
                    {
                        item.IsSelected = false;
                    }
                }
            }
        }
    }
}
