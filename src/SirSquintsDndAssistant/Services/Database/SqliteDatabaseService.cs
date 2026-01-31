using SQLite;
using SirSquintsDndAssistant.Models.Campaign;
using SirSquintsDndAssistant.Models.Combat;
using SirSquintsDndAssistant.Models.Content;
using SirSquintsDndAssistant.Models.Creatures;
using SirSquintsDndAssistant.Models.Encounter;
using SirSquintsDndAssistant.Models.Homebrew;
using SirSquintsDndAssistant.Models.BattleMap;
using SirSquintsDndAssistant.Models.Multiplayer;
using DndCondition = SirSquintsDndAssistant.Models.Content.Condition;

namespace SirSquintsDndAssistant.Services.Database;

public class SqliteDatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;

    public SqliteDatabaseService()
    {
        // Set database path based on platform
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _dbPath = Path.Combine(documentsPath, "sirsquints_dnd_assistant.db3");
    }

    public async Task InitializeAsync()
    {
        if (_database != null)
            return;

        _database = new SQLiteAsyncConnection(_dbPath);

        // Create all tables
        await _database.CreateTableAsync<Monster>();
        await _database.CreateTableAsync<NPC>();
        await _database.CreateTableAsync<PlayerCharacter>();
        await _database.CreateTableAsync<Campaign>();
        await _database.CreateTableAsync<Session>();
        await _database.CreateTableAsync<Quest>();
        await _database.CreateTableAsync<Spell>();
        await _database.CreateTableAsync<Equipment>();
        await _database.CreateTableAsync<MagicItem>();
        await _database.CreateTableAsync<EncounterTemplate>();
        await _database.CreateTableAsync<CombatEncounter>();
        await _database.CreateTableAsync<InitiativeEntry>();
        await _database.CreateTableAsync<DndCondition>();
        await _database.CreateTableAsync<CombatLogEntry>();
        await _database.CreateTableAsync<SpellSlotTracker>();
        await _database.CreateTableAsync<StatusEffect>();
        await _database.CreateTableAsync<HomebrewMonster>();
        await _database.CreateTableAsync<HomebrewSpell>();
        await _database.CreateTableAsync<HomebrewItem>();
        await _database.CreateTableAsync<SessionPrepItem>();
        await _database.CreateTableAsync<WikiEntry>();
        await _database.CreateTableAsync<Models.BattleMap.BattleMap>();
        await _database.CreateTableAsync<MapToken>();
        await _database.CreateTableAsync<GameSession>();
        await _database.CreateTableAsync<SessionPlayer>();
        await _database.CreateTableAsync<SharedDiceRoll>();

        // Create indexes for performance
        await CreateIndexes();
    }

    private async Task CreateIndexes()
    {
        if (_database == null)
            return;

        try
        {
            // Monster indexes
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_monster_name ON Monster(Name)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_monster_cr ON Monster(ChallengeRating)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_monster_type ON Monster(Type)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_monster_favorite ON Monster(IsFavorite)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_monster_source ON Monster(Source)");

            // Composite index for common filter combinations
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_monster_type_cr ON Monster(Type, ChallengeRating)");

            // Spell indexes
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_spell_name ON Spell(Name)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_spell_level ON Spell(Level)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_spell_school ON Spell(School)");

            // Equipment indexes
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_equipment_name ON Equipment(Name)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_magicitem_name ON MagicItem(Name)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_magicitem_rarity ON MagicItem(Rarity)");

            // Campaign/Session indexes
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_session_campaign ON Session(CampaignId)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_quest_campaign ON Quest(CampaignId)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_npc_campaign ON NPC(CampaignId)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_playerchar_campaign ON PlayerCharacter(CampaignId)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_campaign_active ON Campaign(IsActive)");

            // Combat indexes
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_initiative_combat ON InitiativeEntry(CombatEncounterId)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_combat_active ON CombatEncounter(IsActive)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_initiative_sort ON InitiativeEntry(Initiative DESC, SortOrder)");

            // Encounter indexes
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_encounter_name ON EncounterTemplate(Name)");

            // Condition indexes
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_condition_name ON Condition(Name)");

            // Combat log indexes
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_combatlog_encounter ON CombatLogEntry(CombatEncounterId)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_combatlog_round ON CombatLogEntry(CombatEncounterId, Round)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_combatlog_timestamp ON CombatLogEntry(Timestamp)");

            // Spell slot tracker indexes
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_spellslot_initiative ON SpellSlotTracker(InitiativeEntryId)");

            // Status effect indexes
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_statuseffect_initiative ON StatusEffect(InitiativeEntryId)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_statuseffect_source ON StatusEffect(SourceName)");

            // Homebrew indexes
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_homebrew_monster_name ON HomebrewMonster(Name)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_homebrew_spell_name ON HomebrewSpell(Name)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_homebrew_spell_level ON HomebrewSpell(Level)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_homebrew_item_name ON HomebrewItem(Name)");

            // Session prep indexes
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_sessionprep_session ON SessionPrepItem(SessionId)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_sessionprep_campaign ON SessionPrepItem(CampaignId)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_wiki_campaign ON WikiEntry(CampaignId)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_wiki_category ON WikiEntry(CampaignId, Category)");

            // Battle map indexes
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_battlemap_name ON BattleMap(Name)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_maptoken_map ON MapToken(BattleMapId)");

            // Multiplayer indexes
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_gamesession_code ON GameSession(SessionCode)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_gamesession_state ON GameSession(State)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_sessionplayer_session ON SessionPlayer(GameSessionId)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_shareddiceroll_session ON SharedDiceRoll(GameSessionId)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating indexes: {ex.Message}");
        }
    }

    public SQLiteAsyncConnection GetConnection()
    {
        if (_database == null)
            throw new InvalidOperationException("Database not initialized. Call InitializeAsync first.");

        return _database;
    }

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_database == null)
            await InitializeAsync();

        return _database!;
    }

    public async Task<int> SaveItemAsync<T>(T item) where T : new()
    {
        if (_database == null)
            throw new InvalidOperationException("Database not initialized.");

        // Check if item has an Id property with value > 0 (update), otherwise insert
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null)
        {
            var id = (int?)idProperty.GetValue(item);
            if (id > 0)
                return await _database.UpdateAsync(item);
        }

        return await _database.InsertAsync(item);
    }

    public async Task<int> DeleteItemAsync<T>(T item) where T : new()
    {
        if (_database == null)
            throw new InvalidOperationException("Database not initialized.");

        return await _database.DeleteAsync(item);
    }

    public async Task<int> DeleteAllAsync<T>() where T : new()
    {
        if (_database == null)
            throw new InvalidOperationException("Database not initialized.");

        return await _database.DeleteAllAsync<T>();
    }

    public async Task<T?> GetItemAsync<T>(int id) where T : new()
    {
        if (_database == null)
            throw new InvalidOperationException("Database not initialized.");

        return await _database.GetAsync<T>(id);
    }

    public async Task<List<T>> GetItemsAsync<T>() where T : new()
    {
        if (_database == null)
            throw new InvalidOperationException("Database not initialized.");

        return await _database.Table<T>().ToListAsync();
    }
}
