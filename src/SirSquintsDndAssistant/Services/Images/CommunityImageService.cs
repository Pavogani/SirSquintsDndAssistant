using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace SirSquintsDndAssistant.Services.Images;

/// <summary>
/// Service for fetching images from community sources and caching them locally.
/// Uses multiple fallback sources for D&D content images.
/// Implements LRU cache with size limits to prevent memory leaks.
/// </summary>
public class CommunityImageService : ICommunityImageService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _cacheFolder;
    private readonly object _cacheLock = new();
    private bool _disposed;

    // LRU cache with size limits to prevent memory leaks
    private const int MaxMemoryCacheSize = 100;  // Max ImageSource objects in memory
    private const int MaxUrlCacheSize = 500;     // Max URL lookup cache entries

    private readonly Dictionary<string, ImageSource> _memoryCache = new();
    private readonly LinkedList<string> _memoryCacheLru = new();
    private readonly Dictionary<string, string> _urlCache = new();
    private readonly LinkedList<string> _urlCacheLru = new();

    // Known image source patterns
    private static readonly Dictionary<string, string> MonsterTypeIcons = new()
    {
        ["aberration"] = "aberration",
        ["beast"] = "beast",
        ["celestial"] = "celestial",
        ["construct"] = "construct",
        ["dragon"] = "dragon",
        ["elemental"] = "elemental",
        ["fey"] = "fey",
        ["fiend"] = "fiend",
        ["giant"] = "giant",
        ["humanoid"] = "humanoid",
        ["monstrosity"] = "monstrosity",
        ["ooze"] = "ooze",
        ["plant"] = "plant",
        ["undead"] = "undead"
    };

    private static readonly Dictionary<string, string> SpellSchoolIcons = new()
    {
        ["abjuration"] = "abjuration",
        ["conjuration"] = "conjuration",
        ["divination"] = "divination",
        ["enchantment"] = "enchantment",
        ["evocation"] = "evocation",
        ["illusion"] = "illusion",
        ["necromancy"] = "necromancy",
        ["transmutation"] = "transmutation"
    };

    public CommunityImageService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("ImageClient");
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("DndGmAssistant", "1.0"));
        _httpClient.Timeout = TimeSpan.FromSeconds(15);

        _cacheFolder = Path.Combine(FileSystem.AppDataDirectory, "image_cache");
        EnsureCacheFolderExists();
    }

    private void EnsureCacheFolderExists()
    {
        try
        {
            if (!Directory.Exists(_cacheFolder))
            {
                Directory.CreateDirectory(_cacheFolder);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating cache folder: {ex.Message}");
        }
    }

    public async Task<string?> GetMonsterImageUrlAsync(string monsterName, string? monsterType = null)
    {
        if (string.IsNullOrWhiteSpace(monsterName))
            return null;

        var cacheKey = $"monster_{monsterName.ToLowerInvariant()}";

        // Check URL cache with LRU tracking
        var cachedUrl = GetFromUrlCache(cacheKey);
        if (cachedUrl != null)
            return cachedUrl;

        // Try multiple sources in order of preference
        var urls = GenerateMonsterImageUrls(monsterName, monsterType);

        foreach (var url in urls)
        {
            if (await IsImageAccessibleAsync(url))
            {
                AddToUrlCache(cacheKey, url);
                return url;
            }
        }

        return null;
    }

    public async Task<string?> GetSpellImageUrlAsync(string spellName, string? school = null)
    {
        if (string.IsNullOrWhiteSpace(spellName))
            return null;

        var cacheKey = $"spell_{spellName.ToLowerInvariant()}";
        var cachedUrl = GetFromUrlCache(cacheKey);
        if (cachedUrl != null)
            return cachedUrl;

        var urls = GenerateSpellImageUrls(spellName, school);

        foreach (var url in urls)
        {
            if (await IsImageAccessibleAsync(url))
            {
                AddToUrlCache(cacheKey, url);
                return url;
            }
        }

        return null;
    }

    public async Task<string?> GetItemImageUrlAsync(string itemName, string? itemType = null)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return null;

        var cacheKey = $"item_{itemName.ToLowerInvariant()}";
        var cachedUrl = GetFromUrlCache(cacheKey);
        if (cachedUrl != null)
            return cachedUrl;

        var urls = GenerateItemImageUrls(itemName, itemType);

        foreach (var url in urls)
        {
            if (await IsImageAccessibleAsync(url))
            {
                AddToUrlCache(cacheKey, url);
                return url;
            }
        }

        return null;
    }

    private IEnumerable<string> GenerateMonsterImageUrls(string monsterName, string? monsterType)
    {
        if (string.IsNullOrWhiteSpace(monsterName))
            yield break;

        var slug = NameToSlug(monsterName);
        var slugDash = NameToSlugDash(monsterName);
        var encodedName = Uri.EscapeDataString(monsterName);

        // 5e.tools - Most reliable, check common books first
        var priorityBooks = new[] { "MM", "MPMM", "VGM", "MTF", "FTD", "ToB1", "ToB", "CC" };
        foreach (var book in priorityBooks)
        {
            yield return $"https://5e.tools/img/bestiary/{book}/{encodedName}.webp";
        }

        // AideDD (French D&D site with decent coverage)
        yield return $"https://www.aidedd.org/dnd/images/{slug}.jpg";

        // Open5e static images
        yield return $"https://api.open5e.com/static/img/monsters/{slugDash}.png";

        // More 5e.tools books
        var moreBooks = new[] { "BGG", "TftYP", "CoS", "OotA", "ToA", "SKT", "WDH", "BGDIA", "IDRotF", "ToB2", "ToB3" };
        foreach (var book in moreBooks)
        {
            yield return $"https://5e.tools/img/bestiary/{book}/{encodedName}.webp";
        }
    }

    private IEnumerable<string> GenerateSpellImageUrls(string spellName, string? school)
    {
        var slug = NameToSlug(spellName);
        var encodedName = Uri.EscapeDataString(spellName);

        // 5e.tools spell images
        yield return $"https://5e.tools/img/spells/PHB/{encodedName}.webp";
        yield return $"https://5e.tools/img/spells/XGE/{encodedName}.webp";
        yield return $"https://5e.tools/img/spells/TCE/{encodedName}.webp";

        // Open Game Art (limited coverage)
        yield return $"https://opengameart.org/sites/default/files/spell_{slug}.png";

        // NOTE: Wikia URLs removed - pattern was malformed and unreliable
    }

    private IEnumerable<string> GenerateItemImageUrls(string itemName, string? itemType)
    {
        var slug = NameToSlug(itemName);
        var encodedName = Uri.EscapeDataString(itemName);

        // 5e.tools item images
        yield return $"https://5e.tools/img/items/DMG/{encodedName}.webp";
        yield return $"https://5e.tools/img/items/PHB/{encodedName}.webp";
        yield return $"https://5e.tools/img/items/XGE/{encodedName}.webp";

        // Open Game Art (limited coverage)
        yield return $"https://opengameart.org/sites/default/files/item_{slug}.png";

        // NOTE: Wikia URLs removed - pattern was malformed and unreliable
    }

    private static string NameToSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "unknown";

        return name
            .ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("'", "")
            .Replace(",", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("-", "_");
    }

    private static string NameToSlugDash(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "unknown";

        return name
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace(",", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("_", "-");
    }

    private async Task<bool> IsImageAccessibleAsync(string url)
    {
        try
        {
            // Try HEAD request first (more efficient)
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
            using var headResponse = await _httpClient.SendAsync(headRequest);

            if (headResponse.IsSuccessStatusCode)
            {
                var contentType = headResponse.Content.Headers.ContentType?.MediaType;
                return contentType?.StartsWith("image/") == true;
            }

            // Some servers don't support HEAD, fall back to GET with range header
            if (headResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed ||
                headResponse.StatusCode == System.Net.HttpStatusCode.NotImplemented)
            {
                using var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
                getRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0); // Request only first byte
                using var getResponse = await _httpClient.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead);

                if (getResponse.IsSuccessStatusCode || getResponse.StatusCode == System.Net.HttpStatusCode.PartialContent)
                {
                    var contentType = getResponse.Content.Headers.ContentType?.MediaType;
                    return contentType?.StartsWith("image/") == true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking image URL {url}: {ex.Message}");
        }

        return false;
    }

    public async Task<string?> DownloadAndCacheImageAsync(string imageUrl, string prefix, string name)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        try
        {
            // Generate a unique filename based on URL hash
            var urlHash = GenerateUrlHash(imageUrl);
            var slug = NameToSlug(name);
            var extension = GetImageExtension(imageUrl);
            var fileName = $"{prefix}_{slug}_{urlHash}{extension}";
            var localPath = Path.Combine(_cacheFolder, fileName);

            // Check if already cached
            if (File.Exists(localPath))
                return localPath;

            // Download the image
            using var response = await _httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType?.StartsWith("image/") != true)
                return null;

            // Save to cache
            var bytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(localPath, bytes);

            System.Diagnostics.Debug.WriteLine($"Cached image: {fileName}");
            return localPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error downloading image {imageUrl}: {ex.Message}");
            return null;
        }
    }

    public async Task<ImageSource?> GetImageSourceAsync(string? imageUrl, string? localPath)
    {
        // Priority: local path > URL
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            var cached = GetFromMemoryCache(localPath);
            if (cached != null)
                return cached;

            if (File.Exists(localPath))
            {
                try
                {
                    var source = await Task.Run(() =>
                    {
                        using var stream = File.OpenRead(localPath);
                        var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        ms.Position = 0;
                        return ImageSource.FromStream(() => new MemoryStream(ms.ToArray()));
                    });

                    AddToMemoryCache(localPath, source);
                    return source;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading local image: {ex.Message}");
                }
            }
        }

        // Try URL - trust it if we have one (don't validate every time, too slow)
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            var cached = GetFromMemoryCache(imageUrl);
            if (cached != null)
                return cached;

            try
            {
                var source = ImageSource.FromUri(new Uri(imageUrl));
                AddToMemoryCache(imageUrl, source);
                return source;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading URL image: {ex.Message}");
            }
        }

        return null;
    }

    /// <summary>
    /// Validates a URL and returns it if accessible, otherwise searches for an alternative.
    /// </summary>
    /// <param name="existingUrl">Existing URL to validate first</param>
    /// <param name="name">Name of the content to search for</param>
    /// <param name="type">Subtype (e.g., monster type, spell school)</param>
    /// <param name="contentType">Type of content: "monster", "spell", or "item"</param>
    public async Task<string?> ValidateOrFindImageUrlAsync(string? existingUrl, string name, string? type = null, string contentType = "monster")
    {
        // First try the existing URL
        if (!string.IsNullOrWhiteSpace(existingUrl))
        {
            if (await IsImageAccessibleAsync(existingUrl))
                return existingUrl;
        }

        // Fall back to searching based on content type
        return contentType.ToLowerInvariant() switch
        {
            "spell" => await GetSpellImageUrlAsync(name, type),
            "item" or "equipment" or "magicitem" => await GetItemImageUrlAsync(name, type),
            _ => await GetMonsterImageUrlAsync(name, type)
        };
    }

    private static string GenerateUrlHash(string url)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(bytes)[..8].ToLowerInvariant();
    }

    private static string GetImageExtension(string url)
    {
        var uri = new Uri(url);
        var path = uri.AbsolutePath.ToLowerInvariant();

        if (path.EndsWith(".png")) return ".png";
        if (path.EndsWith(".jpg") || path.EndsWith(".jpeg")) return ".jpg";
        if (path.EndsWith(".gif")) return ".gif";
        if (path.EndsWith(".webp")) return ".webp";
        if (path.EndsWith(".svg")) return ".svg";

        return ".png"; // Default
    }

    public void ClearImageCache()
    {
        lock (_cacheLock)
        {
            _memoryCache.Clear();
            _memoryCacheLru.Clear();
            _urlCache.Clear();
            _urlCacheLru.Clear();
        }

        try
        {
            if (Directory.Exists(_cacheFolder))
            {
                foreach (var file in Directory.GetFiles(_cacheFolder))
                {
                    try { File.Delete(file); }
                    catch { /* ignore individual file errors */ }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing cache folder: {ex.Message}");
        }
    }

    #region LRU Cache Helpers

    private string? GetFromUrlCache(string key)
    {
        lock (_cacheLock)
        {
            if (_urlCache.TryGetValue(key, out var value))
            {
                // Move to front of LRU list (most recently used)
                _urlCacheLru.Remove(key);
                _urlCacheLru.AddFirst(key);
                return value;
            }
            return null;
        }
    }

    private void AddToUrlCache(string key, string value)
    {
        lock (_cacheLock)
        {
            // If already exists, update and move to front
            if (_urlCache.ContainsKey(key))
            {
                _urlCache[key] = value;
                _urlCacheLru.Remove(key);
                _urlCacheLru.AddFirst(key);
                return;
            }

            // Evict oldest if at capacity
            while (_urlCache.Count >= MaxUrlCacheSize && _urlCacheLru.Count > 0)
            {
                var oldest = _urlCacheLru.Last!.Value;
                _urlCacheLru.RemoveLast();
                _urlCache.Remove(oldest);
            }

            // Add new entry
            _urlCache[key] = value;
            _urlCacheLru.AddFirst(key);
        }
    }

    private ImageSource? GetFromMemoryCache(string key)
    {
        lock (_cacheLock)
        {
            if (_memoryCache.TryGetValue(key, out var value))
            {
                // Move to front of LRU list (most recently used)
                _memoryCacheLru.Remove(key);
                _memoryCacheLru.AddFirst(key);
                return value;
            }
            return null;
        }
    }

    private void AddToMemoryCache(string key, ImageSource value)
    {
        lock (_cacheLock)
        {
            // If already exists, update and move to front
            if (_memoryCache.ContainsKey(key))
            {
                _memoryCache[key] = value;
                _memoryCacheLru.Remove(key);
                _memoryCacheLru.AddFirst(key);
                return;
            }

            // Evict oldest if at capacity
            while (_memoryCache.Count >= MaxMemoryCacheSize && _memoryCacheLru.Count > 0)
            {
                var oldest = _memoryCacheLru.Last!.Value;
                _memoryCacheLru.RemoveLast();
                _memoryCache.Remove(oldest);
                System.Diagnostics.Debug.WriteLine($"LRU evicted from memory cache: {oldest}");
            }

            // Add new entry
            _memoryCache[key] = value;
            _memoryCacheLru.AddFirst(key);
        }
    }

    #endregion

    public string GetMonsterPlaceholderResource(string? monsterType)
    {
        if (string.IsNullOrWhiteSpace(monsterType))
            return "monster_default.png";

        var typeKey = monsterType.ToLowerInvariant().Trim();

        // Map common type variations to standard types
        if (typeKey.Contains("aberration")) typeKey = "aberration";
        else if (typeKey.Contains("beast")) typeKey = "beast";
        else if (typeKey.Contains("celestial")) typeKey = "celestial";
        else if (typeKey.Contains("construct")) typeKey = "construct";
        else if (typeKey.Contains("dragon")) typeKey = "dragon";
        else if (typeKey.Contains("elemental")) typeKey = "elemental";
        else if (typeKey.Contains("fey")) typeKey = "fey";
        else if (typeKey.Contains("fiend")) typeKey = "fiend";
        else if (typeKey.Contains("giant")) typeKey = "giant";
        else if (typeKey.Contains("humanoid")) typeKey = "humanoid";
        else if (typeKey.Contains("monstrosity")) typeKey = "monstrosity";
        else if (typeKey.Contains("ooze")) typeKey = "ooze";
        else if (typeKey.Contains("plant")) typeKey = "plant";
        else if (typeKey.Contains("undead")) typeKey = "undead";
        else return "monster_default.png";

        return MonsterTypeIcons.TryGetValue(typeKey, out var icon)
            ? $"monster_{icon}.png"
            : "monster_default.png";
    }

    public string GetSpellPlaceholderResource(string? school)
    {
        if (string.IsNullOrWhiteSpace(school))
            return "spell_default.png";

        var schoolKey = school.ToLowerInvariant().Trim();

        return SpellSchoolIcons.TryGetValue(schoolKey, out var icon)
            ? $"spell_{icon}.png"
            : "spell_default.png";
    }

    public string GetItemPlaceholderResource(string? itemType)
    {
        if (string.IsNullOrWhiteSpace(itemType))
            return "item_default.png";

        var typeKey = itemType.ToLowerInvariant().Trim();

        // Map item types to placeholder resources
        if (typeKey.Contains("weapon") || typeKey.Contains("sword") || typeKey.Contains("axe") ||
            typeKey.Contains("bow") || typeKey.Contains("dagger") || typeKey.Contains("mace"))
            return "item_weapon.png";

        if (typeKey.Contains("armor") || typeKey.Contains("shield") || typeKey.Contains("mail"))
            return "item_armor.png";

        if (typeKey.Contains("potion") || typeKey.Contains("elixir"))
            return "item_potion.png";

        if (typeKey.Contains("scroll") || typeKey.Contains("book") || typeKey.Contains("tome"))
            return "item_scroll.png";

        if (typeKey.Contains("wondrous") || typeKey.Contains("magic"))
            return "item_wondrous.png";

        if (typeKey.Contains("ring"))
            return "item_ring.png";

        if (typeKey.Contains("wand") || typeKey.Contains("rod") || typeKey.Contains("staff"))
            return "item_wand.png";

        return "item_default.png";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_cacheLock)
        {
            _memoryCache.Clear();
            _memoryCacheLru.Clear();
            _urlCache.Clear();
            _urlCacheLru.Clear();
        }
        _httpClient.Dispose();
    }
}
