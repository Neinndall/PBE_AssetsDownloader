using System.Windows;

namespace PBE_AssetsDownloader.UI.Dialogs
{
    public enum UpdateMode
    {
        None,
        Clean,
        Replace
    }

    public partial class UpdateModeDialog : Window
    {
        public UpdateMode SelectedMode { get; private set; } = UpdateMode.None;

        public UpdateModeDialog()
        {
            InitializeComponent();
        }

        private void CleanInstallButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedMode = UpdateMode.Clean;
            DialogResult = true;
        }

        private void ReplaceInstallButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedMode = UpdateMode.Replace;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }
    }
}
