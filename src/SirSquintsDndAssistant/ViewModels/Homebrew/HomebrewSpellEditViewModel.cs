using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Homebrew;
using SirSquintsDndAssistant.Services.Homebrew;
using SirSquintsDndAssistant.Services.Utilities;

namespace SirSquintsDndAssistant.ViewModels.Homebrew;

[QueryProperty(nameof(SpellId), "id")]
public partial class HomebrewSpellEditViewModel : ObservableObject
{
    private readonly IHomebrewService _homebrewService;
    private readonly IDialogService _dialogService;
    private int _spellId;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isNewSpell;

    // Basic Info
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _level;

    [ObservableProperty]
    private string _school = "Evocation";

    [ObservableProperty]
    private bool _isRitual;

    // Casting
    [ObservableProperty]
    private string _castingTime = "1 action";

    [ObservableProperty]
    private string _range = "60 feet";

    [ObservableProperty]
    private bool _requiresVerbal = true;

    [ObservableProperty]
    private bool _requiresSomatic = true;

    [ObservableProperty]
    private bool _requiresMaterial;

    [ObservableProperty]
    private string _materialComponents = string.Empty;

    // Duration
    [ObservableProperty]
    private string _duration = "Instantaneous";

    [ObservableProperty]
    private bool _requiresConcentration;

    // Effect
    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _higherLevels = string.Empty;

    // Damage
    [ObservableProperty]
    private bool _dealsDamage;

    [ObservableProperty]
    private string _damageType = string.Empty;

    [ObservableProperty]
    private string _damageDice = string.Empty;

    // Save
    [ObservableProperty]
    private bool _requiresSave;

    [ObservableProperty]
    private string _saveType = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _tags = string.Empty;

    public List<int> Levels { get; } = new() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    public List<string> Schools { get; } = new() { "Abjuration", "Conjuration", "Divination", "Enchantment", "Evocation", "Illusion", "Necromancy", "Transmutation" };
    public List<string> SaveTypes { get; } = new() { "", "STR", "DEX", "CON", "INT", "WIS", "CHA" };
    public List<string> DamageTypes { get; } = new() { "", "Acid", "Bludgeoning", "Cold", "Fire", "Force", "Lightning", "Necrotic", "Piercing", "Poison", "Psychic", "Radiant", "Slashing", "Thunder" };

    public string SpellId
    {
        set
        {
            if (int.TryParse(value, out var id))
            {
                _spellId = id;
                IsNewSpell = false;
                LoadSpellAsync().ConfigureAwait(false);
            }
            else
            {
                IsNewSpell = true;
            }
        }
    }

    public HomebrewSpellEditViewModel(IHomebrewService homebrewService, IDialogService dialogService)
    {
        _homebrewService = homebrewService;
        _dialogService = dialogService;
    }

    private async Task LoadSpellAsync()
    {
        if (_spellId <= 0) return;

        IsLoading = true;
        try
        {
            var spell = await _homebrewService.GetSpellAsync(_spellId);
            if (spell != null)
            {
                Name = spell.Name;
                Level = spell.Level;
                School = spell.School;
                IsRitual = spell.IsRitual;
                CastingTime = spell.CastingTime;
                Range = spell.Range;
                RequiresVerbal = spell.RequiresVerbal;
                RequiresSomatic = spell.RequiresSomatic;
                RequiresMaterial = spell.RequiresMaterial;
                MaterialComponents = spell.MaterialComponents;
                Duration = spell.Duration;
                RequiresConcentration = spell.RequiresConcentration;
                Description = spell.Description;
                HigherLevels = spell.HigherLevels;
                DealsDamage = spell.DealsDamage;
                DamageType = spell.DamageType;
                DamageDice = spell.DamageDice;
                RequiresSave = spell.RequiresSave;
                SaveType = spell.SaveType;
                Notes = spell.Notes;
                Tags = spell.Tags;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await _dialogService.DisplayAlertAsync("Validation Error", "Spell name is required.");
            return;
        }

        IsLoading = true;
        try
        {
            var spell = new HomebrewSpell
            {
                Id = IsNewSpell ? 0 : _spellId,
                Name = Name,
                Level = Level,
                School = School,
                IsRitual = IsRitual,
                CastingTime = CastingTime,
                Range = Range,
                RequiresVerbal = RequiresVerbal,
                RequiresSomatic = RequiresSomatic,
                RequiresMaterial = RequiresMaterial,
                MaterialComponents = MaterialComponents,
                Duration = Duration,
                RequiresConcentration = RequiresConcentration,
                Description = Description,
                HigherLevels = HigherLevels,
                DealsDamage = DealsDamage,
                DamageType = DamageType,
                DamageDice = DamageDice,
                RequiresSave = RequiresSave,
                SaveType = SaveType,
                Notes = Notes,
                Tags = Tags,
                UpdatedAt = DateTime.Now
            };

            if (IsNewSpell)
            {
                spell.CreatedAt = DateTime.Now;
            }

            await _homebrewService.SaveSpellAsync(spell);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await _dialogService.DisplayAlertAsync("Save Failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
