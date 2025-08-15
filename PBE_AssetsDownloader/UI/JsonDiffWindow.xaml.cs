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
        private SideBySideDiffModel _diffModel;
        private DiffPanelNavigation _diffPanelNavigation;
        private bool _isWordLevelDiff = false;
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
            _ = DisplayDiffAsync(oldFilePath, newFilePath);
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

        private async Task DisplayDiffAsync(string oldFilePath, string newFilePath)
        {
            try
            {
                var result = await Task.Run(() =>
                {
                    if (!File.Exists(oldFilePath))
                    {
                        // Optimization: If old file doesn't exist, build the model manually.
                        string newJson = File.Exists(newFilePath) ? File.ReadAllText(newFilePath) : "";
                        var newLines = newJson.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                        var diffModel = new SideBySideDiffModel();
                        
                        for (int i = 0; i < newLines.Length; i++)
                        {
                            diffModel.NewText.Lines.Add(new DiffPiece(newLines[i], ChangeType.Inserted, i + 1));
                        }
                        
                        return new { diffModel, OldText = "", NewText = newJson };
                    }
                    else
                    {
                        // Original path for actual comparisons
                        string oldJson = File.ReadAllText(oldFilePath);
                        string newJson = File.Exists(newFilePath) ? File.ReadAllText(newFilePath) : "";

                        var diffBuilder = new SideBySideDiffBuilder(new Differ());
                        var diffModel = diffBuilder.BuildDiffModel(oldJson, newJson, false);

                        var normalizedOld = JsonDiffHelper.NormalizeTextForAlignment(diffModel.OldText);
                        var normalizedNew = JsonDiffHelper.NormalizeTextForAlignment(diffModel.NewText);

                        return new { diffModel, OldText = normalizedOld.Text, NewText = normalizedNew.Text };
                    }
                });

                _diffModel = result.diffModel;

                // This part runs on the UI thread and will freeze on large files.
                OldJsonContent.Document = new TextDocument(result.OldText);
                NewJsonContent.Document = new TextDocument(result.NewText);

                // Hide loading and show diff
                LoadingOverlay.Visibility = Visibility.Collapsed;
                _loadingAnimation.Stop();
                DiffGrid.Visibility = Visibility.Visible;

                bool hasChanges = _diffModel.OldText.Lines.Any(l => l.Type == ChangeType.Deleted || l.Type == ChangeType.Modified) ||
                                  _diffModel.NewText.Lines.Any(l => l.Type == ChangeType.Inserted || l.Type == ChangeType.Modified);

                if (!hasChanges)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _customMessageBoxService.ShowInfo("Comparison Result", "No differences found. The two files are identical.", this, CustomMessageBoxIcon.Info);
                        Close();
                    });
                    return;
                }

                OldJsonContent.UpdateLayout();
                NewJsonContent.UpdateLayout();

                ApplyDiffHighlighting();

                _diffPanelNavigation = new DiffPanelNavigation(OldNavigationPanel, NewNavigationPanel, _diffModel);
                _diffPanelNavigation.ScrollRequested += ScrollToLine;
                _diffPanelNavigation.DrawPanels();

                if (_diffPanelNavigation != null)
                {
                    _diffPanelNavigation.NavigateToNextDifference(0);
                }
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

        private void ApplyDiffHighlighting()
        {
            OldJsonContent.TextArea.TextView.BackgroundRenderers.Clear();
            NewJsonContent.TextArea.TextView.BackgroundRenderers.Clear();

            OldJsonContent.TextArea.TextView.BackgroundRenderers.Add(new DiffBackgroundRenderer(_diffModel, _isWordLevelDiff, true));
            NewJsonContent.TextArea.TextView.BackgroundRenderers.Add(new DiffBackgroundRenderer(_diffModel, _isWordLevelDiff, false));
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
            ApplyDiffHighlighting();
            OldJsonContent.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
            NewJsonContent.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        }
    }
}