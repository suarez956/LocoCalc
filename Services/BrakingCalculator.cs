using LocoCalc.Models;

namespace LocoCalc.Services;

public static class BrakingCalculator
{
    /// <summary>
    /// Returns braking %: sum of active braking weights / sum of all total weights x 100.
    /// Each entry's braking weight already reflects EDB state via ConsistEntry.BrakingWeightTonnes.
    /// </summary>
    public static double Calculate(IEnumerable<ConsistEntry> entries)
    {
        var list = entries.ToList();
        if (list.Count == 0) return 0;

        double totalWeight = list.Sum(e => e.TotalWeightTonnes);
        if (totalWeight == 0) return 0;

        double activeBrakingWeight = list
            .Where(e => e.BrakesEnabled)
            .Sum(e => e.HasEDB && e.EdbActive ? e.BrakingWeightWithEDB!.Value : e.BrakingWeightTonnes);

        return activeBrakingWeight / totalWeight * 100.0;
    }

    public static ConsistPosition DerivePosition(int index, int total)
    {
        if (total == 1) return ConsistPosition.Front;
        if (index == 0) return ConsistPosition.Front;
        if (index == total - 1) return ConsistPosition.Rear;
        return ConsistPosition.Middle;
    }

    public static bool DefaultBrakesEnabled(ConsistPosition position) => position switch
    {
        ConsistPosition.Rear   => true,
        ConsistPosition.Middle => false,
        ConsistPosition.Front  => true,
        _                      => true
    };
}
