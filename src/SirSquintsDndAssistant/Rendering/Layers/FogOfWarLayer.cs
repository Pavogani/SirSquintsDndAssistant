using SkiaSharp;

namespace SirSquintsDndAssistant.Rendering.Layers;

/// <summary>
/// Renders the fog of war overlay on the battle map.
/// </summary>
public class FogOfWarLayer : IMapLayer
{
#pragma warning disable CS0414 // Field is assigned but never read - reserved for future dirty tracking
    private bool _needsRedraw = true;
#pragma warning restore CS0414

    public MapLayerType LayerType => MapLayerType.FogOfWar;
    public int ZIndex => 40;
    public bool IsVisible { get; set; } = true;

    /// <summary>Color of the fog of war.</summary>
    public SKColor FogColor { get; set; } = new SKColor(20, 20, 20, 230);

    /// <summary>Whether to use smooth edges on the fog.</summary>
    public bool SmoothEdges { get; set; } = true;

    public void Render(SKCanvas canvas, MapRenderContext context)
    {
        // Skip if fog of war is disabled or in DM view (DM sees everything)
        if (!IsVisible || !context.UseFogOfWar) return;

        // In DM view, show fog as semi-transparent overlay
        byte alpha = context.IsDmView ? (byte)100 : (byte)230;

        var cellSize = context.EffectiveCellSize;
        var offset = context.ViewportOffset;
        var (minX, minY, maxX, maxY) = context.GetVisibleGridBounds();

        using var fogPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = FogColor.WithAlpha(alpha),
            IsAntialias = SmoothEdges
        };

        // Draw fog on unrevealed cells
        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                string cellKey = $"{x},{y}";

                // If cell is NOT revealed, draw fog
                if (!context.RevealedCells.Contains(cellKey))
                {
                    var rect = new SKRect(
                        x * cellSize + offset.X,
                        y * cellSize + offset.Y,
                        (x + 1) * cellSize + offset.X,
                        (y + 1) * cellSize + offset.Y
                    );

                    if (SmoothEdges)
                    {
                        // Check adjacent cells for smooth edges
                        DrawFogCellWithEdges(canvas, context, x, y, cellSize, offset, fogPaint);
                    }
                    else
                    {
                        canvas.DrawRect(rect, fogPaint);
                    }
                }
            }
        }

        _needsRedraw = false;
    }

    private void DrawFogCellWithEdges(SKCanvas canvas, MapRenderContext context, int x, int y,
        float cellSize, SKPoint offset, SKPaint fogPaint)
    {
        var rect = new SKRect(
            x * cellSize + offset.X,
            y * cellSize + offset.Y,
            (x + 1) * cellSize + offset.X,
            (y + 1) * cellSize + offset.Y
        );

        // Check if adjacent cells are revealed for soft edges
        bool topRevealed = context.RevealedCells.Contains($"{x},{y - 1}");
        bool bottomRevealed = context.RevealedCells.Contains($"{x},{y + 1}");
        bool leftRevealed = context.RevealedCells.Contains($"{x - 1},{y}");
        bool rightRevealed = context.RevealedCells.Contains($"{x + 1},{y}");

        // If any adjacent cell is revealed, use gradient edge
        if (topRevealed || bottomRevealed || leftRevealed || rightRevealed)
        {
            // Create a path with rounded corners toward revealed cells
            using var path = new SKPath();

            float cornerRadius = cellSize / 4;

            // Start building the path
            path.MoveTo(rect.Left + (leftRevealed ? cornerRadius : 0), rect.Top);

            // Top edge
            if (topRevealed)
            {
                path.LineTo(rect.Left + cornerRadius, rect.Top);
                path.QuadTo(rect.MidX, rect.Top + cornerRadius, rect.Right - cornerRadius, rect.Top);
            }
            else
            {
                path.LineTo(rect.Right - (rightRevealed ? cornerRadius : 0), rect.Top);
            }

            // Right edge
            if (rightRevealed)
            {
                path.QuadTo(rect.Right - cornerRadius, rect.MidY, rect.Right - (bottomRevealed ? cornerRadius : 0), rect.Bottom - (bottomRevealed ? cornerRadius : 0));
            }
            else
            {
                path.LineTo(rect.Right, rect.Bottom - (bottomRevealed ? cornerRadius : 0));
            }

            // Bottom edge
            if (bottomRevealed)
            {
                path.QuadTo(rect.MidX, rect.Bottom - cornerRadius, rect.Left + (leftRevealed ? cornerRadius : 0), rect.Bottom - (leftRevealed ? cornerRadius : 0));
            }
            else
            {
                path.LineTo(rect.Left + (leftRevealed ? cornerRadius : 0), rect.Bottom);
            }

            // Left edge
            if (leftRevealed)
            {
                path.QuadTo(rect.Left + cornerRadius, rect.MidY, rect.Left + (topRevealed ? cornerRadius : 0), rect.Top + (topRevealed ? cornerRadius : 0));
            }
            else
            {
                path.LineTo(rect.Left, rect.Top + (topRevealed ? cornerRadius : 0));
            }

            path.Close();
            canvas.DrawPath(path, fogPaint);
        }
        else
        {
            // No adjacent revealed cells, draw solid rectangle
            canvas.DrawRect(rect, fogPaint);
        }
    }

    public void Invalidate() => _needsRedraw = true;

    public void Dispose()
    {
        // No persistent resources
    }
}
