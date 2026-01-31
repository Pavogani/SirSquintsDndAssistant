# Technical Debt & Issues Tracker

Last Updated: January 31, 2026

This document tracks all known issues, bugs, and incomplete features for prioritization during the planned rework.

---

## Status Legend
- [ ] Not Started
- [~] In Progress
- [x] Completed

---

## CRITICAL (P0) - Must Fix Before Release

### Memory Leaks

- [x] **CommunityImageService** - `_memoryCache` grows unbounded
  - File: `Services/Images/CommunityImageService.cs`
  - Impact: App memory usage grows until crash
  - Fix: Add LRU cache with size limit, proper disposal
  - **FIXED**: Implemented LRU cache with MaxMemoryCacheSize=100, MaxUrlCacheSize=500

- [x] **HttpClient disposal** - Not disposed in error paths
  - File: `Services/Images/CommunityImageService.cs`
  - Fix: Use `using` statements or ensure `finally` blocks
  - **FIXED**: Implemented IDisposable, proper disposal in Dispose()

- [x] **DataSyncService** - Memory pressure during bulk sync
  - File: `Services/DataSync/DataSyncService.cs`
  - Impact: Can OOM on low-memory devices
  - Fix: Chunk processing, don't load all items into memory
  - **FIXED**: Added ChunkInsertSize=100, items inserted in batches during download

### Data Sync Issues

- [x] **Open5e API pagination** - Was using `offset` instead of `page`
  - File: `Services/Api/Open5eApiClient.cs`
  - Impact: Only 260 of 3,200+ monsters loaded
  - **FIXED**: Changed to `page` parameter

- [x] **Artificial data limits** - Hard caps on sync
  - File: `Services/DataSync/DataSyncService.cs`
  - Lines: ~156 (`page <= 5`), ~272/302 (`Math.Min(..., 100)`)
  - Impact: Missing equipment, magic items, spells
  - **FIXED**: Removed limits, full pagination used

- [x] **ForceResyncAsync doesn't clear database**
  - File: `Services/DataSync/DataSyncService.cs`
  - Impact: Duplicate data, stale records persist
  - **FIXED**: Added DeleteAllAsync calls before re-syncing

### N+1 Query Performance

- [x] **Monster details fetching** - Individual API calls per monster
  - File: `Services/Api/Dnd5eApiClient.cs`
  - Impact: Thousands of HTTP requests, slow sync
  - **FIXED**: Added parallel fetching with SemaphoreSlim(MaxConcurrentRequests=10)

---

## HIGH (P1) - Significant Impact

### Event Handler Memory Leaks

| ViewModel | Event | Status |
|-----------|-------|--------|
| InitiativeTrackerViewModel | CombatantsChanged | [x] Fixed |
| CombatEncounterViewModel | CombatantsChanged | [x] Fixed |
| BattleMapViewModel | MapChanged | [x] Fixed |
| SpellSlotsViewModel | SlotsChanged | [x] Fixed |
| ConditionsViewModel | ConditionsChanged | [x] Fixed |
| NPCDetailViewModel | PropertyChanged | [x] Fixed |
| SessionNotesViewModel | NotesChanged | [x] Fixed |
| QuestTrackerViewModel | QuestsChanged | [x] Fixed |
| AmbiencePlayerViewModel | TrackChanged | [x] Fixed |
| PartyManagerViewModel | PartyChanged | [x] Fixed |
| EncounterBuilderViewModel | EncounterChanged | [x] Fixed |
| HomebrewCreatorViewModel | ContentChanged | [x] Fixed |

**Status**: All ViewModels now implement IDisposable and unsubscribe in Dispose()

### Fire-and-Forget Async Calls

- [x] Added `SafeFireAndForget()` extension method in Extensions/TaskExtensions.cs
- [x] Updated all fire-and-forget calls to use SafeFireAndForget() for proper error logging

### Race Conditions

- [x] `IsBusy` checks not thread-safe - Fixed with proper locking patterns
- [x] Rapid command execution causes duplicates - Fixed with IsBusy guards
- [x] **Search debounce missing on text changed** - Added DebounceHelper class
  - MonsterDatabaseViewModel - Added debounce
  - SpellbookViewModel - Added debounce
  - ItemDatabaseViewModel - Added debounce
  - EncounterBuilderViewModel - Added debounce

### JSON Serialization Mismatches

- [x] **Already fixed** - All API models in `Models/Api/Dnd5eApiModels.cs` have proper `[JsonPropertyName]` attributes
  - Mapping to entity models happens in service layer, works correctly

### Image Loading (~33% Success Rate)

- [x] D&D Beyond URLs require authentication - **REMOVED** from URL sources
- [x] Wikia URL patterns malformed - **REMOVED** from URL sources
- [x] 5e.tools needs exact name matching - Added multiple book sources (MM, MPMM, VGM, MTF, FTD, etc.)
- [x] No placeholder fallback when all sources fail - **ADDED** GetMonsterPlaceholderResource(), GetSpellPlaceholderResource(), GetItemPlaceholderResource()

**Status**: Service updated with placeholder methods. Placeholder image files need to be created.

---

## MEDIUM (P2) - Should Fix

### SkiaSharp Resource Leaks

- [x] `SKBitmap` created but not disposed in `MapRenderer.cs` - **FIXED**: Proper using statements
- [x] `SKPathEffect` leaks in `TokenLayer.cs` - **FIXED**: Dispose() implemented
- [x] `SKPaint` recreated every frame - **FIXED**: GridLayer and TokenLayer dispose paints properly

### Model Validation

- [x] `BattleMap.GridWidth/GridHeight` allows 0 or negative - **FIXED**: Added [Range(1, 200)] attributes
- [x] `Monster.ChallengeRating` allows negative - **FIXED**: Added [Range(0, 30)] attribute
- [x] `Spell.Level` allows outside 0-9 range - **FIXED**: Added [Range(0, 9)] attribute
- [x] Added validation attributes to Monster ability scores [Range(1, 30)]
- [x] FK constraints on SQLite tables - **FIXED**: Added PRAGMA foreign_keys = ON and cascade delete triggers

### Dead Code

- [x] Cleaned up unused variables (`buildingCount` in BattleMapService, `_lastPinchDistance` in BattleMapCanvas)
- [x] Added #pragma warnings for `_needsRedraw` fields reserved for future dirty tracking optimization
- Note: IGameStateService, LegacyImportService, PlayerCharacterSnapshot mentioned in original doc do not exist in codebase

### Incomplete Features

#### Homebrew System
- [x] Edit doesn't load existing values - **FIXED**: Added HomebrewMonsterEditViewModel, HomebrewSpellEditViewModel, HomebrewItemEditViewModel with full edit pages
- [x] Delete has no confirmation - **ALREADY IMPLEMENTED**: Confirmation dialogs exist
- [x] No export/import - **ALREADY IMPLEMENTED**: ExportAllToJsonAsync and ImportFromJsonAsync exist

#### Battle Map
- [x] Fog of War doesn't persist - **FIXED**: Added RevealFogCellsAsync, HideFogCellsAsync, ResetFogOfWarAsync, RevealAllFogAsync with auto-save
- [ ] Terrain effects not applied in combat - Visual only, needs integration with InitiativeTracker
- [ ] Image backgrounds not working - UI needed for background image selection

#### Campaign Management
- [x] Session linking incomplete - **FIXED**: Added prev/next session navigation, GoToSessionByNumber
- [x] Quest dependencies not tracked - **FIXED**: Added QuestNode hierarchy, CreateSubQuestAsync, SetParentQuestAsync, dependency validation
- [ ] Timeline not implemented - Visual timeline UI needed

#### Audio/Ambience
- [ ] Presets hardcoded - Custom preset saving requires database table
- [x] Volume not persisted - **FIXED**: Added Preferences storage for volume and mute state
- [x] No crossfade - **FIXED**: Added CrossfadeToAsync method with simultaneous fade out/in

---

## LOW (P3) - Nice to Have

### Code Quality

- [ ] Mix of `async void` and `async Task`
- [ ] Inconsistent `var` vs explicit types
- [ ] Missing XML documentation on interfaces
- [ ] No unit tests

### UI/UX Polish

- [x] **47 hardcoded colors** - Replaced with design token system (Colors.xaml, Styles.xaml)
  - Primary, Secondary, Tertiary colors defined
  - Card, StatBlock, CombatantCard, MonsterCard component styles
  - AppThemeBinding for dark mode support
- [ ] Inconsistent spacing values
- [x] **Loading indicators** - All major pages have ActivityIndicator bound to IsBusy
- [x] **Empty states** - All CollectionViews have EmptyView templates
- [ ] No localization support

### Performance

- [ ] `GridLayer` recalculates cells every render
- [ ] `TokenLayer` no dirty tracking (field reserved for future use)
- [ ] Full redraw on any change

---

## Files Most In Need of Refactoring

1. ~~**DataSyncService.cs**~~ - [x] Fixed: pagination, memory, parallel fetching
2. ~~**CommunityImageService.cs**~~ - [x] Fixed: LRU caching, disposal, placeholder support
3. ~~**InitiativeTrackerViewModel.cs**~~ - [x] Fixed: Event leaks, busy state
4. ~~**BattleMapViewModel.cs**~~ - [x] Fixed: Event leaks, complex state
5. ~~**Monster.cs**~~ - [x] Fixed: Added validation attributes
6. ~~**MapRenderer.cs**~~ - [x] Fixed: Resource disposal
7. ~~**Open5eApiClient.cs**~~ - [x] Fixed: Pagination logic
8. ~~**Dnd5eApiClient.cs**~~ - [x] Fixed: Parallel fetching

---

## Completed Rework Summary

### Phase 1: Critical Fixes - COMPLETE
1. [x] Fix memory leaks in CommunityImageService (LRU cache implemented)
2. [x] Fix DataSyncService pagination and limits (full pagination, chunked inserts)
3. [x] Add proper ForceResyncAsync database clearing
4. [x] JSON serialization attributes (verified - already in API models)

### Phase 2: Stability - COMPLETE
1. [x] Implement IDisposable on ViewModels (all 12+ ViewModels updated)
2. [x] Fix fire-and-forget async patterns (SafeFireAndForget extension added)
3. [x] Add validation to models (BattleMap, Monster, Spell have Range/Required attributes)
4. [x] Fix SkiaSharp resource leaks (all layers verified)

### Phase 3: UI Rework - COMPLETE
1. [x] Consolidated design system (Colors.xaml, Styles.xaml with design tokens)
2. [x] Applied D&D Classic theme to all major pages
3. [x] Added search debounce to all search ViewModels
4. [x] Added loading indicators to all data-loading pages
5. [x] Added empty state views to all collections

### Phase 4: Polish - IN PROGRESS
1. [x] Complete homebrew system - Edit pages added, delete/export already implemented
2. [~] Complete battle map features - Fog persistence added, terrain effects and image backgrounds pending
3. [x] Complete campaign management - Session linking and quest dependencies added, timeline pending
4. [~] Fix audio system - Volume persistence and crossfade added, custom presets pending
5. [ ] Performance optimization (dirty tracking)
6. [ ] Add unit tests

### Remaining Items
- Battle map: Terrain effects integration, image background UI
- Audio: Custom preset database storage
- Campaign: Visual timeline component
- Performance: GridLayer/TokenLayer dirty tracking
- Quality: Unit test coverage

