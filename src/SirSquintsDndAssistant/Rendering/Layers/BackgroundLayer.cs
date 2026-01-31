using SkiaSharp;

namespace SirSquintsDndAssistant.Rendering.Layers;

/// <summary>
/// Renders the map background (solid color or image).
/// </summary>
public class BackgroundLayer : IMapLayer
{
#pragma warning disable CS0414 // Field is assigned but never read - reserved for future dirty tracking
    private bool _needsRedraw = true;
#pragma warning restore CS0414

    public MapLayerType LayerType => MapLayerType.Background;
    public int ZIndex => 0;
    public bool IsVisible { get; set; } = true;

    public void Render(SKCanvas canvas, MapRenderContext context)
    {
        if (!IsVisible || context.Map == null) return;

        // Clear with background color first
        var bgColor = SKColor.Parse(context.Map.BackgroundColor ?? "#2d2d2d");
        canvas.Clear(bgColor);

        // Draw background image if available
        if (context.BackgroundImage != null)
        {
            DrawBackgroundImage(canvas, context);
        }

        _needsRedraw = false;
    }

    private void DrawBackgroundImage(SKCanvas canvas, MapRenderContext context)
    {
        if (context.BackgroundImage == null) return;

        var image = context.BackgroundImage;

        // Calculate how the image should be positioned
        // The image should fill the grid area
        var gridWidth = (context.Map?.GridWidth ?? 20) * context.EffectiveCellSize;
        var gridHeight = (context.Map?.GridHeight ?? 20) * context.EffectiveCellSize;

        var destRect = new SKRect(
            context.ViewportOffset.X,
            context.ViewportOffset.Y,
            context.ViewportOffset.X + gridWidth,
            context.ViewportOffset.Y + gridHeight
        );

        using var paint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.Medium
        };

        canvas.DrawBitmap(image, destRect, paint);
    }

    public void Invalidate() => _needsRedraw = true;

    public void Dispose()
    {
        // No resources to dispose
    }
}
