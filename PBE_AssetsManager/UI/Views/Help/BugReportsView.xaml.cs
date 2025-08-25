using System.Windows.Controls;
using System.Diagnostics;

namespace PBE_AssetsManager.UI.Views.Help
{
    public partial class BugReportsView : UserControl
    {
        public BugReportsView()
        {
            InitializeComponent();
        }

        private void buttonReportBug_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Neinndall/PBE_AssetsManager/issues",
                UseShellExecute = true
            });
        }
    }
}