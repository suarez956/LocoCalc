using LocoCalc.Models;

namespace LocoCalc.Services;

public static class BrakingCalculator
{
    public static double Calculate(IEnumerable<ConsistEntry> entries)
    {
        var list = entries.ToList();
        if (list.Count == 0) return 0;
        double total = list.Sum(e => e.TotalWeightTonnes);
        if (total == 0) return 0;
        double active = list
            .Where(e => e.BrakesEnabled)
            .Sum(e => ActiveBrake(e));
        
        return Math.Floor(active / total * 100.0);
    }

    public static double ActiveBrake(ConsistEntry e)
    {
        if (e.RModeActive && e.BrakingWeightTonnesR.HasValue)
            return (e.EdbActive && e.BrakingWeightWithEDBR.HasValue)
                ? e.BrakingWeightWithEDBR!.Value
                : e.BrakingWeightTonnesR!.Value;
        return (e.EdbActive && e.BrakingWeightWithEDB.HasValue)
            ? e.BrakingWeightWithEDB!.Value
            : e.BrakingWeightTonnes;
    }

    public static ConsistPosition DerivePosition(int index, int total)
    {
        if (total == 1) return ConsistPosition.Front;
        if (index == 0) return ConsistPosition.Front;
        if (index == total - 1) return ConsistPosition.Rear;
        return ConsistPosition.Middle;
    }

    /// Middle = transported dead loco → brakes OFF by default
    public static bool DefaultBrakesEnabled(ConsistPosition pos) => pos != ConsistPosition.Middle;

    public static int ConsistMaxSpeed(IEnumerable<ConsistEntry> entries)
    {
        var list = entries.ToList();
        return list.Count == 0 ? 0 : list.Min(e => e.MaxSpeed);
    }

    /// FP3 only when ALL locos are FP3
    public static string ConsistFpClass(IEnumerable<ConsistEntry> entries)
    {
        var list = entries.ToList();
        return list.Count > 0 && list.All(e => e.FpClass == "FP3") ? "FP3" : "FP2";
    }

    // Order from lowest to highest track class per the line-class table
    private static readonly string[] AxleLoadOrder = { "A", "B1", "B2", "C2", "C3", "C4", "D2", "D3", "D4" };

    /// <summary>
    /// Returns the minimum (most restrictive) track line class across all entries.
    /// Returns null when no entry has an axle load set.
    /// </summary>
    public static string? ConsistAxleLoad(IEnumerable<ConsistEntry> entries)
    {
        var values = entries
            .Select(e => e.AxleLoad)
            .Where(a => a is not null && Array.IndexOf(AxleLoadOrder, a) >= 0)
            .ToList();
        if (values.Count == 0) return null;
        return values.OrderBy(a => Array.IndexOf(AxleLoadOrder, a)).Last();
    }
}
