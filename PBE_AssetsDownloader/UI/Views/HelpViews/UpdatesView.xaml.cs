using System.Windows.Controls;
using PBE_AssetsDownloader.Utils;

namespace PBE_AssetsDownloader.UI.Views.HelpViews
{
    public partial class UpdatesView : UserControl
    {
        public UpdatesView()
        {
            InitializeComponent();
        }

        private async void buttonCheckUpdates_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Assuming the parent window is the owner for the update dialog
            var parentWindow = System.Windows.Window.GetWindow(this);
            await UpdateManager.CheckForUpdatesAsync(parentWindow, true);
        }
    }
}