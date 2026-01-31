using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Extensions;
using SirSquintsDndAssistant.Models.Encounter;
using SirSquintsDndAssistant.Models.Creatures;
using SirSquintsDndAssistant.Services.Database.Repositories;
using SirSquintsDndAssistant.Services.Encounter;
using SirSquintsDndAssistant.Services.Combat;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace SirSquintsDndAssistant.ViewModels.Encounter;

public partial class EncounterBuilderViewModel : BaseViewModel
{
    private readonly IMonsterRepository _monsterRepository;
    private readonly IEncounterRepository _encounterRepository;
    private readonly IDifficultyCalculator _difficultyCalculator;
    private readonly ICombatService _combatService;
    private readonly DebounceHelper _searchDebounce = new();

    [ObservableProperty]
    private ObservableCollection<EncounterMonster> encounterMonsters = new();

    [ObservableProperty]
    private ObservableCollection<Monster> availableMonsters = new();

    [ObservableProperty]
    private Monster? selectedMonster;

    [ObservableProperty]
    private string encounterName = "New Encounter";

    [ObservableProperty]
    private int partyLevel = 1;

    [ObservableProperty]
    private int partySize = 4;

    [ObservableProperty]
    private int totalXp;

    [ObservableProperty]
    private int adjustedXp;

    [ObservableProperty]
    private string difficulty = "Unknown";

    [ObservableProperty]
    private string difficultyColor = "Gray";

    [ObservableProperty]
    private string searchText = string.Empty;

    public EncounterBuilderViewModel(
        IMonsterRepository monsterRepository,
        IEncounterRepository encounterRepository,
        IDifficultyCalculator difficultyCalculator,
        ICombatService combatService)
    {
        _monsterRepository = monsterRepository;
        _encounterRepository = encounterRepository;
        _difficultyCalculator = difficultyCalculator;
        _combatService = combatService;
        Title = "Encounter Builder";
    }

    [RelayCommand]
    private async Task LoadMonstersAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var monsters = await _monsterRepository.GetAllAsync();
            AvailableMonsters.Clear();

            foreach (var monster in monsters.Take(50)) // Show first 50 for performance
            {
                AvailableMonsters.Add(monster);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SearchMonstersAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadMonstersAsync();
            return;
        }

        try
        {
            IsBusy = true;
            var monsters = await _monsterRepository.SearchAsync(SearchText);
            AvailableMonsters.Clear();

            foreach (var monster in monsters.Take(50))
            {
                AvailableMonsters.Add(monster);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddMonsterToEncounter(Monster? monster)
    {
        if (monster == null) return;

        var existing = EncounterMonsters.FirstOrDefault(em => em.MonsterId == monster.Id);
        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            EncounterMonsters.Add(new EncounterMonster
            {
                MonsterId = monster.Id,
                MonsterName = monster.Name,
                ChallengeRating = monster.ChallengeRating,
                ExperiencePoints = monster.ExperiencePoints,
                Quantity = 1
            });
        }

        CalculateDifficulty();
    }

    [RelayCommand]
    private void RemoveMonsterFromEncounter(EncounterMonster encounterMonster)
    {
        EncounterMonsters.Remove(encounterMonster);
        CalculateDifficulty();
    }

    [RelayCommand]
    private void IncreaseQuantity(EncounterMonster encounterMonster)
    {
        encounterMonster.Quantity++;
        CalculateDifficulty();
    }

    [RelayCommand]
    private void DecreaseQuantity(EncounterMonster encounterMonster)
    {
        if (encounterMonster.Quantity > 1)
        {
            encounterMonster.Quantity--;
            CalculateDifficulty();
        }
    }

    [RelayCommand]
    private async Task SaveEncounterAsync()
    {
        if (EncounterMonsters.Count == 0)
            return;

        var template = new EncounterTemplate
        {
            Name = EncounterName,
            PartyLevel = PartyLevel,
            PartySize = PartySize,
            Difficulty = Difficulty,
            MonstersJson = JsonSerializer.Serialize(EncounterMonsters),
            Created = DateTime.Now
        };

        await _encounterRepository.SaveAsync(template);

        // Clear the builder
        EncounterMonsters.Clear();
        EncounterName = "New Encounter";
        CalculateDifficulty();
    }

    [RelayCommand]
    private async Task StartCombatFromEncounterAsync()
    {
        if (EncounterMonsters.Count == 0)
            return;

        // Start a new combat
        await _combatService.StartNewCombatAsync(EncounterName);

        // Add all monsters to combat
        foreach (var encounterMonster in EncounterMonsters)
        {
            for (int i = 0; i < encounterMonster.Quantity; i++)
            {
                var name = encounterMonster.Quantity > 1
                    ? $"{encounterMonster.MonsterName} #{i + 1}"
                    : encounterMonster.MonsterName;

                // Fetch full monster details
                var monsters = await _monsterRepository.SearchAsync(encounterMonster.MonsterName);
                var monster = monsters.FirstOrDefault();

                if (monster != null)
                {
                    var dexMod = (monster.Dexterity - 10) / 2;
                    await _combatService.AddCombatantAsync(
                        name,
                        "Monster",
                        monster.ArmorClass,
                        monster.HitPoints,
                        dexMod,
                        monster.Id);
                }
            }
        }

        // Navigate to initiative tracker
        await Shell.Current.GoToAsync("//initiative");
    }

    private void CalculateDifficulty()
    {
        // Calculate base XP
        TotalXp = EncounterMonsters.Sum(em => em.TotalXp);

        // Calculate adjusted XP based on number of monsters
        var totalMonsterCount = EncounterMonsters.Sum(em => em.Quantity);
        AdjustedXp = _difficultyCalculator.CalculateAdjustedXp(TotalXp, totalMonsterCount);

        // Determine difficulty
        Difficulty = _difficultyCalculator.CalculateDifficulty(AdjustedXp, PartyLevel, PartySize);

        // Set difficulty color
        DifficultyColor = Difficulty switch
        {
            "Trivial" => "#90EE90",
            "Easy" => "#228B22",
            "Medium" => "#DAA520",
            "Hard" => "#FF8C00",
            "Deadly" => "#DC143C",
            _ => "Gray"
        };
    }

    partial void OnPartyLevelChanged(int value)
    {
        CalculateDifficulty();
    }

    partial void OnPartySizeChanged(int value)
    {
        CalculateDifficulty();
    }

    partial void OnSearchTextChanged(string value)
    {
        // Debounce search to avoid excessive queries while typing
        _searchDebounce.DebounceAsync(SearchMonstersAsync);
    }
}
