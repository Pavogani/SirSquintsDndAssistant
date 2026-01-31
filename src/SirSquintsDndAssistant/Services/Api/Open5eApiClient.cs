using System.Text.Json;
using SirSquintsDndAssistant.Models.Api;

namespace SirSquintsDndAssistant.Services.Api;

public class Open5eApiClient : IOpen5eApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public Open5eApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.open5e.com/");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<Open5eMonsterListResponse?> GetMonstersAsync(int page = 1, int limit = 100)
    {
        try
        {
            // Use v1 API with page parameter (not offset)
            var response = await _httpClient.GetAsync($"v1/monsters/?limit={limit}&page={page}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Open5e API page {page}: Got {json.Length} bytes");
            return JsonSerializer.Deserialize<Open5eMonsterListResponse>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching Open5e monsters page {page}: {ex.Message}");
            return null;
        }
    }
}
