namespace LocoCalc.Models;

/// <summary>
/// Represents a locomotive type loaded from a JSON definition file.
/// Position is assigned at runtime based on consist order.
/// </summary>
public class LocomotiveDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public double TotalWeightTonnes { get; set; }
    public double LengthM { get; set; }

    /// <summary>Braking weight WITHOUT electrodynamic brake (conservative, always available).</summary>
    public double BrakingWeightTonnes { get; set; }

    /// <summary>
    /// Braking weight WITH electrodynamic brake (EDB). Null means this loco has no EDB.
    /// </summary>
    public double? BrakingWeightWithEDB { get; set; }

    public bool HasEDB => BrakingWeightWithEDB.HasValue;
}
