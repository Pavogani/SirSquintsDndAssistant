using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Content;
using SirSquintsDndAssistant.Services.Database.Repositories;
using SirSquintsDndAssistant.Services.Images;
using SirSquintsDndAssistant.Services.Utilities;
using System.Text.Json;

namespace SirSquintsDndAssistant.ViewModels.Reference;

[QueryProperty(nameof(Equipment), "Equipment")]
public partial class EquipmentDetailViewModel : BaseViewModel
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly ICommunityImageService _imageService;
    private readonly IImageService _localImageService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private Equipment? equipment;

    [ObservableProperty]
    private string costDisplay = string.Empty;

    [ObservableProperty]
    private string categoryDisplay = string.Empty;

    [ObservableProperty]
    private string propertiesDisplay = string.Empty;

    [ObservableProperty]
    private string descriptionDisplay = string.Empty;

    [ObservableProperty]
    private bool hasProperties;

    [ObservableProperty]
    private bool hasDescription;

    [ObservableProperty]
    private bool isWeapon;

    [ObservableProperty]
    private bool isArmor;

    [ObservableProperty]
    private ImageSource? equipmentImage;

    [ObservableProperty]
    private bool hasImage;

    [ObservableProperty]
    private bool isLoadingImage;

    public EquipmentDetailViewModel(
        IEquipmentRepository equipmentRepository,
        ICommunityImageService imageService,
        IImageService localImageService,
        IDialogService dialogService)
    {
        _equipmentRepository = equipmentRepository;
        _imageService = imageService;
        _localImageService = localImageService;
        _dialogService = dialogService;
        Title = "Equipment Details";
    }

    partial void OnEquipmentChanged(Equipment? value)
    {
        if (value != null)
        {
            Title = value.Name;
            FormatCostDisplay();
            FormatCategoryDisplay();
            ParseProperties();
            ParseDescription();
            _ = LoadImageAsync();
        }
    }

    private async Task LoadImageAsync()
    {
        if (Equipment == null) return;

        IsLoadingImage = true;
        try
        {
            EquipmentImage = await _imageService.GetImageSourceAsync(Equipment.ImageUrl, Equipment.ImagePath);
            HasImage = EquipmentImage != null;
        }
        finally
        {
            IsLoadingImage = false;
        }
    }

    [RelayCommand]
    private async Task SearchCommunityImageAsync()
    {
        if (Equipment == null) return;

        IsLoadingImage = true;
        try
        {
            var imageUrl = await _imageService.GetItemImageUrlAsync(Equipment.Name, Equipment.EquipmentCategory);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                var localPath = await _imageService.DownloadAndCacheImageAsync(imageUrl, "equipment", Equipment.Name);
                if (!string.IsNullOrEmpty(localPath))
                {
                    Equipment.ImagePath = localPath;
                    Equipment.ImageUrl = imageUrl;
                    await _equipmentRepository.SaveAsync(Equipment);
                    await LoadImageAsync();
                    await _dialogService.DisplayAlertAsync("Image Found", $"Found and cached image for {Equipment.Name}");
                }
                else
                {
                    Equipment.ImageUrl = imageUrl;
                    await _equipmentRepository.SaveAsync(Equipment);
                    await LoadImageAsync();
                }
            }
            else
            {
                await _dialogService.DisplayAlertAsync("No Image Found", $"Could not find a community image for {Equipment.Name}. You can add a custom image instead.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error searching for image: {ex.Message}");
            await _dialogService.DisplayAlertAsync("Error", "Failed to search for image. Please try again.");
        }
        finally
        {
            IsLoadingImage = false;
        }
    }

    [RelayCommand]
    private async Task PickCustomImageAsync()
    {
        if (Equipment == null) return;

        var imagePath = await _localImageService.PickAndSaveImageAsync($"equipment_{Equipment.Id}");
        if (!string.IsNullOrEmpty(imagePath))
        {
            Equipment.ImagePath = imagePath;
            await _equipmentRepository.SaveAsync(Equipment);
            await LoadImageAsync();
        }
    }

    [RelayCommand]
    private async Task SetImageUrlAsync()
    {
        if (Equipment == null) return;

        var url = await _dialogService.DisplayPromptAsync(
            "Set Image URL",
            "Enter the URL of the equipment image:",
            initialValue: Equipment.ImageUrl);

        if (!string.IsNullOrWhiteSpace(url))
        {
            Equipment.ImageUrl = url;
            await _equipmentRepository.SaveAsync(Equipment);
            await LoadImageAsync();
        }
    }

    [RelayCommand]
    private async Task RemoveImageAsync()
    {
        if (Equipment == null) return;

        var confirm = await _dialogService.DisplayConfirmAsync(
            "Remove Image",
            "Are you sure you want to remove the equipment image?");

        if (confirm)
        {
            if (!string.IsNullOrEmpty(Equipment.ImagePath))
            {
                await _localImageService.DeleteImageAsync(Equipment.ImagePath);
            }
            Equipment.ImagePath = string.Empty;
            Equipment.ImageUrl = string.Empty;
            await _equipmentRepository.SaveAsync(Equipment);
            EquipmentImage = null;
            HasImage = false;
        }
    }

    private void FormatCostDisplay()
    {
        if (Equipment == null) return;

        if (Equipment.Cost > 0 && !string.IsNullOrEmpty(Equipment.CostCurrency))
        {
            CostDisplay = $"{Equipment.Cost} {Equipment.CostCurrency}";
        }
        else
        {
            CostDisplay = "Unknown";
        }
    }

    private void FormatCategoryDisplay()
    {
        if (Equipment == null) return;

        var categories = new List<string>();

        if (!string.IsNullOrEmpty(Equipment.EquipmentCategory))
            categories.Add(Equipment.EquipmentCategory);

        if (!string.IsNullOrEmpty(Equipment.WeaponCategory))
        {
            categories.Add(Equipment.WeaponCategory);
            IsWeapon = true;
        }

        if (!string.IsNullOrEmpty(Equipment.WeaponRange))
            categories.Add(Equipment.WeaponRange);

        if (!string.IsNullOrEmpty(Equipment.ArmorCategory))
        {
            categories.Add($"{Equipment.ArmorCategory} Armor");
            IsArmor = true;
        }

        CategoryDisplay = categories.Count > 0 ? string.Join(" - ", categories) : "General";
    }

    private void ParseProperties()
    {
        HasProperties = false;
        PropertiesDisplay = string.Empty;

        if (Equipment == null || string.IsNullOrEmpty(Equipment.PropertiesJson))
            return;

        try
        {
            var properties = JsonSerializer.Deserialize<List<string>>(Equipment.PropertiesJson);
            if (properties != null && properties.Count > 0)
            {
                PropertiesDisplay = string.Join(", ", properties);
                HasProperties = true;
            }
        }
        catch (JsonException)
        {
            // Try parsing as list of objects with Name property
            try
            {
                var propObjects = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(Equipment.PropertiesJson);
                if (propObjects != null && propObjects.Count > 0)
                {
                    var names = propObjects
                        .Where(p => p.ContainsKey("name"))
                        .Select(p => p["name"]?.ToString() ?? "")
                        .Where(n => !string.IsNullOrEmpty(n));
                    PropertiesDisplay = string.Join(", ", names);
                    HasProperties = true;
                }
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing properties JSON: {ex.Message}");
            }
        }
    }

    private void ParseDescription()
    {
        HasDescription = false;
        DescriptionDisplay = string.Empty;

        if (Equipment == null || string.IsNullOrEmpty(Equipment.DescriptionJson))
            return;

        try
        {
            var descriptions = JsonSerializer.Deserialize<List<string>>(Equipment.DescriptionJson);
            if (descriptions != null && descriptions.Count > 0)
            {
                DescriptionDisplay = string.Join("\n\n", descriptions);
                HasDescription = true;
            }
        }
        catch (JsonException)
        {
            // Maybe it's a plain string - use as-is
            DescriptionDisplay = Equipment.DescriptionJson;
            HasDescription = !string.IsNullOrEmpty(DescriptionDisplay);
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
