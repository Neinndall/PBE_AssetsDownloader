using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Newtonsoft.Json;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using PBE_AssetsDownloader.UI.Helpers;

namespace PBE_AssetsDownloader.UI
{
    public partial class JsonDiffWindow : Window
    {
        private SideBySideDiffModel _diffModel;
        private bool _isScrollingSynced;
        private DiffPanelNavigation _diffPanelNavigation;

        public JsonDiffWindow(string oldJson, string newJson)
        {
            InitializeComponent();
            ConfigureEditors();
            LoadJsonSyntaxHighlighting();
            _ = DisplayDiffAsync(oldJson, newJson);
            SetupScrollSync();
        }

        private void ConfigureEditors()
        {
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
                editor.FontFamily = DiffColorsHelper.VisualSettings.EditorFontFamily;
                editor.FontSize = DiffColorsHelper.VisualSettings.EditorFontSize;
                editor.ShowLineNumbers = true;
                editor.WordWrap = false;
            }
        }

        private void LoadJsonSyntaxHighlighting()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = Array.Find(assembly.GetManifestResourceNames(), name => name.EndsWith("JsonSyntaxHighlighting.xshd"));
                
                if (resourceName != null)
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        using var reader = XmlReader.Create(stream);
                        var jsonHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        OldJsonContent.SyntaxHighlighting = jsonHighlighting;
                        NewJsonContent.SyntaxHighlighting = jsonHighlighting;
                    }
                }
            }
            catch
            {
                // Silently continue without syntax highlighting if it fails
            }
        }

        private async Task DisplayDiffAsync(string oldJson, string newJson)
        {
            var formattedOldJson = FormatJson(oldJson);
            var formattedNewJson = FormatJson(newJson);

            await Task.Run(() =>
            {
                var differ = new Differ();
                var diffBuilder = new SideBySideDiffBuilder(differ);
                _diffModel = diffBuilder.BuildDiffModel(formattedOldJson, formattedNewJson);
            });

            var normalizedOld = NormalizeTextForAlignment(_diffModel.OldText);
            var normalizedNew = NormalizeTextForAlignment(_diffModel.NewText);

            OldJsonContent.Text = normalizedOld.Text;
            NewJsonContent.Text = normalizedNew.Text;

            ApplyDiffHighlighting(normalizedOld.LineTypes, normalizedNew.LineTypes);
            
            _diffPanelNavigation = new DiffPanelNavigation(OldNavigationPanel, NewNavigationPanel, _diffModel);
            _diffPanelNavigation.ScrollRequested += ScrollToLine;
            _diffPanelNavigation.DrawPanels();
        }

        private (string Text, List<ChangeType> LineTypes) NormalizeTextForAlignment(DiffPaneModel paneModel)
        {
            var lines = new List<string>();
            var lineTypes = new List<ChangeType>();
            
            foreach (var line in paneModel.Lines)
            {
                lines.Add(line.Type == ChangeType.Imaginary ? "" : line.Text ?? "");
                lineTypes.Add(line.Type);
            }
            
            return (string.Join("\r\n", lines), lineTypes);
        }

        private static string FormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;
            
            try
            {
                var parsed = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(parsed, Newtonsoft.Json.Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }

        private void ApplyDiffHighlighting(List<ChangeType> oldLineTypes, List<ChangeType> newLineTypes)
        {
            OldJsonContent.TextArea.TextView.BackgroundRenderers.Clear();
            NewJsonContent.TextArea.TextView.BackgroundRenderers.Clear();

            OldJsonContent.TextArea.TextView.BackgroundRenderers.Add(new DiffBackgroundRenderer(oldLineTypes));
            NewJsonContent.TextArea.TextView.BackgroundRenderers.Add(new DiffBackgroundRenderer(newLineTypes));
        }

        private void ScrollToLine(int lineNumber)
        {
            _isScrollingSynced = true;
            try
            {
                var lineIndex = Math.Max(0, lineNumber - 1);
                OldJsonContent.ScrollTo(lineNumber, 0);
                NewJsonContent.ScrollTo(lineNumber, 0);
            }
            finally
            {
                _isScrollingSynced = false;
            }
        }

        private void SetupScrollSync()
        {
            OldJsonContent.Loaded += (_, _) => SetupScrollSyncAfterLoaded();
        }

        private void SetupScrollSyncAfterLoaded()
        {
            OldJsonContent.PreviewMouseWheel += OnEditorMouseWheel;
            NewJsonContent.PreviewMouseWheel += OnEditorMouseWheel;
        }

        private void OnEditorMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_isScrollingSynced) return;

            _isScrollingSynced = true;
            try
            {
                var currentOffset = ((ICSharpCode.AvalonEdit.TextEditor)sender).VerticalOffset;
                var scrollLines = SystemParameters.WheelScrollLines;
                var lineHeight = DiffColorsHelper.VisualSettings.StandardLineHeight;
                var scrollAmount = (e.Delta > 0 ? -1 : 1) * scrollLines * lineHeight;
                var newOffset = Math.Max(0, currentOffset + scrollAmount);

                OldJsonContent.ScrollToVerticalOffset(newOffset);
                NewJsonContent.ScrollToVerticalOffset(newOffset);

                e.Handled = true;
            }
            finally
            {
                _isScrollingSynced = false;
            }
        }
    }

    public class DiffBackgroundRenderer : IBackgroundRenderer
    {
        private readonly List<ChangeType> _lineTypes;

        public DiffBackgroundRenderer(List<ChangeType> lineTypes)
        {
            _lineTypes = lineTypes;
        }

        public KnownLayer Layer => KnownLayer.Background;

        public void Draw(TextView textView, DrawingContext drawingContext)
        { 
            if (_lineTypes == null) return;

            foreach (var line in textView.VisualLines)
            {
                var lineNumber = line.FirstDocumentLine.LineNumber - 1;
                if (lineNumber < 0 || lineNumber >= _lineTypes.Count) continue;

                var backgroundColor = DiffColorsHelper.GetBackgroundColor(_lineTypes[lineNumber]);
                if (backgroundColor == Colors.Transparent) continue;

                var backgroundBrush = new SolidColorBrush(backgroundColor);
                var rect = new Rect(0, line.VisualTop - textView.ScrollOffset.Y, 
                                  textView.ActualWidth, line.Height);
                drawingContext.DrawRectangle(backgroundBrush, null, rect);
            }
        }
    }
}