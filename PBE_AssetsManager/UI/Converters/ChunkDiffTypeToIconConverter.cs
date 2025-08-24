using System;
using System.Globalization;
using System.Windows.Data;
using Material.Icons; // 👈 cambio aquí
using PBE_AssetsManager.Info;

namespace PBE_AssetsManager.UI.Converters
{
    public class ChunkDiffTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ChunkDiffType type)
            {
                switch (type)
                {
                    case ChunkDiffType.New: return MaterialIconKind.PlusCircleOutline;
                    case ChunkDiffType.Removed: return MaterialIconKind.MinusCircleOutline;
                    case ChunkDiffType.Modified: return MaterialIconKind.Sync; // o SyncCircleOutline
                    case ChunkDiffType.Renamed: return MaterialIconKind.ArrowRightCircleOutline;
                    default: return MaterialIconKind.HelpCircleOutline;
                }
            }
            return MaterialIconKind.HelpCircleOutline;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
