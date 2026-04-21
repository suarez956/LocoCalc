using SkiaSharp;

namespace LocoCalc.Services;

/// <summary>
/// Stateless drawing helpers for SkiaSharp-based PDF generation.
/// All methods take an explicit <paramref name="canvas"/> so they are
/// side-effect-free and easy to unit-test without a live document.
/// </summary>
public static class CustomSkiaPDFHelpers
{
    // ── Color ────────────────────────────────────────────────────────────────

    /// <summary>Parse a CSS hex colour string (e.g. "#f97316") into an SKColor.</summary>
    public static SKColor Color(string hex) => SKColor.Parse(hex);

    // ── Paint factory ────────────────────────────────────────────────────────

    /// <summary>
    /// Create a text/fill paint.  Caller is responsible for disposing the returned object.
    /// </summary>
    /// <param name="fontSize">Text size in points.</param>
    /// <param name="bold">Apply synthetic bold (FakeBoldText).</param>
    /// <param name="textColor">Foreground colour; defaults to black.</param>
    public static SKPaint CreatePaint(float fontSize, bool bold = false, SKColor? textColor = null)
        => new SKPaint
        {
            Color        = textColor ?? SKColors.Black,
            TextSize     = fontSize,
            IsAntialias  = true,
            FakeBoldText = bold
        };

    // ── Primitives ───────────────────────────────────────────────────────────

    /// <summary>Stroke a rectangle outline on <paramref name="canvas"/>.</summary>
    public static void StrokeRect(
        SKCanvas canvas, float left, float top, float width, float height,
        float strokeWidth = 0.8f)
    {
        using var paint = new SKPaint
        {
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            Color       = SKColors.Black,
            IsAntialias = true
        };
        canvas.DrawRect(SKRect.Create(left, top, width, height), paint);
    }

    /// <summary>Fill a rectangle on <paramref name="canvas"/> with a solid colour.</summary>
    public static void FillRect(
        SKCanvas canvas, float left, float top, float width, float height,
        SKColor fillColor)
    {
        using var paint = new SKPaint { Style = SKPaintStyle.Fill, Color = fillColor };
        canvas.DrawRect(SKRect.Create(left, top, width, height), paint);
    }

    /// <summary>Draw a text string; <paramref name="baseline"/> is the Y of the text baseline.</summary>
    public static void DrawText(
        SKCanvas canvas, string text, float x, float baseline, SKPaint paint)
        => canvas.DrawText(text, x, baseline, paint);

    /// <summary>Draw a straight line segment on <paramref name="canvas"/>.</summary>
    public static void DrawLine(
        SKCanvas canvas, float x0, float y0, float x1, float y1,
        float strokeWidth = 0.8f)
    {
        using var paint = new SKPaint
        {
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            Color       = SKColors.Black,
            IsAntialias = true
        };
        canvas.DrawLine(x0, y0, x1, y1, paint);
    }

    // ── Selective-border rectangle ────────────────────────────────────────────

    /// <summary>
    /// Draw only the requested sides of a rectangle.
    /// Use the <see cref="RectSides"/> flags to specify which edges to render.
    /// </summary>
    public static void StrokeRectSides(
        SKCanvas canvas, float left, float top, float width, float height,
        RectSides sides, float strokeWidth = 0.8f)
    {
        if (sides == RectSides.None) return;
        float right  = left + width;
        float bottom = top  + height;
        if ((sides & RectSides.Top)    != 0) DrawLine(canvas, left,  top,    right, top,    strokeWidth);
        if ((sides & RectSides.Right)  != 0) DrawLine(canvas, right, top,    right, bottom, strokeWidth);
        if ((sides & RectSides.Bottom) != 0) DrawLine(canvas, left,  bottom, right, bottom, strokeWidth);
        if ((sides & RectSides.Left)   != 0) DrawLine(canvas, left,  top,    left,  bottom, strokeWidth);
    }
}

/// <summary>Flags for selecting individual sides of a rectangle border.</summary>
[Flags]
public enum RectSides
{
    None   = 0,
    Top    = 1,
    Right  = 2,
    Bottom = 4,
    Left   = 8,
    All    = Top | Right | Bottom | Left
}
