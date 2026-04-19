namespace LocoCalcAvalonia.Models;

public enum ConsistPosition { Front, Middle, Rear }

public class ConsistEntry
{
    public string DefinitionId { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public double TotalWeightTonnes { get; set; }
    public double BrakingWeightTonnes { get; set; }
    public double? BrakingWeightTonnesR { get; set; }
    public double? BrakingWeightWithEDB { get; set; }
    public double? BrakingWeightWithEDBR { get; set; }
    public double LengthM { get; set; }
    public int MaxSpeed { get; set; }
    public string FpClass { get; set; } = "FP2";
    public string? AxleLoad { get; set; }
    public ConsistPosition Position { get; set; }
    public bool BrakesEnabled { get; set; }
    public bool EdbActive { get; set; }
    public bool RModeActive { get; set; }
    public string? CustomName { get; set; }
    public string UicFormat { get; set; } = "A";
    public List<string>? UicPrefixes { get; set; }
    public int UicPrefixOffset { get; set; } = 4;
    public bool UicValidateCheck { get; set; }
    public string UicTypePrefix { get; set; } = string.Empty;
    public int? Twr30 { get; set; }
    public int? Twr50 { get; set; }
    public int AxleCount { get; set; } = 4;
    public double? SecuringForceKn { get; set; }
    public bool MultipleUnit { get; set; } = false;
    public bool IsTransported { get; set; } = false;

    public bool HasEDB  => BrakingWeightWithEDB.HasValue;
    public bool HasRMode => BrakingWeightTonnesR.HasValue;
    public bool HasTwr  => Twr30.HasValue;
}
