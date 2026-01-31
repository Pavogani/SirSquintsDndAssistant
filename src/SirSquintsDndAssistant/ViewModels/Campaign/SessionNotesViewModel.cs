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
    private List<Session> _orderedSessions = new();

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

    // Navigation properties
    [ObservableProperty]
    private bool hasPreviousSession;

    [ObservableProperty]
    private bool hasNextSession;

    [ObservableProperty]
    private string previousSessionTitle = string.Empty;

    [ObservableProperty]
    private string nextSessionTitle = string.Empty;

    [ObservableProperty]
    private int currentSessionIndex;

    [ObservableProperty]
    private int totalSessionCount;

    public SessionNotesViewModel(ISessionRepository sessionRepository, ICampaignRepository campaignRepository)
    {
        _sessionRepository = sessionRepository;
        _campaignRepository = campaignRepository;
        Title = "Session Notes";
    }

    partial void OnSelectedSessionChanged(Session? value)
    {
        UpdateNavigationState();
    }

    private void UpdateNavigationState()
    {
        if (SelectedSession == null || _orderedSessions.Count == 0)
        {
            HasPreviousSession = false;
            HasNextSession = false;
            PreviousSessionTitle = string.Empty;
            NextSessionTitle = string.Empty;
            CurrentSessionIndex = 0;
            return;
        }

        var currentIndex = _orderedSessions.FindIndex(s => s.Id == SelectedSession.Id);
        CurrentSessionIndex = currentIndex + 1;
        TotalSessionCount = _orderedSessions.Count;

        // Check for previous session (earlier in list = later session number)
        HasPreviousSession = currentIndex > 0;
        if (HasPreviousSession)
        {
            var prev = _orderedSessions[currentIndex - 1];
            PreviousSessionTitle = $"← #{prev.SessionNumber}: {prev.Title}";
        }
        else
        {
            PreviousSessionTitle = string.Empty;
        }

        // Check for next session (later in list = earlier session number)
        HasNextSession = currentIndex < _orderedSessions.Count - 1;
        if (HasNextSession)
        {
            var next = _orderedSessions[currentIndex + 1];
            NextSessionTitle = $"#{next.SessionNumber}: {next.Title} →";
        }
        else
        {
            NextSessionTitle = string.Empty;
        }
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

                // Order by session number descending (most recent first)
                _orderedSessions = sessions.OrderByDescending(s => s.SessionNumber).ToList();

                Sessions.Clear();
                foreach (var session in _orderedSessions)
                {
                    Sessions.Add(session);
                }

                TotalSessionCount = _orderedSessions.Count;
                UpdateNavigationState();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoToPreviousSession()
    {
        if (!HasPreviousSession || SelectedSession == null) return;

        var currentIndex = _orderedSessions.FindIndex(s => s.Id == SelectedSession.Id);
        if (currentIndex > 0)
        {
            SelectSession(_orderedSessions[currentIndex - 1]);
        }
    }

    [RelayCommand]
    private void GoToNextSession()
    {
        if (!HasNextSession || SelectedSession == null) return;

        var currentIndex = _orderedSessions.FindIndex(s => s.Id == SelectedSession.Id);
        if (currentIndex < _orderedSessions.Count - 1)
        {
            SelectSession(_orderedSessions[currentIndex + 1]);
        }
    }

    [RelayCommand]
    private void GoToFirstSession()
    {
        if (_orderedSessions.Count > 0)
        {
            // First in ordered list = most recent session
            SelectSession(_orderedSessions.First());
        }
    }

    [RelayCommand]
    private void GoToLastSession()
    {
        if (_orderedSessions.Count > 0)
        {
            // Last in ordered list = first session
            SelectSession(_orderedSessions.Last());
        }
    }

    [RelayCommand]
    private async Task GoToSessionByNumberAsync()
    {
        if (_orderedSessions.Count == 0) return;

        var maxSession = _orderedSessions.Max(s => s.SessionNumber);
        var input = await Shell.Current.DisplayPromptAsync("Go to Session",
            $"Enter session number (1-{maxSession}):",
            keyboard: Keyboard.Numeric);

        if (int.TryParse(input, out var number))
        {
            var session = _orderedSessions.FirstOrDefault(s => s.SessionNumber == number);
            if (session != null)
            {
                SelectSession(session);
            }
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
