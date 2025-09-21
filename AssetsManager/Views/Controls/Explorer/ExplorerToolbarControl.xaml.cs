using System.Windows;
using System.Windows.Controls;

namespace AssetsManager.Views.Controls.Explorer
{
    public partial class ExplorerToolbarControl : UserControl
    {
        public event TextChangedEventHandler SearchTextChanged;
        public event RoutedEventHandler CollapseToContainerClicked;
        public event RoutedEventHandler LoadComparisonClicked;

        public string SearchText => SearchTextBox.Text;

        public ExplorerToolbarControl()
        {
            InitializeComponent();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchTextChanged?.Invoke(this, e);
        }

        private void CollapseToContainerButton_Click(object sender, RoutedEventArgs e)
        {
            CollapseToContainerClicked?.Invoke(this, e);
        }

        private void LoadComparisonButton_Click(object sender, RoutedEventArgs e)
        {
            LoadComparisonClicked?.Invoke(this, e);
        }
    }
}
