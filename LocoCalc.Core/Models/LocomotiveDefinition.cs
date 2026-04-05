using System.Text.Json.Serialization;

namespace LocoCalcAvalonia.Models;

public class LocomotiveDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public double TotalWeightTonnes { get; set; }
    public double BrakingWeightTonnes { get; set; }
    public double? BrakingWeightTonnesR { get; set; }
    public double? BrakingWeightWithEDB { get; set; }
    public double? BrakingWeightWithEDBR { get; set; }
    public double LengthM { get; set; }
    public int MaxSpeed { get; set; } = 80;

    [JsonPropertyName("fp")]
    public string FpClass { get; set; } = "FP2";

    /// <summary>Track line class (A, B1, B2, C2, C3, C4, D2, D3, D4). Null = not specified.</summary>
    [JsonPropertyName("axleLoad")]
    public string? AxleLoad { get; set; }

    [JsonPropertyName("traction")]
    public string Traction { get; set; } = "diesel";

    /// <summary>"A" = XX XX XXXX XXX-X · "B" = XX XX X XXX XXX-X</summary>
    [JsonPropertyName("uicFormat")]
    public string UicFormat { get; set; } = "A";

    /// <summary>
    /// Allowed digit substrings for the UIC number, matched at <see cref="UicPrefixOffset"/>.
    /// Empty/null means no restriction. Example: ["3630","3632"] for Řada 363.
    /// </summary>
    [JsonPropertyName("uicPrefixes")]
    public List<string>? UicPrefixes { get; set; }

    /// <summary>Zero-based index in the raw 12-digit string where <see cref="UicPrefixes"/> are checked. Default 4 (skips 2-digit type + 2-digit country).</summary>
    [JsonPropertyName("uicPrefixOffset")]
    public int UicPrefixOffset { get; set; } = 4;

    /// <summary>
    /// Explicit override for check-digit validation. When null, validation is enabled automatically
    /// for Format B locos (Czech) and disabled for all others.
    /// </summary>
    [JsonPropertyName("uicValidateCheck")]
    public bool? UicValidateCheckOverride { get; set; }

    /// <summary>True when UIC check-digit should be validated for this class.</summary>
    [JsonIgnore]
    public bool UicValidateCheck => UicValidateCheckOverride ?? UicFormat == "B";

    /// <summary>
    /// Pre-fill prefix for the UIC input dialog (raw digits, no spaces).
    /// Czech electric locos → "91547", diesel → "92542", non-Czech → empty.
    /// </summary>
    [JsonIgnore]
    public string UicTypePrefix => UicFormat == "B"
        ? (Traction == "diesel" ? "92542" : "91547")
        : string.Empty;

    public bool HasEDB   => BrakingWeightWithEDB.HasValue;
    public bool HasRMode => BrakingWeightTonnesR.HasValue;
    public bool IsFP3    => FpClass == "FP3";
}
