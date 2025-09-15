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
        private readonly LogService _logService;
        private readonly ExtractionService _extractionService;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private AppSettings _appSettings;

        public HomeWindow(
            LogService logService,
            ExtractionService extractionService,
            AppSettings appSettings,
            CustomMessageBoxService customMessageBoxService)
        {
            InitializeComponent();
            _logService = logService;
            _extractionService = extractionService;
            _appSettings = appSettings;
            _customMessageBoxService = customMessageBoxService;

            // Inject dependencies into child controls
            HomeControl.LogService = _logService;
            HomeControl.AppSettings = _appSettings;

            // Handle events from child controls
            HomeControl.StartRequested += ActionsControl_StartRequested;
        }

        public void UpdateSettings(AppSettings newSettings, bool wasResetToDefaults)
        {
            _appSettings = newSettings;
            HomeControl.UpdateSettings(newSettings, wasResetToDefaults);
        }

        private async void ActionsControl_StartRequested(object sender, System.EventArgs e)
        {
            string oldHashesPath = HomeControl.OldHashesPath;
            string newHashesPath = HomeControl.NewHashesPath;

            if (string.IsNullOrEmpty(oldHashesPath) || string.IsNullOrEmpty(newHashesPath))
            {
                _logService.LogWarning("Please select both hash directories.");
                _customMessageBoxService.ShowWarning("Warning", "Please select both hash directories.", Window.GetWindow(this));
                return;
            }

            _appSettings.NewHashesPath = newHashesPath;
            _appSettings.OldHashesPath = oldHashesPath;

            await _extractionService.ExecuteAsync();
        }
    }
}
