using System.Globalization;

namespace SSHExplorer.Resources.Converters;

public class BoolToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string stringParams)
            return string.Empty;

        var strings = stringParams.Split(',');
        if (strings.Length != 2)
            return string.Empty;

        var trueString = strings[0].Trim();
        var falseString = strings[1].Trim();

        return boolValue ? trueString : falseString;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}