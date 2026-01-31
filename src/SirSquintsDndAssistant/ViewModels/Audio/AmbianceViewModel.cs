using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Services.Audio;

namespace SirSquintsDndAssistant.ViewModels.Audio;

public partial class AmbianceViewModel : ObservableObject
{
    private readonly IAudioService _audioService;

    [ObservableProperty]
    private ObservableCollection<AmbianceCategory> _ambianceCategories = new();

    [ObservableProperty]
    private ObservableCollection<SoundEffectCategory> _soundEffectCategories = new();

    [ObservableProperty]
    private string? _currentAmbianceName;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private double _volume = 0.7;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private string _selectedCategory = "All";

    public List<string> Categories { get; } = new()
    {
        "All", "Tavern", "Nature", "Dungeon", "Combat", "Urban", "Mystical", "Special"
    };

    public AmbianceViewModel(IAudioService audioService)
    {
        _audioService = audioService;
        LoadAmbiances();
        LoadSoundEffects();
    }

    private void LoadAmbiances()
    {
        var ambiances = _audioService.GetAvailableAmbiances();
        var grouped = ambiances.GroupBy(a => a.Category);

        AmbianceCategories.Clear();
        foreach (var group in grouped.OrderBy(g => g.Key))
        {
            AmbianceCategories.Add(new AmbianceCategory
            {
                Name = group.Key,
                Ambiances = new ObservableCollection<AmbiancePreset>(group.ToList())
            });
        }
    }

    private void LoadSoundEffects()
    {
        var effects = _audioService.GetAvailableSoundEffects();
        var grouped = effects.GroupBy(e => e.Category);

        SoundEffectCategories.Clear();
        foreach (var group in grouped.OrderBy(g => g.Key))
        {
            SoundEffectCategories.Add(new SoundEffectCategory
            {
                Name = group.Key,
                SoundEffects = new ObservableCollection<SoundEffect>(group.ToList())
            });
        }
    }

    [RelayCommand]
    private async Task PlayAmbianceAsync(AmbiancePreset ambiance)
    {
        await _audioService.PlayAmbianceAsync(ambiance.Name);
        CurrentAmbianceName = _audioService.CurrentAmbianceName;
        IsPlaying = _audioService.IsPlaying;
    }

    [RelayCommand]
    private async Task StopAmbianceAsync()
    {
        await _audioService.StopAmbianceAsync();
        CurrentAmbianceName = null;
        IsPlaying = false;
    }

    [RelayCommand]
    private async Task FadeOutAsync()
    {
        await _audioService.FadeOutAsync();
        CurrentAmbianceName = null;
        IsPlaying = false;
    }

    [RelayCommand]
    private async Task PlaySoundEffectAsync(SoundEffect effect)
    {
        await _audioService.PlaySoundEffectAsync(effect.Name);
    }

    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        _audioService.IsMuted = IsMuted;
    }

    partial void OnVolumeChanged(double value)
    {
        _audioService.Volume = value;
    }
}

public class AmbianceCategory
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<AmbiancePreset> Ambiances { get; set; } = new();
}

public class SoundEffectCategory
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<SoundEffect> SoundEffects { get; set; } = new();
}
