using System.Globalization;

namespace SirSquintsDndAssistant.Converters;

public class DifficultyColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string difficulty)
        {
            return difficulty switch
            {
                "Trivial" => Color.FromArgb("#90EE90"),
                "Easy" => Color.FromArgb("#228B22"),
                "Medium" => Color.FromArgb("#DAA520"),
                "Hard" => Color.FromArgb("#FF8C00"),
                "Deadly" => Color.FromArgb("#DC143C"),
                _ => Color.FromArgb("#808080")
            };
        }
        return Color.FromArgb("#808080");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
