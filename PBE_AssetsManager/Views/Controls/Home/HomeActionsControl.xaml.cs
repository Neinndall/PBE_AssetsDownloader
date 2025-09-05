using System;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views.Controls.Home
{
    public partial class HomeActionsControl : UserControl
    {
        public event EventHandler StartRequested;

        public HomeActionsControl()
        {
            InitializeComponent();
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            StartRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
