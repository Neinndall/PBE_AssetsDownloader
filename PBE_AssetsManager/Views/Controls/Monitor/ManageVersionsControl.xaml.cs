
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Services.Versions;
using PBE_AssetsManager.Views.Models;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views.Controls.Monitor
{
    public partial class ManageVersionsControl : UserControl
    {
        public VersionService VersionService { get; set; }
        public LogService LogService { get; set; }
        private ManageVersions _viewModel;

        public ManageVersionsControl()
        {
            InitializeComponent();
            this.Loaded += ManageVersionsControl_Loaded;
        }

        private async void ManageVersionsControl_Loaded(object sender, RoutedEventArgs e)
        {
            // ViewModel is instantiated here, once services are available.
            if (_viewModel == null && VersionService != null && LogService != null)
            {
                _viewModel = new ManageVersions(VersionService, LogService);
                this.DataContext = _viewModel;
                await _viewModel.LoadVersionFilesAsync();
            }
        }

        private async void FetchVersions_Click(object sender, RoutedEventArgs e)
        {
            if (VersionService != null && LogService != null)
            {
                LogService.Log("User initiated version fetch.");
                await VersionService.FetchAllVersionsAsync();
                if (_viewModel != null)
                {
                    await _viewModel.LoadVersionFilesAsync(); // Refresh the lists after fetching
                }
            }
            else
            {
                MessageBox.Show("Services not initialized.", "Error");
            }
        }
    }
}
