using System.Windows.Controls;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader.UI.Views.SettingsViews
{
    public partial class LogsSettingsView : UserControl
    {
        public LogsSettingsView()
        {
            InitializeComponent();
        }

        public void ApplySettingsToUI(LogService logService)
        {
            logService.SetLogOutput(richTextBoxLogs);
        }
    }
}