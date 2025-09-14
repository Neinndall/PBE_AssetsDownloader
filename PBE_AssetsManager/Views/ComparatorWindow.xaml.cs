using PBE_AssetsManager.Services.Comparator;
using PBE_AssetsManager.Services.Downloads;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Services.Monitor;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Controls.Comparator;
using System;
using System.Windows.Controls;
using PBE_AssetsManager.Services.Hashes;

namespace PBE_AssetsManager.Views
{
    public partial class ComparatorWindow : UserControl
    {
        public event EventHandler<LoadWadComparisonEventArgs> LoadWadComparisonRequested;

        public ComparatorWindow(
            CustomMessageBoxService customMessageBoxService,
            WadComparatorService wadComparatorService,
            LogService logService,
            DirectoriesCreator directoriesCreator,
            AssetDownloader assetDownloader,
            WadDifferenceService wadDifferenceService,
            WadPackagingService wadPackagingService,
            BackupManager backupManager,
            AppSettings appSettings,
            DiffViewService diffViewService,
            IServiceProvider serviceProvider,
            HashResolverService hashResolverService
            )
        {
            InitializeComponent();
            WadComparisonControl.LoadWadComparisonRequested += (sender, args) => LoadWadComparisonRequested?.Invoke(this, args);

            // Set services for JsonComparisonControl
            JsonComparisonControl.CustomMessageBoxService = customMessageBoxService;
            JsonComparisonControl.DiffViewService = diffViewService;

            // Set services for WadComparisonControl
            WadComparisonControl.CustomMessageBoxService = customMessageBoxService;
            WadComparisonControl.WadComparatorService = wadComparatorService;
            WadComparisonControl.LogService = logService;
            WadComparisonControl.DirectoriesCreator = directoriesCreator;
            WadComparisonControl.AssetDownloaderService = assetDownloader;
            WadComparisonControl.WadDifferenceService = wadDifferenceService;
            WadComparisonControl.WadPackagingService = wadPackagingService;
            WadComparisonControl.BackupManager = backupManager;
            WadComparisonControl.AppSettings = appSettings;
            WadComparisonControl.ServiceProvider = serviceProvider;
            WadComparisonControl.DiffViewService = diffViewService;
            WadComparisonControl.HashResolverService = hashResolverService;
        }
    }
}