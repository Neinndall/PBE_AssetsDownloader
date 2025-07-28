using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.Threading.Tasks;
using System.Windows.Threading;
using Newtonsoft.Json; // Added for JSON formatting

namespace PBE_AssetsDownloader.UI
{
    public partial class JsonDiffWindow : Window
    {
        public JsonDiffWindow(string oldJson, string newJson)
        {
            InitializeComponent();
            _ = DisplayDiffAsync(oldJson, newJson);
        }

        private string FormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;
            try
            {
                // Parse the JSON and then re-serialize it with formatting
                var parsedJson = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            }
            catch (JsonException)
            {
                // If it's not valid JSON, return the original string
                return json;
            }
        }

        private async Task DisplayDiffAsync(string oldJson, string newJson)
        {
            OldJsonContent.Document.Blocks.Add(new Paragraph(new Run("Calculating differences...")));
            NewJsonContent.Document.Blocks.Add(new Paragraph(new Run("Calculating differences...")));

            SideBySideDiffModel diffModel = null;

            await Task.Run(() =>
            {
                // Format JSON before diffing
                string formattedOldJson = FormatJson(oldJson);
                string formattedNewJson = FormatJson(newJson);

                var differ = new Differ();
                var diffBuilder = new SideBySideDiffBuilder(differ);
                diffModel = diffBuilder.BuildDiffModel(formattedOldJson, formattedNewJson);
            });

            OldJsonContent.Document.Blocks.Clear();
            NewJsonContent.Document.Blocks.Clear();

            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < diffModel.OldText.Lines.Count; i++)
                {
                    var oldLine = diffModel.OldText.Lines[i];
                    var newLine = diffModel.NewText.Lines[i];

                    // Render OldJsonContent
                    var oldRun = new Run(oldLine.Text);
                    var oldParagraph = new Paragraph(oldRun);
                    oldParagraph.Margin = new Thickness(0);

                    switch (oldLine.Type)
                    {
                        case ChangeType.Deleted:
                            oldParagraph.Background = new SolidColorBrush(Color.FromArgb(40, 240, 128, 128)); // Lighter LightCoral
                            break;
                        case ChangeType.Modified:
                            oldParagraph.Background = new SolidColorBrush(Color.FromArgb(40, 255, 165, 0)); // Lighter Orange
                            break;
                        case ChangeType.Imaginary:
                            oldRun.Text = "\u00A0"; // Non-breaking space for alignment
                            oldParagraph.Background = Brushes.LightGray;
                            break;
                        default:
                            break;
                    }
                    OldJsonContent.Document.Blocks.Add(oldParagraph);

                    // Render NewJsonContent
                    var newRun = new Run(newLine.Text);
                    var newParagraph = new Paragraph(newRun);
                    newParagraph.Margin = new Thickness(0);

                    switch (newLine.Type)
                    {
                        case ChangeType.Inserted:
                            newParagraph.Background = new SolidColorBrush(Color.FromArgb(40, 144, 238, 144)); // Lighter LightGreen
                            break;
                        case ChangeType.Modified:
                            newParagraph.Background = new SolidColorBrush(Color.FromArgb(40, 255, 165, 0)); // Lighter Orange
                            break;
                        case ChangeType.Imaginary:
                            newRun.Text = "\u00A0"; // Non-breaking space for alignment
                            newParagraph.Background = Brushes.LightGray;
                            break;
                        default:
                            break;
                    }
                    NewJsonContent.Document.Blocks.Add(newParagraph);
                }
            });
        }
    }
}
