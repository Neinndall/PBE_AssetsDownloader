
using PBE_AssetsManager.Views.Models;
using Material.Icons;
using Material.Icons.WPF;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PBE_AssetsManager.Views.Converters
{
    public class PathToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not FileSystemNodeModel node)
            {
                return MaterialIconKind.FileOutline;
            }

            if (node.Type == NodeType.RealDirectory || node.Type == NodeType.VirtualDirectory)
            {
                return MaterialIconKind.FolderOutline;
            }

            if (node.Type == NodeType.WadFile)
            {
                return MaterialIconKind.PackageVariant;
            }

            return node.Extension.ToLowerInvariant() switch
            {
                ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" => MaterialIconKind.ImageOutline,
                ".dds" or ".tex" => MaterialIconKind.FileImageOutline,
                ".json" => MaterialIconKind.CodeJson,
                ".txt" or ".log" => MaterialIconKind.FileDocumentOutline,
                ".lua" => MaterialIconKind.LanguageLua,
                ".xml" => MaterialIconKind.FileXmlBox,
                ".html" => MaterialIconKind.LanguageHtml5,
                ".css" => MaterialIconKind.LanguageCss3,
                ".js" => MaterialIconKind.LanguageJavascript,
                ".cs" => MaterialIconKind.LanguageCsharp,
                _ => MaterialIconKind.FileOutline,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
