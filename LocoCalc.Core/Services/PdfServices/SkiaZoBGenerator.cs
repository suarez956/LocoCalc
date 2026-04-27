using LocoCalc.Models;
using SkiaSharp;
using RS = LocoCalc.Services.RectSides;

namespace LocoCalc.Services.PdfServices;

/// <summary>
/// Generates the MZOB (Mezinárodní zpráva o brzdění) PDF using SkiaSharp.
/// Shared by Desktop and Android — no platform-specific dependencies.
/// </summary>
public class SkiaZoBGenerator
{
    private const float PageW   = 595f;
    private const float PageH   = 842f;
    private const float Margin  = 28f;
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
        // Without EDB, in the loco's current mode (P or R)
        static double BrakeNoEDB(ConsistEntry e)
        {
            if (!e.BrakesEnabled) return 0;
            if (e.RModeActive && e.BrakingWeightTonnesR.HasValue) return e.BrakingWeightTonnesR.Value;
            return e.BrakingWeightTonnes;
        }
        // With EDB assumed active, in the loco's current mode (P or R)
        static double BrakeWithEDB(ConsistEntry e)
        {
            if (!e.BrakesEnabled) return 0;
            if (e.RModeActive)
            {
                if (e.BrakingWeightWithEDBR.HasValue) return e.BrakingWeightWithEDBR.Value;
                if (e.BrakingWeightTonnesR.HasValue)  return e.BrakingWeightTonnesR.Value;
            }
            return e.BrakingWeightWithEDB ?? e.BrakingWeightTonnes;
        }

        var active      = entries.Where(e => !e.IsTransported).ToList();
        var transported = entries.Where(e =>  e.IsTransported).ToList();

        int    activeCount    = active.Count;
        int    transportedCount   = transported.Count;
        double activeWeightTotal  = active.Sum(e => e.TotalWeightTonnes);
        double transportedWeightTotal = transported.Sum(e => e.TotalWeightTonnes);
        double activeBrakesNoEdb    = active.Sum(BrakeNoEDB);
        double transportedBrakesNoEdb   = transported.Sum(BrakeNoEDB);
        double? activeBrakesWithEdb      = active.Where(e => e is { HasEDB: true, BrakesEnabled: true }).Any()
                                             ? active.Sum(BrakeWithEDB) : null;
        double? transportedBrakesWithEdb = transported.Where(e => e is { HasEDB: true, BrakesEnabled: true }).Any()
                                             ? transported.Sum(BrakeWithEDB) : null;
        double activeLengthM    = active.Sum(e => e.LengthM);
        double transportedLengthM   = transported.Sum(e => e.LengthM);
        int    activeAxles   = active.Sum(e => e.AxleCount);
        int    transportedAxles  = transported.Sum(e => e.AxleCount);
        double? activeSecuringForce      = active.Where(e => e.SecuringForceKn.HasValue)
                                             .Aggregate((double?)null, (sum, e) => (sum ?? 0) + e.SecuringForceKn!.Value);
        double? transportedSecuringForce = transported.Where(e => e.SecuringForceKn.HasValue)
                                             .Aggregate((double?)null, (sum, e) => (sum ?? 0) + e.SecuringForceKn!.Value);

        // ── Totals for braking % ───────────────────────────────────────────────
        double weightTotal        = activeWeightTotal + transportedWeightTotal;
        double actualEdb   = weightTotal > 0 ? Math.Floor(((activeBrakesWithEdb ?? activeBrakesNoEdb) + (transportedBrakesWithEdb ?? transportedBrakesNoEdb)) / weightTotal * 100) : 0;
        double actualNoEdb = weightTotal > 0 ? Math.Floor((activeBrakesNoEdb + transportedBrakesNoEdb) / weightTotal * 100) : 0;
        string actualStr   = (activeBrakesWithEdb.HasValue || transportedBrakesWithEdb.HasValue)
                                 ? $"{actualEdb:F0} / {actualNoEdb:F0}"
                                 : $"{actualNoEdb:F0}";

        double? requiredPct = null;
        if (!string.IsNullOrWhiteSpace(requiredBrakingPct) &&
            double.TryParse(requiredBrakingPct!.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var rp))
            requiredPct = rp;

        string missingStr = string.Empty;
        if (requiredPct.HasValue)
        {
            if (activeBrakesWithEdb.HasValue || transportedBrakesWithEdb.HasValue)
            {
                double diffEDB   = actualEdb   - requiredPct.Value;
                double diffNoEDB = actualNoEdb - requiredPct.Value;
                if (diffEDB < 0 || diffNoEDB < 0)
                    missingStr = $"{Math.Max(0, -diffEDB):F0} / {Math.Max(0, -diffNoEDB):F0}";
            }
            else
            {
                double diff = actualNoEdb - requiredPct.Value;
                if (diff < 0) missingStr = $"{-diff:F0}";
            }
        }

        int pCount        = entries.Count(e => !e.RModeActive && e.BrakesEnabled);
        int rCount        = entries.Count(e => e.RModeActive && e.BrakesEnabled);
        int disabledCount = entries.Count(e => !e.BrakesEnabled);
        int securingCount = entries.Count(e => e.SecuringForceKn is > 0);
        bool anyR         = entries.Any(e => e.RModeActive && !e.IsTransported);
        string bMode      = anyR ? "R" : "P";
        var    lead       = entries.FirstOrDefault();
        var    last       = entries.LastOrDefault();
        string leadNum    = lead?.CustomName ?? string.Empty;
        string lastNum    = last?.CustomName ?? string.Empty;
        var    now        = DateTime.Now;

        static string BrakingTextValue(double? withEDB, double withoutEDB)
            => withEDB.HasValue && Math.Abs(withEDB.Value - withoutEDB) > 0.5
                ? $"{withEDB:F0} / {withoutEDB:F0}"
                : $"{withoutEDB:F0}";

        static string LengthAxleText(double lenM, int axles)
            => axles > 0 ? $"{lenM:F0} / {axles}" : string.Empty;

        // ── Row 6–10 aggregate values ──────────────────────────────────────────
        string[][] rowData = new string[5][];
        rowData[0] =
        [
            activeCount  > 0 ? activeCount.ToString()  : string.Empty,
            transportedCount > 0 ? transportedCount.ToString()  : string.Empty,
            "0",
            transportedCount > 0 ? transportedCount.ToString()  : "0",
            (activeCount + transportedCount).ToString()
        ];
        rowData[1] =
        [
            activeWeightTotal  > 0 ? $"{activeWeightTotal:F0}"  : string.Empty,
            transportedWeightTotal > 0 ? $"{transportedWeightTotal:F0}" : string.Empty,
            "0",
            transportedWeightTotal > 0 ? $"{transportedWeightTotal:F0}" : "0",
            $"{activeWeightTotal + transportedWeightTotal:F0}"
        ];
        double? totalBrakEDB = activeBrakesWithEdb.HasValue || transportedBrakesWithEdb.HasValue
            ? (activeBrakesWithEdb ?? activeBrakesNoEdb) + (transportedBrakesWithEdb ?? transportedBrakesNoEdb)
            : null;
        rowData[2] =
        [
            activeCount  > 0 ? BrakingTextValue(activeBrakesWithEdb,  activeBrakesNoEdb)  : string.Empty,
            transportedCount > 0 ? BrakingTextValue(transportedBrakesWithEdb, transportedBrakesNoEdb) : string.Empty,
            string.Empty,
            transportedCount > 0 ? BrakingTextValue(transportedBrakesWithEdb, transportedBrakesNoEdb) : "/",
            BrakingTextValue(totalBrakEDB, activeBrakesNoEdb + transportedBrakesNoEdb)
        ];
        rowData[3] =
        [
            activeCount  > 0 ? LengthAxleText(activeLengthM,  activeAxles)  : string.Empty,
            transportedCount > 0 ? LengthAxleText(transportedLengthM, transportedAxles) : "/",
            "/",
            transportedCount > 0 ? LengthAxleText(transportedLengthM, transportedAxles) : "/",
            LengthAxleText(activeLengthM + transportedLengthM, activeAxles + transportedAxles)
        ];
        double totSecKn = (activeSecuringForce ?? 0) + (transportedSecuringForce ?? 0);
        rowData[4] =
        [
            activeCount  > 0 && activeSecuringForce.HasValue  ? $"{activeSecuringForce:F0}"  : string.Empty,
            transportedCount > 0 && transportedSecuringForce.HasValue ? $"{transportedSecuringForce:F0}" : string.Empty,
            string.Empty,
            transportedCount > 0 && transportedSecuringForce.HasValue ? $"{transportedSecuringForce:F0}" : string.Empty,
            totSecKn > 0 ? $"{totSecKn:F0}" : string.Empty
        ];

        // ── Page 2: ETCS values ────────────────────────────────────────────────
        double p2Ab  = entries.Where(e => e.BrakesEnabled).Sum(e => BrakingCalculator.ActiveBrake(e));
        double p2Pct = weightTotal > 0 ? Math.Floor(p2Ab / weightTotal * 100.0) : 0;
        string p2Fp       = BrakingCalculator.ConsistFpClass(entries);
        int    p2FpMm     = p2Fp == "FP3" ? 130 : 100;
        string p2AxleLoad = BrakingCalculator.ConsistAxleLoad(entries) ?? "—";
        bool   p2LowBrake = p2Pct < 50;

        // ── PDF rendering ──────────────────────────────────────────────────────
        using var ms     = new MemoryStream();
        using var pdfDoc = SKDocument.CreatePdf(ms);

        SKCanvas cv = null!;

        // ── Drawing helpers (thin wrappers that capture the current canvas) ───────

        TextPaint Pt(float size, bool bold = false, SKColor? color = null)
            => CustomSkiaPDFHelpers.CreateTextStyle(size, bold, color);

        void Box(float x, float y, float w, float h, float strokeWidth = 0.8f)
            => CustomSkiaPDFHelpers.StrokeRect(cv, x, y, w, h, strokeWidth);

        void Fill(float x, float y, float w, float h, SKColor color)
            => CustomSkiaPDFHelpers.FillRect(cv, x, y, w, h, color);

        void Txt(string text, float x, float y2, TextPaint tp)
            => CustomSkiaPDFHelpers.DrawText(cv, text, x, y2, tp);

        void Sides(float x, float y, float w, float h, RS sides, float strokeWidth = 0.8f)
            => CustomSkiaPDFHelpers.StrokeRectSides(cv, x, y, w, h, sides, strokeWidth);

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

        float y   = Margin;
        float lw  = ContentW * 0.24f;
        float sw2 = ContentW * 0.045f;
        float dw  = (ContentW - lw - sw2) / 5f;
        float[] cx = // label | sub | A | B | C | D | E | end
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
        Sides(Margin, y, ContentW, noteH, RS.Top | RS.Right | RS.Left);
        Txt("5  Poznámky", Margin + 3, y + 10, smallPt);
        y += noteH;

        // ── Column headers (letter row + description row) ───────────────────────
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
                    string val;
                    if (sub == 0)
                        val = rowData[row][ci];
                    else if (row == 3)
                        val = "/";                                            // 9.2-3: all cols
                    else if (row == 2 && ci is 0 or 4)
                        val = "/";                                            // 8.2-3: A and E
                    else if (row == 2 && ci == 3 && transportedCount == 0)
                        val = "/";                                            // 8.2-3: D, no transported
                    else
                        val = string.Empty;
                    if (!string.IsNullOrEmpty(val))
                    {
                        float tw = normalPt.MeasureText(val);
                        Txt(val, x0 + (w0 - tw) / 2, sy + subH - 3, normalPt);
                    }
                }
            }
            y += rowTotalH;
        }

        // ── Row 11: Brake mode table ───────────────────────────────────────────────
        // Col 0: narrow label column ("11" / "11.x"), half of a 1/9 unit.
        // Cols 1–8: 8 equal data columns sharing the remaining width.
        {
            float cw11Lbl   = ContentW / 18f;
            float cw11Data  = (ContentW - cw11Lbl) / 8f;
            float sh11H     = 18f;
            float ch11LtrH  = 8f;
            float ch11ModeH = 8f;
            float d11H      = 11f;

            float X11(int col) => col == 0
                ? Margin
                : Margin + cw11Lbl + (col - 1) * cw11Data;

            // — Section header row ————————————————————————————————————————————
            float totalHdrH = sh11H + ch11LtrH + ch11ModeH;
            Sides(X11(0), y, cw11Lbl, totalHdrH, RS.Left | RS.Right);
            Sides(X11(1), y, 6 * cw11Data, sh11H, RS.All);

            using var sh11Pt = Pt(5.5f);
            float r11NumTw = boldPt.MeasureText("11");
            Txt("11", X11(0) + (cw11Lbl - r11NumTw) / 2f, y + totalHdrH / 2f + 4f, boldPt);
            Txt("Počet vozidel v soupravě s brzdou v činnosti", X11(1) + 3, y + 12, sh11Pt);

            using var sh11Tiny = Pt(4.5f, color: C("#000000"));
            Sides(X11(7), y, cw11Data, sh11H, RS.Top | RS.Right | RS.Bottom);
            Txt("Počet zajišťovacích brzd", X11(7) + 2, y + 9, sh11Tiny);
            Sides(X11(8), y, cw11Data, sh11H, RS.Right | RS.Bottom);
            Txt("Vypnuté brzdy", X11(8) + 2, y + 9, sh11Tiny);
            y += sh11H;

            // F–K: two sub-header rows (letter + mode); L and M: one combined tall cell
            string[] r11TopLbl = { "F",  "G",      "H", "I", "J", "K" };
            string[] r11BotLbl = { "D",  "K,L,LL", "G", "P", "R", "R+Mg" };
            using var r11BoldLbl = Pt(5.5f, bold: true);
            using var r11ModeLbl = Pt(4.5f, color: C("#000000"));

            // — L and M: single tall cell spanning letter + mode height ————————
            float lmCombH = ch11LtrH + ch11ModeH;
            for (int i = 0; i < 2; i++)
            {
                float x0   = X11(7 + i);
                Sides(x0, y, cw11Data, lmCombH, RS.Right | RS.Bottom);
                string lbl = i == 0 ? "L" : "M";
                float  lmTw = r11BoldLbl.MeasureText(lbl);
                Txt(lbl, x0 + (cw11Data - lmTw) / 2, y + lmCombH / 2 + 3, r11BoldLbl);
            }

            // — Sub-header letter row (F–K only) ——————————————————————————————
            for (int ci = 0; ci < 6; ci++)
            {
                Sides(X11(ci + 1), y, cw11Data, ch11LtrH, RS.Top | RS.Right | RS.Bottom | RS.Left);
                float cx11 = X11(ci + 1);
                float tw1  = r11BoldLbl.MeasureText(r11TopLbl[ci]);
                Txt(r11TopLbl[ci], cx11 + (cw11Data - tw1) / 2, y + ch11LtrH - 2, r11BoldLbl);
            }
            y += ch11LtrH;

            // — Sub-header mode designation row (F–K only) ———————————————————
            for (int ci = 0; ci < 6; ci++)
            {
                Sides(X11(ci + 1), y, cw11Data, ch11ModeH, RS.Right | RS.Bottom);
                float cx11 = X11(ci + 1);
                float tw2  = r11ModeLbl.MeasureText(r11BotLbl[ci]);
                Txt(r11BotLbl[ci], cx11 + (cw11Data - tw2) / 2, y + ch11ModeH - 2, r11ModeLbl);
            }
            y += ch11ModeH;

            // — Data rows 11.1–11.3 ————————————————————————————————————————————
            string[] r11Val =
            [
                string.Empty,                                                 // 0 — unused (loop starts at 1)
                string.Empty,                                                 // 1: F  (D brake)  — not tracked
                string.Empty,                                                 // 2: G  (K,L,LL)  — not tracked
                string.Empty,                                                 // 3: H  (G brake) — not tracked
                pCount        > 0 ? pCount.ToString()        : string.Empty, // 4: I  (P mode)
                rCount        > 0 ? rCount.ToString()        : string.Empty, // 5: J  (R mode)
                string.Empty,                                                 // 6: K  (R+Mg)    — not tracked
                securingCount > 0 ? securingCount.ToString() : string.Empty, // 7: L  (securing)
                disabledCount > 0 ? disabledCount.ToString() : string.Empty, // 8: M  (disabled)
            ];

            using var r11SubPt = Pt(5.5f, color: C("#000000"));
            for (int ri = 0; ri < 3; ri++)
            {
                Sides(X11(0), y, cw11Lbl, d11H, RS.All);
                string subLbl = $"11.{ri + 1}";
                float  slTw   = r11SubPt.MeasureText(subLbl);
                Txt(subLbl, X11(0) + (cw11Lbl - slTw) / 2, y + d11H - 3, r11SubPt);

                for (int ci = 1; ci < 9; ci++)
                {
                    Sides(X11(ci), y, cw11Data, d11H, RS.Right | RS.Bottom);
                    string val = ri == 0 ? r11Val[ci] : string.Empty;
                    if (!string.IsNullOrEmpty(val))
                    {
                        float tw = normalPt.MeasureText(val);
                        Txt(val, X11(ci) + (cw11Data - tw) / 2, y + d11H - 3, normalPt);
                    }
                }
                y += d11H;
            }
        }

        // ── Row 12: Braking percentages ─────────────────────────────────────────
        // Narrow left column with "12" / "12.x" labels, N–U data columns to the right.
        {
            float r12LblW  = ContentW / 18f;
            float r12DataW = ContentW - r12LblW;

            // N–U data columns: N and U wider (station name / loco number)
            float[] r12Pct = { 22f, 6f, 9f, 12f, 12f, 23f, 16f };
            float   r12Sum = r12Pct.Sum();
            float[] r12x   = new float[8];
            r12x[0] = Margin + r12LblW;
            for (int i = 0; i < 7; i++)
                r12x[i + 1] = r12x[i] + r12DataW * r12Pct[i] / r12Sum;

            float r12LblH  = 12f;
            float r12LtrH  = 12f;
            float r12DataH = 16f;

            string[] r12Letters = { "N", "O", "P", "R", "S", "T", "U" };
            string[] r12Labels  = {
                "Výchozí stanice", "Režim", "Potřebná %",
                "Skutečná % s/bez EDB", "Chybějící % s/bez EDB",
                "Podpis stroj.", "Č. hnacího voz."
            };
            using var r12LblPt = Pt(5f, color: C("#000000"));

            // Left label column: "12" centered across both header rows
            float r12TotalHdrH = r12LblH + r12LtrH;
            Sides(Margin, y, r12LblW, r12TotalHdrH, RS.Left);
            float r12NumTw = boldPt.MeasureText("12");
            Txt("12", Margin + (r12LblW - r12NumTw) / 2f, y + r12TotalHdrH / 2f + 4f, boldPt);

            // Header row 1: description labels
            for (int ci = 0; ci < 7; ci++)
            {
                float colW = r12x[ci + 1] - r12x[ci];
                Sides(r12x[ci], y, colW, r12LblH,
                      RS.Right | RS.Bottom | RS.Left);
                float ltw = r12LblPt.MeasureText(r12Labels[ci]);
                Txt(r12Labels[ci], r12x[ci] + (colW - ltw) / 2, y + r12LblH - 3, r12LblPt);
            }
            y += r12LblH;

            // Header row 2: column letters (N–U)
            for (int ci = 0; ci < 7; ci++)
            {
                float colW = r12x[ci + 1] - r12x[ci];
                Sides(r12x[ci], y, colW, r12LtrH, RS.Right | RS.Bottom | RS.Left);
                float btw = boldPt.MeasureText(r12Letters[ci]);
                Txt(r12Letters[ci], r12x[ci] + (colW - btw) / 2, y + r12LtrH - 3, boldPt);
            }
            y += r12LtrH;

            // Data rows 12.1–12.3
            string[] r12BlankRow = [string.Empty, string.Empty, string.Empty, "/", "/", string.Empty, string.Empty];
            string[][] r12Data =
            {
                [startStation ?? string.Empty, bMode,
                 requiredPct.HasValue ? $"{requiredPct:F0}" : string.Empty,
                 actualStr, missingStr.Length > 0 ? missingStr : "/", string.Empty, leadNum],
                r12BlankRow,
                r12BlankRow,
            };

            using var r12SubPt = Pt(5.5f, color: C("#000000"));
            for (int ri = 0; ri < 3; ri++)
            {
                Sides(Margin, y, r12LblW, r12DataH, RS.Right | RS.Bottom | RS.Left | RS.Top);
                string subLbl = $"12.{ri + 1}";
                float  slTw   = r12SubPt.MeasureText(subLbl);
                Txt(subLbl, Margin + (r12LblW - slTw) / 2f, y + r12DataH - 4f, r12SubPt);

                for (int ci = 0; ci < 7; ci++)
                {
                    float colW = r12x[ci + 1] - r12x[ci];
                    Sides(r12x[ci], y, colW, r12DataH, RS.Right | RS.Bottom);
                    string val = r12Data[ri][ci];
                    if (!string.IsNullOrEmpty(val))
                    {
                        TextPaint vp = (ci == 1 && ri == 0) ? boldPt : normalPt;
                        float   tw = vp.MeasureText(val);
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
        // PAGE 2 — ETCS summary
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
            $"{activeLengthM + transportedLengthM:F0} m",
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
            TextPaint valPaint = i switch {
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

        float[] tc = {
            M2,
            M2 + CW2 * (3f / 13f),
            M2 + CW2 * (5f / 13f),
            M2 + CW2 * (9f / 13f),
            M2 + CW2 * (11f / 13f),
            M2 + CW2
        };
        string[] tHdrs = {
            "Lokomotivy", "Hmotnost", "Brd. váha / EDB", "Délka", "Brzdy"
        };
        const float thdrH = 24f;
        const float trowH = 22f;

        Fill(M2, y2, CW2, thdrH, C("#1a1a2e"));
        for (int ci = 0; ci < 5; ci++)
        {
            float colX = tc[ci], colW = tc[ci + 1] - colX;
            float tw   = p2TblHdr.MeasureText(tHdrs[ci]);
            Txt(tHdrs[ci], colX + (colW - tw) / 2, y2 + thdrH - 7, p2TblHdr);
        }
        y2 += thdrH;

        double totalBwNoEdb  = 0;
        double totalBwWithEdb = 0;
        bool   anyEdb        = false;
        string[] rowBgHex = { "#ffffff", "#f9f9f9" };
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            Fill(M2, y2, CW2, trowH, C(rowBgHex[i % 2]));

            double bwBase  = BrakeNoEDB(e);
            double? bwEdb  = !e.BrakesEnabled ? null
                : e.RModeActive  && e.BrakingWeightWithEDBR.HasValue ? e.BrakingWeightWithEDBR!.Value
                : !e.RModeActive && e.BrakingWeightWithEDB.HasValue  ? e.BrakingWeightWithEDB!.Value
                : (double?)null;

            totalBwNoEdb  += bwBase;
            totalBwWithEdb += bwEdb ?? bwBase;
            if (bwEdb.HasValue) anyEdb = true;

            string bwCombined = !e.BrakesEnabled ? "—"
                : bwEdb.HasValue
                    ? $"{bwBase:F0} / {bwEdb.Value:F0} t"
                    : $"{bwBase:F0} / --- t";

            string brakesTxt = e.BrakesEnabled
                ? (e.EdbActive ? (e.RModeActive ? "R+E" : "P+E") : (e.RModeActive ? "R" : "P"))
                : "x";

            string[] vals = {
                e.CustomName ?? e.Designation,
                $"{e.TotalWeightTonnes:F1} t",
                bwCombined,
                $"{e.LengthM:F0} m",
                brakesTxt
            };
            TextPaint[] valPaints = {
                p2TblCell,
                p2TblCell,
                e.BrakesEnabled ? p2TblCell : p2TblRed,
                p2TblCell,
                e.BrakesEnabled ? p2TblGreen : p2TblRed
            };

            for (int ci = 0; ci < 5; ci++)
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
        string totalBwStr = anyEdb
            ? $"{totalBwNoEdb:F0} / {totalBwWithEdb:F0} t"
            : $"{totalBwNoEdb:F0} / --- t";
        string[] totVals = {
            "Celkem",
            $"{weightTotal:F1} t",
            totalBwStr,
            $"{activeLengthM + transportedLengthM:F0} m",
            "—"
        };
        for (int ci = 0; ci < 5; ci++)
        {
            float colX = tc[ci], colW = tc[ci + 1] - colX;
            float tw   = p2TblBold.MeasureText(totVals[ci]);
            float tx   = ci == 0 ? colX + 5 : colX + (colW - tw) / 2;
            Txt(totVals[ci], tx, y2 + trowH - 6, p2TblBold);
        }
        y2 += trowH + 8;

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
