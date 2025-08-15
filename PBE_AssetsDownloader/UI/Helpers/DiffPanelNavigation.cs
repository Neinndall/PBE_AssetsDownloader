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
        private readonly List<int> _diffLines;

        // Interaction state
        private bool _isDragging;

        // Visual Elements & Transforms
        private readonly TranslateTransform _scrollTransform;
        private double _totalDrawingHeight;
        private readonly SolidColorBrush _backgroundPanelBrush;
        private readonly SolidColorBrush _removedBrush;
        private readonly SolidColorBrush _addedBrush;
        private readonly SolidColorBrush _modifiedBrush;
        private readonly SolidColorBrush _guideBrush;
        private Rectangle _oldViewportRect;
        private Rectangle _newViewportRect;

        public event Action<int> ScrollRequested;

        public DiffPanelNavigation(Canvas oldPanel, Canvas newPanel, SideBySideDiffModel diffModel)
        {
            _oldPanel = oldPanel;
            _newPanel = newPanel;
            _diffModel = diffModel;
            _diffLines = new List<int>();

            _scrollTransform = new TranslateTransform();

            // Load Brushes
            _backgroundPanelBrush = new SolidColorBrush((Color)Application.Current.FindResource("BackgroundPanelNavigation"));
            _removedBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationRemoved"));
            _addedBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationAdded"));
            _modifiedBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationModified"));
            _guideBrush = (SolidColorBrush)Application.Current.FindResource("ViewportNavigation");

            // Freeze for performance
            _backgroundPanelBrush.Freeze();
            _removedBrush.Freeze();
            _addedBrush.Freeze();
            _modifiedBrush.Freeze();
            _guideBrush.Freeze();

            SetupEvents();
            FindDiffLines();
        }

        private void SetupEvents()
        {
            _oldPanel.MouseLeftButtonDown += Panel_MouseLeftButtonDown;
            _oldPanel.MouseMove += Panel_MouseMove;
            _oldPanel.MouseLeftButtonUp += Panel_MouseLeftButtonUp;
            _oldPanel.MouseLeave += Panel_MouseLeave; // Clean up if mouse leaves panel
            _oldPanel.SizeChanged += (s, e) => DrawPanels();

            _newPanel.MouseLeftButtonDown += Panel_MouseLeftButtonDown;
            _newPanel.MouseMove += Panel_MouseMove;
            _newPanel.MouseLeftButtonUp += Panel_MouseLeftButtonUp;
            _newPanel.MouseLeave += Panel_MouseLeave; // Clean up if mouse leaves panel
            _newPanel.SizeChanged += (s, e) => DrawPanels();
        }

        private void FindDiffLines()
        {
            if (_diffModel == null) return;
            var diffBlockStartLines = new HashSet<int>();
            bool inDiffBlock = false;
            for (int i = 0; i < _diffModel.NewText.Lines.Count; i++)
            {
                bool isLineChanged = _diffModel.OldText.Lines[i].Type != ChangeType.Unchanged || _diffModel.NewText.Lines[i].Type != ChangeType.Unchanged;
                if (isLineChanged && !inDiffBlock)
                {
                    diffBlockStartLines.Add(i + 1);
                    inDiffBlock = true;
                }
                else if (!isLineChanged)
                {
                    inDiffBlock = false;
                }
            }
            _diffLines.Clear();
            _diffLines.AddRange(diffBlockStartLines);
            _diffLines.Sort();
        }

        public void UpdateScroll(double verticalOffset, double extentHeight, double viewportHeight)
        {
            // Update viewport rectangle first
            if (_oldViewportRect != null && _newViewportRect != null && extentHeight > viewportHeight)
            {
                var panelHeight = _oldPanel.ActualHeight;

                // Calculate viewport size and position relative to the panel
                var rectHeight = (viewportHeight / extentHeight) * panelHeight;
                var rectY = (verticalOffset / extentHeight) * panelHeight;

                // Ensure values are within bounds
                rectHeight = Math.Max(2, rectHeight); // Ensure it's at least visible
                rectY = Math.Max(0, Math.Min(rectY, panelHeight - rectHeight));

                _oldViewportRect.Height = rectHeight;
                _oldViewportRect.Width = _oldPanel.ActualWidth;
                Canvas.SetTop(_oldViewportRect, rectY);
                _oldViewportRect.Visibility = Visibility.Visible;

                _newViewportRect.Height = rectHeight;
                _newViewportRect.Width = _newPanel.ActualWidth;
                Canvas.SetTop(_newViewportRect, rectY);
                _newViewportRect.Visibility = Visibility.Visible;
            }
            else if (_oldViewportRect != null && _newViewportRect != null)
            {
                // Hide viewport if not needed (content fits in editor)
                _oldViewportRect.Visibility = Visibility.Collapsed;
                _newViewportRect.Visibility = Visibility.Collapsed;
            }

            // Then, update the scroll transform for the markers
            if (_totalDrawingHeight <= _oldPanel.ActualHeight || extentHeight <= viewportHeight)
            {
                _scrollTransform.Y = 0;
                return;
            }

            var scrollableRange = _totalDrawingHeight - _oldPanel.ActualHeight;
            var scrollableEditorRange = extentHeight - viewportHeight;

            // Avoid division by zero if the editor range is not scrollable
            if (scrollableEditorRange <= 0)
            {
                _scrollTransform.Y = 0;
                return;
            }

            var ratio = verticalOffset / scrollableEditorRange;
            var transformY = -1 * ratio * scrollableRange;

            _scrollTransform.Y = transformY;
        }


        public void DrawPanels()
        {
            _oldPanel.Children.Clear();
            _newPanel.Children.Clear();

            _oldPanel.Background = _backgroundPanelBrush;
            _newPanel.Background = _backgroundPanelBrush;

            if (_diffModel?.OldText?.Lines == null || _oldPanel.ActualHeight <= 0) return;

            _totalDrawingHeight = _oldPanel.ActualHeight * 3;

            // Draw the markers (scrolling canvas)
            DrawPanelContent(_oldPanel, _diffModel.OldText.Lines);
            DrawPanelContent(_newPanel, _diffModel.NewText.Lines);

            // Create and add the viewport rectangle, which will be positioned by UpdateScroll
            _oldViewportRect = new Rectangle { Fill = _guideBrush, IsHitTestVisible = false };
            _newViewportRect = new Rectangle { Fill = _guideBrush, IsHitTestVisible = false };
            _oldPanel.Children.Add(_oldViewportRect);
            _newPanel.Children.Add(_newViewportRect);
        }


        private void DrawPanelContent(Canvas panel, IReadOnlyList<DiffPiece> lines)
        {
            var totalLines = Math.Max(_diffModel.OldText.Lines.Count, _diffModel.NewText.Lines.Count);
            if (totalLines == 0) return;

            var innerCanvas = new Canvas();

            for (int i = 0; i < lines.Count; i++)
            {
                var brush = GetBrushForChangeType(lines[i].Type);
                if (brush == null) continue;

                var y = ((double)i / totalLines) * _totalDrawingHeight;

                var rect = new Rectangle
                {
                    Width = panel.ActualWidth,
                    Height = 2,
                    Fill = brush,
                    IsHitTestVisible = false
                };

                Canvas.SetTop(rect, y);
                innerCanvas.Children.Add(rect);
            }
            innerCanvas.RenderTransform = _scrollTransform;
            panel.Children.Add(innerCanvas);
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            var panel = (Canvas)sender;
            panel.CaptureMouse();

            var position = e.GetPosition(panel);
            HandleNavigation(panel, position.Y);
        }

        private void Panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var panel = (Canvas)sender;
                var position = e.GetPosition(panel);
                HandleNavigation(panel, position.Y);
            }
        }

        private void Panel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CleanUpDrag((Canvas)sender);
        }

        private void Panel_MouseLeave(object sender, MouseEventArgs e)
        {
            CleanUpDrag((Canvas)sender);
        }

        private void CleanUpDrag(Canvas panel)
        {
            if (_isDragging)
            {
                _isDragging = false;
                panel.ReleaseMouseCapture();
            }
        }

        private void HandleNavigation(Canvas panel, double y)
        {
            var panelHeight = panel.ActualHeight;
            if (panelHeight <= 0) return;

            var yOnVirtualRibbon = y - _scrollTransform.Y;
            var totalLines = Math.Max(_diffModel.OldText.Lines.Count, _diffModel.NewText.Lines.Count);
            if (_totalDrawingHeight <= 0 || totalLines == 0) return;

            var targetLine = (int)((yOnVirtualRibbon / _totalDrawingHeight) * totalLines);

            ScrollRequested?.Invoke(targetLine);
        }
        
        public void NavigateToNextDifference(int currentLine)
        {
            if (_diffLines.Count == 0) return;
            var nextDiffLine = _diffLines.FirstOrDefault(line => line > currentLine);
            if (nextDiffLine == 0) nextDiffLine = _diffLines[0];
            ScrollRequested?.Invoke(nextDiffLine);
        }

        public void NavigateToPreviousDifference(int currentLine)
        {
            if (_diffLines.Count == 0) return;
            var previousDiffLine = _diffLines.LastOrDefault(line => line < currentLine);
            if (previousDiffLine == 0) previousDiffLine = _diffLines.Last();
            ScrollRequested?.Invoke(previousDiffLine);
        }

        public int GetFirstDiffLine()
        {
            return _diffLines.Count > 0 ? _diffLines[0] : -1;
        }

        private SolidColorBrush GetBrushForChangeType(ChangeType changeType)
        {
            return changeType switch
            {
                ChangeType.Deleted => _removedBrush,
                ChangeType.Inserted => _addedBrush,
                ChangeType.Modified => _modifiedBrush,
                _ => null
            };
        }
    }
}

