using LocoCalcAvalonia.Models;

namespace LocoCalcAvalonia.Services;

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
