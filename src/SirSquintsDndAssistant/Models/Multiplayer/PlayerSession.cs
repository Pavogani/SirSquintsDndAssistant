using SQLite;

namespace SirSquintsDndAssistant.Models.Multiplayer;

/// <summary>
/// Represents a shared game session for multiplayer functionality.
/// </summary>
public class GameSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string SessionCode { get; set; } = string.Empty; // 6-character code for joining
    public string Name { get; set; } = string.Empty;

    public int CampaignId { get; set; }
    public int? ActiveCombatId { get; set; }
    public int? ActiveMapId { get; set; }

    // Session state
    public SessionState State { get; set; } = SessionState.Lobby;

    // Display settings (what players can see)
    public bool ShowInitiativeOrder { get; set; } = true;
    public bool ShowMonsterHP { get; set; }
    public bool ShowMonsterNames { get; set; } = true;
    public bool ShowBattleMap { get; set; } = true;
    public bool ShowFogOfWar { get; set; } = true;

    // DM broadcast message
    public string BroadcastMessage { get; set; } = string.Empty;
    public string BroadcastTitle { get; set; } = string.Empty;

    // Timing
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastActivityAt { get; set; } = DateTime.Now;
    public DateTime? EndedAt { get; set; }

    // Metadata
    public string DmName { get; set; } = string.Empty;
    public int MaxPlayers { get; set; } = 6;
}

public enum SessionState
{
    Lobby,
    Active,
    Paused,
    Ended
}

/// <summary>
/// Represents a player connected to a game session.
/// </summary>
public class SessionPlayer
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int GameSessionId { get; set; }
    public int? PlayerCharacterId { get; set; }

    public string PlayerName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // Connection status
    public bool IsConnected { get; set; }
    public bool IsReady { get; set; }

    // Permissions
    public bool CanRollDice { get; set; } = true;
    public bool CanEditCharacter { get; set; } = true;
    public bool CanSeeOtherCharacters { get; set; } = true;

    // Activity
    public DateTime JoinedAt { get; set; } = DateTime.Now;
    public DateTime LastPingAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Shared dice roll visible to all players.
/// </summary>
public class SharedDiceRoll
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int GameSessionId { get; set; }
    public int? SessionPlayerId { get; set; }

    public string RollerName { get; set; } = string.Empty;
    public string RollDescription { get; set; } = string.Empty; // "Attack Roll", "Saving Throw", etc.
    public string DiceExpression { get; set; } = string.Empty; // "1d20+5"

    // Results
    public string IndividualRollsJson { get; set; } = "[]"; // [12, 5, 3]
    public int Modifier { get; set; }
    public int Total { get; set; }

    // Special cases
    public bool IsNatural20 { get; set; }
    public bool IsNatural1 { get; set; }
    public bool IsSecret { get; set; } // DM only

    public DateTime RolledAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Player dashboard state showing their character info and current combat status.
/// </summary>
public class PlayerDashboard
{
    // Character info (pulled from PlayerCharacter)
    public string CharacterName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int Level { get; set; }
    public int CurrentHP { get; set; }
    public int MaxHP { get; set; }
    public int TempHP { get; set; }
    public int ArmorClass { get; set; }

    // Ability scores
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Constitution { get; set; }
    public int Intelligence { get; set; }
    public int Wisdom { get; set; }
    public int Charisma { get; set; }

    // Combat state
    public bool IsInCombat { get; set; }
    public bool IsCurrentTurn { get; set; }
    public int InitiativeOrder { get; set; }
    public string CurrentConditions { get; set; } = string.Empty;

    // Spell slots (if caster)
    public bool HasSpellSlots { get; set; }
    public string SpellSlotsJson { get; set; } = "{}"; // { "1": [4, 2], "2": [3, 1] } = [max, current]

    // Resources
    public string ResourcesJson { get; set; } = "[]"; // Ki points, Rage uses, etc.

    // Notes from DM
    public string DmNote { get; set; } = string.Empty;
}
