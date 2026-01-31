using SkiaSharp;
using SirSquintsDndAssistant.Models.BattleMap;

namespace SirSquintsDndAssistant.Rendering.Layers;

/// <summary>
/// Renders tokens (creatures) on the battle map.
/// </summary>
public class TokenLayer : IMapLayer
{
#pragma warning disable CS0414 // Field is assigned but never read - reserved for future dirty tracking
    private bool _needsRedraw = true;
#pragma warning restore CS0414

    // Pooled paint objects to avoid allocations per frame
    private SKPaint? _fillPaint;
    private SKPaint? _strokePaint;
    private SKPaint? _textPaint;
    private SKPaint? _shadowPaint;
    private SKPaint? _hpBgPaint;
    private SKPaint? _hpFillPaint;
    private SKPaint? _hpBorderPaint;
    private SKPaint? _conditionPaint;
    private SKPaint? _conditionTextPaint;
    private SKPaint? _movementPaint;
    private SKPaint? _movementTextPaint;
    private SKPaint? _auraFillPaint;
    private SKPaint? _auraStrokePaint;
    private SKPathEffect? _auraPathEffect;

    public MapLayerType LayerType => MapLayerType.Tokens;
    public int ZIndex => 30;
    public bool IsVisible { get; set; } = true;

    /// <summary>Whether to show HP bars on tokens.</summary>
    public bool ShowHpBars { get; set; } = true;

    /// <summary>Whether to show token labels.</summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>Whether to show condition indicators.</summary>
    public bool ShowConditions { get; set; } = true;

    public void Render(SKCanvas canvas, MapRenderContext context)
    {
        if (!IsVisible || context.Tokens == null || context.Tokens.Count == 0) return;

        // Render tokens in order (selected token last so it's on top)
        var orderedTokens = context.Tokens
            .OrderBy(t => t == context.SelectedToken ? 1 : 0)
            .ToList();

        foreach (var token in orderedTokens)
        {
            if (!token.IsVisible && !context.IsDmView) continue;

            DrawToken(canvas, context, token, token == context.SelectedToken);
        }

        _needsRedraw = false;
    }

    private void DrawToken(SKCanvas canvas, MapRenderContext context, MapToken token, bool isSelected)
    {
        var cellSize = context.EffectiveCellSize;
        var offset = context.ViewportOffset;

        // Calculate token size based on creature size
        int tokenCells = GetCreatureSize(token.Size);
        float tokenSize = tokenCells * cellSize;

        // Token position (center of the token)
        float x = token.GridX * cellSize + offset.X + tokenSize / 2;
        float y = token.GridY * cellSize + offset.Y + tokenSize / 2;
        float radius = (tokenSize / 2) - 4;

        // Draw auras first (behind the token)
        DrawAuras(canvas, x, y, token, cellSize);

        // Draw token based on shape
        var tokenColor = SKColor.Parse(token.Color ?? (token.IsEnemy ? "#dc3545" : "#28a745"));

        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = tokenColor,
            IsAntialias = true
        };

        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = isSelected ? SKColors.Yellow : SKColors.White,
            StrokeWidth = isSelected ? 4 : 2,
            IsAntialias = true
        };

        // Apply transparency for hidden tokens (DM view only)
        if (!token.IsVisible)
        {
            fillPaint.Color = tokenColor.WithAlpha(128);
            strokePaint.Color = strokePaint.Color.WithAlpha(128);
        }

        if (token.Shape == TokenShape.Square)
        {
            var rect = new SKRect(x - radius, y - radius, x + radius, y + radius);
            canvas.DrawRoundRect(rect, 8, 8, fillPaint);
            canvas.DrawRoundRect(rect, 8, 8, strokePaint);
        }
        else // Circle is default
        {
            canvas.DrawCircle(x, y, radius, fillPaint);
            canvas.DrawCircle(x, y, radius, strokePaint);
        }

        // Draw token label
        if (ShowLabels && !string.IsNullOrEmpty(token.Label))
        {
            DrawLabel(canvas, x, y, token.Label, tokenSize);
        }

        // Draw HP bar
        if (ShowHpBars && token.MaxHP > 0)
        {
            DrawHpBar(canvas, x, y + radius + 4, radius * 2, token);
        }

        // Draw condition indicators
        if (ShowConditions && !string.IsNullOrEmpty(token.ConditionsJson) && token.ConditionsJson != "[]")
        {
            DrawConditionIndicator(canvas, x + radius - 8, y - radius + 8);
        }

        // Draw movement indicator
        if (token.MovementUsed > 0)
        {
            DrawMovementIndicator(canvas, x - radius + 8, y - radius + 8, token);
        }
    }

    private void DrawLabel(SKCanvas canvas, float x, float y, string label, float tokenSize)
    {
        // Only show first 2 characters as label on the token
        string displayLabel = label.Length > 2 ? label[..2].ToUpper() : label.ToUpper();

        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = Math.Max(12, tokenSize / 3),
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            FakeBoldText = true
        };

        // Add shadow for readability
        using var shadowPaint = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(150),
            TextSize = textPaint.TextSize,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            FakeBoldText = true
        };

        canvas.DrawText(displayLabel, x + 1, y + textPaint.TextSize / 3 + 1, shadowPaint);
        canvas.DrawText(displayLabel, x, y + textPaint.TextSize / 3, textPaint);
    }

    private void DrawHpBar(SKCanvas canvas, float x, float y, float width, MapToken token)
    {
        float barHeight = 6;
        float hpPercent = Math.Clamp((float)token.CurrentHP / token.MaxHP, 0, 1);

        // Background
        using var bgPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.DarkGray
        };

        var bgRect = new SKRect(x - width / 2, y, x + width / 2, y + barHeight);
        canvas.DrawRoundRect(bgRect, 2, 2, bgPaint);

        // HP fill
        var hpColor = hpPercent > 0.5f ? SKColors.Green :
                      hpPercent > 0.25f ? SKColors.Orange : SKColors.Red;

        using var hpPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = hpColor
        };

        var hpRect = new SKRect(x - width / 2, y, x - width / 2 + width * hpPercent, y + barHeight);
        canvas.DrawRoundRect(hpRect, 2, 2, hpPaint);

        // Border
        using var borderPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.White.WithAlpha(150),
            StrokeWidth = 1
        };
        canvas.DrawRoundRect(bgRect, 2, 2, borderPaint);
    }

    private void DrawConditionIndicator(SKCanvas canvas, float x, float y)
    {
        // Small circle indicator that conditions are present
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Purple,
            IsAntialias = true
        };

        canvas.DrawCircle(x, y, 6, paint);

        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 10,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };

        canvas.DrawText("!", x, y + 3, textPaint);
    }

    private void DrawMovementIndicator(SKCanvas canvas, float x, float y, MapToken token)
    {
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = token.MovementUsed >= token.MovementTotal ? SKColors.Red : SKColors.Yellow,
            IsAntialias = true
        };

        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 8,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };

        canvas.DrawCircle(x, y, 8, paint);
        canvas.DrawText($"{token.MovementTotal - token.MovementUsed}", x, y + 3, textPaint);
    }

    private void DrawAuras(SKCanvas canvas, float centerX, float centerY, MapToken token, float cellSize)
    {
        if (string.IsNullOrEmpty(token.AurasJson) || token.AurasJson == "[]") return;

        try
        {
            var auras = System.Text.Json.JsonSerializer.Deserialize<List<TokenAura>>(token.AurasJson);
            if (auras == null) return;

            foreach (var aura in auras)
            {
                float auraRadius = (aura.RadiusFeet / 5f) * cellSize; // 5ft per cell
                var auraColor = SKColor.Parse(aura.Color ?? "#ffff00");

                using var fillPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = auraColor.WithAlpha(40),
                    IsAntialias = true
                };

                using var strokePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = auraColor.WithAlpha(150),
                    StrokeWidth = 2,
                    IsAntialias = true,
                    PathEffect = SKPathEffect.CreateDash(new float[] { 10, 5 }, 0)
                };

                canvas.DrawCircle(centerX, centerY, auraRadius, fillPaint);
                canvas.DrawCircle(centerX, centerY, auraRadius, strokePaint);
            }
        }
        catch
        {
            // Ignore aura parsing errors
        }
    }

    private int GetCreatureSize(CreatureSize size)
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

    public void Invalidate() => _needsRedraw = true;

    private void EnsurePaints()
    {
        _fillPaint ??= new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
        _strokePaint ??= new SKPaint { Style = SKPaintStyle.Stroke, IsAntialias = true };
        _textPaint ??= new SKPaint { Color = SKColors.White, IsAntialias = true, TextAlign = SKTextAlign.Center, FakeBoldText = true };
        _shadowPaint ??= new SKPaint { Color = SKColors.Black.WithAlpha(150), IsAntialias = true, TextAlign = SKTextAlign.Center, FakeBoldText = true };
        _hpBgPaint ??= new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.DarkGray };
        _hpFillPaint ??= new SKPaint { Style = SKPaintStyle.Fill };
        _hpBorderPaint ??= new SKPaint { Style = SKPaintStyle.Stroke, Color = SKColors.White.WithAlpha(150), StrokeWidth = 1 };
        _conditionPaint ??= new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.Purple, IsAntialias = true };
        _conditionTextPaint ??= new SKPaint { Color = SKColors.White, TextSize = 10, IsAntialias = true, TextAlign = SKTextAlign.Center };
        _movementPaint ??= new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
        _movementTextPaint ??= new SKPaint { Color = SKColors.Black, TextSize = 8, IsAntialias = true, TextAlign = SKTextAlign.Center };
        _auraFillPaint ??= new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
        _auraStrokePaint ??= new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
        _auraPathEffect ??= SKPathEffect.CreateDash(new float[] { 10, 5 }, 0);
    }

    public void Dispose()
    {
        _fillPaint?.Dispose();
        _strokePaint?.Dispose();
        _textPaint?.Dispose();
        _shadowPaint?.Dispose();
        _hpBgPaint?.Dispose();
        _hpFillPaint?.Dispose();
        _hpBorderPaint?.Dispose();
        _conditionPaint?.Dispose();
        _conditionTextPaint?.Dispose();
        _movementPaint?.Dispose();
        _movementTextPaint?.Dispose();
        _auraFillPaint?.Dispose();
        _auraStrokePaint?.Dispose();
        _auraPathEffect?.Dispose();
    }
}

/// <summary>
/// Represents an aura effect around a token.
/// </summary>
public class TokenAura
{
    public int RadiusFeet { get; set; } = 10;
    public string Color { get; set; } = "#ffff00";
    public string? Name { get; set; }
}
