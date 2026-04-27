using SkiaSharp;

namespace LocoCalc.Services;

public static class CustomSkiaPDFHelpers
{
    public static SKColor Color(string hex) => SKColor.Parse(hex);

    /// <summary>Create a text style. Caller is responsible for disposing the returned object.</summary>
    public static TextPaint CreateTextStyle(float fontSize, bool bold = false, SKColor? textColor = null)
    {
        var font = new SKFont(SKTypeface.Default, fontSize);
        if (bold) font.Embolden = true;
        var paint = new SKPaint { Color = textColor ?? SKColors.Black, IsAntialias = true };
        return new TextPaint(font, paint);
    }

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

    public static void FillRect(
        SKCanvas canvas, float left, float top, float width, float height,
        SKColor fillColor)
    {
        using var paint = new SKPaint { Style = SKPaintStyle.Fill, Color = fillColor };
        canvas.DrawRect(SKRect.Create(left, top, width, height), paint);
    }

    public static void DrawText(
        SKCanvas canvas, string text, float x, float baseline, TextPaint tp)
        => canvas.DrawText(text, x, baseline, SKTextAlign.Left, tp.Font, tp.Paint);

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

/// <summary>Holds an SKFont + SKPaint pair for SkiaSharp 3.x text drawing.</summary>
public sealed class TextPaint : IDisposable
{
    public readonly SKFont  Font;
    public readonly SKPaint Paint;

    public TextPaint(SKFont font, SKPaint paint) { Font = font; Paint = paint; }
    public float MeasureText(string text) => Font.MeasureText(text);
    public void  Dispose() { Font.Dispose(); Paint.Dispose(); }
}

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
