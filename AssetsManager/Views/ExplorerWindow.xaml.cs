using AssetsManager.Services.Hashes;
using AssetsManager.Services.Comparator;
using AssetsManager.Services.Explorer;
using AssetsManager.Services;
using AssetsManager.Services.Core;
using AssetsManager.Services.Monitor;
using AssetsManager.Utils;
using AssetsManager.Views.Models;
using System.Windows;
using System.Windows.Controls;

namespace AssetsManager.Views
{
    public partial class ExplorerWindow : UserControl
    {
        public ExplorerWindow(
            LogService logService,
            CustomMessageBoxService customMessageBoxService,
            HashResolverService hashResolverService,
            WadNodeLoaderService wadNodeLoaderService,
            WadExtractionService wadExtractionService,
            WadSearchBoxService wadSearchBoxService,
            DirectoriesCreator directoriesCreator,
            ExplorerPreviewService explorerPreviewService,
            JsBeautifierService jsBeautifierService
        )
        {
            InitializeComponent();
            FileExplorer.LogService = logService;
            FileExplorer.CustomMessageBoxService = customMessageBoxService;
            FileExplorer.HashResolverService = hashResolverService;
            FileExplorer.WadNodeLoaderService = wadNodeLoaderService;
            FileExplorer.WadExtractionService = wadExtractionService;
            FileExplorer.WadSearchBoxService = wadSearchBoxService;

            FilePreviewer.LogService = logService;
            FilePreviewer.CustomMessageBoxService = customMessageBoxService;
            FilePreviewer.DirectoriesCreator = directoriesCreator;
            FilePreviewer.ExplorerPreviewService = explorerPreviewService;

            FileExplorer.FilePinned += FileExplorer_FilePinned;
            FileExplorer.ExplorerContextMenuOpening += FileExplorer_ExplorerContextMenuOpening;
        }

        private void FileExplorer_ExplorerContextMenuOpening(object sender, RoutedEventArgs e)
        {
            if (FileExplorer.PinMenuItem is not null && FileExplorer.FileTreeView.SelectedItem is FileSystemNodeModel selectedNode)
            {
                FileExplorer.PinMenuItem.IsEnabled = selectedNode.Type != NodeType.RealDirectory && selectedNode.Type != NodeType.VirtualDirectory;
            }
        }

        private void FileExplorer_FilePinned(object sender, FileSystemNodeModel e)
        {
            FilePreviewer.PinFile(e);
        }

        private async void FileExplorer_FileSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileSystemNodeModel selectedNode)
            {
                await FilePreviewer.ShowPreviewAsync(selectedNode);
            }
        }
    }
}
