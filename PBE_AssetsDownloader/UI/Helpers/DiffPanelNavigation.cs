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
        private readonly SolidColorBrush _emptyBrush;

        public event Action<int> ScrollRequested;

        public DiffPanelNavigation(Canvas oldPanel, Canvas newPanel, SideBySideDiffModel diffModel)
        {
            _oldPanel = oldPanel;
            _newPanel = newPanel;
            _diffModel = diffModel;
            _diffLines = new List<int>();

            // Cache brushes for performance
            _backgroundPanelBrush = new SolidColorBrush((Color)Application.Current.FindResource("BackgroundPanelNavigation"));

            _removedBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationRemoved"));
            _addedBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationAdded"));
            _modifiedBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationModified"));
            _emptyBrush = new SolidColorBrush((Color)Application.Current.FindResource("DiffNavigationEmpty"));

            _backgroundPanelBrush.Freeze();
            _removedBrush.Freeze();
            _addedBrush.Freeze();
            _modifiedBrush.Freeze();
            _emptyBrush.Freeze();

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

            if (_diffModel?.OldText?.Lines == null || _diffModel.NewText?.Lines == null) return;

            var panelHeight = _oldPanel.ActualHeight > 0 ? _oldPanel.ActualHeight : 600; // Default height as a fallback
            var totalLines = Math.Max(_diffModel.OldText.Lines.Count, _diffModel.NewText.Lines.Count);
            if (totalLines == 0) return;

            var lineHeight = panelHeight / totalLines;

            var oldLines = _diffModel.OldText.Lines;
            var newLines = _diffModel.NewText.Lines;

            for (int i = 0; i < totalLines; i++)
            {
                var oldLine = i < oldLines.Count ? oldLines[i] : null;
                var newLine = i < newLines.Count ? newLines[i] : null;

                if (oldLine == null || newLine == null) continue;

                // Determine brush for the old panel
                Brush oldBrush = null;
                switch (oldLine.Type)
                {
                    case ChangeType.Deleted:
                        oldBrush = _removedBrush;
                        break;
                    case ChangeType.Modified:
                        oldBrush = _modifiedBrush;
                        break;
                    case ChangeType.Imaginary:
                        oldBrush = _emptyBrush;
                        break;
                }

                // Determine brush for the new panel
                Brush newBrush = null;
                switch (newLine.Type)
                {
                    case ChangeType.Inserted:
                        newBrush = _addedBrush;
                        break;
                    case ChangeType.Modified:
                        newBrush = _modifiedBrush;
                        break;
                    case ChangeType.Imaginary:
                        newBrush = _emptyBrush;
                        break;
                }

                // Draw rectangles if a brush was assigned
                if (oldBrush != null)
                {
                    DrawRectangle(_oldPanel, oldBrush, i * lineHeight, lineHeight);
                }

                if (newBrush != null)
                {
                    DrawRectangle(_newPanel, newBrush, i * lineHeight, lineHeight);
                }
            }
        }

        private void DrawRectangle(Canvas panel, Brush brush, double top, double height)
        {
            var rect = new Rectangle
            {
                Width = panel.ActualWidth,
                Height = Math.Max(2.0, height), // Ensure a minimum height to be visible
                Fill = brush,
                IsHitTestVisible = false
            };

            Canvas.SetTop(rect, top);
            panel.Children.Add(rect);
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
