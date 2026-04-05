using LocoCalcAvalonia.Models;
using LocoCalcAvalonia.Services;

namespace LocoCalcAvalonia.Services;

/// <summary>
/// Wraps PdfReportService in a Lazy so QuestPDF (and its SkiaSharp heap)
/// are not initialized until the first PDF is actually generated.
/// </summary>
internal sealed class LazyPdfGenerator : IPdfGenerator
{
    private static readonly Lazy<PdfReportService> _inner = new(() => new PdfReportService());

    public byte[] Generate(
        IReadOnlyList<ConsistEntry> entries, string consistName,
        int maxSpeed, bool isCs, bool darkMode,
        string? startStation, string? endStation)
        => ((IPdfGenerator)_inner.Value).Generate(
            entries, consistName, maxSpeed, isCs, darkMode, startStation, endStation);
}
