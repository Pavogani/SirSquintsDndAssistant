using SkiaSharp;

namespace SirSquintsDndAssistant.Rendering.Layers;

/// <summary>
/// Type of map layer for ordering and identification.
/// </summary>
public enum MapLayerType
{
    Background = 0,
    Grid = 10,
    Terrain = 20,
    Tokens = 30,
    FogOfWar = 40,
    UI = 50
}

/// <summary>
/// Interface for a renderable map layer.
/// Layers are rendered in order of their ZIndex.
/// </summary>
public interface IMapLayer : IDisposable
{
    /// <summary>The type of this layer.</summary>
    MapLayerType LayerType { get; }

    /// <summary>Z-index for rendering order (lower = rendered first/bottom).</summary>
    int ZIndex { get; }

    /// <summary>Whether this layer should be rendered.</summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Render this layer to the canvas.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
    /// <param name="context">The render context containing all map state.</param>
    void Render(SKCanvas canvas, MapRenderContext context);

    /// <summary>
    /// Mark this layer as needing redraw.
    /// </summary>
    void Invalidate();
}
