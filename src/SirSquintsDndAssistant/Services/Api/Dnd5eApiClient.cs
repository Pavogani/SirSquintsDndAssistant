using System.Text.Json;
using SirSquintsDndAssistant.Models.Api;

namespace SirSquintsDndAssistant.Services.Api;

public class Dnd5eApiClient : IDnd5eApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string BaseUrl = "https://www.dnd5eapi.co/api/2014/";

    public Dnd5eApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<Dnd5eApiListResponse?> GetMonstersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("monsters");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dnd5eApiListResponse>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error fetching monsters list: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON parsing error for monsters list: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error fetching monsters list: {ex.Message}");
            return null;
        }
    }

    public async Task<Dnd5eMonsterResponse?> GetMonsterAsync(string index)
    {
        try
        {
            var response = await _httpClient.GetAsync($"monsters/{index}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dnd5eMonsterResponse>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error fetching monster '{index}': {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON parsing error for monster '{index}': {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error fetching monster '{index}': {ex.Message}");
            return null;
        }
    }

    public async Task<Dnd5eApiListResponse?> GetSpellsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("spells");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dnd5eApiListResponse>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error fetching spells list: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON parsing error for spells list: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error fetching spells list: {ex.Message}");
            return null;
        }
    }

    public async Task<Dnd5eSpellResponse?> GetSpellAsync(string index)
    {
        try
        {
            var response = await _httpClient.GetAsync($"spells/{index}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dnd5eSpellResponse>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error fetching spell '{index}': {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON parsing error for spell '{index}': {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error fetching spell '{index}': {ex.Message}");
            return null;
        }
    }

    public async Task<Dnd5eApiListResponse?> GetEquipmentAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("equipment");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dnd5eApiListResponse>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error fetching equipment list: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON parsing error for equipment list: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error fetching equipment list: {ex.Message}");
            return null;
        }
    }

    public async Task<Dnd5eEquipmentDetailResponse?> GetEquipmentDetailAsync(string index)
    {
        try
        {
            var response = await _httpClient.GetAsync($"equipment/{index}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dnd5eEquipmentDetailResponse>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error fetching equipment '{index}': {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON parsing error for equipment '{index}': {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error fetching equipment '{index}': {ex.Message}");
            return null;
        }
    }

    public async Task<Dnd5eApiListResponse?> GetMagicItemsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("magic-items");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dnd5eApiListResponse>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error fetching magic items list: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON parsing error for magic items list: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error fetching magic items list: {ex.Message}");
            return null;
        }
    }

    public async Task<Dnd5eMagicItemDetailResponse?> GetMagicItemDetailAsync(string index)
    {
        try
        {
            var response = await _httpClient.GetAsync($"magic-items/{index}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dnd5eMagicItemDetailResponse>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error fetching magic item '{index}': {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON parsing error for magic item '{index}': {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error fetching magic item '{index}': {ex.Message}");
            return null;
        }
    }

    public async Task<Dnd5eApiListResponse?> GetConditionsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("conditions");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dnd5eApiListResponse>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error fetching conditions list: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON parsing error for conditions list: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error fetching conditions list: {ex.Message}");
            return null;
        }
    }

    public async Task<Dnd5eConditionResponse?> GetConditionAsync(string index)
    {
        try
        {
            var response = await _httpClient.GetAsync($"conditions/{index}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dnd5eConditionResponse>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error fetching condition '{index}': {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON parsing error for condition '{index}': {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error fetching condition '{index}': {ex.Message}");
            return null;
        }
    }
}
