using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace CryptoGuard.MAUI.Converters
{
    public class ProfitColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is decimal profit)
            {
                if (profit > 0)
                    return Color.FromArgb("#22c55e"); // yeşil
                if (profit < 0)
                    return Color.FromArgb("#ef4444"); // kırmızı
                return Color.FromArgb("#a6adc8"); // nötr
            }
            return Color.FromArgb("#a6adc8");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 