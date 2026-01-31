using System.Text.Json.Serialization;

namespace SirSquintsDndAssistant.Models.Creatures;

/// <summary>
/// Represents a monster action or special ability for display
/// </summary>
public class MonsterAbility
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;
}
