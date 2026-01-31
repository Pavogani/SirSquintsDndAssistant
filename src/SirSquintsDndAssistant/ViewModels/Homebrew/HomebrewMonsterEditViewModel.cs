using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Homebrew;
using SirSquintsDndAssistant.Services.Homebrew;
using SirSquintsDndAssistant.Services.Utilities;

namespace SirSquintsDndAssistant.ViewModels.Homebrew;

[QueryProperty(nameof(MonsterId), "id")]
public partial class HomebrewMonsterEditViewModel : ObservableObject
{
    private readonly IHomebrewService _homebrewService;
    private readonly IDialogService _dialogService;
    private int _monsterId;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isNewMonster;

    // Basic Info
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _size = "Medium";

    [ObservableProperty]
    private string _type = "Humanoid";

    [ObservableProperty]
    private string _alignment = "Unaligned";

    // Stats
    [ObservableProperty]
    private int _armorClass = 10;

    [ObservableProperty]
    private string _armorType = string.Empty;

    [ObservableProperty]
    private int _hitPoints;

    [ObservableProperty]
    private string _hitDice = string.Empty;

    // Speed
    [ObservableProperty]
    private int _walkSpeed = 30;

    [ObservableProperty]
    private int _flySpeed;

    [ObservableProperty]
    private int _swimSpeed;

    [ObservableProperty]
    private int _climbSpeed;

    [ObservableProperty]
    private int _burrowSpeed;

    // Ability Scores
    [ObservableProperty]
    private int _strength = 10;

    [ObservableProperty]
    private int _dexterity = 10;

    [ObservableProperty]
    private int _constitution = 10;

    [ObservableProperty]
    private int _intelligence = 10;

    [ObservableProperty]
    private int _wisdom = 10;

    [ObservableProperty]
    private int _charisma = 10;

    // Challenge
    [ObservableProperty]
    private double _challengeRating;

    [ObservableProperty]
    private int _experiencePoints;

    // Text Fields
    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _tags = string.Empty;

    // JSON Fields (simplified as text for editing)
    [ObservableProperty]
    private string _specialAbilities = string.Empty;

    [ObservableProperty]
    private string _actions = string.Empty;

    [ObservableProperty]
    private string _reactions = string.Empty;

    [ObservableProperty]
    private string _legendaryActions = string.Empty;

    public List<string> Sizes { get; } = new() { "Tiny", "Small", "Medium", "Large", "Huge", "Gargantuan" };
    public List<string> Types { get; } = new() { "Aberration", "Beast", "Celestial", "Construct", "Dragon", "Elemental", "Fey", "Fiend", "Giant", "Humanoid", "Monstrosity", "Ooze", "Plant", "Undead" };
    public List<string> Alignments { get; } = new() { "Lawful Good", "Neutral Good", "Chaotic Good", "Lawful Neutral", "True Neutral", "Chaotic Neutral", "Lawful Evil", "Neutral Evil", "Chaotic Evil", "Unaligned" };

    public string MonsterId
    {
        set
        {
            if (int.TryParse(value, out var id))
            {
                _monsterId = id;
                IsNewMonster = false;
                LoadMonsterAsync().ConfigureAwait(false);
            }
            else
            {
                IsNewMonster = true;
            }
        }
    }

    public HomebrewMonsterEditViewModel(IHomebrewService homebrewService, IDialogService dialogService)
    {
        _homebrewService = homebrewService;
        _dialogService = dialogService;
    }

    private async Task LoadMonsterAsync()
    {
        if (_monsterId <= 0) return;

        IsLoading = true;
        try
        {
            var monster = await _homebrewService.GetMonsterAsync(_monsterId);
            if (monster != null)
            {
                // Load all values from the existing monster
                Name = monster.Name;
                Size = monster.Size;
                Type = monster.Type;
                Alignment = monster.Alignment;
                ArmorClass = monster.ArmorClass;
                ArmorType = monster.ArmorType;
                HitPoints = monster.HitPoints;
                HitDice = monster.HitDice;
                WalkSpeed = monster.WalkSpeed;
                FlySpeed = monster.FlySpeed;
                SwimSpeed = monster.SwimSpeed;
                ClimbSpeed = monster.ClimbSpeed;
                BurrowSpeed = monster.BurrowSpeed;
                Strength = monster.Strength;
                Dexterity = monster.Dexterity;
                Constitution = monster.Constitution;
                Intelligence = monster.Intelligence;
                Wisdom = monster.Wisdom;
                Charisma = monster.Charisma;
                ChallengeRating = monster.ChallengeRating;
                ExperiencePoints = monster.ExperiencePoints;
                Description = monster.Description;
                Notes = monster.Notes;
                Tags = monster.Tags;
                SpecialAbilities = monster.SpecialAbilitiesJson;
                Actions = monster.ActionsJson;
                Reactions = monster.ReactionsJson;
                LegendaryActions = monster.LegendaryActionsJson;
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
            await _dialogService.DisplayAlertAsync("Validation Error", "Monster name is required.");
            return;
        }

        IsLoading = true;
        try
        {
            var monster = new HomebrewMonster
            {
                Id = IsNewMonster ? 0 : _monsterId,
                Name = Name,
                Size = Size,
                Type = Type,
                Alignment = Alignment,
                ArmorClass = ArmorClass,
                ArmorType = ArmorType,
                HitPoints = HitPoints,
                HitDice = HitDice,
                WalkSpeed = WalkSpeed,
                FlySpeed = FlySpeed,
                SwimSpeed = SwimSpeed,
                ClimbSpeed = ClimbSpeed,
                BurrowSpeed = BurrowSpeed,
                Strength = Strength,
                Dexterity = Dexterity,
                Constitution = Constitution,
                Intelligence = Intelligence,
                Wisdom = Wisdom,
                Charisma = Charisma,
                ChallengeRating = ChallengeRating,
                ExperiencePoints = ExperiencePoints,
                Description = Description,
                Notes = Notes,
                Tags = Tags,
                SpecialAbilitiesJson = SpecialAbilities,
                ActionsJson = Actions,
                ReactionsJson = Reactions,
                LegendaryActionsJson = LegendaryActions,
                UpdatedAt = DateTime.Now
            };

            if (IsNewMonster)
            {
                monster.CreatedAt = DateTime.Now;
            }

            await _homebrewService.SaveMonsterAsync(monster);
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
