using SkiaSharp;

namespace SirSquintsDndAssistant.Rendering.Layers;

/// <summary>
/// Renders the grid lines on the battle map.
/// </summary>
public class GridLayer : IMapLayer
{
#pragma warning disable CS0414 // Field is assigned but never read - reserved for future dirty tracking
    private bool _needsRedraw = true;
#pragma warning restore CS0414
    private SKPaint? _gridPaint;
    private SKPaint? _majorGridPaint;
    private SKPaint? _coordPaint;

    public MapLayerType LayerType => MapLayerType.Grid;
    public int ZIndex => 10;
    public bool IsVisible { get; set; } = true;

    /// <summary>Whether to show coordinate labels on the grid.</summary>
    public bool ShowCoordinates { get; set; } = false;

    /// <summary>Draw major grid lines every N cells.</summary>
    public int MajorGridInterval { get; set; } = 5;

    public void Render(SKCanvas canvas, MapRenderContext context)
    {
        if (!IsVisible || !context.ShowGrid || context.Map == null) return;

        EnsurePaints(context);

        var (minX, minY, maxX, maxY) = context.GetVisibleGridBounds();
        var cellSize = context.EffectiveCellSize;
        var offset = context.ViewportOffset;

        // Draw vertical lines
        for (int x = minX; x <= maxX; x++)
        {
            float screenX = x * cellSize + offset.X;
            bool isMajor = x % MajorGridInterval == 0;
            var paint = isMajor ? _majorGridPaint : _gridPaint;

            canvas.DrawLine(
                screenX, minY * cellSize + offset.Y,
                screenX, maxY * cellSize + offset.Y,
                paint!
            );
        }

        // Draw horizontal lines
        for (int y = minY; y <= maxY; y++)
        {
            float screenY = y * cellSize + offset.Y;
            bool isMajor = y % MajorGridInterval == 0;
            var paint = isMajor ? _majorGridPaint : _gridPaint;

            canvas.DrawLine(
                minX * cellSize + offset.X, screenY,
                maxX * cellSize + offset.X, screenY,
                paint!
            );
        }

        // Draw coordinate labels if enabled
        if (ShowCoordinates && cellSize >= 20)
        {
            DrawCoordinates(canvas, context, minX, minY, maxX, maxY);
        }

        _needsRedraw = false;
    }

    private void DrawCoordinates(SKCanvas canvas, MapRenderContext context, int minX, int minY, int maxX, int maxY)
    {
        var cellSize = context.EffectiveCellSize;
        var offset = context.ViewportOffset;

        for (int x = minX; x <= maxX; x += MajorGridInterval)
        {
            for (int y = minY; y <= maxY; y += MajorGridInterval)
            {
                float screenX = x * cellSize + offset.X + 2;
                float screenY = y * cellSize + offset.Y + 10;

                canvas.DrawText($"{x},{y}", screenX, screenY, _coordPaint!);
            }
        }
    }

    private void EnsurePaints(MapRenderContext context)
    {
        var gridColor = SKColor.Parse(context.Map?.GridColor ?? "#666666");
        var gridOpacity = (byte)((context.Map?.GridOpacity ?? 0.5f) * 255);

        _gridPaint ??= new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };
        _gridPaint.Color = gridColor.WithAlpha(gridOpacity);

        _majorGridPaint ??= new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };
        _majorGridPaint.Color = gridColor.WithAlpha((byte)Math.Min(255, gridOpacity + 50));

        _coordPaint ??= new SKPaint
        {
            Color = SKColors.White.WithAlpha(150),
            TextSize = 10,
            IsAntialias = true
        };
    }

    public void Invalidate() => _needsRedraw = true;

    public void Dispose()
    {
        _gridPaint?.Dispose();
        _majorGridPaint?.Dispose();
        _coordPaint?.Dispose();
    }
}
