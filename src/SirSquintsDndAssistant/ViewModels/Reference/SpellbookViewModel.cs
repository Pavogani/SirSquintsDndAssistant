using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Extensions;
using SirSquintsDndAssistant.Models.Content;
using SirSquintsDndAssistant.Services.Database.Repositories;
using System.Collections.ObjectModel;

namespace SirSquintsDndAssistant.ViewModels.Reference;

public partial class SpellbookViewModel : BaseViewModel
{
    private readonly ISpellRepository _spellRepository;
    private readonly DebounceHelper _searchDebounce = new();
    private const int PageSize = 50;

    [ObservableProperty]
    private ObservableCollection<Spell> spells = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int selectedLevel = -1;

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

    public ObservableCollection<string> SpellLevels { get; } = new()
    {
        "All", "Cantrip", "1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th", "9th"
    };

    public SpellbookViewModel(ISpellRepository spellRepository)
    {
        _spellRepository = spellRepository;
        Title = "Spellbook";
    }

    [RelayCommand]
    private async Task LoadSpellsAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            CurrentPage = 1;
            await LoadPageAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadPageAsync()
    {
        PagedResult<Spell> result;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            result = await _spellRepository.SearchPagedAsync(SearchText, CurrentPage, PageSize);
        }
        else if (SelectedLevel >= 0)
        {
            result = await _spellRepository.GetByLevelPagedAsync(SelectedLevel, CurrentPage, PageSize);
        }
        else
        {
            result = await _spellRepository.GetPagedAsync(CurrentPage, PageSize);
        }

        Spells.Clear();
        foreach (var spell in result.Items)
        {
            Spells.Add(spell);
        }

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
        HasNextPage = result.HasNextPage;
        HasPreviousPage = result.HasPreviousPage;
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
        if (!HasNextPage || IsLoadingMore) return;

        try
        {
            IsLoadingMore = true;
            CurrentPage++;

            PagedResult<Spell> result;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                result = await _spellRepository.SearchPagedAsync(SearchText, CurrentPage, PageSize);
            }
            else if (SelectedLevel >= 0)
            {
                result = await _spellRepository.GetByLevelPagedAsync(SelectedLevel, CurrentPage, PageSize);
            }
            else
            {
                result = await _spellRepository.GetPagedAsync(CurrentPage, PageSize);
            }

            foreach (var spell in result.Items)
            {
                Spells.Add(spell);
            }

            HasNextPage = result.HasNextPage;
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        CurrentPage = 1;
        await LoadPageAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        // Debounce search to avoid excessive queries while typing
        _searchDebounce.DebounceAsync(ApplyFiltersAsync);
    }

    partial void OnSelectedLevelChanged(int value)
    {
        ApplyFiltersAsync().SafeFireAndForget();
    }

    [RelayCommand]
    private async Task ViewSpellAsync(Spell spell)
    {
        if (spell == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "Spell", spell }
        };

        await Shell.Current.GoToAsync("spelldetail", navigationParameter);
    }
}
