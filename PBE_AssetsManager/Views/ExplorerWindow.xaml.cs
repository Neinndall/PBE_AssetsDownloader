using PBE_AssetsManager.Services.Hashes;
using PBE_AssetsManager.Services.Comparator;
using PBE_AssetsManager.Services.Explorer;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Services.Monitor;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Models;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views
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
