using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Homebrew;
using SirSquintsDndAssistant.Services.Homebrew;
using SirSquintsDndAssistant.Services.Utilities;

namespace SirSquintsDndAssistant.ViewModels.Homebrew;

[QueryProperty(nameof(ItemId), "id")]
public partial class HomebrewItemEditViewModel : ObservableObject
{
    private readonly IHomebrewService _homebrewService;
    private readonly IDialogService _dialogService;
    private int _itemId;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isNewItem;

    // Basic Info
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ItemType _itemType;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private bool _isMagic;

    [ObservableProperty]
    private string _rarity = "Common";

    [ObservableProperty]
    private bool _requiresAttunement;

    [ObservableProperty]
    private string _attunementRequirement = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _weight = string.Empty;

    [ObservableProperty]
    private string _cost = string.Empty;

    // Weapon properties
    [ObservableProperty]
    private bool _isWeapon;

    [ObservableProperty]
    private string _weaponType = string.Empty;

    [ObservableProperty]
    private string _damageDice = string.Empty;

    [ObservableProperty]
    private string _damageType = string.Empty;

    [ObservableProperty]
    private string _range = string.Empty;

    // Armor properties
    [ObservableProperty]
    private bool _isArmor;

    [ObservableProperty]
    private string _armorType = string.Empty;

    [ObservableProperty]
    private int _baseAC;

    [ObservableProperty]
    private bool _addDexModifier;

    [ObservableProperty]
    private int _maxDexModifier;

    // Magic bonuses
    [ObservableProperty]
    private int _bonusToAttack;

    [ObservableProperty]
    private int _bonusToAC;

    [ObservableProperty]
    private int _bonusToDamage;

    // Charges
    [ObservableProperty]
    private bool _hasCharges;

    [ObservableProperty]
    private int _maxCharges;

    [ObservableProperty]
    private string _rechargeRate = string.Empty;

    // Cursed
    [ObservableProperty]
    private bool _isCursed;

    [ObservableProperty]
    private string _curseDescription = string.Empty;

    [ObservableProperty]
    private bool _isConsumable;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _tags = string.Empty;

    public List<ItemType> ItemTypes { get; } = Enum.GetValues<ItemType>().ToList();
    public List<string> Rarities { get; } = new() { "Common", "Uncommon", "Rare", "Very Rare", "Legendary", "Artifact" };
    public List<string> WeaponTypes { get; } = new() { "Simple Melee", "Simple Ranged", "Martial Melee", "Martial Ranged" };
    public List<string> ArmorTypes { get; } = new() { "Light", "Medium", "Heavy", "Shield" };
    public List<string> DamageTypes { get; } = new() { "Bludgeoning", "Piercing", "Slashing", "Acid", "Cold", "Fire", "Force", "Lightning", "Necrotic", "Poison", "Psychic", "Radiant", "Thunder" };

    public string ItemId
    {
        set
        {
            if (int.TryParse(value, out var id))
            {
                _itemId = id;
                IsNewItem = false;
                LoadItemAsync().ConfigureAwait(false);
            }
            else
            {
                IsNewItem = true;
            }
        }
    }

    public HomebrewItemEditViewModel(IHomebrewService homebrewService, IDialogService dialogService)
    {
        _homebrewService = homebrewService;
        _dialogService = dialogService;
    }

    private async Task LoadItemAsync()
    {
        if (_itemId <= 0) return;

        IsLoading = true;
        try
        {
            var item = await _homebrewService.GetItemAsync(_itemId);
            if (item != null)
            {
                Name = item.Name;
                ItemType = item.ItemType;
                Category = item.Category;
                IsMagic = item.IsMagic;
                Rarity = item.Rarity;
                RequiresAttunement = item.RequiresAttunement;
                AttunementRequirement = item.AttunementRequirement;
                Description = item.Description;
                Weight = item.Weight;
                Cost = item.Cost;
                IsWeapon = item.IsWeapon;
                WeaponType = item.WeaponType;
                DamageDice = item.DamageDice;
                DamageType = item.DamageType;
                Range = item.Range;
                IsArmor = item.IsArmor;
                ArmorType = item.ArmorType;
                BaseAC = item.BaseAC;
                AddDexModifier = item.AddDexModifier;
                MaxDexModifier = item.MaxDexModifier;
                BonusToAttack = item.BonusToAttack;
                BonusToAC = item.BonusToAC;
                BonusToDamage = item.BonusToDamage;
                HasCharges = item.HasCharges;
                MaxCharges = item.MaxCharges;
                RechargeRate = item.RechargeRate;
                IsCursed = item.IsCursed;
                CurseDescription = item.CurseDescription;
                IsConsumable = item.IsConsumable;
                Notes = item.Notes;
                Tags = item.Tags;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await _dialogService.DisplayAlertAsync("Validation Error", "Item name is required.");
            return;
        }

        IsLoading = true;
        try
        {
            var item = new HomebrewItem
            {
                Id = IsNewItem ? 0 : _itemId,
                Name = Name,
                ItemType = ItemType,
                Category = Category,
                IsMagic = IsMagic,
                Rarity = Rarity,
                RequiresAttunement = RequiresAttunement,
                AttunementRequirement = AttunementRequirement,
                Description = Description,
                Weight = Weight,
                Cost = Cost,
                IsWeapon = IsWeapon,
                WeaponType = WeaponType,
                DamageDice = DamageDice,
                DamageType = DamageType,
                Range = Range,
                IsArmor = IsArmor,
                ArmorType = ArmorType,
                BaseAC = BaseAC,
                AddDexModifier = AddDexModifier,
                MaxDexModifier = MaxDexModifier,
                BonusToAttack = BonusToAttack,
                BonusToAC = BonusToAC,
                BonusToDamage = BonusToDamage,
                HasCharges = HasCharges,
                MaxCharges = MaxCharges,
                RechargeRate = RechargeRate,
                IsCursed = IsCursed,
                CurseDescription = CurseDescription,
                IsConsumable = IsConsumable,
                Notes = Notes,
                Tags = Tags,
                UpdatedAt = DateTime.Now
            };

            if (IsNewItem)
            {
                item.CreatedAt = DateTime.Now;
            }

            await _homebrewService.SaveItemAsync(item);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await _dialogService.DisplayAlertAsync("Save Failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
