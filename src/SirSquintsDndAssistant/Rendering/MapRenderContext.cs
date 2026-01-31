using SkiaSharp;
using SirSquintsDndAssistant.Models.BattleMap;
using SirSquintsDndAssistant.Input;

namespace SirSquintsDndAssistant.Rendering;

/// <summary>
/// Contains all the state needed for rendering the battle map.
/// Passed to each layer during rendering.
/// </summary>
public class MapRenderContext
{
    /// <summary>The current battle map being rendered.</summary>
    public BattleMap? Map { get; set; }

    /// <summary>All tokens on the current map.</summary>
    public List<MapToken> Tokens { get; set; } = new();

    /// <summary>All terrain overlays on the current map.</summary>
    public List<TerrainOverlay> Terrain { get; set; } = new();

    /// <summary>Set of revealed cell coordinates (format: "x,y").</summary>
    public HashSet<string> RevealedCells { get; set; } = new();

    /// <summary>Size of each grid cell in pixels (before zoom).</summary>
    public float CellPixelSize { get; set; } = 40f;

    /// <summary>Viewport offset for panning (in pixels).</summary>
    public SKPoint ViewportOffset { get; set; } = SKPoint.Empty;

    /// <summary>Current zoom level (1.0 = 100%).</summary>
    public float ZoomLevel { get; set; } = 1.0f;

    /// <summary>Currently selected token, if any.</summary>
    public MapToken? SelectedToken { get; set; }

    /// <summary>Active measurement being drawn, if any.</summary>
    public MapMeasurement? ActiveMeasurement { get; set; }

    /// <summary>Current tool being used.</summary>
    public MapTool CurrentTool { get; set; } = MapTool.Select;

    /// <summary>Whether to show the grid.</summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>Whether fog of war is enabled.</summary>
    public bool UseFogOfWar { get; set; } = false;

    /// <summary>Whether viewing as DM (can see through fog).</summary>
    public bool IsDmView { get; set; } = true;

    /// <summary>Background image bitmap, if loaded.</summary>
    public SKBitmap? BackgroundImage { get; set; }

    /// <summary>Width of the canvas in pixels.</summary>
    public float CanvasWidth { get; set; }

    /// <summary>Height of the canvas in pixels.</summary>
    public float CanvasHeight { get; set; }

    /// <summary>
    /// Calculate the actual cell size considering zoom.
    /// </summary>
    public float EffectiveCellSize => CellPixelSize * ZoomLevel;

    /// <summary>
    /// Convert grid coordinates to screen coordinates.
    /// </summary>
    public SKPoint GridToScreen(int gridX, int gridY)
    {
        float x = gridX * EffectiveCellSize + ViewportOffset.X;
        float y = gridY * EffectiveCellSize + ViewportOffset.Y;
        return new SKPoint(x, y);
    }

    /// <summary>
    /// Convert screen coordinates to grid coordinates.
    /// </summary>
    public (int gridX, int gridY) ScreenToGrid(SKPoint screenPoint)
    {
        float adjustedX = (screenPoint.X - ViewportOffset.X) / EffectiveCellSize;
        float adjustedY = (screenPoint.Y - ViewportOffset.Y) / EffectiveCellSize;
        return ((int)Math.Floor(adjustedX), (int)Math.Floor(adjustedY));
    }

    /// <summary>
    /// Check if a grid cell is currently visible on screen.
    /// </summary>
    public bool IsCellVisible(int gridX, int gridY)
    {
        var screenPos = GridToScreen(gridX, gridY);
        return screenPos.X + EffectiveCellSize >= 0 &&
               screenPos.Y + EffectiveCellSize >= 0 &&
               screenPos.X <= CanvasWidth &&
               screenPos.Y <= CanvasHeight;
    }

    /// <summary>
    /// Get the visible grid bounds for culling.
    /// </summary>
    public (int minX, int minY, int maxX, int maxY) GetVisibleGridBounds()
    {
        var (minX, minY) = ScreenToGrid(SKPoint.Empty);
        var (maxX, maxY) = ScreenToGrid(new SKPoint(CanvasWidth, CanvasHeight));

        // Add padding for partially visible cells
        minX = Math.Max(0, minX - 1);
        minY = Math.Max(0, minY - 1);
        maxX = Math.Min(Map?.GridWidth ?? 100, maxX + 2);
        maxY = Math.Min(Map?.GridHeight ?? 100, maxY + 2);

        return (minX, minY, maxX, maxY);
    }
}
