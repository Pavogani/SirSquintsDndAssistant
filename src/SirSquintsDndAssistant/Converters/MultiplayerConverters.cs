using System.Globalization;

namespace SirSquintsDndAssistant.Converters;

public class BoolToConnectionColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
            return isConnected ? Color.FromArgb("#4CAF50") : Color.FromArgb("#9E9E9E");
        return Color.FromArgb("#9E9E9E");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToReadyColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isReady)
            return isReady ? Color.FromArgb("#4CAF50") : Color.FromArgb("#FF9800");
        return Color.FromArgb("#FF9800");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToReadyTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isReady)
            return isReady ? "Ready" : "Not Ready";
        return "Not Ready";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToNat20ColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isNat20 && isNat20)
            return Color.FromArgb("#FFD700"); // Gold for nat 20
        return Color.FromArgb("#7B1FA2"); // Default purple
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
