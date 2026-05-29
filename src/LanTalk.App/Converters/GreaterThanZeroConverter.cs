using System.Globalization;
using Avalonia.Data.Converters;

namespace LanTalk.App.Converters;

public sealed class GreaterThanZeroConverter : IValueConverter
{
    public static readonly GreaterThanZeroConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int intValue => intValue > 0,
            long longValue => longValue > 0,
            double doubleValue => doubleValue > 0,
            _ => false
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

