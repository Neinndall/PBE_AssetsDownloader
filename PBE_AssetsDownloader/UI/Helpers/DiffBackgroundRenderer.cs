
using DiffPlex.DiffBuilder.Model;
using ICSharpCode.AvalonEdit.Rendering;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace PBE_AssetsDownloader.UI.Helpers
{
    public class DiffBackgroundRenderer : IBackgroundRenderer
    {
        private readonly List<ChangeType> _lineTypes;

        public DiffBackgroundRenderer(List<ChangeType> lineTypes)
        {
            _lineTypes = lineTypes;
        }

        public KnownLayer Layer => KnownLayer.Background;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_lineTypes == null) return;

            foreach (var line in textView.VisualLines)
            {
                var lineNumber = line.FirstDocumentLine.LineNumber - 1;
                if (lineNumber < 0 || lineNumber >= _lineTypes.Count) continue;

                var backgroundColor = DiffColorsHelper.GetBackgroundColor(_lineTypes[lineNumber]);
                if (backgroundColor == Colors.Transparent) continue;

                var backgroundBrush = new SolidColorBrush(backgroundColor);
                var rect = new Rect(0, line.VisualTop - textView.ScrollOffset.Y,
                                  textView.ActualWidth, line.Height);
                drawingContext.DrawRectangle(backgroundBrush, null, rect);
            }
        }
    }
}
