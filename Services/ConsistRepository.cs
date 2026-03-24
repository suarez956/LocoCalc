using System.Text.Json;
using LocoCalc.Models;

namespace LocoCalc.Services;

public class ConsistRepository
{
    private readonly string _consistFolder;
    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public ConsistRepository()
    {
        var baseDir = AppContext.BaseDirectory;
        _consistFolder = Path.Combine(baseDir, "Data", "Consists");
        Directory.CreateDirectory(_consistFolder);
    }

    public IReadOnlyList<Consist> GetAll()
    {
        var list = new List<Consist>();
        foreach (var file in Directory.EnumerateFiles(_consistFolder, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var c = JsonSerializer.Deserialize<Consist>(json, _opts);
                if (c != null) list.Add(c);
            }
            catch { }
        }
        return list.OrderByDescending(c => c.LastModified).ToList();
    }

    public void Save(Consist consist)
    {
        consist.LastModified = DateTime.UtcNow;
        var path = Path.Combine(_consistFolder, $"{consist.Id}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(consist, _opts));
    }

    public void Delete(Consist consist)
    {
        var path = Path.Combine(_consistFolder, $"{consist.Id}.json");
        if (File.Exists(path)) File.Delete(path);
    }
}
