using AssetsManager.Services.Core;
using AssetsManager.Services.Downloads;
using AssetsManager.Views.Controls.Export;
using System;
using System.Windows.Controls;

namespace AssetsManager.Views
{
    public partial class ExportWindow : UserControl
    {
        public ExportWindow(
            LogService logService,
            ExportService exportService
            )
        {
            InitializeComponent();

            DirectoryConfig.LogService = logService;
            DirectoryConfig.ExportService = exportService;

            FilterConfig.LogService = logService;
            FilterConfig.ExportService = exportService;

            ExportActions.ExportService = exportService;
        }
    }
}