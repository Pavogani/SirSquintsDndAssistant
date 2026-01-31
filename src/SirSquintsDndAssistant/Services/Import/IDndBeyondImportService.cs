using SirSquintsDndAssistant.Models.Creatures;

namespace SirSquintsDndAssistant.Services.Import;

public class CharacterImportResult
{
    public bool Success { get; set; }
    public PlayerCharacter? Character { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IDndBeyondImportService
{
    Task<CharacterImportResult> ImportCharacterAsync(string characterId);
}
