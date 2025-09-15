using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using AssetsManager.Services;
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

        public FilePreviewerControl()
        {
            InitializeComponent();
            this.Loaded += FilePreviewerControl_Loaded;
            this.Unloaded += FilePreviewerControl_Unloaded;
        }

        private async void FilePreviewerControl_Loaded(object sender, RoutedEventArgs e)
        {
            // The service now gets the controls from its direct parent
            ExplorerPreviewService.Initialize(
                ImagePreview,
                WebView2Preview,
                PreviewPlaceholder,
                SelectFileMessagePanel,
                UnsupportedFileMessagePanel,
                UnsupportedFileMessage
            );

            await InitializeWebView2();
        }

        private void FilePreviewerControl_Unloaded(object sender, RoutedEventArgs e)
        {
            WebView2Preview.CoreWebView2?.Navigate("about:blank");
        }

        public async Task ShowPreviewAsync(FileSystemNodeModel node)
        {
            await ExplorerPreviewService.ShowPreviewAsync(node);
        }

        private async Task InitializeWebView2()
        {
            try
            {
                var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: DirectoriesCreator.WebView2DataPath);
                await WebView2Preview.EnsureCoreWebView2Async(environment);
                WebView2Preview.DefaultBackgroundColor = System.Drawing.Color.Transparent;

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