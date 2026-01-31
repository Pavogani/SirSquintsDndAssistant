using System.Globalization;

namespace SirSquintsDndAssistant.Converters;

public class DeathSaveColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int count || parameter is not string param)
            return Colors.LightGray;

        // Parse the parameter to determine which indicator we're coloring
        // Format: "success1", "success2", "success3", "fail1", "fail2", "fail3"
        bool isSuccess = param.StartsWith("success");
        int index = int.Parse(param[^1..]);

        if (isSuccess)
        {
            // Green if this success slot is filled, gray if not
            return count >= index ? Colors.Green : Colors.LightGray;
        }
        else
        {
            // Red if this failure slot is filled, gray if not
            return count >= index ? Colors.Red : Colors.LightGray;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
