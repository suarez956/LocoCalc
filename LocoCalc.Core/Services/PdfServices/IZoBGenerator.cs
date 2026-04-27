using LocoCalc.Models;

namespace LocoCalc.Services.PdfServices;

public interface IZoBGenerator
{
    byte[] Generate(
        IReadOnlyList<ConsistEntry> entries,
        string consistName,
        int maxSpeed,
        string? requiredBrakingPct,
        string? startStation,
        string? endStation);
}
