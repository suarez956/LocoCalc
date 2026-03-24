namespace LocoCalc.Models;

public enum ConsistPosition { Front, Middle, Rear }

/// <summary>One locomotive slot in an active consist.</summary>
public class ConsistEntry
{
    public string DefinitionId { get; set; } = string.Empty;
    public ConsistPosition Position { get; set; }
    public bool BrakesEnabled { get; set; }

    // Snapshot fields
    public string Designation { get; set; } = string.Empty;
    public double TotalWeightTonnes { get; set; }
    public double LengthM { get; set; }

    /// <summary>Active braking weight — either WithEDB or WithoutEDB depending on EdbActive.</summary>
    public double BrakingWeightTonnes { get; set; }

    /// <summary>Whether electrodynamic brake is active for this entry (only meaningful if loco has EDB).</summary>
    public bool EdbActive { get; set; }

    /// <summary>Braking weight with EDB. Null if loco has no EDB.</summary>
    public double? BrakingWeightWithEDB { get; set; }

    public bool HasEDB => BrakingWeightWithEDB.HasValue;
}
