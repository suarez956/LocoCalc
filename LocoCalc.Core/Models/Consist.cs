namespace LocoCalc.Models;

public class Consist
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New Consist";
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public List<ConsistEntry> Entries { get; set; } = new();

    public string? StartStationId   { get; set; }
    public string? StartStationName { get; set; }
    public string? EndStationId     { get; set; }
    public string? EndStationName   { get; set; }
    public string? RequiredBrakingPct { get; set; }
}
