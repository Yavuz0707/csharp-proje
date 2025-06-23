using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace CryptoGuard.MAUI.Converters
{
    public class NotNullAndOriginalPostNullConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = OriginalPostId, values[1] = OriginalPost
            return values[0] != null && values[1] == null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
} 