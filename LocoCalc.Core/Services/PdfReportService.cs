using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LocoCalcAvalonia.Models;

namespace LocoCalcAvalonia.Services;

public class PdfReportService : IPdfGenerator
{
    static PdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // IPdfGenerator instance method
    byte[] IPdfGenerator.Generate(
        IReadOnlyList<ConsistEntry> entries, string consistName,
        int maxSpeed, bool isCs, bool darkMode)
        => Generate(entries, consistName, maxSpeed, isCs, darkMode);

    public static byte[] Generate(
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

        // ── Theme colours ──────────────────────────────────────────────────
        var bgPage     = darkMode ? "#0f0f1a" : "#ffffff";
        var bgCard     = darkMode ? "#1e1e32" : "#f8f8f8";
        var bgEtcs     = darkMode ? "#161626" : "#1a1a2e";
        var bgTableHdr = "#1a1a2e";  // always dark header
        var bgRowEven  = "#ffffff";   // always white rows for readability
        var bgRowOdd   = "#f9f9f9";
        var bgTotals   = "#f0f0f0";
        var txtMain    = darkMode ? "#e8e8ff" : "#1a1a2e";
        var txtSub     = darkMode ? "#8888aa" : "#666666";
        var txtMeta    = darkMode ? "#8888aa" : "#888888";
        var txtCell    = "#111111";  // always dark — table background is always white

        // Card helper: single container chain — BorderLeft > Background > Padding > Column
        void Card(ColumnDescriptor col, string label, string value, string valueColor)
        {
            col.Item()
               .BorderLeft(3).BorderColor("#f97316")
               .Background(bgCard)
               .Padding(10)
               .Column(c =>
               {
                   c.Item().Text(label).FontSize(8).Bold().FontColor("#888888");
                   c.Item().Text(value).FontSize(20).Bold().FontColor(valueColor);
               });
        }

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15, Unit.Millimetre);
                page.Background(bgPage);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Arial").FontColor(txtMain));

                // ── Header ─────────────────────────────────────────────────
                // Must be a single child: wrap Row + orange line in one Column
                page.Header().Column(hdrCol =>
                {
                    hdrCol.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("LocoCalc").FontSize(22).Bold().FontColor("#f97316");
                            c.Item().Text(consistName).FontSize(16).Bold().FontColor(txtMain);
                            c.Item().Text(T("Zpráva o brzdění", "Braking Report"))
                             .FontSize(10).FontColor(txtSub);
                        });
                        row.ConstantItem(160).AlignRight().Column(c =>
                        {
                            c.Item().Text(date).FontSize(10).FontColor("#888888");
                        });
                    });
                    // Orange divider line — second item in column, NOT second child of header
                    hdrCol.Item().PaddingTop(6).LineHorizontal(2).LineColor("#f97316");
                });

                // ── Content ────────────────────────────────────────────────
                page.Content().PaddingTop(12).Column(col =>
                {
                    // Warning
                    if (lowBrake)
                    {
                        col.Item()
                           .Border(1).BorderColor("#e74c3c")
                           .Background(darkMode ? "#3f0f0f" : "#fdecea")
                           .Padding(10)
                           .Column(w =>
                           {
                               w.Item().Text(T("⚠ VAROVÁNÍ — nedostatečná brzdící procenta",
                                               "⚠ WARNING — insufficient braking percent"))
                                .Bold().FontColor("#922b21");
                               w.Item().PaddingTop(4)
                                .Text(T($"Brzdící procenta {pct:F0}% jsou pod minimem 50%.\n🚫 Vlak nesmí jet na traťové úseky vybavené pouze ETCS!",
                                        $"Braking percentage {pct:F0}% is below the 50% minimum.\n🚫 Train must not proceed onto ETCS-only track sections!"))
                                .FontSize(10).FontColor("#922b21");
                           });
                        col.Item().Height(8);
                    }

                    // Summary cards — row 1
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c => Card(c,
                            T("BRZDÍCÍ %", "BRAKING %"), $"{pct:F0} %",
                            lowBrake ? "#c0392b" : pct < 65 ? "#f97316" : "#27ae60"));
                        row.ConstantItem(8);
                        row.RelativeItem().Column(c => Card(c,
                            T("MAX. RYCHLOST", "MAX SPEED"), $"{maxSpeed} km/h", "#f97316"));
                    });
                    col.Item().Height(6);

                    // Summary cards — row 2
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c => Card(c,
                            T("DÉLKA SOUPRAVY", "CONSIST LENGTH"), $"{len:F0} m", "#1a1a2e"));
                        row.ConstantItem(8);
                        row.RelativeItem().Column(c => Card(c,
                            T("CELK. HMOTNOST", "TOTAL WEIGHT"), $"{total:F1} t", "#1a1a2e"));
                    });
                    col.Item().Height(10);

                    // ETCS dark box
                    col.Item()
                       .Background(bgEtcs)
                       .Padding(14)
                       .Column(etcs =>
                       {
                           etcs.Item().Text(T("ETCS PARAMETRY", "ETCS PARAMETERS"))
                               .FontSize(9).Bold().FontColor("#aaaaaa");
                           etcs.Item().PaddingTop(8).Table(t =>
                           {
                               t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                               void ERow(string label, string value, string color = "#ffffff")
                               {
                                   t.Cell().PaddingVertical(4).Text(label).FontColor("#aaaaaa").FontSize(10);
                                   t.Cell().PaddingVertical(4).Text(value).Bold().FontColor(color).FontSize(10);
                               }
                               ERow(T("Průjezdný průřez", "Cross-section"), "GC", "#f97316");
                               ERow(T("Nedostatek převýšení", "Cant deficiency"), $"{fp}  ({fpMm} mm)");
                               ERow(T("Max. rychlost ETCS", "ETCS max speed"), $"{maxSpeed} km/h", "#f97316");
                               ERow(T("Brzdící procenta", "Braking percentage"), $"{pct:F0} %",
                                    lowBrake ? "#e74c3c" : "#2ecc71");
                               ERow(T("Délka vlaku", "Train length"), $"{len:F0} m");
                           });
                       });
                    col.Item().Height(12);

                    // Consist table
                    col.Item().Column(tbl =>
                    {
                        tbl.Item().Text(T("SLOŽENÍ SOUPRAVY", "CONSIST COMPOSITION"))
                           .FontSize(9).Bold().FontColor(txtSub);
                        tbl.Item().PaddingTop(4).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            void Hdr(string txt) =>
                                t.Cell().Background("#1a1a2e").Padding(6)
                                 .Text(txt).Bold().FontColor("#ffffff").FontSize(9);
                            Hdr(T("Řada", "Series"));
                            Hdr(T("Hmotnost", "Weight"));
                            Hdr(T("Brzd. váha", "Brake wt"));
                            Hdr(T("Délka", "Length"));
                            Hdr(T("Brzdy", "Brakes"));

                            for (int i = 0; i < entries.Count; i++)
                            {
                                var e        = entries[i];
                                var bg       = i % 2 == 0 ? "#ffffff" : "#f9f9f9";
                                var bw       = e.BrakesEnabled ? BrakingCalculator.ActiveBrake(e) : 0;
                                var edbNote  = e.BrakesEnabled && e.EdbActive ? " (EDB)" : "";
                                var bwColor  = e.BrakesEnabled ? "#111111" : "#c0392b";
                                var brakesTxt = e.BrakesEnabled ? (e.EdbActive ? "P+E" : "P") : "x";

                                t.Cell().Background(bg).Padding(5).Text(e.Designation).FontSize(10);
                                t.Cell().Background(bg).Padding(5).AlignCenter()
                                 .Text($"{e.TotalWeightTonnes:F1} t").FontSize(10);
                                t.Cell().Background(bg).Padding(5).AlignCenter()
                                 .Text($"{bw:F0} t{edbNote}").FontColor(bwColor).FontSize(10);
                                t.Cell().Background(bg).Padding(5).AlignCenter()
                                 .Text($"{e.LengthM:F0} m").FontSize(10);
                                t.Cell().Background(bg).Padding(5).AlignCenter()
                                 .Text(brakesTxt)
                                 .FontColor(e.BrakesEnabled ? "#27ae60" : "#c0392b").FontSize(10);
                            }

                            void Tot(string txt) =>
                                t.Cell().Background(bgTotals).Padding(5)
                                 .Text(txt).Bold().FontColor(txtMain).FontSize(10);
                            Tot(T("Celkem", "Total"));
                            Tot($"{total:F1} t");
                            Tot($"{ab:F0} t");
                            Tot($"{len:F0} m");
                            Tot("—");
                        });
                    });

                    col.Item().PaddingTop(8)
                       .Text("FP3 (130 mm): 163, 186, 189, 363, 363.5, 372, 383, 386, 388, 393  |  FP2 (100 mm): ostatní")
                       .FontSize(8).FontColor(txtMeta);
                });

                // ── Footer ─────────────────────────────────────────────────
                page.Footer().AlignCenter()
                    .Text($"LocoCalc  ·  {date}")
                    .FontSize(9).FontColor("#999999");
            });
        });

        return doc.GeneratePdf();
    }
}