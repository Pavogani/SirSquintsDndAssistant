using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Creatures;
using SirSquintsDndAssistant.Services.Database.Repositories;
using System.Collections.ObjectModel;

namespace SirSquintsDndAssistant.ViewModels.Campaign;

public partial class PlayerCharactersViewModel : BaseViewModel
{
    private readonly IPlayerCharacterRepository _playerCharacterRepository;
    private readonly ICampaignRepository _campaignRepository;

    [ObservableProperty]
    private ObservableCollection<PlayerCharacter> characters = new();

    [ObservableProperty]
    private int? activeCampaignId;

    public PlayerCharactersViewModel(IPlayerCharacterRepository playerCharacterRepository, ICampaignRepository campaignRepository)
    {
        _playerCharacterRepository = playerCharacterRepository;
        _campaignRepository = campaignRepository;
        Title = "Party";
    }

    [RelayCommand]
    private async Task LoadCharactersAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var activeCampaign = await _campaignRepository.GetActiveCampaignAsync();
            ActiveCampaignId = activeCampaign?.Id;

            List<PlayerCharacter> chars;
            if (ActiveCampaignId.HasValue)
            {
                chars = await _playerCharacterRepository.GetByCampaignAsync(ActiveCampaignId.Value);
            }
            else
            {
                chars = await _playerCharacterRepository.GetAllAsync();
            }

            Characters.Clear();
            foreach (var character in chars)
            {
                Characters.Add(character);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteCharacterAsync(PlayerCharacter character)
    {
        await _playerCharacterRepository.DeleteAsync(character);
        await LoadCharactersAsync();
    }
}
