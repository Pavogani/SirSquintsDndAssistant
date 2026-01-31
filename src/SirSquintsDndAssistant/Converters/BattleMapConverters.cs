using System.Globalization;
using SirSquintsDndAssistant.Input;

namespace SirSquintsDndAssistant.Converters;

public class BoolToOnOffConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? "ON" : "OFF";
        return "OFF";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToEnemyColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEnemy)
            return isEnemy ? Color.FromArgb("#DC143C") : Color.FromArgb("#4169E1");
        return Color.FromArgb("#4169E1");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToEnemyTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEnemy)
            return isEnemy ? "Enemy" : "Ally";
        return "Ally";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToVisibleIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isVisible)
            return isVisible ? "👁" : "👁‍🗨";
        return "👁";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts current MapTool to button background color.
/// Parameter should be the tool name to match against.
/// </summary>
public class ToolButtonColorConverter : IValueConverter
{
    private static readonly Color ActiveColor = Color.FromArgb("#4CAF50");
    private static readonly Color InactiveColor = Color.FromArgb("#666666");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MapTool currentTool && parameter is string toolName)
        {
            if (Enum.TryParse<MapTool>(toolName, out var targetTool))
            {
                return currentTool == targetTool ? ActiveColor : InactiveColor;
            }
        }
        return InactiveColor;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to toggle button color (green when on, gray when off).
/// </summary>
public class BoolToToggleColorConverter : IValueConverter
{
    private static readonly Color OnColor = Color.FromArgb("#4CAF50");
    private static readonly Color OffColor = Color.FromArgb("#666666");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOn)
            return isOn ? OnColor : OffColor;
        return OffColor;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
