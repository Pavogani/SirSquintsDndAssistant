using System.Text.RegularExpressions;
using SirSquintsDndAssistant.Models.Creatures;
using SirSquintsDndAssistant.Models.Content;
using SirSquintsDndAssistant.Services.Database;

namespace SirSquintsDndAssistant.Services.DataSync;

/// <summary>
/// Service for detecting and managing duplicate entries.
/// </summary>
public class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly IDatabaseService _databaseService;

    // Source priority (higher = more authoritative)
    private static readonly Dictionary<string, int> SourcePriority = new(StringComparer.OrdinalIgnoreCase)
    {
        { "dnd5eapi", 100 },
        { "open5e", 90 },
        { "5e.tools", 80 },
        { "5etools", 80 },
        { "kobold fight club", 70 },
        { "critterdb", 70 },
        { "custom", 50 },
        { "import", 40 },
        { "homebrew", 30 }
    };

    public DuplicateDetectionService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    #region Monster Duplicates

    public async Task<DuplicateCheckResult<Monster>> CheckMonsterDuplicateAsync(Monster monster)
    {
        var result = new DuplicateCheckResult<Monster>();
        var existing = await _databaseService.GetItemsAsync<Monster>();

        // Check API ID first
        if (!string.IsNullOrEmpty(monster.ApiId))
        {
            var apiMatch = existing.FirstOrDefault(m => m.ApiId == monster.ApiId);
            if (apiMatch != null)
            {
                return new DuplicateCheckResult<Monster>
                {
                    IsDuplicate = true,
                    ExistingItem = apiMatch,
                    MatchType = DuplicateMatchType.ApiIdMatch,
                    MatchConfidence = 1.0,
                    MatchReason = "Same API ID"
                };
            }
        }

        // Check exact name match
        var exactMatch = existing.FirstOrDefault(m =>
            string.Equals(m.Name, monster.Name, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
        {
            return new DuplicateCheckResult<Monster>
            {
                IsDuplicate = true,
                ExistingItem = exactMatch,
                MatchType = DuplicateMatchType.ExactName,
                MatchConfidence = 0.95,
                MatchReason = "Exact name match"
            };
        }

        // Check normalized name
        var normalizedName = NormalizeName(monster.Name);
        var normalizedMatch = existing.FirstOrDefault(m =>
            NormalizeName(m.Name) == normalizedName);
        if (normalizedMatch != null)
        {
            return new DuplicateCheckResult<Monster>
            {
                IsDuplicate = true,
                ExistingItem = normalizedMatch,
                MatchType = DuplicateMatchType.NormalizedName,
                MatchConfidence = 0.9,
                MatchReason = "Normalized name match"
            };
        }

        // Check partial match with CR and type
        var partialMatches = existing.Where(m =>
            FuzzyNameMatch(m.Name, monster.Name) > 0.8 &&
            m.ChallengeRating == monster.ChallengeRating &&
            string.Equals(m.Type, monster.Type, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (partialMatches.Count > 0)
        {
            var bestMatch = partialMatches.OrderByDescending(m => FuzzyNameMatch(m.Name, monster.Name)).First();
            return new DuplicateCheckResult<Monster>
            {
                IsDuplicate = true,
                ExistingItem = bestMatch,
                MatchType = DuplicateMatchType.PartialMatch,
                MatchConfidence = 0.75,
                MatchReason = "Partial name match with same CR and type"
            };
        }

        return new DuplicateCheckResult<Monster> { IsDuplicate = false };
    }

    public async Task<List<DuplicateGroup<Monster>>> FindAllMonsterDuplicatesAsync()
    {
        var monsters = await _databaseService.GetItemsAsync<Monster>();
        var groups = new List<DuplicateGroup<Monster>>();
        var processed = new HashSet<int>();

        foreach (var monster in monsters)
        {
            if (processed.Contains(monster.Id)) continue;

            var normalizedName = NormalizeName(monster.Name);
            var duplicates = monsters
                .Where(m => m.Id != monster.Id && NormalizeName(m.Name) == normalizedName)
                .ToList();

            if (duplicates.Count > 0)
            {
                var allItems = new List<Monster> { monster };
                allItems.AddRange(duplicates);

                var entries = allItems.Select(m => new DuplicateEntry<Monster>
                {
                    Item = m,
                    Source = m.Source,
                    SourcePriority = GetSourcePriority(m.Source),
                    LastUpdated = m.LastUpdated
                }).ToList();

                var preferred = entries.OrderByDescending(e => e.SourcePriority)
                    .ThenByDescending(e => e.LastUpdated)
                    .First().Item;

                groups.Add(new DuplicateGroup<Monster>
                {
                    Name = monster.Name,
                    Entries = entries,
                    PreferredEntry = preferred
                });

                foreach (var d in duplicates)
                {
                    processed.Add(d.Id);
                }
            }

            processed.Add(monster.Id);
        }

        return groups;
    }

    #endregion

    #region Spell Duplicates

    public async Task<DuplicateCheckResult<Spell>> CheckSpellDuplicateAsync(Spell spell)
    {
        var existing = await _databaseService.GetItemsAsync<Spell>();

        // Check API ID first
        if (!string.IsNullOrEmpty(spell.ApiId))
        {
            var apiMatch = existing.FirstOrDefault(s => s.ApiId == spell.ApiId);
            if (apiMatch != null)
            {
                return new DuplicateCheckResult<Spell>
                {
                    IsDuplicate = true,
                    ExistingItem = apiMatch,
                    MatchType = DuplicateMatchType.ApiIdMatch,
                    MatchConfidence = 1.0,
                    MatchReason = "Same API ID"
                };
            }
        }

        // Check exact name match
        var exactMatch = existing.FirstOrDefault(s =>
            string.Equals(s.Name, spell.Name, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
        {
            return new DuplicateCheckResult<Spell>
            {
                IsDuplicate = true,
                ExistingItem = exactMatch,
                MatchType = DuplicateMatchType.ExactName,
                MatchConfidence = 0.95,
                MatchReason = "Exact name match"
            };
        }

        // Check normalized name with level match
        var normalizedName = NormalizeName(spell.Name);
        var normalizedMatch = existing.FirstOrDefault(s =>
            NormalizeName(s.Name) == normalizedName && s.Level == spell.Level);
        if (normalizedMatch != null)
        {
            return new DuplicateCheckResult<Spell>
            {
                IsDuplicate = true,
                ExistingItem = normalizedMatch,
                MatchType = DuplicateMatchType.NormalizedName,
                MatchConfidence = 0.9,
                MatchReason = "Normalized name match with same level"
            };
        }

        return new DuplicateCheckResult<Spell> { IsDuplicate = false };
    }

    public async Task<List<DuplicateGroup<Spell>>> FindAllSpellDuplicatesAsync()
    {
        var spells = await _databaseService.GetItemsAsync<Spell>();
        var groups = new List<DuplicateGroup<Spell>>();
        var processed = new HashSet<int>();

        foreach (var spell in spells)
        {
            if (processed.Contains(spell.Id)) continue;

            var normalizedName = NormalizeName(spell.Name);
            var duplicates = spells
                .Where(s => s.Id != spell.Id && NormalizeName(s.Name) == normalizedName)
                .ToList();

            if (duplicates.Count > 0)
            {
                var allItems = new List<Spell> { spell };
                allItems.AddRange(duplicates);

                var entries = allItems.Select(s => new DuplicateEntry<Spell>
                {
                    Item = s,
                    Source = s.Source,
                    SourcePriority = GetSourcePriority(s.Source),
                    LastUpdated = s.LastUpdated
                }).ToList();

                var preferred = entries.OrderByDescending(e => e.SourcePriority)
                    .ThenByDescending(e => e.LastUpdated)
                    .First().Item;

                groups.Add(new DuplicateGroup<Spell>
                {
                    Name = spell.Name,
                    Entries = entries,
                    PreferredEntry = preferred
                });

                foreach (var d in duplicates)
                {
                    processed.Add(d.Id);
                }
            }

            processed.Add(spell.Id);
        }

        return groups;
    }

    #endregion

    #region Equipment Duplicates

    public async Task<DuplicateCheckResult<Equipment>> CheckEquipmentDuplicateAsync(Equipment equipment)
    {
        var existing = await _databaseService.GetItemsAsync<Equipment>();

        // Check API ID first
        if (!string.IsNullOrEmpty(equipment.ApiId))
        {
            var apiMatch = existing.FirstOrDefault(e => e.ApiId == equipment.ApiId);
            if (apiMatch != null)
            {
                return new DuplicateCheckResult<Equipment>
                {
                    IsDuplicate = true,
                    ExistingItem = apiMatch,
                    MatchType = DuplicateMatchType.ApiIdMatch,
                    MatchConfidence = 1.0,
                    MatchReason = "Same API ID"
                };
            }
        }

        // Check exact name match
        var exactMatch = existing.FirstOrDefault(e =>
            string.Equals(e.Name, equipment.Name, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
        {
            return new DuplicateCheckResult<Equipment>
            {
                IsDuplicate = true,
                ExistingItem = exactMatch,
                MatchType = DuplicateMatchType.ExactName,
                MatchConfidence = 0.95,
                MatchReason = "Exact name match"
            };
        }

        // Check normalized name
        var normalizedName = NormalizeName(equipment.Name);
        var normalizedMatch = existing.FirstOrDefault(e =>
            NormalizeName(e.Name) == normalizedName);
        if (normalizedMatch != null)
        {
            return new DuplicateCheckResult<Equipment>
            {
                IsDuplicate = true,
                ExistingItem = normalizedMatch,
                MatchType = DuplicateMatchType.NormalizedName,
                MatchConfidence = 0.9,
                MatchReason = "Normalized name match"
            };
        }

        return new DuplicateCheckResult<Equipment> { IsDuplicate = false };
    }

    public async Task<List<DuplicateGroup<Equipment>>> FindAllEquipmentDuplicatesAsync()
    {
        var equipment = await _databaseService.GetItemsAsync<Equipment>();
        var groups = new List<DuplicateGroup<Equipment>>();
        var processed = new HashSet<int>();

        foreach (var item in equipment)
        {
            if (processed.Contains(item.Id)) continue;

            var normalizedName = NormalizeName(item.Name);
            var duplicates = equipment
                .Where(e => e.Id != item.Id && NormalizeName(e.Name) == normalizedName)
                .ToList();

            if (duplicates.Count > 0)
            {
                var allItems = new List<Equipment> { item };
                allItems.AddRange(duplicates);

                var entries = allItems.Select(e => new DuplicateEntry<Equipment>
                {
                    Item = e,
                    Source = e.Source,
                    SourcePriority = GetSourcePriority(e.Source),
                    LastUpdated = e.LastUpdated
                }).ToList();

                var preferred = entries.OrderByDescending(e => e.SourcePriority)
                    .ThenByDescending(e => e.LastUpdated)
                    .First().Item;

                groups.Add(new DuplicateGroup<Equipment>
                {
                    Name = item.Name,
                    Entries = entries,
                    PreferredEntry = preferred
                });

                foreach (var d in duplicates)
                {
                    processed.Add(d.Id);
                }
            }

            processed.Add(item.Id);
        }

        return groups;
    }

    #endregion

    #region Merge and Delete

    public async Task<MergeResult> MergeDuplicatesAsync<T>(List<T> duplicates, T preferred) where T : class, new()
    {
        var result = new MergeResult();

        try
        {
            // Delete all duplicates except the preferred one
            var idsToDelete = new List<int>();

            foreach (var item in duplicates)
            {
                if (item == preferred) continue;

                // Get ID via reflection (assumes property named "Id")
                var idProp = typeof(T).GetProperty("Id");
                if (idProp != null)
                {
                    var id = (int?)idProp.GetValue(item);
                    if (id.HasValue)
                    {
                        idsToDelete.Add(id.Value);
                    }
                }
            }

            foreach (var id in idsToDelete)
            {
                var itemToDelete = await _databaseService.GetItemAsync<T>(id);
                if (itemToDelete != null)
                {
                    await _databaseService.DeleteItemAsync(itemToDelete);
                    result.ItemsDeleted++;
                }
            }

            result.ItemsKept = 1;
            result.Success = true;
            result.Message = $"Merged duplicates: kept 1, deleted {result.ItemsDeleted}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Merge failed: {ex.Message}";
        }

        return result;
    }

    public async Task<int> DeleteDuplicatesAsync<T>(List<int> idsToDelete) where T : class, new()
    {
        int deleted = 0;

        foreach (var id in idsToDelete)
        {
            var item = await _databaseService.GetItemAsync<T>(id);
            if (item != null)
            {
                await _databaseService.DeleteItemAsync(item);
                deleted++;
            }
        }

        return deleted;
    }

    #endregion

    #region Helper Methods

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";

        // Remove special characters, convert to lowercase
        var normalized = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9\s]", "");
        // Remove extra whitespace
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }

    private static double FuzzyNameMatch(string name1, string name2)
    {
        if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2))
            return 0;

        var n1 = NormalizeName(name1);
        var n2 = NormalizeName(name2);

        if (n1 == n2) return 1.0;

        // Calculate Levenshtein distance
        var distance = LevenshteinDistance(n1, n2);
        var maxLength = Math.Max(n1.Length, n2.Length);

        return maxLength > 0 ? 1.0 - ((double)distance / maxLength) : 0;
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        var m = s1.Length;
        var n = s2.Length;
        var d = new int[m + 1, n + 1];

        for (var i = 0; i <= m; i++) d[i, 0] = i;
        for (var j = 0; j <= n; j++) d[0, j] = j;

        for (var j = 1; j <= n; j++)
        {
            for (var i = 1; i <= m; i++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[m, n];
    }

    private static int GetSourcePriority(string source)
    {
        if (string.IsNullOrEmpty(source)) return 0;

        foreach (var kvp in SourcePriority)
        {
            if (source.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return 10; // Default priority for unknown sources
    }

    #endregion
}
