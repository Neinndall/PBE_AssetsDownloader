using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views.Controls.Explorer
{
    public partial class ExplorerToolbarControl : UserControl
    {
        public event TextChangedEventHandler SearchTextChanged;
        public event RoutedEventHandler CollapseAllClicked;

        public string SearchText => SearchTextBox.Text;

        public ExplorerToolbarControl()
        {
            InitializeComponent();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchTextChanged?.Invoke(this, e);
        }

        private void CollapseAllButton_Click(object sender, RoutedEventArgs e)
        {
            CollapseAllClicked?.Invoke(this, e);
        }
    }
}
