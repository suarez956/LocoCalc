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
        int maxSpeed, bool isCs, bool darkMode,
        string? startStation, string? endStation)
        => Generate(entries, consistName, maxSpeed, isCs, darkMode, startStation, endStation);

    public static byte[] Generate(
        IReadOnlyList<ConsistEntry> entries,
        string consistName,
        int maxSpeed,
        bool isCs,
        bool darkMode = false,
        string? startStation = null,
        string? endStation = null)
    {
        var total    = entries.Sum(e => e.TotalWeightTonnes);
        var len      = entries.Sum(e => e.LengthM);
        var ab       = entries.Where(e => e.BrakesEnabled).Sum(e => BrakingCalculator.ActiveBrake(e));
        var pct      = total > 0 ? Math.Floor(ab / total * 100.0) : 0;
        var fp       = BrakingCalculator.ConsistFpClass(entries);
        var fpMm     = fp == "FP3" ? 130 : 100;
        var date     = DateTime.Now.ToString(isCs ? "dd. MM. yyyy HH:mm" : "dd MMM yyyy HH:mm");
        var lowBrake = pct < 50;

        string T(string key) => LocalizationService.GetString(key, isCs);

        // ── Theme colours ──────────────────────────────────────────────────
        var th         = PdfThemeService.Get(darkMode);

        // Card helper: single container chain — BorderLeft > Background > Padding > Column
        void Card(ColumnDescriptor col, string label, string value, string valueColor)
        {
            col.Item()
               .BorderLeft(3).BorderColor(th.Orange)
               .Background(th.BgCard)
               .Padding(10)
               .Column(c =>
               {
                   c.Item().Text(label).FontSize(8).Bold().FontColor(th.TxtMeta);
                   c.Item().Text(value).FontSize(20).Bold().FontColor(valueColor);
               });
        }

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15, Unit.Millimetre);
                page.PageColor(th.BgPage);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Arial").FontColor(th.TxtMain));

                // ── Header ─────────────────────────────────────────────────
                page.Header().Column(hdrCol =>
                {
                    hdrCol.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("LocoCalc").FontSize(22).Bold().FontColor(th.Orange);
                            c.Item().Text(consistName).FontSize(16).Bold().FontColor(th.TxtMain);
                            c.Item().Text(T("PdfBrakingReport"))
                             .FontSize(10).FontColor(th.TxtSub);
                            if (startStation is not null || endStation is not null)
                            {
                                var route = $"{startStation ?? "?"} → {endStation ?? "?"}";
                                c.Item().PaddingTop(3)
                                 .Text($"{T("PdfRoute")}: {route}")
                                 .FontSize(10).FontColor(th.TxtMeta);
                            }
                        });
                        row.ConstantItem(160).AlignRight().Column(c =>
                        {
                            c.Item().Text(date).FontSize(10).FontColor(th.TxtMeta);
                        });
                    });
                    hdrCol.Item().PaddingTop(6).LineHorizontal(2).LineColor(th.Orange);
                });

                // ── Content ────────────────────────────────────────────────
                page.Content().PaddingTop(12).Column(col =>
                {
                    // Warning
                    if (lowBrake)
                    {
                        col.Item()
                           .Border(1).BorderColor(th.WarnBorder)
                           .Background(th.BgWarn)
                           .Padding(10)
                           .Column(w =>
                           {
                               w.Item().Text(T("PdfWarnTitle"))
                                .Bold().FontColor(th.TxtWarn);
                               w.Item().PaddingTop(4)
                                .Text(string.Format(T("PdfWarnBody"), pct))
                                .FontSize(10).FontColor(th.TxtWarn);
                           });
                        col.Item().Height(8);
                    }

                    // Summary cards — row 1
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c => Card(c,
                            T("PdfBrakingPctLabel"), $"{pct:F0} %",
                            lowBrake ? th.Red : pct < 65 ? th.Orange : th.Green));
                        row.ConstantItem(8);
                        row.RelativeItem().Column(c => Card(c,
                            T("PdfMaxSpeedLabel"), $"{maxSpeed} km/h", th.Orange));
                    });
                    col.Item().Height(6);

                    // Summary cards — row 2
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c => Card(c,
                            T("PdfLengthLabel"), $"{len:F0} m", th.TxtMain));
                        row.ConstantItem(8);
                        row.RelativeItem().Column(c => Card(c,
                            T("PdfWeightLabel"), $"{total:F1} t", th.TxtMain));
                    });
                    col.Item().Height(10);

                    // ETCS dark box
                    col.Item()
                       .Background(th.BgEtcs)
                       .Padding(14)
                       .Column(etcs =>
                       {
                           etcs.Item().Text(T("PdfEtcsParams"))
                               .FontSize(9).Bold().FontColor(th.TxtEtcs);
                           etcs.Item().PaddingTop(8).Table(t =>
                           {
                               t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                               void ERow(string label, string value, string color = "#ffffff")
                               {
                                   t.Cell().PaddingVertical(4).Text(label).FontColor(th.TxtEtcs).FontSize(10);
                                   t.Cell().PaddingVertical(4).Text(value).Bold().FontColor(color).FontSize(10);
                               }
                               ERow(T("CrossSection"), T("PdfCrossSectionValue"), th.Orange);
                               ERow(T("CantDeficiency"), $"{fp}  ({fpMm} mm)");
                               ERow(T("EtcsMaxSpeed"), $"{maxSpeed} km/h", th.Orange);
                               ERow(T("BrakingPctLabel"), $"{pct:F0} %",
                                    lowBrake ? th.Red : th.Green);
                               ERow(T("TrainLength"), $"{len:F0} m");
                           });
                       });
                    col.Item().Height(12);

                    // Consist table
                    col.Item().Column(tbl =>
                    {
                        tbl.Item().Text(T("PdfConsistComp"))
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
                            });

                            void Hdr(string txt) =>
                                t.Cell().Background(th.BgTableHdr).Padding(6)
                                 .Text(txt).Bold().FontColor("#ffffff").FontSize(9);
                            Hdr(T("PdfColSeries"));
                            Hdr(T("PdfColWeight"));
                            Hdr(T("PdfColBrakeWt"));
                            Hdr(T("PdfColLength"));
                            Hdr(T("PdfColBrakes"));

                            for (int i = 0; i < entries.Count; i++)
                            {
                                var e        = entries[i];
                                var bg       = i % 2 == 0 ? th.BgRowEven : th.BgRowOdd;
                                var bw       = e.BrakesEnabled ? BrakingCalculator.ActiveBrake(e) : 0;
                                var edbNote  = e.BrakesEnabled && e.EdbActive ? " (EDB)" : "";
                                var bwColor  = e.BrakesEnabled ? th.TxtCell : th.Red;
                                var brakesTxt = e.BrakesEnabled ? (e.EdbActive ? "P+E" : "P") : "x";

                                t.Cell().Background(bg).Padding(5).Text(e.CustomName ?? e.Designation).FontColor(th.TxtCell).FontSize(10);
                                t.Cell().Background(bg).Padding(5).AlignCenter()
                                 .Text($"{e.TotalWeightTonnes:F1} t").FontColor(th.TxtCell).FontSize(10);
                                t.Cell().Background(bg).Padding(5).AlignCenter()
                                 .Text($"{bw:F0} t{edbNote}").FontColor(bwColor).FontSize(10);
                                t.Cell().Background(bg).Padding(5).AlignCenter()
                                 .Text($"{e.LengthM:F0} m").FontColor(th.TxtCell).FontSize(10);
                                t.Cell().Background(bg).Padding(5).AlignCenter()
                                 .Text(brakesTxt)
                                 .FontColor(e.BrakesEnabled ? th.Green : th.Red).FontSize(10);
                            }

                            void Tot(string txt) =>
                                t.Cell().Background(th.BgTotals).Padding(5)
                                 .Text(txt).Bold().FontColor(th.TxtCell).FontSize(10);
                            Tot(T("PdfTotal"));
                            Tot($"{total:F1} t");
                            Tot($"{ab:F0} t");
                            Tot($"{len:F0} m");
                            Tot("—");
                        });
                    });

                    col.Item().PaddingTop(8)
                       .Text(T("PdfFpLegend"))
                       .FontSize(8).FontColor(th.TxtMeta);
                });

                // ── Footer ─────────────────────────────────────────────────
                page.Footer().AlignCenter()
                    .Text($"LocoCalc  ·  {date}")
                    .FontSize(9).FontColor(th.FooterText);
            });
        });

        return doc.GeneratePdf();
    }
}
