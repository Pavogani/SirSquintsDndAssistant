using System.Text.Json;
using System.Text.RegularExpressions;
using SirSquintsDndAssistant.Models.Api;
using SirSquintsDndAssistant.Models.Creatures;
using SirSquintsDndAssistant.Models.Import;
using SirSquintsDndAssistant.Models.Content;
using SirSquintsDndAssistant.Services.Database;

namespace SirSquintsDndAssistant.Services.Import;

/// <summary>
/// Service for importing D&D data from various JSON formats.
/// </summary>
public class JsonDataImportService : IJsonDataImportService
{
    private readonly IDatabaseService _databaseService;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonDataImportService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    #region Format Detection

    public async Task<JsonDataFormat> DetectFormatAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        return DetectFormat(json);
    }

    public JsonDataFormat DetectFormat(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // 5e.tools format - has "monster", "spell", or "item" arrays
            if (root.TryGetProperty("monster", out _) ||
                root.TryGetProperty("spell", out _) ||
                root.TryGetProperty("item", out _) ||
                root.TryGetProperty("baseitem", out _))
            {
                return JsonDataFormat.FiveETools;
            }

            // Kobold Fight Club - has "monsters" array with specific structure
            if (root.TryGetProperty("monsters", out var monsters) &&
                monsters.ValueKind == JsonValueKind.Array &&
                monsters.GetArrayLength() > 0)
            {
                var first = monsters[0];
                if (first.TryGetProperty("ac", out _) && first.TryGetProperty("hp", out _))
                {
                    return JsonDataFormat.KoboldFightClub;
                }
            }

            // CritterDB - has "creatures" array
            if (root.TryGetProperty("creatures", out _))
            {
                return JsonDataFormat.CritterDb;
            }

            // Open5e - has "results" array (API response format)
            if (root.TryGetProperty("results", out _))
            {
                return JsonDataFormat.Open5e;
            }

            // Array of monsters with standard D&D fields
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var first = root[0];
                if (first.TryGetProperty("name", out _) &&
                    (first.TryGetProperty("armor_class", out _) || first.TryGetProperty("ac", out _)))
                {
                    return JsonDataFormat.Custom;
                }
            }

            return JsonDataFormat.Unknown;
        }
        catch
        {
            return JsonDataFormat.Unknown;
        }
    }

    public async Task<(bool IsValid, List<string> Errors)> ValidateFileAsync(string filePath)
    {
        var errors = new List<string>();

        if (!File.Exists(filePath))
        {
            errors.Add("File does not exist");
            return (false, errors);
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            using var doc = JsonDocument.Parse(json);

            var format = DetectFormat(json);
            if (format == JsonDataFormat.Unknown)
            {
                errors.Add("Unable to detect JSON format. Supported formats: 5e.tools, Kobold Fight Club, CritterDB");
            }

            return (errors.Count == 0, errors);
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON: {ex.Message}");
            return (false, errors);
        }
        catch (Exception ex)
        {
            errors.Add($"Error reading file: {ex.Message}");
            return (false, errors);
        }
    }

    #endregion

    #region Monster Import

    public async Task<ImportResult> ImportMonstersAsync(string filePath, ImportOptions? options = null)
    {
        var json = await File.ReadAllTextAsync(filePath);
        options ??= new ImportOptions { SourceName = Path.GetFileNameWithoutExtension(filePath) };
        return await ImportMonstersFromJsonAsync(json, options);
    }

    public async Task<ImportResult> ImportMonstersFromJsonAsync(string json, ImportOptions? options = null)
    {
        options ??= new ImportOptions();
        var result = new ImportResult
        {
            StartTime = DateTime.Now,
            SourceType = "Monsters"
        };

        try
        {
            var format = DetectFormat(json);
            result.SourceName = $"{options.SourceName} ({format})";

            List<Monster> monsters = format switch
            {
                JsonDataFormat.FiveETools => ParseFiveEToolsMonsters(json),
                JsonDataFormat.KoboldFightClub => ParseKfcMonsters(json),
                JsonDataFormat.CritterDb => ParseCritterDbMonsters(json),
                JsonDataFormat.Custom => ParseCustomMonsters(json),
                _ => throw new NotSupportedException($"Format not supported for monster import: {format}")
            };

            result.TotalItems = monsters.Count;

            // Get existing monsters for duplicate detection
            var existingMonsters = await _databaseService.GetItemsAsync<Monster>();
            var existingNames = existingMonsters.Select(m => NormalizeName(m.Name)).ToHashSet();

            foreach (var monster in monsters)
            {
                try
                {
                    // Apply filter
                    if (options.Filter != null && !MatchesFilter(monster, options.Filter))
                    {
                        result.ItemsSkipped++;
                        continue;
                    }

                    var normalizedName = NormalizeName(monster.Name);

                    // Check for duplicates
                    if (existingNames.Contains(normalizedName))
                    {
                        result.DuplicatesFound++;

                        if (options.SkipDuplicates && !options.UpdateExisting)
                        {
                            result.ItemsSkipped++;
                            result.ImportedItems.Add(new ImportedItem
                            {
                                Name = monster.Name,
                                Type = "Monster",
                                WasDuplicate = true
                            });
                            continue;
                        }

                        if (options.UpdateExisting)
                        {
                            var existing = existingMonsters.First(m => NormalizeName(m.Name) == normalizedName);
                            monster.Id = existing.Id;
                        }
                    }

                    monster.Source = options.SourceName;
                    monster.LastUpdated = DateTime.Now;

                    await _databaseService.SaveItemAsync(monster);
                    existingNames.Add(normalizedName);

                    result.ItemsImported++;
                    result.ImportedItems.Add(new ImportedItem
                    {
                        Name = monster.Name,
                        Type = "Monster",
                        DatabaseId = monster.Id,
                        WasDuplicate = result.DuplicatesFound > 0 && options.UpdateExisting
                    });
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Error importing {monster.Name}: {ex.Message}");
                }
            }

            result.Success = result.ErrorCount == 0;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        result.EndTime = DateTime.Now;
        return result;
    }

    private List<Monster> ParseFiveEToolsMonsters(string json)
    {
        var bestiary = JsonSerializer.Deserialize<FiveEToolsBestiary>(json, _jsonOptions);
        if (bestiary?.Monster == null) return new List<Monster>();

        return bestiary.Monster.Select(m => new Monster
        {
            ApiId = $"5etools-{m.Name.ToLowerInvariant().Replace(' ', '-')}",
            Name = m.Name,
            Size = m.Size?.FirstOrDefault() ?? "Medium",
            Type = ParseType(m.Type),
            Alignment = ParseAlignment(m.Alignment),
            ArmorClass = ParseAc(m.Ac),
            HitPoints = m.Hp?.Average ?? 0,
            HitDice = m.Hp?.Formula ?? "",
            ChallengeRating = ParseCr(m.Cr),
            ExperiencePoints = CalculateXp(ParseCr(m.Cr)),
            Strength = m.Str,
            Dexterity = m.Dex,
            Constitution = m.Con,
            Intelligence = m.Int,
            Wisdom = m.Wis,
            Charisma = m.Cha,
            SpeedsJson = JsonSerializer.Serialize(ParseSpeed(m.Speed)),
            SkillsJson = m.Skill != null ? JsonSerializer.Serialize(m.Skill) : "{}",
            SavingThrowsJson = m.Save != null ? JsonSerializer.Serialize(m.Save) : "{}",
            SpecialAbilitiesJson = m.Trait != null ? JsonSerializer.Serialize(m.Trait.Select(t => new { t.Name, Desc = ParseEntries(t.Entries) })) : "[]",
            ActionsJson = m.Action != null ? JsonSerializer.Serialize(m.Action.Select(a => new { a.Name, Desc = ParseEntries(a.Entries) })) : "[]",
            Source = $"5e.tools ({m.Source})"
        }).ToList();
    }

    private List<Monster> ParseKfcMonsters(string json)
    {
        var export = JsonSerializer.Deserialize<KoboldFightClubExport>(json, _jsonOptions);
        if (export?.Monsters == null) return new List<Monster>();

        return export.Monsters.Select(m => new Monster
        {
            ApiId = $"kfc-{m.Name.ToLowerInvariant().Replace(' ', '-')}",
            Name = m.Name,
            Size = m.Size,
            Type = m.Type,
            Alignment = m.Alignment,
            ArmorClass = ParseAcString(m.Ac),
            HitPoints = ParseHpString(m.Hp),
            HitDice = ExtractHitDice(m.Hp),
            ChallengeRating = ParseCrString(m.Cr),
            ExperiencePoints = CalculateXp(ParseCrString(m.Cr)),
            Strength = m.Str,
            Dexterity = m.Dex,
            Constitution = m.Con,
            Intelligence = m.Int,
            Wisdom = m.Wis,
            Charisma = m.Cha,
            SpeedsJson = JsonSerializer.Serialize(ParseSpeedString(m.Speed)),
            SpecialAbilitiesJson = ParseTraitsString(m.Traits),
            ActionsJson = ParseActionsString(m.Actions),
            Source = "Kobold Fight Club"
        }).ToList();
    }

    private List<Monster> ParseCritterDbMonsters(string json)
    {
        var export = JsonSerializer.Deserialize<CritterDbExport>(json, _jsonOptions);
        if (export?.Creatures == null) return new List<Monster>();

        return export.Creatures.Select(c => new Monster
        {
            ApiId = $"critterdb-{c.Name.ToLowerInvariant().Replace(' ', '-')}",
            Name = c.Name,
            Size = c.Stats?.Size ?? "Medium",
            Type = c.Stats?.Race ?? "Unknown",
            Alignment = c.Stats?.Alignment ?? "",
            ArmorClass = c.Stats?.ArmorClass ?? 10,
            HitPoints = c.Stats?.HitPoints ?? 0,
            HitDice = c.Stats?.HitPointsStr ?? "",
            ChallengeRating = c.Stats?.ChallengeRating ?? 0,
            ExperiencePoints = CalculateXp(c.Stats?.ChallengeRating ?? 0),
            Strength = c.Stats?.AbilityScores?.Strength ?? 10,
            Dexterity = c.Stats?.AbilityScores?.Dexterity ?? 10,
            Constitution = c.Stats?.AbilityScores?.Constitution ?? 10,
            Intelligence = c.Stats?.AbilityScores?.Intelligence ?? 10,
            Wisdom = c.Stats?.AbilityScores?.Wisdom ?? 10,
            Charisma = c.Stats?.AbilityScores?.Charisma ?? 10,
            SpeedsJson = JsonSerializer.Serialize(new { walk = c.Stats?.Speed ?? 30 }),
            SpecialAbilitiesJson = c.Stats?.Abilities != null ? JsonSerializer.Serialize(c.Stats.Abilities.Select(a => new { a.Name, Desc = a.Description })) : "[]",
            ActionsJson = c.Stats?.Actions != null ? JsonSerializer.Serialize(c.Stats.Actions.Select(a => new { a.Name, Desc = a.Description })) : "[]",
            Source = "CritterDB"
        }).ToList();
    }

    private List<Monster> ParseCustomMonsters(string json)
    {
        // Try parsing as an array of monster objects
        try
        {
            var monsters = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json, _jsonOptions);
            if (monsters == null) return new List<Monster>();

            return monsters.Select(m => new Monster
            {
                ApiId = $"custom-{GetStringValue(m, "name", "unknown").ToLowerInvariant().Replace(' ', '-')}",
                Name = GetStringValue(m, "name", "Unknown"),
                Size = GetStringValue(m, "size", "Medium"),
                Type = GetStringValue(m, "type", "Unknown"),
                Alignment = GetStringValue(m, "alignment", ""),
                ArmorClass = GetIntValue(m, "armor_class", GetIntValue(m, "ac", 10)),
                HitPoints = GetIntValue(m, "hit_points", GetIntValue(m, "hp", 0)),
                HitDice = GetStringValue(m, "hit_dice", ""),
                ChallengeRating = GetDoubleValue(m, "challenge_rating", GetDoubleValue(m, "cr", 0)),
                ExperiencePoints = GetIntValue(m, "xp", CalculateXp(GetDoubleValue(m, "challenge_rating", 0))),
                Strength = GetIntValue(m, "strength", GetIntValue(m, "str", 10)),
                Dexterity = GetIntValue(m, "dexterity", GetIntValue(m, "dex", 10)),
                Constitution = GetIntValue(m, "constitution", GetIntValue(m, "con", 10)),
                Intelligence = GetIntValue(m, "intelligence", GetIntValue(m, "int", 10)),
                Wisdom = GetIntValue(m, "wisdom", GetIntValue(m, "wis", 10)),
                Charisma = GetIntValue(m, "charisma", GetIntValue(m, "cha", 10)),
                Source = "Custom Import"
            }).ToList();
        }
        catch
        {
            return new List<Monster>();
        }
    }

    #endregion

    #region Spell Import

    public async Task<ImportResult> ImportSpellsAsync(string filePath, ImportOptions? options = null)
    {
        var json = await File.ReadAllTextAsync(filePath);
        options ??= new ImportOptions { SourceName = Path.GetFileNameWithoutExtension(filePath) };
        return await ImportSpellsFromJsonAsync(json, options);
    }

    public async Task<ImportResult> ImportSpellsFromJsonAsync(string json, ImportOptions? options = null)
    {
        options ??= new ImportOptions();
        var result = new ImportResult
        {
            StartTime = DateTime.Now,
            SourceType = "Spells"
        };

        try
        {
            var format = DetectFormat(json);
            result.SourceName = $"{options.SourceName} ({format})";

            if (format != JsonDataFormat.FiveETools)
            {
                result.Errors.Add("Only 5e.tools format is supported for spell imports");
                result.Success = false;
                result.EndTime = DateTime.Now;
                return result;
            }

            var spellbook = JsonSerializer.Deserialize<FiveEToolsSpellbook>(json, _jsonOptions);
            if (spellbook?.Spell == null)
            {
                result.Errors.Add("No spells found in file");
                result.Success = false;
                result.EndTime = DateTime.Now;
                return result;
            }

            result.TotalItems = spellbook.Spell.Count;

            var existingSpells = await _databaseService.GetItemsAsync<Spell>();
            var existingNames = existingSpells.Select(s => NormalizeName(s.Name)).ToHashSet();

            foreach (var fiveESpell in spellbook.Spell)
            {
                try
                {
                    var spell = new Spell
                    {
                        ApiId = $"5etools-{fiveESpell.Name.ToLowerInvariant().Replace(' ', '-')}",
                        Name = fiveESpell.Name,
                        Level = fiveESpell.Level,
                        School = MapSchool(fiveESpell.School),
                        CastingTime = FormatTime(fiveESpell.Time),
                        Range = FormatRange(fiveESpell.Range),
                        Components = FormatComponents(fiveESpell.Components),
                        Duration = FormatDuration(fiveESpell.Duration),
                        Description = ParseEntries(fiveESpell.Entries),
                        HigherLevels = fiveESpell.EntriesHigherLevel != null ? ParseEntries(fiveESpell.EntriesHigherLevel.SelectMany(e => e.Entries ?? new List<object>()).ToList()) : "",
                        ClassesJson = JsonSerializer.Serialize(fiveESpell.Classes?.FromClassList?.Select(c => c.Name).ToList() ?? new List<string>()),
                        Source = $"5e.tools ({fiveESpell.Source})",
                        LastUpdated = DateTime.Now
                    };

                    var normalizedName = NormalizeName(spell.Name);

                    if (existingNames.Contains(normalizedName))
                    {
                        result.DuplicatesFound++;
                        if (options.SkipDuplicates && !options.UpdateExisting)
                        {
                            result.ItemsSkipped++;
                            continue;
                        }

                        if (options.UpdateExisting)
                        {
                            var existing = existingSpells.First(s => NormalizeName(s.Name) == normalizedName);
                            spell.Id = existing.Id;
                        }
                    }

                    await _databaseService.SaveItemAsync(spell);
                    existingNames.Add(normalizedName);
                    result.ItemsImported++;

                    result.ImportedItems.Add(new ImportedItem
                    {
                        Name = spell.Name,
                        Type = "Spell",
                        DatabaseId = spell.Id
                    });
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Error importing {fiveESpell.Name}: {ex.Message}");
                }
            }

            result.Success = result.ErrorCount == 0;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        result.EndTime = DateTime.Now;
        return result;
    }

    #endregion

    #region Item Import

    public async Task<ImportResult> ImportItemsAsync(string filePath, ImportOptions? options = null)
    {
        var json = await File.ReadAllTextAsync(filePath);
        options ??= new ImportOptions { SourceName = Path.GetFileNameWithoutExtension(filePath) };
        return await ImportItemsFromJsonAsync(json, options);
    }

    public async Task<ImportResult> ImportItemsFromJsonAsync(string json, ImportOptions? options = null)
    {
        options ??= new ImportOptions();
        var result = new ImportResult
        {
            StartTime = DateTime.Now,
            SourceType = "Items"
        };

        try
        {
            var format = DetectFormat(json);
            result.SourceName = $"{options.SourceName} ({format})";

            if (format != JsonDataFormat.FiveETools)
            {
                result.Errors.Add("Only 5e.tools format is supported for item imports");
                result.Success = false;
                result.EndTime = DateTime.Now;
                return result;
            }

            var itemsData = JsonSerializer.Deserialize<FiveEToolsItems>(json, _jsonOptions);
            var allItems = new List<FiveEToolsItem>();

            if (itemsData?.Item != null) allItems.AddRange(itemsData.Item);
            if (itemsData?.BaseItem != null) allItems.AddRange(itemsData.BaseItem);

            if (allItems.Count == 0)
            {
                result.Errors.Add("No items found in file");
                result.Success = false;
                result.EndTime = DateTime.Now;
                return result;
            }

            result.TotalItems = allItems.Count;

            var existingItems = await _databaseService.GetItemsAsync<Equipment>();
            var existingNames = existingItems.Select(e => NormalizeName(e.Name)).ToHashSet();

            foreach (var fiveEItem in allItems)
            {
                try
                {
                    var item = new Equipment
                    {
                        ApiId = $"5etools-{fiveEItem.Name.ToLowerInvariant().Replace(' ', '-')}",
                        Name = fiveEItem.Name,
                        EquipmentCategory = MapItemCategory(fiveEItem.Type),
                        WeaponCategory = fiveEItem.WeaponCategory ?? "",
                        ArmorCategory = fiveEItem.Armor == true ? MapItemCategory(fiveEItem.Type) : "",
                        Weight = fiveEItem.Weight ?? 0,
                        Cost = fiveEItem.Value ?? 0,
                        CostCurrency = "cp",
                        DescriptionJson = JsonSerializer.Serialize(ParseEntries(fiveEItem.Entries)),
                        PropertiesJson = fiveEItem.Property != null ? JsonSerializer.Serialize(fiveEItem.Property) : "[]",
                        Source = $"5e.tools ({fiveEItem.Source})",
                        LastUpdated = DateTime.Now
                    };

                    var normalizedName = NormalizeName(item.Name);

                    if (existingNames.Contains(normalizedName))
                    {
                        result.DuplicatesFound++;
                        if (options.SkipDuplicates && !options.UpdateExisting)
                        {
                            result.ItemsSkipped++;
                            continue;
                        }

                        if (options.UpdateExisting)
                        {
                            var existing = existingItems.First(e => NormalizeName(e.Name) == normalizedName);
                            item.Id = existing.Id;
                        }
                    }

                    await _databaseService.SaveItemAsync(item);
                    existingNames.Add(normalizedName);
                    result.ItemsImported++;

                    result.ImportedItems.Add(new ImportedItem
                    {
                        Name = item.Name,
                        Type = "Equipment",
                        DatabaseId = item.Id
                    });
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Error importing {fiveEItem.Name}: {ex.Message}");
                }
            }

            result.Success = result.ErrorCount == 0;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        result.EndTime = DateTime.Now;
        return result;
    }

    #endregion

    #region Helper Methods

    private static string NormalizeName(string name)
    {
        return name.ToLowerInvariant().Trim();
    }

    private bool MatchesFilter(Monster monster, ImportFilter filter)
    {
        if (filter.MinChallengeRating.HasValue && monster.ChallengeRating < filter.MinChallengeRating.Value)
            return false;

        if (filter.MaxChallengeRating.HasValue && monster.ChallengeRating > filter.MaxChallengeRating.Value)
            return false;

        if (filter.Types?.Count > 0 && !filter.Types.Contains(monster.Type, StringComparer.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(filter.NameContains) &&
            !monster.Name.Contains(filter.NameContains, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static string ParseType(object? type)
    {
        if (type == null) return "Unknown";
        if (type is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return element.GetString() ?? "Unknown";
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("type", out var typeVal))
                return typeVal.GetString() ?? "Unknown";
        }
        return type.ToString() ?? "Unknown";
    }

    private static string ParseAlignment(List<object>? alignment)
    {
        if (alignment == null || alignment.Count == 0) return "";
        var alignments = new Dictionary<string, string>
        {
            {"L", "Lawful"}, {"N", "Neutral"}, {"C", "Chaotic"},
            {"G", "Good"}, {"E", "Evil"}
        };

        var parts = new List<string>();
        foreach (var a in alignment)
        {
            var str = a.ToString() ?? "";
            if (alignments.TryGetValue(str, out var full))
                parts.Add(full);
            else if (str == "A")
                return "Any Alignment";
            else if (str == "U")
                return "Unaligned";
        }

        return string.Join(" ", parts);
    }

    private static int ParseAc(List<object>? ac)
    {
        if (ac == null || ac.Count == 0) return 10;
        var first = ac[0];
        if (first is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number)
                return element.GetInt32();
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("ac", out var acVal))
                return acVal.GetInt32();
        }
        return 10;
    }

    private static double ParseCr(object? cr)
    {
        if (cr == null) return 0;
        if (cr is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return ParseCrString(element.GetString());
            if (element.ValueKind == JsonValueKind.Number)
                return element.GetDouble();
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("cr", out var crVal))
                return ParseCrString(crVal.GetString());
        }
        return 0;
    }

    private static double ParseCrString(string? cr)
    {
        if (string.IsNullOrEmpty(cr)) return 0;
        if (cr == "1/8") return 0.125;
        if (cr == "1/4") return 0.25;
        if (cr == "1/2") return 0.5;
        return double.TryParse(cr, out var d) ? d : 0;
    }

    private static int CalculateXp(double cr)
    {
        var xpByCr = new Dictionary<double, int>
        {
            {0, 10}, {0.125, 25}, {0.25, 50}, {0.5, 100},
            {1, 200}, {2, 450}, {3, 700}, {4, 1100}, {5, 1800},
            {6, 2300}, {7, 2900}, {8, 3900}, {9, 5000}, {10, 5900},
            {11, 7200}, {12, 8400}, {13, 10000}, {14, 11500}, {15, 13000},
            {16, 15000}, {17, 18000}, {18, 20000}, {19, 22000}, {20, 25000},
            {21, 33000}, {22, 41000}, {23, 50000}, {24, 62000}, {25, 75000},
            {26, 90000}, {27, 105000}, {28, 120000}, {29, 135000}, {30, 155000}
        };
        return xpByCr.GetValueOrDefault(cr, 0);
    }

    private static Dictionary<string, int> ParseSpeed(FiveEToolsSpeed? speed)
    {
        var result = new Dictionary<string, int>();
        if (speed == null) return result;

        void AddSpeed(string name, object? value)
        {
            if (value == null) return;
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Number)
                    result[name] = element.GetInt32();
                else if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("number", out var num))
                    result[name] = num.GetInt32();
            }
        }

        AddSpeed("walk", speed.Walk);
        AddSpeed("fly", speed.Fly);
        AddSpeed("swim", speed.Swim);
        AddSpeed("climb", speed.Climb);
        AddSpeed("burrow", speed.Burrow);

        return result;
    }

    private static string ParseEntries(List<object>? entries)
    {
        if (entries == null || entries.Count == 0) return "";

        var parts = new List<string>();
        foreach (var entry in entries)
        {
            if (entry is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.String)
                    parts.Add(element.GetString() ?? "");
                else if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("entries", out var subEntries))
                    parts.Add(ParseEntries(subEntries.Deserialize<List<object>>()));
            }
            else
            {
                parts.Add(entry.ToString() ?? "");
            }
        }

        return string.Join("\n\n", parts);
    }

    private static int ParseAcString(string ac)
    {
        var match = Regex.Match(ac, @"(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 10;
    }

    private static int ParseHpString(string hp)
    {
        var match = Regex.Match(hp, @"(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private static string ExtractHitDice(string hp)
    {
        var match = Regex.Match(hp, @"\(([^)]+)\)");
        return match.Success ? match.Groups[1].Value : "";
    }

    private static Dictionary<string, int> ParseSpeedString(string speed)
    {
        var result = new Dictionary<string, int>();
        var match = Regex.Match(speed, @"(\d+)\s*ft");
        if (match.Success)
            result["walk"] = int.Parse(match.Groups[1].Value);
        return result;
    }

    private static string ParseTraitsString(string? traits)
    {
        if (string.IsNullOrEmpty(traits)) return "[]";
        // Split by newlines and parse as traits
        var items = traits.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(t =>
            {
                var colonIndex = t.IndexOf(':');
                if (colonIndex > 0)
                    return new { Name = t[..colonIndex].Trim(), Desc = t[(colonIndex + 1)..].Trim() };
                return new { Name = "Trait", Desc = t.Trim() };
            });
        return JsonSerializer.Serialize(items);
    }

    private static string ParseActionsString(string? actions)
    {
        if (string.IsNullOrEmpty(actions)) return "[]";
        var items = actions.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(a =>
            {
                var colonIndex = a.IndexOf(':');
                if (colonIndex > 0)
                    return new { Name = a[..colonIndex].Trim(), Desc = a[(colonIndex + 1)..].Trim() };
                return new { Name = "Action", Desc = a.Trim() };
            });
        return JsonSerializer.Serialize(items);
    }

    private static string GetStringValue(Dictionary<string, JsonElement> dict, string key, string defaultValue)
    {
        if (dict.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String)
            return element.GetString() ?? defaultValue;
        return defaultValue;
    }

    private static int GetIntValue(Dictionary<string, JsonElement> dict, string key, int defaultValue)
    {
        if (dict.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.Number)
            return element.GetInt32();
        return defaultValue;
    }

    private static double GetDoubleValue(Dictionary<string, JsonElement> dict, string key, double defaultValue)
    {
        if (dict.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.Number)
            return element.GetDouble();
        return defaultValue;
    }

    private static string MapSchool(string school)
    {
        return school.ToUpperInvariant() switch
        {
            "A" => "Abjuration",
            "C" => "Conjuration",
            "D" => "Divination",
            "E" => "Enchantment",
            "V" => "Evocation",
            "I" => "Illusion",
            "N" => "Necromancy",
            "T" => "Transmutation",
            _ => school
        };
    }

    private static string FormatTime(List<FiveEToolsTime>? times)
    {
        if (times == null || times.Count == 0) return "1 action";
        var t = times[0];
        return $"{t.Number} {t.Unit}{(t.Number > 1 ? "s" : "")}";
    }

    private static string FormatRange(FiveEToolsRange? range)
    {
        if (range == null) return "Self";
        if (range.Type == "point" && range.Distance != null)
        {
            if (range.Distance.Type == "self") return "Self";
            if (range.Distance.Type == "touch") return "Touch";
            return $"{range.Distance.Amount} {range.Distance.Type}";
        }
        return range.Type;
    }

    private static string FormatComponents(FiveEToolsComponents? components)
    {
        if (components == null) return "";
        var parts = new List<string>();
        if (components.V) parts.Add("V");
        if (components.S) parts.Add("S");
        if (components.M != null)
        {
            if (components.M is JsonElement element && element.ValueKind == JsonValueKind.String)
                parts.Add($"M ({element.GetString()})");
            else
                parts.Add("M");
        }
        return string.Join(", ", parts);
    }

    private static string FormatDuration(List<FiveEToolsDuration>? durations)
    {
        if (durations == null || durations.Count == 0) return "Instantaneous";
        var d = durations[0];

        var prefix = d.Concentration == true ? "Concentration, up to " : "";

        if (d.Type == "instant") return "Instantaneous";
        if (d.Type == "permanent") return "Until dispelled";
        if (d.Duration != null && d.Duration.Amount.HasValue)
            return $"{prefix}{d.Duration.Amount} {d.Duration.Type}{(d.Duration.Amount > 1 ? "s" : "")}";

        return d.Type;
    }

    private static string MapItemCategory(string? type)
    {
        return type?.ToUpperInvariant() switch
        {
            "S" => "Shield",
            "M" => "Melee Weapon",
            "R" => "Ranged Weapon",
            "A" => "Ammunition",
            "LA" => "Light Armor",
            "MA" => "Medium Armor",
            "HA" => "Heavy Armor",
            "W" => "Wondrous Item",
            "P" => "Potion",
            "SC" => "Scroll",
            "WD" => "Wand",
            "RD" => "Rod",
            "ST" => "Staff",
            "RG" => "Ring",
            "G" => "Adventuring Gear",
            "INS" => "Instrument",
            "AT" => "Artisan's Tools",
            "GS" => "Gaming Set",
            "T" => "Tools",
            _ => "Miscellaneous"
        };
    }

    private static string FormatCost(int? copperValue)
    {
        if (!copperValue.HasValue || copperValue.Value == 0) return "—";

        var cp = copperValue.Value;
        if (cp >= 100)
        {
            var gp = cp / 100;
            var remainder = cp % 100;
            if (remainder == 0) return $"{gp} gp";
            return $"{gp} gp, {remainder} cp";
        }
        if (cp >= 10)
        {
            var sp = cp / 10;
            var remainder = cp % 10;
            if (remainder == 0) return $"{sp} sp";
            return $"{sp} sp, {remainder} cp";
        }
        return $"{cp} cp";
    }

    #endregion
}
