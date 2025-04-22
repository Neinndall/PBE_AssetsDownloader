using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace PBE_AssetsDownloader.Info
{
    public static class ApplicationInfos
    {
        public static void SetInfo(Form form)
        {
            // Obtener la versión de la aplicación desde el manifiesto del proyecto
            var version = Application.ProductVersion;

            // Actualizar el título de la ventana
            form.Text = $"PBE_AssetsDownloader - League Of Legends | v.{version}";

            // Establecer el icono de la aplicación
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img", "Icon.ico");
            form.Icon = new Icon(iconPath);
        }

        public static void SetIcon(Form form)
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img", "Icon.ico");
            if (File.Exists(iconPath))
            {
                form.Icon = new Icon(iconPath);
            }
            else
            {
                MessageBox.Show("Icon not found at the specified path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
