using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Extensions;
using SirSquintsDndAssistant.Models.Content;
using SirSquintsDndAssistant.Services.Database.Repositories;
using System.Collections.ObjectModel;

namespace SirSquintsDndAssistant.ViewModels.Reference;

public partial class ItemDatabaseViewModel : BaseViewModel
{
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IMagicItemRepository _magicItemRepo;
    private readonly DebounceHelper _searchDebounce = new();
    private const int PageSize = 50;

    [ObservableProperty]
    private ObservableCollection<Equipment> equipment = new();

    [ObservableProperty]
    private ObservableCollection<MagicItem> magicItems = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool showEquipment = true;

    [ObservableProperty]
    private bool showMagicItems = true;

    [ObservableProperty]
    private int equipmentPage = 1;

    [ObservableProperty]
    private int magicItemPage = 1;

    [ObservableProperty]
    private bool hasMoreEquipment;

    [ObservableProperty]
    private bool hasMoreMagicItems;

    [ObservableProperty]
    private bool isLoadingMore;

    [ObservableProperty]
    private int totalEquipmentCount;

    [ObservableProperty]
    private int totalMagicItemCount;

    public ItemDatabaseViewModel(IEquipmentRepository equipmentRepo, IMagicItemRepository magicItemRepo)
    {
        _equipmentRepo = equipmentRepo;
        _magicItemRepo = magicItemRepo;
        Title = "Items";
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            EquipmentPage = 1;
            MagicItemPage = 1;

            await LoadPageAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadPageAsync()
    {
        PagedResult<Equipment> eqResult;
        PagedResult<MagicItem> miResult;

        try
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                eqResult = await _equipmentRepo.GetPagedAsync(EquipmentPage, PageSize);
                miResult = await _magicItemRepo.GetPagedAsync(MagicItemPage, PageSize);
            }
            else
            {
                eqResult = await _equipmentRepo.SearchPagedAsync(SearchText, EquipmentPage, PageSize);
                miResult = await _magicItemRepo.SearchPagedAsync(SearchText, MagicItemPage, PageSize);
            }

            System.Diagnostics.Debug.WriteLine($"ItemDatabase: Equipment loaded {eqResult.Items.Count} items, total {eqResult.TotalCount}");
            System.Diagnostics.Debug.WriteLine($"ItemDatabase: Magic Items loaded {miResult.Items.Count} items, total {miResult.TotalCount}");

            Equipment.Clear();
            MagicItems.Clear();

            foreach (var item in eqResult.Items)
                Equipment.Add(item);

            foreach (var item in miResult.Items)
                MagicItems.Add(item);

            TotalEquipmentCount = eqResult.TotalCount;
            TotalMagicItemCount = miResult.TotalCount;
            HasMoreEquipment = eqResult.HasNextPage;
            HasMoreMagicItems = miResult.HasNextPage;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ItemDatabase ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ItemDatabase STACK: {ex.StackTrace}");
        }
    }

    [RelayCommand]
    private async Task LoadMoreEquipmentAsync()
    {
        if (!HasMoreEquipment || IsLoadingMore) return;

        try
        {
            IsLoadingMore = true;
            EquipmentPage++;

            PagedResult<Equipment> result;
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                result = await _equipmentRepo.GetPagedAsync(EquipmentPage, PageSize);
            }
            else
            {
                result = await _equipmentRepo.SearchPagedAsync(SearchText, EquipmentPage, PageSize);
            }

            foreach (var item in result.Items)
            {
                Equipment.Add(item);
            }

            HasMoreEquipment = result.HasNextPage;
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    [RelayCommand]
    private async Task LoadMoreMagicItemsAsync()
    {
        if (!HasMoreMagicItems || IsLoadingMore) return;

        try
        {
            IsLoadingMore = true;
            MagicItemPage++;

            PagedResult<MagicItem> result;
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                result = await _magicItemRepo.GetPagedAsync(MagicItemPage, PageSize);
            }
            else
            {
                result = await _magicItemRepo.SearchPagedAsync(SearchText, MagicItemPage, PageSize);
            }

            foreach (var item in result.Items)
            {
                MagicItems.Add(item);
            }

            HasMoreMagicItems = result.HasNextPage;
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        // Debounce search to avoid excessive queries while typing
        _searchDebounce.DebounceAsync(LoadItemsAsync);
    }

    [RelayCommand]
    private async Task ViewEquipmentAsync(Equipment item)
    {
        if (item == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "Equipment", item }
        };

        await Shell.Current.GoToAsync("equipmentdetail", navigationParameter);
    }

    [RelayCommand]
    private async Task ViewMagicItemAsync(MagicItem item)
    {
        if (item == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "MagicItem", item }
        };

        await Shell.Current.GoToAsync("magicitemdetail", navigationParameter);
    }
}
