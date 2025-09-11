
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Services.Versions;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views.Controls.Monitor
{
    public partial class ManageVersionsControl : UserControl
    {
        public VersionService VersionService { get; set; }
        public LogService LogService { get; set; }

        public ManageVersionsControl()
        {
            InitializeComponent();
        }

        private async void FetchVersions_Click(object sender, RoutedEventArgs e)
        {
            if (VersionService != null && LogService != null)
            {
                LogService.Log("User initiated version fetch.");
                await VersionService.FetchAllVersionsAsync();
            }
            else
            {
                MessageBox.Show("Services not initialized.", "Error");
            }
        }
    }
}
