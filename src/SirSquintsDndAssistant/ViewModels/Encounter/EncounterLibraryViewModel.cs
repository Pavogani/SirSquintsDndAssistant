using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Encounter;
using SirSquintsDndAssistant.Services.Database.Repositories;
using SirSquintsDndAssistant.Services.Combat;
using SirSquintsDndAssistant.Services.Utilities;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace SirSquintsDndAssistant.ViewModels.Encounter;

public partial class EncounterLibraryViewModel : BaseViewModel
{
    private readonly IEncounterRepository _encounterRepository;
    private readonly IMonsterRepository _monsterRepository;
    private readonly ICombatService _combatService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<EncounterTemplate> encounters = new();

    [ObservableProperty]
    private EncounterTemplate? selectedEncounter;

    [ObservableProperty]
    private ObservableCollection<EncounterMonster> selectedEncounterMonsters = new();

    [ObservableProperty]
    private bool hasEncounters;

    [ObservableProperty]
    private bool isDetailVisible;

    public EncounterLibraryViewModel(
        IEncounterRepository encounterRepository,
        IMonsterRepository monsterRepository,
        ICombatService combatService,
        IDialogService dialogService)
    {
        _encounterRepository = encounterRepository;
        _monsterRepository = monsterRepository;
        _combatService = combatService;
        _dialogService = dialogService;
        Title = "Encounter Library";
    }

    [RelayCommand]
    private async Task LoadEncountersAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var encounters = await _encounterRepository.GetAllAsync();
            Encounters.Clear();

            foreach (var encounter in encounters)
            {
                Encounters.Add(encounter);
            }

            HasEncounters = Encounters.Count > 0;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ViewEncounterDetails(EncounterTemplate encounter)
    {
        SelectedEncounter = encounter;
        SelectedEncounterMonsters.Clear();

        if (!string.IsNullOrEmpty(encounter.MonstersJson))
        {
            try
            {
                var monsters = JsonSerializer.Deserialize<List<EncounterMonster>>(encounter.MonstersJson);
                if (monsters != null)
                {
                    foreach (var monster in monsters)
                    {
                        SelectedEncounterMonsters.Add(monster);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing monsters JSON: {ex.Message}");
            }
        }

        IsDetailVisible = true;
    }

    [RelayCommand]
    private void HideDetails()
    {
        IsDetailVisible = false;
        SelectedEncounter = null;
        SelectedEncounterMonsters.Clear();
    }

    [RelayCommand]
    private async Task DeleteEncounterAsync(EncounterTemplate encounter)
    {
        var confirm = await _dialogService.DisplayConfirmAsync(
            "Delete Encounter",
            $"Are you sure you want to delete '{encounter.Name}'?",
            "Delete",
            "Cancel");

        if (confirm)
        {
            await _encounterRepository.DeleteAsync(encounter);
            Encounters.Remove(encounter);
            HasEncounters = Encounters.Count > 0;

            if (SelectedEncounter == encounter)
            {
                HideDetails();
            }
        }
    }

    [RelayCommand]
    private async Task StartCombatAsync(EncounterTemplate encounter)
    {
        if (string.IsNullOrEmpty(encounter.MonstersJson))
            return;

        try
        {
            var monsters = JsonSerializer.Deserialize<List<EncounterMonster>>(encounter.MonstersJson);
            if (monsters == null || monsters.Count == 0)
                return;

            // Start a new combat
            await _combatService.StartNewCombatAsync(encounter.Name);

            // Add all monsters to combat
            foreach (var encounterMonster in monsters)
            {
                for (int i = 0; i < encounterMonster.Quantity; i++)
                {
                    var name = encounterMonster.Quantity > 1
                        ? $"{encounterMonster.MonsterName} #{i + 1}"
                        : encounterMonster.MonsterName;

                    // Fetch full monster details
                    var monsterList = await _monsterRepository.SearchAsync(encounterMonster.MonsterName);
                    var monster = monsterList.FirstOrDefault();

                    if (monster != null)
                    {
                        var dexMod = (monster.Dexterity - 10) / 2;
                        var entry = await _combatService.AddCombatantAsync(
                            name,
                            "Monster",
                            monster.ArmorClass,
                            monster.HitPoints,
                            dexMod,
                            monster.Id);

                        await _combatService.RollInitiativeAsync(entry);
                    }
                }
            }

            // Navigate to initiative tracker
            await Shell.Current.GoToAsync("//initiative");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting combat: {ex.Message}");
            await _dialogService.DisplayAlertAsync(
                "Error",
                "Failed to start combat from this encounter.");
        }
    }

    public string GetDifficultyColor(string difficulty)
    {
        return difficulty switch
        {
            "Trivial" => "#90EE90",
            "Easy" => "#228B22",
            "Medium" => "#DAA520",
            "Hard" => "#FF8C00",
            "Deadly" => "#DC143C",
            _ => "Gray"
        };
    }
}
