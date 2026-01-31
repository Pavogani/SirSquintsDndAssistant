using System.Text.Json;
using SirSquintsDndAssistant.Models.BattleMap;
using SirSquintsDndAssistant.Services.Database;

namespace SirSquintsDndAssistant.Services.BattleMap;

public interface IBattleMapService
{
    // Maps
    Task<List<Models.BattleMap.BattleMap>> GetAllMapsAsync();
    Task<Models.BattleMap.BattleMap?> GetMapAsync(int id);
    Task<Models.BattleMap.BattleMap> SaveMapAsync(Models.BattleMap.BattleMap map);
    Task DeleteMapAsync(int id);
    Task<Models.BattleMap.BattleMap> DuplicateMapAsync(int id);

    // Tokens
    Task<List<MapToken>> GetTokensForMapAsync(int mapId);
    Task<MapToken> SaveTokenAsync(MapToken token);
    Task DeleteTokenAsync(int tokenId);
    Task MoveTokenAsync(int tokenId, int newX, int newY);
    Task ClearAllTokensAsync(int mapId);

    // Token from combat
    Task<MapToken> CreateTokenFromInitiativeEntryAsync(int mapId, int initiativeEntryId, int gridX, int gridY);

    // Terrain
    Task<List<TerrainOverlay>> GetTerrainForMapAsync(int mapId);
    Task UpdateTerrainAsync(int mapId, List<TerrainOverlay> terrain);
    Task AddTerrainOverlayAsync(int mapId, TerrainOverlay overlay);
    Task RemoveTerrainOverlayAsync(int mapId, int overlayId);

    // Fog of War
    Task RevealAreaAsync(int mapId, int centerX, int centerY, int radius);
    Task HideAreaAsync(int mapId, int centerX, int centerY, int radius);
    Task ResetFogOfWarAsync(int mapId);

    // Measurements
    double CalculateDistance(int startX, int startY, int endX, int endY);
    List<(int x, int y)> GetLineOfSight(int startX, int startY, int endX, int endY);
    List<(int x, int y)> GetAreaOfEffect(int centerX, int centerY, int radius, MeasurementType shape);

    // Map Generation
    Task<Models.BattleMap.BattleMap> GenerateMapAsync(string name, MapBiome biome, int width = 20, int height = 15);
}

public enum MapBiome
{
    Forest,
    Plains,
    Mountains,
    Sewers,
    Graveyard,
    Swamp,
    Desert,
    Cave,
    Dungeon,
    Town,
    Castle,
    Beach,
    Tundra
}

public class BattleMapService : IBattleMapService
{
    private readonly IDatabaseService _databaseService;

    public BattleMapService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    #region Maps

    public async Task<List<Models.BattleMap.BattleMap>> GetAllMapsAsync()
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Models.BattleMap.BattleMap>()
                .OrderBy(m => m.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting battle maps: {ex.Message}");
            return new List<Models.BattleMap.BattleMap>();
        }
    }

    public async Task<Models.BattleMap.BattleMap?> GetMapAsync(int id)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.GetAsync<Models.BattleMap.BattleMap>(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting battle map: {ex.Message}");
            return null;
        }
    }

    public async Task<Models.BattleMap.BattleMap> SaveMapAsync(Models.BattleMap.BattleMap map)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            map.UpdatedAt = DateTime.Now;

            if (map.Id == 0)
            {
                map.CreatedAt = DateTime.Now;
                await db.InsertAsync(map);
            }
            else
            {
                await db.UpdateAsync(map);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving battle map: {ex.Message}");
        }

        return map;
    }

    public async Task DeleteMapAsync(int id)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            // Delete associated tokens first
            await ClearAllTokensAsync(id);
            await db.DeleteAsync<Models.BattleMap.BattleMap>(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting battle map: {ex.Message}");
        }
    }

    public async Task<Models.BattleMap.BattleMap> DuplicateMapAsync(int id)
    {
        var original = await GetMapAsync(id);
        if (original == null)
            throw new InvalidOperationException("Map not found");

        var duplicate = new Models.BattleMap.BattleMap
        {
            Name = $"{original.Name} (Copy)",
            Description = original.Description,
            GridWidth = original.GridWidth,
            GridHeight = original.GridHeight,
            CellSize = original.CellSize,
            BackgroundImagePath = original.BackgroundImagePath,
            BackgroundType = original.BackgroundType,
            ShowGrid = original.ShowGrid,
            GridColor = original.GridColor,
            GridOpacity = original.GridOpacity,
            TerrainJson = original.TerrainJson,
            Tags = original.Tags
        };

        return await SaveMapAsync(duplicate);
    }

    #endregion

    #region Tokens

    public async Task<List<MapToken>> GetTokensForMapAsync(int mapId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<MapToken>()
                .Where(t => t.BattleMapId == mapId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting map tokens: {ex.Message}");
            return new List<MapToken>();
        }
    }

    public async Task<MapToken> SaveTokenAsync(MapToken token)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();

            if (token.Id == 0)
            {
                await db.InsertAsync(token);
            }
            else
            {
                await db.UpdateAsync(token);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving map token: {ex.Message}");
        }

        return token;
    }

    public async Task DeleteTokenAsync(int tokenId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.DeleteAsync<MapToken>(tokenId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting map token: {ex.Message}");
        }
    }

    public async Task MoveTokenAsync(int tokenId, int newX, int newY)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var token = await db.GetAsync<MapToken>(tokenId);
            if (token != null)
            {
                // Track movement path
                var path = string.IsNullOrEmpty(token.MovementPathJson)
                    ? new List<int[]>()
                    : JsonSerializer.Deserialize<List<int[]>>(token.MovementPathJson) ?? new List<int[]>();

                path.Add(new[] { token.GridX, token.GridY });

                // Calculate movement used
                var distance = CalculateDistance(token.GridX, token.GridY, newX, newY);
                token.MovementUsed += (int)distance;

                token.GridX = newX;
                token.GridY = newY;
                token.MovementPathJson = JsonSerializer.Serialize(path);

                await db.UpdateAsync(token);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error moving token: {ex.Message}");
        }
    }

    public async Task ClearAllTokensAsync(int mapId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.ExecuteAsync("DELETE FROM MapToken WHERE BattleMapId = ?", mapId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing tokens: {ex.Message}");
        }
    }

    public async Task<MapToken> CreateTokenFromInitiativeEntryAsync(int mapId, int initiativeEntryId, int gridX, int gridY)
    {
        // This would normally fetch from InitiativeEntry and create a token
        // For now, create a basic token
        var token = new MapToken
        {
            BattleMapId = mapId,
            InitiativeEntryId = initiativeEntryId,
            Name = $"Combatant {initiativeEntryId}",
            Label = initiativeEntryId.ToString(),
            GridX = gridX,
            GridY = gridY,
            IsVisible = true
        };

        return await SaveTokenAsync(token);
    }

    #endregion

    #region Terrain

    public async Task<List<TerrainOverlay>> GetTerrainForMapAsync(int mapId)
    {
        try
        {
            var map = await GetMapAsync(mapId);
            if (map == null || string.IsNullOrEmpty(map.TerrainJson))
                return new List<TerrainOverlay>();

            return JsonSerializer.Deserialize<List<TerrainOverlay>>(map.TerrainJson)
                   ?? new List<TerrainOverlay>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting terrain: {ex.Message}");
            return new List<TerrainOverlay>();
        }
    }

    public async Task UpdateTerrainAsync(int mapId, List<TerrainOverlay> terrain)
    {
        try
        {
            var map = await GetMapAsync(mapId);
            if (map != null)
            {
                map.TerrainJson = JsonSerializer.Serialize(terrain);
                await SaveMapAsync(map);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating terrain: {ex.Message}");
        }
    }

    public async Task AddTerrainOverlayAsync(int mapId, TerrainOverlay overlay)
    {
        var terrain = await GetTerrainForMapAsync(mapId);
        overlay.Id = terrain.Count > 0 ? terrain.Max(t => t.Id) + 1 : 1;
        terrain.Add(overlay);
        await UpdateTerrainAsync(mapId, terrain);
    }

    public async Task RemoveTerrainOverlayAsync(int mapId, int overlayId)
    {
        var terrain = await GetTerrainForMapAsync(mapId);
        terrain.RemoveAll(t => t.Id == overlayId);
        await UpdateTerrainAsync(mapId, terrain);
    }

    #endregion

    #region Fog of War

    public async Task RevealAreaAsync(int mapId, int centerX, int centerY, int radius)
    {
        try
        {
            var map = await GetMapAsync(mapId);
            if (map == null) return;

            var revealed = string.IsNullOrEmpty(map.RevealedCellsJson)
                ? new HashSet<string>()
                : JsonSerializer.Deserialize<HashSet<string>>(map.RevealedCellsJson) ?? new HashSet<string>();

            // Reveal cells within radius
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (x >= 0 && x < map.GridWidth && y >= 0 && y < map.GridHeight)
                    {
                        var distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                        if (distance <= radius)
                        {
                            revealed.Add($"{x},{y}");
                        }
                    }
                }
            }

            map.RevealedCellsJson = JsonSerializer.Serialize(revealed);
            await SaveMapAsync(map);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error revealing area: {ex.Message}");
        }
    }

    public async Task HideAreaAsync(int mapId, int centerX, int centerY, int radius)
    {
        try
        {
            var map = await GetMapAsync(mapId);
            if (map == null) return;

            var revealed = string.IsNullOrEmpty(map.RevealedCellsJson)
                ? new HashSet<string>()
                : JsonSerializer.Deserialize<HashSet<string>>(map.RevealedCellsJson) ?? new HashSet<string>();

            // Hide cells within radius
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    var distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                    if (distance <= radius)
                    {
                        revealed.Remove($"{x},{y}");
                    }
                }
            }

            map.RevealedCellsJson = JsonSerializer.Serialize(revealed);
            await SaveMapAsync(map);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error hiding area: {ex.Message}");
        }
    }

    public async Task ResetFogOfWarAsync(int mapId)
    {
        try
        {
            var map = await GetMapAsync(mapId);
            if (map != null)
            {
                map.RevealedCellsJson = "[]";
                await SaveMapAsync(map);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error resetting fog of war: {ex.Message}");
        }
    }

    #endregion

    #region Measurements

    public double CalculateDistance(int startX, int startY, int endX, int endY)
    {
        // D&D 5e diagonal movement: alternating 5ft/10ft
        var dx = Math.Abs(endX - startX);
        var dy = Math.Abs(endY - startY);
        var diagonals = Math.Min(dx, dy);
        var straights = Math.Max(dx, dy) - diagonals;
        return (straights * 5) + (diagonals * 5) + ((diagonals / 2) * 5);
    }

    public List<(int x, int y)> GetLineOfSight(int startX, int startY, int endX, int endY)
    {
        // Bresenham's line algorithm
        var result = new List<(int x, int y)>();

        int dx = Math.Abs(endX - startX);
        int dy = Math.Abs(endY - startY);
        int sx = startX < endX ? 1 : -1;
        int sy = startY < endY ? 1 : -1;
        int err = dx - dy;

        int x = startX;
        int y = startY;

        while (true)
        {
            result.Add((x, y));

            if (x == endX && y == endY)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }

        return result;
    }

    public List<(int x, int y)> GetAreaOfEffect(int centerX, int centerY, int radius, MeasurementType shape)
    {
        var result = new List<(int x, int y)>();

        switch (shape)
        {
            case MeasurementType.Circle:
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    for (int y = centerY - radius; y <= centerY + radius; y++)
                    {
                        var distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                        if (distance <= radius)
                        {
                            result.Add((x, y));
                        }
                    }
                }
                break;

            case MeasurementType.Square:
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    for (int y = centerY - radius; y <= centerY + radius; y++)
                    {
                        result.Add((x, y));
                    }
                }
                break;

            default:
                result.Add((centerX, centerY));
                break;
        }

        return result;
    }

    #endregion

    #region Map Generation

    public async Task<Models.BattleMap.BattleMap> GenerateMapAsync(string name, MapBiome biome, int width = 20, int height = 15)
    {
        var random = Random.Shared;
        var terrain = new List<TerrainOverlay>();
        var backgroundColor = GetBiomeBackgroundColor(biome);

        // Generate terrain features based on biome
        switch (biome)
        {
            case MapBiome.Forest:
                terrain = GenerateForestTerrain(width, height, random);
                break;
            case MapBiome.Plains:
                terrain = GeneratePlainsTerrain(width, height, random);
                break;
            case MapBiome.Mountains:
                terrain = GenerateMountainTerrain(width, height, random);
                break;
            case MapBiome.Sewers:
                terrain = GenerateSewersTerrain(width, height, random);
                break;
            case MapBiome.Graveyard:
                terrain = GenerateGraveyardTerrain(width, height, random);
                break;
            case MapBiome.Swamp:
                terrain = GenerateSwampTerrain(width, height, random);
                break;
            case MapBiome.Desert:
                terrain = GenerateDesertTerrain(width, height, random);
                break;
            case MapBiome.Cave:
                terrain = GenerateCaveTerrain(width, height, random);
                break;
            case MapBiome.Dungeon:
                terrain = GenerateDungeonTerrain(width, height, random);
                break;
            case MapBiome.Town:
                terrain = GenerateTownTerrain(width, height, random);
                break;
            case MapBiome.Castle:
                terrain = GenerateCastleTerrain(width, height, random);
                break;
            case MapBiome.Beach:
                terrain = GenerateBeachTerrain(width, height, random);
                break;
            case MapBiome.Tundra:
                terrain = GenerateTundraTerrain(width, height, random);
                break;
        }

        var map = new Models.BattleMap.BattleMap
        {
            Name = name,
            Description = $"Generated {biome} map",
            GridWidth = width,
            GridHeight = height,
            CellSize = 5,
            ShowGrid = true,
            BackgroundColor = backgroundColor,
            TerrainJson = JsonSerializer.Serialize(terrain),
            Tags = biome.ToString()
        };

        return await SaveMapAsync(map);
    }

    private string GetBiomeBackgroundColor(MapBiome biome) => biome switch
    {
        MapBiome.Forest => "#2D4A2D",
        MapBiome.Plains => "#7A8B5A",
        MapBiome.Mountains => "#6B6B6B",
        MapBiome.Sewers => "#3A3A3A",
        MapBiome.Graveyard => "#4A4A5A",
        MapBiome.Swamp => "#4A5A3A",
        MapBiome.Desert => "#C4A35A",
        MapBiome.Cave => "#2A2A2A",
        MapBiome.Dungeon => "#3A3A4A",
        MapBiome.Town => "#8A7A6A",
        MapBiome.Castle => "#5A5A6A",
        MapBiome.Beach => "#C4B896",
        MapBiome.Tundra => "#A0B0C0",
        _ => "#4A5A4A"
    };

    private List<TerrainOverlay> GenerateForestTerrain(int width, int height, Random random)
    {
        var terrain = new List<TerrainOverlay>();
        int id = 1;

        // Add trees (blocking terrain)
        int treeCount = (width * height) / 8;
        for (int i = 0; i < treeCount; i++)
        {
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Tree",
                Type = TerrainType.Cover,
                StartX = random.Next(width),
                StartY = random.Next(height),
                Width = 1,
                Height = 1,
                Color = "#1A4A1A",
                Opacity = 0.8,
                BlocksMovement = true,
                BlocksSight = true
            });
        }

        // Add undergrowth (difficult terrain)
        int brushCount = (width * height) / 10;
        for (int i = 0; i < brushCount; i++)
        {
            int w = random.Next(1, 3);
            int h = random.Next(1, 3);
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Undergrowth",
                Type = TerrainType.DifficultTerrain,
                StartX = random.Next(width - w),
                StartY = random.Next(height - h),
                Width = w,
                Height = h,
                Color = "#3A6A2A",
                Opacity = 0.4,
                IsDifficultTerrain = true
            });
        }

        // Add a clearing path
        int pathY = height / 2;
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Forest Path",
            Type = TerrainType.Normal,
            StartX = 0,
            StartY = pathY - 1,
            Width = width,
            Height = 2,
            Color = "#8B7355",
            Opacity = 0.5
        });

        return terrain;
    }

    private List<TerrainOverlay> GeneratePlainsTerrain(int width, int height, Random random)
    {
        var terrain = new List<TerrainOverlay>();
        int id = 1;

        // Add scattered rocks
        int rockCount = (width * height) / 20;
        for (int i = 0; i < rockCount; i++)
        {
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Boulder",
                Type = TerrainType.Cover,
                StartX = random.Next(width),
                StartY = random.Next(height),
                Width = 1,
                Height = 1,
                Color = "#7A7A7A",
                Opacity = 0.9,
                BlocksMovement = true
            });
        }

        // Add tall grass
        int grassCount = (width * height) / 12;
        for (int i = 0; i < grassCount; i++)
        {
            int w = random.Next(2, 4);
            int h = random.Next(2, 4);
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Tall Grass",
                Type = TerrainType.DifficultTerrain,
                StartX = random.Next(width - w),
                StartY = random.Next(height - h),
                Width = w,
                Height = h,
                Color = "#6A8A4A",
                Opacity = 0.3,
                IsDifficultTerrain = true
            });
        }

        return terrain;
    }

    private List<TerrainOverlay> GenerateMountainTerrain(int width, int height, Random random)
    {
        var terrain = new List<TerrainOverlay>();
        int id = 1;

        // Add cliff walls
        int cliffCount = (width * height) / 15;
        for (int i = 0; i < cliffCount; i++)
        {
            int w = random.Next(1, 4);
            int h = random.Next(1, 3);
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Cliff",
                Type = TerrainType.Wall,
                StartX = random.Next(width - w),
                StartY = random.Next(height - h),
                Width = w,
                Height = h,
                Color = "#5A5A5A",
                Opacity = 1.0,
                BlocksMovement = true,
                BlocksSight = true
            });
        }

        // Add rocky ground
        int rockyCount = (width * height) / 8;
        for (int i = 0; i < rockyCount; i++)
        {
            int w = random.Next(2, 4);
            int h = random.Next(2, 4);
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Rocky Ground",
                Type = TerrainType.DifficultTerrain,
                StartX = random.Next(width - w),
                StartY = random.Next(height - h),
                Width = w,
                Height = h,
                Color = "#8A8A7A",
                Opacity = 0.4,
                IsDifficultTerrain = true
            });
        }

        // Add a chasm
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Chasm",
            Type = TerrainType.Pit,
            StartX = width / 3,
            StartY = 0,
            Width = 2,
            Height = height,
            Color = "#1A1A1A",
            Opacity = 0.9,
            BlocksMovement = true,
            EffectDescription = "30 ft drop"
        });

        return terrain;
    }

    private List<TerrainOverlay> GenerateSewersTerrain(int width, int height, Random random)
    {
        var terrain = new List<TerrainOverlay>();
        int id = 1;

        // Add water channel in center
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Sewage Channel",
            Type = TerrainType.Water,
            StartX = width / 2 - 1,
            StartY = 0,
            Width = 2,
            Height = height,
            Color = "#3A4A2A",
            Opacity = 0.7,
            IsDifficultTerrain = true,
            EffectDescription = "Difficult terrain, poisoned on failed DC 12 CON save if submerged"
        });

        // Add pillars
        for (int x = 3; x < width - 3; x += 5)
        {
            for (int y = 3; y < height - 3; y += 5)
            {
                terrain.Add(new TerrainOverlay
                {
                    Id = id++,
                    Name = "Pillar",
                    Type = TerrainType.Cover,
                    StartX = x,
                    StartY = y,
                    Width = 1,
                    Height = 1,
                    Color = "#5A5A5A",
                    Opacity = 1.0,
                    BlocksMovement = true,
                    BlocksSight = true
                });
            }
        }

        // Add grates
        int grateCount = 4;
        for (int i = 0; i < grateCount; i++)
        {
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Grate",
                Type = TerrainType.Hazard,
                StartX = random.Next(width),
                StartY = random.Next(height),
                Width = 1,
                Height = 1,
                Color = "#4A4A4A",
                Opacity = 0.6,
                EffectDescription = "May collapse - DC 10 DEX save or fall 10 ft"
            });
        }

        return terrain;
    }

    private List<TerrainOverlay> GenerateGraveyardTerrain(int width, int height, Random random)
    {
        var terrain = new List<TerrainOverlay>();
        int id = 1;

        // Add gravestones in rows
        for (int x = 2; x < width - 2; x += 3)
        {
            for (int y = 2; y < height - 2; y += 3)
            {
                if (random.Next(100) < 70)
                {
                    terrain.Add(new TerrainOverlay
                    {
                        Id = id++,
                        Name = "Gravestone",
                        Type = TerrainType.Cover,
                        StartX = x,
                        StartY = y,
                        Width = 1,
                        Height = 1,
                        Color = "#6A6A7A",
                        Opacity = 0.9,
                        BlocksMovement = true
                    });
                }
            }
        }

        // Add mausoleums
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Mausoleum",
            Type = TerrainType.Wall,
            StartX = width / 2 - 2,
            StartY = height / 2 - 1,
            Width = 4,
            Height = 3,
            Color = "#5A5A6A",
            Opacity = 1.0,
            BlocksMovement = true,
            BlocksSight = true
        });

        // Add open graves
        int graveCount = 3;
        for (int i = 0; i < graveCount; i++)
        {
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Open Grave",
                Type = TerrainType.Pit,
                StartX = random.Next(2, width - 3),
                StartY = random.Next(2, height - 3),
                Width = 1,
                Height = 2,
                Color = "#2A2A2A",
                Opacity = 0.8,
                BlocksMovement = true,
                EffectDescription = "6 ft deep pit"
            });
        }

        return terrain;
    }

    private List<TerrainOverlay> GenerateSwampTerrain(int width, int height, Random random)
    {
        var terrain = new List<TerrainOverlay>();
        int id = 1;

        // Add murky water pools
        int poolCount = (width * height) / 12;
        for (int i = 0; i < poolCount; i++)
        {
            int w = random.Next(2, 5);
            int h = random.Next(2, 5);
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Murky Water",
                Type = TerrainType.Water,
                StartX = random.Next(width - w),
                StartY = random.Next(height - h),
                Width = w,
                Height = h,
                Color = "#3A4A3A",
                Opacity = 0.6,
                IsDifficultTerrain = true,
                IsCircular = random.Next(2) == 0
            });
        }

        // Add dead trees
        int treeCount = (width * height) / 15;
        for (int i = 0; i < treeCount; i++)
        {
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Dead Tree",
                Type = TerrainType.Cover,
                StartX = random.Next(width),
                StartY = random.Next(height),
                Width = 1,
                Height = 1,
                Color = "#4A3A2A",
                Opacity = 0.8,
                BlocksMovement = true
            });
        }

        // Add quicksand
        int quicksandCount = 2;
        for (int i = 0; i < quicksandCount; i++)
        {
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Quicksand",
                Type = TerrainType.Hazard,
                StartX = random.Next(2, width - 4),
                StartY = random.Next(2, height - 4),
                Width = 2,
                Height = 2,
                Color = "#5A4A3A",
                Opacity = 0.5,
                EffectDescription = "DC 12 STR save or sink and become restrained"
            });
        }

        return terrain;
    }

    private List<TerrainOverlay> GenerateDesertTerrain(int width, int height, Random random)
    {
        var terrain = new List<TerrainOverlay>();
        int id = 1;

        // Add dunes (difficult terrain)
        int duneCount = (width * height) / 10;
        for (int i = 0; i < duneCount; i++)
        {
            int w = random.Next(2, 5);
            int h = random.Next(2, 4);
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Sand Dune",
                Type = TerrainType.DifficultTerrain,
                StartX = random.Next(width - w),
                StartY = random.Next(height - h),
                Width = w,
                Height = h,
                Color = "#D4B896",
                Opacity = 0.4,
                IsDifficultTerrain = true
            });
        }

        // Add rock outcroppings
        int rockCount = (width * height) / 25;
        for (int i = 0; i < rockCount; i++)
        {
            int w = random.Next(1, 3);
            int h = random.Next(1, 3);
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Rock Formation",
                Type = TerrainType.Cover,
                StartX = random.Next(width - w),
                StartY = random.Next(height - h),
                Width = w,
                Height = h,
                Color = "#8A7A6A",
                Opacity = 0.9,
                BlocksMovement = true,
                BlocksSight = h > 1
            });
        }

        // Add oasis
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Oasis",
            Type = TerrainType.Water,
            StartX = width / 2 - 2,
            StartY = height / 2 - 2,
            Width = 4,
            Height = 4,
            Color = "#4A8AB0",
            Opacity = 0.7,
            IsCircular = true,
            Radius = 2
        });

        return terrain;
    }

    private List<TerrainOverlay> GenerateCaveTerrain(int width, int height, Random random)
    {
        var terrain = new List<TerrainOverlay>();
        int id = 1;

        // Add cave walls around edges
        for (int x = 0; x < width; x++)
        {
            if (random.Next(100) < 40)
            {
                int h = random.Next(1, 3);
                terrain.Add(new TerrainOverlay
                {
                    Id = id++,
                    Name = "Cave Wall",
                    Type = TerrainType.Wall,
                    StartX = x,
                    StartY = 0,
                    Width = 1,
                    Height = h,
                    Color = "#3A3A3A",
                    Opacity = 1.0,
                    BlocksMovement = true,
                    BlocksSight = true
                });
            }
            if (random.Next(100) < 40)
            {
                int h = random.Next(1, 3);
                terrain.Add(new TerrainOverlay
                {
                    Id = id++,
                    Name = "Cave Wall",
                    Type = TerrainType.Wall,
                    StartX = x,
                    StartY = height - h,
                    Width = 1,
                    Height = h,
                    Color = "#3A3A3A",
                    Opacity = 1.0,
                    BlocksMovement = true,
                    BlocksSight = true
                });
            }
        }

        // Add stalagmites
        int stalagmiteCount = (width * height) / 15;
        for (int i = 0; i < stalagmiteCount; i++)
        {
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Stalagmite",
                Type = TerrainType.Cover,
                StartX = random.Next(2, width - 2),
                StartY = random.Next(2, height - 2),
                Width = 1,
                Height = 1,
                Color = "#5A5A5A",
                Opacity = 0.9,
                BlocksMovement = true
            });
        }

        // Add underground pool
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Underground Pool",
            Type = TerrainType.Water,
            StartX = width / 2 - 2,
            StartY = height - 4,
            Width = 4,
            Height = 3,
            Color = "#2A4A6A",
            Opacity = 0.7,
            IsDifficultTerrain = true
        });

        return terrain;
    }

    private List<TerrainOverlay> GenerateDungeonTerrain(int width, int height, Random random)
    {
        var terrain = new List<TerrainOverlay>();
        int id = 1;

        // Add walls creating rooms
        // Horizontal walls
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Wall",
            Type = TerrainType.Wall,
            StartX = 0,
            StartY = height / 3,
            Width = width * 2 / 3,
            Height = 1,
            Color = "#4A4A5A",
            Opacity = 1.0,
            BlocksMovement = true,
            BlocksSight = true
        });

        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Wall",
            Type = TerrainType.Wall,
            StartX = width / 3,
            StartY = height * 2 / 3,
            Width = width * 2 / 3,
            Height = 1,
            Color = "#4A4A5A",
            Opacity = 1.0,
            BlocksMovement = true,
            BlocksSight = true
        });

        // Add pillars
        for (int x = 4; x < width - 4; x += 6)
        {
            for (int y = 4; y < height - 4; y += 6)
            {
                terrain.Add(new TerrainOverlay
                {
                    Id = id++,
                    Name = "Pillar",
                    Type = TerrainType.Cover,
                    StartX = x,
                    StartY = y,
                    Width = 1,
                    Height = 1,
                    Color = "#5A5A6A",
                    Opacity = 1.0,
                    BlocksMovement = true,
                    BlocksSight = true
                });
            }
        }

        // Add trap
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Pit Trap",
            Type = TerrainType.Pit,
            StartX = width / 2,
            StartY = height / 2,
            Width = 2,
            Height = 2,
            Color = "#2A2A2A",
            Opacity = 0.3,
            EffectDescription = "DC 15 Perception to spot, 2d6 falling damage"
        });

        return terrain;
    }

    private List<TerrainOverlay> GenerateTownTerrain(int width, int height, Random random)
    {
        var terrain = new List<TerrainOverlay>();
        int id = 1;

        // Add buildings at corners
        var positions = new List<(int x, int y, int w, int h)>
        {
            (1, 1, 4, 3),
            (width - 5, 1, 4, 3),
            (1, height - 4, 4, 3),
            (width - 5, height - 4, 4, 3)
        };

        foreach (var pos in positions)
        {
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Building",
                Type = TerrainType.Wall,
                StartX = pos.x,
                StartY = pos.y,
                Width = pos.w,
                Height = pos.h,
                Color = "#6A5A4A",
                Opacity = 1.0,
                BlocksMovement = true,
                BlocksSight = true
            });
        }

        // Add market stalls
        for (int x = width / 3; x < width * 2 / 3; x += 3)
        {
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Market Stall",
                Type = TerrainType.Cover,
                StartX = x,
                StartY = height / 2,
                Width = 2,
                Height = 1,
                Color = "#8A6A4A",
                Opacity = 0.8,
                BlocksMovement = true
            });
        }

        // Add well
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Town Well",
            Type = TerrainType.Cover,
            StartX = width / 2,
            StartY = height / 2 - 2,
            Width = 1,
            Height = 1,
            Color = "#5A5A5A",
            Opacity = 0.9,
            BlocksMovement = true
        });

        return terrain;
    }

    private List<TerrainOverlay> GenerateCastleTerrain(int width, int height, Random random)
    {
        var terrain = new List<TerrainOverlay>();
        int id = 1;

        // Add outer walls
        // Top wall
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Castle Wall",
            Type = TerrainType.Wall,
            StartX = 0,
            StartY = 0,
            Width = width,
            Height = 1,
            Color = "#5A5A6A",
            Opacity = 1.0,
            BlocksMovement = true,
            BlocksSight = true
        });

        // Bottom wall
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Castle Wall",
            Type = TerrainType.Wall,
            StartX = 0,
            StartY = height - 1,
            Width = width,
            Height = 1,
            Color = "#5A5A6A",
            Opacity = 1.0,
            BlocksMovement = true,
            BlocksSight = true
        });

        // Left wall
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Castle Wall",
            Type = TerrainType.Wall,
            StartX = 0,
            StartY = 0,
            Width = 1,
            Height = height,
            Color = "#5A5A6A",
            Opacity = 1.0,
            BlocksMovement = true,
            BlocksSight = true
        });

        // Right wall
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Castle Wall",
            Type = TerrainType.Wall,
            StartX = width - 1,
            StartY = 0,
            Width = 1,
            Height = height,
            Color = "#5A5A6A",
            Opacity = 1.0,
            BlocksMovement = true,
            BlocksSight = true
        });

        // Add corner towers
        var corners = new[] { (1, 1), (width - 3, 1), (1, height - 3), (width - 3, height - 3) };
        foreach (var corner in corners)
        {
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Tower",
                Type = TerrainType.Wall,
                StartX = corner.Item1,
                StartY = corner.Item2,
                Width = 2,
                Height = 2,
                Color = "#4A4A5A",
                Opacity = 1.0,
                BlocksMovement = true,
                BlocksSight = true
            });
        }

        // Add throne/altar in center
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Throne",
            Type = TerrainType.Cover,
            StartX = width / 2,
            StartY = height / 4,
            Width = 1,
            Height = 1,
            Color = "#8A6A2A",
            Opacity = 0.9,
            BlocksMovement = true
        });

        // Add pillars
        for (int x = 4; x < width - 4; x += 4)
        {
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Pillar",
                Type = TerrainType.Cover,
                StartX = x,
                StartY = height / 2,
                Width = 1,
                Height = 1,
                Color = "#6A6A7A",
                Opacity = 1.0,
                BlocksMovement = true,
                BlocksSight = true
            });
        }

        return terrain;
    }

    private List<TerrainOverlay> GenerateBeachTerrain(int width, int height, Random random)
    {
        var terrain = new List<TerrainOverlay>();
        int id = 1;

        // Add water on one side
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Ocean",
            Type = TerrainType.Water,
            StartX = 0,
            StartY = 0,
            Width = width / 3,
            Height = height,
            Color = "#3A7AB0",
            Opacity = 0.7,
            IsDifficultTerrain = true
        });

        // Add tidal zone
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Tidal Zone",
            Type = TerrainType.DifficultTerrain,
            StartX = width / 3,
            StartY = 0,
            Width = 2,
            Height = height,
            Color = "#6A9AC0",
            Opacity = 0.4,
            IsDifficultTerrain = true
        });

        // Add rocks
        int rockCount = 5;
        for (int i = 0; i < rockCount; i++)
        {
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Beach Rock",
                Type = TerrainType.Cover,
                StartX = random.Next(width / 3, width - 2),
                StartY = random.Next(height),
                Width = 1,
                Height = 1,
                Color = "#7A7A6A",
                Opacity = 0.9,
                BlocksMovement = true
            });
        }

        // Add palm trees
        int palmCount = 3;
        for (int i = 0; i < palmCount; i++)
        {
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Palm Tree",
                Type = TerrainType.Cover,
                StartX = random.Next(width * 2 / 3, width - 1),
                StartY = random.Next(height),
                Width = 1,
                Height = 1,
                Color = "#2A6A2A",
                Opacity = 0.8,
                BlocksMovement = true
            });
        }

        return terrain;
    }

    private List<TerrainOverlay> GenerateTundraTerrain(int width, int height, Random random)
    {
        var terrain = new List<TerrainOverlay>();
        int id = 1;

        // Add ice patches
        int iceCount = (width * height) / 10;
        for (int i = 0; i < iceCount; i++)
        {
            int w = random.Next(2, 4);
            int h = random.Next(2, 4);
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Ice Patch",
                Type = TerrainType.DifficultTerrain,
                StartX = random.Next(width - w),
                StartY = random.Next(height - h),
                Width = w,
                Height = h,
                Color = "#B0D0E0",
                Opacity = 0.5,
                IsDifficultTerrain = true,
                EffectDescription = "DC 10 DEX save or fall prone when moving"
            });
        }

        // Add snow drifts
        int driftCount = (width * height) / 15;
        for (int i = 0; i < driftCount; i++)
        {
            int w = random.Next(1, 3);
            int h = random.Next(1, 2);
            terrain.Add(new TerrainOverlay
            {
                Id = id++,
                Name = "Snow Drift",
                Type = TerrainType.DifficultTerrain,
                StartX = random.Next(width - w),
                StartY = random.Next(height - h),
                Width = w,
                Height = h,
                Color = "#E0E8F0",
                Opacity = 0.6,
                IsDifficultTerrain = true
            });
        }

        // Add frozen pond
        terrain.Add(new TerrainOverlay
        {
            Id = id++,
            Name = "Frozen Pond",
            Type = TerrainType.Water,
            StartX = width / 2 - 2,
            StartY = height / 2 - 2,
            Width = 4,
            Height = 3,
            Color = "#90B0D0",
            Opacity = 0.7,
            EffectDescription = "May break through - DC 12 DEX save or fall into freezing water"
        });

        return terrain;
    }

    #endregion
}
