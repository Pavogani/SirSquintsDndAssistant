using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Multiplayer;
using SirSquintsDndAssistant.Services.Multiplayer;
using SirSquintsDndAssistant.Services.Utilities;
using SirSquintsDndAssistant.Services.Database.Repositories;

namespace SirSquintsDndAssistant.ViewModels.Multiplayer;

public partial class PlayerSessionViewModel : ObservableObject
{
    private readonly IPlayerSessionService _sessionService;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IDialogService _dialogService;
    private readonly DiceRoller _diceRoller;

    [ObservableProperty]
    private GameSession? _currentSession;

    [ObservableProperty]
    private ObservableCollection<SessionPlayer> _players = new();

    [ObservableProperty]
    private ObservableCollection<SharedDiceRoll> _recentRolls = new();

    [ObservableProperty]
    private ObservableCollection<Models.Campaign.Campaign> _campaigns = new();

    [ObservableProperty]
    private Models.Campaign.Campaign? _selectedCampaign;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasActiveSession;

    [ObservableProperty]
    private string _sessionCode = string.Empty;

    [ObservableProperty]
    private string _joinCode = string.Empty;

    [ObservableProperty]
    private string _dmName = string.Empty;

    [ObservableProperty]
    private string _sessionName = string.Empty;

    [ObservableProperty]
    private string _broadcastTitle = string.Empty;

    [ObservableProperty]
    private string _broadcastMessage = string.Empty;

    [ObservableProperty]
    private int _playerCount;

    public PlayerSessionViewModel(
        IPlayerSessionService sessionService,
        ICampaignRepository campaignRepository,
        IDialogService dialogService,
        DiceRoller diceRoller)
    {
        _sessionService = sessionService;
        _campaignRepository = campaignRepository;
        _dialogService = dialogService;
        _diceRoller = diceRoller;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            // Load campaigns
            var campaigns = await _campaignRepository.GetAllAsync();
            Campaigns = new ObservableCollection<Models.Campaign.Campaign>(campaigns);
            SelectedCampaign = campaigns.FirstOrDefault(c => c.IsActive) ?? campaigns.FirstOrDefault();

            // Check for active session
            var activeSession = await _sessionService.GetActiveSessionAsync();
            if (activeSession != null)
            {
                CurrentSession = activeSession;
                HasActiveSession = true;
                SessionCode = activeSession.SessionCode;
                SessionName = activeSession.Name;
                BroadcastTitle = activeSession.BroadcastTitle;
                BroadcastMessage = activeSession.BroadcastMessage;

                await LoadPlayersAsync();
                await LoadRecentRollsAsync();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPlayersAsync()
    {
        if (CurrentSession == null) return;

        var players = await _sessionService.GetPlayersInSessionAsync(CurrentSession.Id);
        Players = new ObservableCollection<SessionPlayer>(players);
        PlayerCount = players.Count;
    }

    private async Task LoadRecentRollsAsync()
    {
        if (CurrentSession == null) return;

        var rolls = await _sessionService.GetRecentRollsAsync(CurrentSession.Id, 20);
        RecentRolls = new ObservableCollection<SharedDiceRoll>(rolls);
    }

    [RelayCommand]
    private async Task CreateSessionAsync()
    {
        if (SelectedCampaign == null)
        {
            await _dialogService.DisplayAlertAsync("Error", "Please select a campaign first.");
            return;
        }

        if (string.IsNullOrWhiteSpace(DmName))
        {
            DmName = await _dialogService.DisplayPromptAsync("DM Name", "Enter your name:") ?? "DM";
        }

        var name = await _dialogService.DisplayPromptAsync("Session Name",
            "Enter a name for this session:", initialValue: $"{SelectedCampaign.Name} Session");
        if (string.IsNullOrWhiteSpace(name)) return;

        CurrentSession = await _sessionService.CreateSessionAsync(name, SelectedCampaign.Id, DmName);
        HasActiveSession = true;
        SessionCode = CurrentSession.SessionCode;
        SessionName = CurrentSession.Name;

        await _dialogService.DisplayAlertAsync("Session Created",
            $"Share this code with your players: {SessionCode}");
    }

    [RelayCommand]
    private async Task JoinSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(JoinCode))
        {
            JoinCode = await _dialogService.DisplayPromptAsync("Join Session",
                "Enter the 6-character session code:") ?? "";
        }

        if (string.IsNullOrWhiteSpace(JoinCode) || JoinCode.Length != 6)
        {
            await _dialogService.DisplayAlertAsync("Invalid Code", "Please enter a valid 6-character code.");
            return;
        }

        var session = await _sessionService.GetSessionByCodeAsync(JoinCode.ToUpper());
        if (session == null)
        {
            await _dialogService.DisplayAlertAsync("Session Not Found",
                "No active session found with that code.");
            return;
        }

        var playerName = await _dialogService.DisplayPromptAsync("Your Name",
            "Enter your name:");
        if (string.IsNullOrWhiteSpace(playerName)) return;

        await _sessionService.JoinSessionAsync(session.Id, playerName, null);
        CurrentSession = session;
        HasActiveSession = true;
        SessionCode = session.SessionCode;
        SessionName = session.Name;

        await LoadPlayersAsync();
        await LoadRecentRollsAsync();
    }

    [RelayCommand]
    private async Task EndSessionAsync()
    {
        if (CurrentSession == null) return;

        var confirm = await _dialogService.DisplayConfirmAsync("End Session",
            "Are you sure you want to end this session?");
        if (!confirm) return;

        await _sessionService.EndSessionAsync(CurrentSession.Id);
        CurrentSession = null;
        HasActiveSession = false;
        SessionCode = string.Empty;
        Players.Clear();
        RecentRolls.Clear();
    }

    [RelayCommand]
    private async Task BroadcastToPlayersAsync()
    {
        if (CurrentSession == null) return;

        var title = await _dialogService.DisplayPromptAsync("Broadcast",
            "Enter message title:", initialValue: BroadcastTitle);
        if (title == null) return;

        var message = await _dialogService.DisplayPromptAsync("Broadcast",
            "Enter message:", initialValue: BroadcastMessage, maxLength: 500);
        if (message == null) return;

        BroadcastTitle = title;
        BroadcastMessage = message;

        await _sessionService.BroadcastMessageAsync(CurrentSession.Id, title, message);
        await _dialogService.DisplayAlertAsync("Broadcast Sent", "Your message has been sent to all players.");
    }

    [RelayCommand]
    private async Task ClearBroadcastAsync()
    {
        if (CurrentSession == null) return;

        await _sessionService.ClearBroadcastAsync(CurrentSession.Id);
        BroadcastTitle = string.Empty;
        BroadcastMessage = string.Empty;
    }

    [RelayCommand]
    private async Task ShareDiceRollAsync()
    {
        if (CurrentSession == null)
        {
            await _dialogService.DisplayAlertAsync("No Session", "Join or create a session first.");
            return;
        }

        var expression = await _dialogService.DisplayPromptAsync("Roll Dice",
            "Enter dice expression (e.g., 1d20+5, 2d6+3):");
        if (string.IsNullOrWhiteSpace(expression)) return;

        try
        {
            var (rolls, modifier) = ParseAndRollDice(expression);

            await _sessionService.ShareDiceRollAsync(
                CurrentSession.Id,
                null,
                string.IsNullOrWhiteSpace(DmName) ? "DM" : DmName,
                expression,
                rolls,
                modifier);

            await LoadRecentRollsAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.DisplayAlertAsync("Roll Error", $"Invalid dice expression: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task QuickRollD20Async()
    {
        if (CurrentSession == null) return;

        var (total, rolls) = _diceRoller.RollWithDetails(1, 20);
        await _sessionService.ShareDiceRollAsync(
            CurrentSession.Id,
            null,
            string.IsNullOrWhiteSpace(DmName) ? "DM" : DmName,
            "1d20",
            rolls.ToArray(),
            0);

        await LoadRecentRollsAsync();
    }

    private (int[] rolls, int modifier) ParseAndRollDice(string notation)
    {
        var parts = notation.ToLower().Replace(" ", "").Split('d');
        if (parts.Length != 2) throw new ArgumentException("Invalid dice notation");

        int count = int.Parse(parts[0]);
        int modifier = 0;
        int sides;

        if (parts[1].Contains('+'))
        {
            var sideParts = parts[1].Split('+');
            sides = int.Parse(sideParts[0]);
            modifier = int.Parse(sideParts[1]);
        }
        else if (parts[1].Contains('-'))
        {
            var sideParts = parts[1].Split('-');
            sides = int.Parse(sideParts[0]);
            modifier = -int.Parse(sideParts[1]);
        }
        else
        {
            sides = int.Parse(parts[1]);
        }

        var (_, rolls) = _diceRoller.RollWithDetails(count, sides);
        return (rolls.ToArray(), modifier);
    }

    [RelayCommand]
    private async Task RemovePlayerAsync(SessionPlayer player)
    {
        if (CurrentSession == null) return;

        var confirm = await _dialogService.DisplayConfirmAsync("Remove Player",
            $"Remove '{player.PlayerName}' from the session?");
        if (!confirm) return;

        await _sessionService.LeaveSessionAsync(player.Id);
        Players.Remove(player);
        PlayerCount--;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (CurrentSession == null) return;

        await LoadPlayersAsync();
        await LoadRecentRollsAsync();

        // Refresh session state
        var session = await _sessionService.GetSessionByCodeAsync(SessionCode);
        if (session != null)
        {
            CurrentSession = session;
            BroadcastTitle = session.BroadcastTitle;
            BroadcastMessage = session.BroadcastMessage;
        }
    }

    [RelayCommand]
    private async Task CopySessionCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionCode)) return;

        await Clipboard.SetTextAsync(SessionCode);
        await _dialogService.DisplayAlertAsync("Copied", $"Session code '{SessionCode}' copied to clipboard.");
    }
}
