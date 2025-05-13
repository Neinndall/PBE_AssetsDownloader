using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Info;

namespace PBE_AssetsDownloader.UI
{
    public partial class PreviewAssetsForm : Form
    {
        private readonly string inputFolder;
        private readonly List<string> selectedAssetTypes;
        private readonly Func<IEnumerable<string>, List<string>, List<string>> filterAssetsByType;

        // Constructor que recibe la función filterAssetsByType
        public PreviewAssetsForm(string inputFolder, List<string> selectedAssetTypes, Func<IEnumerable<string>, List<string>, List<string>> filterAssetsByType)
        {
            InitializeComponent();
            BackgroundHelper.SetBackgroundImage(this);
            ApplicationInfos.SetIcon(this);
            
            this.inputFolder = inputFolder;
            this.selectedAssetTypes = selectedAssetTypes;
            this.filterAssetsByType = filterAssetsByType;

            PreviewAssets();
        }

        private void PreviewAssets()
        {
            var differencesGamePath = Path.Combine(inputFolder, "differences_game.txt");
            var differencesLcuPath = Path.Combine(inputFolder, "differences_lcu.txt");

            // Leer los archivos de diferencias
            var gameLines = File.Exists(differencesGamePath) ? File.ReadAllLines(differencesGamePath) : Array.Empty<string>();
            var lcuLines = File.Exists(differencesLcuPath) ? File.ReadAllLines(differencesLcuPath) : Array.Empty<string>();

            if (!gameLines.Any() && !lcuLines.Any())
            {
                richTextBoxAssets.AppendText("No assets found in the provided differences.\n");
                return;
            }

            // Filtrar los assets según el tipo seleccionado usando la función proporcionada
            var gameAssets = filterAssetsByType(gameLines, selectedAssetTypes);
            var lcuAssets = filterAssetsByType(lcuLines, selectedAssetTypes);

            // Mostrar las diferencias en el RichTextBox
            richTextBoxAssets.Clear();
            richTextBoxAssets.SelectionIndent = 4;

            // Game Assets
            richTextBoxAssets.AppendText("Game Assets:\n");
            if (gameAssets.Any())
            {
                richTextBoxAssets.AppendText(string.Join("\n", gameAssets) + "\n\n");
            }
            else
            {
                richTextBoxAssets.AppendText(" No game assets found.\n\n");
            }

            // LCU Assets
            richTextBoxAssets.AppendText("LCU Assets:\n");
            if (lcuAssets.Any())
            {
                richTextBoxAssets.AppendText(string.Join("\n", lcuAssets));
            }
            else
            {
                richTextBoxAssets.AppendText(" No LCU assets found.");
            }
        }
    }
}
