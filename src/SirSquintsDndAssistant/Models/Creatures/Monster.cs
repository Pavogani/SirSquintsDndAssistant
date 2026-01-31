using SQLite;
using System.ComponentModel.DataAnnotations;

namespace SirSquintsDndAssistant.Models.Creatures;

public class Monster
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string ApiId { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Alignment { get; set; } = string.Empty;

    [Range(0, 40, ErrorMessage = "Armor Class must be between 0 and 40")]
    public int ArmorClass { get; set; }

    [Range(1, 1000, ErrorMessage = "Hit Points must be between 1 and 1000")]
    public int HitPoints { get; set; }

    public string HitDice { get; set; } = string.Empty;

    [Range(0, 30, ErrorMessage = "Challenge Rating must be between 0 and 30")]
    public double ChallengeRating { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Experience Points must be non-negative")]
    public int ExperiencePoints { get; set; }

    // Ability Scores (1-30 per D&D rules)
    [Range(1, 30)] public int Strength { get; set; }
    [Range(1, 30)] public int Dexterity { get; set; }
    [Range(1, 30)] public int Constitution { get; set; }
    [Range(1, 30)] public int Intelligence { get; set; }
    [Range(1, 30)] public int Wisdom { get; set; }
    [Range(1, 30)] public int Charisma { get; set; }

    // Serialized JSON for complex data
    public string SpeedsJson { get; set; } = string.Empty;
    public string SkillsJson { get; set; } = string.Empty;
    public string ActionsJson { get; set; } = string.Empty;
    public string SpecialAbilitiesJson { get; set; } = string.Empty;
    public string SavingThrowsJson { get; set; } = string.Empty;
    public string LegendaryActionsJson { get; set; } = string.Empty;
    public string ReactionsJson { get; set; } = string.Empty;

    // Defenses
    public string DamageResistances { get; set; } = string.Empty;
    public string DamageImmunities { get; set; } = string.Empty;
    public string DamageVulnerabilities { get; set; } = string.Empty;
    public string ConditionImmunities { get; set; } = string.Empty;

    // Senses and Languages
    public string Senses { get; set; } = string.Empty;
    public string Languages { get; set; } = string.Empty;

    // Description/Lore
    public string Description { get; set; } = string.Empty;
    public string LegendaryDescription { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty; // "dnd5eapi" or "open5e:tob2" etc.
    public DateTime LastUpdated { get; set; }
    public bool IsFavorite { get; set; }

    // Image support
    public string ImageUrl { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty; // Local cached/custom image
}
