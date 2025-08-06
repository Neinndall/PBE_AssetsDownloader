using DiffPlex.DiffBuilder.Model;
using ICSharpCode.AvalonEdit.Rendering;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;

namespace PBE_AssetsDownloader.UI.Helpers
{
    public class DiffBackgroundRenderer : IBackgroundRenderer
    {
        private readonly SideBySideDiffModel _diffModel;
        private readonly bool _isWordLevel;
        private readonly bool _isOldEditor;

        public DiffBackgroundRenderer(SideBySideDiffModel diffModel, bool isWordLevel, bool isOldEditor)
        {
            _diffModel = diffModel;
            _isWordLevel = isWordLevel;
            _isOldEditor = isOldEditor;
        }

        public KnownLayer Layer => KnownLayer.Background;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView.VisualLines.Count == 0 || _diffModel == null) return;

            var diffLines = _isOldEditor ? _diffModel.OldText.Lines : _diffModel.NewText.Lines;

            foreach (var line in textView.VisualLines)
            {
                var lineNumber = line.FirstDocumentLine.LineNumber - 1;
                if (lineNumber < 0 || lineNumber >= diffLines.Count) continue;

                var diffLine = diffLines[lineNumber];
                if (diffLine.Type == ChangeType.Unchanged && !_isWordLevel) continue;

                var lineBackgroundColor = DiffColorsHelper.GetBackgroundColor(diffLine.Type);
                if (lineBackgroundColor != Colors.Transparent)
                {
                    var backgroundBrush = new SolidColorBrush(lineBackgroundColor) { Opacity = 0.2 };
                    var rect = new Rect(0, line.VisualTop - textView.ScrollOffset.Y, textView.ActualWidth, line.Height);
                    drawingContext.DrawRectangle(backgroundBrush, null, rect);
                }

                if (_isWordLevel && diffLine.Type != ChangeType.Unchanged)
                {
                    var wordHighlightColor = DiffColorsHelper.GetWordHighlightColor(diffLine.Type);
                    var wordHighlightBrush = new SolidColorBrush(wordHighlightColor) { Opacity = 0.4 };

                    if (diffLine.SubPieces != null)
                    {
                        int startOffset = line.FirstDocumentLine.Offset;
                        foreach (var piece in diffLine.SubPieces)
                        {
                            if (piece.Text == null) continue; // Defensive check

                            if (piece.Type == ChangeType.Unchanged)
                            {
                                startOffset += piece.Text.Length;
                                continue;
                            }

                            int endOffset = startOffset + piece.Text.Length;
                            var geoBuilder = new BackgroundGeometryBuilder();
                            geoBuilder.AddSegment(textView, new TextSegment { StartOffset = startOffset, EndOffset = endOffset });
                            var geometry = geoBuilder.CreateGeometry();
                            if (geometry != null)
                            {
                                drawingContext.DrawGeometry(wordHighlightBrush, null, geometry);
                            }
                            startOffset = endOffset;
                        }
                    }
                }
            }
        }
    }
}