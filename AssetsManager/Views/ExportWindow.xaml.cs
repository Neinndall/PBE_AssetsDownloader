using System;
using System.Windows.Controls;
using AssetsManager.Services.Core;
using AssetsManager.Services.Downloads;
using AssetsManager.Views.Controls.Export;
using AssetsManager.Utils;

namespace AssetsManager.Views
{
    public partial class ExportWindow : UserControl
    {
        public ExportWindow(
            LogService logService,
            DirectoriesCreator directoriesCreator,
            ExportService exportService
            )
        {
            InitializeComponent();

            DirectoryConfig.LogService = logService;
            DirectoryConfig.ExportService = exportService;
            DirectoryConfig.DirectoriesCreator = directoriesCreator;
            
            FilterConfig.LogService = logService;
            FilterConfig.ExportService = exportService;

        }
    }
}