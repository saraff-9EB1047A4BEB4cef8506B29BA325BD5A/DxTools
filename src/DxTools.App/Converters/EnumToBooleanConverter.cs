using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DxTools.App.Converters;

public sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is not string expected)
        {
            return false;
        }

        return string.Equals(value.ToString(), expected, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not true || parameter is not string expected)
        {
            return System.Windows.Data.Binding.DoNothing;
        }

        var enumType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return enumType.IsEnum && Enum.TryParse(enumType, expected, ignoreCase: true, out var result)
            ? result
            : System.Windows.Data.Binding.DoNothing;
    }
}
