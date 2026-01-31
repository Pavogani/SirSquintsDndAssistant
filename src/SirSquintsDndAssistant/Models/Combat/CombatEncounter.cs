using SQLite;

namespace SirSquintsDndAssistant.Models.Combat;

public class CombatEncounter
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int? SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsActive { get; set; }
    public int CurrentRound { get; set; }
    public int CurrentTurnIndex { get; set; }
    public string CombatantsJson { get; set; } = string.Empty; // Serialized initiative order
    public string CombatLogJson { get; set; } = string.Empty; // Event log
}
