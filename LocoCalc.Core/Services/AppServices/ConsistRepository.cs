using System.Text.Json;
using System.Text.Json.Serialization;
using LocoCalc.Models;

namespace LocoCalc.Services;

public class ConsistRepository
{
    private readonly string _folder;

    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ConsistRepository(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(_folder);
    }

    public IReadOnlyList<Consist> GetAll() =>
        Directory.EnumerateFiles(_folder, "*.json")
            .Select(f =>
            {
                try { return JsonSerializer.Deserialize<Consist>(File.ReadAllText(f), _opts); }
                catch { return null; }
            })
            .Where(c => c != null)
            .Cast<Consist>()
            .OrderByDescending(c => c.LastModified)
            .ToList();

    public void Save(Consist consist)
    {
        consist.LastModified = DateTime.UtcNow;
        File.WriteAllText(
            Path.Combine(_folder, $"{consist.Id}.json"),
            JsonSerializer.Serialize(consist, _opts));
    }

    public void Delete(Consist consist)
    {
        var p = Path.Combine(_folder, $"{consist.Id}.json");
        if (File.Exists(p)) File.Delete(p);
    }
}
