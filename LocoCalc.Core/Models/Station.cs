using System.Text.Json.Serialization;

namespace LocoCalcAvalonia.Models;

public class Station
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}
