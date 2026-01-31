using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Campaign;
using SirSquintsDndAssistant.Services.Database.Repositories;
using System.Collections.ObjectModel;

namespace SirSquintsDndAssistant.ViewModels.Campaign;

public partial class SessionNotesViewModel : BaseViewModel
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ICampaignRepository _campaignRepository;

    [ObservableProperty]
    private ObservableCollection<Session> sessions = new();

    [ObservableProperty]
    private Session? selectedSession;

    [ObservableProperty]
    private string sessionTitle = string.Empty;

    [ObservableProperty]
    private string sessionNotes = string.Empty;

    [ObservableProperty]
    private int? activeCampaignId;

    public SessionNotesViewModel(ISessionRepository sessionRepository, ICampaignRepository campaignRepository)
    {
        _sessionRepository = sessionRepository;
        _campaignRepository = campaignRepository;
        Title = "Session Notes";
    }

    [RelayCommand]
    private async Task LoadSessionsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var activeCampaign = await _campaignRepository.GetActiveCampaignAsync();
            ActiveCampaignId = activeCampaign?.Id;

            if (ActiveCampaignId.HasValue)
            {
                var sessions = await _sessionRepository.GetByCampaignAsync(ActiveCampaignId.Value);
                Sessions.Clear();

                foreach (var session in sessions)
                {
                    Sessions.Add(session);
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateSessionAsync()
    {
        if (!ActiveCampaignId.HasValue || string.IsNullOrWhiteSpace(SessionTitle))
            return;

        var latestSession = await _sessionRepository.GetLatestSessionAsync(ActiveCampaignId.Value);
        var sessionNumber = latestSession?.SessionNumber + 1 ?? 1;

        var session = new Session
        {
            CampaignId = ActiveCampaignId.Value,
            SessionNumber = sessionNumber,
            Title = SessionTitle,
            SessionDate = DateTime.Now,
            NotesMarkdown = SessionNotes,
            Created = DateTime.Now,
            Modified = DateTime.Now
        };

        await _sessionRepository.SaveAsync(session);
        await LoadSessionsAsync();

        SessionTitle = string.Empty;
        SessionNotes = string.Empty;
    }

    [RelayCommand]
    private async Task SaveSessionAsync(Session session)
    {
        if (session == null) return;
        await _sessionRepository.SaveAsync(session);
    }

    [RelayCommand]
    private void SelectSession(Session? session)
    {
        SelectedSession = session;
        if (session != null)
        {
            SessionTitle = session.Title;
            SessionNotes = session.NotesMarkdown;
        }
    }
}
