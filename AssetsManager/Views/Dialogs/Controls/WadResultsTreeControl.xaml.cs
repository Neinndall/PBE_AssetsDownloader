using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AssetsManager.Views.Dialogs.Controls
{
    public partial class WadResultsTreeControl : UserControl
    {
        public static readonly RoutedEvent ViewDifferencesClickEvent = EventManager.RegisterRoutedEvent(
            nameof(ViewDifferencesClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(WadResultsTreeControl));

        public event RoutedEventHandler ViewDifferencesClick
        {
            add { AddHandler(ViewDifferencesClickEvent, value); }
            remove { RemoveHandler(ViewDifferencesClickEvent, value); }
        }

        public static readonly RoutedEvent DownloadMenuItemClickEvent = EventManager.RegisterRoutedEvent(
            nameof(DownloadMenuItemClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(WadResultsTreeControl));

        public event RoutedEventHandler DownloadMenuItemClick
        {
            add { AddHandler(DownloadMenuItemClickEvent, value); }
            remove { RemoveHandler(DownloadMenuItemClickEvent, value); }
        }

        public event RoutedPropertyChangedEventHandler<object> SelectedItemChanged;
        public event TextChangedEventHandler SearchTextChanged;
        public event RoutedEventHandler WadContextMenuOpening;

        public IEnumerable<object> ItemsSource
        {
            get => resultsTreeView.ItemsSource as IEnumerable<object>;
            set => resultsTreeView.ItemsSource = value;
        }

        public object SelectedItem => resultsTreeView.SelectedItem;

        public MenuItem ViewDifferencesMenuItem => (this.FindResource("WadDiffContextMenu") as ContextMenu)?.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "ViewDifferencesMenuItem");
        public MenuItem DownloadMenuItem => (this.FindResource("WadDiffContextMenu") as ContextMenu)?.Items.OfType<MenuItem>().FirstOrDefault(m => "Download Selected".Equals(m.Header as string));

        public WadResultsTreeControl()
        {
            InitializeComponent();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchPlaceholder.Visibility = string.IsNullOrEmpty(searchTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            SearchTextChanged?.Invoke(this, e);
        }

        private void ResultsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedItemChanged?.Invoke(this, e);
        }

        private void ViewDifferences_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ViewDifferencesClickEvent, SelectedItem));
        }

        private void DownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(DownloadMenuItemClickEvent, SelectedItem));
        }

        public void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (VisualUpwardSearch(e.OriginalSource as DependencyObject) is TreeViewItem treeViewItem)
            {
                treeViewItem.IsSelected = true;
                e.Handled = true;
            }
        }

        private static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            return source as TreeViewItem;
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            WadContextMenuOpening?.Invoke(this, e);
        }
    }
}