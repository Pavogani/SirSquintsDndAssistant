using SkiaSharp;
using SirSquintsDndAssistant.Models.BattleMap;

namespace SirSquintsDndAssistant.Rendering.Layers;

/// <summary>
/// Renders terrain overlays on the battle map.
/// </summary>
public class TerrainLayer : IMapLayer
{
#pragma warning disable CS0414 // Field is assigned but never read - reserved for future dirty tracking
    private bool _needsRedraw = true;
#pragma warning restore CS0414

    public MapLayerType LayerType => MapLayerType.Terrain;
    public int ZIndex => 20;
    public bool IsVisible { get; set; } = true;

    public void Render(SKCanvas canvas, MapRenderContext context)
    {
        if (!IsVisible || context.Terrain == null || context.Terrain.Count == 0) return;

        foreach (var terrain in context.Terrain)
        {
            DrawTerrain(canvas, context, terrain);
        }

        _needsRedraw = false;
    }

    private void DrawTerrain(SKCanvas canvas, MapRenderContext context, TerrainOverlay terrain)
    {
        var cellSize = context.EffectiveCellSize;
        var offset = context.ViewportOffset;

        // Get terrain color based on type
        var color = GetTerrainColor(terrain);
        var opacity = (byte)(terrain.Opacity * 255);

        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = color.WithAlpha(opacity),
            IsAntialias = true
        };

        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = color.WithAlpha((byte)Math.Min(255, opacity + 50)),
            StrokeWidth = 2,
            IsAntialias = true
        };

        if (terrain.IsCircular)
        {
            // Draw circular terrain
            float centerX = terrain.StartX * cellSize + offset.X + (terrain.Radius * cellSize);
            float centerY = terrain.StartY * cellSize + offset.Y + (terrain.Radius * cellSize);
            float radius = terrain.Radius * cellSize;

            canvas.DrawCircle(centerX, centerY, radius, fillPaint);
            canvas.DrawCircle(centerX, centerY, radius, strokePaint);
        }
        else
        {
            // Draw rectangular terrain
            var rect = new SKRect(
                terrain.StartX * cellSize + offset.X,
                terrain.StartY * cellSize + offset.Y,
                (terrain.StartX + terrain.Width) * cellSize + offset.X,
                (terrain.StartY + terrain.Height) * cellSize + offset.Y
            );

            canvas.DrawRect(rect, fillPaint);
            canvas.DrawRect(rect, strokePaint);

            // Draw pattern for difficult terrain
            if (terrain.IsDifficultTerrain)
            {
                DrawDifficultTerrainPattern(canvas, rect, color);
            }
        }

        // Draw label if present
        if (!string.IsNullOrEmpty(terrain.EffectDescription))
        {
            DrawTerrainLabel(canvas, context, terrain);
        }
    }

    private void DrawDifficultTerrainPattern(SKCanvas canvas, SKRect rect, SKColor color)
    {
        using var patternPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = color.WithAlpha(100),
            StrokeWidth = 1,
            IsAntialias = true
        };

        // Draw diagonal stripes
        float spacing = 10;
        for (float x = rect.Left - rect.Height; x < rect.Right; x += spacing)
        {
            canvas.Save();
            canvas.ClipRect(rect);
            canvas.DrawLine(x, rect.Bottom, x + rect.Height, rect.Top, patternPaint);
            canvas.Restore();
        }
    }

    private void DrawTerrainLabel(SKCanvas canvas, MapRenderContext context, TerrainOverlay terrain)
    {
        var cellSize = context.EffectiveCellSize;
        var offset = context.ViewportOffset;

        float centerX = (terrain.StartX + terrain.Width / 2f) * cellSize + offset.X;
        float centerY = (terrain.StartY + terrain.Height / 2f) * cellSize + offset.Y;

        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 12,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };

        // Draw text background
        var text = terrain.EffectDescription ?? "";
        var textBounds = new SKRect();
        textPaint.MeasureText(text, ref textBounds);

        using var bgPaint = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(150),
            Style = SKPaintStyle.Fill
        };

        var bgRect = new SKRect(
            centerX - textBounds.Width / 2 - 4,
            centerY - textBounds.Height / 2 - 2,
            centerX + textBounds.Width / 2 + 4,
            centerY + textBounds.Height / 2 + 2
        );

        canvas.DrawRoundRect(bgRect, 4, 4, bgPaint);
        canvas.DrawText(text, centerX, centerY + 4, textPaint);
    }

    private SKColor GetTerrainColor(TerrainOverlay terrain)
    {
        if (!string.IsNullOrEmpty(terrain.Color))
        {
            return SKColor.Parse(terrain.Color);
        }

        return terrain.Type switch
        {
            TerrainType.DifficultTerrain => new SKColor(139, 90, 43), // Brown
            TerrainType.Water => new SKColor(64, 164, 223), // Blue
            TerrainType.Lava => new SKColor(255, 69, 0), // Red-orange
            TerrainType.Pit => new SKColor(30, 30, 30), // Dark gray
            TerrainType.Wall => new SKColor(100, 100, 100), // Gray
            TerrainType.Cover => new SKColor(34, 139, 34), // Green
            TerrainType.SpellEffect => new SKColor(147, 112, 219), // Purple
            TerrainType.Hazard => new SKColor(255, 215, 0), // Gold
            TerrainType.Custom => new SKColor(200, 200, 200), // Light gray
            _ => new SKColor(128, 128, 128) // Default gray
        };
    }

    public void Invalidate() => _needsRedraw = true;

    public void Dispose()
    {
        // No persistent resources
    }
}
