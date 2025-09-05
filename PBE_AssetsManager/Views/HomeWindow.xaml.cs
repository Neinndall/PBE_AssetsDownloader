using PBE_AssetsManager.Services.Downloads;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Utils;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views
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
            DirectorySelection.LogService = _logService;
            DirectorySelection.AppSettings = _appSettings;

            // Handle events from child controls
            ActionsControl.StartRequested += ActionsControl_StartRequested;
        }

        public void UpdateSettings(AppSettings newSettings, bool wasResetToDefaults)
        {
            _appSettings = newSettings;
            DirectorySelection.UpdateSettings(newSettings, wasResetToDefaults);
        }

        private async void ActionsControl_StartRequested(object sender, System.EventArgs e)
        {
            string oldHashesPath = DirectorySelection.OldHashesPath;
            string newHashesPath = DirectorySelection.NewHashesPath;

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
