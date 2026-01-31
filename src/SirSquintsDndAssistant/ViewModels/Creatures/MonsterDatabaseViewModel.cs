using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Extensions;
using SirSquintsDndAssistant.Models.Creatures;
using SirSquintsDndAssistant.Services.Database.Repositories;
using System.Collections.ObjectModel;

namespace SirSquintsDndAssistant.ViewModels.Creatures;

public partial class MonsterDatabaseViewModel : BaseViewModel
{
    private readonly IMonsterRepository _monsterRepository;
    private readonly DebounceHelper _searchDebounce = new();
    private const int PageSize = 50;

    [ObservableProperty]
    private ObservableCollection<Monster> monsters = new();

    [ObservableProperty]
    private Monster? selectedMonster;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedCrFilter = "All";

    [ObservableProperty]
    private string selectedTypeFilter = "All";

    [ObservableProperty]
    private bool showFavoritesOnly;

    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private int totalPages = 1;

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private bool hasNextPage;

    [ObservableProperty]
    private bool hasPreviousPage;

    [ObservableProperty]
    private bool isLoadingMore;

    public ObservableCollection<string> CrFilters { get; } = new()
    {
        "All", "0-1", "2-5", "6-10", "11-15", "16-20", "20+"
    };

    public ObservableCollection<string> TypeFilters { get; } = new()
    {
        "All", "Aberration", "Beast", "Celestial", "Construct", "Dragon",
        "Elemental", "Fey", "Fiend", "Giant", "Humanoid", "Monstrosity",
        "Ooze", "Plant", "Undead"
    };

    public MonsterDatabaseViewModel(IMonsterRepository monsterRepository)
    {
        _monsterRepository = monsterRepository;
        Title = "Monster Database";
    }

    [RelayCommand]
    private async Task LoadMonstersAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            CurrentPage = 1;
            await LoadPageAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading monsters: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadPageAsync()
    {
        PagedResult<Monster> result;

        // Apply filters using appropriate repository method
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            result = await _monsterRepository.SearchPagedAsync(SearchText, CurrentPage, PageSize);
        }
        else if (SelectedCrFilter != "All")
        {
            var (minCR, maxCR) = GetCrRange(SelectedCrFilter);
            result = await _monsterRepository.GetByChallengeRatingPagedAsync(minCR, maxCR, CurrentPage, PageSize);
        }
        else if (SelectedTypeFilter != "All")
        {
            result = await _monsterRepository.GetByTypePagedAsync(SelectedTypeFilter, CurrentPage, PageSize);
        }
        else
        {
            result = await _monsterRepository.GetPagedAsync(CurrentPage, PageSize);
        }

        // Apply client-side filters that aren't handled by repository
        var filteredItems = result.Items.AsEnumerable();

        if (ShowFavoritesOnly)
        {
            filteredItems = filteredItems.Where(m => m.IsFavorite);
        }

        // Update UI
        Monsters.Clear();
        foreach (var monster in filteredItems)
        {
            Monsters.Add(monster);
        }

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
        HasNextPage = result.HasNextPage;
        HasPreviousPage = result.HasPreviousPage;
    }

    private (double minCR, double maxCR) GetCrRange(string filter)
    {
        return filter switch
        {
            "0-1" => (0, 1),
            "2-5" => (2, 5),
            "6-10" => (6, 10),
            "11-15" => (11, 15),
            "16-20" => (16, 20),
            "20+" => (20, 30),
            _ => (0, 30)
        };
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (!HasNextPage || IsLoadingMore) return;

        try
        {
            IsLoadingMore = true;
            CurrentPage++;
            await LoadPageAsync();
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (!HasPreviousPage || IsLoadingMore) return;

        try
        {
            IsLoadingMore = true;
            CurrentPage--;
            await LoadPageAsync();
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        // For infinite scroll: append next page to current list
        if (!HasNextPage || IsLoadingMore) return;

        try
        {
            IsLoadingMore = true;
            CurrentPage++;

            PagedResult<Monster> result;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                result = await _monsterRepository.SearchPagedAsync(SearchText, CurrentPage, PageSize);
            }
            else if (SelectedCrFilter != "All")
            {
                var (minCR, maxCR) = GetCrRange(SelectedCrFilter);
                result = await _monsterRepository.GetByChallengeRatingPagedAsync(minCR, maxCR, CurrentPage, PageSize);
            }
            else if (SelectedTypeFilter != "All")
            {
                result = await _monsterRepository.GetByTypePagedAsync(SelectedTypeFilter, CurrentPage, PageSize);
            }
            else
            {
                result = await _monsterRepository.GetPagedAsync(CurrentPage, PageSize);
            }

            var filteredItems = result.Items.AsEnumerable();
            if (ShowFavoritesOnly)
            {
                filteredItems = filteredItems.Where(m => m.IsFavorite);
            }

            foreach (var monster in filteredItems)
            {
                Monsters.Add(monster);
            }

            HasNextPage = result.HasNextPage;
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadPageAsync();
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        CurrentPage = 1;
        await LoadPageAsync();
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(Monster monster)
    {
        if (monster == null) return;

        monster.IsFavorite = !monster.IsFavorite;
        await _monsterRepository.SaveAsync(monster);

        // Refresh if showing favorites only
        if (ShowFavoritesOnly)
        {
            await ApplyFiltersAsync();
        }
    }

    [RelayCommand]
    private async Task MonsterSelectedAsync(Monster? monster)
    {
        if (monster == null) return;

        // Navigate to detail page
        var navigationParameter = new Dictionary<string, object>
        {
            { "Monster", monster }
        };

        await Shell.Current.GoToAsync("monsterdetail", navigationParameter);

        // Clear selection
        SelectedMonster = null;
    }

    partial void OnSearchTextChanged(string value)
    {
        // Debounce search to avoid excessive queries while typing
        _searchDebounce.DebounceAsync(SearchAsync);
    }

    partial void OnSelectedCrFilterChanged(string value)
    {
        ApplyFiltersAsync().SafeFireAndForget();
    }

    partial void OnSelectedTypeFilterChanged(string value)
    {
        ApplyFiltersAsync().SafeFireAndForget();
    }

    partial void OnShowFavoritesOnlyChanged(bool value)
    {
        ApplyFiltersAsync().SafeFireAndForget();
    }
}
