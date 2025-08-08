using System.Windows.Controls;
using System.Windows;
using Material.Icons;
using System;
using Material.Icons.WPF;
using System.Windows.Media.Animation;

namespace PBE_AssetsDownloader.UI.Views
{
    public partial class LogView : UserControl
    {
        public RichTextBox LogRichTextBox => richTextBoxLogs;
        
        public LogView()
        {
            InitializeComponent();
        }
    }
}