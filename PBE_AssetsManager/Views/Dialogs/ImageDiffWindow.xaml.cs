using System.Windows;
using System.Windows.Media.Imaging;

namespace PBE_AssetsManager.Views.Dialogs
{
    public partial class ImageDiffWindow : Window
    {
        public ImageDiffWindow(BitmapSource oldImage, BitmapSource newImage)
        {
            InitializeComponent();
            OldImage.Source = oldImage;
            NewImage.Source = newImage;
        }
    }
}
