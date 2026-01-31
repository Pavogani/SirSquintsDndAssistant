using SQLite;
using System.ComponentModel.DataAnnotations;

namespace SirSquintsDndAssistant.Models.BattleMap;

/// <summary>
/// Represents a battle map for tactical combat.
/// </summary>
public class BattleMap
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Grid settings
    [Range(1, 200, ErrorMessage = "Grid width must be between 1 and 200")]
    public int GridWidth { get; set; } = 20; // Number of cells

    [Range(1, 200, ErrorMessage = "Grid height must be between 1 and 200")]
    public int GridHeight { get; set; } = 15;

    [Range(1, 30, ErrorMessage = "Cell size must be between 1 and 30 feet")]
    public int CellSize { get; set; } = 5; // Feet per cell (5ft standard)

    // Background
    public string BackgroundImagePath { get; set; } = string.Empty;
    public MapBackgroundType BackgroundType { get; set; } = MapBackgroundType.Grid;

    // Grid appearance
    public bool ShowGrid { get; set; } = true;
    public string GridColor { get; set; } = "#808080";
    public double GridOpacity { get; set; } = 0.5;
    public string BackgroundColor { get; set; } = "#2D2D2D";

    // Fog of War
    public bool UseFogOfWar { get; set; }
    public string RevealedCellsJson { get; set; } = "[]"; // List of revealed grid coordinates

    // Lighting
    public LightingCondition Lighting { get; set; } = LightingCondition.BrightLight;

    // Terrain data (stored as JSON)
    public string TerrainJson { get; set; } = "[]"; // List of terrain overlays

    // Associated combat
    public int? CombatEncounterId { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsFavorite { get; set; }
    public string Tags { get; set; } = string.Empty;
}

public enum MapBackgroundType
{
    Grid,
    Image,
    Solid
}

public enum LightingCondition
{
    BrightLight,
    DimLight,
    Darkness
}

/// <summary>
/// Represents a token (creature/object) on the battle map.
/// </summary>
public class MapToken
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int BattleMapId { get; set; }
    public int? InitiativeEntryId { get; set; } // Links to combat tracker

    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty; // Short label shown on token

    // Position (grid coordinates)
    public int GridX { get; set; }
    public int GridY { get; set; }

    // Size
    public CreatureSize Size { get; set; } = CreatureSize.Medium;

    // Appearance
    public string Color { get; set; } = "#FF0000"; // Token color
    public string ImagePath { get; set; } = string.Empty;
    public TokenShape Shape { get; set; } = TokenShape.Circle;

    // Status
    public bool IsVisible { get; set; } = true;
    public bool IsEnemy { get; set; }
    public int CurrentHP { get; set; }
    public int MaxHP { get; set; }

    // Conditions (visual indicators)
    public string ConditionsJson { get; set; } = "[]";

    // Movement tracking
    public int MovementUsed { get; set; }
    public int MovementTotal { get; set; } = 30;
    public string MovementPathJson { get; set; } = "[]"; // Path taken this turn

    // Auras/Effects
    public string AurasJson { get; set; } = "[]"; // Visual effect radiuses
}

public enum CreatureSize
{
    Tiny,      // 2.5 ft, shares space (1 cell)
    Small,     // 5 ft (1 cell)
    Medium,    // 5 ft (1 cell)
    Large,     // 10 ft (2 cells)
    Huge,      // 15 ft (3 cells)
    Gargantuan // 20+ ft (4 cells)
}

public enum TokenShape
{
    Circle,
    Square,
    Image
}

/// <summary>
/// Represents terrain or an effect overlay on the map.
/// </summary>
public class TerrainOverlay
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TerrainType Type { get; set; }

    // Area (grid coordinates)
    public int StartX { get; set; }
    public int StartY { get; set; }
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;

    // For circular areas (spells, etc)
    public bool IsCircular { get; set; }
    public int Radius { get; set; } // In grid cells

    // Appearance
    public string Color { get; set; } = "#00FF00";
    public double Opacity { get; set; } = 0.3;
    public string Pattern { get; set; } = string.Empty; // "solid", "striped", "dotted"

    // Effect
    public bool BlocksMovement { get; set; }
    public bool BlocksSight { get; set; }
    public bool IsDifficultTerrain { get; set; }
    public string EffectDescription { get; set; } = string.Empty;
}

public enum TerrainType
{
    Normal,
    DifficultTerrain,
    Water,
    Lava,
    Pit,
    Wall,
    Cover,
    SpellEffect,
    Hazard,
    Custom
}

/// <summary>
/// Represents a measurement or line on the map.
/// </summary>
public class MapMeasurement
{
    public int StartX { get; set; }
    public int StartY { get; set; }
    public int EndX { get; set; }
    public int EndY { get; set; }

    public string Color { get; set; } = "#FFFF00";
    public MeasurementType MeasurementType { get; set; }

    public int DistanceFeet => (int)CalculateDistance();
    public double DistanceInFeet => CalculateDistance();

    private double CalculateDistance()
    {
        // Standard 5ft grid diagonal movement (every other diagonal = 10ft)
        var dx = Math.Abs(EndX - StartX);
        var dy = Math.Abs(EndY - StartY);
        var diagonals = Math.Min(dx, dy);
        var straights = Math.Max(dx, dy) - diagonals;
        return (straights * 5) + (diagonals * 5) + ((diagonals / 2) * 5);
    }
}

public enum MeasurementType
{
    Line,
    Cone,
    Circle,
    Square
}
