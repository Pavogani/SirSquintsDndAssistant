namespace SirSquintsDndAssistant.Services.Images;

public interface ICommunityImageService
{
    Task<string?> GetMonsterImageUrlAsync(string monsterName, string? monsterType = null);
    Task<string?> GetSpellImageUrlAsync(string spellName, string? school = null);
    Task<string?> GetItemImageUrlAsync(string itemName, string? itemType = null);
    Task<string?> DownloadAndCacheImageAsync(string imageUrl, string prefix, string name);
    Task<ImageSource?> GetImageSourceAsync(string? imageUrl, string? localPath);
    Task<string?> ValidateOrFindImageUrlAsync(string? existingUrl, string name, string? type = null, string contentType = "monster");
    void ClearImageCache();

    /// <summary>
    /// Gets a placeholder image resource name based on monster type.
    /// Returns a resource name like "monster_aberration.png" that can be used with ImageSource.FromFile().
    /// </summary>
    string GetMonsterPlaceholderResource(string? monsterType);

    /// <summary>
    /// Gets a placeholder image resource name based on spell school.
    /// Returns a resource name like "spell_evocation.png" that can be used with ImageSource.FromFile().
    /// </summary>
    string GetSpellPlaceholderResource(string? school);

    /// <summary>
    /// Gets a placeholder image resource name for items.
    /// Returns a resource name like "item_weapon.png" that can be used with ImageSource.FromFile().
    /// </summary>
    string GetItemPlaceholderResource(string? itemType);
}

public class ImageSearchResult
{
    public string Url { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Attribution { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
