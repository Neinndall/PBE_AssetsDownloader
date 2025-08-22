using System.Collections.Generic;
using System.Windows;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader.UI.Dialogs
{
    public partial class WadComparisonResultWindow : Window
    {
        public WadComparisonResultWindow(List<ChunkDiff> diffs)
        {
            InitializeComponent();
            resultsDataGrid.ItemsSource = diffs;
        }
    }
}
