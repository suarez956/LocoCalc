using SkiaSharp;
using LocoCalcAvalonia.Models;
using LocoCalcAvalonia.Services;
using RS = LocoCalcAvalonia.Services.RectSides;

namespace LocoCalcAvalonia.Android;

/// <summary>
/// Generates the MZOB (Mezinárodní zpráva o brzdění) PDF for Android
/// using SkiaSharp PDF generation (SKDocument.CreatePdf).
/// Mirrors the data logic and two-page layout of ZoBPdfService (Desktop).
/// </summary>
public class AndroidZoBGenerator : IZoBGenerator
{
    private const float PageW  = 595f;
    private const float PageH  = 842f;
    private const float Margin = 28f;
    private const float ContentW = PageW - Margin * 2;

    private static SKColor C(string hex) => CustomSkiaPDFHelpers.Color(hex);

    public byte[] Generate(
        IReadOnlyList<ConsistEntry> entries,
        string consistName,
        int maxSpeed,
        string? requiredBrakingPct,
        string? startStation,
        string? endStation)
    {
        // ── Aggregate active vs transported ────────────────────────────────────
        var active      = entries.Where(e => !e.IsTransported).ToList();
        var transported = entries.Where(e =>  e.IsTransported).ToList();

        int    actCount    = active.Count;
        int    tranCount   = transported.Count;
        double actWeightT  = active.Sum(e => e.TotalWeightTonnes);
        double tranWeightT = transported.Sum(e => e.TotalWeightTonnes);
        double actBrakP    = active.Sum(e => e.BrakingWeightTonnes);
        double tranBrakP   = transported.Sum(e => e.BrakingWeightTonnes);
        double? actBrakEDB = active.Any(e => e.HasEDB)
                               ? active.Sum(e => e.BrakingWeightWithEDB ?? e.BrakingWeightTonnes)
                               : null;
        double? tranBrakEDB = transported.Any(e => e.HasEDB)
                               ? transported.Sum(e => e.BrakingWeightWithEDB ?? e.BrakingWeightTonnes)
                               : null;
        double actLenM    = active.Sum(e => e.LengthM);
        double tranLenM   = transported.Sum(e => e.LengthM);
        int    actAxles   = active.Sum(e => e.AxleCount);
        int    tranAxles  = transported.Sum(e => e.AxleCount);
        double? actSecKn  = active.Any(e => e.SecuringForceKn.HasValue)
                              ? active.Sum(e => e.SecuringForceKn ?? 0) : null;
        double? tranSecKn = transported.Any(e => e.SecuringForceKn.HasValue)
                              ? transported.Sum(e => e.SecuringForceKn ?? 0) : null;

        // ── Totals for braking % ───────────────────────────────────────────────
        double totW        = actWeightT + tranWeightT;
        double actualEDB   = totW > 0 ? Math.Floor(((actBrakEDB ?? actBrakP) + tranBrakP) / totW * 100) : 0;
        double actualNoEDB = totW > 0 ? Math.Floor((actBrakP + tranBrakP) / totW * 100) : 0;
        string actualStr   = actBrakEDB.HasValue
                                 ? $"{actualEDB:F0} / {actualNoEDB:F0}"
                                 : $"{actualNoEDB:F0}";

        double? reqPct = null;
        if (!string.IsNullOrWhiteSpace(requiredBrakingPct) &&
            double.TryParse(requiredBrakingPct!.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var rp))
            reqPct = rp;

        string missingStr = string.Empty;
        if (reqPct.HasValue)
        {
            if (actBrakEDB.HasValue)
            {
                double diffEDB   = actualEDB   - reqPct.Value;
                double diffNoEDB = actualNoEDB - reqPct.Value;
                if (diffEDB < 0 || diffNoEDB < 0)
                    missingStr = $"{Math.Max(0, -diffEDB):F0} / {Math.Max(0, -diffNoEDB):F0}";
            }
            else
            {
                double diff = actualNoEDB - reqPct.Value;
                if (diff < 0) missingStr = $"{-diff:F0}";
            }
        }

        int pCount        = entries.Count(e => !e.RModeActive && e.BrakesEnabled);
        int rCount        = entries.Count(e => e.RModeActive && e.BrakesEnabled);
        int disabledCount = entries.Count(e => !e.BrakesEnabled);
        int securingCount = entries.Count(e => e.SecuringForceKn is > 0);
        bool anyR         = entries.Any(e => e.RModeActive && !e.IsTransported);
        string bMode      = anyR ? "R" : "P";
        var    lead       = entries.FirstOrDefault(e => !e.IsTransported) ?? entries.FirstOrDefault();
        var    last       = entries.LastOrDefault();
        string leadNum    = lead?.CustomName ?? string.Empty;
        string lastNum    = last?.CustomName ?? string.Empty;
        var    now        = DateTime.Now;

        static string Bv(double? withEDB, double withoutEDB)
            => withEDB.HasValue && Math.Abs(withEDB.Value - withoutEDB) > 0.5
                ? $"{withEDB:F0}/{withoutEDB:F0}"
                : $"{withoutEDB:F0}";

        static string Len(double lenM, int axles)
            => axles > 0 ? $"{lenM:F0}/{axles}" : string.Empty;

        // ── Row 6–10 aggregate values ──────────────────────────────────────────
        string[][] rowData = new string[5][];
        rowData[0] = new[]
        {
            actCount  > 0 ? actCount.ToString()  : string.Empty,
            tranCount > 0 ? tranCount.ToString()  : string.Empty,
            "0",
            tranCount > 0 ? tranCount.ToString()  : "0",
            (actCount + tranCount).ToString()
        };
        rowData[1] = new[]
        {
            actWeightT  > 0 ? $"{actWeightT:F0}"  : string.Empty,
            tranWeightT > 0 ? $"{tranWeightT:F0}" : string.Empty,
            "0",
            tranWeightT > 0 ? $"{tranWeightT:F0}" : "0",
            $"{actWeightT + tranWeightT:F0}"
        };
        rowData[2] = new[]
        {
            actCount  > 0 ? Bv(actBrakEDB,  actBrakP)  : string.Empty,
            tranCount > 0 ? Bv(tranBrakEDB, tranBrakP) : string.Empty,
            "/",
            tranCount > 0 ? Bv(tranBrakEDB, tranBrakP) : "/",
            Bv(actBrakEDB.HasValue ? actBrakEDB + tranBrakP : null, actBrakP + tranBrakP)
        };
        rowData[3] = new[]
        {
            actCount  > 0 ? Len(actLenM,  actAxles)  : string.Empty,
            tranCount > 0 ? Len(tranLenM, tranAxles) : string.Empty,
            "/",
            tranCount > 0 ? Len(tranLenM, tranAxles) : "/",
            Len(actLenM + tranLenM, actAxles + tranAxles)
        };
        double totSecKn = (actSecKn ?? 0) + (tranSecKn ?? 0);
        rowData[4] = new[]
        {
            actCount  > 0 && actSecKn.HasValue  ? $"{actSecKn:F0}"  : string.Empty,
            tranCount > 0 && tranSecKn.HasValue ? $"{tranSecKn:F0}" : string.Empty,
            string.Empty,
            tranCount > 0 && tranSecKn.HasValue ? $"{tranSecKn:F0}" : string.Empty,
            totSecKn > 0 ? $"{totSecKn:F0}" : string.Empty
        };

        // ── Page 2: ETCS values ────────────────────────────────────────────────
        double p2Total    = entries.Sum(e => e.TotalWeightTonnes);
        double p2Len      = entries.Sum(e => e.LengthM);
        double p2Ab       = entries.Where(e => e.BrakesEnabled).Sum(e => BrakingCalculator.ActiveBrake(e));
        double p2Pct      = p2Total > 0 ? Math.Floor(p2Ab / p2Total * 100.0) : 0;
        string p2Fp       = BrakingCalculator.ConsistFpClass(entries);
        int    p2FpMm     = p2Fp == "FP3" ? 130 : 100;
        string p2AxleLoad = BrakingCalculator.ConsistAxleLoad(entries) ?? "—";
        bool   p2LowBrake = p2Pct < 50;

        // ── PDF rendering ──────────────────────────────────────────────────────
        using var ms     = new MemoryStream();
        using var pdfDoc = SKDocument.CreatePdf(ms);

        SKCanvas cv = null!;

        // ── Drawing helpers (thin wrappers that capture the current canvas) ───────

        SKPaint Pt(float size, bool bold = false, SKColor? color = null)
            => CustomSkiaPDFHelpers.CreatePaint(size, bold, color);

        void Box(float x, float y, float w, float h, float strokeWidth = 0.8f)
            => CustomSkiaPDFHelpers.StrokeRect(cv, x, y, w, h, strokeWidth);

        void Fill(float x, float y, float w, float h, SKColor color)
            => CustomSkiaPDFHelpers.FillRect(cv, x, y, w, h, color);

        void Txt(string text, float x, float y2, SKPaint paint)
            => CustomSkiaPDFHelpers.DrawText(cv, text, x, y2, paint);

        void Sides(float x, float y, float w, float h, RS sides, float strokeWidth = 0.8f)
            => CustomSkiaPDFHelpers.StrokeRectSides(cv, x, y, w, h, sides, strokeWidth);

        float[] Cols() // label | sub | A | B | C | D | E | end
        {
            float lw  = ContentW * 0.24f;
            float sw2 = ContentW * 0.045f;
            float dw  = (ContentW - lw - sw2) / 5f;
            return new[]
            {
                Margin,
                Margin + lw,
                Margin + lw + sw2,
                Margin + lw + sw2 + dw,
                Margin + lw + sw2 + dw * 2,
                Margin + lw + sw2 + dw * 3,
                Margin + lw + sw2 + dw * 4,
                Margin + ContentW
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PAGE 1 — MZOB form
        // ═══════════════════════════════════════════════════════════════════════
        cv = pdfDoc.BeginPage(PageW, PageH);
        Fill(0, 0, PageW, PageH, C("#ffffff"));

        using var normalPt = Pt(7);
        using var boldPt   = Pt(8,    bold: true);
        using var smallPt  = Pt(5.5f, color: C("#444444"));
        using var titlePt  = Pt(13,   bold: true);
        using var logoPt   = Pt(11,   bold: true, color: C("#000000"));

        float y = Margin;
        float[] cx = Cols();

        // ── Header ─────────────────────────────────────────────────────────────
        float hdrH = 38;
        Box(Margin, y, 80, hdrH);
        Txt("2154 CDC", Margin + 4, y + 11, smallPt);
        Txt("ČD Cargo, a. s.", Margin + 4, y + 28, logoPt);

        float centerBoxX = Margin + 80, centerBoxW = ContentW - 150;
        Box(centerBoxX, y, centerBoxW, hdrH);
        float titleTw = titlePt.MeasureText("Mezinárodní zpráva o brzdění");
        Txt("Mezinárodní zpráva o brzdění", centerBoxX + (centerBoxW - titleTw) / 2, y + 24, titlePt);

        Box(Margin + ContentW - 70, y, 70, hdrH);
        Txt("Vytvořeno:",                    Margin + ContentW - 68, y + 12, smallPt);
        Txt(now.ToString("dd.MM.yyyy HH:mm"), Margin + ContentW - 68, y + 26, normalPt);
        y += hdrH;

        // ── Fields 1–4 ─────────────────────────────────────────────────────────
        float fieldH = 28;
        float[] fw   = { 0, ContentW * 0.22f, ContentW * 0.22f, ContentW * 0.28f, ContentW * 0.28f };
        float   fx   = Margin;
        string[] flabels = { "", "1  Číslo vlaku", "2  Datum odjezdu", "3  Výchozí stanice", "4  Konečná stanice" };
        string[] fvalues = { "", consistName, now.ToString("dd.MM.yyyy"), startStation ?? string.Empty, endStation ?? string.Empty };
        for (int i = 1; i <= 4; i++)
        {
            Box(fx, y, fw[i], fieldH);
            Txt(flabels[i], fx + 3, y + 9,  smallPt);
            Txt(fvalues[i], fx + 3, y + 22, normalPt);
            fx += fw[i];
        }
        y += fieldH;

        // ── Field 5: Poznámky ───────────────────────────────────────────────────
        float noteH = 200;
        //Box(Margin, y, ContentW, noteH);
        Sides(Margin, y, ContentW, noteH, RS.Top | RS.Right | RS.Left);
        Txt("5  Poznámky", Margin + 3, y + 10, smallPt);
        y += noteH;

        // ── Column headers (letter row + description row) ───────────────────────
        // Desktop layout: label+sub area has LEFT border only (no top, no right, no
        // bottom between the two header rows). A–E cells have RIGHT+BOTTOM (no top,
        // no left). The top of the first data row closes the label+sub area at bottom.
        float hdr1H = 14, hdr2H = 16;

        // Row 1 – letters
        Sides(cx[0], y, cx[2] - cx[0], hdr1H, RS.Left);
        string[] letters = { "A", "B", "C", "D", "E" };
        for (int i = 0; i < 5; i++)
        {
            float x0 = cx[i + 2], w0 = cx[i + 3] - x0;
            Sides(x0, y, w0, hdr1H, RS.All);
            float tw = boldPt.MeasureText(letters[i]);
            Txt(letters[i], x0 + (w0 - tw) / 2, y + hdr1H - 3, boldPt);
        }
        y += hdr1H;

        // Row 2 – descriptions
        Sides(cx[0], y, cx[2] - cx[0], hdr2H, RS.Left);
        string[] descs = { "Činná hnací vozidla", "Dopravovaná hnací vozidla", "Vozy celkem", "Souprava B+C", "Vlak A+D" };
        for (int i = 0; i < 5; i++)
        {
            float x0 = cx[i + 2], w0 = cx[i + 3] - x0;
            Sides(x0, y, w0, hdr2H, RS.Right | RS.Bottom | RS.Left);
            Txt(descs[i], x0 + 2, y + hdr2H - 4, smallPt);
        }
        y += hdr2H;

        // ── Rows 6–10: 3 sub-rows each; only sub-row 1 has data ────────────────
        float subH = 14;
        string[] rowLabels = {
            "6  Počet", "7  Hmotnost [t]", "8  Brzdící váha [t] s/bez EDB",
            "9  Délka [m] / nápravy", "10  Zajišťovací síla [kN]"
        };

        for (int row = 0; row < 5; row++)
        {
            float rowTop    = y;
            float rowTotalH = subH * 3;

            Box(cx[0], rowTop, cx[1] - cx[0], rowTotalH);
            Txt(rowLabels[row], cx[0] + 3, rowTop + rowTotalH / 2 + 3, normalPt);

            for (int sub = 0; sub < 3; sub++)
            {
                float sy = rowTop + sub * subH;

                Box(cx[1], sy, cx[2] - cx[1], subH, strokeWidth: 0.5f);
                Txt($"{row + 6}.{sub + 1}", cx[1] + 2, sy + subH - 3, smallPt);

                for (int ci = 0; ci < 5; ci++)
                {
                    float x0 = cx[ci + 2], w0 = cx[ci + 3] - x0;
                    Box(x0, sy, w0, subH, strokeWidth: 0.5f);
                    string val = sub == 0 ? rowData[row][ci] : string.Empty;
                    if (!string.IsNullOrEmpty(val))
                        Txt(val, x0 + 3, sy + subH - 3, normalPt);
                }
            }
            y += rowTotalH;
        }

        // ── Row 11: Brake mode table (9 equal columns) ─────────────────────────
        // Mirrors desktop: section-header row, sub-column-header row, 3 data rows.
        // No top border anywhere. Left border only on col-0 of data rows and
        // col-1 of the sub-header row (matching QuestPDF border logic).
        {
            float cw11  = ContentW / 9f;
            float sh11H = 18f;  // section-header height
            float ch11H = 14f;  // column-header height
            float d11H  = 11f;  // data-row height

            // Shorthand for column x-position
            float X11(int col) => Margin + col * cw11;

            // — Section header row (no top border, no col-0 cell) ——————————
            // Cols 1–6 span: main label
            Sides(X11(0), y, 6 * cw11, 2 * sh11H, RS.Left);
            Sides(X11(1), y, 6 * cw11, sh11H, RS.All);
            
            using var sh11Pt = Pt(5.5f);
            Txt("11", cw11, y + (sh11H / 2), Pt(8f, true));
            Txt("Počet vozidel v soupravě s brzdou v činnosti",X11(1) + 3, y + 12, sh11Pt);
            // Col 7: securing
            Sides(X11(7), y, cw11, sh11H, RS.Top | RS.Right | RS.Bottom);
            using var sh11Tiny = Pt(4.5f, color: C("#333333"));
            Txt("Počet zajišť.",  X11(7) + 2, y + 9,  sh11Tiny);
            Txt("brzd",          X11(7) + 2, y + 16, sh11Tiny);
            // Col 8: disabled
            Sides(X11(8), y, cw11, sh11H, RS.Right | RS.Bottom);
            Txt("Vypnuté",       X11(8) + 2, y + 9,  sh11Tiny);
            Txt("brzdy",        X11(8) + 2, y + 16, sh11Tiny);
            y += sh11H;

            // — Sub-column header row (no top, col-1 has left border) ———————
            string[] r11TopLbl = { "F",  "G",      "H", "I", "J", "K",    "L", "M" };
            string[] r11BotLbl = { "D",  "K,L,LL", "G", "P", "R", "R+Mg", "L", "M" };
            using var r11BoldLbl = Pt(5.5f, bold: true);
            using var r11ModeLbl = Pt(4.5f, color: C("#666666"));
            for (int ci = 0; ci < 8; ci++)
            {
                Sides(X11(ci + 1), y, cw11, ch11H,
                      RS.Top | RS.Right | RS.Bottom | (ci == 0 ? RS.Left : RS.None));
                float cx11 = X11(ci + 1);
                float tw1  = r11BoldLbl.MeasureText(r11TopLbl[ci]);
                Txt(r11TopLbl[ci], cx11 + (cw11 - tw1) / 2, y + 7, r11BoldLbl);
                float tw2  = r11ModeLbl.MeasureText(r11BotLbl[ci]);
                Txt(r11BotLbl[ci], cx11 + (cw11 - tw2) / 2, y + ch11H - 2, r11ModeLbl);
            }
            y += ch11H;

            // — Data rows 11.1–11.3 (no top, col-0 has left border) ——————————
            string[] r11Val = new string[9];
            r11Val[4] = pCount        > 0 ? pCount.ToString()        : string.Empty;
            r11Val[5] = rCount        > 0 ? rCount.ToString()        : string.Empty;
            r11Val[7] = securingCount > 0 ? securingCount.ToString() : string.Empty;
            r11Val[8] = disabledCount > 0 ? disabledCount.ToString() : string.Empty;

            using var r11SubPt = Pt(5.5f, color: C("#555555"));
            for (int ri = 0; ri < 3; ri++)
            {
                // Col 0: sub-row label
                Sides(X11(0), y, cw11, d11H, RS.All);
                string subLbl = $"11.{ri + 1}";
                float  slTw   = r11SubPt.MeasureText(subLbl);
                Txt(subLbl, X11(0) + (cw11 - slTw) / 2, y + d11H - 3, r11SubPt);

                // Cols 1–8: values (only row 0 has data)
                for (int ci = 1; ci < 9; ci++)
                {
                    Sides(X11(ci), y, cw11, d11H, RS.Right | RS.Bottom);
                    string val = ri == 0 ? (r11Val[ci] ?? string.Empty) : string.Empty;
                    if (!string.IsNullOrEmpty(val))
                    {
                        float tw = normalPt.MeasureText(val);
                        Txt(val, X11(ci) + (cw11 - tw) / 2, y + d11H - 3, normalPt);
                    }
                }
                y += d11H;
            }
        }

        // ── Row 12: Braking percentages ─────────────────────────────────────────
        // Desktop relative column widths: 18:7:10:14:14:25:12  (total 100)
        // No top border; left border only on col 0.
        {
            float[] r12w  = { 18f, 7f, 10f, 14f, 14f, 25f, 12f };
            float[] r12x  = new float[8];
            r12x[0] = Margin;
            for (int i = 0; i < 7; i++)
                r12x[i + 1] = r12x[i] + ContentW * r12w[i] / 100f;

            float r12HdrH  = 24f;
            float r12DataH = 16f;

            // Header row
            string[] r12Letters = { "N", "O", "P", "R", "S", "T", "U" };
            string[] r12Labels  = {
                "Výchozí stanice", "Režim", "Potřebná %",
                "Skutečná % s/bez EDB", "Chybějící % s/bez EDB",
                "Podpis stroj.", "Č. hnacího voz."
            };
            using var r12LblPt = Pt(5f, color: C("#444444"));
            for (int ci = 0; ci < 7; ci++)
            {
                float colW = r12x[ci + 1] - r12x[ci];
                Sides(r12x[ci], y, colW, r12HdrH,
                      RS.Right | RS.Bottom | (ci == 0 ? RS.Left : RS.None));
                // label (small, italic-ish)
                float ltw = r12LblPt.MeasureText(r12Labels[ci]);
                Txt(r12Labels[ci], r12x[ci] + (colW - ltw) / 2, y + 10, r12LblPt);
                // letter (bold, larger)
                float btw = boldPt.MeasureText(r12Letters[ci]);
                Txt(r12Letters[ci], r12x[ci] + (colW - btw) / 2, y + 21, boldPt);
            }
            y += r12HdrH;

            // Data rows 12.1–12.3
            // Row 0: actual data; rows 1–2: "/" in actual% and missing% cols
            string[][] r12Data = {
                new[] { startStation ?? string.Empty, bMode,
                        reqPct.HasValue ? $"{reqPct:F0}" : string.Empty,
                        actualStr, missingStr, string.Empty, leadNum },
                new[] { string.Empty, string.Empty, string.Empty, "/", "/", string.Empty, string.Empty },
                new[] { string.Empty, string.Empty, string.Empty, "/", "/", string.Empty, string.Empty },
            };

            using var r12SubPt = Pt(5.5f, color: C("#555555"));
            for (int ri = 0; ri < 3; ri++)
            {
                // Col 0: sub-row number (top) + optional station (below)
                float col0W = r12x[1] - r12x[0];
                Sides(r12x[0], y, col0W, r12DataH,
                      RS.Right | RS.Bottom | RS.Left);
                Txt($"12.{ri + 1}", r12x[0] + 2, y + 7, r12SubPt);
                if (!string.IsNullOrEmpty(r12Data[ri][0]))
                    Txt(r12Data[ri][0], r12x[0] + 2, y + 14, normalPt);

                // Cols 1–6
                for (int ci = 1; ci < 7; ci++)
                {
                    float colW = r12x[ci + 1] - r12x[ci];
                    Sides(r12x[ci], y, colW, r12DataH,
                          RS.Right | RS.Bottom);
                    string val = r12Data[ri][ci];
                    if (!string.IsNullOrEmpty(val))
                    {
                        // bMode (ci=1, row 0) is bold; everything else normal
                        SKPaint vp  = (ci == 1 && ri == 0) ? boldPt : normalPt;
                        float   tw  = vp.MeasureText(val);
                        Txt(val, r12x[ci] + (colW - tw) / 2, y + r12DataH - 4, vp);
                    }
                }
                y += r12DataH;
            }
        }

        // ── Row 13: Potvrzení o zkoušce brzdy ───────────────────────────────────
        float r13H = 28;
        Box(Margin, y, ContentW, r13H);
        Txt("13  Potvrzení o zkoušce brzdy", Margin + 3, y + 11, boldPt);
        Txt("Konec: ___________  Hod. ______  Min. ______  Datum: ___________  Podpis: _______________",
            Margin + 3, y + 23, smallPt);
        y += r13H;

        // ── Rows 14 & 15 ────────────────────────────────────────────────────────
        float r14H = 26;
        Box(Margin,           y, ContentW / 2f, r14H);
        Txt("14  Číslo posledního vozidla ve vlaku", Margin + 3, y + 10, smallPt);
        Txt(lastNum,           Margin + 3, y + 22, normalPt);

        Box(Margin + ContentW / 2f, y, ContentW / 2f, r14H);
        Txt("15  Nejvyšší rychlost soupravy [km/h]", Margin + ContentW / 2f + 3, y + 10, smallPt);
        Txt($"{maxSpeed}",     Margin + ContentW / 2f + 3, y + 22, boldPt);
        y += r14H;

        // ── Rows 16 & 17 ────────────────────────────────────────────────────────
        float r16H = 30;
        Box(Margin,           y, ContentW / 2f, r16H);
        Txt("16  Nebezpečné věci na vlaku (RID)", Margin + 3, y + 11, smallPt);
        Txt("Ano [ ]   Ne [X]", Margin + 3, y + 24, normalPt);

        Box(Margin + ContentW / 2f, y, ContentW / 2f, r16H);
        Txt("17  MZ na vlaku", Margin + ContentW / 2f + 3, y + 11, smallPt);
        Txt("Ano [ ]   Ne [X]", Margin + ContentW / 2f + 3, y + 24, normalPt);
        y += r16H;

        // ── Formula row ─────────────────────────────────────────────────────────
        float fmH = 16;
        Box(Margin, y, ContentW, fmH);
        Txt("12-R = 100 × (0,75 × BVG + BVF) × K / mv          ETCS = 100 × (0,75 × BVG + BVF) / mv",
            Margin + ContentW / 2 - 130, y + 11, smallPt);

        pdfDoc.EndPage();

        // ═══════════════════════════════════════════════════════════════════════
        // PAGE 2 — ETCS summary (mirrors ZoBPdfService page 2 layout)
        // ═══════════════════════════════════════════════════════════════════════
        const float M2  = 42f;
        float       CW2 = PageW - M2 * 2;

        cv = pdfDoc.BeginPage(PageW, PageH);
        Fill(0, 0, PageW, PageH, C("#ffffff"));

        using var p2Name     = Pt(16, bold: true,  color: C("#1a1a2e"));
        using var p2Sub      = Pt(10,               color: C("#666666"));
        using var p2Meta     = Pt(10,               color: C("#888888"));
        using var p2EtcsTtl  = Pt(9,  bold: true,  color: C("#aaaaaa"));
        using var p2EtcsLbl  = Pt(10,               color: C("#aaaaaa"));
        using var p2White    = Pt(10, bold: true,  color: C("#ffffff"));
        using var p2Orange   = Pt(10, bold: true,  color: C("#f97316"));
        using var p2Green    = Pt(10, bold: true,  color: C("#27ae60"));
        using var p2RedV     = Pt(10, bold: true,  color: C("#c0392b"));
        using var p2TblHdr   = Pt(9,  bold: true,  color: C("#ffffff"));
        using var p2TblCell  = Pt(10,               color: C("#111111"));
        using var p2TblBold  = Pt(10, bold: true,  color: C("#111111"));
        using var p2TblMeta  = Pt(10,               color: C("#888888"));
        using var p2TblRed   = Pt(10,               color: C("#c0392b"));
        using var p2TblGreen = Pt(10,               color: C("#27ae60"));
        using var p2SecTitle = Pt(9,  bold: true,  color: C("#666666"));
        using var p2Legend   = Pt(8,                color: C("#888888"));
        using var p2Footer   = Pt(9,                color: C("#999999"));

        float y2 = M2;

        // ── Header: consist name + subtitle + date ──────────────────────────────
        Txt(consistName, M2, y2 + 16, p2Name);
        Txt("ETCS – parametry vlaku", M2, y2 + 32, p2Sub);
        string dateStr = now.ToString("dd. MM. yyyy HH:mm");
        float  dateTw  = p2Meta.MeasureText(dateStr);
        Txt(dateStr, M2 + CW2 - dateTw, y2 + 16, p2Meta);
        y2 += 40;

        // Orange rule
        Fill(M2, y2, CW2, 2, C("#f97316"));
        y2 += 12;

        // ── ETCS dark box ────────────────────────────────────────────────────────
        string[] etcsLabels = {
            "Průjezdný průřez",
            "Nedostatek převýšení",
            "Max. rychlost ETCS",
            "Brzdící procenta",
            "Délka vlaku",
            "Traťové zatížení"
        };
        string[] etcsValues = {
            "GC",
            $"{p2Fp}  ({p2FpMm} mm)",
            $"{maxSpeed} km/h",
            $"{p2Pct:F0} %",
            $"{p2Len:F0} m",
            p2AxleLoad
        };

        const float etcsPad  = 14f;
        const float etcsRowH = 22f;
        float etcsBoxH = etcsPad * 2 + 12 + 8 + etcsLabels.Length * etcsRowH;
        Fill(M2, y2, CW2, etcsBoxH, C("#1a1a2e"));

        float iy = y2 + etcsPad;
        Txt("ETCS PARAMETRY", M2 + etcsPad, iy + 9, p2EtcsTtl);
        iy += 12 + 8;

        float halfW = CW2 / 2f;
        for (int i = 0; i < etcsLabels.Length; i++)
        {
            float ry = iy + i * etcsRowH;
            Txt(etcsLabels[i], M2 + etcsPad, ry + 14, p2EtcsLbl);
            SKPaint valPaint = i switch {
                0 => p2Orange,
                2 => p2Orange,
                3 => p2LowBrake ? p2RedV : p2Green,
                5 => p2Orange,
                _ => p2White
            };
            Txt(etcsValues[i], M2 + etcsPad + halfW, ry + 14, valPaint);
        }
        y2 += etcsBoxH + 12;

        // ── Consist table ────────────────────────────────────────────────────────
        Txt("SLOŽENÍ SOUPRAVY", M2, y2 + 10, p2SecTitle);
        y2 += 18;

        // Column boundaries: 6 cols with relative widths 3:2:2:2:2:2 (total 13)
        float[] tc = {
            M2,
            M2 + CW2 * (3f / 13f),
            M2 + CW2 * (5f / 13f),
            M2 + CW2 * (7f / 13f),
            M2 + CW2 * (9f / 13f),
            M2 + CW2 * (11f / 13f),
            M2 + CW2
        };
        string[] tHdrs = {
            "Lokomotivy", "Hmotnost", "Brd. váha", "Brd. váha EDB", "Délka", "Brzdy"
        };
        const float thdrH = 24f;
        const float trowH = 22f;

        // Header row
        Fill(M2, y2, CW2, thdrH, C("#1a1a2e"));
        for (int ci = 0; ci < 6; ci++)
        {
            float colX = tc[ci], colW = tc[ci + 1] - colX;
            float tw   = p2TblHdr.MeasureText(tHdrs[ci]);
            Txt(tHdrs[ci], colX + (colW - tw) / 2, y2 + thdrH - 7, p2TblHdr);
        }
        y2 += thdrH;

        // Data rows
        double abBase2    = 0;
        string[] rowBgHex = { "#ffffff", "#f9f9f9" };
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            Fill(M2, y2, CW2, trowH, C(rowBgHex[i % 2]));

            double bwBase = e.BrakesEnabled
                ? (e.RModeActive && e.BrakingWeightTonnesR.HasValue
                    ? e.BrakingWeightTonnesR!.Value
                    : e.BrakingWeightTonnes)
                : 0;
            abBase2 += bwBase;

            string bwEdbStr = !e.BrakesEnabled ? "—"
                : e.RModeActive && e.BrakingWeightWithEDBR.HasValue
                    ? $"{e.BrakingWeightWithEDBR!.Value:F0} t"
                : !e.RModeActive && e.BrakingWeightWithEDB.HasValue
                    ? $"{e.BrakingWeightWithEDB!.Value:F0} t"
                : "—";

            string brakesTxt = e.BrakesEnabled
                ? (e.EdbActive ? (e.RModeActive ? "R+E" : "P+E") : (e.RModeActive ? "R" : "P"))
                : "x";

            string[] vals = {
                e.CustomName ?? e.Designation,
                $"{e.TotalWeightTonnes:F1} t",
                $"{bwBase:F0} t",
                bwEdbStr,
                $"{e.LengthM:F0} m",
                brakesTxt
            };
            SKPaint[] valPaints = {
                p2TblCell,
                p2TblCell,
                e.BrakesEnabled ? p2TblCell : p2TblRed,
                bwEdbStr == "—" ? p2TblMeta : p2TblCell,
                p2TblCell,
                e.BrakesEnabled ? p2TblGreen : p2TblRed
            };

            for (int ci = 0; ci < 6; ci++)
            {
                float colX = tc[ci], colW = tc[ci + 1] - colX;
                float tw   = valPaints[ci].MeasureText(vals[ci]);
                float tx   = ci == 0 ? colX + 5 : colX + (colW - tw) / 2;
                Txt(vals[ci], tx, y2 + trowH - 6, valPaints[ci]);
            }
            y2 += trowH;
        }

        // Totals row
        Fill(M2, y2, CW2, trowH, C("#f0f0f0"));
        string[] totVals = {
            "Celkem",
            $"{p2Total:F1} t",
            $"{abBase2:F0} t",
            $"{p2Ab:F0} t",
            $"{p2Len:F0} m",
            "—"
        };
        for (int ci = 0; ci < 6; ci++)
        {
            float colX = tc[ci], colW = tc[ci + 1] - colX;
            float tw   = p2TblBold.MeasureText(totVals[ci]);
            float tx   = ci == 0 ? colX + 5 : colX + (colW - tw) / 2;
            Txt(totVals[ci], tx, y2 + trowH - 6, p2TblBold);
        }
        y2 += trowH + 8;

        // FP legend
        Txt("FP3 (130 mm): 163, 186, 189, 363, 363.5, 372, 383, 386, 388, 393  |  FP2 (100 mm): ostatní",
            M2, y2 + 8, p2Legend);

        // ── Footer ──────────────────────────────────────────────────────────────
        string footerTxt = $"LocoCalc  ·  ČD Cargo MZOB  ·  {now:dd. MM. yyyy HH:mm}";
        float  ftw       = p2Footer.MeasureText(footerTxt);
        Txt(footerTxt, (PageW - ftw) / 2f, PageH - 20, p2Footer);

        pdfDoc.EndPage();
        pdfDoc.Close();

        return ms.ToArray();
    }
}
