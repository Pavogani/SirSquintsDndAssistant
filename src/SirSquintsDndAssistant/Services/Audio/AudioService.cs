using Plugin.Maui.Audio;

namespace SirSquintsDndAssistant.Services.Audio;

/// <summary>
/// Audio service for playing ambient sounds and sound effects.
/// Uses Plugin.Maui.Audio for cross-platform audio playback.
/// </summary>
public class AudioService : IAudioService, IDisposable
{
    private const string VolumePreferenceKey = "audio_volume";
    private const string MutedPreferenceKey = "audio_muted";
    private const double DefaultVolume = 0.7;

    private readonly IAudioManager _audioManager;
    private readonly List<AmbiancePreset> _ambiances;
    private readonly List<SoundEffect> _soundEffects;
    private IAudioPlayer? _currentPlayer;
    private IAudioPlayer? _crossfadePlayer;
    private CancellationTokenSource? _fadeTokenSource;
    private bool _disposed;

    public bool IsPlaying { get; private set; }
    public string? CurrentAmbianceName { get; private set; }
    public double Volume { get; private set; }
    public bool IsMuted { get; private set; }

    public AudioService()
    {
        _audioManager = AudioManager.Current;
        _ambiances = InitializeAmbiances();
        _soundEffects = InitializeSoundEffects();

        // Load persisted audio settings
        Volume = Preferences.Default.Get(VolumePreferenceKey, DefaultVolume);
        IsMuted = Preferences.Default.Get(MutedPreferenceKey, false);
    }

    private static List<AmbiancePreset> InitializeAmbiances() => new()
    {
        // Tavern/Inn - using free ambient sound URLs
        new AmbiancePreset("Bustling Tavern", "Lively tavern with chatter, clinking glasses, and music", "Tavern", "🍺")
        { AudioUrl = "https://freesound.org/data/previews/462/462766_2488625-lq.mp3" },
        new AmbiancePreset("Quiet Inn", "Peaceful inn with crackling fire and soft murmurs", "Tavern", "🏨")
        { AudioUrl = "https://freesound.org/data/previews/370/370920_5450487-lq.mp3" },
        new AmbiancePreset("Seedy Dive", "Dark tavern with hushed conversations and creaking floors", "Tavern", "🌙"),

        // Nature
        new AmbiancePreset("Forest Day", "Birds chirping, leaves rustling, gentle breeze", "Nature", "🌲")
        { AudioUrl = "https://freesound.org/data/previews/566/566971_12606545-lq.mp3" },
        new AmbiancePreset("Forest Night", "Crickets, owls, mysterious night sounds", "Nature", "🦉")
        { AudioUrl = "https://freesound.org/data/previews/531/531947_5173388-lq.mp3" },
        new AmbiancePreset("Rainstorm", "Heavy rain with thunder and wind", "Nature", "🌧️")
        { AudioUrl = "https://freesound.org/data/previews/346/346642_5121236-lq.mp3" },
        new AmbiancePreset("Ocean Waves", "Calm ocean waves on a beach", "Nature", "🌊")
        { AudioUrl = "https://freesound.org/data/previews/557/557035_7676166-lq.mp3" },
        new AmbiancePreset("River", "Flowing river with birds and nature sounds", "Nature", "🏞️")
        { AudioUrl = "https://freesound.org/data/previews/365/365187_5653632-lq.mp3" },
        new AmbiancePreset("Mountain Wind", "High altitude wind with distant echoes", "Nature", "⛰️")
        { AudioUrl = "https://freesound.org/data/previews/244/244896_4284968-lq.mp3" },
        new AmbiancePreset("Swamp", "Murky swamp with buzzing insects and croaking frogs", "Nature", "🐸")
        { AudioUrl = "https://freesound.org/data/previews/324/324902_5260872-lq.mp3" },

        // Dungeon/Underground
        new AmbiancePreset("Stone Dungeon", "Dripping water, echoing footsteps, distant chains", "Dungeon", "🏰")
        { AudioUrl = "https://freesound.org/data/previews/385/385046_7278698-lq.mp3" },
        new AmbiancePreset("Cave", "Deep cave with water drops and wind howls", "Dungeon", "🕳️")
        { AudioUrl = "https://freesound.org/data/previews/398/398808_4284968-lq.mp3" },
        new AmbiancePreset("Crypt", "Eerie silence with occasional creaks and whispers", "Dungeon", "⚰️"),
        new AmbiancePreset("Sewer", "Flowing water, rats, and echoing drips", "Dungeon", "🐀"),
        new AmbiancePreset("Mine", "Pickaxes, carts, and cave-ins in distance", "Dungeon", "⛏️"),

        // Combat
        new AmbiancePreset("Battle Music", "Epic orchestral combat music", "Combat", "⚔️"),
        new AmbiancePreset("Tense Standoff", "Ominous tension music before battle", "Combat", "😰"),
        new AmbiancePreset("Boss Fight", "Intense dramatic boss encounter music", "Combat", "👹"),
        new AmbiancePreset("Victory", "Triumphant victory fanfare", "Combat", "🏆"),

        // Urban
        new AmbiancePreset("Busy Market", "Merchants hawking wares, crowds, and coins", "Urban", "🛒")
        { AudioUrl = "https://freesound.org/data/previews/431/431328_4284968-lq.mp3" },
        new AmbiancePreset("City Streets", "Footsteps, distant conversations, city life", "Urban", "🏙️"),
        new AmbiancePreset("Temple", "Reverent chanting, bells, and sacred silence", "Urban", "⛪"),
        new AmbiancePreset("Castle", "Stone halls, guards, and nobility", "Urban", "🏰"),
        new AmbiancePreset("Docks", "Ships creaking, seagulls, and sailors", "Urban", "⚓"),

        // Mystical
        new AmbiancePreset("Magical Library", "Turning pages, magical hums, arcane whispers", "Mystical", "📚"),
        new AmbiancePreset("Feywild", "Ethereal music, tinkling bells, magical sounds", "Mystical", "🧚"),
        new AmbiancePreset("Shadowfell", "Dark whispers, cold wind, dread atmosphere", "Mystical", "👻"),
        new AmbiancePreset("Astral Plane", "Cosmic hums, void echoes, otherworldly tones", "Mystical", "✨"),

        // Special
        new AmbiancePreset("Campfire", "Crackling fire with night sounds", "Special", "🔥")
        { AudioUrl = "https://freesound.org/data/previews/370/370920_5450487-lq.mp3" },
        new AmbiancePreset("Blizzard", "Howling wind and heavy snow", "Special", "❄️")
        { AudioUrl = "https://freesound.org/data/previews/277/277021_5078568-lq.mp3" },
        new AmbiancePreset("Desert", "Hot wind, sand, and distant mirages", "Special", "🏜️"),
        new AmbiancePreset("Ship at Sea", "Creaking wood, waves, and seagulls", "Special", "⛵"),
    };

    private static List<SoundEffect> InitializeSoundEffects() => new()
    {
        // Combat
        new SoundEffect("Sword Clash", "Metal weapons clashing", "Combat")
        { AudioUrl = "https://freesound.org/data/previews/320/320181_1661766-lq.mp3" },
        new SoundEffect("Arrow Shot", "Bow releasing an arrow", "Combat")
        { AudioUrl = "https://freesound.org/data/previews/321/321102_5121236-lq.mp3" },
        new SoundEffect("Spell Cast", "Magical spell being cast", "Combat")
        { AudioUrl = "https://freesound.org/data/previews/221/221683_1015240-lq.mp3" },
        new SoundEffect("Fireball", "Explosive fire magic", "Combat")
        { AudioUrl = "https://freesound.org/data/previews/156/156031_2703579-lq.mp3" },
        new SoundEffect("Lightning", "Electric discharge", "Combat")
        { AudioUrl = "https://freesound.org/data/previews/368/368808_4284968-lq.mp3" },
        new SoundEffect("Critical Hit", "Devastating blow landed", "Combat"),
        new SoundEffect("Shield Block", "Attack blocked by shield", "Combat"),

        // Creatures
        new SoundEffect("Dragon Roar", "Terrifying dragon roar", "Creatures")
        { AudioUrl = "https://freesound.org/data/previews/233/233390_4284968-lq.mp3" },
        new SoundEffect("Wolf Howl", "Wolf howling at moon", "Creatures")
        { AudioUrl = "https://freesound.org/data/previews/398/398145_4284968-lq.mp3" },
        new SoundEffect("Goblin Cackle", "Mischievous goblin laughter", "Creatures"),
        new SoundEffect("Zombie Groan", "Undead moaning", "Creatures"),
        new SoundEffect("Ghost Wail", "Spectral screaming", "Creatures"),

        // Environment
        new SoundEffect("Door Open", "Heavy door creaking open", "Environment")
        { AudioUrl = "https://freesound.org/data/previews/104/104528_1029401-lq.mp3" },
        new SoundEffect("Door Slam", "Door slamming shut", "Environment"),
        new SoundEffect("Chest Open", "Treasure chest being opened", "Environment")
        { AudioUrl = "https://freesound.org/data/previews/411/411749_5121236-lq.mp3" },
        new SoundEffect("Trap Trigger", "Trap mechanism activating", "Environment"),
        new SoundEffect("Collapse", "Stone or wood collapsing", "Environment"),
        new SoundEffect("Thunder", "Loud thunderclap", "Environment")
        { AudioUrl = "https://freesound.org/data/previews/368/368808_4284968-lq.mp3" },

        // Dice & Game
        new SoundEffect("Dice Roll", "Dice rolling on table", "Game")
        { AudioUrl = "https://freesound.org/data/previews/220/220744_4100837-lq.mp3" },
        new SoundEffect("Level Up", "Achievement fanfare", "Game"),
        new SoundEffect("Gold Coins", "Coins clinking", "Game")
        { AudioUrl = "https://freesound.org/data/previews/406/406639_7376026-lq.mp3" },
        new SoundEffect("Page Turn", "Book page turning", "Game"),

        // Alerts
        new SoundEffect("Initiative", "Combat starting alert", "Alert"),
        new SoundEffect("Turn Start", "Turn notification", "Alert"),
        new SoundEffect("Low HP Warning", "Health critical alert", "Alert"),
    };

    public IReadOnlyList<AmbiancePreset> GetAvailableAmbiances() => _ambiances.AsReadOnly();

    public IReadOnlyList<SoundEffect> GetAvailableSoundEffects() => _soundEffects.AsReadOnly();

    public async Task PlayAmbianceAsync(string ambianceName)
    {
        if (_disposed) return;

        // Stop current ambiance if playing
        await StopAmbianceAsync();

        var ambiance = _ambiances.FirstOrDefault(a => a.Name == ambianceName);
        if (ambiance == null)
        {
            System.Diagnostics.Debug.WriteLine($"Ambiance not found: {ambianceName}");
            return;
        }

        try
        {
            if (!string.IsNullOrEmpty(ambiance.AudioUrl))
            {
                // Stream audio from URL
                using var httpClient = new HttpClient();
                var stream = await httpClient.GetStreamAsync(ambiance.AudioUrl);

                // Create a memory stream copy for the audio player
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                _currentPlayer = _audioManager.CreatePlayer(memoryStream);
                _currentPlayer.Loop = true;
                _currentPlayer.Volume = IsMuted ? 0 : Volume;
                _currentPlayer.Play();

                CurrentAmbianceName = ambianceName;
                IsPlaying = true;
                System.Diagnostics.Debug.WriteLine($"Playing ambiance: {ambianceName}");
            }
            else
            {
                // No audio URL available - just track state
                CurrentAmbianceName = ambianceName;
                IsPlaying = true;
                System.Diagnostics.Debug.WriteLine($"Ambiance: {ambianceName} (no audio file available)");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing ambiance: {ex.Message}");
            CurrentAmbianceName = ambianceName;
            IsPlaying = true; // Still mark as playing for UI purposes
        }
    }

    public async Task StopAmbianceAsync()
    {
        if (_disposed) return;

        _fadeTokenSource?.Cancel();

        if (_currentPlayer != null)
        {
            _currentPlayer.Stop();
            _currentPlayer.Dispose();
            _currentPlayer = null;
        }

        if (IsPlaying)
        {
            System.Diagnostics.Debug.WriteLine($"Stopping ambiance: {CurrentAmbianceName}");
        }

        IsPlaying = false;
        CurrentAmbianceName = null;
        await Task.CompletedTask;
    }

    public async Task PlaySoundEffectAsync(string soundName)
    {
        if (_disposed || IsMuted) return;

        var effect = _soundEffects.FirstOrDefault(s => s.Name == soundName);
        if (effect == null)
        {
            System.Diagnostics.Debug.WriteLine($"Sound effect not found: {soundName}");
            return;
        }

        try
        {
            if (!string.IsNullOrEmpty(effect.AudioUrl))
            {
                // Stream audio from URL
                using var httpClient = new HttpClient();
                var stream = await httpClient.GetStreamAsync(effect.AudioUrl);

                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var player = _audioManager.CreatePlayer(memoryStream);
                player.Volume = Volume;
                player.Play();

                // Dispose after playing (fire and forget)
                _ = Task.Run(async () =>
                {
                    await Task.Delay(10000); // Max 10 seconds
                    player.Dispose();
                });

                System.Diagnostics.Debug.WriteLine($"Playing sound effect: {soundName}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Sound effect: {soundName} (no audio file available)");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing sound effect: {ex.Message}");
        }
    }

    public async Task FadeOutAsync(int durationMs = 2000)
    {
        if (_disposed || !IsPlaying || _currentPlayer == null) return;

        _fadeTokenSource?.Cancel();
        _fadeTokenSource = new CancellationTokenSource();

        try
        {
            var steps = 20;
            var stepDuration = durationMs / steps;
            var volumeStep = Volume / steps;
            var currentVolume = Volume;

            for (int i = 0; i < steps && !_fadeTokenSource.Token.IsCancellationRequested; i++)
            {
                currentVolume -= volumeStep;
                if (_currentPlayer != null)
                    _currentPlayer.Volume = Math.Max(0, currentVolume);
                await Task.Delay(stepDuration, _fadeTokenSource.Token);
            }

            await StopAmbianceAsync();
        }
        catch (OperationCanceledException)
        {
            // Fade was cancelled
        }
    }

    public async Task FadeInAsync(int durationMs = 2000)
    {
        if (_disposed || !IsPlaying || _currentPlayer == null) return;

        _fadeTokenSource?.Cancel();
        _fadeTokenSource = new CancellationTokenSource();

        try
        {
            var targetVolume = 0.7;
            var currentVolume = 0.0;
            var steps = 20;
            var stepDuration = durationMs / steps;
            var volumeStep = targetVolume / steps;

            if (_currentPlayer != null)
                _currentPlayer.Volume = 0;

            for (int i = 0; i < steps && !_fadeTokenSource.Token.IsCancellationRequested; i++)
            {
                currentVolume += volumeStep;
                if (_currentPlayer != null)
                    _currentPlayer.Volume = currentVolume;
                await Task.Delay(stepDuration, _fadeTokenSource.Token);
            }

            Volume = targetVolume;
        }
        catch (OperationCanceledException)
        {
            // Fade was cancelled
        }
    }

    public void SetVolume(double volume)
    {
        Volume = Math.Clamp(volume, 0, 1);
        Preferences.Default.Set(VolumePreferenceKey, Volume);

        if (_currentPlayer != null && !IsMuted)
        {
            _currentPlayer.Volume = Volume;
        }
    }

    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        Preferences.Default.Set(MutedPreferenceKey, muted);

        if (_currentPlayer != null)
        {
            _currentPlayer.Volume = muted ? 0 : Volume;
        }
    }

    /// <summary>
    /// Crossfade from current ambiance to a new one.
    /// Fades out the current track while simultaneously fading in the new track.
    /// </summary>
    public async Task CrossfadeToAsync(string ambianceName, int durationMs = 2000)
    {
        if (_disposed) return;

        var ambiance = _ambiances.FirstOrDefault(a => a.Name == ambianceName);
        if (ambiance == null)
        {
            System.Diagnostics.Debug.WriteLine($"Ambiance not found: {ambianceName}");
            return;
        }

        _fadeTokenSource?.Cancel();
        _fadeTokenSource = new CancellationTokenSource();

        try
        {
            // Start loading the new track
            IAudioPlayer? newPlayer = null;
            if (!string.IsNullOrEmpty(ambiance.AudioUrl))
            {
                using var httpClient = new HttpClient();
                var stream = await httpClient.GetStreamAsync(ambiance.AudioUrl);
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                newPlayer = _audioManager.CreatePlayer(memoryStream);
                newPlayer.Loop = true;
                newPlayer.Volume = 0; // Start at 0 for fade in
                newPlayer.Play();
            }

            var steps = 20;
            var stepDuration = durationMs / steps;
            var volumeStep = Volume / steps;

            // Crossfade: fade out old, fade in new
            for (int i = 0; i < steps && !_fadeTokenSource.Token.IsCancellationRequested; i++)
            {
                var fadeOutVolume = Volume - (volumeStep * (i + 1));
                var fadeInVolume = volumeStep * (i + 1);

                if (_currentPlayer != null)
                    _currentPlayer.Volume = Math.Max(0, fadeOutVolume);

                if (newPlayer != null && !IsMuted)
                    newPlayer.Volume = Math.Min(Volume, fadeInVolume);

                await Task.Delay(stepDuration, _fadeTokenSource.Token);
            }

            // Clean up old player
            _currentPlayer?.Stop();
            _currentPlayer?.Dispose();

            // Set new player as current
            _currentPlayer = newPlayer;
            CurrentAmbianceName = ambianceName;
            IsPlaying = newPlayer != null;

            System.Diagnostics.Debug.WriteLine($"Crossfaded to ambiance: {ambianceName}");
        }
        catch (OperationCanceledException)
        {
            // Crossfade was cancelled
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during crossfade: {ex.Message}");
            // Fall back to regular play
            await PlayAmbianceAsync(ambianceName);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _fadeTokenSource?.Cancel();
        _fadeTokenSource?.Dispose();

        _currentPlayer?.Stop();
        _currentPlayer?.Dispose();
        _currentPlayer = null;

        IsPlaying = false;
        CurrentAmbianceName = null;
    }
}
