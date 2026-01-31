using SkiaSharp;
using SirSquintsDndAssistant.Models.BattleMap;
using SirSquintsDndAssistant.Input;

namespace SirSquintsDndAssistant.Rendering.Layers;

/// <summary>
/// Renders UI elements like selection indicators, measurements, and tool previews.
/// </summary>
public class UILayer : IMapLayer
{
#pragma warning disable CS0414 // Field is assigned but never read - reserved for future dirty tracking
    private bool _needsRedraw = true;
#pragma warning restore CS0414

    public MapLayerType LayerType => MapLayerType.UI;
    public int ZIndex => 50;
    public bool IsVisible { get; set; } = true;

    /// <summary>Current position for hover effects (grid coordinates).</summary>
    public (int x, int y)? HoverCell { get; set; }

    /// <summary>Start position for measurement (grid coordinates).</summary>
    public (int x, int y)? MeasureStart { get; set; }

    /// <summary>End position for measurement (grid coordinates).</summary>
    public (int x, int y)? MeasureEnd { get; set; }

    public void Render(SKCanvas canvas, MapRenderContext context)
    {
        if (!IsVisible) return;

        // Draw hover highlight
        if (HoverCell.HasValue)
        {
            DrawHoverHighlight(canvas, context, HoverCell.Value.x, HoverCell.Value.y);
        }

        // Draw active measurement
        if (context.ActiveMeasurement != null)
        {
            DrawMeasurement(canvas, context, context.ActiveMeasurement);
        }
        else if (MeasureStart.HasValue && MeasureEnd.HasValue)
        {
            DrawMeasurementPreview(canvas, context);
        }

        // Draw tool-specific UI
        DrawToolUI(canvas, context);

        _needsRedraw = false;
    }

    private void DrawHoverHighlight(SKCanvas canvas, MapRenderContext context, int gridX, int gridY)
    {
        var cellSize = context.EffectiveCellSize;
        var offset = context.ViewportOffset;

        var rect = new SKRect(
            gridX * cellSize + offset.X + 2,
            gridY * cellSize + offset.Y + 2,
            (gridX + 1) * cellSize + offset.X - 2,
            (gridY + 1) * cellSize + offset.Y - 2
        );

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.White.WithAlpha(100),
            StrokeWidth = 2,
            IsAntialias = true
        };

        canvas.DrawRoundRect(rect, 4, 4, paint);
    }

    private void DrawMeasurement(SKCanvas canvas, MapRenderContext context, MapMeasurement measurement)
    {
        var cellSize = context.EffectiveCellSize;
        var offset = context.ViewportOffset;

        // Calculate screen positions
        float startX = (measurement.StartX + 0.5f) * cellSize + offset.X;
        float startY = (measurement.StartY + 0.5f) * cellSize + offset.Y;
        float endX = (measurement.EndX + 0.5f) * cellSize + offset.X;
        float endY = (measurement.EndY + 0.5f) * cellSize + offset.Y;

        var color = SKColor.Parse(measurement.Color ?? "#ffff00");

        switch (measurement.MeasurementType)
        {
            case MeasurementType.Line:
                DrawLineMeasurement(canvas, startX, startY, endX, endY, color, measurement.DistanceFeet);
                break;
            case MeasurementType.Cone:
                DrawConeMeasurement(canvas, startX, startY, endX, endY, color, cellSize);
                break;
            case MeasurementType.Circle:
                DrawCircleMeasurement(canvas, startX, startY, endX, endY, color, cellSize);
                break;
            case MeasurementType.Square:
                DrawSquareMeasurement(canvas, startX, startY, endX, endY, color);
                break;
        }
    }

    private void DrawLineMeasurement(SKCanvas canvas, float startX, float startY, float endX, float endY, SKColor color, int distance)
    {
        // Draw line
        using var linePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = color,
            StrokeWidth = 3,
            IsAntialias = true
        };

        canvas.DrawLine(startX, startY, endX, endY, linePaint);

        // Draw endpoints
        using var endpointPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = color,
            IsAntialias = true
        };

        canvas.DrawCircle(startX, startY, 6, endpointPaint);
        canvas.DrawCircle(endX, endY, 6, endpointPaint);

        // Draw distance label
        float midX = (startX + endX) / 2;
        float midY = (startY + endY) / 2;

        DrawDistanceLabel(canvas, midX, midY, $"{distance} ft");
    }

    private void DrawConeMeasurement(SKCanvas canvas, float startX, float startY, float endX, float endY, SKColor color, float cellSize)
    {
        // Calculate cone parameters
        float dx = endX - startX;
        float dy = endY - startY;
        float length = (float)Math.Sqrt(dx * dx + dy * dy);
        float angle = (float)Math.Atan2(dy, dx);

        // Cone width is equal to length (53-degree cone)
        float halfWidth = length / 2;

        using var path = new SKPath();
        path.MoveTo(startX, startY);

        // Calculate cone edges
        float leftAngle = angle - (float)Math.PI / 4;
        float rightAngle = angle + (float)Math.PI / 4;

        path.LineTo(
            startX + length * (float)Math.Cos(leftAngle),
            startY + length * (float)Math.Sin(leftAngle)
        );
        path.LineTo(
            startX + length * (float)Math.Cos(rightAngle),
            startY + length * (float)Math.Sin(rightAngle)
        );
        path.Close();

        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = color.WithAlpha(60),
            IsAntialias = true
        };

        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = color,
            StrokeWidth = 2,
            IsAntialias = true
        };

        canvas.DrawPath(path, fillPaint);
        canvas.DrawPath(path, strokePaint);

        // Distance label
        int distanceFeet = (int)(length / cellSize * 5);
        DrawDistanceLabel(canvas, endX, endY, $"{distanceFeet} ft cone");
    }

    private void DrawCircleMeasurement(SKCanvas canvas, float startX, float startY, float endX, float endY, SKColor color, float cellSize)
    {
        float dx = endX - startX;
        float dy = endY - startY;
        float radius = (float)Math.Sqrt(dx * dx + dy * dy);

        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = color.WithAlpha(60),
            IsAntialias = true
        };

        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = color,
            StrokeWidth = 2,
            IsAntialias = true
        };

        canvas.DrawCircle(startX, startY, radius, fillPaint);
        canvas.DrawCircle(startX, startY, radius, strokePaint);

        // Distance label
        int radiusFeet = (int)(radius / cellSize * 5);
        DrawDistanceLabel(canvas, startX, startY - radius - 20, $"{radiusFeet} ft radius");
    }

    private void DrawSquareMeasurement(SKCanvas canvas, float startX, float startY, float endX, float endY, SKColor color)
    {
        var rect = new SKRect(
            Math.Min(startX, endX),
            Math.Min(startY, endY),
            Math.Max(startX, endX),
            Math.Max(startY, endY)
        );

        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = color.WithAlpha(60),
            IsAntialias = true
        };

        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = color,
            StrokeWidth = 2,
            IsAntialias = true
        };

        canvas.DrawRect(rect, fillPaint);
        canvas.DrawRect(rect, strokePaint);
    }

    private void DrawMeasurementPreview(SKCanvas canvas, MapRenderContext context)
    {
        if (!MeasureStart.HasValue || !MeasureEnd.HasValue) return;

        var cellSize = context.EffectiveCellSize;
        var offset = context.ViewportOffset;

        float startX = (MeasureStart.Value.x + 0.5f) * cellSize + offset.X;
        float startY = (MeasureStart.Value.y + 0.5f) * cellSize + offset.Y;
        float endX = (MeasureEnd.Value.x + 0.5f) * cellSize + offset.X;
        float endY = (MeasureEnd.Value.y + 0.5f) * cellSize + offset.Y;

        // Calculate distance using D&D 5e diagonal rules
        int distance = CalculateDistance(
            MeasureStart.Value.x, MeasureStart.Value.y,
            MeasureEnd.Value.x, MeasureEnd.Value.y
        );

        var color = SKColors.Yellow;

        switch (context.CurrentTool)
        {
            case MapTool.MeasureLine:
                DrawLineMeasurement(canvas, startX, startY, endX, endY, color, distance);
                break;
            case MapTool.MeasureCone:
                DrawConeMeasurement(canvas, startX, startY, endX, endY, color, cellSize);
                break;
            case MapTool.MeasureCircle:
                DrawCircleMeasurement(canvas, startX, startY, endX, endY, color, cellSize);
                break;
            case MapTool.MeasureSquare:
                DrawSquareMeasurement(canvas, startX, startY, endX, endY, color);
                break;
        }
    }

    private void DrawDistanceLabel(SKCanvas canvas, float x, float y, string text)
    {
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 14,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            FakeBoldText = true
        };

        var bounds = new SKRect();
        textPaint.MeasureText(text, ref bounds);

        using var bgPaint = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(180),
            Style = SKPaintStyle.Fill
        };

        var bgRect = new SKRect(
            x - bounds.Width / 2 - 6,
            y - bounds.Height - 4,
            x + bounds.Width / 2 + 6,
            y + 4
        );

        canvas.DrawRoundRect(bgRect, 4, 4, bgPaint);
        canvas.DrawText(text, x, y, textPaint);
    }

    private void DrawToolUI(SKCanvas canvas, MapRenderContext context)
    {
        // Draw tool-specific UI hints
        switch (context.CurrentTool)
        {
            case MapTool.RevealFog:
            case MapTool.HideFog:
                DrawFogBrushPreview(canvas, context);
                break;
        }
    }

    private void DrawFogBrushPreview(SKCanvas canvas, MapRenderContext context)
    {
        if (!HoverCell.HasValue) return;

        var cellSize = context.EffectiveCellSize;
        var offset = context.ViewportOffset;

        // Draw 3x3 brush preview
        int brushSize = 3;
        int halfBrush = brushSize / 2;

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = context.CurrentTool == MapTool.RevealFog ? SKColors.Yellow : SKColors.DarkGray,
            StrokeWidth = 2,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
        };

        var rect = new SKRect(
            (HoverCell.Value.x - halfBrush) * cellSize + offset.X,
            (HoverCell.Value.y - halfBrush) * cellSize + offset.Y,
            (HoverCell.Value.x + halfBrush + 1) * cellSize + offset.X,
            (HoverCell.Value.y + halfBrush + 1) * cellSize + offset.Y
        );

        canvas.DrawRect(rect, paint);
    }

    /// <summary>
    /// Calculate distance in feet using D&D 5e diagonal rules.
    /// Every other diagonal counts as 10ft instead of 5ft.
    /// </summary>
    private int CalculateDistance(int startX, int startY, int endX, int endY)
    {
        int dx = Math.Abs(endX - startX);
        int dy = Math.Abs(endY - startY);

        int straight = Math.Abs(dx - dy);
        int diagonal = Math.Min(dx, dy);

        // D&D 5e: alternating 5/10 for diagonals
        int diagonalCost = (diagonal / 2) * 15 + (diagonal % 2) * 5;

        return (straight * 5) + diagonalCost;
    }

    public void Invalidate() => _needsRedraw = true;

    public void Dispose()
    {
        // No persistent resources
    }
}
