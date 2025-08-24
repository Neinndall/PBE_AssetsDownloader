using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Media.Animation;
using ICSharpCode.AvalonEdit.Document;
using PBE_AssetsManager.UI.Helpers;
using PBE_AssetsManager.Services;

namespace PBE_AssetsManager.UI.Dialogs
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

            var originalStoryboard = (Storyboard)FindResource("SpinningIconAnimation");
            _loadingAnimation = originalStoryboard.Clone();
            Storyboard.SetTarget(_loadingAnimation, ProgressIcon);
            _loadingAnimation.Begin();
        }

        public async Task LoadAndDisplayDiffAsync(string oldJson, string newJson, string oldFileName, string newFileName)
        {
            try
            {
                OldFileNameLabel.Text = oldFileName;
                NewFileNameLabel.Text = newFileName;

                if (string.IsNullOrEmpty(oldJson))
                {
                    HandleEmptyOldJson(newJson);
                    return;
                }

                _originalDiffModel = await Task.Run(() => new SideBySideDiffBuilder(new Differ()).BuildDiffModel(oldJson, newJson, false));

                LoadingOverlay.Visibility = Visibility.Collapsed;
                _loadingAnimation.Stop();
                DiffGrid.Visibility = Visibility.Visible;

                UpdateDiffView();

                if (AreFilesIdentical())
                {
                    _customMessageBoxService.ShowInfo("Comparison Result", "No differences found. The two files are identical.", this);
                    Close();
                }
            }
            catch (Exception ex)
            {
                _customMessageBoxService.ShowError("Error", $"Failed to load comparison: {ex.Message}. Check logs for details.", this);
                Close();
            }
        }

        public async Task LoadAndDisplayDiffAsync(string oldFilePath, string newFilePath)
        {
            string oldJson = File.Exists(oldFilePath) ? await File.ReadAllTextAsync(oldFilePath) : "";
            string newJson = File.Exists(newFilePath) ? await File.ReadAllTextAsync(newFilePath) : "";
            await LoadAndDisplayDiffAsync(oldJson, newJson, Path.GetFileName(oldFilePath), Path.GetFileName(newFilePath));
        }

        private void HandleEmptyOldJson(string newJson)
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            _loadingAnimation.Stop();
            DiffGrid.Visibility = Visibility.Visible;
            OldJsonContent.Document = new TextDocument("");
            NewJsonContent.Document = new TextDocument(newJson);
            NextDiffButton.IsEnabled = false;
            PreviousDiffButton.IsEnabled = false;
            WordLevelDiffButton.IsEnabled = false;
            HideUnchangedButton.IsEnabled = false;
            OldNavigationPanel.Visibility = Visibility.Collapsed;
            NewNavigationPanel.Visibility = Visibility.Collapsed;
        }

        private bool AreFilesIdentical()
        {
            return _originalDiffModel.OldText.Lines.All(l => l.Type == ChangeType.Unchanged) &&
                   _originalDiffModel.NewText.Lines.All(l => l.Type == ChangeType.Unchanged);
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
                // Silently continue
            }
        }

        private void UpdateDiffView(int? diffIndexToRestore = null)
        {
            if (_originalDiffModel == null) return;

            var modelToShow = _hideUnchangedLines ? FilterDiffModel(_originalDiffModel) : _originalDiffModel;
            var originalModelForNav = _hideUnchangedLines ? _originalDiffModel : null;

            var normalizedOld = JsonDiffHelper.NormalizeTextForAlignment(modelToShow.OldText);
            var normalizedNew = JsonDiffHelper.NormalizeTextForAlignment(modelToShow.NewText);

            OldJsonContent.Document = new TextDocument(normalizedOld.Text);
            NewJsonContent.Document = new TextDocument(normalizedNew.Text);

            OldJsonContent.UpdateLayout();
            NewJsonContent.UpdateLayout();

            ApplyDiffHighlighting(modelToShow);

            _diffPanelNavigation = new DiffPanelNavigation(OldNavigationPanel, NewNavigationPanel, OldJsonContent, NewJsonContent, modelToShow, originalModelForNav);
            _diffPanelNavigation.ScrollRequested += ScrollToLine;

            EventHandler layoutUpdatedHandler = null;
            layoutUpdatedHandler = (s, e) =>
            {
                NewJsonContent.TextArea.TextView.LayoutUpdated -= layoutUpdatedHandler;
                if (diffIndexToRestore.HasValue && diffIndexToRestore.Value != -1)
                {
                    _diffPanelNavigation?.NavigateToDifferenceByIndex(diffIndexToRestore.Value);
                }
                else
                {
                    _diffPanelNavigation?.NavigateToNextDifference(0);
                }
            };
            NewJsonContent.TextArea.TextView.LayoutUpdated += layoutUpdatedHandler;
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

                if (_diffPanelNavigation != null) _diffPanelNavigation.CurrentLine = lineNumber;

                UpdateLayout();

                _diffPanelNavigation?.DrawPanels();

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

            OldJsonContent.TextArea.TextView.ScrollOffsetChanged += (s, e) => _diffPanelNavigation?.DrawPanels();
            NewJsonContent.TextArea.TextView.ScrollOffsetChanged += (s, e) => _diffPanelNavigation?.DrawPanels();
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

            var modelToShow = _hideUnchangedLines ? FilterDiffModel(_originalDiffModel) : _originalDiffModel;
            ApplyDiffHighlighting(modelToShow);

            OldJsonContent.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
            NewJsonContent.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        }

        private void HideUnchangedButton_Click(object sender, RoutedEventArgs e)
        {
            _hideUnchangedLines = HideUnchangedButton.IsChecked ?? false;
            int currentDiffIndex = _diffPanelNavigation?.FindClosestDifferenceIndex(NewJsonContent.TextArea.Caret.Line) ?? -1;
            UpdateDiffView(currentDiffIndex);
        }
    }
}
