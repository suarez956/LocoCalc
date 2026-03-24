using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace LocoCalcAvalonia.Converters;

public class HexColorConverter : IValueConverter
{
    public static readonly HexColorConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex) return SolidColorBrush.Parse(hex);
        return Brushes.Transparent;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToColorConverter : IValueConverter
{
    public string TrueColor  { get; set; } = "#f97316";
    public string FalseColor { get; set; } = "#252540";
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => SolidColorBrush.Parse(value is true ? TrueColor : FalseColor);
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public static readonly InverseBoolConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}
