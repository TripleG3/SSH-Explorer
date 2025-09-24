using System.Globalization;

namespace SSHExplorer.Resources.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isInverted = parameter is string param && param.Equals("Inverted", StringComparison.OrdinalIgnoreCase);
        var isNull = value is null;
        
        if (isInverted)
            return !isNull;
        
        return isNull;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}