using Android.Graphics;
using Android.Graphics.Pdf;
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
        bool darkMode = false)
    {
        var total    = entries.Sum(e => e.TotalWeightTonnes);
        var len      = entries.Sum(e => e.LengthM);
        var ab       = entries.Where(e => e.BrakesEnabled).Sum(e => BrakingCalculator.ActiveBrake(e));
        var pct      = total > 0 ? Math.Floor(ab / total * 100.0) : 0;
        var fp       = BrakingCalculator.ConsistFpClass(entries);
        var fpMm     = fp == "FP3" ? 130 : 100;
        var date     = DateTime.Now.ToString(isCs ? "dd. MM. yyyy HH:mm" : "dd MMM yyyy HH:mm");
        var lowBrake = pct < 50;

        string T(string cs, string en) => isCs ? cs : en;

        // ── Paints ──────────────────────────────────────────────────────────
        Paint TextPaint(float size, Color color, bool bold = false)
        {
            var p = new Paint { AntiAlias = true };
            p.Color = color;
            p.TextSize = size;
            if (bold) p.FakeBoldText = true;
            return p;
        }
        Paint FillPaint(Color color)
        {
            var p = new Paint { AntiAlias = true };
            p.Color = color;
            p.SetStyle(Paint.Style.Fill);
            return p;
        }

        var orange     = Color.ParseColor("#f97316");
        var dark       = Color.ParseColor("#1a1a2e");
        var grey       = Color.ParseColor(darkMode ? "#8888aa" : "#666666");
        var lightGrey  = Color.ParseColor(darkMode ? "#8888aa" : "#888888");
        var white      = Color.White;
        var black      = Color.ParseColor("#111111");
        var green      = Color.ParseColor("#27ae60");
        var red        = Color.ParseColor("#c0392b");
        var bgPage     = Color.ParseColor(darkMode ? "#0f0f1a" : "#ffffff");
        var bgCard     = Color.ParseColor(darkMode ? "#1e1e32" : "#f8f8f8");
        var bgEtcs     = Color.ParseColor(darkMode ? "#161626" : "#1a1a2e");
        var bgRow      = Color.ParseColor("#f9f9f9");
        var txtMain    = Color.ParseColor(darkMode ? "#e8e8ff" : "#1a1a2e");
        var warnBg     = Color.ParseColor(darkMode ? "#3f0f0f" : "#fdecea");
        var warnText   = Color.ParseColor("#922b21");

        var doc = new PdfDocument();
        var pageInfo = new PdfDocument.PageInfo.Builder(PageW, PageH, 1).Create();
        var page = doc.StartPage(pageInfo);
        var cv = page.Canvas;

        // Page background
        cv.DrawRect(new RectF(0, 0, PageW, PageH), FillPaint(bgPage));

        float y = Margin;

        // ── Header ───────────────────────────────────────────────────────────
        cv.DrawText("LocoCalc", Margin, y + 20, TextPaint(22, orange, true));
        cv.DrawText(consistName, Margin, y + 40, TextPaint(14, txtMain, true));
        cv.DrawText(T("Zpráva o brzdění", "Braking Report"), Margin, y + 55, TextPaint(10, grey));
        cv.DrawText(date, PageW - Margin - 100, y + 40, TextPaint(10, lightGrey));
        y += 62;
        // Orange line
        cv.DrawLine(Margin, y, PageW - Margin, y, FillPaint(orange));
        y += 12;

        // ── Warning ───────────────────────────────────────────────────────────
        if (lowBrake)
        {
            var rect = new RectF(Margin, y, PageW - Margin, y + 44);
            cv.DrawRect(rect, FillPaint(warnBg));
            cv.DrawText(T("⚠ VAROVÁNÍ — nedostatečná brzdící procenta",
                          "⚠ WARNING — insufficient braking percent"),
                Margin + 6, y + 16, TextPaint(9, warnText, true));
            cv.DrawText(T($"Brzdící procenta {pct:F0}% jsou pod 50%. Vlak nesmí jet na úseky jen s ETCS.",
                          $"Braking {pct:F0}% below 50%. Train must not use ETCS-only tracks."),
                Margin + 6, y + 32, TextPaint(8, warnText));
            y += 50;
        }

        // ── Summary cards ────────────────────────────────────────────────────
        float cardW = (ContentW - 9) / 2f;
        void DrawCard(float cx, float cy, string label, string value, Color valColor)
        {
            var r = new RectF(cx, cy, cx + cardW, cy + 48);
            cv.DrawRect(r, FillPaint(bgCard));
            cv.DrawRect(new RectF(cx, cy, cx + 3, cy + 48), FillPaint(orange));
            cv.DrawText(label, cx + 8, cy + 14, TextPaint(7.5f, lightGrey));
            cv.DrawText(value, cx + 8, cy + 38, TextPaint(18, valColor, true));
        }

        var pctColor = lowBrake ? red : pct < 65 ? orange : green;
        DrawCard(Margin, y, T("BRZDÍCÍ %", "BRAKING %"), $"{pct:F0} %", pctColor);
        DrawCard(Margin + cardW + 9, y, T("MAX. RYCHLOST", "MAX SPEED"), $"{maxSpeed} km/h", orange);
        y += 54;
        DrawCard(Margin, y, T("DÉLKA SOUPRAVY", "CONSIST LENGTH"), $"{len:F0} m", txtMain);
        DrawCard(Margin + cardW + 9, y, T("CELK. HMOTNOST", "TOTAL WEIGHT"), $"{total:F1} t", txtMain);
        y += 54;

        // ── ETCS box ─────────────────────────────────────────────────────────
        cv.DrawRect(new RectF(Margin, y, PageW - Margin, y + 90), FillPaint(bgEtcs));
        cv.DrawText(T("ETCS PARAMETRY", "ETCS PARAMETERS"), Margin + 8, y + 14,
            TextPaint(8, Color.ParseColor("#aaaaaa"), true));
        var etcsRows = new[]
        {
            (T("Průjezdný průřez", "Cross-section"), "GC", orange),
            (T("Nedostatek převýšení", "Cant deficiency"), $"{fp} ({fpMm} mm)", white),
            (T("Max. rychlost ETCS", "ETCS max speed"), $"{maxSpeed} km/h", orange),
            (T("Brzdící procenta", "Braking pct"), $"{pct:F0} %", lowBrake ? red : green),
            (T("Délka vlaku", "Train length"), $"{len:F0} m", white),
        };
        float ey = y + 22;
        foreach (var (lbl, val, col) in etcsRows)
        {
            cv.DrawText(lbl, Margin + 8, ey, TextPaint(8, Color.ParseColor("#aaaaaa")));
            cv.DrawText(val, Margin + ContentW / 2f, ey, TextPaint(8, col, true));
            ey += 13;
        }
        y += 96;

        // ── Consist table ─────────────────────────────────────────────────────
        cv.DrawText(T("SLOŽENÍ SOUPRAVY", "CONSIST COMPOSITION"),
            Margin, y + 10, TextPaint(8, grey, true));
        y += 16;

        // Column widths
        float[] cols = { 120, 70, 70, 60, 50 };
        string[] hdrs = {
            T("Řada","Series"), T("Hmotnost","Weight"),
            T("Brzd. váha","Brake wt"), T("Délka","Length"),
            T("Brzdy","Brakes")
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
            var bw      = e.BrakesEnabled ? BrakingCalculator.ActiveBrake(e) : 0;
            var edbNote = e.BrakesEnabled && e.EdbActive ? " (EDB)" : "";
            var bwColor = e.BrakesEnabled ? black : red;
            var brakesTxt = isCs
                ? (e.BrakesEnabled ? (e.Position == ConsistPosition.Rear ? "ZAP (zámek)" : "ZAP") : "VYP")
                : (e.BrakesEnabled ? (e.Position == ConsistPosition.Rear ? "ON (locked)" : "ON") : "OFF");

            cv.DrawRect(new RectF(Margin, y, PageW - Margin, y + 16), FillPaint(bg));
            var rowData = new (string txt, Color col)[]
            {
                (e.Designation, black),
                ($"{e.TotalWeightTonnes:F1} t", black),
                ($"{bw:F0} t{edbNote}", bwColor),
                ($"{e.LengthM:F0} m", black),
                (brakesTxt, e.BrakesEnabled ? green : red),
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
            T("Celkem", "Total"), $"{total:F1} t",
            $"{ab:F0} t", $"{len:F0} m", "—"
        };
        cv.DrawRect(new RectF(Margin, y, PageW - Margin, y + 16),
            FillPaint(Color.ParseColor("#f0f0f0")));
        cx2 = Margin;
        foreach (var (txt, w) in totData.Zip(cols))
        {
            cv.DrawText(txt, cx2 + 4, y + 12, TextPaint(7.5f, black, true));
            cx2 += w;
        }
        y += 22;

        // FP legend
        cv.DrawText("FP3 (130mm): 163,186,189,363,363.5,372,383,386,388,393  |  FP2: ostatní",
            Margin, y + 10, TextPaint(7, lightGrey));

        // Footer
        cv.DrawLine(Margin, PageH - 30, PageW - Margin, PageH - 30, FillPaint(Color.ParseColor("#dddddd")));
        cv.DrawText($"LocoCalc  ·  {date}", Margin, PageH - 18, TextPaint(8, lightGrey));

        doc.FinishPage(page);

        using var ms = new MemoryStream();
        doc.WriteTo(ms);
        doc.Close();
        return ms.ToArray();
    }
}
