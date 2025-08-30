using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Models;

namespace PBE_AssetsManager.Views.Explorer
{
    public partial class FilePreviewerControl : UserControl
    {
        private readonly LogService _logService;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly ExplorerPreviewService _explorerPreviewService;

        public FilePreviewerControl()
        {
            InitializeComponent();

            _logService = App.ServiceProvider.GetRequiredService<LogService>();
            _customMessageBoxService = App.ServiceProvider.GetRequiredService<CustomMessageBoxService>();
            _directoriesCreator = App.ServiceProvider.GetRequiredService<DirectoriesCreator>();
            _explorerPreviewService = App.ServiceProvider.GetRequiredService<ExplorerPreviewService>();

            // The service now gets the controls from its direct parent
            _explorerPreviewService.Initialize(
                ImagePreview,
                WebView2Preview,
                PreviewPlaceholder,
                SelectFileMessagePanel,
                UnsupportedFileMessagePanel,
                UnsupportedFileMessage
            );

            this.Loaded += FilePreviewerControl_Loaded;
            this.Unloaded += FilePreviewerControl_Unloaded;
        }

        private async void FilePreviewerControl_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebView2();
        }

        private void FilePreviewerControl_Unloaded(object sender, RoutedEventArgs e)
        {
            WebView2Preview.CoreWebView2?.Navigate("about:blank");
        }

        public async Task ShowPreviewAsync(FileSystemNodeModel node)
        {
            await _explorerPreviewService.ShowPreviewAsync(node);
        }

        private async Task InitializeWebView2()
        {
            try
            {
                var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: _directoriesCreator.WebView2DataPath);
                await WebView2Preview.EnsureCoreWebView2Async(environment);
                WebView2Preview.DefaultBackgroundColor = System.Drawing.Color.Transparent;

                WebView2Preview.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "preview.assets",
                    _directoriesCreator.TempPreviewPath,
                    CoreWebView2HostResourceAccessKind.Allow
                );
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "WebView2 initialization failed. Previews will be affected.");
                _customMessageBoxService.ShowError(
                    "Error",
                    "Could not initialize content viewer. Some previews may not work correctly.",
                    Window.GetWindow(this)
                );
            }
        }
    }
}
