using System;
using System.Windows.Forms;

namespace PBE_AssetsDownloader.Utils
{
    public partial class ProgressForm : Form
    {
        public ProgressForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Actualiza de manera segura la barra de progreso y el texto asociado en el formulario.
        /// </summary>
        /// <param name="progress">El valor de progreso a establecer (0-100).</param>
        /// <param name="labelText">El texto a mostrar en el label (opcional).</param>
        public void SetProgress(int progress, string labelText = "")
        {
            try
            {
                // Verifica si es necesario hacer un invocador para actualizar los controles en el hilo de la UI
                if (progressBar.InvokeRequired || labelProgress.InvokeRequired)
                {
                    // Si estamos en un hilo diferente al de la UI, invocamos el método en el hilo correcto
                    progressBar.Invoke(new Action<int, string>(SetProgress), progress, labelText);
                }
                else
                {
                    // Limitar el valor del progreso entre 0 y 100
                    progressBar.Value = Math.Max(0, Math.Min(progress, 100));

                    // Actualiza el texto del label con el texto proporcionado
                    labelProgress.Text = labelText;
                }
            }
            catch (ObjectDisposedException)
            {
                // El formulario o los controles pueden ya no estar disponibles, manejamos el error silenciosamente
            }
        }
    }
}