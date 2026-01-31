using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Content;
using SirSquintsDndAssistant.Services.Database.Repositories;
using SirSquintsDndAssistant.Services.Images;
using SirSquintsDndAssistant.Services.Utilities;

namespace SirSquintsDndAssistant.ViewModels.Reference;

[QueryProperty(nameof(MagicItem), "MagicItem")]
public partial class MagicItemDetailViewModel : BaseViewModel
{
    private readonly IMagicItemRepository _magicItemRepository;
    private readonly ICommunityImageService _imageService;
    private readonly IImageService _localImageService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private MagicItem? magicItem;

    [ObservableProperty]
    private string rarityColor = "#808080";

    [ObservableProperty]
    private string attunementDisplay = string.Empty;

    [ObservableProperty]
    private bool hasDescription;

    [ObservableProperty]
    private ImageSource? itemImage;

    [ObservableProperty]
    private bool hasImage;

    [ObservableProperty]
    private bool isLoadingImage;

    public MagicItemDetailViewModel(
        IMagicItemRepository magicItemRepository,
        ICommunityImageService imageService,
        IImageService localImageService,
        IDialogService dialogService)
    {
        _magicItemRepository = magicItemRepository;
        _imageService = imageService;
        _localImageService = localImageService;
        _dialogService = dialogService;
        Title = "Magic Item Details";
    }

    partial void OnMagicItemChanged(MagicItem? value)
    {
        if (value != null)
        {
            Title = value.Name;
            SetRarityColor();
            SetAttunementDisplay();
            HasDescription = !string.IsNullOrEmpty(value.Description);
            _ = LoadImageAsync();
        }
    }

    private async Task LoadImageAsync()
    {
        if (MagicItem == null) return;

        IsLoadingImage = true;
        try
        {
            ItemImage = await _imageService.GetImageSourceAsync(MagicItem.ImageUrl, MagicItem.ImagePath);
            HasImage = ItemImage != null;
        }
        finally
        {
            IsLoadingImage = false;
        }
    }

    [RelayCommand]
    private async Task SearchCommunityImageAsync()
    {
        if (MagicItem == null) return;

        IsLoadingImage = true;
        try
        {
            var imageUrl = await _imageService.GetItemImageUrlAsync(MagicItem.Name, MagicItem.Type);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                var localPath = await _imageService.DownloadAndCacheImageAsync(imageUrl, "magicitem", MagicItem.Name);
                if (!string.IsNullOrEmpty(localPath))
                {
                    MagicItem.ImagePath = localPath;
                    MagicItem.ImageUrl = imageUrl;
                    await _magicItemRepository.SaveAsync(MagicItem);
                    await LoadImageAsync();
                    await _dialogService.DisplayAlertAsync("Image Found", $"Found and cached image for {MagicItem.Name}");
                }
                else
                {
                    MagicItem.ImageUrl = imageUrl;
                    await _magicItemRepository.SaveAsync(MagicItem);
                    await LoadImageAsync();
                }
            }
            else
            {
                await _dialogService.DisplayAlertAsync("No Image Found", $"Could not find a community image for {MagicItem.Name}. You can add a custom image instead.");
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
        if (MagicItem == null) return;

        var imagePath = await _localImageService.PickAndSaveImageAsync($"magicitem_{MagicItem.Id}");
        if (!string.IsNullOrEmpty(imagePath))
        {
            MagicItem.ImagePath = imagePath;
            await _magicItemRepository.SaveAsync(MagicItem);
            await LoadImageAsync();
        }
    }

    [RelayCommand]
    private async Task SetImageUrlAsync()
    {
        if (MagicItem == null) return;

        var url = await _dialogService.DisplayPromptAsync(
            "Set Image URL",
            "Enter the URL of the magic item image:",
            initialValue: MagicItem.ImageUrl);

        if (!string.IsNullOrWhiteSpace(url))
        {
            MagicItem.ImageUrl = url;
            await _magicItemRepository.SaveAsync(MagicItem);
            await LoadImageAsync();
        }
    }

    [RelayCommand]
    private async Task RemoveImageAsync()
    {
        if (MagicItem == null) return;

        var confirm = await _dialogService.DisplayConfirmAsync(
            "Remove Image",
            "Are you sure you want to remove the magic item image?");

        if (confirm)
        {
            if (!string.IsNullOrEmpty(MagicItem.ImagePath))
            {
                await _localImageService.DeleteImageAsync(MagicItem.ImagePath);
            }
            MagicItem.ImagePath = string.Empty;
            MagicItem.ImageUrl = string.Empty;
            await _magicItemRepository.SaveAsync(MagicItem);
            ItemImage = null;
            HasImage = false;
        }
    }

    private void SetRarityColor()
    {
        if (MagicItem == null) return;

        RarityColor = MagicItem.Rarity?.ToLower() switch
        {
            "common" => "#808080",
            "uncommon" => "#228B22",
            "rare" => "#4169E1",
            "very rare" => "#8B008B",
            "legendary" => "#FF8C00",
            "artifact" => "#8B0000",
            _ => "#808080"
        };
    }

    private void SetAttunementDisplay()
    {
        if (MagicItem == null) return;

        AttunementDisplay = MagicItem.RequiresAttunement
            ? "Requires Attunement"
            : "No Attunement Required";
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
