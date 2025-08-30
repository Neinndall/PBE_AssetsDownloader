using System.Windows;
using System.Windows.Controls;
using PBE_AssetsManager.Views.Models;

namespace PBE_AssetsManager.Views
{
    public partial class ExplorerWindow : UserControl
    {
        public ExplorerWindow()
        {
            InitializeComponent();
        }

        private async void FileExplorer_FileSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileSystemNodeModel selectedNode)
            {
                await FilePreviewer.ShowPreviewAsync(selectedNode);
            }
        }
    }
}