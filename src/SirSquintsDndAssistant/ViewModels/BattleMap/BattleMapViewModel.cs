using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Extensions;
using SirSquintsDndAssistant.Services.BattleMap;
using SirSquintsDndAssistant.Services.Utilities;
using SirSquintsDndAssistant.Input;
using BattleMapModel = SirSquintsDndAssistant.Models.BattleMap.BattleMap;
using SirSquintsDndAssistant.Models.BattleMap;
using MapBiome = SirSquintsDndAssistant.Services.BattleMap.MapBiome;

namespace SirSquintsDndAssistant.ViewModels.BattleMap;

public partial class BattleMapViewModel : ObservableObject
{
    private readonly IBattleMapService _battleMapService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<BattleMapModel> _maps = new();

    [ObservableProperty]
    private BattleMapModel? _selectedMap;

    [ObservableProperty]
    private ObservableCollection<MapToken> _tokens = new();

    [ObservableProperty]
    private MapToken? _selectedToken;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isMapSelected;

    [ObservableProperty]
    private string _mapCountText = "0 maps";

    // Grid display settings
    [ObservableProperty]
    private int _gridWidth = 20;

    [ObservableProperty]
    private int _gridHeight = 15;

    [ObservableProperty]
    private bool _showGrid = true;

    [ObservableProperty]
    private bool _useFogOfWar;

    [ObservableProperty]
    private bool _isDmView = true;

    // Tool & viewport
    [ObservableProperty]
    private MapTool _currentTool = MapTool.Select;

    [ObservableProperty]
    private float _zoomLevel = 1.0f;

    // Status
    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private int _tokenCount;

    // Measurement
    [ObservableProperty]
    private string _measurementResult = string.Empty;

    public BattleMapViewModel(IBattleMapService battleMapService, IDialogService dialogService)
    {
        _battleMapService = battleMapService;
        _dialogService = dialogService;
    }

    partial void OnSelectedMapChanged(BattleMapModel? value)
    {
        IsMapSelected = value != null;
        if (value != null)
        {
            GridWidth = value.GridWidth;
            GridHeight = value.GridHeight;
            ShowGrid = value.ShowGrid;
            UseFogOfWar = value.UseFogOfWar;
            StatusText = $"Map: {value.Name}";
            LoadTokensAsync().SafeFireAndForget();
        }
        else
        {
            Tokens.Clear();
            TokenCount = 0;
            StatusText = "No map selected";
        }
    }

    partial void OnTokensChanged(ObservableCollection<MapToken> value)
    {
        TokenCount = value?.Count ?? 0;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var maps = await _battleMapService.GetAllMapsAsync();
            Maps = new ObservableCollection<BattleMapModel>(maps);
            MapCountText = $"{maps.Count} map{(maps.Count == 1 ? "" : "s")}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTokensAsync()
    {
        if (SelectedMap == null) return;

        try
        {
            var tokens = await _battleMapService.GetTokensForMapAsync(SelectedMap.Id);
            Tokens = new ObservableCollection<MapToken>(tokens);
            TokenCount = tokens.Count;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading tokens: {ex.Message}");
        }
    }

    #region Tool Commands

    [RelayCommand]
    private void SetTool(string toolName)
    {
        CurrentTool = Enum.TryParse<MapTool>(toolName, out var tool) ? tool : MapTool.Select;
        StatusText = $"Tool: {CurrentTool}";
    }

    [RelayCommand]
    private void ZoomIn()
    {
        ZoomLevel = Math.Min(4.0f, ZoomLevel + 0.25f);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel = Math.Max(0.25f, ZoomLevel - 0.25f);
    }

    [RelayCommand]
    private void ResetViewport()
    {
        ZoomLevel = 1.0f;
        StatusText = "Viewport reset";
    }

    [RelayCommand]
    private void ToggleDmView()
    {
        IsDmView = !IsDmView;
        StatusText = IsDmView ? "DM View (see through fog)" : "Player View";
    }

    #endregion

    #region Canvas Event Handlers

    public void HandleCellTap(int gridX, int gridY)
    {
        StatusText = $"Cell: ({gridX}, {gridY})";

        if (CurrentTool == MapTool.PlaceToken)
        {
            AddTokenAtPositionAsync(gridX, gridY).SafeFireAndForget();
        }
    }

    public void SelectToken(MapToken token)
    {
        SelectedToken = token;
        StatusText = $"Selected: {token.Name} at ({token.GridX}, {token.GridY})";
    }

    public async void HandleTokenDrag(MapToken token, int newX, int newY)
    {
        if (newX < 0 || newY < 0 || newX >= GridWidth || newY >= GridHeight)
        {
            StatusText = "Invalid position";
            return;
        }

        // Calculate movement distance
        int distance = (int)_battleMapService.CalculateDistance(token.GridX, token.GridY, newX, newY);

        token.GridX = newX;
        token.GridY = newY;
        token.MovementUsed += distance;

        await _battleMapService.SaveTokenAsync(token);

        StatusText = $"Moved {token.Name} to ({newX}, {newY}) - {distance}ft";

        // Refresh the tokens collection to update UI
        var index = Tokens.IndexOf(token);
        if (index >= 0)
        {
            Tokens[index] = token;
        }
    }

    public async Task SaveFogStateAsync(HashSet<string> revealedCells)
    {
        if (SelectedMap == null) return;

        SelectedMap.RevealedCellsJson = JsonSerializer.Serialize(revealedCells.ToList());
        await _battleMapService.SaveMapAsync(SelectedMap);
        StatusText = $"Fog updated ({revealedCells.Count} cells revealed)";
    }

    private async Task AddTokenAtPositionAsync(int gridX, int gridY)
    {
        if (SelectedMap == null) return;

        var name = await _dialogService.DisplayPromptAsync("Add Token", "Enter token name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        var token = new MapToken
        {
            BattleMapId = SelectedMap.Id,
            Name = name,
            Label = name.Length > 2 ? name[..2].ToUpper() : name.ToUpper(),
            GridX = gridX,
            GridY = gridY,
            Color = "#FF0000",
            IsVisible = true,
            MovementTotal = 30
        };

        await _battleMapService.SaveTokenAsync(token);
        await LoadTokensAsync();

        StatusText = $"Added {name} at ({gridX}, {gridY})";
    }

    #endregion

    #region Map Commands

    [RelayCommand]
    private async Task CreateMapAsync()
    {
        var name = await _dialogService.DisplayPromptAsync("New Battle Map", "Enter map name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        var map = new BattleMapModel
        {
            Name = name,
            GridWidth = 20,
            GridHeight = 15,
            CellSize = 5,
            ShowGrid = true
        };

        await _battleMapService.SaveMapAsync(map);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task GenerateMapAsync()
    {
        var biomes = new[]
        {
            "Forest", "Plains", "Mountains", "Sewers", "Graveyard",
            "Swamp", "Desert", "Cave", "Dungeon", "Town",
            "Castle", "Beach", "Tundra"
        };

        var selectedBiome = await _dialogService.DisplayActionSheetAsync(
            "Select Terrain Type", "Cancel", null, biomes);

        if (selectedBiome == "Cancel" || selectedBiome == null) return;

        var name = await _dialogService.DisplayPromptAsync("Map Name",
            $"Enter name for {selectedBiome} map:",
            initialValue: $"{selectedBiome} Battlefield");

        if (string.IsNullOrWhiteSpace(name)) return;

        var widthStr = await _dialogService.DisplayPromptAsync("Map Width",
            "Enter grid width (10-40):", initialValue: "20", keyboard: Keyboard.Numeric);
        if (!int.TryParse(widthStr, out var width) || width < 10 || width > 40)
            width = 20;

        var heightStr = await _dialogService.DisplayPromptAsync("Map Height",
            "Enter grid height (10-30):", initialValue: "15", keyboard: Keyboard.Numeric);
        if (!int.TryParse(heightStr, out var height) || height < 10 || height > 30)
            height = 15;

        var biome = Enum.Parse<MapBiome>(selectedBiome);
        var map = await _battleMapService.GenerateMapAsync(name, biome, width, height);

        await LoadDataAsync();
        SelectedMap = Maps.FirstOrDefault(m => m.Id == map.Id);
        StatusText = $"Generated {selectedBiome} map: {name}";
    }

    [RelayCommand]
    private async Task DeleteMapAsync(BattleMapModel map)
    {
        var confirm = await _dialogService.DisplayConfirmAsync("Delete Map",
            $"Are you sure you want to delete '{map.Name}'? All tokens will also be deleted.");
        if (!confirm) return;

        await _battleMapService.DeleteMapAsync(map.Id);
        if (SelectedMap?.Id == map.Id)
        {
            SelectedMap = null;
        }
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DuplicateMapAsync(BattleMapModel map)
    {
        await _battleMapService.DuplicateMapAsync(map.Id);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task EditMapSettingsAsync()
    {
        if (SelectedMap == null) return;

        var widthStr = await _dialogService.DisplayPromptAsync("Grid Width",
            "Enter grid width (cells):", initialValue: SelectedMap.GridWidth.ToString());
        if (!int.TryParse(widthStr, out var width) || width < 5 || width > 100) return;

        var heightStr = await _dialogService.DisplayPromptAsync("Grid Height",
            "Enter grid height (cells):", initialValue: SelectedMap.GridHeight.ToString());
        if (!int.TryParse(heightStr, out var height) || height < 5 || height > 100) return;

        SelectedMap.GridWidth = width;
        SelectedMap.GridHeight = height;
        GridWidth = width;
        GridHeight = height;

        await _battleMapService.SaveMapAsync(SelectedMap);
    }

    [RelayCommand]
    private async Task ToggleGridAsync()
    {
        if (SelectedMap == null) return;

        SelectedMap.ShowGrid = !SelectedMap.ShowGrid;
        ShowGrid = SelectedMap.ShowGrid;
        await _battleMapService.SaveMapAsync(SelectedMap);
    }

    [RelayCommand]
    private async Task ToggleFogOfWarAsync()
    {
        if (SelectedMap == null) return;

        SelectedMap.UseFogOfWar = !SelectedMap.UseFogOfWar;
        UseFogOfWar = SelectedMap.UseFogOfWar;
        await _battleMapService.SaveMapAsync(SelectedMap);
    }

    #endregion

    #region Token Commands

    [RelayCommand]
    private async Task AddTokenAsync()
    {
        if (SelectedMap == null) return;

        var name = await _dialogService.DisplayPromptAsync("Add Token", "Enter token name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        var token = new MapToken
        {
            BattleMapId = SelectedMap.Id,
            Name = name,
            Label = name.Length > 2 ? name[..2].ToUpper() : name.ToUpper(),
            GridX = GridWidth / 2,
            GridY = GridHeight / 2,
            Color = "#FF0000",
            IsVisible = true,
            MovementTotal = 30
        };

        await _battleMapService.SaveTokenAsync(token);
        await LoadTokensAsync();
    }

    [RelayCommand]
    private async Task DeleteTokenAsync(MapToken token)
    {
        var confirm = await _dialogService.DisplayConfirmAsync("Delete Token",
            $"Remove '{token.Name}' from the map?");
        if (!confirm) return;

        await _battleMapService.DeleteTokenAsync(token.Id);
        Tokens.Remove(token);
        TokenCount = Tokens.Count;
    }

    [RelayCommand]
    private async Task MoveTokenAsync(MapToken token)
    {
        var xStr = await _dialogService.DisplayPromptAsync("Move Token",
            "Enter X position (column):", initialValue: token.GridX.ToString());
        if (!int.TryParse(xStr, out var x) || x < 0 || x >= GridWidth) return;

        var yStr = await _dialogService.DisplayPromptAsync("Move Token",
            "Enter Y position (row):", initialValue: token.GridY.ToString());
        if (!int.TryParse(yStr, out var y) || y < 0 || y >= GridHeight) return;

        await _battleMapService.MoveTokenAsync(token.Id, x, y);
        await LoadTokensAsync();
    }

    [RelayCommand]
    private async Task ToggleTokenEnemyAsync(MapToken token)
    {
        token.IsEnemy = !token.IsEnemy;
        token.Color = token.IsEnemy ? "#DC143C" : "#4169E1";
        await _battleMapService.SaveTokenAsync(token);
        await LoadTokensAsync();
    }

    [RelayCommand]
    private async Task SetCreatureSizeAsync(MapToken token)
    {
        var sizes = new[] { "Tiny", "Small", "Medium", "Large", "Huge", "Gargantuan" };
        var selected = await _dialogService.DisplayActionSheetAsync("Token Size", "Cancel", null, sizes);
        if (selected == "Cancel" || selected == null) return;

        token.Size = selected switch
        {
            "Tiny" => CreatureSize.Tiny,
            "Small" => CreatureSize.Small,
            "Medium" => CreatureSize.Medium,
            "Large" => CreatureSize.Large,
            "Huge" => CreatureSize.Huge,
            "Gargantuan" => CreatureSize.Gargantuan,
            _ => CreatureSize.Medium
        };

        await _battleMapService.SaveTokenAsync(token);
        await LoadTokensAsync();
    }

    [RelayCommand]
    private async Task ClearAllTokensAsync()
    {
        if (SelectedMap == null) return;

        var confirm = await _dialogService.DisplayConfirmAsync("Clear Tokens",
            "Remove all tokens from this map?");
        if (!confirm) return;

        await _battleMapService.ClearAllTokensAsync(SelectedMap.Id);
        Tokens.Clear();
        TokenCount = 0;
    }

    [RelayCommand]
    private async Task MeasureDistanceAsync()
    {
        var startXStr = await _dialogService.DisplayPromptAsync("Measure Distance", "Start X:");
        if (!int.TryParse(startXStr, out var startX)) return;

        var startYStr = await _dialogService.DisplayPromptAsync("Measure Distance", "Start Y:");
        if (!int.TryParse(startYStr, out var startY)) return;

        var endXStr = await _dialogService.DisplayPromptAsync("Measure Distance", "End X:");
        if (!int.TryParse(endXStr, out var endX)) return;

        var endYStr = await _dialogService.DisplayPromptAsync("Measure Distance", "End Y:");
        if (!int.TryParse(endYStr, out var endY)) return;

        var distance = _battleMapService.CalculateDistance(startX, startY, endX, endY);
        MeasurementResult = $"Distance: {distance} ft";

        await _dialogService.DisplayAlertAsync("Measurement", $"Distance: {distance} feet");
    }

    [RelayCommand]
    private async Task ResetTokenMovementAsync()
    {
        if (SelectedMap == null) return;

        foreach (var token in Tokens)
        {
            token.MovementUsed = 0;
            token.MovementPathJson = "[]";
            await _battleMapService.SaveTokenAsync(token);
        }

        await LoadTokensAsync();
        await _dialogService.DisplayAlertAsync("Movement Reset", "All token movement has been reset for a new round.");
    }

    #endregion
}
