using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LocoCalcAvalonia.Models;
using LocoCalcAvalonia.Services;

namespace LocoCalcAvalonia.Desktop.Services;

/// <summary>
/// Generates the official "Mezinárodní zpráva o brzdění" (MZOB) PDF form,
/// matching ČD Cargo form 2154 CDC layout. Always Czech-only.
/// </summary>
public class ZoBPdfService : IZoBGenerator
{
    static ZoBPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    byte[] IZoBGenerator.Generate(
        IReadOnlyList<ConsistEntry> entries,
        string consistName,
        int maxSpeed,
        string? requiredBrakingPct,
        string? startStation,
        string? endStation)
    {
        // ── Aggregate active vs transported ───────────────────────────────────
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

        // ── Totals for braking % ──────────────────────────────────────────────
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

        int pCount        = entries.Count(e => !e.IsTransported && !e.RModeActive && e.BrakesEnabled);
        int rCount        = entries.Count(e => !e.IsTransported &&  e.RModeActive && e.BrakesEnabled);
        int disabledCount = entries.Count(e => !e.BrakesEnabled);
        int securingCount = entries.Count(e => e.SecuringForceKn is > 0);
        bool anyR         = entries.Any(e => e.RModeActive && !e.IsTransported);
        string bMode      = anyR ? "R" : "P";
        var    lead       = entries.FirstOrDefault(e => !e.IsTransported) ?? entries.FirstOrDefault();
        var last = entries.LastOrDefault();
        string leadNum    = lead?.CustomName ?? string.Empty;
        string lastNum =  last?.CustomName ?? string.Empty;
        var    now        = DateTime.Now;

        // ── Helpers ──────────────────────────────────────────────────────────
        static string Bv(double? withEDB, double withoutEDB)
            => withEDB.HasValue && Math.Abs(withEDB.Value - withoutEDB) > 0.5
                ? $"{withEDB:F0}/{withoutEDB:F0}"
                : $"{withoutEDB:F0}";

        static string Len(double lenM, int axles)
            => axles > 0 ? $"{lenM:F0}/{axles}" : string.Empty;

        // Relative column weights: label | sub | A | B | C | D | E
        const int RL = 22, RS = 10, RA = 14, RB = 14, RC = 8, RD = 8, RE = 20;

        void MainCols(TableColumnsDefinitionDescriptor c)
        {
            c.RelativeColumn(RL);
            c.RelativeColumn(RS);
            c.RelativeColumn(RA);
            c.RelativeColumn(RB);
            c.RelativeColumn(RC);
            c.RelativeColumn(RD);
            c.RelativeColumn(RE);
        }

        // ── QuestPDF document ─────────────────────────────────────────────────
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(8, Unit.Millimetre);
                page.DefaultTextStyle(t =>
                    t.FontSize(7).FontFamily("Arial").FontColor("#000000"));

                page.Content().Column(col =>
                {
                    // ── Form header ──────────────────────────────────────────
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(80);
                            c.RelativeColumn();
                            c.ConstantColumn(68);
                        });

                        t.Cell().Row(1).Column(1)
                            .Border(1).BorderColor("#000000").Padding(3)
                            .Column(c =>
                            {
                                c.Item().Text("2154 CDC").FontSize(6).FontColor("#000000");
                                c.Item().PaddingTop(2)
                                    .Text(t2 =>
                                    {
                                        t2.Span("ČD ").FontSize(11).Bold().FontColor("#000000");
                                        t2.Span("Cargo, a.s.").FontSize(11).Bold().FontColor("#000000");
                                    });
                            });

                        t.Cell().Row(1).Column(2)
                            .Border(1).BorderColor("#000000").BorderLeft(0)
                            .AlignCenter().AlignMiddle().Padding(3)
                            .Text("Mezinárodní zpráva o brzdění").FontSize(14).Bold();

                        t.Cell().Row(1).Column(3)
                            .Border(1).BorderColor("#000000").BorderLeft(0).Padding(3)
                            .Column(c =>
                            {
                                c.Item().Text("Vytvořeno:").FontSize(6).FontColor("#666666");
                                c.Item().PaddingTop(1).Text(now.ToString("dd.MM.yyyy HH:mm")).FontSize(7.5f);
                            });
                    });

                    // ── Fields 1–4 ───────────────────────────────────────────
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                            c.RelativeColumn(3);
                        });

                        void F(uint c, string lbl, string val)
                        {
                            t.Cell().Row(1).Column(c)
                                .BorderBottom(1).BorderRight(1)
                                .BorderLeft(c == 1 ? 1 : 0).BorderColor("#000000")
                                .Padding(2)
                                .Column(x =>
                                {
                                    x.Item().Text(lbl).FontSize(6).FontColor("#666666");
                                    x.Item().Text(val).FontSize(8);
                                });
                        }

                        F(1, "1  Číslo vlaku",    consistName);
                        F(2, "2  Datum odjezdu",   now.ToString("dd.MM.yyyy"));
                        F(3, "3  Výchozí stanice", startStation ?? string.Empty);
                        F(4, "4  Konečná stanice", endStation   ?? string.Empty);
                    });

                    // ── Field 5: Poznámky ────────────────────────────────────
                    col.Item().Height(200)
                        .Border(1).BorderTop(0).BorderBottom(0).BorderColor("#000000").Padding(2)
                        .Text("5  Poznámky").FontSize(6).FontColor("#666666");

                    // ── Main data table (rows 6–10): 3 sub-rows, only row 1 filled ──
                    col.Item().Table(tbl =>
                    {
                        tbl.ColumnsDefinition(MainCols);

                        // Column header row
                        tbl.Cell().Row(1).Column(1).ColumnSpan(2)
                            .BorderLeft(1).BorderTop(0)
                            .BorderColor("#000000").Padding(1);
                        tbl.Cell().Row(2).Column(1).ColumnSpan(2)
                            .BorderLeft(1).BorderTop(0)
                            .BorderColor("#000000").Padding(1);

                        void ColHdr(uint c, string desc, string letter)
                        {
                            tbl.Cell().Row(1).Column(c).Border(1).BorderColor("#000000").BorderLeft(0).BorderTop(0)
                                .Column(x =>
                            {
                                x.Item().Text(letter).FontSize(8).Bold().AlignCenter();
                            });
                            tbl.Cell().Row(2).Column(c)
                                .Border(1).BorderColor("#000000").BorderTop(0).BorderLeft(0)
                                .Padding(1).AlignCenter()
                                .Column(x =>
                                {
                                    x.Item().Text(desc).FontSize(6f).Italic().FontColor("#444444").AlignLeft();
                                });
                        }

                        ColHdr(3, "Činných hnacích\nvozidel",      "A");
                        ColHdr(4, "Dopravovaných\nhnacích vozidel", "B");
                        ColHdr(5, "Vozy celkem",                    "C");
                        ColHdr(6, "Souprava celkem\nB+C",           "D");
                        ColHdr(7, "Vlak celkem\nA+D",               "E");

                        uint nextRow = 3;
                        const uint sub = 3;

                        // Emit a 3-sub-row section; only sub-row 1 carries data.
                        // No RowSpan — avoids the Height interaction where the spanning cell's
                        // natural height (12.4pt) forces sub-row 1 to 12.4pt while Height(10)
                        // caps data cells at 10pt, leaving only 8pt content (< 8.4pt needed for
                        // FontSize 7). Without RowSpan all cells in a row are exactly Height(10);
                        // using PaddingLeft/Right only gives 10pt vertical content area which fits.
                        void DataSection(
                            string rowLabel, string rowNum,
                            string aVal, string bVal, string cVal, string dVal, string eVal)
                        {
                            for (uint i = 0; i < sub; i++)
                            {
                                uint sr  = nextRow + i;
                                float bt = i == sub - 1 ? 1f : 0.5f;

                                // Col 1: row label in first sub-row only; empty border cells below
                                var lbl = tbl.Cell().Row(sr).Column(1)
                                    .BorderLeft(1).BorderTop(1).BorderRight(1).BorderBottom(bt)
                                    .BorderColor("#000000").Height(10)
                                    .PaddingLeft(2).PaddingRight(2).AlignMiddle();
                                if (i == 0) lbl.Text(rowLabel).FontSize(7);
                                else        lbl.Text(string.Empty).FontSize(7);

                                // Col 2: sub-row number
                                tbl.Cell().Row(sr).Column(2)
                                    .BorderLeft(0).BorderTop(1).BorderRight(1).BorderBottom(bt)
                                    .BorderColor("#000000").Height(10)
                                    .PaddingLeft(1).PaddingRight(1).AlignCenter().AlignMiddle()
                                    .Text($"{rowNum}.{i + 1}").FontSize(5.5f).FontColor("#555555");

                                void DC(uint c, string v)
                                {
                                    tbl.Cell().Row(sr).Column(c)
                                        .BorderLeft(0).BorderTop(0).BorderRight(1).BorderBottom(bt)
                                        .BorderColor("#000000").Height(10)
                                        .PaddingLeft(1).PaddingRight(1).AlignCenter().AlignMiddle()
                                        .Text(v).FontSize(7);
                                }

                                if (i == 0)
                                {
                                    DC(3, aVal); DC(4, bVal); DC(5, cVal);
                                    DC(6, dVal); DC(7, eVal);
                                }
                                else
                                {
                                    DC(3, ""); DC(4, ""); DC(5, "");
                                    DC(6, ""); DC(7, "");
                                }
                            }

                            nextRow += sub;
                        }

                        // Row 6: Počet
                        DataSection("6  Počet", "6",
                            actCount  > 0 ? actCount.ToString()  : string.Empty,
                            tranCount > 0 ? tranCount.ToString() : string.Empty,
                            "0",
                            tranCount > 0 ? tranCount.ToString() : "0",
                            (actCount + tranCount).ToString());

                        // Row 7: Hmotnost
                        DataSection("7  Hmotnost [t]", "7",
                            actWeightT  > 0 ? $"{actWeightT:F0}"  : string.Empty,
                            tranWeightT > 0 ? $"{tranWeightT:F0}" : string.Empty,
                            "0",
                            tranWeightT > 0 ? $"{tranWeightT:F0}" : "0",
                            $"{actWeightT + tranWeightT:F0}");

                        // Row 8: Brzdící váha s/bez EDB
                        DataSection("8  Brzdící váha [t]  s/bez EDB", "8",
                            actCount  > 0 ? Bv(actBrakEDB,  actBrakP)  : string.Empty,
                            tranCount > 0 ? Bv(tranBrakEDB, tranBrakP) : string.Empty,
                            "/",
                            tranCount > 0 ? Bv(tranBrakEDB, tranBrakP) : "/",
                            Bv(actBrakEDB.HasValue ? actBrakEDB + tranBrakP : null,
                               actBrakP + tranBrakP));

                        // Row 9: Délka / nápravy
                        DataSection("9  Délka [m] / nápravy", "9",
                            actCount  > 0 ? Len(actLenM,  actAxles)  : string.Empty,
                            tranCount > 0 ? Len(tranLenM, tranAxles) : string.Empty,
                            "/",
                            tranCount > 0 ? Len(tranLenM, tranAxles) : "/",
                            Len(actLenM + tranLenM, actAxles + tranAxles));

                        // Row 10: Zajišťovací síla
                        var totSecKn = (actSecKn ?? 0) + (tranSecKn ?? 0);
                        DataSection("10  Zajišťovací síla [kN]", "10",
                            actCount  > 0 && actSecKn.HasValue  ? $"{actSecKn:F0}"  : string.Empty,
                            tranCount > 0 && tranSecKn.HasValue ? $"{tranSecKn:F0}" : string.Empty,
                            string.Empty,
                            tranCount > 0 && tranSecKn.HasValue ? $"{tranSecKn:F0}" : string.Empty,
                            totSecKn > 0 ? $"{totSecKn:F0}" : string.Empty);
                    });

                    // ── Row 11: Brake mode counts — single consolidated table ──
                    // 9 equal columns; header spans cols 1–7 (D→R+Mg), col 8 = securing, col 9 = disabled
                    col.Item().Table(r11 =>
                    {
                        r11.ColumnsDefinition(c =>
                        {
                            for (int i = 0; i < 9; i++) c.RelativeColumn(1);
                        });

                        // Row 1: top-level section headers
                        r11.Cell().Row(1).Column(2).ColumnSpan(6)
                            .Border(1).BorderTop(0).BorderColor("#000000")
                            .Height(18).Padding(1).AlignLeft().AlignMiddle()
                            .Text("11   Počet vozidel v soupravě s brzdou v činnosti")
                            .FontSize(6);

                        r11.Cell().Row(1).Column(8)
                            .Border(1).BorderTop(0).BorderLeft(0).BorderColor("#000000")
                            .Height(18).Padding(1).AlignLeft().AlignMiddle()
                            .Text("Počet zajišťovacích brzd").FontSize(6);

                        r11.Cell().Row(1).Column(9)
                            .Border(1).BorderTop(0).BorderLeft(0).BorderColor("#000000")
                            .Height(18).Padding(1).AlignLeft().AlignMiddle()
                            .Text("Vypnuté brzdy").FontSize(6);

                        // Row 2: brake-mode sub-column headers
                        string[] topLbls = { "F", "G",        "H", "I", "J", "K",    "L", "M" };
                        string[] botLbls = { "D", "K, L, LL", "G", "P", "R", "R+Mg", "L", "M" };
                        for (uint ci = 0; ci < 8; ci++)
                        {
                            r11.Cell().Row(2).Column(ci + 2)
                                .Border(1).BorderTop(0).BorderLeft(ci == 0 ? 1 : 0)
                                .BorderColor("#000000").Height(14).PaddingLeft(1).PaddingRight(1).AlignCenter().AlignMiddle()
                                .Column(c =>
                                {
                                    c.Item().Text(topLbls[ci]).FontSize(5.5f).Bold();
                                    c.Item().Text(botLbls[ci]).FontSize(4.5f).FontColor("#666666");
                                });
                        }

                        // Rows 3–5: data rows 11.1–11.3 (only row 11.1 gets values)
                        string[] dataVals = new string[9];
                        dataVals[4] = pCount        > 0 ? pCount.ToString()        : string.Empty;
                        dataVals[5] = rCount        > 0 ? rCount.ToString()        : string.Empty;
                        dataVals[7] = securingCount > 0 ? securingCount.ToString() : string.Empty;
                        dataVals[8] = disabledCount > 0 ? disabledCount.ToString() : string.Empty;

                        for (uint ri = 0; ri < 3; ri++)
                        {
                            uint tr = ri + 3;
                            for (uint ci = 0; ci < 9; ci++)
                            {
                                string txt = ci == 0
                                    ? $"11.{ri + 1}"
                                    : (ri == 0 ? dataVals[ci] : string.Empty);

                                r11.Cell().Row(tr).Column(ci + 1)
                                    .Border(1).BorderTop(0).BorderLeft(ci == 0 ? 1 : 0)
                                    .BorderColor("#000000").Height(11).Padding(1)
                                    .AlignCenter().AlignMiddle()
                                    .Text(txt)
                                    .FontSize(ci == 0 ? 5.5f : 7f)
                                    .FontColor(ci == 0 ? "#555555" : "#000000");
                            }
                        }
                    });

                    // ── Row 12: Braking percentages — single consolidated table ─
                    col.Item().Table(r12 =>
                    {
                        r12.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(18);
                            c.RelativeColumn(7);
                            c.RelativeColumn(10);
                            c.RelativeColumn(14);
                            c.RelativeColumn(14);
                            c.RelativeColumn(25);
                            c.RelativeColumn(12);
                        });

                        // Row 1: column headers
                        string[] r12Lbls = {
                            "Výchozí/Nácestná\nstanice", "Režim\nbrzdění", "Potřebná\nbrzdící %",
                            "Skutečné brzdící %\ns/bez EDB", "Chybějící brzdící %\ns/bez EDB",
                            "Strojvedoucí – podpis", "Číslo vedoucího\nhnacího vozidla"
                        };
                        string[] r12Letters = { "N", "O", "P", "R", "S", "T", "U" };
                        for (uint ci = 0; ci < 7; ci++)
                        {
                            r12.Cell().Row(1).Column(ci + 1)
                                .Border(1).BorderTop(0).BorderLeft(ci == 0 ? 1 : 0)
                                .BorderColor("#000000").Padding(1).AlignCenter()
                                .Column(x =>
                                {
                                    x.Item().Text(r12Lbls[ci]).FontSize(5f).Italic().FontColor("#444444");
                                    x.Item().Text(r12Letters[ci]).FontSize(8).Bold();
                                });
                        }

                        // Rows 2–4: data rows 12.1–12.3
                        void D12(uint row, uint col2, string v, bool bold = false)
                        {
                            var cell = r12.Cell().Row(row).Column(col2)
                                .Border(1).BorderTop(0).BorderLeft(col2 == 1 ? 1 : 0)
                                .BorderColor("#000000").Height(16)
                                .PaddingLeft(1).PaddingRight(3).AlignCenter().AlignMiddle();
                            if (bold) cell.Text(v).FontSize(8).Bold();
                            else      cell.Text(v).FontSize(7);
                        }

                        for (uint ri = 0; ri < 3; ri++)
                        {
                            uint tr = ri + 2;

                            // Col 1: sub-row label + optional station name
                            r12.Cell().Row(tr).Column(1)
                                .Border(1).BorderTop(0).BorderLeft(1).BorderColor("#000000")
                                .Height(16).PaddingLeft(1).PaddingRight(1)
                                .Column(c =>
                                {
                                    c.Item().Text($"12.{ri + 1}").FontSize(5.5f).FontColor("#555555");
                                    if (ri == 0)
                                        c.Item().Text(startStation ?? string.Empty).FontSize(7);
                                });

                            if (ri == 0)
                            {
                                D12(tr, 2, bMode,                                          bold: true);
                                D12(tr, 3, reqPct.HasValue ? $"{reqPct:F0}" : string.Empty);
                                D12(tr, 4, actualStr,                                      bold: true);
                                D12(tr, 5, missingStr);
                                D12(tr, 6, string.Empty);
                                D12(tr, 7, leadNum);
                            }
                            else
                            {
                                D12(tr, 2, string.Empty); D12(tr, 3, string.Empty);
                                D12(tr, 4, "/");          D12(tr, 5, "/");
                                D12(tr, 6, string.Empty); D12(tr, 7, string.Empty);
                            }
                        }
                    });

                    // ── Row 13: Potvrzení o zkoušce brzdy ─────────────────────
                    col.Item()
                        .Border(1).BorderTop(0).BorderColor("#000000").Padding(3)
                        .Column(c =>
                        {
                            c.Item().Text("13  Potvrzení o zkoušce brzdy").FontSize(7).Bold();
                            c.Item().PaddingTop(2).Row(r =>
                            {
                                r.AutoItem().Text("Konec:").FontSize(6.5f);
                                r.ConstantItem(42).BorderBottom(0.5f).BorderColor("#999999");
                                r.AutoItem().PaddingLeft(4).Text("Hod.").FontSize(6.5f);
                                r.ConstantItem(28).BorderBottom(0.5f).BorderColor("#999999");
                                r.AutoItem().PaddingLeft(4).Text("Min.").FontSize(6.5f);
                                r.ConstantItem(22).BorderBottom(0.5f).BorderColor("#999999");
                                r.AutoItem().PaddingLeft(5).Text("Datum:").FontSize(6.5f);
                                r.ConstantItem(60).BorderBottom(0.5f).BorderColor("#999999");
                                r.AutoItem().PaddingLeft(5).Text("Podpis:").FontSize(6.5f);
                                r.RelativeItem().BorderBottom(0.5f).BorderColor("#999999");
                            });
                        });

                    // ── Rows 14 & 15 ──────────────────────────────────────────
                    col.Item().Table(r1415 =>
                    {
                        r1415.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        r1415.Cell().Row(1).Column(1)
                            .Border(1).BorderTop(0).BorderColor("#000000").Padding(3)
                            .Column(c =>
                            {
                                c.Item().Text("14  Číslo posledního vozidla ve vlaku")
                                    .FontSize(6.5f).FontColor("#555555");
                                c.Item().Height(10);
                                c.Item().Text($"{lastNum}").FontSize(6.5f);
                            });

                        r1415.Cell().Row(1).Column(2)
                            .Border(1).BorderTop(0).BorderLeft(0).BorderColor("#000000").Padding(3)
                            .Column(c =>
                            {
                                c.Item().Text("15  Nejvyšší rychlost soupravy [km/h]")
                                    .FontSize(6.5f).FontColor("#555555");
                                c.Item().Text($"{maxSpeed}").FontSize(11).Bold();
                            });
                    });

                    // ── Rows 16 & 17 (pre-checked Ne) ─────────────────────────
                    col.Item().Table(r1617 =>
                    {
                        r1617.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        void CheckCell(uint c, string rowNum, string label)
                        {
                            r1617.Cell().Row(1).Column(c)
                                .Border(1).BorderTop(0).BorderLeft(c == 1 ? 1 : 0)
                                .BorderColor("#000000").Padding(3)
                                .Column(x =>
                                {
                                    x.Item().Text($"{rowNum}  {label}").FontSize(6.5f).Bold();
                                    x.Item().PaddingTop(2).Row(r =>
                                    {
                                        r.AutoItem().Text("Ano [ ]").FontSize(7);
                                        r.AutoItem().PaddingLeft(6).Text("Ne [X]").FontSize(7).Bold();
                                    });
                                    x.Item().PaddingTop(1)
                                        .Text("Podrobnosti viz výkaz vozidel")
                                        .FontSize(6).Italic().FontColor("#666666");
                                });
                        }

                        CheckCell(1, "16", "Nebezpečné věci na vlaku (RID)");
                        CheckCell(2, "17", "MZ na vlaku");
                    });
                }); 
            });

            // ── Page 2: ETCS summary — reuses PdfReportService layout ──────────
            // Shared values needed for the ETCS box
            var p2Total    = entries.Sum(e => e.TotalWeightTonnes);
            var p2Len      = entries.Sum(e => e.LengthM);
            var p2Ab       = entries.Where(e => e.BrakesEnabled).Sum(e => BrakingCalculator.ActiveBrake(e));
            var p2Pct      = p2Total > 0 ? Math.Floor(p2Ab / p2Total * 100.0) : 0;
            var p2Fp       = BrakingCalculator.ConsistFpClass(entries);
            var p2FpMm     = p2Fp == "FP3" ? 130 : 100;
            var p2AxleLoad = BrakingCalculator.ConsistAxleLoad(entries) ?? "—";
            var p2LowBrake = p2Pct < 50;
            var th         = PdfThemeService.Get(false);
            string Tcs(string key) => LocalizationService.GetString(key, cs: true);

            container.Page(page2 =>
            {
                page2.Size(PageSizes.A4);
                page2.Margin(15, Unit.Millimetre);
                page2.DefaultTextStyle(t =>
                    t.FontSize(10).FontFamily("Arial").FontColor(th.TxtMain));

                // Header
                page2.Header().Column(hdrCol =>
                {
                    hdrCol.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(consistName).FontSize(16).Bold().FontColor(th.TxtMain);
                            c.Item().Text("ETCS – parametry vlaku").FontSize(10).FontColor(th.TxtSub);
                        });
                        row.ConstantItem(160).AlignRight().Column(c =>
                            c.Item().Text(now.ToString("dd. MM. yyyy HH:mm"))
                             .FontSize(10).FontColor(th.TxtMeta));
                    });
                    hdrCol.Item().PaddingTop(6).LineHorizontal(2).LineColor(th.Orange);
                });

                page2.Content().PaddingTop(12).Column(col =>
                {
                    // ETCS dark box
                    col.Item()
                       .Background(th.BgEtcs)
                       .Padding(14)
                       .Column(etcs =>
                       {
                           etcs.Item().Text(Tcs("PdfEtcsParams"))
                               .FontSize(9).Bold().FontColor(th.TxtEtcs);
                           etcs.Item().PaddingTop(8).Table(t =>
                           {
                               t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                               void ERow(string label, string value, string color = "#ffffff")
                               {
                                   t.Cell().PaddingVertical(4).Text(label).FontColor(th.TxtEtcs).FontSize(10);
                                   t.Cell().PaddingVertical(4).Text(value).Bold().FontColor(color).FontSize(10);
                               }
                               ERow(Tcs("CrossSection"),    Tcs("PdfCrossSectionValue"), th.Orange);
                               ERow(Tcs("CantDeficiency"),  $"{p2Fp}  ({p2FpMm} mm)");
                               ERow(Tcs("EtcsMaxSpeed"),    $"{maxSpeed} km/h", th.Orange);
                               ERow(Tcs("BrakingPctLabel"), $"{p2Pct:F0} %", p2LowBrake ? th.Red : th.Green);
                               ERow(Tcs("TrainLength"),     $"{p2Len:F0} m");
                               ERow(Tcs("AxleLoad"),        p2AxleLoad, th.Orange);
                           });
                       });
                    col.Item().Height(12);

                    // Consist table
                    col.Item().Column(tbl =>
                    {
                        tbl.Item().Text(Tcs("PdfConsistComp"))
                           .FontSize(9).Bold().FontColor(th.TxtSub);
                        tbl.Item().PaddingTop(4).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            void Hdr(string txt) =>
                                t.Cell().Background(th.BgTableHdr).Padding(6)
                                 .Text(txt).Bold().FontColor("#ffffff").FontSize(9);
                            Hdr(Tcs("PdfColSeries"));
                            Hdr(Tcs("PdfColWeight"));
                            Hdr(Tcs("PdfColBrakeWt"));
                            Hdr(Tcs("PdfColBrakeWtEDB"));
                            Hdr(Tcs("PdfColLength"));
                            Hdr(Tcs("PdfColBrakes"));

                            double abBase2 = 0;
                            for (int i = 0; i < entries.Count; i++)
                            {
                                var e      = entries[i];
                                var bg     = i % 2 == 0 ? th.BgRowEven : th.BgRowOdd;
                                var bwBase = e.BrakesEnabled
                                    ? (e.RModeActive && e.BrakingWeightTonnesR.HasValue
                                        ? e.BrakingWeightTonnesR!.Value
                                        : e.BrakingWeightTonnes)
                                    : 0;
                                abBase2 += bwBase;
                                var bwEdbStr = !e.BrakesEnabled ? "—"
                                    : e.RModeActive && e.BrakingWeightWithEDBR.HasValue
                                        ? $"{e.BrakingWeightWithEDBR!.Value:F0} t"
                                    : !e.RModeActive && e.BrakingWeightWithEDB.HasValue
                                        ? $"{e.BrakingWeightWithEDB!.Value:F0} t"
                                    : "—";
                                var brakesTxt = e.BrakesEnabled
                                    ? (e.EdbActive ? (e.RModeActive ? "R+E" : "P+E") : (e.RModeActive ? "R" : "P"))
                                    : "x";

                                t.Cell().Background(bg).Padding(5)
                                 .Text(e.CustomName ?? e.Designation).FontColor(th.TxtCell).FontSize(10);
                                t.Cell().Background(bg).Padding(5).AlignCenter()
                                 .Text($"{e.TotalWeightTonnes:F1} t").FontColor(th.TxtCell).FontSize(10);
                                t.Cell().Background(bg).Padding(5).AlignCenter()
                                 .Text($"{bwBase:F0} t")
                                 .FontColor(e.BrakesEnabled ? th.TxtCell : th.Red).FontSize(10);
                                t.Cell().Background(bg).Padding(5).AlignCenter()
                                 .Text(bwEdbStr)
                                 .FontColor(bwEdbStr == "—" ? th.TxtMeta : th.TxtCell).FontSize(10);
                                t.Cell().Background(bg).Padding(5).AlignCenter()
                                 .Text($"{e.LengthM:F0} m").FontColor(th.TxtCell).FontSize(10);
                                t.Cell().Background(bg).Padding(5).AlignCenter()
                                 .Text(brakesTxt)
                                 .FontColor(e.BrakesEnabled ? th.Green : th.Red).FontSize(10);
                            }

                            void Tot(string txt) =>
                                t.Cell().Background(th.BgTotals).Padding(5)
                                 .Text(txt).Bold().FontColor(th.TxtCell).FontSize(10);
                            Tot(Tcs("PdfTotal"));
                            Tot($"{p2Total:F1} t");
                            Tot($"{abBase2:F0} t");
                            Tot($"{p2Ab:F0} t");
                            Tot($"{p2Len:F0} m");
                            Tot("—");
                        });
                    });

                    col.Item().PaddingTop(8)
                       .Text(Tcs("PdfFpLegend"))
                       .FontSize(8).FontColor(th.TxtMeta);
                });

                page2.Footer().AlignCenter()
                    .Text($"LocoCalc  ·  ČD Cargo MZOB  ·  {now:dd. MM. yyyy HH:mm}")
                    .FontSize(9).FontColor(th.FooterText);
            });
        });

        return doc.GeneratePdf();
    }
}
