namespace SirSquintsDndAssistant.Services.Audio;

public interface IAudioService
{
    bool IsPlaying { get; }
    string? CurrentAmbianceName { get; }
    double Volume { get; set; }
    bool IsMuted { get; set; }

    Task PlayAmbianceAsync(string ambianceName);
    Task StopAmbianceAsync();
    Task PlaySoundEffectAsync(string soundName);
    Task FadeOutAsync(int durationMs = 2000);
    Task FadeInAsync(int durationMs = 2000);
    void SetVolume(double volume);
    void SetMuted(bool muted);

    IReadOnlyList<AmbiancePreset> GetAvailableAmbiances();
    IReadOnlyList<SoundEffect> GetAvailableSoundEffects();
}

public class AmbiancePreset
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string IconGlyph { get; set; }
    public string? AudioUrl { get; set; }

    public AmbiancePreset(string name, string description, string category, string iconGlyph)
    {
        Name = name;
        Description = description;
        Category = category;
        IconGlyph = iconGlyph;
    }
}

public class SoundEffect
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string? AudioUrl { get; set; }

    public SoundEffect(string name, string description, string category)
    {
        Name = name;
        Description = description;
        Category = category;
    }
}
