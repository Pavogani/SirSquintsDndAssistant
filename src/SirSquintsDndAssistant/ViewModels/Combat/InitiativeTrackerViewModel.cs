using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Extensions;
using SirSquintsDndAssistant.Models.Combat;
using SirSquintsDndAssistant.Models.Creatures;
using SirSquintsDndAssistant.Services.Combat;
using SirSquintsDndAssistant.Services.Database.Repositories;
using SirSquintsDndAssistant.Services.Utilities;
using System.Collections.ObjectModel;
using DndCondition = SirSquintsDndAssistant.Models.Content.Condition;

namespace SirSquintsDndAssistant.ViewModels.Combat;

public partial class InitiativeTrackerViewModel : BaseViewModel
{
    private readonly ICombatService _combatService;
    private readonly IMonsterRepository _monsterRepository;
    private readonly IPlayerCharacterRepository _playerCharacterRepository;
    private readonly IConditionRepository _conditionRepository;
    private readonly IDialogService _dialogService;
    private readonly ISpellSlotService _spellSlotService;
    private readonly ICombatLogService _combatLogService;

    [ObservableProperty]
    private ObservableCollection<InitiativeEntry> combatants = new();

    [ObservableProperty]
    private ObservableCollection<PlayerCharacter> availablePlayers = new();

    [ObservableProperty]
    private ObservableCollection<DndCondition> availableConditions = new();

    [ObservableProperty]
    private int currentRound = 1;

    [ObservableProperty]
    private int currentTurnIndex = 0;

    [ObservableProperty]
    private InitiativeEntry? currentCombatant;

    [ObservableProperty]
    private bool isCombatActive;

    [ObservableProperty]
    private string encounterName = "New Combat";

    [ObservableProperty]
    private bool isPlayerPanelVisible;

    [ObservableProperty]
    private bool isConditionPanelVisible;

    [ObservableProperty]
    private InitiativeEntry? conditionTargetEntry;

    // Combat Log
    [ObservableProperty]
    private ObservableCollection<CombatLogEntry> combatLogEntries = new();

    [ObservableProperty]
    private bool isCombatLogVisible;

    // Spell Slots
    [ObservableProperty]
    private bool isSpellSlotPanelVisible;

    [ObservableProperty]
    private InitiativeEntry? spellSlotTargetEntry;

    [ObservableProperty]
    private SpellSlotTracker? currentSpellTracker;

    // Add Combatant fields
    [ObservableProperty]
    private string newCombatantName = string.Empty;

    [ObservableProperty]
    private string newCombatantType = "Monster";

    [ObservableProperty]
    private int newCombatantAC = 10;

    [ObservableProperty]
    private int newCombatantHP = 10;

    [ObservableProperty]
    private int newCombatantInitBonus = 0;

    public ObservableCollection<string> CombatantTypes { get; } = new()
    {
        "Monster", "Player", "NPC", "Other"
    };

    public InitiativeTrackerViewModel(
        ICombatService combatService,
        IMonsterRepository monsterRepository,
        IPlayerCharacterRepository playerCharacterRepository,
        IConditionRepository conditionRepository,
        IDialogService dialogService,
        ISpellSlotService spellSlotService,
        ICombatLogService combatLogService)
    {
        _combatService = combatService;
        _monsterRepository = monsterRepository;
        _playerCharacterRepository = playerCharacterRepository;
        _conditionRepository = conditionRepository;
        _dialogService = dialogService;
        _spellSlotService = spellSlotService;
        _combatLogService = combatLogService;
        Title = "Initiative Tracker";

        // Subscribe to combat log events
        _combatLogService.LogEntryAdded += OnCombatLogEntryAdded;
    }

    private void OnCombatLogEntryAdded(object? sender, CombatLogEntry entry)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CombatLogEntries.Insert(0, entry); // Add to top of list
            if (CombatLogEntries.Count > 100)
            {
                CombatLogEntries.RemoveAt(CombatLogEntries.Count - 1); // Keep last 100 entries
            }
        });
    }

    [RelayCommand]
    private async Task LoadCombatAsync()
    {
        await _combatService.LoadActiveCombatAsync();
        await RefreshCombatantsAsync();
    }

    [RelayCommand]
    private async Task StartCombatAsync()
    {
        if (string.IsNullOrWhiteSpace(EncounterName))
        {
            EncounterName = $"Combat {DateTime.Now:yyyy-MM-dd HH:mm}";
        }

        await _combatService.StartNewCombatAsync(EncounterName);
        IsCombatActive = true;
        CurrentRound = 1;
        CurrentTurnIndex = 0;
        Combatants.Clear();
    }

    [RelayCommand]
    private async Task EndCombatAsync()
    {
        await _combatService.EndCombatAsync();
        IsCombatActive = false;
        Combatants.Clear();
        EncounterName = "New Combat";
    }

    [RelayCommand]
    private async Task AddCombatantAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCombatantName))
            return;

        var entry = await _combatService.AddCombatantAsync(
            NewCombatantName,
            NewCombatantType,
            NewCombatantAC,
            NewCombatantHP,
            NewCombatantInitBonus);

        // Roll initiative automatically
        await _combatService.RollInitiativeAsync(entry);

        await RefreshCombatantsAsync();

        // Clear form
        NewCombatantName = string.Empty;
        NewCombatantAC = 10;
        NewCombatantHP = 10;
        NewCombatantInitBonus = 0;
    }

    [RelayCommand]
    private async Task RemoveCombatantAsync(InitiativeEntry entry)
    {
        await _combatService.RemoveCombatantAsync(entry);
        await RefreshCombatantsAsync();
    }

    [RelayCommand]
    private async Task RollAllInitiativeAsync()
    {
        foreach (var combatant in Combatants)
        {
            await _combatService.RollInitiativeAsync(combatant);
        }

        await _combatService.SortByInitiativeAsync();
        await RefreshCombatantsAsync();
    }

    [RelayCommand]
    private async Task NextTurnAsync()
    {
        await _combatService.NextTurnAsync();

        if (_combatService.ActiveCombat != null)
        {
            CurrentRound = _combatService.ActiveCombat.CurrentRound;
            CurrentTurnIndex = _combatService.ActiveCombat.CurrentTurnIndex;
            UpdateCurrentCombatant();
        }
    }

    [RelayCommand]
    private async Task PreviousTurnAsync()
    {
        await _combatService.PreviousTurnAsync();

        if (_combatService.ActiveCombat != null)
        {
            CurrentRound = _combatService.ActiveCombat.CurrentRound;
            CurrentTurnIndex = _combatService.ActiveCombat.CurrentTurnIndex;
            UpdateCurrentCombatant();
        }
    }

    [RelayCommand]
    private async Task ApplyDamageAsync(InitiativeEntry entry)
    {
        var result = await _dialogService.DisplayPromptAsync(
            "Apply Damage",
            $"Enter damage amount for {entry.Name}:",
            "Apply",
            "Cancel",
            placeholder: "0",
            keyboard: Keyboard.Numeric);

        if (!string.IsNullOrEmpty(result) && int.TryParse(result, out int damage) && damage > 0)
        {
            await _combatService.ApplyDamageAsync(entry, damage);
            await RefreshCombatantsAsync();
        }
    }

    [RelayCommand]
    private async Task ApplyHealingAsync(InitiativeEntry entry)
    {
        var result = await _dialogService.DisplayPromptAsync(
            "Apply Healing",
            $"Enter healing amount for {entry.Name}:",
            "Heal",
            "Cancel",
            placeholder: "0",
            keyboard: Keyboard.Numeric);

        if (!string.IsNullOrEmpty(result) && int.TryParse(result, out int healing) && healing > 0)
        {
            await _combatService.ApplyHealingAsync(entry, healing);
            await RefreshCombatantsAsync();
        }
    }

    [RelayCommand]
    private async Task QuickDamageAsync(InitiativeEntry entry)
    {
        // Quick damage of 1
        await _combatService.ApplyDamageAsync(entry, 1);
        await RefreshCombatantsAsync();
    }

    [RelayCommand]
    private async Task QuickHealAsync(InitiativeEntry entry)
    {
        // Quick heal of 1
        await _combatService.ApplyHealingAsync(entry, 1);
        await RefreshCombatantsAsync();
    }

    [RelayCommand]
    private void TogglePlayerPanel()
    {
        IsPlayerPanelVisible = !IsPlayerPanelVisible;
        if (IsPlayerPanelVisible)
        {
            LoadPlayersAsync().SafeFireAndForget();
        }
    }

    [RelayCommand]
    private async Task LoadPlayersAsync()
    {
        var players = await _playerCharacterRepository.GetAllAsync();
        AvailablePlayers.Clear();
        foreach (var player in players)
        {
            AvailablePlayers.Add(player);
        }
    }

    [RelayCommand]
    private async Task AddPlayerToCombatAsync(PlayerCharacter player)
    {
        if (player == null || !IsCombatActive)
            return;

        // Check if player is already in combat
        if (Combatants.Any(c => c.CombatantType == "Player" && c.ReferenceId == player.Id))
        {
            await _dialogService.DisplayAlertAsync(
                "Already Added",
                $"{player.Name} is already in combat.");
            return;
        }

        // Calculate initiative bonus from level (rough estimate: level/4 as Dex bonus)
        int initBonus = player.Level / 4;

        var entry = await _combatService.AddCombatantAsync(
            player.Name,
            "Player",
            player.ArmorClass,
            player.MaxHitPoints,
            initBonus,
            player.Id);

        await _combatService.RollInitiativeAsync(entry);
        await RefreshCombatantsAsync();

        IsPlayerPanelVisible = false;
    }

    [RelayCommand]
    private async Task AddAllPlayersToCombatAsync()
    {
        if (!IsCombatActive)
            return;

        foreach (var player in AvailablePlayers)
        {
            // Skip if already in combat
            if (Combatants.Any(c => c.CombatantType == "Player" && c.ReferenceId == player.Id))
                continue;

            int initBonus = player.Level / 4;

            var entry = await _combatService.AddCombatantAsync(
                player.Name,
                "Player",
                player.ArmorClass,
                player.MaxHitPoints,
                initBonus,
                player.Id);

            await _combatService.RollInitiativeAsync(entry);
        }

        await RefreshCombatantsAsync();
        IsPlayerPanelVisible = false;
    }

    private async Task RefreshCombatantsAsync()
    {
        var combatants = await _combatService.GetCombatantsAsync();
        Combatants.Clear();

        foreach (var combatant in combatants.OrderByDescending(c => c.Initiative).ThenBy(c => c.SortOrder))
        {
            Combatants.Add(combatant);
        }

        UpdateCurrentCombatant();
    }

    private void UpdateCurrentCombatant()
    {
        if (Combatants.Count > 0 && CurrentTurnIndex >= 0 && CurrentTurnIndex < Combatants.Count)
        {
            CurrentCombatant = Combatants[CurrentTurnIndex];
        }
        else
        {
            CurrentCombatant = null;
        }
    }

    [RelayCommand]
    private async Task OpenConditionPanelAsync(InitiativeEntry entry)
    {
        ConditionTargetEntry = entry;
        await LoadConditionsAsync();
        IsConditionPanelVisible = true;
    }

    [RelayCommand]
    private void CloseConditionPanel()
    {
        IsConditionPanelVisible = false;
        ConditionTargetEntry = null;
    }

    [RelayCommand]
    private async Task LoadConditionsAsync()
    {
        var conditions = await _conditionRepository.GetAllAsync();
        AvailableConditions.Clear();
        foreach (var condition in conditions)
        {
            AvailableConditions.Add(condition);
        }
    }

    [RelayCommand]
    private async Task AddConditionAsync(DndCondition condition)
    {
        if (ConditionTargetEntry == null || condition == null)
            return;

        await _combatService.AddConditionAsync(ConditionTargetEntry, condition.Name);
        await RefreshCombatantsAsync();
    }

    [RelayCommand]
    private async Task RemoveConditionAsync(string conditionName)
    {
        if (ConditionTargetEntry == null || string.IsNullOrEmpty(conditionName))
            return;

        await _combatService.RemoveConditionAsync(ConditionTargetEntry, conditionName);
        await RefreshCombatantsAsync();
    }

    public List<string> GetConditionsForEntry(InitiativeEntry entry)
    {
        return _combatService.GetConditions(entry);
    }

    // Combat Log Commands
    [RelayCommand]
    private void ToggleCombatLog()
    {
        IsCombatLogVisible = !IsCombatLogVisible;
        if (IsCombatLogVisible)
        {
            // Load existing log entries
            var entries = _combatLogService.CurrentSessionLog;
            CombatLogEntries.Clear();
            foreach (var entry in entries.OrderByDescending(e => e.Timestamp).Take(100))
            {
                CombatLogEntries.Add(entry);
            }
        }
    }

    [RelayCommand]
    private async Task AddCustomLogEntryAsync()
    {
        var description = await _dialogService.DisplayPromptAsync(
            "Add Log Entry",
            "Enter custom combat note:",
            maxLength: 200);

        if (!string.IsNullOrWhiteSpace(description) && _combatService.ActiveCombat != null)
        {
            await _combatLogService.LogCustomAsync(
                _combatService.ActiveCombat.Id,
                CurrentRound,
                description);
        }
    }

    [RelayCommand]
    private void ClearCombatLog()
    {
        _combatLogService.ClearCurrentSessionLog();
        CombatLogEntries.Clear();
    }

    // Spell Slot Commands
    [RelayCommand]
    private async Task OpenSpellSlotPanelAsync(InitiativeEntry entry)
    {
        SpellSlotTargetEntry = entry;
        CurrentSpellTracker = await _spellSlotService.GetTrackerForCombatantAsync(entry.Id);
        IsSpellSlotPanelVisible = true;
    }

    [RelayCommand]
    private void CloseSpellSlotPanel()
    {
        IsSpellSlotPanelVisible = false;
        SpellSlotTargetEntry = null;
        CurrentSpellTracker = null;
    }

    [RelayCommand]
    private async Task CreateSpellTrackerAsync()
    {
        if (SpellSlotTargetEntry == null) return;

        var classes = new[] { "Bard", "Cleric", "Druid", "Paladin", "Ranger", "Sorcerer", "Warlock", "Wizard", "Custom" };
        var selectedClass = await _dialogService.DisplayActionSheetAsync(
            "Select Class",
            "Cancel",
            null,
            classes);

        if (selectedClass == "Cancel" || selectedClass == null) return;

        if (selectedClass == "Custom")
        {
            // Get custom slots
            var slots = await _dialogService.DisplayPromptAsync(
                "Custom Spell Slots",
                "Enter max slots per level (comma separated, e.g., 4,3,3,2):",
                maxLength: 50);

            if (!string.IsNullOrWhiteSpace(slots))
            {
                var slotArray = slots.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out var v) ? v : 0)
                    .ToArray();

                CurrentSpellTracker = await _spellSlotService.CreateCustomTrackerAsync(
                    SpellSlotTargetEntry.Id,
                    SpellSlotTargetEntry.Name,
                    slotArray);
            }
        }
        else
        {
            var levelStr = await _dialogService.DisplayPromptAsync(
                "Caster Level",
                $"Enter {selectedClass} level:",
                keyboard: Keyboard.Numeric);

            if (int.TryParse(levelStr, out var level) && level > 0 && level <= 20)
            {
                CurrentSpellTracker = await _spellSlotService.CreateTrackerAsync(
                    SpellSlotTargetEntry.Id,
                    SpellSlotTargetEntry.Name,
                    selectedClass,
                    level);
            }
        }
    }

    [RelayCommand]
    private async Task UseSpellSlotAsync(int level)
    {
        if (CurrentSpellTracker == null) return;

        await _spellSlotService.UseSpellSlotAsync(CurrentSpellTracker.Id, level);
        CurrentSpellTracker = await _spellSlotService.GetTrackerForCombatantAsync(CurrentSpellTracker.InitiativeEntryId);

        // Log the spell cast
        if (_combatService.ActiveCombat != null && SpellSlotTargetEntry != null)
        {
            await _combatLogService.LogSpellCastAsync(
                _combatService.ActiveCombat.Id,
                CurrentRound,
                SpellSlotTargetEntry.Name,
                $"Used level {level} slot");
        }
    }

    [RelayCommand]
    private async Task RestoreSpellSlotAsync(int level)
    {
        if (CurrentSpellTracker == null) return;

        await _spellSlotService.RestoreSpellSlotAsync(CurrentSpellTracker.Id, level);
        CurrentSpellTracker = await _spellSlotService.GetTrackerForCombatantAsync(CurrentSpellTracker.InitiativeEntryId);
    }

    [RelayCommand]
    private async Task ShortRestAsync()
    {
        if (CurrentSpellTracker == null) return;

        await _spellSlotService.ShortRestAsync(CurrentSpellTracker.Id);
        CurrentSpellTracker = await _spellSlotService.GetTrackerForCombatantAsync(CurrentSpellTracker.InitiativeEntryId);
        await _dialogService.DisplayAlertAsync("Short Rest", "Pact magic slots restored (if applicable).");
    }

    [RelayCommand]
    private async Task LongRestAsync()
    {
        if (CurrentSpellTracker == null) return;

        await _spellSlotService.LongRestAsync(CurrentSpellTracker.Id);
        CurrentSpellTracker = await _spellSlotService.GetTrackerForCombatantAsync(CurrentSpellTracker.InitiativeEntryId);
        await _dialogService.DisplayAlertAsync("Long Rest", "All spell slots restored.");
    }

    // Temp HP Commands
    [RelayCommand]
    private async Task AddTempHpAsync(InitiativeEntry entry)
    {
        var result = await _dialogService.DisplayPromptAsync(
            "Add Temp HP",
            $"Enter temporary hit points for {entry.Name}:",
            "Add",
            "Cancel",
            placeholder: "0",
            keyboard: Keyboard.Numeric);

        if (!string.IsNullOrEmpty(result) && int.TryParse(result, out int tempHp) && tempHp > 0)
        {
            await _combatService.AddTempHpAsync(entry, tempHp);
            await RefreshCombatantsAsync();
        }
    }

    [RelayCommand]
    private async Task RemoveTempHpAsync(InitiativeEntry entry)
    {
        await _combatService.RemoveTempHpAsync(entry);
        await RefreshCombatantsAsync();
    }

    // Concentration Commands
    [RelayCommand]
    private async Task StartConcentrationAsync(InitiativeEntry entry)
    {
        var result = await _dialogService.DisplayPromptAsync(
            "Concentration",
            $"What spell is {entry.Name} concentrating on?",
            "Start",
            "Cancel",
            placeholder: "Spell name");

        if (!string.IsNullOrEmpty(result))
        {
            await _combatService.StartConcentrationAsync(entry, result);
            await RefreshCombatantsAsync();
        }
    }

    [RelayCommand]
    private async Task EndConcentrationAsync(InitiativeEntry entry)
    {
        await _combatService.EndConcentrationAsync(entry);
        await RefreshCombatantsAsync();
    }

    [RelayCommand]
    private async Task ToggleConcentrationAsync(InitiativeEntry entry)
    {
        if (entry.IsConcentrating)
        {
            // End concentration
            await _combatService.EndConcentrationAsync(entry);
            await RefreshCombatantsAsync();
        }
        else
        {
            // Start concentration - prompt for spell name
            await StartConcentrationAsync(entry);
        }
    }

    // Death Save Commands
    [RelayCommand]
    private async Task AddDeathSaveSuccessAsync(InitiativeEntry entry)
    {
        await _combatService.AddDeathSaveSuccessAsync(entry);
        await RefreshCombatantsAsync();

        if (entry.DeathSaveSuccesses >= 3)
        {
            await _dialogService.DisplayAlertAsync(
                "Stabilized!",
                $"{entry.Name} has stabilized with 3 successful death saves.");
        }
    }

    [RelayCommand]
    private async Task AddDeathSaveFailureAsync(InitiativeEntry entry)
    {
        await _combatService.AddDeathSaveFailureAsync(entry);
        await RefreshCombatantsAsync();

        if (entry.DeathSaveFailures >= 3)
        {
            await _dialogService.DisplayAlertAsync(
                "Character Death",
                $"{entry.Name} has died with 3 failed death saves.");
        }
    }

    [RelayCommand]
    private async Task ResetDeathSavesAsync(InitiativeEntry entry)
    {
        await _combatService.ResetDeathSavesAsync(entry);
        await RefreshCombatantsAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe from events to prevent memory leaks
            _combatLogService.LogEntryAdded -= OnCombatLogEntryAdded;
        }

        base.Dispose(disposing);
    }
}
