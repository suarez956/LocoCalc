using LocoCalcAvalonia.Models;

namespace LocoCalcAvalonia.Services;

/// <summary>
/// Implements the traction weight ratio (poměrové číslo) weight-distribution logic from
/// ČD Cargo KP Doplněk (platný od 01.04.2026), §6.2.
///
/// Table selection:
///   • Any diesel in consist  → use TWR "30" table
///   • All electric           → use TWR "50" table
///
/// Formula (§6.2):
///   DH_per_unit    = GrossTrainWeight / Σ(TWR of every active loco)
///   Loco share (t) = DH_per_unit × its TWR
/// </summary>
public static class TractionCalculator
{
    public record LocoShare(
        string Designation,
        int    Twr,
        double AssignedWeightTonnes);

    public record TractionResult(
        double                   TotalTrainWeightTonnes,
        bool                     UseElectricTable,
        int                      SumTwr,
        IReadOnlyList<LocoShare> Shares);

    /// <summary>
    /// Calculates weight distribution across all locos in the consist that have a TWR value.
    /// Returns null if any pulling loco lacks the required TWR for the selected table.
    /// </summary>
    public static TractionResult? Calculate(
        IReadOnlyList<ConsistEntry> entries,
        double totalTrainWeightTonnes)
    {
        // Only locos with a TWR can participate
        var active = entries.Where(e => e.HasTwr).ToList();
        if (active.Count == 0) return null;

        // Use TWR50 table only when NO diesel is present among active locos
        bool anyDiesel = active.Any(e => e.Twr50 == null);
        bool useElectric = !anyDiesel;

        // Pick the applicable TWR for each loco
        var picks = active.Select(e => useElectric
            ? (e.Twr50 ?? e.Twr30)   // all-electric: prefer TWR50, fallback TWR30
            : e.Twr30)
            .ToList();

        // Any null means a loco can't participate in the selected table
        if (picks.Any(p => p == null)) return null;

        int sum = picks.Sum(p => p!.Value);
        if (sum == 0) return null;

        double perUnit = totalTrainWeightTonnes / sum;

        var shares = active.Zip(picks, (entry, twr) =>
            new LocoShare(
                entry.Designation,
                twr!.Value,
                Math.Round(perUnit * twr!.Value, 1)))
            .ToList();

        return new TractionResult(totalTrainWeightTonnes, useElectric, sum, shares);
    }
}
