// PBE_AssetsDownloader/UI/UpdateProgressWindow.xaml.cs

using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsDownloader.UI // <--- Make sure this matches x:Class
{
    /// <summary>
    /// Interaction logic for UpdateProgressWindow.xaml
    /// </summary>
    public partial class UpdateProgressWindow : Window
    {
        public UpdateProgressWindow()
        {
            InitializeComponent();
        }

        public void SetProgress(int percentage, string message)
        {
            if (!CheckAccess())
            {
                Dispatcher.Invoke(() => SetProgress(percentage, message));
                return;
            }

            DownloadProgressBar.Value = percentage;
            MessageTextBlock.Text = message;
        }
    }
}