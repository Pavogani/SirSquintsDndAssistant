using SirSquintsDndAssistant.Services.Database.Repositories;

namespace SirSquintsDndAssistant.Services.Validation;

public interface IDataValidationService
{
    Task<ValidationReport> ValidateAllDataAsync();
    Task<int> RemoveDuplicateMonstersAsync();
    Task<int> RemoveDuplicateSpellsAsync();
    Task<int> RemoveDuplicateEquipmentAsync();
    Task<int> RemoveDuplicateMagicItemsAsync();
    Task<int> RemoveAllDuplicatesAsync();
}

public class ValidationReport
{
    public int TotalMonsters { get; set; }
    public int DuplicateMonsters { get; set; }
    public int TotalSpells { get; set; }
    public int DuplicateSpells { get; set; }
    public int TotalEquipment { get; set; }
    public int DuplicateEquipment { get; set; }
    public int TotalMagicItems { get; set; }
    public int DuplicateMagicItems { get; set; }
    public int TotalConditions { get; set; }
    public List<string> MonstersWithMissingData { get; set; } = new();
    public List<string> SpellsWithMissingData { get; set; } = new();
    public bool IsValid => DuplicateMonsters == 0 && DuplicateSpells == 0 &&
                           DuplicateEquipment == 0 && DuplicateMagicItems == 0 &&
                           MonstersWithMissingData.Count == 0 && SpellsWithMissingData.Count == 0;

    public override string ToString()
    {
        return $"Validation Report:\n" +
               $"Monsters: {TotalMonsters} ({DuplicateMonsters} duplicates)\n" +
               $"Spells: {TotalSpells} ({DuplicateSpells} duplicates)\n" +
               $"Equipment: {TotalEquipment} ({DuplicateEquipment} duplicates)\n" +
               $"Magic Items: {TotalMagicItems} ({DuplicateMagicItems} duplicates)\n" +
               $"Conditions: {TotalConditions}\n" +
               $"Monsters with missing data: {MonstersWithMissingData.Count}\n" +
               $"Spells with missing data: {SpellsWithMissingData.Count}";
    }
}

public class DataValidationService : IDataValidationService
{
    private readonly IMonsterRepository _monsterRepo;
    private readonly ISpellRepository _spellRepo;
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IMagicItemRepository _magicItemRepo;
    private readonly IConditionRepository _conditionRepo;

    public DataValidationService(
        IMonsterRepository monsterRepo,
        ISpellRepository spellRepo,
        IEquipmentRepository equipmentRepo,
        IMagicItemRepository magicItemRepo,
        IConditionRepository conditionRepo)
    {
        _monsterRepo = monsterRepo;
        _spellRepo = spellRepo;
        _equipmentRepo = equipmentRepo;
        _magicItemRepo = magicItemRepo;
        _conditionRepo = conditionRepo;
    }

    public async Task<ValidationReport> ValidateAllDataAsync()
    {
        var report = new ValidationReport();

        // Validate monsters
        var monsters = await _monsterRepo.GetAllAsync();
        report.TotalMonsters = monsters.Count;
        report.DuplicateMonsters = monsters
            .GroupBy(m => m.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Sum(g => g.Count() - 1);

        report.MonstersWithMissingData = monsters
            .Where(m => string.IsNullOrEmpty(m.Name) || m.HitPoints <= 0 || m.ArmorClass <= 0)
            .Select(m => m.Name ?? "Unknown")
            .ToList();

        // Validate spells
        var spells = await _spellRepo.GetAllAsync();
        report.TotalSpells = spells.Count;
        report.DuplicateSpells = spells
            .GroupBy(s => s.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Sum(g => g.Count() - 1);

        report.SpellsWithMissingData = spells
            .Where(s => string.IsNullOrEmpty(s.Name) || string.IsNullOrEmpty(s.Description))
            .Select(s => s.Name ?? "Unknown")
            .ToList();

        // Validate equipment
        var equipment = await _equipmentRepo.GetAllAsync();
        report.TotalEquipment = equipment.Count;
        report.DuplicateEquipment = equipment
            .GroupBy(e => e.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Sum(g => g.Count() - 1);

        // Validate magic items
        var magicItems = await _magicItemRepo.GetAllAsync();
        report.TotalMagicItems = magicItems.Count;
        report.DuplicateMagicItems = magicItems
            .GroupBy(m => m.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Sum(g => g.Count() - 1);

        // Validate conditions
        var conditions = await _conditionRepo.GetAllAsync();
        report.TotalConditions = conditions.Count;

        return report;
    }

    public async Task<int> RemoveDuplicateMonstersAsync()
    {
        var monsters = await _monsterRepo.GetAllAsync();
        var duplicates = monsters
            .GroupBy(m => m.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1)) // Keep the first, remove the rest
            .ToList();

        int removed = 0;
        foreach (var duplicate in duplicates)
        {
            await _monsterRepo.DeleteAsync(duplicate);
            removed++;
        }

        return removed;
    }

    public async Task<int> RemoveDuplicateSpellsAsync()
    {
        var spells = await _spellRepo.GetAllAsync();
        var duplicates = spells
            .GroupBy(s => s.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1))
            .ToList();

        int removed = 0;
        foreach (var duplicate in duplicates)
        {
            await _spellRepo.DeleteAsync(duplicate);
            removed++;
        }

        return removed;
    }

    public async Task<int> RemoveDuplicateEquipmentAsync()
    {
        var equipment = await _equipmentRepo.GetAllAsync();
        var duplicates = equipment
            .GroupBy(e => e.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1))
            .ToList();

        int removed = 0;
        foreach (var duplicate in duplicates)
        {
            await _equipmentRepo.DeleteAsync(duplicate);
            removed++;
        }

        return removed;
    }

    public async Task<int> RemoveDuplicateMagicItemsAsync()
    {
        var magicItems = await _magicItemRepo.GetAllAsync();
        var duplicates = magicItems
            .GroupBy(m => m.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1))
            .ToList();

        int removed = 0;
        foreach (var duplicate in duplicates)
        {
            await _magicItemRepo.DeleteAsync(duplicate);
            removed++;
        }

        return removed;
    }

    public async Task<int> RemoveAllDuplicatesAsync()
    {
        int totalRemoved = 0;
        totalRemoved += await RemoveDuplicateMonstersAsync();
        totalRemoved += await RemoveDuplicateSpellsAsync();
        totalRemoved += await RemoveDuplicateEquipmentAsync();
        totalRemoved += await RemoveDuplicateMagicItemsAsync();
        return totalRemoved;
    }
}
