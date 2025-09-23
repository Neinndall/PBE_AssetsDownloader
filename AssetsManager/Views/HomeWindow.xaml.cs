using AssetsManager.Services.Downloads;
using AssetsManager.Services;
using AssetsManager.Services.Core;
using AssetsManager.Utils;
using System.Windows;
using System.Windows.Controls;

namespace AssetsManager.Views
{
    public partial class HomeWindow : UserControl
    {
        private AppSettings _appSettings;

        public HomeWindow(
            LogService logService,
            ExtractionService extractionService,
            AppSettings appSettings,
            CustomMessageBoxService customMessageBoxService)
        {
            InitializeComponent();
            _appSettings = appSettings;

            // Inject dependencies into child controls
            HomeControl.LogService = logService;
            HomeControl.AppSettings = appSettings;
            HomeControl.ExtractionService = extractionService;
            HomeControl.CustomMessageBoxService = customMessageBoxService;
        }

        public void UpdateSettings(AppSettings newSettings, bool wasResetToDefaults)
        {
            _appSettings = newSettings;
            HomeControl.UpdateSettings(newSettings, wasResetToDefaults);
        }
    }
}
