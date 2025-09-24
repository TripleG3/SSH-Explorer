using System.Globalization;

namespace SSHExplorer.Resources.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string colorParams)
            return Colors.Transparent;

        var colors = colorParams.Split(',');
        if (colors.Length != 2)
            return Colors.Transparent;

        var trueColor = colors[0].Trim();
        var falseColor = colors[1].Trim();

        var colorName = boolValue ? trueColor : falseColor;
        
        // Try to get the color from the Colors class
        var colorProperty = typeof(Colors).GetProperty(colorName);
        if (colorProperty != null)
        {
            return (Color?)colorProperty.GetValue(null) ?? Colors.Transparent;
        }

        // Try to parse as hex color
        if (Color.TryParse(colorName, out var color))
        {
            return color;
        }

        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}