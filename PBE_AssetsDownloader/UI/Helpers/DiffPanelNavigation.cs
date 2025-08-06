using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly List<int> _diffLines;

        public event Action<int> ScrollRequested;

        public DiffPanelNavigation(Canvas oldPanel, Canvas newPanel, SideBySideDiffModel diffModel)
        {
            _oldPanel = oldPanel;
            _newPanel = newPanel;
            _diffModel = diffModel;
            _diffLines = new List<int>();
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

            

            // Track the last change type to group consecutive changes
            ChangeType lastChangeType = ChangeType.Unchanged;

            // Iterate through the new text lines to find the start of each diff block
            for (int i = 0; i < _diffModel.NewText.Lines.Count; i++)
            {
                var currentLine = _diffModel.NewText.Lines[i];

                // If the current line is a change (inserted, deleted, modified)
                // and it's different from the last change type, or it's the very first line
                // then it marks the beginning of a new diff block.
                if (currentLine.Type != ChangeType.Unchanged && currentLine.Type != lastChangeType)
                {
                    _diffLines.Add(i + 1); // Add 1 because line numbers are 1-based
                }
                lastChangeType = currentLine.Type;
            }

            // Also consider changes in the old text for navigation, especially for deletions
            lastChangeType = ChangeType.Unchanged;
            for (int i = 0; i < _diffModel.OldText.Lines.Count; i++)
            {
                var currentLine = _diffModel.OldText.Lines[i];
                if (currentLine.Type != ChangeType.Unchanged && currentLine.Type != lastChangeType)
                {
                    // Add to diffLines if not already present (e.g., for deletions that don't have a corresponding new line)
                    if (!_diffLines.Contains(i + 1))
                    {
                        _diffLines.Add(i + 1);
                    }
                }
                lastChangeType = currentLine.Type;
            }

            _diffLines.Sort(); // Ensure lines are in ascending order
        }

        public void NavigateToNextDifference(int currentLine)
        {
            if (_diffLines.Count == 0) return;

            var nextDiffLine = _diffLines.FirstOrDefault(line => line > currentLine);
            if (nextDiffLine == 0) // Wrap around
            {
                nextDiffLine = _diffLines[0];
            }
            ScrollToLine(nextDiffLine);
        }

        public void NavigateToPreviousDifference(int currentLine)
        {
            if (_diffLines.Count == 0) return;

            var previousDiffLine = _diffLines.LastOrDefault(line => line < currentLine);
            if (previousDiffLine == 0) // Wrap around
            {
                previousDiffLine = _diffLines.Last();
            }
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

            if (_diffModel?.OldText?.Lines == null) return;

            var panelHeight = _oldPanel.ActualHeight > 0 ? _oldPanel.ActualHeight : 600;
            var totalLines = Math.Max(_diffModel.OldText.Lines.Count, _diffModel.NewText.Lines.Count);
            if (totalLines == 0) return;
            var lineHeight = panelHeight / totalLines;

            // Draw for Old Panel
            for (int i = 0; i < _diffModel.OldText.Lines.Count; i++)
            {
                var changeType = _diffModel.OldText.Lines[i].Type;
                var color = DiffColorsHelper.GetNavigationColor(changeType);
                if (color == Colors.Transparent) continue;

                var rect = new Rectangle
                {
                    Width = _oldPanel.ActualWidth,
                    Height = Math.Max(DiffColorsHelper.VisualSettings.NavigationRectMinHeight, lineHeight),
                    Fill = new SolidColorBrush(color),
                    Cursor = Cursors.Hand,
                    Tag = i + 1
                };

                rect.MouseLeftButtonDown += NavigationRect_Click;
                Canvas.SetTop(rect, i * lineHeight);
                _oldPanel.Children.Add(rect);
            }

            // Draw for New Panel
            for (int i = 0; i < _diffModel.NewText.Lines.Count; i++)
            {
                var changeType = _diffModel.NewText.Lines[i].Type;
                var color = DiffColorsHelper.GetNavigationColor(changeType);
                if (color == Colors.Transparent) continue;

                var rect = new Rectangle
                {
                    Width = _newPanel.ActualWidth,
                    Height = Math.Max(DiffColorsHelper.VisualSettings.NavigationRectMinHeight, lineHeight),
                    Fill = new SolidColorBrush(color),
                    Cursor = Cursors.Hand,
                    Tag = i + 1
                };

                rect.MouseLeftButtonDown += NavigationRect_Click;
                Canvas.SetTop(rect, i * lineHeight);
                _newPanel.Children.Add(rect);
            }
        }

        private void NavigationPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            if (sender is Canvas panel)
            {
                panel.CaptureMouse();
                HandleNavigation(panel, e.GetPosition(panel).Y);
            }
        }

        private void NavigationPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && sender is Canvas panel)
            {
                HandleNavigation(panel, e.GetPosition(panel).Y);
            }
        }

        private void NavigationPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            if (sender is Canvas panel)
            {
                panel.ReleaseMouseCapture();
            }
        }

        private void NavigationRect_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle { Tag: int lineNumber })
            {
                ScrollRequested?.Invoke(lineNumber);
                e.Handled = true; // Prevent bubbling to the parent Canvas
            }
        }

        private void HandleNavigation(Canvas panel, double y)
        {
            var totalLines = Math.Max(_diffModel.OldText.Lines.Count, _diffModel.NewText.Lines.Count);
            var panelHeight = panel.ActualHeight;
            var lineNumber = (int)((y / panelHeight) * totalLines) + 1;
            ScrollRequested?.Invoke(lineNumber);
        }
    }
}
