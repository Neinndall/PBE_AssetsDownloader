using System;
using System.IO;
using System.Windows.Forms;
using PBE_NewFileExtractor.Info;

namespace PBE_NewFileExtractor.UI
{
    public partial class HelpForm : Form
    {
        private const string ChangelogFilePath = "changelogs.txt"; // Ruta del archivo de changelogs

        public HelpForm()
        {
            InitializeComponent();
            // LLamamos al Icono en la pestaña de Settings
            ApplicationInfos.SetIcon(this);
        }

        // Mensaje sobre la herramienta
        private void btnAbout_Click(object sender, EventArgs e)
        {
            ShowText("PBE_AssetsDownloader is a tool to download new assets from each Server Update (PBE).");   // \nVersión 1.0
        }

        private void btnChangelogs_Click(object sender, EventArgs e)
        {
            if (File.Exists(ChangelogFilePath))
            {
                string changelogContent = File.ReadAllText(ChangelogFilePath);
                ShowText(changelogContent);
            }
            else
            {
                ShowText("The changelog file was not found.");
            }
        }

        private void ShowText(string text)
        {
            // Limpiar el panel de contenido
            contentPanel.Controls.Clear();

            // Crear un nuevo Label para mostrar el texto
            Label contentLabel = new Label
            {
                AutoSize = false,
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.TopLeft,
                Padding = new Padding(10),
                AutoEllipsis = true
            };

            // Añadir el Label al panel de contenido
            contentPanel.Controls.Add(contentLabel);
        }
    }
}
