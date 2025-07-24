// PBE_AssetsDownloader/Utils/BackgroundHelper.cs

using System;
using System.IO;        // Para Stream
using System.Reflection; // Para Assembly
using System.Windows;   // Para Window
using System.Windows.Media; // Para ImageBrush, Stretch
using System.Windows.Media.Imaging; // Para BitmapImage
using Serilog;

namespace PBE_AssetsDownloader.Utils
{
    public static class BackgroundHelper
    {
        // La ruta correcta para un Embedded Resource
        private const string EmbeddedImagePath = "PBE_AssetsDownloader.img.background.season2025_background.jpg";

        public static void SetBackgroundImage(Window window, Stretch stretch = Stretch.UniformToFill)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(EmbeddedImagePath))
                {
                    if (stream == null)
                    {
                        MessageBox.Show($"Error: La imagen de fondo '{EmbeddedImagePath}' no se encontró como recurso incrustado. Verifica la acción de compilación y el nombre del recurso.", "Error de Carga", MessageBoxButton.OK, MessageBoxImage.Error);
                        Log.Warning($"La imagen de fondo '{EmbeddedImagePath}' no se encontró como recurso incrustado.");
                        return;
                    }

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = stream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Cache the image to avoid stream issues
                    bitmapImage.EndInit();
                    bitmapImage.Freeze(); // Freeze for better performance and thread safety

                    ImageBrush imageBrush = new ImageBrush(bitmapImage);
                    imageBrush.Stretch = stretch;

                    window.Background = imageBrush;
                    Log.Information($"Imagen de fondo '{EmbeddedImagePath}' cargada y aplicada correctamente.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la imagen de fondo: {ex.Message}", "Error de Carga", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, $"Error al cargar la imagen de fondo '{EmbeddedImagePath}'.");
            }
        }
    }
}