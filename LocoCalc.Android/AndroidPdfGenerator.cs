using Android.Graphics;
using Android.Graphics.Pdf;
using Avalonia.Platform;
using LocoCalcAvalonia.Models;
using LocoCalcAvalonia.Services;
using Paint = Android.Graphics.Paint;

namespace LocoCalcAvalonia.Android;

/// <summary>
/// Generates PDF using the built-in Android.Graphics.Pdf API.
/// No QuestPDF or SkiaSharp required.
/// </summary>
public class AndroidPdfGenerator : IPdfGenerator
{
    // A4 at 72 DPI
    private const int PageW = 595;
    private const int PageH = 842;
    private const int Margin = 40;
    private const int ContentW = PageW - Margin * 2;

    public byte[] Generate(
        IReadOnlyList<ConsistEntry> entries,
        string consistName,
        int maxSpeed,
        bool isCs,
        bool darkMode = false,
        string? startStation = null,
        string? endStation = null)
    {
        // ── FA font ──────────────────────────────────────────────────────────
        Typeface? faTypeface = null;
        try
        {
            using var faStream = AssetLoader.Open(new Uri("avares://LocoCalc.Core/Assets/fa-solid-900.ttf"));
            var faBytes = new byte[faStream.Length];
            _ = faStream.Read(faBytes, 0, faBytes.Length);
            var tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "lc_fa_solid.ttf");
            System.IO.File.WriteAllBytes(tmp, faBytes);
            faTypeface = Typeface.CreateFromFile(tmp);
        }
        catch { /* icon chars fall back to system font if FA unavailable */ }

        var total    = entries.Sum(e => e.TotalWeightTonnes);
        var len      = entries.Sum(e => e.LengthM);
        var ab       = entries.Where(e => e.BrakesEnabled).Sum(e => BrakingCalculator.ActiveBrake(e));
        var abBase   = entries.Where(e => e.BrakesEnabled).Sum(e =>
            e.RModeActive && e.BrakingWeightTonnesR.HasValue ? e.BrakingWeightTonnesR!.Value : e.BrakingWeightTonnes);
        var pct      = total > 0 ? Math.Floor(ab / total * 100.0) : 0;
        var fp       = BrakingCalculator.ConsistFpClass(entries);
        var fpMm     = fp == "FP3" ? 130 : 100;
        var axleLoad = BrakingCalculator.ConsistAxleLoad(entries) ?? "—";
        var date     = DateTime.Now.ToString(isCs ? "dd. MM. yyyy HH:mm" : "dd MMM yyyy HH:mm");
        var lowBrake = pct < 50;

        string T(string key) => LocalizationService.GetString(key, isCs);

        // ── Paints ──────────────────────────────────────────────────────────
        Paint TextPaint(float size, Color color, bool bold = false)
        {
            var p = new Paint { AntiAlias = true };
            p.Color = color;
            p.TextSize = size;
            if (bold) p.FakeBoldText = true;
            return p;
        }
        Paint FaPaint(float size, Color color)
        {
            var p = new Paint { AntiAlias = true };
            p.Color = color;
            p.TextSize = size;
            p.SetTypeface(faTypeface ?? Typeface.Default);
            return p;
        }
        Paint FillPaint(Color color)
        {
            var p = new Paint { AntiAlias = true };
            p.Color = color;
            p.SetStyle(Paint.Style.Fill);
            return p;
        }

        // ── Theme colours ────────────────────────────────────────────────────
        var th      = PdfThemeService.Get(darkMode);
        var orange  = Color.ParseColor(th.Orange);
        var dark    = Color.ParseColor(th.BgTableHdr);  // always-dark table header
        var grey    = Color.ParseColor(th.TxtSub);
        var white   = Color.White;
        var black   = Color.ParseColor(th.TxtCell);
        var green   = Color.ParseColor(th.Green);
        var red     = Color.ParseColor(th.Red);
        var bgPage  = Color.ParseColor(th.BgPage);
        var bgCard  = Color.ParseColor(th.BgCard);
        var bgEtcs  = Color.ParseColor(th.BgEtcs);
        var bgRow   = Color.ParseColor(th.BgRowOdd);
        var txtMain = Color.ParseColor(th.TxtMain);
        var txtEtcs = Color.ParseColor(th.TxtEtcs);
        var warnBg  = Color.ParseColor(th.BgWarn);
        var warnTxt = Color.ParseColor(th.TxtWarn);
        var footer  = Color.ParseColor(th.FooterText);
        var ftrLine = Color.ParseColor(th.FooterLine);

        var doc = new PdfDocument();
        var pageInfo = new PdfDocument.PageInfo.Builder(PageW, PageH, 1).Create();
        var page = doc.StartPage(pageInfo) ?? throw new InvalidOperationException("Failed to start PDF page.");
        var cv = page.Canvas ?? throw new InvalidOperationException("PDF page canvas is null.");

        // Page background
        cv.DrawRect(new RectF(0, 0, PageW, PageH), FillPaint(bgPage));

        float y = Margin;

        // ── Header ───────────────────────────────────────────────────────────
        cv.DrawText("LocoCalc", Margin, y + 20, TextPaint(22, orange, true));
        cv.DrawText(consistName, Margin, y + 40, TextPaint(14, txtMain, true));
        cv.DrawText(T("PdfBrakingReport"), Margin, y + 55, TextPaint(10, grey));
        if (startStation is not null || endStation is not null)
        {
            var route = $"{T("PdfRoute")}: {startStation ?? "?"} → {endStation ?? "?"}";
            cv.DrawText(route, Margin, y + 68, TextPaint(10, footer));
            y += 13;
        }
        cv.DrawText(date, PageW - Margin - 100, y + 40, TextPaint(10, footer));
        y += 62;
        cv.DrawLine(Margin, y, PageW - Margin, y, FillPaint(orange));
        y += 12;

        // ── Warning ───────────────────────────────────────────────────────────
        if (lowBrake)
        {
            var rect = new RectF(Margin, y, PageW - Margin, y + 54);
            cv.DrawRect(rect, FillPaint(warnBg));
            // Title row: FA icon + text
            cv.DrawText("\uF071", Margin + 6, y + 14, FaPaint(9, warnTxt));
            cv.DrawText(T("PdfWarnTitle"), Margin + 20, y + 14, TextPaint(9, warnTxt, true));
            // Body row
            cv.DrawText(string.Format(T("PdfWarnBody"), pct), Margin + 6, y + 30, TextPaint(8, warnTxt));
            // Sub row: FA icon + text
            cv.DrawText("\uF05E", Margin + 6, y + 46, FaPaint(8, warnTxt));
            cv.DrawText(LocalizationService.GetString("WarnLowBrakeSub", isCs), Margin + 20, y + 46, TextPaint(8, warnTxt));
            y += 60;
        }

        // ── Summary cards ────────────────────────────────────────────────────
        float cardW = (ContentW - 9) / 2f;
        void DrawCard(float cx, float cy, string label, string value, Color valColor)
        {
            var r = new RectF(cx, cy, cx + cardW, cy + 48);
            cv.DrawRect(r, FillPaint(bgCard));
            cv.DrawRect(new RectF(cx, cy, cx + 3, cy + 48), FillPaint(orange));
            cv.DrawText(label, cx + 8, cy + 14, TextPaint(7.5f, footer));
            cv.DrawText(value, cx + 8, cy + 38, TextPaint(18, valColor, true));
        }

        var pctColor = lowBrake ? red : pct < 65 ? orange : green;
        DrawCard(Margin,           y, T("PdfBrakingPctLabel"), $"{pct:F0} %",      pctColor);
        DrawCard(Margin + cardW + 9, y, T("PdfMaxSpeedLabel"), $"{maxSpeed} km/h", orange);
        y += 54;
        DrawCard(Margin,           y, T("PdfLengthLabel"), $"{len:F0} m",    txtMain);
        DrawCard(Margin + cardW + 9, y, T("PdfWeightLabel"), $"{total:F1} t", txtMain);
        y += 54;

        // ── ETCS box ─────────────────────────────────────────────────────────
        const float EtcsRowH = 17f;
        const float EtcsBoxH = 20f + 6 * EtcsRowH + 8f;   // header + rows + bottom pad
        cv.DrawRect(new RectF(Margin, y, PageW - Margin, y + EtcsBoxH), FillPaint(bgEtcs));
        cv.DrawText(T("PdfEtcsParams"), Margin + 8, y + 14, TextPaint(8, txtEtcs, true));
        var etcsRows = new[]
        {
            (T("CrossSection"),   T("PdfCrossSectionValue"),         orange),
            (T("CantDeficiency"), $"{fp} ({fpMm} mm)",               white),
            (T("EtcsMaxSpeed"),   $"{maxSpeed} km/h",                orange),
            (T("BrakingPctLabel"),$"{pct:F0} %",                     lowBrake ? red : green),
            (T("TrainLength"),    $"{len:F0} m",                     white),
            (T("AxleLoad"),       axleLoad,                          orange),
        };
        float ey = y + 20f + EtcsRowH * 0.75f;   // baseline of first row
        foreach (var (lbl, val, col) in etcsRows)
        {
            cv.DrawText(lbl, Margin + 8, ey, TextPaint(8, txtEtcs));
            cv.DrawText(val, Margin + ContentW / 2f, ey, TextPaint(8, col, true));
            ey += EtcsRowH;
        }
        y += EtcsBoxH + 6f;

        // ── Consist table ─────────────────────────────────────────────────────
        cv.DrawText(T("PdfConsistComp"), Margin, y + 10, TextPaint(8, grey, true));
        y += 16;

        float[] cols = { 105, 58, 58, 65, 52, 42 };
        string[] hdrs = {
            T("PdfColSeries"), T("PdfColWeight"),
            T("PdfColBrakeWt"), T("PdfColBrakeWtEDB"), T("PdfColLength"), T("PdfColBrakes")
        };

        // Header row
        float cx2 = Margin;
        cv.DrawRect(new RectF(Margin, y, PageW - Margin, y + 18), FillPaint(dark));
        for (int i = 0; i < hdrs.Length; i++)
        {
            cv.DrawText(hdrs[i], cx2 + 4, y + 13, TextPaint(7.5f, white, true));
            cx2 += cols[i];
        }
        y += 18;

        // Data rows
        for (int i = 0; i < entries.Count; i++)
        {
            var e       = entries[i];
            var bg      = i % 2 == 0 ? white : bgRow;
            var bwBase  = e.BrakesEnabled
                ? (e.RModeActive && e.BrakingWeightTonnesR.HasValue ? e.BrakingWeightTonnesR!.Value : e.BrakingWeightTonnes)
                : 0;
            var bwEdbStr = !e.BrakesEnabled ? "—"
                : e.RModeActive && e.BrakingWeightWithEDBR.HasValue ? $"{e.BrakingWeightWithEDBR!.Value:F0} t"
                : !e.RModeActive && e.BrakingWeightWithEDB.HasValue ? $"{e.BrakingWeightWithEDB!.Value:F0} t"
                : "—";
            var bwColor = e.BrakesEnabled ? black : red;
            var brakesTxt = e.BrakesEnabled ? (e.EdbActive ? (e.RModeActive ? "R+E" : "P+E") : (e.RModeActive ? "R" : "P")) : "x";

            cv.DrawRect(new RectF(Margin, y, PageW - Margin, y + 16), FillPaint(bg));
            var rowData = new (string txt, Color col)[]
            {
                (e.CustomName ?? e.Designation,  black),
                ($"{e.TotalWeightTonnes:F1} t",  black),
                ($"{bwBase:F0} t",               bwColor),
                (bwEdbStr,                       bwEdbStr == "—" ? footer : black),
                ($"{e.LengthM:F0} m",            black),
                (brakesTxt,                      e.BrakesEnabled ? green : red),
            };
            cx2 = Margin;
            foreach (var ((txt, col), w) in rowData.Zip(cols))
            {
                cv.DrawText(txt, cx2 + 4, y + 12, TextPaint(7.5f, col));
                cx2 += w;
            }
            y += 16;
        }

        // Totals row
        var totData = new[]
        {
            T("PdfTotal"), $"{total:F1} t", $"{abBase:F0} t", $"{ab:F0} t", $"{len:F0} m", "—"
        };
        cv.DrawRect(new RectF(Margin, y, PageW - Margin, y + 16),
            FillPaint(Color.ParseColor(th.BgTotals)));
        cx2 = Margin;
        foreach (var (txt, w) in totData.Zip(cols))
        {
            cv.DrawText(txt, cx2 + 4, y + 12, TextPaint(7.5f, black, true));
            cx2 += w;
        }
        y += 22;

        // FP legend
        cv.DrawText(T("PdfFpLegend"), Margin, y + 10, TextPaint(7, footer));

        // Footer
        cv.DrawLine(Margin, PageH - 30, PageW - Margin, PageH - 30, FillPaint(ftrLine));
        cv.DrawText($"LocoCalc  ·  {date}", Margin, PageH - 18, TextPaint(8, footer));

        doc.FinishPage(page);

        using var ms = new MemoryStream();
        doc.WriteTo(ms);
        doc.Close();
        return ms.ToArray();
    }
}
