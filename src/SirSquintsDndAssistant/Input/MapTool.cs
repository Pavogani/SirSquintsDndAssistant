namespace SirSquintsDndAssistant.Input;

/// <summary>
/// Available tools for interacting with the battle map.
/// </summary>
public enum MapTool
{
    /// <summary>Pan the viewport by dragging.</summary>
    Pan,

    /// <summary>Select tokens by tapping.</summary>
    Select,

    /// <summary>Move selected token to a new position.</summary>
    Move,

    /// <summary>Measure distance between two points (line).</summary>
    MeasureLine,

    /// <summary>Draw a cone template for spell effects.</summary>
    MeasureCone,

    /// <summary>Draw a circle template for spell effects.</summary>
    MeasureCircle,

    /// <summary>Draw a square/cube template for spell effects.</summary>
    MeasureSquare,

    /// <summary>Reveal cells hidden by fog of war.</summary>
    RevealFog,

    /// <summary>Hide cells with fog of war.</summary>
    HideFog,

    /// <summary>Place terrain overlay on the map.</summary>
    PlaceTerrain,

    /// <summary>Place a new token on the map.</summary>
    PlaceToken,

    /// <summary>Delete tokens or terrain.</summary>
    Erase
}
