using System;
using System.IO;
using System.Windows.Forms;
using PBE_AssetsDownloader.Info;

namespace PBE_AssetsDownloader.UI
{
    public partial class HelpForm : Form
    {
        private const string ChangelogFilePath = "changelogs.txt";

        public HelpForm()
        {
            InitializeComponent();
            ApplicationInfos.SetIcon(this);
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            string aboutText = 
                "PBE_AssetsDownloader - League of Legends" + Environment.NewLine +
                Environment.NewLine +
                "Description:" + Environment.NewLine +
                "This tool is designed to automatically download and manage new assets from League of Legends PBE server updates. " +
                "It helps content creators and developers stay up-to-date with the latest changes and additions to the game." + Environment.NewLine +
                Environment.NewLine +
                "Key Features:" + Environment.NewLine +
                "• Automatic detection of new PBE updates." + Environment.NewLine +
                "• Downloads and organizes new game assets." + Environment.NewLine +
                "• Supports synchronization with CDTB." + Environment.NewLine +
                "• Auto-copy functionality for hash files." + Environment.NewLine +
                Environment.NewLine +
                "How to Use:" + Environment.NewLine +
                "1. Configure your desired settings in the Settings menu" + Environment.NewLine +
                "2. Select the directories for the old and new hashes files." + Environment.NewLine +
                "3. Enable auto-copy if needed." + Environment.NewLine +
                "4. The tool will automatically check for and download new PBE assets." + Environment.NewLine +
                Environment.NewLine +
                "For more information and updates, check the Changelogs section.";

            ShowText(aboutText);
        }

        private void btnChangelogs_Click(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                ShowText($"Error reading changelog file: {ex.Message}");
            }
        }

        private void ShowText(string text)
        {
            richTextBoxContent.Clear();
            richTextBoxContent.SelectionIndent = 4;
            richTextBoxContent.Text = text;
        }
    }
}