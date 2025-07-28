using System.Windows.Controls;

namespace PBE_AssetsDownloader.UI.Views
{
    public partial class LogView : UserControl
    {
        // Propiedades públicas para que MainWindow pueda acceder a los controles internos
        public RichTextBox LogRichTextBox => richTextBoxLogs;
        public ScrollViewer LogScrollViewerControl => LogScrollViewer;

        public LogView()
        {
            InitializeComponent();
        }
    }
}