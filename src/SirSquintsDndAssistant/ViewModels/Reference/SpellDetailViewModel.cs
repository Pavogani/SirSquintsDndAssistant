using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Content;
using SirSquintsDndAssistant.Services.Database.Repositories;
using SirSquintsDndAssistant.Services.Images;
using SirSquintsDndAssistant.Services.Utilities;
using System.Text.Json;

namespace SirSquintsDndAssistant.ViewModels.Reference;

[QueryProperty(nameof(Spell), "Spell")]
public partial class SpellDetailViewModel : BaseViewModel
{
    private readonly ISpellRepository _spellRepository;
    private readonly ICommunityImageService _imageService;
    private readonly IImageService _localImageService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private Spell? spell;

    [ObservableProperty]
    private string levelDisplay = string.Empty;

    [ObservableProperty]
    private string classesDisplay = string.Empty;

    [ObservableProperty]
    private bool hasHigherLevels;

    [ObservableProperty]
    private ImageSource? spellImage;

    [ObservableProperty]
    private bool hasImage;

    [ObservableProperty]
    private bool isLoadingImage;

    public SpellDetailViewModel(
        ISpellRepository spellRepository,
        ICommunityImageService imageService,
        IImageService localImageService,
        IDialogService dialogService)
    {
        _spellRepository = spellRepository;
        _imageService = imageService;
        _localImageService = localImageService;
        _dialogService = dialogService;
        Title = "Spell Details";
    }

    partial void OnSpellChanged(Spell? value)
    {
        if (value != null)
        {
            Title = value.Name;
            FormatLevelDisplay();
            FormatClassesDisplay();
            HasHigherLevels = !string.IsNullOrEmpty(value.HigherLevels);
            _ = LoadImageAsync();
        }
    }

    private async Task LoadImageAsync()
    {
        if (Spell == null) return;

        IsLoadingImage = true;
        try
        {
            SpellImage = await _imageService.GetImageSourceAsync(Spell.ImageUrl, Spell.ImagePath);
            HasImage = SpellImage != null;
        }
        finally
        {
            IsLoadingImage = false;
        }
    }

    [RelayCommand]
    private async Task SearchCommunityImageAsync()
    {
        if (Spell == null) return;

        IsLoadingImage = true;
        try
        {
            var imageUrl = await _imageService.GetSpellImageUrlAsync(Spell.Name, Spell.School);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                var localPath = await _imageService.DownloadAndCacheImageAsync(imageUrl, "spell", Spell.Name);
                if (!string.IsNullOrEmpty(localPath))
                {
                    Spell.ImagePath = localPath;
                    Spell.ImageUrl = imageUrl;
                    await _spellRepository.SaveAsync(Spell);
                    await LoadImageAsync();
                    await _dialogService.DisplayAlertAsync("Image Found", $"Found and cached image for {Spell.Name}");
                }
                else
                {
                    Spell.ImageUrl = imageUrl;
                    await _spellRepository.SaveAsync(Spell);
                    await LoadImageAsync();
                }
            }
            else
            {
                await _dialogService.DisplayAlertAsync("No Image Found", $"Could not find a community image for {Spell.Name}. You can add a custom image instead.");
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
        if (Spell == null) return;

        var imagePath = await _localImageService.PickAndSaveImageAsync($"spell_{Spell.Id}");
        if (!string.IsNullOrEmpty(imagePath))
        {
            Spell.ImagePath = imagePath;
            await _spellRepository.SaveAsync(Spell);
            await LoadImageAsync();
        }
    }

    [RelayCommand]
    private async Task SetImageUrlAsync()
    {
        if (Spell == null) return;

        var url = await _dialogService.DisplayPromptAsync(
            "Set Image URL",
            "Enter the URL of the spell image:",
            initialValue: Spell.ImageUrl);

        if (!string.IsNullOrWhiteSpace(url))
        {
            Spell.ImageUrl = url;
            await _spellRepository.SaveAsync(Spell);
            await LoadImageAsync();
        }
    }

    [RelayCommand]
    private async Task RemoveImageAsync()
    {
        if (Spell == null) return;

        var confirm = await _dialogService.DisplayConfirmAsync(
            "Remove Image",
            "Are you sure you want to remove the spell image?");

        if (confirm)
        {
            if (!string.IsNullOrEmpty(Spell.ImagePath))
            {
                await _localImageService.DeleteImageAsync(Spell.ImagePath);
            }
            Spell.ImagePath = string.Empty;
            Spell.ImageUrl = string.Empty;
            await _spellRepository.SaveAsync(Spell);
            SpellImage = null;
            HasImage = false;
        }
    }

    private void FormatLevelDisplay()
    {
        if (Spell == null) return;

        LevelDisplay = Spell.Level switch
        {
            0 => $"{Spell.School} cantrip",
            1 => $"1st-level {Spell.School.ToLower()}",
            2 => $"2nd-level {Spell.School.ToLower()}",
            3 => $"3rd-level {Spell.School.ToLower()}",
            _ => $"{Spell.Level}th-level {Spell.School.ToLower()}"
        };
    }

    private void FormatClassesDisplay()
    {
        if (Spell == null || string.IsNullOrEmpty(Spell.ClassesJson))
        {
            ClassesDisplay = "Unknown";
            return;
        }

        try
        {
            var classes = JsonSerializer.Deserialize<List<string>>(Spell.ClassesJson);
            if (classes != null && classes.Count > 0)
            {
                ClassesDisplay = string.Join(", ", classes);
            }
            else
            {
                ClassesDisplay = "Unknown";
            }
        }
        catch
        {
            ClassesDisplay = "Unknown";
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Spell == null) return;

        Spell.IsFavorite = !Spell.IsFavorite;
        await _spellRepository.SaveAsync(Spell);
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
