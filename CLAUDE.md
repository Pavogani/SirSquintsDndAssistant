# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build for Windows (primary development target)
cd src/SirSquintsDndAssistant
dotnet build -f net10.0-windows10.0.19041.0

# Build for Android
dotnet build -f net10.0-android

# Run on Windows
dotnet run -f net10.0-windows10.0.19041.0

# Clean and rebuild
dotnet clean && dotnet build -f net10.0-windows10.0.19041.0
```

## Architecture Overview

This is a .NET MAUI cross-platform app (Windows & Android) for D&D Game Masters. It follows MVVM architecture using CommunityToolkit.Mvvm with SQLite for local storage.

### Project Structure

```
src/SirSquintsDndAssistant/
├── Models/           # SQLite entities (Campaign, Monster, Spell, etc.)
├── ViewModels/       # MVVM ViewModels using [ObservableProperty] and [RelayCommand]
├── Views/            # XAML pages and code-behind
├── Services/         # Business logic layer
│   ├── Api/          # External API clients (dnd5eapi.co, open5e.com)
│   ├── Database/     # IDatabaseService and repositories
│   ├── DataSync/     # Initial data download and sync
│   ├── Combat/       # Initiative, spell slots, status effects
│   ├── BattleMap/    # Grid-based map system with terrain generation
│   └── Utilities/    # Dice roller, generators, dialog service
├── Rendering/        # SkiaSharp battle map rendering layers
├── Controls/         # Custom controls (BattleMapCanvas)
├── Input/            # Input handling (MapTool enum, gestures)
└── Converters/       # XAML value converters
```

### Key Architectural Patterns

**Data Flow**: ViewModels inject repository interfaces → repositories use IDatabaseService → SQLite storage

**Startup Flow**: App.xaml.cs checks `IDataSyncService.IsInitialSyncCompleteAsync()` → routes to DataSyncPage or AppShell

**Navigation**: Shell-based with TabBar containing Combat, Creatures, Encounters, Campaign, Reference, Settings tabs

### External Data Sources

- **D&D 5e API** (`dnd5eapi.co`): Monsters, spells, equipment from SRD
- **Open5e API** (`api.open5e.com`): Additional monsters with full pagination
- **D&D Beyond** (unofficial): Character import via character ID

### Database

SQLite with 22+ tables. Key models:
- `Monster`, `Spell`, `Equipment`, `MagicItem` - D&D reference content
- `Campaign`, `Session`, `Quest`, `NPC` - Campaign management
- `CombatEncounter`, `InitiativeEntry` - Combat tracking
- `BattleMap`, `MapToken`, `TerrainOverlay` - Battle maps
- `HomebrewMonster/Spell/Item` - User-created content

### Service Registration

All services registered in `MauiProgram.cs:RegisterServices()`. Uses `AddSingleton` for repositories/services, `AddTransient` for ViewModels and Views.

### Battle Map Rendering

Uses SkiaSharp with layer architecture:
- `MapRenderer` orchestrates rendering via `MapRenderContext`
- Layers: Background → Grid → Terrain → Tokens → Fog of War → UI
- `BattleMapCanvas` is a custom `SKCanvasView` with bindable properties

### Combat System

`ICombatService` manages initiative order with:
- HP/Temp HP tracking
- Condition management (from downloaded conditions)
- Concentration tracking
- Death saves for players at 0 HP
- Spell slot tracking per caster class

## Important Conventions

- ViewModels inherit from `BaseViewModel` (provides `IsBusy`, `Title`)
- Use `[ObservableProperty]` and `[RelayCommand]` attributes (generates property/command boilerplate)
- Repositories return `PagedResult<T>` for large collections (50 items/page)
- Use `Random.Shared` instead of `new Random()` for thread safety
- Use `IDialogService` for dialogs instead of `Application.Current.MainPage`
- XAML converters in `Converters/` folder, registered in `App.xaml` resources

## Data Sync

First launch downloads 1,100+ items from APIs. Progress reported via `IProgress<SyncProgress>`. Sync state stored in Preferences. "Force Re-Download" in Settings clears sync flags.

## Audio System

`IAudioService` with `Plugin.Maui.Audio` for cross-platform playback. Ambiance presets and sound effects stream from freesound.org URLs.

## Known Limitations

- Warnings about MVVM Toolkit AOT compatibility (MVVMTK0045) are expected and benign
- No monster images in SRD data - images must be user-provided or searched via `ICommunityImageService`
- D&D Beyond import uses unofficial API that may break

---

## Comprehensive Issue Analysis (January 2026)

A thorough code analysis was performed. Below are all identified issues requiring attention before or during any rework.

### CRITICAL ISSUES (Must Fix)

#### 1. Memory Leaks in Services

**CommunityImageService.cs** - HttpClient not properly disposed in error paths:
- `_memoryCache` stores `ImageSource` objects indefinitely without cleanup
- No cache size limits - will grow unbounded during long sessions
- `IDisposable` pattern implemented but `_httpClient` may leak on early returns

**DataSyncService.cs** - Memory pressure during sync:
- Downloads thousands of items into memory before batch insert
- No chunked processing for large datasets

#### 2. Broken Pagination Logic

**Open5eApiClient.cs**:
- `GetMonstersPagedAsync()` uses `offset` parameter but API expects `page`
- Was fetching only 260 monsters instead of 3,200+ (FIXED in session)

**DataSyncService.cs**:
- Hard limits of 5 pages or 100 items artificially cap data
- `while (page <= 5)` at line ~156 stops early
- `Math.Min(equipmentList.Results.Count, 100)` caps equipment

#### 3. N+1 Query Patterns

**Dnd5eApiClient.cs**:
- `GetAllMonstersAsync()` fetches index, then individual calls per monster
- Should use batch endpoint or parallel with semaphore

### HIGH PRIORITY ISSUES

#### Services Layer (8 issues)

1. **AudioService.cs** - No disposal of audio streams on stop/skip
2. **CombatService.cs** - Event handlers registered but never unregistered
3. **InitiativeService.cs** - `SortedEntries` recalculated on every access
4. **BattleMapService.cs** - No validation on map dimensions (can create 0x0 maps)
5. **SpellSlotService.cs** - Hardcoded spell slot tables should be data-driven
6. **CharacterImportService.cs** - D&D Beyond URL parsing is brittle
7. **TerrainGenerationService.cs** - `Random.Shared` used inconsistently with `new Random(seed)`
8. **DialogService.cs** - Fire-and-forget async calls without error handling

#### ViewModel Issues (60+ issues)

**Memory Leak Event Handlers** (affects 12+ ViewModels):
```csharp
// Pattern found in multiple ViewModels - events subscribed but never unsubscribed
_combatService.CombatantsChanged += OnCombatantsChanged;  // Never -=
```

Affected ViewModels:
- `InitiativeTrackerViewModel`
- `CombatEncounterViewModel`
- `BattleMapViewModel`
- `SpellSlotsViewModel`
- `ConditionsViewModel`
- `NPCDetailViewModel`
- `SessionNotesViewModel`
- `QuestTrackerViewModel`
- `AmbiencePlayerViewModel`
- `PartyManagerViewModel`
- `EncounterBuilderViewModel`
- `HomebrewCreatorViewModel`

**Fire-and-Forget Async** (24 instances):
```csharp
// Dangerous pattern - exceptions silently swallowed
_ = LoadDataAsync();  // Found in OnAppearing handlers
```

**Race Conditions** (8 instances):
- `IsBusy` checks are not thread-safe
- Multiple rapid taps can trigger duplicate commands
- `SearchText` changed handler fires during typing, causing overlapping queries

**Missing Null Checks** (15 instances):
- `SelectedMonster`, `SelectedSpell`, `SelectedItem` accessed without null guards
- Navigation parameters assumed to always exist

#### Model Issues (40 issues)

**JSON Serialization Mismatches**:
```csharp
// Monster.cs - API returns "hit_points" but property is "HitPoints"
public int HitPoints { get; set; }  // Missing [JsonPropertyName("hit_points")]
```

Affected properties across models:
- `Monster`: `hit_dice`, `armor_class`, `challenge_rating`, `special_abilities`
- `Spell`: `higher_level`, `casting_time`, `at_higher_levels`
- `Equipment`: `equipment_category`, `weapon_category`, `armor_category`
- `MagicItem`: `requires_attunement`

**Missing Foreign Keys**:
- `InitiativeEntry.EncounterId` has no FK constraint
- `MapToken.BattleMapId` not enforced
- `TerrainOverlay.BattleMapId` not enforced

**No Validation**:
- `Monster.ChallengeRating` allows negative values
- `BattleMap.Width/Height` allows 0 or negative
- `Spell.Level` allows values outside 0-9 range

**Incomplete Models**:
- `HomebrewMonster` missing `LegendaryActions`, `LairActions`
- `Equipment` missing weight and cost for many items
- `Condition` missing `Duration` and `EndCondition` fields

#### View/XAML Issues (10 issues)

1. **Missing xmlns** - Several pages missing `xmlns:viewmodels` causing design-time errors
2. **MVVM Violations** - Code-behind directly manipulates ViewModel state in 4 pages
3. **Hardcoded Colors** - 47 instances of hardcoded hex colors instead of using resources
4. **Hardcoded Strings** - No localization, all strings inline
5. **Missing Loading States** - 8 pages have no loading indicator while fetching
6. **Empty State Missing** - Collections show blank instead of "No items found"
7. **Inconsistent Spacing** - Padding/Margin values vary (8, 10, 12, 15, 16, 20)
8. **Dead XAML** - Commented-out sections in 6 pages
9. **Missing DataType** - `DataTemplate` without `x:DataType` loses compile-time checking
10. **Event Handlers in XAML** - Should use Command binding instead

#### Rendering/Controls Issues (12 issues)

**SkiaSharp Resource Leaks**:
```csharp
// MapRenderer.cs - SKBitmap created but not disposed
var bitmap = SKBitmap.Decode(stream);  // Never disposed

// TokenLayer.cs - SKPathEffect leaks
paint.PathEffect = SKPathEffect.CreateDash(...);  // Not disposed
```

**Dead Code**:
- `_needsRedraw` flag defined but never checked in 3 layer classes
- `InvalidateVisual()` method exists but never called

**Missing Null Safety**:
- `MapRenderContext.CurrentMap` can be null but accessed without checks
- `Tokens` collection iterated without null check

**Performance Issues**:
- `TokenLayer` recreates `SKPaint` objects every frame
- `GridLayer` recalculates visible cells every render
- No dirty rect tracking - full redraw on any change

### MEDIUM PRIORITY ISSUES

#### Dead Code (should be removed)

1. **Unused Services**:
   - `IGameStateService` interface with no implementation
   - `LegacyImportService` - obsolete import logic

2. **Unused Models**:
   - `PlayerCharacterSnapshot` - never instantiated
   - `EncounterTemplate` - partial implementation abandoned

3. **Unused Methods** (across codebase):
   - `MonsterRepository.GetByChallengeRatingRangeAsync()` - never called
   - `SpellRepository.GetByComponentsAsync()` - never called
   - `BattleMapService.ExportMapAsync()` - incomplete stub

#### Incomplete Features

1. **Homebrew System**:
   - Create works, Edit broken (doesn't load existing values)
   - Delete has no confirmation dialog
   - No export/import of homebrew content

2. **Battle Map**:
   - Fog of War UI exists but reveal/hide doesn't persist
   - Terrain effects not applied during combat
   - No image background support (UI present, logic missing)

3. **Campaign Management**:
   - Session linking incomplete
   - Quest dependencies not tracked
   - Timeline/calendar not implemented

4. **Character Import**:
   - D&D Beyond import only gets basic stats
   - No spell/inventory import
   - No update mechanism for existing characters

5. **Audio/Ambience**:
   - Presets hardcoded, no custom presets
   - Volume not persisted between sessions
   - No crossfade between tracks

### LOW PRIORITY ISSUES

1. **Code Style Inconsistencies**:
   - Mix of `async void` and `async Task` for similar operations
   - Some files use `var`, others explicit types
   - Inconsistent null-forgiving operator usage (`!`)

2. **Missing XML Documentation**:
   - Public interfaces lack `<summary>` tags
   - Complex methods have no parameter documentation

3. **Test Coverage**:
   - No unit tests exist
   - No integration tests for API clients
   - No UI automation tests

### RECOMMENDED REWORK PRIORITIES

1. **Fix Critical Memory Leaks** - Implement proper disposal patterns
2. **Fix Data Sync** - Remove artificial limits, add proper pagination
3. **Add Event Unsubscription** - Implement `IDisposable` on ViewModels
4. **Fix JSON Serialization** - Add proper `[JsonPropertyName]` attributes
5. **Implement Dirty Tracking** - For SkiaSharp rendering performance
6. **Add Validation** - Model validation before database operations
7. **Consolidate Design System** - Remove hardcoded colors/spacing
8. **Remove Dead Code** - Clean up unused services and methods
