namespace SirSquintsDndAssistant.Models.Validation;

/// <summary>
/// Validation result containing any errors found.
/// </summary>
public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();

    public void AddError(string error) => Errors.Add(error);

    public static ValidationResult Success() => new();

    public static ValidationResult Failure(string error)
    {
        var result = new ValidationResult();
        result.AddError(error);
        return result;
    }
}

/// <summary>
/// Static validation methods for model objects.
/// </summary>
public static class ModelValidator
{
    /// <summary>
    /// Validate a BattleMap object.
    /// </summary>
    public static ValidationResult ValidateBattleMap(BattleMap.BattleMap map)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(map.Name))
            result.AddError("Map name is required.");

        if (map.GridWidth < 5)
            result.AddError("Grid width must be at least 5.");

        if (map.GridWidth > 100)
            result.AddError("Grid width cannot exceed 100.");

        if (map.GridHeight < 5)
            result.AddError("Grid height must be at least 5.");

        if (map.GridHeight > 100)
            result.AddError("Grid height cannot exceed 100.");

        if (map.CellSize < 1)
            result.AddError("Cell size must be at least 1.");

        if (map.CellSize > 20)
            result.AddError("Cell size cannot exceed 20 feet.");

        if (map.GridOpacity < 0 || map.GridOpacity > 1)
            result.AddError("Grid opacity must be between 0 and 1.");

        return result;
    }

    /// <summary>
    /// Validate a Monster object.
    /// </summary>
    public static ValidationResult ValidateMonster(Creatures.Monster monster)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(monster.Name))
            result.AddError("Monster name is required.");

        if (monster.ChallengeRating < 0)
            result.AddError("Challenge rating cannot be negative.");

        if (monster.ChallengeRating > 30)
            result.AddError("Challenge rating cannot exceed 30.");

        if (monster.ArmorClass < 0)
            result.AddError("Armor class cannot be negative.");

        if (monster.HitPoints < 0)
            result.AddError("Hit points cannot be negative.");

        // Validate ability scores (1-30 is normal range, but allow flexibility)
        ValidateAbilityScore(result, monster.Strength, "Strength");
        ValidateAbilityScore(result, monster.Dexterity, "Dexterity");
        ValidateAbilityScore(result, monster.Constitution, "Constitution");
        ValidateAbilityScore(result, monster.Intelligence, "Intelligence");
        ValidateAbilityScore(result, monster.Wisdom, "Wisdom");
        ValidateAbilityScore(result, monster.Charisma, "Charisma");

        return result;
    }

    /// <summary>
    /// Validate a Spell object.
    /// </summary>
    public static ValidationResult ValidateSpell(Content.Spell spell)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(spell.Name))
            result.AddError("Spell name is required.");

        if (spell.Level < 0)
            result.AddError("Spell level cannot be negative.");

        if (spell.Level > 9)
            result.AddError("Spell level cannot exceed 9.");

        if (string.IsNullOrWhiteSpace(spell.School))
            result.AddError("Spell school is required.");

        return result;
    }

    /// <summary>
    /// Validate an Equipment object.
    /// </summary>
    public static ValidationResult ValidateEquipment(Content.Equipment equipment)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(equipment.Name))
            result.AddError("Equipment name is required.");

        if (equipment.Cost < 0)
            result.AddError("Cost cannot be negative.");

        if (equipment.Weight < 0)
            result.AddError("Weight cannot be negative.");

        return result;
    }

    /// <summary>
    /// Validate a MagicItem object.
    /// </summary>
    public static ValidationResult ValidateMagicItem(Content.MagicItem item)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(item.Name))
            result.AddError("Magic item name is required.");

        return result;
    }

    /// <summary>
    /// Validate a MapToken object.
    /// </summary>
    public static ValidationResult ValidateMapToken(BattleMap.MapToken token)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(token.Name))
            result.AddError("Token name is required.");

        if (token.GridX < 0)
            result.AddError("Grid X position cannot be negative.");

        if (token.GridY < 0)
            result.AddError("Grid Y position cannot be negative.");

        if (token.MaxHP < 0)
            result.AddError("Max HP cannot be negative.");

        if (token.CurrentHP < 0)
            result.AddError("Current HP cannot be negative.");

        if (token.MovementTotal < 0)
            result.AddError("Movement total cannot be negative.");

        return result;
    }

    private static void ValidateAbilityScore(ValidationResult result, int score, string abilityName)
    {
        if (score < 0)
            result.AddError($"{abilityName} cannot be negative.");

        if (score > 50)
            result.AddError($"{abilityName} cannot exceed 50.");
    }
}
