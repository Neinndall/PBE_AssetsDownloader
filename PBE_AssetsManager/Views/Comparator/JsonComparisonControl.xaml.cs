using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Views.Dialogs;
using PBE_AssetsManager.Views.Helpers;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views.Comparator
{
    public partial class JsonComparisonControl : UserControl
    {
        private readonly CustomMessageBoxService _customMessageBoxService;

        public JsonComparisonControl()
        {
            InitializeComponent();
            // Resolve dependencies from the service provider
            _customMessageBoxService = App.ServiceProvider.GetRequiredService<CustomMessageBoxService>();
        }

        private void btnSelectOriginal_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select Original JSON File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                originalFileTextBox.Text = openFileDialog.FileName;
            }
        }

        private void btnSelectNew_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select New JSON File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                newFileTextBox.Text = openFileDialog.FileName;
            }
        }

        private void btnCompare_Click(object sender, RoutedEventArgs e)
        {
            string originalPath = originalFileTextBox.Text;
            string newPath = newFileTextBox.Text;

            if (string.IsNullOrWhiteSpace(originalPath) || string.IsNullOrWhiteSpace(newPath))
            {
                _customMessageBoxService.ShowWarning("Warning", "Please select both an original and a new file.", Window.GetWindow(this));
                return;
            }

            if (!File.Exists(originalPath) || !File.Exists(newPath))
            {
                _customMessageBoxService.ShowError("Error", "One or both of the selected files do not exist.", Window.GetWindow(this));
                return;
            }

            try
            {
                string originalContent = File.ReadAllText(originalPath);
                string newContent = File.ReadAllText(newPath);

                string originalJson = JsonDiffHelper.FormatJson(originalContent);
                string newJson = JsonDiffHelper.FormatJson(newContent);

                var diffWindow = App.ServiceProvider.GetRequiredService<JsonDiffWindow>();
                _ = diffWindow.LoadAndDisplayDiffAsync(originalJson, newJson, Path.GetFileName(originalPath), Path.GetFileName(newPath));
                diffWindow.Show();
            }
            catch (IOException ex)
            {
                _customMessageBoxService.ShowError("Error", $"Error reading files: {ex.Message}", Window.GetWindow(this));
            }
        }
    }
}
