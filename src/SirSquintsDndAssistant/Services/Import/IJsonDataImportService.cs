using SirSquintsDndAssistant.Models.Import;

namespace SirSquintsDndAssistant.Services.Import;

/// <summary>
/// Service for importing D&D data from various JSON formats.
/// Supports 5e.tools, Kobold Fight Club, CritterDB, and custom formats.
/// </summary>
public interface IJsonDataImportService
{
    /// <summary>
    /// Import monsters from a JSON file.
    /// Auto-detects format (5e.tools, KFC, CritterDB, or custom).
    /// </summary>
    Task<ImportResult> ImportMonstersAsync(string filePath, ImportOptions? options = null);

    /// <summary>
    /// Import monsters from a JSON string.
    /// </summary>
    Task<ImportResult> ImportMonstersFromJsonAsync(string json, ImportOptions? options = null);

    /// <summary>
    /// Import spells from a JSON file.
    /// </summary>
    Task<ImportResult> ImportSpellsAsync(string filePath, ImportOptions? options = null);

    /// <summary>
    /// Import spells from a JSON string.
    /// </summary>
    Task<ImportResult> ImportSpellsFromJsonAsync(string json, ImportOptions? options = null);

    /// <summary>
    /// Import items from a JSON file.
    /// </summary>
    Task<ImportResult> ImportItemsAsync(string filePath, ImportOptions? options = null);

    /// <summary>
    /// Import items from a JSON string.
    /// </summary>
    Task<ImportResult> ImportItemsFromJsonAsync(string json, ImportOptions? options = null);

    /// <summary>
    /// Detect the format of a JSON file.
    /// </summary>
    Task<JsonDataFormat> DetectFormatAsync(string filePath);

    /// <summary>
    /// Detect the format of a JSON string.
    /// </summary>
    JsonDataFormat DetectFormat(string json);

    /// <summary>
    /// Validate a JSON file for import compatibility.
    /// </summary>
    Task<(bool IsValid, List<string> Errors)> ValidateFileAsync(string filePath);
}

/// <summary>
/// Supported JSON data formats.
/// </summary>
public enum JsonDataFormat
{
    Unknown,
    FiveETools,      // 5e.tools format
    KoboldFightClub, // KFC export
    CritterDb,       // CritterDB export
    Open5e,          // Open5e API format
    Custom           // Generic/custom format
}
