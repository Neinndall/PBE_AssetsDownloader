using System;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsDownloader.UI.Controls
{
    public partial class SidebarView : UserControl
    {
        // Propiedad para controlar si la sidebar está expandida
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register("IsExpanded", typeof(bool), typeof(SidebarView), 
                new PropertyMetadata(false));

        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        // Evento para comunicar navegación a la ventana principal
        public event Action<string> NavigationRequested;

        public SidebarView()
        {
            InitializeComponent();
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el tag del botón clickeado
            var button = sender as Button;
            if (button?.Tag is string viewTag)
            {
                NavigationRequested?.Invoke(viewTag);
            }
        }
    }
}