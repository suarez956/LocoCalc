using LocoCalcAvalonia.Models;

namespace LocoCalcAvalonia.Services;

/// <summary>
/// Generates PDF bytes from consist data.
/// Desktop: QuestPDF (PdfReportService).
/// Android: native Android.Graphics.Pdf.PdfDocument.
/// </summary>
public interface IPdfGenerator
{
    byte[] Generate(
        IReadOnlyList<ConsistEntry> entries,
        string consistName,
        int maxSpeed,
        bool isCs,
        bool darkMode = false,
        string? startStation = null,
        string? endStation = null);
}
