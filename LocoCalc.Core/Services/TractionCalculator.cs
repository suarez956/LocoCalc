using LocoCalcAvalonia.Models;

namespace LocoCalcAvalonia.Services;

/// <summary>
/// Implements the poměrové číslo weight-distribution logic from
/// ČD Cargo KP Doplněk (platný od 01.04.2026), §6.2.
///
/// Table selection:
///   • Any diesel in consist  → use PČ "30" table
///   • All electric           → use PČ "50" table
///
/// Formula (§6.2):
///   DH_per_unit    = TotalTrainWeight / Σ(PČ of every active loco)
///   Loco share (t) = DH_per_unit × its PČ
/// </summary>
public static class TractionCalculator
{
    public record LocoShare(
        string Designation,
        int    PomerCislo,
        double AssignedWeightTonnes);

    public record TractionResult(
        double                   TotalTrainWeightTonnes,
        bool                     UseElectricTable,
        int                      SumPomerCislo,
        IReadOnlyList<LocoShare> Shares);

    /// <summary>
    /// Calculates weight distribution across all locos in the consist that have a PomerCislo.
    /// Returns null if any pulling loco lacks the required PomerCislo for the selected table.
    /// </summary>
    public static TractionResult? Calculate(
        IReadOnlyList<ConsistEntry> entries,
        double totalTrainWeightTonnes)
    {
        // Only locos with a PomerCislo can participate
        var active = entries.Where(e => e.HasPomerCislo).ToList();
        if (active.Count == 0) return null;

        // Use PČ50 table only when NO diesel is present among active locos
        bool anyDiesel = active.Any(e => e.PomerCislo50 == null);
        bool useElectric = !anyDiesel;

        // Pick the applicable PomerCislo for each loco
        var picks = active.Select(e => useElectric
            ? (e.PomerCislo50 ?? e.PomerCislo30)   // all-electric: prefer PČ50, fallback PČ30
            : e.PomerCislo30)
            .ToList();

        // Any null means a loco can't participate in the selected table
        if (picks.Any(p => p == null)) return null;

        int sum = picks.Sum(p => p!.Value);
        if (sum == 0) return null;

        double perUnit = totalTrainWeightTonnes / sum;

        var shares = active.Zip(picks, (entry, pc) =>
            new LocoShare(
                entry.Designation,
                pc!.Value,
                Math.Round(perUnit * pc!.Value, 1)))
            .ToList();

        return new TractionResult(totalTrainWeightTonnes, useElectric, sum, shares);
    }
}
