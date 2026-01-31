using SQLite;
using System.ComponentModel.DataAnnotations;

namespace SirSquintsDndAssistant.Models.Content;

public class Spell
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string ApiId { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 9, ErrorMessage = "Spell level must be between 0 (cantrip) and 9")]
    public int Level { get; set; }

    [Required]
    public string School { get; set; } = string.Empty;
    public string CastingTime { get; set; } = string.Empty;
    public string Range { get; set; } = string.Empty;
    public string Components { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string HigherLevels { get; set; } = string.Empty;
    public string ClassesJson { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public bool IsFavorite { get; set; }

    // Image support
    public string ImageUrl { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty; // Local cached/custom image
}
