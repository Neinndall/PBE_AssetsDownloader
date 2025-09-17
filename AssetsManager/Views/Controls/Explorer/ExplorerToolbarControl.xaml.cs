using System.Windows;
using System.Windows.Controls;

namespace AssetsManager.Views.Controls.Explorer
{
    public partial class ExplorerToolbarControl : UserControl
    {
        public event TextChangedEventHandler SearchTextChanged;
        public event RoutedEventHandler CollapseToContainerClicked;

        public FileExplorerControl FileExplorerControl { get; set; }

        public string SearchText => SearchTextBox.Text;

        public ExplorerToolbarControl()
        {
            InitializeComponent();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // If the text contains a path separator, assume it's a path for GoTo functionality
            if (!string.IsNullOrEmpty(SearchTextBox.Text) && SearchTextBox.Text.Contains("/"))
            {
                GoToButton.Visibility = Visibility.Visible;
                // Do NOT invoke SearchTextChanged, as we don't want to filter the tree for paths
            }
            else
            {
                GoToButton.Visibility = Visibility.Collapsed;
                // Invoke SearchTextChanged for normal filtering
                SearchTextChanged?.Invoke(this, e);
            }
        }

        private void CollapseToContainerButton_Click(object sender, RoutedEventArgs e)
        {
            CollapseToContainerClicked?.Invoke(this, e);
        }

        private async void GoToButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileExplorerControl != null && !string.IsNullOrEmpty(SearchTextBox.Text))
            {
                await FileExplorerControl.ExpandToPath(SearchTextBox.Text);
            }
        }
    }
}
