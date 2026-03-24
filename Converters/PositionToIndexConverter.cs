using System.Globalization;
using Avalonia.Data.Converters;
using LocoCalc.Models;

namespace LocoCalc.Converters;

/// <summary>
/// Maps ConsistPosition enum ↔ ComboBox index (Front=0, Middle=1, Rear=2).
/// </summary>
public class PositionToIndexConverter : IValueConverter
{
    public static readonly PositionToIndexConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ConsistPosition pos ? (int)pos : 0;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int idx ? (ConsistPosition)idx : ConsistPosition.Front;
}
