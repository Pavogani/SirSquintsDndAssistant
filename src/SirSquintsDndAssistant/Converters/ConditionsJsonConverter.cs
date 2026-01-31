using System.Globalization;
using System.Text.Json;

namespace SirSquintsDndAssistant.Converters;

/// <summary>
/// Converts a JSON array of condition strings to a comma-separated display string.
/// </summary>
public class ConditionsJsonConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string json && !string.IsNullOrEmpty(json))
        {
            try
            {
                var conditions = JsonSerializer.Deserialize<List<string>>(json);
                if (conditions != null && conditions.Count > 0)
                {
                    return string.Join(", ", conditions);
                }
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConditionsJsonConverter: Error parsing JSON: {ex.Message}");
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// ConvertBack is not supported for this one-way converter.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // One-way converter - ConvertBack not needed for display binding
        throw new NotSupportedException("ConditionsJsonConverter is a one-way converter.");
    }
}

/// <summary>
/// Returns true if the JSON conditions array contains any items.
/// </summary>
public class HasConditionsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string json && !string.IsNullOrEmpty(json))
        {
            try
            {
                var conditions = JsonSerializer.Deserialize<List<string>>(json);
                return conditions != null && conditions.Count > 0;
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"HasConditionsConverter: Error parsing JSON: {ex.Message}");
            }
        }
        return false;
    }

    /// <summary>
    /// ConvertBack is not supported for this one-way converter.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // One-way converter - ConvertBack not needed for display binding
        throw new NotSupportedException("HasConditionsConverter is a one-way converter.");
    }
}
