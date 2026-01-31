using SirSquintsDndAssistant.Models.Homebrew;
using SirSquintsDndAssistant.Services.Database;

namespace SirSquintsDndAssistant.Services.Homebrew;

public interface IHomebrewService
{
    // Monsters
    Task<List<HomebrewMonster>> GetAllMonstersAsync();
    Task<HomebrewMonster?> GetMonsterAsync(int id);
    Task<List<HomebrewMonster>> SearchMonstersAsync(string searchText);
    Task<HomebrewMonster> SaveMonsterAsync(HomebrewMonster monster);
    Task DeleteMonsterAsync(int id);
    Task<HomebrewMonster> DuplicateMonsterAsync(int id);

    // Spells
    Task<List<HomebrewSpell>> GetAllSpellsAsync();
    Task<HomebrewSpell?> GetSpellAsync(int id);
    Task<List<HomebrewSpell>> SearchSpellsAsync(string searchText);
    Task<HomebrewSpell> SaveSpellAsync(HomebrewSpell spell);
    Task DeleteSpellAsync(int id);
    Task<HomebrewSpell> DuplicateSpellAsync(int id);

    // Items
    Task<List<HomebrewItem>> GetAllItemsAsync();
    Task<HomebrewItem?> GetItemAsync(int id);
    Task<List<HomebrewItem>> SearchItemsAsync(string searchText);
    Task<HomebrewItem> SaveItemAsync(HomebrewItem item);
    Task DeleteItemAsync(int id);
    Task<HomebrewItem> DuplicateItemAsync(int id);

    // Export/Import
    Task<string> ExportAllToJsonAsync();
    Task ImportFromJsonAsync(string json);

    // Statistics
    Task<HomebrewStats> GetStatsAsync();
}

public record HomebrewStats(int MonsterCount, int SpellCount, int ItemCount);

public class HomebrewService : IHomebrewService
{
    private readonly IDatabaseService _databaseService;

    public HomebrewService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    #region Monsters

    public async Task<List<HomebrewMonster>> GetAllMonstersAsync()
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<HomebrewMonster>()
                .OrderBy(m => m.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting homebrew monsters: {ex.Message}");
            return new List<HomebrewMonster>();
        }
    }

    public async Task<HomebrewMonster?> GetMonsterAsync(int id)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.GetAsync<HomebrewMonster>(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting homebrew monster: {ex.Message}");
            return null;
        }
    }

    public async Task<List<HomebrewMonster>> SearchMonstersAsync(string searchText)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var search = searchText.ToLower();
            return await db.Table<HomebrewMonster>()
                .Where(m => m.Name.ToLower().Contains(search) ||
                           m.Type.ToLower().Contains(search) ||
                           m.Tags.ToLower().Contains(search))
                .OrderBy(m => m.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error searching homebrew monsters: {ex.Message}");
            return new List<HomebrewMonster>();
        }
    }

    public async Task<HomebrewMonster> SaveMonsterAsync(HomebrewMonster monster)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            monster.UpdatedAt = DateTime.Now;

            if (monster.Id == 0)
            {
                monster.CreatedAt = DateTime.Now;
                await db.InsertAsync(monster);
            }
            else
            {
                await db.UpdateAsync(monster);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving homebrew monster: {ex.Message}");
        }

        return monster;
    }

    public async Task DeleteMonsterAsync(int id)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.DeleteAsync<HomebrewMonster>(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting homebrew monster: {ex.Message}");
        }
    }

    public async Task<HomebrewMonster> DuplicateMonsterAsync(int id)
    {
        var original = await GetMonsterAsync(id);
        if (original == null)
            throw new InvalidOperationException("Monster not found");

        var duplicate = new HomebrewMonster
        {
            Name = $"{original.Name} (Copy)",
            Size = original.Size,
            Type = original.Type,
            Alignment = original.Alignment,
            ArmorClass = original.ArmorClass,
            ArmorType = original.ArmorType,
            HitPoints = original.HitPoints,
            HitDice = original.HitDice,
            WalkSpeed = original.WalkSpeed,
            FlySpeed = original.FlySpeed,
            SwimSpeed = original.SwimSpeed,
            ClimbSpeed = original.ClimbSpeed,
            BurrowSpeed = original.BurrowSpeed,
            Strength = original.Strength,
            Dexterity = original.Dexterity,
            Constitution = original.Constitution,
            Intelligence = original.Intelligence,
            Wisdom = original.Wisdom,
            Charisma = original.Charisma,
            SavingThrowsJson = original.SavingThrowsJson,
            SkillsJson = original.SkillsJson,
            DamageResistancesJson = original.DamageResistancesJson,
            DamageImmunitiesJson = original.DamageImmunitiesJson,
            DamageVulnerabilitiesJson = original.DamageVulnerabilitiesJson,
            ConditionImmunitiesJson = original.ConditionImmunitiesJson,
            SensesJson = original.SensesJson,
            LanguagesJson = original.LanguagesJson,
            ChallengeRating = original.ChallengeRating,
            ExperiencePoints = original.ExperiencePoints,
            SpecialAbilitiesJson = original.SpecialAbilitiesJson,
            ActionsJson = original.ActionsJson,
            BonusActionsJson = original.BonusActionsJson,
            ReactionsJson = original.ReactionsJson,
            LegendaryActionsJson = original.LegendaryActionsJson,
            Description = original.Description,
            Notes = original.Notes,
            Tags = original.Tags
        };

        return await SaveMonsterAsync(duplicate);
    }

    #endregion

    #region Spells

    public async Task<List<HomebrewSpell>> GetAllSpellsAsync()
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<HomebrewSpell>()
                .OrderBy(s => s.Level)
                .ThenBy(s => s.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting homebrew spells: {ex.Message}");
            return new List<HomebrewSpell>();
        }
    }

    public async Task<HomebrewSpell?> GetSpellAsync(int id)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.GetAsync<HomebrewSpell>(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting homebrew spell: {ex.Message}");
            return null;
        }
    }

    public async Task<List<HomebrewSpell>> SearchSpellsAsync(string searchText)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var search = searchText.ToLower();
            return await db.Table<HomebrewSpell>()
                .Where(s => s.Name.ToLower().Contains(search) ||
                           s.School.ToLower().Contains(search) ||
                           s.Tags.ToLower().Contains(search))
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error searching homebrew spells: {ex.Message}");
            return new List<HomebrewSpell>();
        }
    }

    public async Task<HomebrewSpell> SaveSpellAsync(HomebrewSpell spell)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            spell.UpdatedAt = DateTime.Now;

            if (spell.Id == 0)
            {
                spell.CreatedAt = DateTime.Now;
                await db.InsertAsync(spell);
            }
            else
            {
                await db.UpdateAsync(spell);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving homebrew spell: {ex.Message}");
        }

        return spell;
    }

    public async Task DeleteSpellAsync(int id)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.DeleteAsync<HomebrewSpell>(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting homebrew spell: {ex.Message}");
        }
    }

    public async Task<HomebrewSpell> DuplicateSpellAsync(int id)
    {
        var original = await GetSpellAsync(id);
        if (original == null)
            throw new InvalidOperationException("Spell not found");

        var duplicate = new HomebrewSpell
        {
            Name = $"{original.Name} (Copy)",
            Level = original.Level,
            School = original.School,
            IsRitual = original.IsRitual,
            CastingTime = original.CastingTime,
            Range = original.Range,
            RequiresVerbal = original.RequiresVerbal,
            RequiresSomatic = original.RequiresSomatic,
            RequiresMaterial = original.RequiresMaterial,
            MaterialComponents = original.MaterialComponents,
            Duration = original.Duration,
            RequiresConcentration = original.RequiresConcentration,
            Description = original.Description,
            HigherLevels = original.HigherLevels,
            ClassesJson = original.ClassesJson,
            Notes = original.Notes,
            Tags = original.Tags
        };

        return await SaveSpellAsync(duplicate);
    }

    #endregion

    #region Items

    public async Task<List<HomebrewItem>> GetAllItemsAsync()
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<HomebrewItem>()
                .OrderBy(i => i.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting homebrew items: {ex.Message}");
            return new List<HomebrewItem>();
        }
    }

    public async Task<HomebrewItem?> GetItemAsync(int id)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.GetAsync<HomebrewItem>(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting homebrew item: {ex.Message}");
            return null;
        }
    }

    public async Task<List<HomebrewItem>> SearchItemsAsync(string searchText)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var search = searchText.ToLower();
            return await db.Table<HomebrewItem>()
                .Where(i => i.Name.ToLower().Contains(search) ||
                           i.Category.ToLower().Contains(search) ||
                           i.Tags.ToLower().Contains(search))
                .OrderBy(i => i.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error searching homebrew items: {ex.Message}");
            return new List<HomebrewItem>();
        }
    }

    public async Task<HomebrewItem> SaveItemAsync(HomebrewItem item)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            item.UpdatedAt = DateTime.Now;

            if (item.Id == 0)
            {
                item.CreatedAt = DateTime.Now;
                await db.InsertAsync(item);
            }
            else
            {
                await db.UpdateAsync(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving homebrew item: {ex.Message}");
        }

        return item;
    }

    public async Task DeleteItemAsync(int id)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.DeleteAsync<HomebrewItem>(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting homebrew item: {ex.Message}");
        }
    }

    public async Task<HomebrewItem> DuplicateItemAsync(int id)
    {
        var original = await GetItemAsync(id);
        if (original == null)
            throw new InvalidOperationException("Item not found");

        var duplicate = new HomebrewItem
        {
            Name = $"{original.Name} (Copy)",
            ItemType = original.ItemType,
            Category = original.Category,
            IsMagic = original.IsMagic,
            Rarity = original.Rarity,
            RequiresAttunement = original.RequiresAttunement,
            AttunementRequirement = original.AttunementRequirement,
            Description = original.Description,
            Weight = original.Weight,
            Cost = original.Cost,
            IsWeapon = original.IsWeapon,
            WeaponType = original.WeaponType,
            DamageDice = original.DamageDice,
            DamageType = original.DamageType,
            WeaponPropertiesJson = original.WeaponPropertiesJson,
            IsArmor = original.IsArmor,
            ArmorType = original.ArmorType,
            BaseAC = original.BaseAC,
            MagicPropertiesJson = original.MagicPropertiesJson,
            Notes = original.Notes,
            Tags = original.Tags
        };

        return await SaveItemAsync(duplicate);
    }

    #endregion

    #region Export/Import

    public async Task<string> ExportAllToJsonAsync()
    {
        var monsters = await GetAllMonstersAsync();
        var spells = await GetAllSpellsAsync();
        var items = await GetAllItemsAsync();

        var exportData = new
        {
            ExportedAt = DateTime.Now,
            Version = "1.0",
            Monsters = monsters,
            Spells = spells,
            Items = items
        };

        return System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public async Task ImportFromJsonAsync(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Monsters", out var monstersElement))
            {
                var monsters = System.Text.Json.JsonSerializer.Deserialize<List<HomebrewMonster>>(monstersElement.GetRawText());
                if (monsters != null)
                {
                    foreach (var monster in monsters)
                    {
                        monster.Id = 0; // Reset ID to create new entries
                        await SaveMonsterAsync(monster);
                    }
                }
            }

            if (root.TryGetProperty("Spells", out var spellsElement))
            {
                var spells = System.Text.Json.JsonSerializer.Deserialize<List<HomebrewSpell>>(spellsElement.GetRawText());
                if (spells != null)
                {
                    foreach (var spell in spells)
                    {
                        spell.Id = 0;
                        await SaveSpellAsync(spell);
                    }
                }
            }

            if (root.TryGetProperty("Items", out var itemsElement))
            {
                var items = System.Text.Json.JsonSerializer.Deserialize<List<HomebrewItem>>(itemsElement.GetRawText());
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        item.Id = 0;
                        await SaveItemAsync(item);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error importing homebrew data: {ex.Message}");
            throw;
        }
    }

    #endregion

    public async Task<HomebrewStats> GetStatsAsync()
    {
        var monsters = await GetAllMonstersAsync();
        var spells = await GetAllSpellsAsync();
        var items = await GetAllItemsAsync();

        return new HomebrewStats(monsters.Count, spells.Count, items.Count);
    }
}
