using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Services.Reference;
using System.Collections.ObjectModel;

namespace SirSquintsDndAssistant.ViewModels.Reference;

public partial class RulesReferenceViewModel : BaseViewModel
{
    private readonly IRulesReferenceService _rulesService;

    [ObservableProperty]
    private ObservableCollection<RuleCategory> categories = new();

    [ObservableProperty]
    private ObservableCollection<QuickRule> searchResults = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isSearching;

    [ObservableProperty]
    private string selectedCategory = "All";

    public List<string> CategoryOptions { get; } = new()
    {
        "All", "Ability Checks", "Combat", "Actions", "Damage", "Death",
        "Spellcasting", "Movement", "Resting"
    };

    public RulesReferenceViewModel(IRulesReferenceService rulesService)
    {
        _rulesService = rulesService;
        Title = "Rules Reference";
    }

    [RelayCommand]
    private void LoadRules()
    {
        var allCategories = _rulesService.GetAllCategories();
        Categories.Clear();
        foreach (var category in allCategories)
        {
            Categories.Add(category);
        }
    }

    [RelayCommand]
    private void Search()
    {
        IsSearching = true;
        var results = _rulesService.SearchRules(SearchText);

        if (SelectedCategory != "All")
        {
            results = results.Where(r => r.Category == SelectedCategory).ToList();
        }

        SearchResults.Clear();
        foreach (var rule in results)
        {
            SearchResults.Add(rule);
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        IsSearching = false;
        SearchResults.Clear();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            Search();
        }
        else
        {
            IsSearching = false;
            SearchResults.Clear();
        }
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        // Always filter when category changes
        if (value != "All")
        {
            // Show all rules in the selected category
            var results = _rulesService.SearchRules(SearchText ?? string.Empty);
            results = results.Where(r => r.Category == value).ToList();

            SearchResults.Clear();
            foreach (var rule in results)
            {
                SearchResults.Add(rule);
            }
            IsSearching = true;
        }
        else if (!string.IsNullOrEmpty(SearchText))
        {
            Search();
        }
        else
        {
            IsSearching = false;
            SearchResults.Clear();
        }
    }
}
