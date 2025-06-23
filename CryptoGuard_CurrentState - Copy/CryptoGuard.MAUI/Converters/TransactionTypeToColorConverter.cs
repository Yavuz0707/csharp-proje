using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using CryptoGuard.Core.Models;

namespace CryptoGuard.MAUI.Converters
{
    public class TransactionTypeToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TransactionType type)
            {
                if (type == TransactionType.Buy)
                    return Color.FromArgb("#7cfa7c"); // Yeşil
                else
                    return Color.FromArgb("#f38ba8"); // Kırmızı
            }
            return Color.FromArgb("#fff");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 