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

namespace AssetsManager.Views.Dialogs.Controls
{
    public partial class DiffNavigationPanel : UserControl
    {
        private TextEditor _oldEditor;
        private TextEditor _newEditor;
        private SideBySideDiffModel _diffModel;
        private SideBySideDiffModel _originalDiffModel;
        private bool _isDragging;
        private Point _dragStartPoint;
        private bool _wasActuallyDragged;
        private readonly List<int> _diffLines;
        public int CurrentLine { get; set; }

        private readonly SolidColorBrush _backgroundPanelBrush, _addedBrush, _removedBrush, _modifiedBrush, _imaginaryBrush, _viewportBrush;
        private Rectangle _oldViewportGuide, _newViewportGuide;

        public event Action<int> ScrollRequested;

        public DiffNavigationPanel()
        {
            InitializeComponent();
            _diffLines = new List<int>();

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
        }

        public void Initialize(TextEditor oldEditor, TextEditor newEditor, SideBySideDiffModel diffModel, SideBySideDiffModel originalDiffModel = null)
        {
            _oldEditor = oldEditor;
            _newEditor = newEditor;
            _diffModel = diffModel;
            _originalDiffModel = originalDiffModel ?? diffModel;

            SetupEvents();
            FindDiffLines();
            InitializeDiffMarkers();
            UpdateViewportGuide();
        }

        private void SetupEvents()
        {
            OldDiffMapCanvas.MouseLeftButtonDown += NavigationPanel_MouseLeftButtonDown;
            OldDiffMapCanvas.MouseMove += NavigationPanel_MouseMove;
            OldDiffMapCanvas.MouseLeftButtonUp += NavigationPanel_MouseLeftButtonUp;
            OldDiffMapCanvas.SizeChanged += (s, e) => { InitializeDiffMarkers(); UpdateViewportGuide(); };

            NewDiffMapCanvas.MouseLeftButtonDown += NavigationPanel_MouseLeftButtonDown;
            NewDiffMapCanvas.MouseMove += NavigationPanel_MouseMove;
            NewDiffMapCanvas.MouseLeftButtonUp += NavigationPanel_MouseLeftButtonUp;
            NewDiffMapCanvas.SizeChanged += (s, e) => { InitializeDiffMarkers(); UpdateViewportGuide(); };
        }

        public void InitializeDiffMarkers()
        {
            OldDiffMapCanvas.Children.Clear();
            NewDiffMapCanvas.Children.Clear();

            OldDiffMapCanvas.Background = _backgroundPanelBrush;
            NewDiffMapCanvas.Background = _backgroundPanelBrush;

            if (_diffModel?.OldText?.Lines == null) return;

            var panelHeight = OldDiffMapCanvas.ActualHeight > 0 ? OldDiffMapCanvas.ActualHeight : 600;
            var totalLines = Math.Max(_diffModel.OldText.Lines.Count, _diffModel.NewText.Lines.Count);
            if (totalLines == 0) return;
            var lineHeight = panelHeight / totalLines;

            DrawPanelContent(OldDiffMapCanvas, _diffModel.OldText.Lines, lineHeight);
            DrawPanelContent(NewDiffMapCanvas, _diffModel.NewText.Lines, lineHeight);
        }

        public void UpdateViewportGuide()
        {
            if (_oldViewportGuide != null) OldDiffMapCanvas.Children.Remove(_oldViewportGuide);
            if (_newViewportGuide != null) NewDiffMapCanvas.Children.Remove(_newViewportGuide);

            var panelHeight = OldDiffMapCanvas.ActualHeight;
            if (panelHeight <= 0) return;

            if (_originalDiffModel != _diffModel)
            {
                var totalLines = Math.Max(_diffModel.OldText.Lines.Count, _diffModel.NewText.Lines.Count);
                if (totalLines == 0 || CurrentLine <= 0) return;
                var lineHeight = panelHeight / totalLines;
                var lineIndex = CurrentLine - 1;
                if (lineIndex < 0 || lineIndex >= totalLines) return;

                var highlighterTop = lineIndex * lineHeight;
                _oldViewportGuide = new Rectangle { Width = OldDiffMapCanvas.ActualWidth, Height = Math.Max(2.0, lineHeight), Fill = _viewportBrush, IsHitTestVisible = false };
                _newViewportGuide = new Rectangle { Width = NewDiffMapCanvas.ActualWidth, Height = Math.Max(2.0, lineHeight), Fill = _viewportBrush, IsHitTestVisible = false };

                Canvas.SetTop(_oldViewportGuide, highlighterTop);
                Canvas.SetTop(_newViewportGuide, highlighterTop);
            }
            else
            {
                if (_newEditor.ExtentHeight <= 0) return;

                var viewportRatio = _newEditor.ViewportHeight / _newEditor.ExtentHeight;
                var offsetRatio = _newEditor.VerticalOffset / _newEditor.ExtentHeight;
                var viewportHeight = panelHeight * viewportRatio;
                var viewportTop = panelHeight * offsetRatio;

                _oldViewportGuide = new Rectangle { Width = OldDiffMapCanvas.ActualWidth, Height = Math.Max(2.0, viewportHeight), Fill = _viewportBrush, IsHitTestVisible = false };
                _newViewportGuide = new Rectangle { Width = NewDiffMapCanvas.ActualWidth, Height = Math.Max(2.0, viewportHeight), Fill = _viewportBrush, IsHitTestVisible = false };

                Canvas.SetTop(_oldViewportGuide, viewportTop);
                Canvas.SetTop(_newViewportGuide, viewportTop);
            }

            OldDiffMapCanvas.Children.Add(_oldViewportGuide);
            NewDiffMapCanvas.Children.Add(_newViewportGuide);
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
                    Height = Math.Max(1.0, lineHeight),
                    Fill = brush,
                    IsHitTestVisible = false
                };

                Canvas.SetTop(rect, i * lineHeight);
                panel.Children.Add(rect);
            }
        }

        private void FindDiffLines()
        {
            if (_diffModel == null) return;
            _diffLines.Clear();
            var diffLineSet = new HashSet<int>();

            if (_originalDiffModel != _diffModel && _diffModel.NewText.Lines.Count > 0)
            {
                diffLineSet.Add(1);

                ChangeType lastChangeTypeNew = _diffModel.NewText.Lines[0].Type;
                ChangeType lastChangeTypeOld = _diffModel.OldText.Lines[0].Type;

                for (int i = 1; i < _diffModel.NewText.Lines.Count; i++)
                {
                    var currentLineNew = _diffModel.NewText.Lines[i];
                    var prevLineNew = _diffModel.NewText.Lines[i - 1];
                    var currentLineOld = _diffModel.OldText.Lines[i];
                    var prevLineOld = _diffModel.OldText.Lines[i - 1];

                    bool typeChanged = currentLineNew.Type != lastChangeTypeNew || currentLineOld.Type != lastChangeTypeOld;

                    bool gapDetected = (currentLineNew.Position.HasValue && prevLineNew.Position.HasValue && currentLineNew.Position.Value != prevLineNew.Position.Value + 1) ||
                                       (currentLineOld.Position.HasValue && prevLineOld.Position.HasValue && currentLineOld.Position.Value != prevLineOld.Position.Value + 1);

                    if (typeChanged || gapDetected)
                    {
                        diffLineSet.Add(i + 1);
                    }

                    lastChangeTypeNew = currentLineNew.Type;
                    lastChangeTypeOld = currentLineOld.Type;
                }
            }
            else
            {
                ChangeType lastChangeType = ChangeType.Unchanged;
                for (int i = 0; i < _diffModel.NewText.Lines.Count; i++)
                {
                    var currentLine = _diffModel.NewText.Lines[i];
                    if (currentLine.Type != ChangeType.Unchanged && currentLine.Type != lastChangeType)
                    {
                        diffLineSet.Add(i + 1);
                    }
                    lastChangeType = currentLine.Type;
                }

                lastChangeType = ChangeType.Unchanged;
                for (int i = 0; i < _diffModel.OldText.Lines.Count; i++)
                {
                    var currentLine = _diffModel.OldText.Lines[i];
                    if (currentLine.Type != ChangeType.Unchanged && currentLine.Type != lastChangeType)
                    {
                        if (!diffLineSet.Contains(i + 1))
                        {
                            diffLineSet.Add(i + 1);
                        }
                    }
                    lastChangeType = currentLine.Type;
                }
            }

            _diffLines.AddRange(diffLineSet);
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
