using PBE_AssetsManager.Services;
using PBE_AssetsManager.Views.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PBE_AssetsManager.Views.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;
using PBE_AssetsManager.Utils;
using System.Threading.Tasks;

namespace PBE_AssetsManager.Views.Controls.Monitor
{
    public partial class FileWatcherControl : UserControl
    {
        private List<MonitoredUrl> _allMonitoredUrls;
        public MonitorService MonitorService { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
        public DiffViewService DiffViewService { get; set; }
        public JsonDataService JsonDataService { get; set; }
        public AppSettings AppSettings { get; set; }
        public LogService LogService { get; set; }
        public CustomMessageBoxService CustomMessageBoxService { get; set; }

        public ObservableCollection<MonitoredUrl> MonitoredItems => MonitorService.MonitoredItems;

        public FileWatcherControl()
        {
            InitializeComponent();
            this.Loaded += FileWatcherControl_Loaded;
        }

        private void FileWatcherControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (MonitorService != null)
            {
                _allMonitoredUrls = MonitorService.MonitoredItems.ToList();
                MonitoredItemsListView.ItemsSource = _allMonitoredUrls;
                MonitorService.MonitoredItems.CollectionChanged += MonitoredItems_CollectionChanged;
            }
        }

        private void MonitoredItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _allMonitoredUrls = MonitorService.MonitoredItems.ToList();
            FilterMonitoredItems(txtSearch.Text);
        }

        private void FilterMonitoredItems(string searchText)
        {
            if (_allMonitoredUrls == null) return;

            var lowerSearchText = searchText.ToLower();
            var filteredItems = _allMonitoredUrls
                .Where(item => item.Alias.ToLower().Contains(lowerSearchText) || item.Url.ToLower().Contains(lowerSearchText))
                .ToList();

            MonitoredItemsListView.ItemsSource = filteredItems;
        }

        private async void AddUrl_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceProvider == null || JsonDataService == null || AppSettings == null || CustomMessageBoxService == null) return;

            var dialog = ServiceProvider.GetRequiredService<InputDialog>();
            dialog.Initialize("Add URL", "Enter the URL of the file or directory:", "");
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                await AddMonitoredUrlAsync(dialog.InputText);
            }
        }

        private void ViewChanges_Click(object sender, RoutedEventArgs e)
        {
            if (DiffViewService != null && sender is FrameworkElement element && element.DataContext is MonitoredUrl url)
            {
                _ = DiffViewService.ShowFileDiffAsync(url.OldFilePath, url.NewFilePath, Application.Current.MainWindow);
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MonitoredUrl urlToRemove && AppSettings != null && CustomMessageBoxService != null)
            {
                if (CustomMessageBoxService.ShowYesNo("Remove URL", $"Are you sure you want to remove '{urlToRemove.Alias}'?") == true)
                {
                    if (urlToRemove == null) return;

                    var item = MonitorService.MonitoredItems.FirstOrDefault(x => x.Url == urlToRemove.Url);
                    if (item != null)
                    {
                        MonitorService.MonitoredItems.Remove(item);
                        AppSettings.MonitoredJsonFiles.Remove(item.Url);
                        AppSettings.JsonDataModificationDates.Remove(item.Url);
                        AppSettings.SaveSettings(AppSettings);
                    }
                }
            }
        }

        public async Task AddMonitoredUrlAsync(string newUrl)
        {
            if (string.IsNullOrWhiteSpace(newUrl) || !IsValidUrl(newUrl))
            {
                CustomMessageBoxService.ShowWarning("Invalid URL", "The entered URL is not valid.");
                return;
            }

            var urlsToAdd = new List<string>();

            if (newUrl.EndsWith("/"))
            {
                var fileInfos = await JsonDataService.GetFileUrlsFromDirectoryAsync(newUrl);
                if (!fileInfos.Any())
                {
                    CustomMessageBoxService.ShowWarning("Empty Directory", "Could not find any .json files in the specified directory.");
                    return;
                }
                urlsToAdd.AddRange(fileInfos.Select(fi => fi.Url));
            }
            else
            {
                urlsToAdd.Add(newUrl);
            }

            int addedCount = 0;
            foreach (var url in urlsToAdd)
            {
                if (!AppSettings.MonitoredJsonFiles.Contains(url))
                {
                    AppSettings.MonitoredJsonFiles.Add(url);
                    AppSettings.JsonDataModificationDates[url] = DateTime.MinValue;
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                CustomMessageBoxService.ShowInfo("URLs Added", $"Added {addedCount} new URL(s) to be monitored.");
                AppSettings.SaveSettings(AppSettings);
                MonitorService.LoadMonitoredUrls();
            }
            else
            {
                CustomMessageBoxService.ShowInfo("Duplicate URL", "The specified URL(s) are already being monitored.");
            }
        }

        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterMonitoredItems(txtSearch.Text);
            if (string.IsNullOrEmpty(txtSearch.Text))
            {
                txtSearchPlaceholder.Visibility = Visibility.Visible;
            }
            else
            {
                txtSearchPlaceholder.Visibility = Visibility.Collapsed;
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
    }
}