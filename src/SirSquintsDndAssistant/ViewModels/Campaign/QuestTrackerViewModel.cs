using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Campaign;
using SirSquintsDndAssistant.Services.Database.Repositories;
using System.Collections.ObjectModel;

namespace SirSquintsDndAssistant.ViewModels.Campaign;

public partial class QuestTrackerViewModel : BaseViewModel
{
    private readonly IQuestRepository _questRepository;
    private readonly ICampaignRepository _campaignRepository;

    [ObservableProperty]
    private ObservableCollection<Quest> activeQuests = new();

    [ObservableProperty]
    private ObservableCollection<Quest> completedQuests = new();

    [ObservableProperty]
    private string newQuestTitle = string.Empty;

    [ObservableProperty]
    private string newQuestDescription = string.Empty;

    [ObservableProperty]
    private int? activeCampaignId;

    public QuestTrackerViewModel(IQuestRepository questRepository, ICampaignRepository campaignRepository)
    {
        _questRepository = questRepository;
        _campaignRepository = campaignRepository;
        Title = "Quest Tracker";
    }

    [RelayCommand]
    private async Task LoadQuestsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var activeCampaign = await _campaignRepository.GetActiveCampaignAsync();
            ActiveCampaignId = activeCampaign?.Id;

            if (ActiveCampaignId.HasValue)
            {
                var active = await _questRepository.GetActiveQuestsAsync(ActiveCampaignId.Value);
                var completed = await _questRepository.GetCompletedQuestsAsync(ActiveCampaignId.Value);

                ActiveQuests.Clear();
                CompletedQuests.Clear();

                foreach (var quest in active)
                    ActiveQuests.Add(quest);

                foreach (var quest in completed)
                    CompletedQuests.Add(quest);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateQuestAsync()
    {
        if (!ActiveCampaignId.HasValue || string.IsNullOrWhiteSpace(NewQuestTitle))
            return;

        var quest = new Quest
        {
            CampaignId = ActiveCampaignId.Value,
            Title = NewQuestTitle,
            Description = NewQuestDescription,
            Status = "Active",
            Created = DateTime.Now
        };

        await _questRepository.SaveAsync(quest);
        await LoadQuestsAsync();

        NewQuestTitle = string.Empty;
        NewQuestDescription = string.Empty;
    }

    [RelayCommand]
    private async Task CompleteQuestAsync(Quest quest)
    {
        quest.Status = "Completed";
        quest.CompletedDate = DateTime.Now;
        await _questRepository.SaveAsync(quest);
        await LoadQuestsAsync();
    }

    [RelayCommand]
    private async Task FailQuestAsync(Quest quest)
    {
        quest.Status = "Failed";
        quest.CompletedDate = DateTime.Now;
        await _questRepository.SaveAsync(quest);
        await LoadQuestsAsync();
    }

    [RelayCommand]
    private async Task DeleteQuestAsync(Quest quest)
    {
        await _questRepository.DeleteAsync(quest);
        await LoadQuestsAsync();
    }
}
