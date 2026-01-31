namespace SirSquintsDndAssistant.Services.Utilities;

public class WeatherGenerator
{
    private readonly Random _random = new();

    private readonly string[] _temperatures = { "Freezing", "Cold", "Cool", "Mild", "Warm", "Hot", "Scorching" };
    private readonly string[] _conditions = { "Clear skies", "Partly cloudy", "Overcast", "Light rain", "Heavy rain", "Thunderstorm", "Light snow", "Heavy snow", "Fog", "Windy", "Storm" };
    private readonly string[] _precipitation = { "None", "Light drizzle", "Rain", "Heavy rain", "Snow", "Hail", "Sleet" };

    public string GenerateWeather()
    {
        var temp = _temperatures[_random.Next(_temperatures.Length)];
        var condition = _conditions[_random.Next(_conditions.Length)];
        var wind = _random.Next(0, 40);

        return $"{temp}, {condition}, Wind: {wind} mph";
    }

    public string GenerateDetailedWeather()
    {
        var temp = _temperatures[_random.Next(_temperatures.Length)];
        var condition = _conditions[_random.Next(_conditions.Length)];
        var precip = _precipitation[_random.Next(_precipitation.Length)];
        var wind = _random.Next(0, 50);
        var visibility = _random.Next(1, 11);

        return $"Temperature: {temp}\nConditions: {condition}\nPrecipitation: {precip}\nWind Speed: {wind} mph\nVisibility: {visibility}/10";
    }
}
