using System.Windows.Controls;
using AssetsManager.Services;
using AssetsManager.Services.Core;
using AssetsManager.Utils;

namespace AssetsManager.Views.Settings
{
    public partial class LogsSettingsView : UserControl
    {
        private LogService _logService;

        public LogsSettingsView()
        {
            InitializeComponent();
        }

        public void SetLogService(LogService logService)
        {
            _logService = logService;
            // Set the output here, as this method is called after the service is available
            // and before the control is necessarily loaded.
            _logService.SetLogOutput(richTextBoxLogs);
        }

        public void ApplySettingsToUI(AppSettings appSettings)
        {
            // This view doesn't depend on AppSettings, but it needs to implement the interface.
            // We set the log output when the control is loaded instead.
        }

        public void SaveSettings()
        {
            // This view doesn't save any settings, but it needs to implement the interface.
        }
    }
}