using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Services.Utilities;

namespace SirSquintsDndAssistant.ViewModels.Utilities;

public partial class AdvancedGeneratorsViewModel : BaseViewModel
{
    private readonly IAdvancedGeneratorService _generatorService;

    [ObservableProperty] private string npcResult = string.Empty;
    [ObservableProperty] private string questResult = string.Empty;
    [ObservableProperty] private string locationResult = string.Empty;
    [ObservableProperty] private string rumorResult = string.Empty;
    [ObservableProperty] private string selectedLocationType = "City";

    public List<string> LocationTypes { get; } = new() { "City", "Town", "Forest", "Mountain", "Dungeon" };

    public AdvancedGeneratorsViewModel(IAdvancedGeneratorService generatorService)
    {
        _generatorService = generatorService;
        Title = "Advanced Generators";
    }

    [RelayCommand]
    private void GenerateNpc()
    {
        var npc = _generatorService.GenerateNpcPersonality();
        NpcResult = npc.ToString();
    }

    [RelayCommand]
    private void GenerateQuest()
    {
        var quest = _generatorService.GenerateQuestHook();
        QuestResult = quest.ToString();
    }

    [RelayCommand]
    private void GenerateLocation()
    {
        var location = _generatorService.GenerateLocationName(SelectedLocationType);
        var tavern = _generatorService.GenerateTavernName();
        var shop = _generatorService.GenerateShopName();

        LocationResult = $"Location: {location}\n" +
                        $"Tavern: {tavern}\n" +
                        $"Shop: {shop}";
    }

    [RelayCommand]
    private void GenerateRumor()
    {
        var rumor = _generatorService.GenerateRumor();
        var secret = _generatorService.GenerateSecret();

        RumorResult = $"Rumor: {rumor}\n\n" +
                     $"Secret: {secret}";
    }
}
