using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Services.Utilities;

namespace SirSquintsDndAssistant.ViewModels;

public partial class RandomTablesViewModel : ObservableObject
{
    private readonly IRandomTableService _randomTableService;

    [ObservableProperty]
    private ObservableCollection<string> _results = new();

    [ObservableProperty]
    private string _selectedCategory = "NPC";

    [ObservableProperty]
    private FullNpc? _generatedNpc;

    [ObservableProperty]
    private TavernDetails? _generatedTavern;

    [ObservableProperty]
    private DungeonRoom? _generatedDungeonRoom;

    [ObservableProperty]
    private bool _showNpcResult;

    [ObservableProperty]
    private bool _showTavernResult;

    [ObservableProperty]
    private bool _showDungeonResult;

    [ObservableProperty]
    private bool _showSimpleResult = true;

    public List<string> Categories { get; } = new()
    {
        "NPC",
        "Tavern",
        "Dungeon Room",
        "Trinket",
        "Plot Hook",
        "Treasure",
        "Combat",
        "Wilderness"
    };

    public RandomTablesViewModel(IRandomTableService randomTableService)
    {
        _randomTableService = randomTableService;
    }

    [RelayCommand]
    private void GenerateRandom()
    {
        HideAllResults();

        switch (SelectedCategory)
        {
            case "NPC":
                GeneratedNpc = _randomTableService.GenerateFullNpc();
                ShowNpcResult = true;
                break;

            case "Tavern":
                GeneratedTavern = _randomTableService.GenerateFullTavern();
                ShowTavernResult = true;
                break;

            case "Dungeon Room":
                GeneratedDungeonRoom = _randomTableService.GenerateDungeonRoom();
                ShowDungeonResult = true;
                break;

            case "Trinket":
                ShowSimpleResult = true;
                Results.Clear();
                Results.Add(_randomTableService.GetRandomTrinket());
                break;

            case "Plot Hook":
                ShowSimpleResult = true;
                Results.Clear();
                Results.Add(_randomTableService.GetRandomPlotHook());
                break;

            case "Treasure":
                ShowSimpleResult = true;
                Results.Clear();
                Results.Add($"Gemstone: {_randomTableService.GetRandomGemstone(Random.Shared.Next(1, 5))}");
                Results.Add($"Art Object: {_randomTableService.GetRandomArtObject(Random.Shared.Next(1, 5))}");
                break;

            case "Combat":
                ShowSimpleResult = true;
                Results.Clear();
                Results.Add($"Battlefield: {_randomTableService.GetRandomBattlefieldCondition()}");
                Results.Add($"Complication: {_randomTableService.GetRandomCombatComplication()}");
                Results.Add($"Critical Hit Effect: {_randomTableService.GetRandomCriticalHitEffect()}");
                Results.Add($"Critical Miss Effect: {_randomTableService.GetRandomCriticalMissEffect()}");
                break;

            case "Wilderness":
                ShowSimpleResult = true;
                Results.Clear();
                Results.Add($"Feature: {_randomTableService.GetRandomWildernessFeature()}");
                Results.Add($"Urban Encounter: {_randomTableService.GetRandomUrbanEncounter()}");
                break;
        }
    }

    [RelayCommand]
    private void RollNpcAppearance()
    {
        ShowSimpleResult = true;
        HideComplexResults();
        Results.Clear();
        Results.Add(_randomTableService.GetRandomNpcAppearance());
    }

    [RelayCommand]
    private void RollNpcPersonality()
    {
        ShowSimpleResult = true;
        HideComplexResults();
        Results.Clear();
        Results.Add(_randomTableService.GetRandomNpcPersonality());
    }

    [RelayCommand]
    private void RollNpcSecret()
    {
        ShowSimpleResult = true;
        HideComplexResults();
        Results.Clear();
        Results.Add(_randomTableService.GetRandomNpcSecret());
    }

    [RelayCommand]
    private void RollTavernName()
    {
        ShowSimpleResult = true;
        HideComplexResults();
        Results.Clear();
        Results.Add(_randomTableService.GetRandomTavernName());
    }

    [RelayCommand]
    private void RollTavernRumor()
    {
        ShowSimpleResult = true;
        HideComplexResults();
        Results.Clear();
        Results.Add(_randomTableService.GetRandomTavernRumor());
    }

    [RelayCommand]
    private void RollDungeonFeature()
    {
        ShowSimpleResult = true;
        HideComplexResults();
        Results.Clear();
        Results.Add(_randomTableService.GetRandomDungeonFeature());
    }

    [RelayCommand]
    private void RollPlotTwist()
    {
        ShowSimpleResult = true;
        HideComplexResults();
        Results.Clear();
        Results.Add(_randomTableService.GetRandomTwist());
    }

    [RelayCommand]
    private void RollVillainMotivation()
    {
        ShowSimpleResult = true;
        HideComplexResults();
        Results.Clear();
        Results.Add(_randomTableService.GetRandomVillainMotivation());
    }

    [RelayCommand]
    private void ClearResults()
    {
        HideAllResults();
        Results.Clear();
    }

    private void HideAllResults()
    {
        ShowNpcResult = false;
        ShowTavernResult = false;
        ShowDungeonResult = false;
        ShowSimpleResult = false;
    }

    private void HideComplexResults()
    {
        ShowNpcResult = false;
        ShowTavernResult = false;
        ShowDungeonResult = false;
    }
}
