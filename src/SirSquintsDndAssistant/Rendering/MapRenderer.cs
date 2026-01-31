using SkiaSharp;
using SirSquintsDndAssistant.Models.BattleMap;
using SirSquintsDndAssistant.Rendering.Layers;
using SirSquintsDndAssistant.Input;

namespace SirSquintsDndAssistant.Rendering;

/// <summary>
/// Core rendering engine for the battle map.
/// Manages layers and coordinates rendering.
/// </summary>
public class MapRenderer : IDisposable
{
    private readonly List<IMapLayer> _layers;
    private readonly MapRenderContext _context;
    private bool _disposed;

    // Layer references for direct access
    private readonly BackgroundLayer _backgroundLayer;
    private readonly GridLayer _gridLayer;
    private readonly TerrainLayer _terrainLayer;
    private readonly TokenLayer _tokenLayer;
    private readonly FogOfWarLayer _fogLayer;
    private readonly UILayer _uiLayer;

    public MapRenderer()
    {
        _context = new MapRenderContext();

        // Create layers
        _backgroundLayer = new BackgroundLayer();
        _gridLayer = new GridLayer();
        _terrainLayer = new TerrainLayer();
        _tokenLayer = new TokenLayer();
        _fogLayer = new FogOfWarLayer();
        _uiLayer = new UILayer();

        // Add layers in render order
        _layers = new List<IMapLayer>
        {
            _backgroundLayer,
            _gridLayer,
            _terrainLayer,
            _tokenLayer,
            _fogLayer,
            _uiLayer
        };
    }

    #region Properties

    /// <summary>Current viewport offset for panning.</summary>
    public SKPoint ViewportOffset
    {
        get => _context.ViewportOffset;
        set => _context.ViewportOffset = value;
    }

    /// <summary>Current zoom level.</summary>
    public float ZoomLevel
    {
        get => _context.ZoomLevel;
        set => _context.ZoomLevel = Math.Clamp(value, 0.25f, 4.0f);
    }

    /// <summary>Base cell size in pixels (before zoom).</summary>
    public float CellPixelSize
    {
        get => _context.CellPixelSize;
        set => _context.CellPixelSize = value;
    }

    /// <summary>Current tool being used.</summary>
    public MapTool CurrentTool
    {
        get => _context.CurrentTool;
        set => _context.CurrentTool = value;
    }

    /// <summary>Whether to show the grid.</summary>
    public bool ShowGrid
    {
        get => _context.ShowGrid;
        set => _context.ShowGrid = value;
    }

    /// <summary>Whether fog of war is enabled.</summary>
    public bool UseFogOfWar
    {
        get => _context.UseFogOfWar;
        set => _context.UseFogOfWar = value;
    }

    /// <summary>Whether viewing as DM (can see through fog).</summary>
    public bool IsDmView
    {
        get => _context.IsDmView;
        set => _context.IsDmView = value;
    }

    /// <summary>Currently selected token.</summary>
    public MapToken? SelectedToken
    {
        get => _context.SelectedToken;
        set => _context.SelectedToken = value;
    }

    /// <summary>Active measurement being drawn.</summary>
    public MapMeasurement? ActiveMeasurement
    {
        get => _context.ActiveMeasurement;
        set => _context.ActiveMeasurement = value;
    }

    /// <summary>Current hover cell position.</summary>
    public (int x, int y)? HoverCell
    {
        get => _uiLayer.HoverCell;
        set => _uiLayer.HoverCell = value;
    }

    /// <summary>Measurement start position.</summary>
    public (int x, int y)? MeasureStart
    {
        get => _uiLayer.MeasureStart;
        set => _uiLayer.MeasureStart = value;
    }

    /// <summary>Measurement end position.</summary>
    public (int x, int y)? MeasureEnd
    {
        get => _uiLayer.MeasureEnd;
        set => _uiLayer.MeasureEnd = value;
    }

    #endregion

    #region Map Data

    /// <summary>
    /// Set the current battle map to render.
    /// </summary>
    public void SetMap(BattleMap? map)
    {
        _context.Map = map;

        if (map != null)
        {
            _context.ShowGrid = map.ShowGrid;
            _context.UseFogOfWar = map.UseFogOfWar;

            // Parse revealed cells
            _context.RevealedCells.Clear();
            if (!string.IsNullOrEmpty(map.RevealedCellsJson) && map.RevealedCellsJson != "[]")
            {
                try
                {
                    var cells = System.Text.Json.JsonSerializer.Deserialize<List<string>>(map.RevealedCellsJson);
                    if (cells != null)
                    {
                        foreach (var cell in cells)
                        {
                            _context.RevealedCells.Add(cell);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing revealed cells JSON: {ex.Message}");
                }
            }

            // Parse terrain
            _context.Terrain.Clear();
            if (!string.IsNullOrEmpty(map.TerrainJson) && map.TerrainJson != "[]")
            {
                try
                {
                    var terrain = System.Text.Json.JsonSerializer.Deserialize<List<TerrainOverlay>>(map.TerrainJson);
                    if (terrain != null)
                    {
                        _context.Terrain.AddRange(terrain);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing terrain JSON: {ex.Message}");
                }
            }
        }

        InvalidateAll();
    }

    /// <summary>
    /// Set the tokens to render.
    /// </summary>
    public void SetTokens(IEnumerable<MapToken>? tokens)
    {
        _context.Tokens.Clear();
        if (tokens != null)
        {
            _context.Tokens.AddRange(tokens);
        }
        _tokenLayer.Invalidate();
    }

    /// <summary>
    /// Set the background image.
    /// </summary>
    public void SetBackgroundImage(SKBitmap? image)
    {
        _context.BackgroundImage?.Dispose();
        _context.BackgroundImage = image;
        _backgroundLayer.Invalidate();
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Render the entire map to the canvas.
    /// </summary>
    public void Render(SKCanvas canvas, SKImageInfo info)
    {
        _context.CanvasWidth = info.Width;
        _context.CanvasHeight = info.Height;

        // Render each layer in order
        foreach (var layer in _layers.OrderBy(l => l.ZIndex))
        {
            if (layer.IsVisible)
            {
                layer.Render(canvas, _context);
            }
        }
    }

    /// <summary>
    /// Invalidate all layers for redraw.
    /// </summary>
    public void InvalidateAll()
    {
        foreach (var layer in _layers)
        {
            layer.Invalidate();
        }
    }

    /// <summary>
    /// Invalidate a specific layer type.
    /// </summary>
    public void InvalidateLayer(MapLayerType layerType)
    {
        var layer = _layers.FirstOrDefault(l => l.LayerType == layerType);
        layer?.Invalidate();
    }

    #endregion

    #region Coordinate Conversion

    /// <summary>
    /// Convert screen coordinates to grid coordinates.
    /// </summary>
    public (int gridX, int gridY) ScreenToGrid(SKPoint screenPoint)
    {
        return _context.ScreenToGrid(screenPoint);
    }

    /// <summary>
    /// Convert grid coordinates to screen coordinates.
    /// </summary>
    public SKPoint GridToScreen(int gridX, int gridY)
    {
        return _context.GridToScreen(gridX, gridY);
    }

    /// <summary>
    /// Get the token at a screen position, if any.
    /// </summary>
    public MapToken? GetTokenAtScreen(SKPoint screenPoint)
    {
        var (gridX, gridY) = ScreenToGrid(screenPoint);
        return GetTokenAtGrid(gridX, gridY);
    }

    /// <summary>
    /// Get the token at a grid position, if any.
    /// </summary>
    public MapToken? GetTokenAtGrid(int gridX, int gridY)
    {
        // Check tokens in reverse order (top tokens first)
        foreach (var token in _context.Tokens.AsEnumerable().Reverse())
        {
            int tokenSize = GetTokenGridSize(token.Size);

            if (gridX >= token.GridX && gridX < token.GridX + tokenSize &&
                gridY >= token.GridY && gridY < token.GridY + tokenSize)
            {
                return token;
            }
        }

        return null;
    }

    private int GetTokenGridSize(CreatureSize size)
    {
        return size switch
        {
            CreatureSize.Tiny => 1,
            CreatureSize.Small => 1,
            CreatureSize.Medium => 1,
            CreatureSize.Large => 2,
            CreatureSize.Huge => 3,
            CreatureSize.Gargantuan => 4,
            _ => 1
        };
    }

    #endregion

    #region Viewport Control

    /// <summary>
    /// Pan the viewport by a delta amount.
    /// </summary>
    public void Pan(float deltaX, float deltaY)
    {
        ViewportOffset = new SKPoint(
            ViewportOffset.X + deltaX,
            ViewportOffset.Y + deltaY
        );
        InvalidateAll();
    }

    /// <summary>
    /// Zoom the viewport around a center point.
    /// </summary>
    public void Zoom(float zoomDelta, SKPoint center)
    {
        float oldZoom = ZoomLevel;
        float newZoom = Math.Clamp(oldZoom + zoomDelta, 0.25f, 4.0f);

        if (Math.Abs(newZoom - oldZoom) < 0.001f) return;

        // Adjust viewport to zoom around the center point
        float zoomRatio = newZoom / oldZoom;

        float newOffsetX = center.X - (center.X - ViewportOffset.X) * zoomRatio;
        float newOffsetY = center.Y - (center.Y - ViewportOffset.Y) * zoomRatio;

        ZoomLevel = newZoom;
        ViewportOffset = new SKPoint(newOffsetX, newOffsetY);

        InvalidateAll();
    }

    /// <summary>
    /// Center the viewport on a grid position.
    /// </summary>
    public void CenterOnGrid(int gridX, int gridY, float canvasWidth, float canvasHeight)
    {
        var screenCenter = new SKPoint(canvasWidth / 2, canvasHeight / 2);
        var cellCenter = new SKPoint(
            (gridX + 0.5f) * _context.EffectiveCellSize,
            (gridY + 0.5f) * _context.EffectiveCellSize
        );

        ViewportOffset = new SKPoint(
            screenCenter.X - cellCenter.X,
            screenCenter.Y - cellCenter.Y
        );

        InvalidateAll();
    }

    /// <summary>
    /// Reset the viewport to default position and zoom.
    /// </summary>
    public void ResetViewport()
    {
        ViewportOffset = SKPoint.Empty;
        ZoomLevel = 1.0f;
        InvalidateAll();
    }

    #endregion

    #region Fog of War

    /// <summary>
    /// Reveal cells in a rectangular area.
    /// </summary>
    public void RevealCells(int startX, int startY, int width, int height)
    {
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                _context.RevealedCells.Add($"{x},{y}");
            }
        }
        _fogLayer.Invalidate();
    }

    /// <summary>
    /// Hide cells in a rectangular area.
    /// </summary>
    public void HideCells(int startX, int startY, int width, int height)
    {
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                _context.RevealedCells.Remove($"{x},{y}");
            }
        }
        _fogLayer.Invalidate();
    }

    /// <summary>
    /// Get the current set of revealed cells for saving.
    /// </summary>
    public HashSet<string> GetRevealedCells()
    {
        return new HashSet<string>(_context.RevealedCells);
    }

    #endregion

    #region Disposal

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var layer in _layers)
        {
            layer.Dispose();
        }

        _context.BackgroundImage?.Dispose();
        _disposed = true;
    }

    #endregion
}
