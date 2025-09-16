using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using AssetsManager.Services.Core;
using AssetsManager.Services.Explorer;
using AssetsManager.Utils;
using AssetsManager.Views.Models;

namespace AssetsManager.Views.Controls.Explorer
{
    public partial class FilePreviewerControl : UserControl
    {
        public LogService LogService { get; set; }
        public CustomMessageBoxService CustomMessageBoxService { get; set; }
        public DirectoriesCreator DirectoriesCreator { get; set; }
        public ExplorerPreviewService ExplorerPreviewService { get; set; }

        public FilePreviewerViewModel ViewModel { get; set; }
        private bool _isLoaded = false;

        public FilePreviewerControl()
        {
            InitializeComponent();
            ViewModel = new FilePreviewerViewModel();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            this.DataContext = ViewModel;
            this.Loaded += FilePreviewerControl_Loaded;
            this.Unloaded += FilePreviewerControl_Unloaded;
        }

        private async void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilePreviewerViewModel.SelectedFile))
            {
                await HandleSelectedFileChangedAsync();
            }
        }

        private async Task HandleSelectedFileChangedAsync()
        {
            try
            {
                await ShowPreviewAsync(ViewModel.SelectedFile?.Node);
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Error handling selected file change");
            }
        }

        private async void Tab_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PinnedFileViewModel vm)
            {
                ViewModel.SelectedFile = vm;
                await HandleSelectedFileChangedAsync();
            }
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PinnedFileViewModel vm)
            {
                ViewModel.UnpinFile(vm);
            }
        }

        private async void FilePreviewerControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded) return;

            try
            {
                ExplorerPreviewService.Initialize(
                    ImagePreview,
                    WebView2Preview,
                    TextEditorPreview,
                    PreviewPlaceholder,
                    SelectFileMessagePanel,
                    UnsupportedFileMessagePanel,
                    UnsupportedFileMessage
                );

                await InitializeWebView2();
                await ExplorerPreviewService.ConfigureWebViewAfterInitializationAsync();
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Error loading FilePreviewerControl");
            }
        }

        private async void FilePreviewerControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Usar el servicio para limpiar consistentemente
                await ExplorerPreviewService.ResetPreviewAsync();
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Error cleaning WebView2 on unload");
            }
        }

        public async Task ShowPreviewAsync(FileSystemNodeModel node)
        {
            var existingPin = ViewModel.PinnedFiles.FirstOrDefault(p => p.Node == node);

            if (existingPin != null)
            {
                ViewModel.SelectedFile = existingPin;
            }
            else
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                ViewModel.SelectedFile = null;
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;

                await ExplorerPreviewService.ShowPreviewAsync(node);
            }
        }

        private async Task InitializeWebView2()
        {
            try
            {
                var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: DirectoriesCreator.WebView2DataPath);
                await WebView2Preview.EnsureCoreWebView2Async(environment);

                WebView2Preview.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "preview.assets",
                    DirectoriesCreator.TempPreviewPath,
                    CoreWebView2HostResourceAccessKind.Allow
                );
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "WebView2 initialization failed. Previews will be affected.");
                CustomMessageBoxService.ShowError(
                    "Error",
                    "Could not initialize content viewer. Some previews may not work correctly.",
                    Window.GetWindow(this)
                );
            }
        }
    }
}