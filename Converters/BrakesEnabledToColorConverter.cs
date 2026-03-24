using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LocoCalc.Converters;

public class BrakesEnabledToColorConverter : IValueConverter
{
    public static readonly BrakesEnabledToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool enabled = value is bool b && b;
        return enabled
            ? new SolidColorBrush(Color.Parse("#22c55e"))   // green-500
            : new SolidColorBrush(Color.Parse("#ef4444"));  // red-500
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
