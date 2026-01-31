using System.Globalization;
using SirSquintsDndAssistant.Models.Combat;

namespace SirSquintsDndAssistant.Converters;

public class HpToProgressConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is InitiativeEntry entry && entry.MaxHitPoints > 0)
        {
            return (double)entry.CurrentHitPoints / entry.MaxHitPoints;
        }
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
