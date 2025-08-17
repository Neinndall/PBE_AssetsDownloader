using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DiffPlex.DiffBuilder.Model;
using ICSharpCode.AvalonEdit;

namespace PBE_AssetsDownloader.UI.Helpers
{
    public class DiffPanelNavigation
    {
        private readonly Canvas _oldPanel;
        private readonly Canvas _newPanel;
        private readonly TextEditor _oldEditor;
        private readonly TextEditor _newEditor;
        private readonly SideBySideDiffModel _diffModel;
        private readonly SideBySideDiffModel _originalDiffModel;
        private bool _isDragging;
        private Point _dragStartPoint;
        private bool _wasActuallyDragged;
        private readonly List<int> _diffLines;
        public bool IsFilteredView { get; set; }
        public int CurrentLine { get; set; }

        // Brush for the overall background of the navigation panels
        private readonly SolidColorBrush _backgroundPanelBrush;

        // Brushes for the diff markers WITHIN the navigation panels
        private readonly SolidColorBrush _addedBrush;
        private readonly SolidColorBrush _removedBrush;
        private readonly SolidColorBrush _modifiedBrush;
        private readonly SolidColorBrush _imaginaryBrush;
        private readonly SolidColorBrush _viewportBrush; 

        public event Action<int> ScrollRequested;

        public DiffPanelNavigation(Canvas oldPanel, Canvas newPanel, TextEditor oldEditor, TextEditor newEditor, SideBySideDiffModel diffModel, SideBySideDiffModel originalDiffModel = null)
        {
            _oldPanel = oldPanel;
            _newPanel = newPanel;
            _oldEditor = oldEditor;
            _newEditor = newEditor;
            _diffModel = diffModel;
            _originalDiffModel = originalDiffModel ?? diffModel;
            _diffLines = new List<int>();

            // Cache brushes for performance
            _backgroundPanelBrush = new SolidColorBrush((Color)Application.Current.FindResource("BackgroundPanelNavigation"));

            _addedBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationAdded"));
            _removedBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationRemoved"));
            _modifiedBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationModified"));
            _imaginaryBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationImaginary"));
            _viewportBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationViewPort"));
 
            _backgroundPanelBrush.Freeze();
            _addedBrush.Freeze();
            _removedBrush.Freeze();
            _modifiedBrush.Freeze();
            _imaginaryBrush.Freeze();
            _viewportBrush.Freeze();

            SetupEvents();
            FindDiffLines();
        }

        private void SetupEvents()
        {
            _oldPanel.MouseLeftButtonDown += NavigationPanel_MouseLeftButtonDown;
            _oldPanel.MouseMove += NavigationPanel_MouseMove;
            _oldPanel.MouseLeftButtonUp += NavigationPanel_MouseLeftButtonUp;
            _oldPanel.SizeChanged += (s, e) => DrawPanels();

            _newPanel.MouseLeftButtonDown += NavigationPanel_MouseLeftButtonDown;
            _newPanel.MouseMove += NavigationPanel_MouseMove;
            _newPanel.MouseLeftButtonUp += NavigationPanel_MouseLeftButtonUp;
            _newPanel.SizeChanged += (s, e) => DrawPanels();
        }

        private void FindDiffLines()
        {
            if (_diffModel == null) return;

            ChangeType lastChangeType = ChangeType.Unchanged;
            for (int i = 0; i < _diffModel.NewText.Lines.Count; i++)
            {
                var currentLine = _diffModel.NewText.Lines[i];
                if (currentLine.Type != ChangeType.Unchanged && currentLine.Type != lastChangeType)
                {
                    _diffLines.Add(i + 1);
                }
                lastChangeType = currentLine.Type;
            }

            lastChangeType = ChangeType.Unchanged;
            for (int i = 0; i < _diffModel.OldText.Lines.Count; i++)
            {
                var currentLine = _diffModel.OldText.Lines[i];
                if (currentLine.Type != ChangeType.Unchanged && currentLine.Type != lastChangeType)
                {
                    if (!_diffLines.Contains(i + 1))
                    {
                        _diffLines.Add(i + 1);
                    }
                }
                lastChangeType = currentLine.Type;
            }

            _diffLines.Sort();
        }

        public void NavigateToNextDifference(int currentLine)
        {
            if (_diffLines.Count == 0) return;
            var nextDiffLine = _diffLines.FirstOrDefault(line => line > currentLine);
            if (nextDiffLine == 0) nextDiffLine = _diffLines[0];
            ScrollToLine(nextDiffLine);
        }

        public void NavigateToPreviousDifference(int currentLine)
        {
            if (_diffLines.Count == 0) return;
            var previousDiffLine = _diffLines.LastOrDefault(line => line < currentLine);
            if (previousDiffLine == 0) previousDiffLine = _diffLines.Last();
            ScrollToLine(previousDiffLine);
        }

        public void NavigateToDifferenceByIndex(int index)
        {
            if (index >= 0 && index < _diffLines.Count)
            {
                var lineToScrollTo = _diffLines[index];
                ScrollToLine(lineToScrollTo);
            }
            else if (_diffLines.Count > 0)
            {
                ScrollToLine(_diffLines[0]);
            }
        }

        public int FindClosestDifferenceIndex(int currentLine)
        {
            if (_diffLines.Count == 0) return -1;

            var nextDiffLine = _diffLines.FirstOrDefault(line => line >= currentLine);

            if (nextDiffLine != 0)
            {
                return _diffLines.IndexOf(nextDiffLine);
            }
            else
            {
                return _diffLines.Count - 1;
            }
        }

        private void ScrollToLine(int lineNumber)
        {
            ScrollRequested?.Invoke(lineNumber);
        }

        public void DrawPanels()
        {
            _oldPanel.Children.Clear();
            _newPanel.Children.Clear();

            _oldPanel.Background = _backgroundPanelBrush;
            _newPanel.Background = _backgroundPanelBrush;

            if (_diffModel?.OldText?.Lines == null) return;

            var panelHeight = _oldPanel.ActualHeight > 0 ? _oldPanel.ActualHeight : 600;
            var totalLines = Math.Max(_diffModel.OldText.Lines.Count, _diffModel.NewText.Lines.Count);
            if (totalLines == 0) return;
            var lineHeight = panelHeight / totalLines;

            DrawPanelContent(_oldPanel, _diffModel.OldText.Lines, lineHeight);
            DrawPanelContent(_newPanel, _diffModel.NewText.Lines, lineHeight);

            if (IsFilteredView)
            {
                // In filtered view, draw a highlighter for the current line.
                if (CurrentLine > 0)
                {
                    var lineIndex = CurrentLine - 1;
                    if (lineIndex >= 0 && lineIndex < totalLines)
                    {
                        var highlighterTop = lineIndex * lineHeight;

                        var oldHighlighterRect = new Rectangle
                        {
                            Width = _oldPanel.ActualWidth,
                            Height = Math.Max(2.0, lineHeight),
                            Fill = _viewportBrush,
                            IsHitTestVisible = false
                        };
                        Canvas.SetTop(oldHighlighterRect, highlighterTop);
                        _oldPanel.Children.Add(oldHighlighterRect);
                
                        var newHighlighterRect = new Rectangle
                        {
                            Width = _newPanel.ActualWidth,
                            Height = Math.Max(2.0, lineHeight),
                            Fill = _viewportBrush,
                            IsHitTestVisible = false
                        };
                        Canvas.SetTop(newHighlighterRect, highlighterTop);
                        _newPanel.Children.Add(newHighlighterRect);
                    }
                }
            }
            else
            {
                // In unfiltered view, draw the original viewport guide.
                if (_newEditor.ExtentHeight > 0)
                {
                    var viewportRatio = _newEditor.ViewportHeight / _newEditor.ExtentHeight;
                    var offsetRatio = _newEditor.VerticalOffset / _newEditor.ExtentHeight;

                    var viewportHeight = panelHeight * viewportRatio;
                    var viewportTop = panelHeight * offsetRatio;

                    // Create guide for the left panel
                    var oldViewportRect = new Rectangle
                    {
                        Width = _oldPanel.ActualWidth,
                        Height = Math.Max(2.0, viewportHeight),
                        Fill = _viewportBrush,
                        IsHitTestVisible = false
                    };

                    // Create guide for the right panel
                    var newViewportRect = new Rectangle
                    {
                        Width = _newPanel.ActualWidth,
                        Height = Math.Max(2.0, viewportHeight),
                        Fill = _viewportBrush,
                        IsHitTestVisible = false
                    };

                    Canvas.SetTop(oldViewportRect, viewportTop);
                    _oldPanel.Children.Add(oldViewportRect);

                    Canvas.SetTop(newViewportRect, viewportTop);
                    _newPanel.Children.Add(newViewportRect);
                }
            }
        }

        private void DrawPanelContent(Canvas panel, IReadOnlyList<DiffPiece> lines, double lineHeight)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var brush = GetBrushForChangeType(lines[i].Type);
                if (brush == null) continue;

                var rect = new Rectangle
                {
                    Width = panel.ActualWidth,
                    Height = Math.Max(2.0, lineHeight),
                    Fill = brush,
                    IsHitTestVisible = false
                };

                Canvas.SetTop(rect, i * lineHeight);
                panel.Children.Add(rect);
            }
        }

        private SolidColorBrush GetBrushForChangeType(ChangeType changeType)
        {
            return changeType switch
            {
                ChangeType.Inserted => _addedBrush,
                ChangeType.Deleted => _removedBrush,
                ChangeType.Modified => _modifiedBrush,
                ChangeType.Imaginary => _imaginaryBrush,
                _ => null
            };
        }

        private void NavigationPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Canvas panel)
            {
                _isDragging = true;
                _wasActuallyDragged = false;
                _dragStartPoint = e.GetPosition(panel);
                panel.CaptureMouse();
            }
        }

        private void NavigationPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && sender is Canvas panel)
            {
                var currentPosition = e.GetPosition(panel);
                var dragVector = _dragStartPoint - currentPosition;

                if (!_wasActuallyDragged &&
                    (Math.Abs(dragVector.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(dragVector.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    _wasActuallyDragged = true;
                }

                if (_wasActuallyDragged)
                {
                    HandleNavigation(panel, currentPosition.Y);
                }
            }
        }

        private void NavigationPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && sender is Canvas panel)
            {
                if (!_wasActuallyDragged)
                {
                    NavigateToClosestDifference(panel, e.GetPosition(panel).Y);
                }
                _isDragging = false;
                panel.ReleaseMouseCapture();
            }
        }

        private void NavigateToClosestDifference(Canvas panel, double y)
        {
            if (!_diffLines.Any()) return;

            var totalLines = Math.Max(_diffModel.OldText.Lines.Count, _diffModel.NewText.Lines.Count);
            var panelHeight = panel.ActualHeight;
            if (panelHeight <= 0) return;

            var clickedLine = (int)((y / panelHeight) * totalLines) + 1;
            var closestDiffLine = _diffLines.OrderBy(diffLine => Math.Abs(diffLine - clickedLine)).First();
            ScrollToLine(closestDiffLine);
        }

        private void HandleNavigation(Canvas panel, double y)
        {
            var totalLines = Math.Max(_diffModel.OldText.Lines.Count, _diffModel.NewText.Lines.Count);
            var panelHeight = panel.ActualHeight;
            if (panelHeight <= 0) return;
            var lineNumber = (int)((y / panelHeight) * totalLines) + 1;
            ScrollRequested?.Invoke(lineNumber);
        }
    }
}