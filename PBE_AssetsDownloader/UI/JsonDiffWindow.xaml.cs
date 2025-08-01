using System;
using System.Windows;
using System.Windows.Media;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using System.IO;
using ICSharpCode.AvalonEdit.Folding;
using System.Reflection;

namespace PBE_AssetsDownloader.UI
{
    public partial class JsonDiffWindow : Window
    {
        private FoldingManager _oldFoldingManager;
        private FoldingManager _newFoldingManager;
        public JsonDiffWindow(string oldJson, string newJson)
        {
            InitializeComponent();
         
            // Initialize folding
            _oldFoldingManager = FoldingManager.Install(OldJsonContent.TextArea);
            _newFoldingManager = FoldingManager.Install(NewJsonContent.TextArea);

            // Configure editors
            ConfigureTextEditors();

            // Load JSON syntax highlighting
            LoadJsonSyntaxHighlighting();

            _ = DisplayDiffAsync(oldJson, newJson);
        }

        private void ConfigureTextEditors()
        {
            // Configure both editors
            var editors = new[] { OldJsonContent, NewJsonContent };
            
            foreach (var editor in editors)
            {
                editor.Options.EnableHyperlinks = false;
                editor.Options.EnableEmailHyperlinks = false;
                editor.Options.ShowEndOfLine = false;
                editor.Options.ShowSpaces = false;
                editor.Options.ShowTabs = false;
                editor.Options.ConvertTabsToSpaces = true;
                editor.Options.IndentationSize = 2;
                editor.FontFamily = new FontFamily("Consolas, Courier New, monospace");
                editor.FontSize = 13;
                
                // NO establecer Background y Foreground aquí - dejar que el highlighting lo maneje
                
                // Line numbers styling
                editor.ShowLineNumbers = true;
            }
        }

        private void LoadJsonSyntaxHighlighting()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string[] resourceNames = assembly.GetManifestResourceNames();
                
                string resourceName = null;
                foreach (string name in resourceNames)
                {
                    if (name.EndsWith("JsonSyntaxHighlighting.xshd"))
                    {
                        resourceName = name;
                        break;
                    }
                }
                
                if (resourceName != null)
                {
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            using (XmlReader reader = XmlReader.Create(stream))
                            {
                                IHighlightingDefinition jsonHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                                OldJsonContent.SyntaxHighlighting = jsonHighlighting;
                                NewJsonContent.SyntaxHighlighting = jsonHighlighting;
                                //MessageBox.Show($"Syntax highlighting loaded successfully from: {resourceName}", "Debug Info");
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Stream for resource '{resourceName}' was null.", "Error - Syntax Highlighting");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("JsonSyntaxHighlighting.xshd resource not found in assembly.", "Error - Syntax Highlighting");
                    // Optionally, show all available resources for debugging
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading syntax highlighting: {ex.Message}", "Error - Syntax Highlighting");
            }
        }

        private string FormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;
            try
            {
                var parsedJson = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
            }
            catch (JsonException)
            {
                return json;
            }
        }

        private async Task DisplayDiffAsync(string oldJson, string newJson)
        {
            string formattedOldJson = FormatJson(oldJson);
            string formattedNewJson = FormatJson(newJson);

            SideBySideDiffModel diffModel = null;

            await Task.Run(() =>
            {
                var differ = new Differ();
                var diffBuilder = new SideBySideDiffBuilder(differ);
                diffModel = diffBuilder.BuildDiffModel(formattedOldJson, formattedNewJson);
            });

            // Set text to AvalonEdit controls
            OldJsonContent.Text = formattedOldJson;
            NewJsonContent.Text = formattedNewJson;

            // Update folding
            _oldFoldingManager?.Clear();
            _newFoldingManager?.Clear();

            // Apply diff highlighting using a custom renderer
            OldJsonContent.TextArea.TextView.BackgroundRenderers.Clear();
            NewJsonContent.TextArea.TextView.BackgroundRenderers.Clear();
            
            if (diffModel != null)
            {
                OldJsonContent.TextArea.TextView.BackgroundRenderers.Add(
                    new DiffBackgroundRenderer(diffModel.OldText, Colors.Red, Colors.Orange, Colors.LightGray));
                NewJsonContent.TextArea.TextView.BackgroundRenderers.Add(
                    new DiffBackgroundRenderer(diffModel.NewText, Colors.LightGreen, Colors.Orange, Colors.LightGray));
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Clean up folding managers
            _oldFoldingManager?.Clear();
            _newFoldingManager?.Clear();
            base.OnClosed(e);
        }
    }

    // Custom BackgroundRenderer for Diff Highlighting
    public class DiffBackgroundRenderer : ICSharpCode.AvalonEdit.Rendering.IBackgroundRenderer
    {
        private readonly DiffPaneModel _diffPaneModel;
        private readonly Color _insertedColor;
        private readonly Color _modifiedColor;
        private readonly Color _imaginaryColor;

        public DiffBackgroundRenderer(DiffPaneModel diffPaneModel, Color insertedColor, Color modifiedColor, Color imaginaryColor)
        {
            _diffPaneModel = diffPaneModel;
            _insertedColor = insertedColor;
            _modifiedColor = modifiedColor;
            _imaginaryColor = imaginaryColor;
        }

        public ICSharpCode.AvalonEdit.Rendering.KnownLayer Layer
        {
            get { return ICSharpCode.AvalonEdit.Rendering.KnownLayer.Background; }
        }

        public void Draw(ICSharpCode.AvalonEdit.Rendering.TextView textView, System.Windows.Media.DrawingContext drawingContext)
        {
            if (_diffPaneModel == null || _diffPaneModel.Lines == null) return;

            foreach (var line in textView.VisualLines)
            {
                int lineNumber = line.FirstDocumentLine.LineNumber;
                if (lineNumber > 0 && lineNumber <= _diffPaneModel.Lines.Count)
                {
                    var diffLine = _diffPaneModel.Lines[lineNumber - 1];
                    Brush backgroundBrush = null;

                    switch (diffLine.Type)
                    {
                        case ChangeType.Inserted:
                            backgroundBrush = new SolidColorBrush(Color.FromArgb(50, _insertedColor.R, _insertedColor.G, _insertedColor.B));
                            break;
                        case ChangeType.Deleted:
                            backgroundBrush = new SolidColorBrush(Color.FromArgb(50, Colors.Red.R, Colors.Red.G, Colors.Red.B));
                            break;
                        case ChangeType.Modified:
                            backgroundBrush = new SolidColorBrush(Color.FromArgb(50, _modifiedColor.R, _modifiedColor.G, _modifiedColor.B));
                            break;
                        case ChangeType.Imaginary:
                            backgroundBrush = new SolidColorBrush(Color.FromArgb(30, _imaginaryColor.R, _imaginaryColor.G, _imaginaryColor.B));
                            break;
                    }

                    if (backgroundBrush != null)
                    {
                        var rect = new Rect(0, line.VisualTop - textView.ScrollOffset.Y, 
                                          textView.ActualWidth, line.Height);
                        drawingContext.DrawRectangle(backgroundBrush, null, rect);
                    }
                }
            }
        }
    }
}