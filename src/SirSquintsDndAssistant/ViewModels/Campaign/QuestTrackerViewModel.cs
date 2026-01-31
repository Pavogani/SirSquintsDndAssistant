using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Campaign;
using SirSquintsDndAssistant.Services.Database.Repositories;
using SirSquintsDndAssistant.Services.Utilities;
using System.Collections.ObjectModel;

namespace SirSquintsDndAssistant.ViewModels.Campaign;

/// <summary>
/// Wrapper for Quest that includes hierarchy information for display.
/// </summary>
public partial class QuestNode : ObservableObject
{
    public Quest Quest { get; set; } = null!;
    public int IndentLevel { get; set; }
    public bool HasChildren { get; set; }
    public bool IsExpanded { get; set; } = true;
    public ObservableCollection<QuestNode> Children { get; set; } = new();

    // Helper properties for display
    public string IndentMargin => new string(' ', IndentLevel * 4);
    public string StatusIcon => Quest.Status switch
    {
        "Active" => "●",
        "Completed" => "✓",
        "Failed" => "✗",
        _ => "○"
    };
    public string StatusColor => Quest.Status switch
    {
        "Active" => "#FFD700",
        "Completed" => "#32CD32",
        "Failed" => "#DC143C",
        _ => "#808080"
    };
    public string ParentDisplay => Quest.ParentQuestId.HasValue ? "(Sub-quest)" : "";
}

public partial class QuestTrackerViewModel : BaseViewModel
{
    private readonly IQuestRepository _questRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IDialogService _dialogService;
    private List<Quest> _allQuests = new();

    [ObservableProperty]
    private ObservableCollection<Quest> activeQuests = new();

    [ObservableProperty]
    private ObservableCollection<Quest> completedQuests = new();

    [ObservableProperty]
    private ObservableCollection<QuestNode> questHierarchy = new();

    [ObservableProperty]
    private string newQuestTitle = string.Empty;

    [ObservableProperty]
    private string newQuestDescription = string.Empty;

    [ObservableProperty]
    private int? activeCampaignId;

    [ObservableProperty]
    private Quest? selectedParentQuest;

    [ObservableProperty]
    private ObservableCollection<Quest> availableParentQuests = new();

    public QuestTrackerViewModel(
        IQuestRepository questRepository,
        ICampaignRepository campaignRepository,
        IDialogService dialogService)
    {
        _questRepository = questRepository;
        _campaignRepository = campaignRepository;
        _dialogService = dialogService;
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
                _allQuests = active.Concat(completed).ToList();

                ActiveQuests.Clear();
                CompletedQuests.Clear();
                AvailableParentQuests.Clear();
                QuestHierarchy.Clear();

                foreach (var quest in active)
                {
                    ActiveQuests.Add(quest);
                    AvailableParentQuests.Add(quest);
                }

                foreach (var quest in completed)
                    CompletedQuests.Add(quest);

                // Build hierarchical view
                BuildQuestHierarchy();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildQuestHierarchy()
    {
        // Get root quests (no parent)
        var rootQuests = _allQuests.Where(q => !q.ParentQuestId.HasValue)
            .OrderBy(q => q.Status == "Completed")
            .ThenByDescending(q => q.Created)
            .ToList();

        foreach (var quest in rootQuests)
        {
            var node = BuildQuestNode(quest, 0);
            QuestHierarchy.Add(node);
        }
    }

    private QuestNode BuildQuestNode(Quest quest, int level)
    {
        var children = _allQuests.Where(q => q.ParentQuestId == quest.Id)
            .OrderBy(q => q.Status == "Completed")
            .ThenByDescending(q => q.Created)
            .ToList();

        var node = new QuestNode
        {
            Quest = quest,
            IndentLevel = level,
            HasChildren = children.Any()
        };

        foreach (var childQuest in children)
        {
            node.Children.Add(BuildQuestNode(childQuest, level + 1));
        }

        return node;
    }

    /// <summary>
    /// Get all child quest IDs (recursive) for a given quest.
    /// </summary>
    private HashSet<int> GetAllDescendantIds(int questId)
    {
        var descendants = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(questId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var children = _allQuests.Where(q => q.ParentQuestId == current);
            foreach (var child in children)
            {
                if (descendants.Add(child.Id))
                {
                    queue.Enqueue(child.Id);
                }
            }
        }

        return descendants;
    }

    /// <summary>
    /// Check if all child quests are completed before allowing parent completion.
    /// </summary>
    private bool HasPendingChildQuests(int questId)
    {
        var children = _allQuests.Where(q => q.ParentQuestId == questId && q.Status == "Active");
        return children.Any();
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
            Created = DateTime.Now,
            ParentQuestId = SelectedParentQuest?.Id
        };

        await _questRepository.SaveAsync(quest);
        await LoadQuestsAsync();

        NewQuestTitle = string.Empty;
        NewQuestDescription = string.Empty;
        SelectedParentQuest = null;
    }

    [RelayCommand]
    private async Task CreateSubQuestAsync(Quest parentQuest)
    {
        if (!ActiveCampaignId.HasValue) return;

        var title = await _dialogService.DisplayPromptAsync("New Sub-Quest",
            $"Enter title for sub-quest of '{parentQuest.Title}':");
        if (string.IsNullOrWhiteSpace(title)) return;

        var quest = new Quest
        {
            CampaignId = ActiveCampaignId.Value,
            Title = title,
            Description = string.Empty,
            Status = "Active",
            Created = DateTime.Now,
            ParentQuestId = parentQuest.Id
        };

        await _questRepository.SaveAsync(quest);
        await LoadQuestsAsync();
    }

    [RelayCommand]
    private async Task CompleteQuestAsync(Quest quest)
    {
        // Check for pending child quests
        if (HasPendingChildQuests(quest.Id))
        {
            var proceed = await _dialogService.DisplayConfirmAsync("Pending Sub-Quests",
                "This quest has incomplete sub-quests. Complete anyway?");
            if (!proceed) return;
        }

        quest.Status = "Completed";
        quest.CompletedDate = DateTime.Now;
        await _questRepository.SaveAsync(quest);
        await LoadQuestsAsync();
    }

    [RelayCommand]
    private async Task FailQuestAsync(Quest quest)
    {
        // Optionally fail child quests too
        var descendants = GetAllDescendantIds(quest.Id);
        if (descendants.Count > 0)
        {
            var failChildren = await _dialogService.DisplayConfirmAsync("Fail Sub-Quests",
                $"This quest has {descendants.Count} sub-quest(s). Fail them too?");
            if (failChildren)
            {
                foreach (var childQuest in _allQuests.Where(q => descendants.Contains(q.Id)))
                {
                    childQuest.Status = "Failed";
                    childQuest.CompletedDate = DateTime.Now;
                    await _questRepository.SaveAsync(childQuest);
                }
            }
        }

        quest.Status = "Failed";
        quest.CompletedDate = DateTime.Now;
        await _questRepository.SaveAsync(quest);
        await LoadQuestsAsync();
    }

    [RelayCommand]
    private async Task DeleteQuestAsync(Quest quest)
    {
        var descendants = GetAllDescendantIds(quest.Id);
        if (descendants.Count > 0)
        {
            var confirm = await _dialogService.DisplayConfirmAsync("Delete Quest",
                $"This will also delete {descendants.Count} sub-quest(s). Continue?");
            if (!confirm) return;
        }

        await _questRepository.DeleteAsync(quest);
        await LoadQuestsAsync();
    }

    [RelayCommand]
    private async Task SetParentQuestAsync(Quest quest)
    {
        // Get available parent quests (can't be self or descendants)
        var descendants = GetAllDescendantIds(quest.Id);
        var available = _allQuests.Where(q =>
            q.Id != quest.Id &&
            q.Status == "Active" &&
            !descendants.Contains(q.Id))
            .ToList();

        if (!available.Any())
        {
            await _dialogService.DisplayAlertAsync("No Available Parents",
                "There are no other active quests to set as parent.");
            return;
        }

        var options = available.Select(q => q.Title).ToArray();
        var selected = await _dialogService.DisplayActionSheetAsync(
            "Select Parent Quest", "Cancel", "Remove Parent", options);

        if (selected == "Cancel") return;

        if (selected == "Remove Parent")
        {
            quest.ParentQuestId = null;
        }
        else
        {
            var parent = available.FirstOrDefault(q => q.Title == selected);
            if (parent != null)
            {
                quest.ParentQuestId = parent.Id;
            }
        }

        await _questRepository.SaveAsync(quest);
        await LoadQuestsAsync();
    }

    [RelayCommand]
    private async Task ReactivateQuestAsync(Quest quest)
    {
        quest.Status = "Active";
        quest.CompletedDate = null;
        await _questRepository.SaveAsync(quest);
        await LoadQuestsAsync();
    }
}
