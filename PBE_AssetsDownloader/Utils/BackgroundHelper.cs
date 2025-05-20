using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace PBE_AssetsDownloader.Utils
{
    public static class BackgroundHelper
    {
        public static void SetBackgroundImage(Form form, ImageLayout layout = ImageLayout.Center)
        {
            const string defaultResource = "PBE_AssetsDownloader.img.background.season2025_background.jpg"; // Nombre del recurso por defecto

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(defaultResource))
                {
                    if (stream != null)
                    {
                        form.BackgroundImage = Image.FromStream(stream);
                        form.BackgroundImageLayout = layout;
                    }
                    else
                    {
                        MessageBox.Show($"La imagen '{defaultResource}' no se encontró.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la imagen: {ex.Message}");
            }
        }
    }
}
