using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Campaign;
using SirSquintsDndAssistant.Services.Database.Repositories;
using System.Collections.ObjectModel;

namespace SirSquintsDndAssistant.ViewModels.Campaign;

public partial class CampaignListViewModel : BaseViewModel
{
    private readonly ICampaignRepository _campaignRepository;

    [ObservableProperty]
    private ObservableCollection<Models.Campaign.Campaign> campaigns = new();

    [ObservableProperty]
    private Models.Campaign.Campaign? selectedCampaign;

    [ObservableProperty]
    private string newCampaignName = string.Empty;

    [ObservableProperty]
    private string newCampaignDescription = string.Empty;

    public CampaignListViewModel(ICampaignRepository campaignRepository)
    {
        _campaignRepository = campaignRepository;
        Title = "Campaigns";
    }

    [RelayCommand]
    private async Task LoadCampaignsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var campaigns = await _campaignRepository.GetAllAsync();
            Campaigns.Clear();

            foreach (var campaign in campaigns)
            {
                Campaigns.Add(campaign);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateCampaignAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCampaignName))
            return;

        var campaign = new Models.Campaign.Campaign
        {
            Name = NewCampaignName,
            Description = NewCampaignDescription,
            StartDate = DateTime.Now,
            IsActive = true,
            Created = DateTime.Now,
            Modified = DateTime.Now
        };

        await _campaignRepository.SaveAsync(campaign);
        await LoadCampaignsAsync();

        NewCampaignName = string.Empty;
        NewCampaignDescription = string.Empty;
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(Models.Campaign.Campaign campaign)
    {
        campaign.IsActive = !campaign.IsActive;
        await _campaignRepository.SaveAsync(campaign);
    }

    [RelayCommand]
    private async Task DeleteCampaignAsync(Models.Campaign.Campaign campaign)
    {
        await _campaignRepository.DeleteAsync(campaign);
        await LoadCampaignsAsync();
    }
}
