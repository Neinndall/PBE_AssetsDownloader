using System;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Info;

namespace PBE_AssetsDownloader.UI
{
    public partial class HelpForm : Form
    {
        private const string EmbeddedChangelogResource = "PBE_AssetsDownloader.changelogs.txt";

        public HelpForm()
        {
            InitializeComponent();
            BackgroundHelper.SetBackgroundImage(this);
            ApplicationInfos.SetIcon(this);
            this.Load += HelpForm_Load;
        }

        private void HelpForm_Load(object sender, EventArgs e)
        {
            tabControl.SelectedIndexChanged += tabControl_SelectedIndexChanged;
            tabControl.SelectedIndex = 0;

            // Asegurarse de que el contenido "About" se cargue al abrir el formulario
            FormatRichText(richTextBoxAbout, GetAboutText());
        }


        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedIndex == 0)
                FormatRichText(richTextBoxAbout, GetAboutText());
            else if (tabControl.SelectedIndex == 1)
                FormatRichText(richTextBoxChangelogs, LoadEmbeddedChangelog());
        }

        private string LoadEmbeddedChangelog()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using Stream stream = assembly.GetManifestResourceStream(EmbeddedChangelogResource);
                if (stream == null) return "Changelog resource not found.";
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                return $"Error reading changelog resource: {ex.Message}";
            }
        }

        private void FormatRichText(RichTextBox box, string content)
        {
            box.Clear();
            box.SelectionIndent = 4;
            box.AppendText(Environment.NewLine);

            foreach (var rawLine in content.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                var line = rawLine.TrimStart();
                bool isTitle = line.EndsWith(":");
                bool isHeader = line.StartsWith("PBE_AssetsDownloader");

                if (line.Contains("[NEW]") || line.Contains("[IMPROVEMENTS]") || line.Contains("[BUG FIXES]"))
                    ApplyStyle(box, FontStyle.Bold, Color.Black, bullet: false);
                else if (isHeader)
                    ApplyStyle(box, FontStyle.Bold, Color.DarkBlue, bullet: false);
                else if (isTitle)
                    ApplyStyle(box, FontStyle.Bold, Color.Black, bullet: false);
                else if (line.StartsWith("•"))
                    ApplyStyle(box, FontStyle.Regular, Color.Black, bullet: true);
                else if (char.IsDigit(line.FirstOrDefault()) && line.Contains("."))
                    ApplyStyle(box, FontStyle.Regular, Color.DarkSlateGray, bullet: false);
                else
                    ApplyStyle(box, FontStyle.Regular, Color.Black, bullet: false);

                box.AppendText(line + Environment.NewLine);
                box.SelectionBullet = false;
            }
        }

        private void ApplyStyle(RichTextBox box, FontStyle style, Color color, bool bullet)
        {
            box.SelectionFont = new Font(box.Font, style);
            box.SelectionColor = color;
            box.SelectionBullet = bullet;
        }

        private string GetAboutText()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "Description:",
                "This app is designed to automatically download and manage new assets from League of Legends PBE server updates. " +
                "It helps content creators and developers stay up-to-date with the latest changes and additions to the game.",
                "",
                "Key Features:",
                "1. Automatic detection of new PBE updates.",
                "2. Downloads and organizes new game assets.",
                "3. Supports synchronization with CDTB.",
                "4. Auto-copy functionality for hash files.",
                "5. Back-Ups for hash files.",
                "6. Manually download assets from others days with differences text files.",
                "",
                "How to Use:",
                "1. Configure your desired settings in the Settings menu",
                "2. Select the directories for the old and new hashes files.",
                "3. Enable auto-copy if needed.",
                "4. The tool will automatically check for and download new PBE assets.",
                "",
                "For more information and updates, check the Changelogs section."
            });
        }
        
        private void buttonReportBug_Click(object sender, EventArgs e)
        {
            // Puedes abrir un enlace o mostrar una ventana
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/Neinndall/PBE_AssetsDownloader/issues", // o un formulario
                UseShellExecute = true
            });
        }


    }
}
