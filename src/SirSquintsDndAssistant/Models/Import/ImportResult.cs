namespace SirSquintsDndAssistant.Models.Import;

/// <summary>
/// Result of a data import operation.
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;

    public int TotalItems { get; set; }
    public int ItemsImported { get; set; }
    public int ItemsSkipped { get; set; }
    public int DuplicatesFound { get; set; }
    public int ErrorCount { get; set; }

    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<ImportedItem> ImportedItems { get; set; } = new();

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;

    public string Summary => $"Imported {ItemsImported}/{TotalItems} items, {DuplicatesFound} duplicates, {ErrorCount} errors";
}

/// <summary>
/// Information about an imported item.
/// </summary>
public class ImportedItem
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Monster", "Spell", "Item", etc.
    public int DatabaseId { get; set; }
    public bool WasDuplicate { get; set; }
    public string? DuplicateOf { get; set; }
}

/// <summary>
/// Configuration for import behavior.
/// </summary>
public class ImportOptions
{
    /// <summary>Skip items that already exist in the database.</summary>
    public bool SkipDuplicates { get; set; } = true;

    /// <summary>Update existing items if they already exist.</summary>
    public bool UpdateExisting { get; set; } = false;

    /// <summary>Source name to mark imported items with.</summary>
    public string SourceName { get; set; } = "import";

    /// <summary>Only import items matching this filter (e.g., CR range, type).</summary>
    public ImportFilter? Filter { get; set; }
}

/// <summary>
/// Filter criteria for imports.
/// </summary>
public class ImportFilter
{
    public double? MinChallengeRating { get; set; }
    public double? MaxChallengeRating { get; set; }
    public List<string>? Types { get; set; }
    public List<string>? Sources { get; set; }
    public string? NameContains { get; set; }
}
