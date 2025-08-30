using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace PBE_AssetsManager.Views.Dialogs
{
    public partial class JsonDiffWindow : Window
    {
        public JsonDiffWindow()
        {
            InitializeComponent();
        }

        public async Task LoadAndDisplayDiffAsync(string oldJson, string newJson, string oldFileName, string newFileName)
        {
            await JsonDiffControl.LoadAndDisplayDiffAsync(oldJson, newJson, oldFileName, newFileName);
        }

        public async Task LoadAndDisplayDiffAsync(string oldFilePath, string newFilePath)
        {
            string oldJson = File.Exists(oldFilePath) ? await File.ReadAllTextAsync(oldFilePath) : "";
            string newJson = File.Exists(newFilePath) ? await File.ReadAllTextAsync(newFilePath) : "";
            await LoadAndDisplayDiffAsync(oldJson, newJson, Path.GetFileName(oldFilePath), Path.GetFileName(newFilePath));
        }
    }
}