using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DiffPlex.DiffBuilder.Model;

namespace PBE_AssetsDownloader.UI.Helpers
{
    public class DiffPanelNavigation
    {
        private readonly Canvas _oldPanel;
        private readonly Canvas _newPanel;
        private readonly SideBySideDiffModel _diffModel;
        private bool _isDragging;
        private Point _dragStartPoint;
        private bool _wasActuallyDragged;
        private readonly List<int> _diffLines;

        // Brush for the overall background of the navigation panels
        private readonly SolidColorBrush _backgroundPanelBrush;

        // Brushes for the diff markers WITHIN the navigation panels
        private readonly SolidColorBrush _removedBrush;
        private readonly SolidColorBrush _addedBrush;
        private readonly SolidColorBrush _modifiedBrush;
        private readonly SolidColorBrush _imaginaryBrush;

        private readonly Canvas _oldMarkerContainer;
        private readonly Canvas _newMarkerContainer;
        private readonly TranslateTransform _oldTransform = new TranslateTransform();
        private readonly TranslateTransform _newTransform = new TranslateTransform();
        private double _virtualHeight;
        private const double MarkerHeight = 3.0;

        public event Action<int> ScrollRequested;

        public DiffPanelNavigation(Canvas oldPanel, Canvas newPanel, SideBySideDiffModel diffModel)
        {
            _oldPanel = oldPanel;
            _newPanel = newPanel;
            _diffModel = diffModel;
            _diffLines = new List<int>();

            // Initialize containers and transforms
            _oldMarkerContainer = new Canvas { RenderTransform = _oldTransform };
            _newMarkerContainer = new Canvas { RenderTransform = _newTransform };
            _oldPanel.Children.Add(_oldMarkerContainer);
            _newPanel.Children.Add(_newMarkerContainer);

            // Cache brushes for performance
            _backgroundPanelBrush = new SolidColorBrush((Color)Application.Current.FindResource("BackgroundPanelNavigation"));
            _removedBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationRemoved"));
            _addedBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationAdded"));
            _modifiedBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationModified"));
            _imaginaryBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationImaginary"));

            _backgroundPanelBrush.Freeze();
            _removedBrush.Freeze();
            _addedBrush.Freeze();
            _modifiedBrush.Freeze();
            _imaginaryBrush.Freeze();

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

        public void UpdateViewScroll(double verticalOffset, double scrollableHeight, double viewportHeight)
        {
            if (_virtualHeight <= _oldPanel.ActualHeight) return;

            var scrollRatio = verticalOffset / scrollableHeight;
            var newY = -scrollRatio * (_virtualHeight - _oldPanel.ActualHeight);

            _oldTransform.Y = newY;
            _newTransform.Y = newY;
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

        private void ScrollToLine(int lineNumber)
        {
            ScrollRequested?.Invoke(lineNumber);
        }

        public void DrawPanels()
        {
            _oldMarkerContainer.Children.Clear();
            _newMarkerContainer.Children.Clear();

            _oldPanel.Background = _backgroundPanelBrush;
            _newPanel.Background = _backgroundPanelBrush;

            if (_diffModel?.OldText?.Lines == null) return;

            var totalLines = Math.Max(_diffModel.OldText.Lines.Count, _diffModel.NewText.Lines.Count);
            if (totalLines == 0) return;

            _virtualHeight = totalLines * MarkerHeight;

            DrawPanelContent(_oldMarkerContainer, _diffModel.OldText.Lines);
            DrawPanelContent(_newMarkerContainer, _diffModel.NewText.Lines);
        }

        private void DrawPanelContent(Canvas container, IReadOnlyList<DiffPiece> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var brush = GetBrushForChangeType(lines[i].Type);
                if (brush == null) continue;

                var rect = new Rectangle
                {
                    Width = _oldPanel.ActualWidth, // Use panel width
                    Height = MarkerHeight,
                    Fill = brush,
                    IsHitTestVisible = false
                };

                Canvas.SetTop(rect, i * MarkerHeight);
                container.Children.Add(rect);
            }
        }

        private SolidColorBrush GetBrushForChangeType(ChangeType changeType)
        {
            return changeType switch
            {
                ChangeType.Deleted => _removedBrush,
                ChangeType.Inserted => _addedBrush,
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
            if (!_isDragging || !(sender is Canvas panel)) return;

            var currentPosition = e.GetPosition(panel);
            var dragVector = currentPosition - _dragStartPoint;

            if (!_wasActuallyDragged &&
                (Math.Abs(dragVector.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(dragVector.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                _wasActuallyDragged = true;
            }

            if (_wasActuallyDragged)
            {
                // This is the new logic for dragging
                if (_virtualHeight <= panel.ActualHeight) return;

                var totalLines = Math.Max(_diffModel.OldText.Lines.Count, _diffModel.NewText.Lines.Count);
                if (totalLines == 0) return;

                // Calculate the scroll ratio based on the mouse position within the panel
                var scrollRatio = Math.Clamp(currentPosition.Y / panel.ActualHeight, 0.0, 1.0);
                var targetLine = (int)(scrollRatio * totalLines);
                
                ScrollToLine(targetLine);
            }
        }

        private void NavigationPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && sender is Canvas panel)
            {
                if (!_wasActuallyDragged)
                {
                    // This is for a simple click, not a drag
                    NavigateToClosestDifference(panel, e.GetPosition(panel).Y);
                }
                _isDragging = false;
                panel.ReleaseMouseCapture();
            }
        }

        private void NavigateToClosestDifference(Canvas panel, double y)
        {
            if (!_diffLines.Any()) return;

            // Translate clicked Y to a position on the virtual, scrolling canvas
            var virtualY = y - _oldTransform.Y;
            var clickedLine = (int)(virtualY / MarkerHeight) + 1;

            var closestDiffLine = _diffLines.OrderBy(diffLine => Math.Abs(diffLine - clickedLine)).FirstOrDefault();
            if (closestDiffLine == 0 && _diffLines.Any()) closestDiffLine = _diffLines.First();

            ScrollToLine(closestDiffLine);
        }
    }
}