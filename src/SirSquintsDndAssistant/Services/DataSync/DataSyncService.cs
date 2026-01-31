using System.Text.Json;
using SirSquintsDndAssistant.Models.Api;
using SirSquintsDndAssistant.Models.Creatures;
using SirSquintsDndAssistant.Models.Content;
using SirSquintsDndAssistant.Services.Api;
using SirSquintsDndAssistant.Services.Database;
using SirSquintsDndAssistant.Services.Database.Repositories;
using DndCondition = SirSquintsDndAssistant.Models.Content.Condition;

namespace SirSquintsDndAssistant.Services.DataSync;

public class DataSyncService : IDataSyncService
{
    private readonly IDnd5eApiClient _dnd5eApi;
    private readonly IOpen5eApiClient _open5eApi;
    private readonly IDatabaseService _database;
    private readonly IMonsterRepository _monsterRepo;
    private readonly ISpellRepository _spellRepo;
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IMagicItemRepository _magicItemRepo;
    private readonly IConditionRepository _conditionRepo;

    private const string INITIAL_SYNC_KEY = "InitialSyncComplete";
    private const string LAST_SYNC_KEY = "LastSyncDate";

    // Parallel fetching configuration
    private const int MaxConcurrentRequests = 10;
    private const int BatchSize = 50;
    private const int ChunkInsertSize = 100; // Insert chunks to reduce memory pressure

    public DataSyncService(
        IDnd5eApiClient dnd5eApi,
        IOpen5eApiClient open5eApi,
        IDatabaseService database,
        IMonsterRepository monsterRepo,
        ISpellRepository spellRepo,
        IEquipmentRepository equipmentRepo,
        IMagicItemRepository magicItemRepo,
        IConditionRepository conditionRepo)
    {
        _dnd5eApi = dnd5eApi;
        _open5eApi = open5eApi;
        _database = database;
        _monsterRepo = monsterRepo;
        _spellRepo = spellRepo;
        _equipmentRepo = equipmentRepo;
        _magicItemRepo = magicItemRepo;
        _conditionRepo = conditionRepo;
    }

    public Task<bool> IsInitialSyncCompleteAsync()
    {
        return Task.FromResult(Preferences.Default.Get(INITIAL_SYNC_KEY, false));
    }

    public async Task PerformInitialSyncAsync(IProgress<SyncProgress>? progress = null)
    {
        try
        {
            ReportProgress(progress, 0, "Starting initial sync...", 0, 0);

            // Step 1: Download ALL monsters from D&D 5e API (with parallel fetching)
            ReportProgress(progress, 2, "Fetching monster list from D&D 5e API...", 0, 0);
            await DownloadDnd5eMonstersParallel(progress);

            // Step 2: Download ALL monsters from Open5e (full pagination)
            ReportProgress(progress, 30, "Fetching additional monsters from Open5e...", 0, 0);
            await DownloadOpen5eMonstersFullPagination(progress);

            // Step 3: Download ALL spells (with parallel fetching)
            ReportProgress(progress, 50, "Fetching spells...", 0, 0);
            await DownloadSpellsParallel(progress);

            // Step 4: Download ALL equipment (with details)
            ReportProgress(progress, 70, "Fetching equipment...", 0, 0);
            await DownloadEquipmentFull(progress);

            // Step 5: Download ALL magic items (with details)
            ReportProgress(progress, 82, "Fetching magic items...", 0, 0);
            await DownloadMagicItemsFull(progress);

            // Step 6: Download conditions
            ReportProgress(progress, 94, "Fetching conditions...", 0, 0);
            await DownloadConditions(progress);

            // Mark sync as complete
            Preferences.Default.Set(INITIAL_SYNC_KEY, true);
            Preferences.Default.Set(LAST_SYNC_KEY, DateTime.Now);

            ReportProgress(progress, 100, "Sync complete! Ready to use.", 0, 0, isComplete: true);
        }
        catch (Exception ex)
        {
            ReportProgress(progress, -1, $"Sync failed: {ex.Message}", 0, 0, hasError: true, errorMessage: ex.Message);
            throw;
        }
    }

    private async Task DownloadDnd5eMonstersParallel(IProgress<SyncProgress>? progress)
    {
        var monsterList = await _dnd5eApi.GetMonstersAsync();
        if (monsterList == null || monsterList.Results.Count == 0)
        {
            ReportProgress(progress, -1, "Failed to fetch monster list from D&D 5e API", 0, 0, hasError: true);
            return;
        }

        var totalMonsters = monsterList.Results.Count;
        var monsters = new List<Monster>();
        var completedCount = 0;
        var errorCount = 0;
        var insertedCount = 0;
        var lockObj = new object();

        System.Diagnostics.Debug.WriteLine($"Starting D&D 5e monster download: {totalMonsters} monsters");

        using var semaphore = new SemaphoreSlim(MaxConcurrentRequests);

        var tasks = monsterList.Results.Select(async monsterRef =>
        {
            await semaphore.WaitAsync();
            try
            {
                var monsterDetail = await _dnd5eApi.GetMonsterAsync(monsterRef.Index);
                if (monsterDetail != null)
                {
                    var monster = new Monster
                    {
                        ApiId = monsterDetail.Index,
                        Name = monsterDetail.Name,
                        Size = monsterDetail.Size,
                        Type = monsterDetail.Type,
                        Alignment = monsterDetail.Alignment,
                        ArmorClass = monsterDetail.ArmorClass?.FirstOrDefault()?.Value ?? 10,
                        HitPoints = monsterDetail.HitPoints,
                        HitDice = monsterDetail.HitDice ?? string.Empty,
                        ChallengeRating = monsterDetail.ChallengeRating,
                        ExperiencePoints = monsterDetail.Xp,
                        Strength = monsterDetail.Strength,
                        Dexterity = monsterDetail.Dexterity,
                        Constitution = monsterDetail.Constitution,
                        Intelligence = monsterDetail.Intelligence,
                        Wisdom = monsterDetail.Wisdom,
                        Charisma = monsterDetail.Charisma,
                        SpeedsJson = JsonSerializer.Serialize(monsterDetail.Speed ?? new Dictionary<string, string>()),
                        ActionsJson = JsonSerializer.Serialize(monsterDetail.Actions ?? new List<Dnd5eAction>()),
                        SpecialAbilitiesJson = JsonSerializer.Serialize(monsterDetail.SpecialAbilities ?? new List<Dnd5eSpecialAbility>()),
                        Source = "dnd5eapi",
                        LastUpdated = DateTime.Now
                    };

                    List<Monster>? chunkToInsert = null;
                    lock (lockObj)
                    {
                        monsters.Add(monster);
                        completedCount++;

                        // Chunked insert: when we have enough items, take them out and insert
                        if (monsters.Count >= ChunkInsertSize)
                        {
                            chunkToInsert = new List<Monster>(monsters);
                            monsters.Clear();
                        }

                        if (completedCount % 10 == 0 || completedCount == totalMonsters)
                        {
                            var progressPercent = 2 + (18 * completedCount / totalMonsters);
                            ReportProgress(progress, progressPercent,
                                $"D&D 5e monsters: {completedCount}/{totalMonsters}",
                                completedCount, totalMonsters);
                        }
                    }

                    // Insert chunk outside of lock to avoid blocking other tasks
                    if (chunkToInsert != null)
                    {
                        await _monsterRepo.BulkInsertAsync(chunkToInsert);
                        Interlocked.Add(ref insertedCount, chunkToInsert.Count);
                        System.Diagnostics.Debug.WriteLine($"D&D 5e: Inserted chunk of {chunkToInsert.Count} monsters");
                    }
                }
            }
            catch (Exception ex)
            {
                lock (lockObj)
                {
                    errorCount++;
                    completedCount++;
                }
                System.Diagnostics.Debug.WriteLine($"Error downloading monster '{monsterRef.Index}': {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        // Insert any remaining monsters
        if (monsters.Count > 0)
        {
            await _monsterRepo.BulkInsertAsync(monsters);
            insertedCount += monsters.Count;
        }

        System.Diagnostics.Debug.WriteLine($"Downloaded {insertedCount} monsters from D&D 5e API ({errorCount} errors)");
    }

    private async Task DownloadOpen5eMonstersFullPagination(IProgress<SyncProgress>? progress)
    {
        var monsters = new List<Monster>();
        // Use slug (unique API ID) for duplicate detection, not name
        var existingSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var errorCount = 0;

        // Get existing monster slugs/ApiIds to avoid true duplicates
        var allExisting = await _monsterRepo.GetAllAsync();
        foreach (var m in allExisting)
        {
            existingSlugs.Add(m.ApiId);
        }

        System.Diagnostics.Debug.WriteLine($"Open5e: Found {existingSlugs.Count} existing monsters in database");

        int page = 1;
        int totalFetched = 0;
        int totalAvailable = 0;
        int skippedDuplicates = 0;
        int totalProcessed = 0;

        // Fetch ALL pages with proper termination
        while (true)
        {
            try
            {
                var response = await _open5eApi.GetMonstersAsync(page, 100);
                if (response == null || response.Results.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Open5e: No results on page {page}, stopping pagination");
                    break;
                }

                if (totalAvailable == 0)
                {
                    totalAvailable = response.Count;
                    System.Diagnostics.Debug.WriteLine($"Open5e: Total available monsters = {totalAvailable}");
                }

                foreach (var open5eMonster in response.Results)
                {
                    totalProcessed++;

                    try
                    {
                        // Generate the slug/ApiId for this monster
                        var slug = open5eMonster.Slug ?? open5eMonster.Name.ToLowerInvariant().Replace(" ", "-");

                        // Skip if this exact monster (by slug) already exists
                        if (existingSlugs.Contains(slug))
                        {
                            skippedDuplicates++;
                            continue;
                        }

                        // Use the CR field if available, otherwise parse the string
                        double cr = open5eMonster.CR ?? 0;
                        if (cr == 0 && !string.IsNullOrEmpty(open5eMonster.ChallengeRating))
                        {
                            // Handle fractional CRs like "1/4", "1/2"
                            if (open5eMonster.ChallengeRating.Contains('/'))
                            {
                                var parts = open5eMonster.ChallengeRating.Split('/');
                                if (parts.Length == 2 && double.TryParse(parts[0], out var num) && double.TryParse(parts[1], out var denom))
                                    cr = num / denom;
                            }
                            else
                            {
                                double.TryParse(open5eMonster.ChallengeRating, out cr);
                            }
                        }

                        // Serialize speed - handle the JsonElement properly
                        string speedJson = "{}";
                        if (open5eMonster.Speed.HasValue)
                        {
                            speedJson = open5eMonster.Speed.Value.GetRawText();
                        }

                        // Serialize actions and special abilities
                        string actionsJson = open5eMonster.Actions != null
                            ? JsonSerializer.Serialize(open5eMonster.Actions)
                            : "[]";
                        string specialAbilitiesJson = open5eMonster.SpecialAbilities != null
                            ? JsonSerializer.Serialize(open5eMonster.SpecialAbilities)
                            : "[]";
                        string legendaryActionsJson = open5eMonster.LegendaryActions != null
                            ? JsonSerializer.Serialize(open5eMonster.LegendaryActions)
                            : "[]";

                        var monster = new Monster
                        {
                            ApiId = slug,
                            Name = open5eMonster.Name,
                            Size = open5eMonster.Size ?? "Medium",
                            Type = open5eMonster.Type ?? "Unknown",
                            Alignment = open5eMonster.Alignment ?? "Unaligned",
                            ArmorClass = open5eMonster.ArmorClass,
                            HitPoints = open5eMonster.HitPoints,
                            HitDice = open5eMonster.HitDice ?? string.Empty,
                            ChallengeRating = cr,
                            ExperiencePoints = CalculateXPFromCR(cr),
                            Strength = open5eMonster.Strength,
                            Dexterity = open5eMonster.Dexterity,
                            Constitution = open5eMonster.Constitution,
                            Intelligence = open5eMonster.Intelligence,
                            Wisdom = open5eMonster.Wisdom,
                            Charisma = open5eMonster.Charisma,
                            SpeedsJson = speedJson,
                            ActionsJson = actionsJson,
                            SpecialAbilitiesJson = specialAbilitiesJson,
                            LegendaryActionsJson = legendaryActionsJson,
                            DamageResistances = open5eMonster.DamageResistances ?? string.Empty,
                            DamageImmunities = open5eMonster.DamageImmunities ?? string.Empty,
                            DamageVulnerabilities = open5eMonster.DamageVulnerabilities ?? string.Empty,
                            ConditionImmunities = open5eMonster.ConditionImmunities ?? string.Empty,
                            Senses = open5eMonster.Senses ?? string.Empty,
                            Languages = open5eMonster.Languages ?? string.Empty,
                            Description = open5eMonster.Description ?? string.Empty,
                            ImageUrl = open5eMonster.ImageUrl ?? string.Empty,
                            Source = $"open5e:{open5eMonster.DocumentSlug}",
                            LastUpdated = DateTime.Now
                        };

                        monsters.Add(monster);
                        existingSlugs.Add(slug); // Track to avoid duplicates within this sync
                        totalFetched++;

                        // Chunked insert to reduce memory pressure during large syncs
                        if (monsters.Count >= ChunkInsertSize)
                        {
                            await _monsterRepo.BulkInsertAsync(monsters);
                            System.Diagnostics.Debug.WriteLine($"Open5e: Inserted chunk of {monsters.Count} monsters");
                            monsters.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        System.Diagnostics.Debug.WriteLine($"Open5e: Error processing monster '{open5eMonster.Name}': {ex.Message}");
                    }
                }

                var progressPercent = 30 + (20 * totalProcessed / Math.Max(totalAvailable, 1));
                ReportProgress(progress, progressPercent,
                    $"Open5e monsters... Page {page}/{(totalAvailable + 99) / 100}, {totalFetched} new",
                    totalFetched, totalAvailable);

                System.Diagnostics.Debug.WriteLine($"Open5e: Page {page} done, totalProcessed={totalProcessed}, totalFetched={totalFetched}");

                // STOP when we've processed all available items OR no more pages
                if (totalProcessed >= totalAvailable || string.IsNullOrEmpty(response.Next))
                {
                    System.Diagnostics.Debug.WriteLine($"Open5e: Pagination complete. Processed {totalProcessed} of {totalAvailable}");
                    break;
                }

                page++;
                await Task.Delay(50); // Small delay to be nice to the API
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Open5e: Error fetching page {page}: {ex.Message}");
                errorCount++;

                // Safety limit - stop after too many consecutive errors
                if (errorCount > 5)
                {
                    System.Diagnostics.Debug.WriteLine("Open5e: Too many errors, stopping");
                    break;
                }

                page++; // Try next page
            }
        }

        // Insert any remaining monsters (chunked insert happens during pagination)
        if (monsters.Count > 0)
        {
            await _monsterRepo.BulkInsertAsync(monsters);
            totalFetched += monsters.Count - (monsters.Count % ChunkInsertSize == 0 ? 0 : monsters.Count);
        }

        System.Diagnostics.Debug.WriteLine($"Open5e: Downloaded {totalFetched} monsters, skipped {skippedDuplicates} duplicates, {errorCount} errors");
    }

    private async Task DownloadSpellsParallel(IProgress<SyncProgress>? progress)
    {
        var spellList = await _dnd5eApi.GetSpellsAsync();
        if (spellList == null || spellList.Results.Count == 0)
        {
            ReportProgress(progress, -1, "Failed to fetch spell list from D&D 5e API", 0, 0, hasError: true);
            return;
        }

        var totalSpells = spellList.Results.Count;
        var spells = new List<Spell>();
        var completedCount = 0;
        var errorCount = 0;
        var insertedCount = 0;
        var lockObj = new object();

        System.Diagnostics.Debug.WriteLine($"Starting spell download: {totalSpells} spells");

        using var semaphore = new SemaphoreSlim(MaxConcurrentRequests);

        var tasks = spellList.Results.Select(async spellRef =>
        {
            await semaphore.WaitAsync();
            try
            {
                var spellDetail = await _dnd5eApi.GetSpellAsync(spellRef.Index);
                if (spellDetail != null)
                {
                    var spell = new Spell
                    {
                        ApiId = spellDetail.Index,
                        Name = spellDetail.Name,
                        Level = spellDetail.Level,
                        School = spellDetail.School?.Name ?? "Unknown",
                        CastingTime = spellDetail.CastingTime ?? string.Empty,
                        Range = spellDetail.Range ?? string.Empty,
                        Components = string.Join(", ", spellDetail.Components ?? new List<string>()),
                        Duration = spellDetail.Duration ?? string.Empty,
                        Description = string.Join("\n\n", spellDetail.Desc ?? new List<string>()),
                        HigherLevels = spellDetail.HigherLevel?.Count > 0 ? string.Join("\n", spellDetail.HigherLevel) : string.Empty,
                        ClassesJson = JsonSerializer.Serialize(spellDetail.Classes?.Select(c => c.Name).ToList() ?? new List<string>()),
                        Source = "dnd5eapi",
                        LastUpdated = DateTime.Now
                    };

                    List<Spell>? chunkToInsert = null;
                    lock (lockObj)
                    {
                        spells.Add(spell);
                        completedCount++;

                        // Chunked insert to reduce memory pressure
                        if (spells.Count >= ChunkInsertSize)
                        {
                            chunkToInsert = new List<Spell>(spells);
                            spells.Clear();
                        }

                        if (completedCount % 20 == 0 || completedCount == totalSpells)
                        {
                            var progressPercent = 50 + (20 * completedCount / totalSpells);
                            ReportProgress(progress, progressPercent,
                                $"Downloading spells... {completedCount}/{totalSpells}",
                                completedCount, totalSpells);
                        }
                    }

                    if (chunkToInsert != null)
                    {
                        await _spellRepo.BulkInsertAsync(chunkToInsert);
                        Interlocked.Add(ref insertedCount, chunkToInsert.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                lock (lockObj)
                {
                    errorCount++;
                    completedCount++;
                }
                System.Diagnostics.Debug.WriteLine($"Error downloading spell '{spellRef.Index}': {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        // Insert any remaining spells
        if (spells.Count > 0)
        {
            await _spellRepo.BulkInsertAsync(spells);
            insertedCount += spells.Count;
        }

        System.Diagnostics.Debug.WriteLine($"Downloaded {insertedCount} spells from D&D 5e API ({errorCount} errors)");
    }

    private async Task DownloadEquipmentFull(IProgress<SyncProgress>? progress)
    {
        var equipmentList = await _dnd5eApi.GetEquipmentAsync();
        if (equipmentList == null || equipmentList.Results.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("Failed to get equipment list from API");
            return;
        }

        var totalItems = equipmentList.Results.Count;
        var equipmentItems = new List<Equipment>();
        var completedCount = 0;
        var errorCount = 0;
        var lockObj = new object();

        System.Diagnostics.Debug.WriteLine($"Starting equipment download: {totalItems} items");

        using var semaphore = new SemaphoreSlim(MaxConcurrentRequests);

        var tasks = equipmentList.Results.Select(async itemRef =>
        {
            await semaphore.WaitAsync();
            try
            {
                var detail = await _dnd5eApi.GetEquipmentDetailAsync(itemRef.Index);

                var item = new Equipment
                {
                    ApiId = itemRef.Index,
                    Name = itemRef.Name,
                    EquipmentCategory = detail?.EquipmentCategory?.Name ?? "Unknown",
                    WeaponCategory = detail?.WeaponCategory ?? string.Empty,
                    WeaponRange = detail?.WeaponRange ?? string.Empty,
                    ArmorCategory = detail?.ArmorCategory ?? string.Empty,
                    Cost = detail?.Cost?.Quantity ?? 0,
                    CostCurrency = detail?.Cost?.Unit ?? "gp",
                    Weight = detail?.Weight ?? 0,
                    DescriptionJson = JsonSerializer.Serialize(detail?.Desc ?? new List<string>()),
                    PropertiesJson = JsonSerializer.Serialize(detail?.Properties?.Select(p => p.Name).ToList() ?? new List<string>()),
                    Source = "dnd5eapi",
                    LastUpdated = DateTime.Now
                };

                lock (lockObj)
                {
                    equipmentItems.Add(item);
                    completedCount++;

                    if (completedCount % 20 == 0 || completedCount == totalItems)
                    {
                        var progressPercent = 70 + (12 * completedCount / totalItems);
                        ReportProgress(progress, progressPercent,
                            $"Downloading equipment... {completedCount}/{totalItems}",
                            completedCount, totalItems);
                    }
                }
            }
            catch (Exception ex)
            {
                lock (lockObj)
                {
                    errorCount++;
                    completedCount++;
                }
                System.Diagnostics.Debug.WriteLine($"Error downloading equipment '{itemRef.Index}': {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        if (equipmentItems.Count > 0)
            await _equipmentRepo.BulkInsertAsync(equipmentItems);

        System.Diagnostics.Debug.WriteLine($"Downloaded {equipmentItems.Count} equipment items ({errorCount} errors)");
    }

    private async Task DownloadMagicItemsFull(IProgress<SyncProgress>? progress)
    {
        var itemList = await _dnd5eApi.GetMagicItemsAsync();
        if (itemList == null || itemList.Results.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("Failed to get magic items list from API");
            return;
        }

        var totalItems = itemList.Results.Count; // Fetch ALL - no limit!
        var magicItems = new List<MagicItem>();
        var completedCount = 0;
        var errorCount = 0;
        var lockObj = new object();

        System.Diagnostics.Debug.WriteLine($"Starting magic items download: {totalItems} items");

        using var semaphore = new SemaphoreSlim(MaxConcurrentRequests);

        var tasks = itemList.Results.Select(async itemRef =>
        {
            await semaphore.WaitAsync();
            try
            {
                // Fetch detailed magic item info
                var detail = await _dnd5eApi.GetMagicItemDetailAsync(itemRef.Index);

                var item = new MagicItem
                {
                    ApiId = itemRef.Index,
                    Name = itemRef.Name,
                    Rarity = detail?.Rarity?.Name ?? "Unknown",
                    Type = detail?.EquipmentCategory?.Name ?? "Wondrous Item",
                    RequiresAttunement = detail?.Desc?.Any(d => d.Contains("attunement", StringComparison.OrdinalIgnoreCase)) ?? false,
                    Description = detail?.Desc != null ? string.Join("\n\n", detail.Desc) : string.Empty,
                    Source = "dnd5eapi",
                    LastUpdated = DateTime.Now
                };

                lock (lockObj)
                {
                    magicItems.Add(item);
                    completedCount++;

                    if (completedCount % 20 == 0 || completedCount == totalItems)
                    {
                        var progressPercent = 82 + (12 * completedCount / totalItems);
                        ReportProgress(progress, progressPercent,
                            $"Downloading magic items... {completedCount}/{totalItems}",
                            completedCount, totalItems);
                    }
                }
            }
            catch (Exception ex)
            {
                lock (lockObj)
                {
                    errorCount++;
                    completedCount++;
                }
                System.Diagnostics.Debug.WriteLine($"Error downloading magic item '{itemRef.Index}': {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        if (magicItems.Count > 0)
            await _magicItemRepo.BulkInsertAsync(magicItems);

        System.Diagnostics.Debug.WriteLine($"Downloaded {magicItems.Count} magic items from D&D 5e API ({errorCount} errors)");
    }

    private async Task DownloadConditions(IProgress<SyncProgress>? progress)
    {
        var conditionList = await _dnd5eApi.GetConditionsAsync();
        if (conditionList == null || conditionList.Results.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("Failed to get conditions list from API");
            return;
        }

        var totalConditions = conditionList.Results.Count;
        var conditions = new List<DndCondition>();
        var completedCount = 0;
        var errorCount = 0;
        var lockObj = new object();

        System.Diagnostics.Debug.WriteLine($"Starting conditions download: {totalConditions} conditions");

        using var semaphore = new SemaphoreSlim(MaxConcurrentRequests);

        var tasks = conditionList.Results.Select(async conditionRef =>
        {
            await semaphore.WaitAsync();
            try
            {
                var conditionDetail = await _dnd5eApi.GetConditionAsync(conditionRef.Index);
                if (conditionDetail != null)
                {
                    var condition = new DndCondition
                    {
                        ApiId = conditionDetail.Index,
                        Name = conditionDetail.Name,
                        Description = string.Join("\n\n", conditionDetail.Desc ?? new List<string>()),
                        Source = "dnd5eapi",
                        LastUpdated = DateTime.Now
                    };

                    lock (lockObj)
                    {
                        conditions.Add(condition);
                        completedCount++;

                        var progressPercent = 94 + (6 * completedCount / totalConditions);
                        ReportProgress(progress, progressPercent,
                            $"Downloading conditions... {completedCount}/{totalConditions}",
                            completedCount, totalConditions);
                    }
                }
            }
            catch (Exception ex)
            {
                lock (lockObj)
                {
                    errorCount++;
                    completedCount++;
                }
                System.Diagnostics.Debug.WriteLine($"Error downloading condition '{conditionRef.Index}': {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        if (conditions.Count > 0)
            await _conditionRepo.BulkInsertAsync(conditions);

        System.Diagnostics.Debug.WriteLine($"Downloaded {conditions.Count} conditions from D&D 5e API ({errorCount} errors)");
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            var monsterCount = await _monsterRepo.GetTotalCountAsync();
            var spellCount = await _spellRepo.GetTotalCountAsync();

            if (monsterCount == 0 || spellCount == 0)
            {
                System.Diagnostics.Debug.WriteLine("CheckForUpdatesAsync: No data found, update needed.");
                return true;
            }

            var lastSyncTime = Preferences.Default.Get<DateTime>(LAST_SYNC_KEY, DateTime.MinValue);
            if (lastSyncTime == DateTime.MinValue)
            {
                System.Diagnostics.Debug.WriteLine("CheckForUpdatesAsync: No last sync time found, update needed.");
                return true;
            }

            var daysSinceSync = (DateTime.Now - lastSyncTime).TotalDays;
            System.Diagnostics.Debug.WriteLine($"CheckForUpdatesAsync: Last sync was {daysSinceSync:F1} days ago.");

            return daysSinceSync > 7;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking for updates: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Force a complete re-sync of all data, clearing existing data first.
    /// </summary>
    public async Task ForceResyncAsync(IProgress<SyncProgress>? progress = null)
    {
        ReportProgress(progress, 0, "Clearing existing data...", 0, 0);

        // Actually clear the database tables before re-syncing
        try
        {
            await _database.DeleteAllAsync<Monster>();
            await _database.DeleteAllAsync<Spell>();
            await _database.DeleteAllAsync<Equipment>();
            await _database.DeleteAllAsync<MagicItem>();
            await _database.DeleteAllAsync<DndCondition>();
            System.Diagnostics.Debug.WriteLine("Cleared all sync data from database");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing database: {ex.Message}");
        }

        // Clear preferences to force a fresh sync
        Preferences.Default.Remove(INITIAL_SYNC_KEY);
        Preferences.Default.Remove(LAST_SYNC_KEY);

        await PerformInitialSyncAsync(progress);
    }

    private void ReportProgress(IProgress<SyncProgress>? progress, int percentage, string message,
        int currentItem, int totalItems, bool isComplete = false, bool hasError = false, string? errorMessage = null)
    {
        progress?.Report(new SyncProgress
        {
            Percentage = percentage,
            Message = message,
            CurrentItem = currentItem,
            TotalItems = totalItems,
            IsComplete = isComplete,
            HasError = hasError,
            ErrorMessage = errorMessage
        });
    }

    private int CalculateXPFromCR(double cr)
    {
        return cr switch
        {
            0 => 10,
            0.125 => 25,
            0.25 => 50,
            0.5 => 100,
            1 => 200,
            2 => 450,
            3 => 700,
            4 => 1100,
            5 => 1800,
            6 => 2300,
            7 => 2900,
            8 => 3900,
            9 => 5000,
            10 => 5900,
            11 => 7200,
            12 => 8400,
            13 => 10000,
            14 => 11500,
            15 => 13000,
            16 => 15000,
            17 => 18000,
            18 => 20000,
            19 => 22000,
            20 => 25000,
            21 => 33000,
            22 => 41000,
            23 => 50000,
            24 => 62000,
            >= 25 => 75000,
            _ => 0
        };
    }

    #region Individual Sync Methods

    public event EventHandler<DataSyncProgressEventArgs>? ProgressChanged;
    public DataSyncResult? LastSyncResult { get; private set; }

    private void RaiseProgressChanged(double progress, string message, int itemsProcessed = 0, int totalItems = 0, string operation = "")
    {
        ProgressChanged?.Invoke(this, new DataSyncProgressEventArgs
        {
            Progress = progress,
            Message = message,
            ItemsProcessed = itemsProcessed,
            TotalItems = totalItems,
            CurrentOperation = operation
        });
    }

    public async Task SyncMonstersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LastSyncResult = new DataSyncResult();
            RaiseProgressChanged(0, "Starting monster sync...", operation: "Monsters");

            // Sync from D&D 5e API
            var monsterList = await _dnd5eApi.GetMonstersAsync();
            if (monsterList?.Results != null)
            {
                var count = monsterList.Results.Count;
                RaiseProgressChanged(0.1, $"Fetching {count} monsters from D&D 5e API...", 0, count, "Monsters");

                var existingApiIds = (await _monsterRepo.GetAllAsync()).Select(m => m.ApiId).ToHashSet();
                var newMonsters = new List<Monster>();
                var completed = 0;

                using var semaphore = new SemaphoreSlim(MaxConcurrentRequests);
                var lockObj = new object();

                var tasks = monsterList.Results.Select(async monsterRef =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        if (existingApiIds.Contains(monsterRef.Index)) return;

                        var monsterDetail = await _dnd5eApi.GetMonsterAsync(monsterRef.Index);
                        if (monsterDetail != null)
                        {
                            var monster = new Monster
                            {
                                ApiId = monsterDetail.Index,
                                Name = monsterDetail.Name,
                                Size = monsterDetail.Size,
                                Type = monsterDetail.Type,
                                Alignment = monsterDetail.Alignment,
                                ArmorClass = monsterDetail.ArmorClass?.FirstOrDefault()?.Value ?? 10,
                                HitPoints = monsterDetail.HitPoints,
                                HitDice = monsterDetail.HitDice ?? string.Empty,
                                ChallengeRating = monsterDetail.ChallengeRating,
                                ExperiencePoints = CalculateXPFromCR(monsterDetail.ChallengeRating),
                                Strength = monsterDetail.Strength,
                                Dexterity = monsterDetail.Dexterity,
                                Constitution = monsterDetail.Constitution,
                                Intelligence = monsterDetail.Intelligence,
                                Wisdom = monsterDetail.Wisdom,
                                Charisma = monsterDetail.Charisma,
                                SpeedsJson = JsonSerializer.Serialize(monsterDetail.Speed),
                                SkillsJson = monsterDetail.Proficiencies != null ? JsonSerializer.Serialize(monsterDetail.Proficiencies.Where(p => p.Proficiency?.Name?.StartsWith("Skill:") == true)) : "[]",
                                SavingThrowsJson = monsterDetail.Proficiencies != null ? JsonSerializer.Serialize(monsterDetail.Proficiencies.Where(p => p.Proficiency?.Name?.StartsWith("Saving Throw:") == true)) : "[]",
                                ActionsJson = monsterDetail.Actions != null ? JsonSerializer.Serialize(monsterDetail.Actions) : "[]",
                                SpecialAbilitiesJson = monsterDetail.SpecialAbilities != null ? JsonSerializer.Serialize(monsterDetail.SpecialAbilities) : "[]",
                                Source = "dnd5eapi",
                                LastUpdated = DateTime.Now
                            };

                            lock (lockObj)
                            {
                                newMonsters.Add(monster);
                                completed++;
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

                if (newMonsters.Count > 0)
                {
                    await _monsterRepo.BulkInsertAsync(newMonsters);
                    LastSyncResult.MonstersAdded = newMonsters.Count;
                }
            }

            RaiseProgressChanged(1.0, $"Monster sync complete. Added {LastSyncResult.MonstersAdded} monsters.", operation: "Monsters");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing monsters: {ex.Message}");
            LastSyncResult ??= new DataSyncResult();
            LastSyncResult.Errors++;
        }
    }

    public async Task SyncSpellsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LastSyncResult ??= new DataSyncResult();
            RaiseProgressChanged(0, "Starting spell sync...", operation: "Spells");

            var spellList = await _dnd5eApi.GetSpellsAsync();
            if (spellList?.Results != null)
            {
                var count = spellList.Results.Count;
                RaiseProgressChanged(0.1, $"Fetching {count} spells...", 0, count, "Spells");

                var existingApiIds = (await _spellRepo.GetAllAsync()).Select(s => s.ApiId).ToHashSet();
                var newSpells = new List<Spell>();
                var completed = 0;

                using var semaphore = new SemaphoreSlim(MaxConcurrentRequests);
                var lockObj = new object();

                var tasks = spellList.Results.Select(async spellRef =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        if (existingApiIds.Contains(spellRef.Index)) return;

                        var spellDetail = await _dnd5eApi.GetSpellAsync(spellRef.Index);
                        if (spellDetail != null)
                        {
                            var spell = new Spell
                            {
                                ApiId = spellDetail.Index,
                                Name = spellDetail.Name,
                                Level = spellDetail.Level,
                                School = spellDetail.School?.Name ?? "Unknown",
                                CastingTime = spellDetail.CastingTime ?? "",
                                Range = spellDetail.Range ?? "",
                                Components = string.Join(", ", spellDetail.Components ?? new List<string>()),
                                Duration = spellDetail.Duration ?? "",
                                Description = string.Join("\n\n", spellDetail.Desc ?? new List<string>()),
                                HigherLevels = string.Join("\n\n", spellDetail.HigherLevel ?? new List<string>()),
                                ClassesJson = JsonSerializer.Serialize(spellDetail.Classes?.Select(c => c.Name) ?? new List<string>()),
                                Source = "dnd5eapi",
                                LastUpdated = DateTime.Now
                            };

                            lock (lockObj)
                            {
                                newSpells.Add(spell);
                                completed++;
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

                if (newSpells.Count > 0)
                {
                    await _spellRepo.BulkInsertAsync(newSpells);
                    LastSyncResult.SpellsAdded = newSpells.Count;
                }
            }

            RaiseProgressChanged(1.0, $"Spell sync complete. Added {LastSyncResult.SpellsAdded} spells.", operation: "Spells");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing spells: {ex.Message}");
            LastSyncResult ??= new DataSyncResult();
            LastSyncResult.Errors++;
        }
    }

    public async Task SyncEquipmentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LastSyncResult ??= new DataSyncResult();
            RaiseProgressChanged(0, "Starting equipment sync...", operation: "Equipment");

            var equipmentList = await _dnd5eApi.GetEquipmentAsync();
            if (equipmentList?.Results != null)
            {
                var count = equipmentList.Results.Count;
                RaiseProgressChanged(0.1, $"Fetching {count} equipment items...", 0, count, "Equipment");

                var existingApiIds = (await _equipmentRepo.GetAllAsync()).Select(e => e.ApiId).ToHashSet();
                var newItems = new List<Equipment>();
                var completed = 0;

                using var semaphore = new SemaphoreSlim(MaxConcurrentRequests);
                var lockObj = new object();

                var tasks = equipmentList.Results.Select(async itemRef =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        if (existingApiIds.Contains(itemRef.Index)) return;

                        var detail = await _dnd5eApi.GetEquipmentDetailAsync(itemRef.Index);
                        if (detail != null)
                        {
                            var equipment = new Equipment
                            {
                                ApiId = itemRef.Index,
                                Name = itemRef.Name,
                                EquipmentCategory = detail.EquipmentCategory?.Name ?? "Gear",
                                WeaponCategory = detail.WeaponCategory ?? "",
                                WeaponRange = detail.WeaponRange ?? "",
                                ArmorCategory = detail.ArmorCategory ?? "",
                                Cost = detail.Cost?.Quantity ?? 0,
                                CostCurrency = detail.Cost?.Unit ?? "gp",
                                Weight = detail.Weight,
                                DescriptionJson = detail.Desc != null ? JsonSerializer.Serialize(detail.Desc) : "[]",
                                PropertiesJson = detail.Properties != null ? JsonSerializer.Serialize(detail.Properties.Select(p => p.Name)) : "[]",
                                Source = "dnd5eapi",
                                LastUpdated = DateTime.Now
                            };

                            lock (lockObj)
                            {
                                newItems.Add(equipment);
                                completed++;
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

                if (newItems.Count > 0)
                {
                    await _equipmentRepo.BulkInsertAsync(newItems);
                    LastSyncResult.EquipmentAdded = newItems.Count;
                }
            }

            RaiseProgressChanged(1.0, $"Equipment sync complete. Added {LastSyncResult.EquipmentAdded} items.", operation: "Equipment");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing equipment: {ex.Message}");
            LastSyncResult ??= new DataSyncResult();
            LastSyncResult.Errors++;
        }
    }

    public async Task SyncMagicItemsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LastSyncResult ??= new DataSyncResult();
            RaiseProgressChanged(0, "Starting magic items sync...", operation: "Magic Items");

            var itemList = await _dnd5eApi.GetMagicItemsAsync();
            if (itemList?.Results != null)
            {
                var count = itemList.Results.Count;
                RaiseProgressChanged(0.1, $"Fetching {count} magic items...", 0, count, "Magic Items");

                var existingApiIds = (await _magicItemRepo.GetAllAsync()).Select(m => m.ApiId).ToHashSet();
                var newItems = new List<MagicItem>();
                var completed = 0;

                using var semaphore = new SemaphoreSlim(MaxConcurrentRequests);
                var lockObj = new object();

                var tasks = itemList.Results.Select(async itemRef =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        if (existingApiIds.Contains(itemRef.Index)) return;

                        var detail = await _dnd5eApi.GetMagicItemDetailAsync(itemRef.Index);
                        var item = new MagicItem
                        {
                            ApiId = itemRef.Index,
                            Name = itemRef.Name,
                            Rarity = detail?.Rarity?.Name ?? "Unknown",
                            Type = detail?.EquipmentCategory?.Name ?? "Wondrous Item",
                            RequiresAttunement = detail?.Desc?.Any(d => d.Contains("attunement", StringComparison.OrdinalIgnoreCase)) ?? false,
                            Description = detail?.Desc != null ? string.Join("\n\n", detail.Desc) : string.Empty,
                            Source = "dnd5eapi",
                            LastUpdated = DateTime.Now
                        };

                        lock (lockObj)
                        {
                            newItems.Add(item);
                            completed++;
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

                if (newItems.Count > 0)
                {
                    await _magicItemRepo.BulkInsertAsync(newItems);
                    LastSyncResult.MagicItemsAdded = newItems.Count;
                }
            }

            RaiseProgressChanged(1.0, $"Magic items sync complete. Added {LastSyncResult.MagicItemsAdded} items.", operation: "Magic Items");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing magic items: {ex.Message}");
            LastSyncResult ??= new DataSyncResult();
            LastSyncResult.Errors++;
        }
    }

    #endregion
}
