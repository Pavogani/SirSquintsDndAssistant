using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Campaign;
using SirSquintsDndAssistant.Services.SessionPrep;
using SirSquintsDndAssistant.Services.Utilities;
using SirSquintsDndAssistant.Services.Database.Repositories;

namespace SirSquintsDndAssistant.ViewModels.Campaign;

public partial class SessionPrepViewModel : ObservableObject
{
    private readonly ISessionPrepService _sessionPrepService;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<Models.Campaign.Campaign> _campaigns = new();

    [ObservableProperty]
    private Models.Campaign.Campaign? _selectedCampaign;

    [ObservableProperty]
    private ObservableCollection<SessionPrepItem> _prepItems = new();

    [ObservableProperty]
    private ObservableCollection<WikiEntry> _wikiEntries = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isCampaignSelected;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private WikiCategory? _selectedCategory;

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private int _totalCount;

    public List<WikiCategory> Categories { get; } = Enum.GetValues<WikiCategory>().ToList();

    public SessionPrepViewModel(ISessionPrepService sessionPrepService, ICampaignRepository campaignRepository, IDialogService dialogService)
    {
        _sessionPrepService = sessionPrepService;
        _campaignRepository = campaignRepository;
        _dialogService = dialogService;
    }

    partial void OnSelectedCampaignChanged(Models.Campaign.Campaign? value)
    {
        IsCampaignSelected = value != null;
        if (value != null)
        {
            _ = LoadPrepItemsAsync();
            _ = LoadWikiEntriesAsync();
        }
        else
        {
            PrepItems.Clear();
            WikiEntries.Clear();
        }
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var campaigns = await _campaignRepository.GetAllAsync();
            Campaigns = new ObservableCollection<Models.Campaign.Campaign>(campaigns);

            // Select active campaign by default
            SelectedCampaign = campaigns.FirstOrDefault(c => c.IsActive) ?? campaigns.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPrepItemsAsync()
    {
        if (SelectedCampaign == null) return;

        try
        {
            var items = await _sessionPrepService.GetPrepItemsForCampaignAsync(SelectedCampaign.Id);
            PrepItems = new ObservableCollection<SessionPrepItem>(items);
            TotalCount = items.Count;
            CompletedCount = items.Count(i => i.IsCompleted);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading prep items: {ex.Message}");
        }
    }

    private async Task LoadWikiEntriesAsync()
    {
        if (SelectedCampaign == null) return;

        try
        {
            List<WikiEntry> entries;
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                entries = await _sessionPrepService.SearchWikiAsync(SelectedCampaign.Id, SearchText);
            }
            else if (SelectedCategory.HasValue)
            {
                entries = await _sessionPrepService.GetWikiEntriesByCategoryAsync(SelectedCampaign.Id, SelectedCategory.Value);
            }
            else
            {
                entries = await _sessionPrepService.GetWikiEntriesForCampaignAsync(SelectedCampaign.Id);
            }
            WikiEntries = new ObservableCollection<WikiEntry>(entries);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading wiki entries: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CreatePrepItemAsync()
    {
        if (SelectedCampaign == null) return;

        var title = await _dialogService.DisplayPromptAsync("New Prep Item", "Enter item title:");
        if (string.IsNullOrWhiteSpace(title)) return;

        var types = new[] { "Agenda", "Scene", "Encounter", "NPC Interaction", "Location", "Plot Point", "Treasure", "Puzzle", "Roleplay", "Combat", "Note", "Other" };
        var typeStr = await _dialogService.DisplayActionSheetAsync("Item Type", "Cancel", null, types);
        if (typeStr == "Cancel" || typeStr == null) return;

        var item = new SessionPrepItem
        {
            CampaignId = SelectedCampaign.Id,
            Title = title,
            ItemType = typeStr switch
            {
                "Agenda" => PrepItemType.Agenda,
                "Scene" => PrepItemType.Scene,
                "Encounter" => PrepItemType.Encounter,
                "NPC Interaction" => PrepItemType.NpcInteraction,
                "Location" => PrepItemType.Location,
                "Plot Point" => PrepItemType.PlotPoint,
                "Treasure" => PrepItemType.Treasure,
                "Puzzle" => PrepItemType.Puzzle,
                "Roleplay" => PrepItemType.Roleplay,
                "Combat" => PrepItemType.Combat,
                "Note" => PrepItemType.Note,
                _ => PrepItemType.Other
            }
        };

        await _sessionPrepService.SavePrepItemAsync(item);
        await LoadPrepItemsAsync();
    }

    [RelayCommand]
    private async Task TogglePrepItemCompletedAsync(SessionPrepItem item)
    {
        await _sessionPrepService.MarkItemCompletedAsync(item.Id, !item.IsCompleted);
        await LoadPrepItemsAsync();
    }

    [RelayCommand]
    private async Task DeletePrepItemAsync(SessionPrepItem item)
    {
        var confirm = await _dialogService.DisplayConfirmAsync("Delete Item",
            $"Delete '{item.Title}'?");
        if (!confirm) return;

        await _sessionPrepService.DeletePrepItemAsync(item.Id);
        PrepItems.Remove(item);
        TotalCount--;
        if (item.IsCompleted) CompletedCount--;
    }

    [RelayCommand]
    private async Task EditPrepItemNotesAsync(SessionPrepItem item)
    {
        var notes = await _dialogService.DisplayPromptAsync("Edit Notes",
            "Enter notes for this item:", initialValue: item.Notes, maxLength: 1000);
        if (notes == null) return;

        item.Notes = notes;
        await _sessionPrepService.SavePrepItemAsync(item);
    }

    [RelayCommand]
    private async Task CreateWikiEntryAsync()
    {
        if (SelectedCampaign == null) return;

        var title = await _dialogService.DisplayPromptAsync("New Wiki Entry", "Enter entry title:");
        if (string.IsNullOrWhiteSpace(title)) return;

        var categories = Categories.Select(c => c.ToString()).ToArray();
        var categoryStr = await _dialogService.DisplayActionSheetAsync("Category", "Cancel", null, categories);
        if (categoryStr == "Cancel" || categoryStr == null) return;

        var entry = new WikiEntry
        {
            CampaignId = SelectedCampaign.Id,
            Title = title,
            Category = Enum.Parse<WikiCategory>(categoryStr)
        };

        await _sessionPrepService.SaveWikiEntryAsync(entry);
        await LoadWikiEntriesAsync();
    }

    [RelayCommand]
    private async Task EditWikiEntryAsync(WikiEntry entry)
    {
        var content = await _dialogService.DisplayPromptAsync("Edit Content",
            $"Content for '{entry.Title}':", initialValue: entry.Content, maxLength: 10000);
        if (content == null) return;

        entry.Content = content;
        await _sessionPrepService.SaveWikiEntryAsync(entry);
    }

    [RelayCommand]
    private async Task DeleteWikiEntryAsync(WikiEntry entry)
    {
        var confirm = await _dialogService.DisplayConfirmAsync("Delete Entry",
            $"Delete '{entry.Title}'?");
        if (!confirm) return;

        await _sessionPrepService.DeleteWikiEntryAsync(entry.Id);
        WikiEntries.Remove(entry);
    }

    [RelayCommand]
    private async Task ToggleWikiSecretAsync(WikiEntry entry)
    {
        entry.IsSecret = !entry.IsSecret;
        await _sessionPrepService.SaveWikiEntryAsync(entry);
        await LoadWikiEntriesAsync();
    }

    [RelayCommand]
    private async Task ToggleWikiPlayerKnownAsync(WikiEntry entry)
    {
        entry.IsPlayerKnown = !entry.IsPlayerKnown;
        await _sessionPrepService.SaveWikiEntryAsync(entry);
        await LoadWikiEntriesAsync();
    }

    [RelayCommand]
    private async Task SearchWikiAsync()
    {
        await LoadWikiEntriesAsync();
    }

    [RelayCommand]
    private async Task FilterByCategoryAsync(WikiCategory? category)
    {
        SelectedCategory = category;
        await LoadWikiEntriesAsync();
    }

    [RelayCommand]
    private async Task ClearFilterAsync()
    {
        SelectedCategory = null;
        SearchText = string.Empty;
        await LoadWikiEntriesAsync();
    }
}
