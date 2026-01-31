using SirSquintsDndAssistant.Models.Api;

namespace SirSquintsDndAssistant.Services.Api;

public interface IOpen5eApiClient
{
    Task<Open5eMonsterListResponse?> GetMonstersAsync(int page = 1, int limit = 100);
}
