using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Homebrew;
using SirSquintsDndAssistant.Services.Homebrew;
using SirSquintsDndAssistant.Services.Utilities;

namespace SirSquintsDndAssistant.ViewModels.Homebrew;

public partial class HomebrewViewModel : ObservableObject
{
    private readonly IHomebrewService _homebrewService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<HomebrewMonster> _monsters = new();

    [ObservableProperty]
    private ObservableCollection<HomebrewSpell> _spells = new();

    [ObservableProperty]
    private ObservableCollection<HomebrewItem> _items = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _monsterCount;

    [ObservableProperty]
    private int _spellCount;

    [ObservableProperty]
    private int _itemCount;

    public HomebrewViewModel(IHomebrewService homebrewService, IDialogService dialogService)
    {
        _homebrewService = homebrewService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var monsters = await _homebrewService.GetAllMonstersAsync();
            var spells = await _homebrewService.GetAllSpellsAsync();
            var items = await _homebrewService.GetAllItemsAsync();

            Monsters = new ObservableCollection<HomebrewMonster>(monsters);
            Spells = new ObservableCollection<HomebrewSpell>(spells);
            Items = new ObservableCollection<HomebrewItem>(items);

            MonsterCount = monsters.Count;
            SpellCount = spells.Count;
            ItemCount = items.Count;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadDataAsync();
            return;
        }

        IsLoading = true;
        try
        {
            var monsters = await _homebrewService.SearchMonstersAsync(SearchText);
            var spells = await _homebrewService.SearchSpellsAsync(SearchText);
            var items = await _homebrewService.SearchItemsAsync(SearchText);

            Monsters = new ObservableCollection<HomebrewMonster>(monsters);
            Spells = new ObservableCollection<HomebrewSpell>(spells);
            Items = new ObservableCollection<HomebrewItem>(items);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateMonsterAsync()
    {
        var name = await _dialogService.DisplayPromptAsync("New Monster", "Enter monster name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        var monster = new HomebrewMonster { Name = name };
        await _homebrewService.SaveMonsterAsync(monster);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task CreateSpellAsync()
    {
        var name = await _dialogService.DisplayPromptAsync("New Spell", "Enter spell name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        var spell = new HomebrewSpell { Name = name };
        await _homebrewService.SaveSpellAsync(spell);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task CreateItemAsync()
    {
        var name = await _dialogService.DisplayPromptAsync("New Item", "Enter item name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        var item = new HomebrewItem { Name = name };
        await _homebrewService.SaveItemAsync(item);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task EditMonsterAsync(HomebrewMonster monster)
    {
        // Navigate to monster edit page with the monster ID as query parameter
        await Shell.Current.GoToAsync($"homebrewMonsterEdit?id={monster.Id}");
    }

    [RelayCommand]
    private async Task EditSpellAsync(HomebrewSpell spell)
    {
        // Navigate to spell edit page with the spell ID as query parameter
        await Shell.Current.GoToAsync($"homebrewSpellEdit?id={spell.Id}");
    }

    [RelayCommand]
    private async Task EditItemAsync(HomebrewItem item)
    {
        // Navigate to item edit page with the item ID as query parameter
        await Shell.Current.GoToAsync($"homebrewItemEdit?id={item.Id}");
    }

    [RelayCommand]
    private async Task DeleteMonsterAsync(HomebrewMonster monster)
    {
        var confirm = await _dialogService.DisplayConfirmAsync("Delete Monster",
            $"Are you sure you want to delete '{monster.Name}'?");
        if (!confirm) return;

        await _homebrewService.DeleteMonsterAsync(monster.Id);
        Monsters.Remove(monster);
        MonsterCount--;
    }

    [RelayCommand]
    private async Task DeleteSpellAsync(HomebrewSpell spell)
    {
        var confirm = await _dialogService.DisplayConfirmAsync("Delete Spell",
            $"Are you sure you want to delete '{spell.Name}'?");
        if (!confirm) return;

        await _homebrewService.DeleteSpellAsync(spell.Id);
        Spells.Remove(spell);
        SpellCount--;
    }

    [RelayCommand]
    private async Task DeleteItemAsync(HomebrewItem item)
    {
        var confirm = await _dialogService.DisplayConfirmAsync("Delete Item",
            $"Are you sure you want to delete '{item.Name}'?");
        if (!confirm) return;

        await _homebrewService.DeleteItemAsync(item.Id);
        Items.Remove(item);
        ItemCount--;
    }

    [RelayCommand]
    private async Task DuplicateMonsterAsync(HomebrewMonster monster)
    {
        await _homebrewService.DuplicateMonsterAsync(monster.Id);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DuplicateSpellAsync(HomebrewSpell spell)
    {
        await _homebrewService.DuplicateSpellAsync(spell.Id);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DuplicateItemAsync(HomebrewItem item)
    {
        await _homebrewService.DuplicateItemAsync(item.Id);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task ExportAllAsync()
    {
        try
        {
            var json = await _homebrewService.ExportAllToJsonAsync();
            await Clipboard.SetTextAsync(json);
            await _dialogService.DisplayAlertAsync("Export Complete",
                "All homebrew content has been copied to clipboard as JSON.");
        }
        catch (Exception ex)
        {
            await _dialogService.DisplayAlertAsync("Export Failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var json = await _dialogService.DisplayPromptAsync("Import Homebrew",
            "Paste JSON content:", maxLength: 100000);
        if (string.IsNullOrWhiteSpace(json)) return;

        try
        {
            await _homebrewService.ImportFromJsonAsync(json);
            await LoadDataAsync();
            await _dialogService.DisplayAlertAsync("Import Complete", "Homebrew content imported successfully.");
        }
        catch (Exception ex)
        {
            await _dialogService.DisplayAlertAsync("Import Failed", ex.Message);
        }
    }
}
