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
using PBE_AssetsDownloader.Services;
using System.Windows.Media.Animation;
using ICSharpCode.AvalonEdit.Document;

namespace PBE_AssetsDownloader.UI
{
    public partial class JsonDiffWindow : Window
    {
        private SideBySideDiffModel _originalDiffModel;
        private DiffPanelNavigation _diffPanelNavigation;
        private bool _isWordLevelDiff = false;
        private bool _hideUnchangedLines = false;
        private readonly CustomMessageBoxService _customMessageBoxService;
        private readonly Storyboard _loadingAnimation;

        public JsonDiffWindow(CustomMessageBoxService customMessageBoxService)
        {
            InitializeComponent();
            _customMessageBoxService = customMessageBoxService;
            ConfigureEditors();
            LoadJsonSyntaxHighlighting();
            SetupScrollSync();

            _loadingAnimation = (Storyboard)LoadingOverlay.Resources["SpinningAnimation"];
            _loadingAnimation.Begin();
        }

        public void LoadDiff(string oldFilePath, string newFilePath)
        {
            _ = LoadAndDisplayDiffAsync(oldFilePath, newFilePath);
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
                editor.FontFamily = new FontFamily("Consolas, Courier New, monospace");
                editor.FontSize = 13;
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

        private async Task LoadAndDisplayDiffAsync(string oldFilePath, string newFilePath)
        {
            try
            {
                _originalDiffModel = await Task.Run(() =>
                {
                    string oldJson = File.Exists(oldFilePath) ? File.ReadAllText(oldFilePath) : "";
                    string newJson = File.Exists(newFilePath) ? File.ReadAllText(newFilePath) : "";
                    var diffBuilder = new SideBySideDiffBuilder(new Differ());
                    return diffBuilder.BuildDiffModel(oldJson, newJson, false);
                });

                // Hide loading and show diff
                LoadingOverlay.Visibility = Visibility.Collapsed;
                _loadingAnimation.Stop();
                DiffGrid.Visibility = Visibility.Visible;

                UpdateDiffView();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    _customMessageBoxService.ShowInfo("Error", $"Failed to load comparison: {ex.Message}. Check logs for details.", this, CustomMessageBoxIcon.Error);
                    Close();
                });
            }
        }

        private void UpdateDiffView()
        {
            if (_originalDiffModel == null) return;

            var modelToShow = _hideUnchangedLines ? FilterDiffModel(_originalDiffModel) : _originalDiffModel;

            var normalizedOld = JsonDiffHelper.NormalizeTextForAlignment(modelToShow.OldText);
            var normalizedNew = JsonDiffHelper.NormalizeTextForAlignment(modelToShow.NewText);

            OldJsonContent.Document = new TextDocument(normalizedOld.Text);
            NewJsonContent.Document = new TextDocument(normalizedNew.Text);

            // Para que el salto a la primera linea de diferencias no sea brusca
            OldJsonContent.UpdateLayout();
            NewJsonContent.UpdateLayout();
            
            ApplyDiffHighlighting(modelToShow);

            // Clean up previous instance before creating a new one
            if (_diffPanelNavigation != null)
            {
                _diffPanelNavigation.ScrollRequested -= ScrollToLine;
            }
            OldNavigationPanel.Children.Clear();
            NewNavigationPanel.Children.Clear();

            _diffPanelNavigation = new DiffPanelNavigation(OldNavigationPanel, NewNavigationPanel, modelToShow);
            _diffPanelNavigation.ScrollRequested += ScrollToLine;
            _diffPanelNavigation.DrawPanels();

            // Scroll to the first difference automatically on load.
            if (_diffPanelNavigation != null)
            {
                _diffPanelNavigation.NavigateToNextDifference(0);
            }
            
            if (_hideUnchangedLines)
            {
                _diffPanelNavigation?.NavigateToNextDifference(0);
            }
        }

        private SideBySideDiffModel FilterDiffModel(SideBySideDiffModel originalModel)
        {
            var filteredModel = new SideBySideDiffModel();
            for (int i = 0; i < originalModel.OldText.Lines.Count; i++)
            {
                var oldLine = originalModel.OldText.Lines[i];
                var newLine = originalModel.NewText.Lines[i];

                if (oldLine.Type != ChangeType.Unchanged || newLine.Type != ChangeType.Unchanged)
                {
                    filteredModel.OldText.Lines.Add(oldLine);
                    filteredModel.NewText.Lines.Add(newLine);
                }
            }
            return filteredModel;
        }

        private void ApplyDiffHighlighting(SideBySideDiffModel diffModel)
        {
            OldJsonContent.TextArea.TextView.BackgroundRenderers.Clear();
            NewJsonContent.TextArea.TextView.BackgroundRenderers.Clear();

            OldJsonContent.TextArea.TextView.BackgroundRenderers.Add(new DiffBackgroundRenderer(diffModel, _isWordLevelDiff, true));
            NewJsonContent.TextArea.TextView.BackgroundRenderers.Add(new DiffBackgroundRenderer(diffModel, _isWordLevelDiff, false));
        }

        private void ScrollToLine(int lineNumber)
        {
            OldJsonContent.TextArea.TextView.ScrollOffsetChanged -= OldEditor_ScrollChanged;
            NewJsonContent.TextArea.TextView.ScrollOffsetChanged -= NewEditor_ScrollChanged;

            try
            {
                OldJsonContent.ScrollTo(lineNumber, 0);
                NewJsonContent.ScrollTo(lineNumber, 0);

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
                var newVerticalOffset = Math.Min(sourceView.VerticalOffset, NewJsonContent.ExtentHeight - NewJsonContent.ViewportHeight);
                var newHorizontalOffset = Math.Min(sourceView.HorizontalOffset, NewJsonContent.ExtentWidth - NewJsonContent.ViewportWidth);
                NewJsonContent.ScrollToVerticalOffset(newVerticalOffset);
                NewJsonContent.ScrollToHorizontalOffset(newHorizontalOffset);
            }
            finally
            {
                NewJsonContent.TextArea.TextView.ScrollOffsetChanged += NewEditor_ScrollChanged;
            }

            _diffPanelNavigation?.UpdateViewScroll(
                OldJsonContent.VerticalOffset,
                OldJsonContent.ExtentHeight - OldJsonContent.ViewportHeight,
                OldJsonContent.ViewportHeight);
        }

        private void NewEditor_ScrollChanged(object sender, EventArgs e)
        {
            OldJsonContent.TextArea.TextView.ScrollOffsetChanged -= OldEditor_ScrollChanged;
            try
            {
                var sourceView = (TextView)sender;
                var newVerticalOffset = Math.Min(sourceView.VerticalOffset, OldJsonContent.ExtentHeight - OldJsonContent.ViewportHeight);
                var newHorizontalOffset = Math.Min(sourceView.HorizontalOffset, OldJsonContent.ExtentWidth - OldJsonContent.ViewportWidth);
                OldJsonContent.ScrollToVerticalOffset(newVerticalOffset);
                OldJsonContent.ScrollToHorizontalOffset(newHorizontalOffset);
            }
            finally
            {
                OldJsonContent.TextArea.TextView.ScrollOffsetChanged += OldEditor_ScrollChanged;
            }

            _diffPanelNavigation?.UpdateViewScroll(
                NewJsonContent.VerticalOffset,
                NewJsonContent.ExtentHeight - NewJsonContent.ViewportHeight,
                NewJsonContent.ViewportHeight);
        }

        private void NextDiffButton_Click(object sender, RoutedEventArgs e)
        {
            _diffPanelNavigation?.NavigateToNextDifference(NewJsonContent.TextArea.Caret.Line);
        }

        private void PreviousDiffButton_Click(object sender, RoutedEventArgs e)
        {
            _diffPanelNavigation?.NavigateToPreviousDifference(NewJsonContent.TextArea.Caret.Line);
        }

        private void WordLevelDiffButton_Click(object sender, RoutedEventArgs e)
        {
            _isWordLevelDiff = WordLevelDiffButton.IsChecked ?? false;
            UpdateDiffView();
        }

        private void HideUnchangedButton_Click(object sender, RoutedEventArgs e)
        {
            _hideUnchangedLines = HideUnchangedButton.IsChecked ?? false;
            UpdateDiffView();
        }
    }
}
