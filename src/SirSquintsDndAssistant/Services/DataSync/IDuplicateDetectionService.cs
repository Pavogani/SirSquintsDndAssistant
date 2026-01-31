using SirSquintsDndAssistant.Models.Creatures;
using SirSquintsDndAssistant.Models.Content;

namespace SirSquintsDndAssistant.Services.DataSync;

/// <summary>
/// Service for detecting duplicate entries during data sync and import.
/// </summary>
public interface IDuplicateDetectionService
{
    /// <summary>
    /// Check if a monster already exists in the database.
    /// </summary>
    Task<DuplicateCheckResult<Monster>> CheckMonsterDuplicateAsync(Monster monster);

    /// <summary>
    /// Check if a spell already exists in the database.
    /// </summary>
    Task<DuplicateCheckResult<Spell>> CheckSpellDuplicateAsync(Spell spell);

    /// <summary>
    /// Check if an equipment item already exists in the database.
    /// </summary>
    Task<DuplicateCheckResult<Equipment>> CheckEquipmentDuplicateAsync(Equipment equipment);

    /// <summary>
    /// Find all duplicates in the monster database.
    /// </summary>
    Task<List<DuplicateGroup<Monster>>> FindAllMonsterDuplicatesAsync();

    /// <summary>
    /// Find all duplicates in the spell database.
    /// </summary>
    Task<List<DuplicateGroup<Spell>>> FindAllSpellDuplicatesAsync();

    /// <summary>
    /// Find all duplicates in the equipment database.
    /// </summary>
    Task<List<DuplicateGroup<Equipment>>> FindAllEquipmentDuplicatesAsync();

    /// <summary>
    /// Merge duplicate entries, keeping the preferred source.
    /// </summary>
    Task<MergeResult> MergeDuplicatesAsync<T>(List<T> duplicates, T preferred) where T : class, new();

    /// <summary>
    /// Delete all duplicate entries, keeping only the preferred one.
    /// </summary>
    Task<int> DeleteDuplicatesAsync<T>(List<int> idsToDelete) where T : class, new();
}

/// <summary>
/// Result of a duplicate check operation.
/// </summary>
public class DuplicateCheckResult<T>
{
    public bool IsDuplicate { get; set; }
    public T? ExistingItem { get; set; }
    public DuplicateMatchType MatchType { get; set; }
    public double MatchConfidence { get; set; } // 0.0 to 1.0
    public string MatchReason { get; set; } = string.Empty;
}

/// <summary>
/// How closely two items match.
/// </summary>
public enum DuplicateMatchType
{
    None,
    ExactName,      // Name matches exactly
    NormalizedName, // Name matches after normalization
    PartialMatch,   // Partial name match with other criteria
    ApiIdMatch      // Same API ID from source
}

/// <summary>
/// A group of duplicate entries.
/// </summary>
public class DuplicateGroup<T>
{
    public string Name { get; set; } = string.Empty;
    public List<DuplicateEntry<T>> Entries { get; set; } = new();
    public T? PreferredEntry { get; set; }
}

/// <summary>
/// An entry in a duplicate group with metadata.
/// </summary>
public class DuplicateEntry<T>
{
    public T Item { get; set; } = default!;
    public string Source { get; set; } = string.Empty;
    public int SourcePriority { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Result of a merge operation.
/// </summary>
public class MergeResult
{
    public bool Success { get; set; }
    public int ItemsDeleted { get; set; }
    public int ItemsKept { get; set; }
    public string Message { get; set; } = string.Empty;
}
