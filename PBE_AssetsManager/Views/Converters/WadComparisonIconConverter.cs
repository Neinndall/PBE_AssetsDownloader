using PBE_AssetsManager.Info;
using PBE_AssetsManager.Views.Dialogs;
using Material.Icons;
using Material.Icons.WPF;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace PBE_AssetsManager.Views.Converters
{
    public class WadComparisonIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                WadGroupViewModel => MaterialIconKind.PackageVariant,
                DiffTypeGroupViewModel diffType => GetDiffTypeIcon(diffType.Type),
                SerializableChunkDiff chunk => GetFileExtensionIcon(chunk.Path),
                _ => MaterialIconKind.FileQuestionOutline,
            };
        }

        private static MaterialIconKind GetDiffTypeIcon(ChunkDiffType type)
        {
            return type switch
            {
                ChunkDiffType.New => MaterialIconKind.FilePlusOutline,
                ChunkDiffType.Removed => MaterialIconKind.FileRemoveOutline,
                ChunkDiffType.Modified => MaterialIconKind.FileEditOutline,
                ChunkDiffType.Renamed => MaterialIconKind.FileMoveOutline,
                _ => MaterialIconKind.FileQuestionOutline,
            };
        }

        private static MaterialIconKind GetFileExtensionIcon(string path)
        {
            string extension = Path.GetExtension(path)?.ToLowerInvariant() ?? "";
            return extension switch
            {
                ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" => MaterialIconKind.ImageOutline,
                ".dds" or ".tex" => MaterialIconKind.FileImageOutline,
                ".json" => MaterialIconKind.CodeJson,
                ".txt" or ".log" => MaterialIconKind.FileDocumentOutline,
                ".lua" => MaterialIconKind.LanguageLua,
                ".xml" => MaterialIconKind.FileXmlBox,
                ".bin" => MaterialIconKind.FileCogOutline,
                ".skl" or ".skn" => MaterialIconKind.Human,
                _ => MaterialIconKind.FileOutline,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
