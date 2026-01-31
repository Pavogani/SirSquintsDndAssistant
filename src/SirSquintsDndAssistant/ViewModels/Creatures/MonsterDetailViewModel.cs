using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Creatures;
using SirSquintsDndAssistant.Services.Database.Repositories;
using SirSquintsDndAssistant.Services.Images;
using SirSquintsDndAssistant.Services.Utilities;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace SirSquintsDndAssistant.ViewModels.Creatures;

[QueryProperty(nameof(Monster), "Monster")]
public partial class MonsterDetailViewModel : BaseViewModel
{
    private readonly IMonsterRepository _monsterRepository;
    private readonly ICommunityImageService _imageService;
    private readonly IImageService _localImageService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private Monster? monster;

    [ObservableProperty]
    private ImageSource? monsterImage;

    [ObservableProperty]
    private bool hasImage;

    [ObservableProperty]
    private bool isLoadingImage;

    [ObservableProperty]
    private string abilityModifiers = string.Empty;

    [ObservableProperty]
    private string speedDisplay = string.Empty;

    [ObservableProperty]
    private ObservableCollection<MonsterAbility> actions = new();

    [ObservableProperty]
    private ObservableCollection<MonsterAbility> specialAbilities = new();

    [ObservableProperty]
    private bool hasActions;

    [ObservableProperty]
    private bool hasSpecialAbilities;

    public MonsterDetailViewModel(
        IMonsterRepository monsterRepository,
        ICommunityImageService imageService,
        IImageService localImageService,
        IDialogService dialogService)
    {
        _monsterRepository = monsterRepository;
        _imageService = imageService;
        _localImageService = localImageService;
        _dialogService = dialogService;
        Title = "Monster Details";
    }

    partial void OnMonsterChanged(Monster? value)
    {
        if (value != null)
        {
            Title = value.Name;
            CalculateAbilityModifiers();
            ParseSpeeds();
            ParseActions();
            ParseSpecialAbilities();
            _ = LoadImageAsync();
        }
    }

    private async Task LoadImageAsync()
    {
        if (Monster == null) return;

        IsLoadingImage = true;
        try
        {
            // Try to load image - GetImageSourceAsync handles priority (local path first, then URL)
            // Method signature: GetImageSourceAsync(string? imageUrl, string? localPath)
            MonsterImage = await _imageService.GetImageSourceAsync(Monster.ImageUrl, Monster.ImagePath);

            // If no image found and we have no URL, try to auto-search for one
            if (MonsterImage == null && string.IsNullOrWhiteSpace(Monster.ImageUrl))
            {
                // Auto-search for image on first view (non-blocking, fire and forget for background)
                _ = TryAutoSearchImageAsync();
            }

            HasImage = MonsterImage != null;
        }
        finally
        {
            IsLoadingImage = false;
        }
    }

    private async Task TryAutoSearchImageAsync()
    {
        if (Monster == null) return;

        try
        {
            var foundUrl = await _imageService.GetMonsterImageUrlAsync(Monster.Name, Monster.Type);
            if (!string.IsNullOrEmpty(foundUrl))
            {
                Monster.ImageUrl = foundUrl;
                await _monsterRepository.SaveAsync(Monster);

                // Reload the image
                MonsterImage = await _imageService.GetImageSourceAsync(foundUrl, null);
                HasImage = MonsterImage != null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Auto-search image failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SearchCommunityImageAsync()
    {
        if (Monster == null) return;

        IsLoadingImage = true;
        try
        {
            var imageUrl = await _imageService.GetMonsterImageUrlAsync(Monster.Name, Monster.Type);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                // Cache the image locally
                var localPath = await _imageService.DownloadAndCacheImageAsync(imageUrl, "monster", Monster.Name);
                if (!string.IsNullOrEmpty(localPath))
                {
                    Monster.ImagePath = localPath;
                    Monster.ImageUrl = imageUrl;
                    await _monsterRepository.SaveAsync(Monster);
                    await LoadImageAsync();
                    await _dialogService.DisplayAlertAsync("Image Found", $"Found and cached image for {Monster.Name}");
                }
                else
                {
                    Monster.ImageUrl = imageUrl;
                    await _monsterRepository.SaveAsync(Monster);
                    await LoadImageAsync();
                }
            }
            else
            {
                await _dialogService.DisplayAlertAsync("No Image Found", $"Could not find a community image for {Monster.Name}. You can add a custom image instead.");
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
        if (Monster == null) return;

        var imagePath = await _localImageService.PickAndSaveImageAsync($"monster_{Monster.Id}");
        if (!string.IsNullOrEmpty(imagePath))
        {
            Monster.ImagePath = imagePath;
            await _monsterRepository.SaveAsync(Monster);
            await LoadImageAsync();
        }
    }

    [RelayCommand]
    private async Task SetImageUrlAsync()
    {
        if (Monster == null) return;

        var url = await _dialogService.DisplayPromptAsync(
            "Set Image URL",
            "Enter the URL of the monster image:",
            initialValue: Monster.ImageUrl);

        if (!string.IsNullOrWhiteSpace(url))
        {
            Monster.ImageUrl = url;
            await _monsterRepository.SaveAsync(Monster);
            await LoadImageAsync();
        }
    }

    [RelayCommand]
    private async Task RemoveImageAsync()
    {
        if (Monster == null) return;

        var confirm = await _dialogService.DisplayConfirmAsync(
            "Remove Image",
            "Are you sure you want to remove the monster image?");

        if (confirm)
        {
            if (!string.IsNullOrEmpty(Monster.ImagePath))
            {
                await _localImageService.DeleteImageAsync(Monster.ImagePath);
            }
            Monster.ImagePath = string.Empty;
            Monster.ImageUrl = string.Empty;
            await _monsterRepository.SaveAsync(Monster);
            MonsterImage = null;
            HasImage = false;
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Monster == null) return;

        Monster.IsFavorite = !Monster.IsFavorite;
        await _monsterRepository.SaveAsync(Monster);
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private void CalculateAbilityModifiers()
    {
        if (Monster == null) return;

        int CalculateMod(int score) => (score - 10) / 2;

        var mods = $"STR: {FormatModifier(CalculateMod(Monster.Strength))} | " +
                   $"DEX: {FormatModifier(CalculateMod(Monster.Dexterity))} | " +
                   $"CON: {FormatModifier(CalculateMod(Monster.Constitution))} | " +
                   $"INT: {FormatModifier(CalculateMod(Monster.Intelligence))} | " +
                   $"WIS: {FormatModifier(CalculateMod(Monster.Wisdom))} | " +
                   $"CHA: {FormatModifier(CalculateMod(Monster.Charisma))}";

        AbilityModifiers = mods;
    }

    private string FormatModifier(int mod)
    {
        return mod >= 0 ? $"+{mod}" : mod.ToString();
    }

    private void ParseSpeeds()
    {
        if (Monster == null || string.IsNullOrEmpty(Monster.SpeedsJson))
        {
            SpeedDisplay = "30 ft.";
            return;
        }

        try
        {
            var speeds = JsonSerializer.Deserialize<Dictionary<string, string>>(Monster.SpeedsJson);
            if (speeds != null && speeds.Count > 0)
            {
                var speedParts = speeds.Select(kvp =>
                    kvp.Key.ToLower() == "walk" ? kvp.Value : $"{kvp.Key} {kvp.Value}");
                SpeedDisplay = string.Join(", ", speedParts);
            }
            else
            {
                SpeedDisplay = "30 ft.";
            }
        }
        catch
        {
            SpeedDisplay = "30 ft.";
        }
    }

    private void ParseActions()
    {
        Actions.Clear();
        HasActions = false;

        if (Monster == null || string.IsNullOrEmpty(Monster.ActionsJson))
            return;

        try
        {
            var actions = JsonSerializer.Deserialize<List<MonsterAbility>>(Monster.ActionsJson);
            if (actions != null && actions.Count > 0)
            {
                foreach (var action in actions)
                {
                    Actions.Add(action);
                }
                HasActions = true;
            }
        }
        catch
        {
            // JSON parsing failed, leave empty
        }
    }

    private void ParseSpecialAbilities()
    {
        SpecialAbilities.Clear();
        HasSpecialAbilities = false;

        if (Monster == null || string.IsNullOrEmpty(Monster.SpecialAbilitiesJson))
            return;

        try
        {
            var abilities = JsonSerializer.Deserialize<List<MonsterAbility>>(Monster.SpecialAbilitiesJson);
            if (abilities != null && abilities.Count > 0)
            {
                foreach (var ability in abilities)
                {
                    SpecialAbilities.Add(ability);
                }
                HasSpecialAbilities = true;
            }
        }
        catch
        {
            // JSON parsing failed, leave empty
        }
    }
}
