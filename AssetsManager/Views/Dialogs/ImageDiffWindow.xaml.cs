using System.Windows;
using System.Windows.Media.Imaging;

namespace AssetsManager.Views.Dialogs
{
    public partial class ImageDiffWindow : Window
    {
        public ImageDiffWindow(BitmapSource oldImage, BitmapSource newImage, string oldFileName, string newFileName)
        {
            InitializeComponent();
            OldImage.Source = oldImage;
            NewImage.Source = newImage;
            OldFileNameLabel.Text = oldFileName;
            NewFileNameLabel.Text = newFileName;
        }
    }
}
