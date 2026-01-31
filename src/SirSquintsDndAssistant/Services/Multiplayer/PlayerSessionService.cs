using SirSquintsDndAssistant.Models.Multiplayer;
using SirSquintsDndAssistant.Services.Database;

namespace SirSquintsDndAssistant.Services.Multiplayer;

public interface IPlayerSessionService
{
    // Game Sessions
    Task<GameSession> CreateSessionAsync(string name, int campaignId, string dmName);
    Task<GameSession?> GetSessionByCodeAsync(string code);
    Task<GameSession?> GetActiveSessionAsync();
    Task UpdateSessionAsync(GameSession session);
    Task EndSessionAsync(int sessionId);

    // Players
    Task<SessionPlayer> JoinSessionAsync(int sessionId, string playerName, int? characterId);
    Task LeaveSessionAsync(int playerId);
    Task<List<SessionPlayer>> GetPlayersInSessionAsync(int sessionId);
    Task UpdatePlayerStatusAsync(int playerId, bool isReady);

    // Dice Rolls
    Task<SharedDiceRoll> ShareDiceRollAsync(int sessionId, int? playerId, string rollerName, string expression, int[] rolls, int modifier);
    Task<List<SharedDiceRoll>> GetRecentRollsAsync(int sessionId, int count = 20);

    // Broadcasting
    Task BroadcastMessageAsync(int sessionId, string title, string message);
    Task ClearBroadcastAsync(int sessionId);

    // Session Code Generation
    string GenerateSessionCode();
}

public class PlayerSessionService : IPlayerSessionService
{
    private readonly IDatabaseService _databaseService;
    private static readonly Random _random = Random.Shared;

    public PlayerSessionService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    #region Game Sessions

    public async Task<GameSession> CreateSessionAsync(string name, int campaignId, string dmName)
    {
        var session = new GameSession
        {
            SessionCode = GenerateSessionCode(),
            Name = name,
            CampaignId = campaignId,
            DmName = dmName,
            State = SessionState.Lobby,
            CreatedAt = DateTime.Now,
            LastActivityAt = DateTime.Now
        };

        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.InsertAsync(session);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating game session: {ex.Message}");
        }

        return session;
    }

    public async Task<GameSession?> GetSessionByCodeAsync(string code)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<GameSession>()
                .Where(s => s.SessionCode == code.ToUpper() && s.State != SessionState.Ended)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting session by code: {ex.Message}");
            return null;
        }
    }

    public async Task<GameSession?> GetActiveSessionAsync()
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<GameSession>()
                .Where(s => s.State == SessionState.Active || s.State == SessionState.Lobby)
                .OrderByDescending(s => s.LastActivityAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting active session: {ex.Message}");
            return null;
        }
    }

    public async Task UpdateSessionAsync(GameSession session)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            session.LastActivityAt = DateTime.Now;
            await db.UpdateAsync(session);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating session: {ex.Message}");
        }
    }

    public async Task EndSessionAsync(int sessionId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var session = await db.GetAsync<GameSession>(sessionId);
            if (session != null)
            {
                session.State = SessionState.Ended;
                session.EndedAt = DateTime.Now;
                await db.UpdateAsync(session);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error ending session: {ex.Message}");
        }
    }

    #endregion

    #region Players

    public async Task<SessionPlayer> JoinSessionAsync(int sessionId, string playerName, int? characterId)
    {
        var player = new SessionPlayer
        {
            GameSessionId = sessionId,
            PlayerName = playerName,
            DisplayName = playerName,
            PlayerCharacterId = characterId,
            IsConnected = true,
            JoinedAt = DateTime.Now,
            LastPingAt = DateTime.Now
        };

        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.InsertAsync(player);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error joining session: {ex.Message}");
        }

        return player;
    }

    public async Task LeaveSessionAsync(int playerId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var player = await db.GetAsync<SessionPlayer>(playerId);
            if (player != null)
            {
                player.IsConnected = false;
                await db.UpdateAsync(player);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error leaving session: {ex.Message}");
        }
    }

    public async Task<List<SessionPlayer>> GetPlayersInSessionAsync(int sessionId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<SessionPlayer>()
                .Where(p => p.GameSessionId == sessionId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting players in session: {ex.Message}");
            return new List<SessionPlayer>();
        }
    }

    public async Task UpdatePlayerStatusAsync(int playerId, bool isReady)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var player = await db.GetAsync<SessionPlayer>(playerId);
            if (player != null)
            {
                player.IsReady = isReady;
                player.LastPingAt = DateTime.Now;
                await db.UpdateAsync(player);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating player status: {ex.Message}");
        }
    }

    #endregion

    #region Dice Rolls

    public async Task<SharedDiceRoll> ShareDiceRollAsync(int sessionId, int? playerId, string rollerName, string expression, int[] rolls, int modifier)
    {
        var total = rolls.Sum() + modifier;
        var isNat20 = rolls.Length == 1 && rolls[0] == 20;
        var isNat1 = rolls.Length == 1 && rolls[0] == 1;

        var roll = new SharedDiceRoll
        {
            GameSessionId = sessionId,
            SessionPlayerId = playerId,
            RollerName = rollerName,
            DiceExpression = expression,
            IndividualRollsJson = System.Text.Json.JsonSerializer.Serialize(rolls),
            Modifier = modifier,
            Total = total,
            IsNatural20 = isNat20,
            IsNatural1 = isNat1,
            RolledAt = DateTime.Now
        };

        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.InsertAsync(roll);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sharing dice roll: {ex.Message}");
        }

        return roll;
    }

    public async Task<List<SharedDiceRoll>> GetRecentRollsAsync(int sessionId, int count = 20)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<SharedDiceRoll>()
                .Where(r => r.GameSessionId == sessionId && !r.IsSecret)
                .OrderByDescending(r => r.RolledAt)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting recent rolls: {ex.Message}");
            return new List<SharedDiceRoll>();
        }
    }

    #endregion

    #region Broadcasting

    public async Task BroadcastMessageAsync(int sessionId, string title, string message)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var session = await db.GetAsync<GameSession>(sessionId);
            if (session != null)
            {
                session.BroadcastTitle = title;
                session.BroadcastMessage = message;
                session.LastActivityAt = DateTime.Now;
                await db.UpdateAsync(session);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error broadcasting message: {ex.Message}");
        }
    }

    public async Task ClearBroadcastAsync(int sessionId)
    {
        await BroadcastMessageAsync(sessionId, string.Empty, string.Empty);
    }

    #endregion

    public string GenerateSessionCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Avoid confusing characters
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}
