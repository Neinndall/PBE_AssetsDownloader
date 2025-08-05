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
using System.Linq;
using PBE_AssetsDownloader.UI.Helpers;
using PBE_AssetsDownloader.UI.Dialogs;

namespace PBE_AssetsDownloader.UI
{
    public partial class JsonDiffWindow : Window
    {
        private SideBySideDiffModel _diffModel;
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
            var formattedOldJson = JsonDiffHelper.FormatJson(oldJson);
            var formattedNewJson = JsonDiffHelper.FormatJson(newJson);

            await Task.Run(() =>
            {
                var differ = new Differ();
                var diffBuilder = new SideBySideDiffBuilder(differ);
                _diffModel = diffBuilder.BuildDiffModel(formattedOldJson, formattedNewJson);
            });

            // Check if there are any differences
            if (_diffModel.OldText.Lines.All(l => l.Type == ChangeType.Unchanged) && 
                _diffModel.NewText.Lines.All(l => l.Type == ChangeType.Unchanged))
            {
                Dispatcher.Invoke(() =>
                {
                    CustomMessageBox.ShowInfo("Comparison Result", "No differences found. The two files are identical.", this, CustomMessageBoxIcon.Info);
                    Close();
                });
                return;
            }

            var normalizedOld = JsonDiffHelper.NormalizeTextForAlignment(_diffModel.OldText);
            var normalizedNew = JsonDiffHelper.NormalizeTextForAlignment(_diffModel.NewText);

            OldJsonContent.Text = normalizedOld.Text;
            NewJsonContent.Text = normalizedNew.Text;

            ApplyDiffHighlighting(normalizedOld.LineTypes, normalizedNew.LineTypes);
            
            _diffPanelNavigation = new DiffPanelNavigation(OldNavigationPanel, NewNavigationPanel, _diffModel);
            _diffPanelNavigation.ScrollRequested += ScrollToLine;
            _diffPanelNavigation.DrawPanels();

            // Automatically scroll to the first difference on load
            if (_diffPanelNavigation != null)
            {
                _diffPanelNavigation.NavigateToNextDifference(0);
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
            OldJsonContent.TextArea.TextView.ScrollOffsetChanged -= OldEditor_ScrollChanged;
            NewJsonContent.TextArea.TextView.ScrollOffsetChanged -= NewEditor_ScrollChanged;

            try
            {
                // Scroll both editors to the target line
                OldJsonContent.ScrollTo(lineNumber, 0);
                NewJsonContent.ScrollTo(lineNumber, 0);

                // Move the caret to the target line and focus the editor
                NewJsonContent.TextArea.Caret.Line = lineNumber;
                NewJsonContent.TextArea.Caret.Column = 1;
                NewJsonContent.Focus();
            }
            finally
            {
                OldJsonContent.TextArea.TextView.ScrollOffsetChanged += OldEditor_ScrollChanged;
                NewJsonContent.TextArea.TextView.ScrollOffsetChanged += NewEditor_ScrollChanged;
            }
        }

        private void SetupScrollSync()
        {
            OldJsonContent.Loaded += (_, _) => SetupScrollSyncAfterLoaded();
        }

        private void SetupScrollSyncAfterLoaded()
        {
            OldJsonContent.TextArea.TextView.ScrollOffsetChanged += OldEditor_ScrollChanged;
            NewJsonContent.TextArea.TextView.ScrollOffsetChanged += NewEditor_ScrollChanged;
        }

        private void OldEditor_ScrollChanged(object sender, EventArgs e)
        {
            NewJsonContent.TextArea.TextView.ScrollOffsetChanged -= NewEditor_ScrollChanged;
            try
            {
                var sourceView = (TextView)sender;
                NewJsonContent.ScrollToVerticalOffset(sourceView.VerticalOffset);
                NewJsonContent.ScrollToHorizontalOffset(sourceView.HorizontalOffset);
            }
            finally
            {
                NewJsonContent.TextArea.TextView.ScrollOffsetChanged += NewEditor_ScrollChanged;
            }
        }

        private void NewEditor_ScrollChanged(object sender, EventArgs e)
        {
            OldJsonContent.TextArea.TextView.ScrollOffsetChanged -= OldEditor_ScrollChanged;
            try
            {
                var sourceView = (TextView)sender;
                OldJsonContent.ScrollToVerticalOffset(sourceView.VerticalOffset);
                OldJsonContent.ScrollToHorizontalOffset(sourceView.HorizontalOffset);
            }
            finally
            {
                OldJsonContent.TextArea.TextView.ScrollOffsetChanged += OldEditor_ScrollChanged;
            }
        }

        private void NextDiffButton_Click(object sender, RoutedEventArgs e)
        {
            _diffPanelNavigation?.NavigateToNextDifference(NewJsonContent.TextArea.Caret.Line);
        }

        private void PreviousDiffButton_Click(object sender, RoutedEventArgs e)
        {
            _diffPanelNavigation?.NavigateToPreviousDifference(NewJsonContent.TextArea.Caret.Line);
        }
    }
}