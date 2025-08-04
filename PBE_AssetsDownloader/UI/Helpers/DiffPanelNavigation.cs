using System;
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

        public event Action<int> ScrollRequested;

        public DiffPanelNavigation(Canvas oldPanel, Canvas newPanel, SideBySideDiffModel diffModel)
        {
            _oldPanel = oldPanel;
            _newPanel = newPanel;
            _diffModel = diffModel;
            SetupEvents();
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
