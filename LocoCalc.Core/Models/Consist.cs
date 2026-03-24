namespace LocoCalcAvalonia.Models;

public class Consist
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New Consist";
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public List<ConsistEntry> Entries { get; set; } = new();
}
