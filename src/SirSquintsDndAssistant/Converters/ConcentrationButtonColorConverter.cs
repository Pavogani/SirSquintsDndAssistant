using System.Globalization;

namespace SirSquintsDndAssistant.Converters;

public class ConcentrationButtonColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConcentrating && isConcentrating)
        {
            return Color.FromArgb("#FF8C00"); // Orange when concentrating
        }
        return Color.FromArgb("#696969"); // Gray when not concentrating
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
