using System.Text.Json.Serialization;

namespace LocoCalcAvalonia.Models;

public class LocomotiveDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public double TotalWeightTonnes { get; set; }
    public double BrakingWeightTonnes { get; set; }
    public double? BrakingWeightWithEDB { get; set; }
    public double LengthM { get; set; }
    public int MaxSpeed { get; set; } = 80;

    [JsonPropertyName("fp")]
    public string FpClass { get; set; } = "FP2";

    [JsonPropertyName("traction")]
    public string Traction { get; set; } = "diesel";

    /// <summary>"A" = XX XX XXXX XXX-X · "B" = XX XX X XXX XXX-X</summary>
    [JsonPropertyName("uicFormat")]
    public string UicFormat { get; set; } = "A";

    public bool HasEDB => BrakingWeightWithEDB.HasValue;
    public bool IsFP3  => FpClass == "FP3";
}
