using System.Windows.Media;

namespace PBE_AssetsDownloader.UI.Helpers
{
    /// <summary>
    /// Helper class to manage diff colors and visual settings consistently across the application.
    /// These colors should match those defined in JsonSyntaxHighlighting.xshd
    /// </summary>
    public static class DiffColorsHelper
    {
        // Visual configuration constants
        public static class VisualSettings
        {
            public static readonly FontFamily EditorFontFamily = new FontFamily("Consolas, Courier New, monospace");
            public static readonly double EditorFontSize = 13.0;
            public static readonly double StandardLineHeight = 16.0;
            public static readonly double NavigationRectWidth = 18.0;
            public static readonly double NavigationRectMinHeight = 2.0;
            public static readonly double NavigationRectLeftOffset = 1.0;
        }
        // Background colors for DiffBackgroundRenderer (with alpha)
        public static class Background
        {
            public static readonly Color Deleted = Color.FromArgb(80, 255, 107, 107);    // #FF6B6B with 80 alpha
            public static readonly Color Inserted = Color.FromArgb(80, 107, 207, 127);   // #6BCF7F with 80 alpha  
            public static readonly Color Modified = Color.FromArgb(80, 255, 217, 61);    // #FFD93D with 80 alpha
            public static readonly Color Imaginary = Color.FromArgb(40, 128, 128, 128);  // #808080 with 40 alpha
        }
        
        // Navigation panel colors (solid colors)
        public static class Navigation
        {
            public static readonly Color Deleted = Color.FromRgb(255, 68, 68);    // #FF4444
            public static readonly Color Inserted = Color.FromRgb(50, 205, 50);   // #32CD32
            public static readonly Color Modified = Color.FromRgb(255, 215, 0);   // #FFD700
            public static readonly Color Imaginary = Color.FromRgb(128, 128, 128); // #808080
        }
        
        /// <summary>
        /// Gets the appropriate background color for a change type
        /// </summary>
        public static Color GetBackgroundColor(DiffPlex.DiffBuilder.Model.ChangeType changeType)
        {
            return changeType switch
            {
                DiffPlex.DiffBuilder.Model.ChangeType.Deleted => Background.Deleted,
                DiffPlex.DiffBuilder.Model.ChangeType.Inserted => Background.Inserted,
                DiffPlex.DiffBuilder.Model.ChangeType.Modified => Background.Modified,
                DiffPlex.DiffBuilder.Model.ChangeType.Imaginary => Background.Imaginary,
                _ => Colors.Transparent
            };
        }
        
        /// <summary>
        /// Gets the appropriate navigation panel color for a change type
        /// </summary>
        public static Color GetNavigationColor(DiffPlex.DiffBuilder.Model.ChangeType changeType)
        {
            return changeType switch
            {
                DiffPlex.DiffBuilder.Model.ChangeType.Deleted => Navigation.Deleted,
                DiffPlex.DiffBuilder.Model.ChangeType.Inserted => Navigation.Inserted,
                DiffPlex.DiffBuilder.Model.ChangeType.Modified => Navigation.Modified,
                DiffPlex.DiffBuilder.Model.ChangeType.Imaginary => Navigation.Imaginary,
                _ => Colors.Transparent
            };
        }
    }
}