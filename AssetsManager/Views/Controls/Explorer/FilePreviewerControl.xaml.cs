using System;
using System.Collections.ObjectModel;
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

        public ObservableCollection<PinnedFileViewModel> PinnedFiles { get; set; }

        private PinnedFileViewModel _selectedFile;
        public PinnedFileViewModel SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (_selectedFile != value)
                {
                    if (_selectedFile != null) _selectedFile.IsSelected = false;
                    _selectedFile = value;
                    if (_selectedFile != null) _selectedFile.IsSelected = true;
                    _ = ShowPreviewAsync(_selectedFile?.Node);
                }
            }
        }

        public FilePreviewerControl()
        {
            InitializeComponent();
            this.DataContext = this;
            PinnedFiles = new ObservableCollection<PinnedFileViewModel>();
            this.Loaded += FilePreviewerControl_Loaded;
            this.Unloaded += FilePreviewerControl_Unloaded;
        }

        public void PinFile(FileSystemNodeModel node)
        {
            if (node == null || PinnedFiles.Any(x => x.Node == node)) return;

            var newPinnedFile = new PinnedFileViewModel(node);
            PinnedFiles.Add(newPinnedFile);
            SelectedFile = newPinnedFile;
        }

        public void UnpinFile(PinnedFileViewModel fileToUnpin)
        {
            if (fileToUnpin == null) return;

            int index = PinnedFiles.IndexOf(fileToUnpin);
            if (index != -1)
            {
                PinnedFiles.RemoveAt(index);
                if (SelectedFile == fileToUnpin)
                {
                    SelectedFile = PinnedFiles.Count > 0 ? PinnedFiles[Math.Max(0, index - 1)] : null;
                }
            }
        }

        private void Tab_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PinnedFileViewModel vm)
            {
                SelectedFile = vm;
            }
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PinnedFileViewModel vm)
            {
                UnpinFile(vm);
            }
        }

        private async void FilePreviewerControl_Loaded(object sender, RoutedEventArgs e)
        {
            ExplorerPreviewService.Initialize(
                ImagePreview,
                WebView2Preview,
                TextPreview,
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
            if (PinnedFiles.Count > 0 && SelectedFile == null)
            {
                SelectedFile = PinnedFiles.FirstOrDefault();
                return;
            }

            if (PinnedFiles.Count > 0 && SelectedFile?.Node != node)
            {
                // A file is selected in the tree, but it's not the currently selected pinned tab.
                // Do nothing to the previewer, preserving the pinned view.
                return;
            }

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