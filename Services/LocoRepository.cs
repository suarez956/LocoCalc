using System.Text.Json;
using LocoCalc.Models;

namespace LocoCalc.Services;

public class LocoRepository
{
    private readonly string _locoFolder;
    private List<LocomotiveDefinition>? _cache;

    public LocoRepository()
    {
        // Resolve relative to the executable so it works from any working dir
        var baseDir = AppContext.BaseDirectory;
        _locoFolder = Path.Combine(baseDir, "Data", "Locos");
    }

    public IReadOnlyList<LocomotiveDefinition> GetAll()
    {
        if (_cache != null) return _cache;

        _cache = new List<LocomotiveDefinition>();

        if (!Directory.Exists(_locoFolder))
            return _cache;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var file in Directory.EnumerateFiles(_locoFolder, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var def = JsonSerializer.Deserialize<LocomotiveDefinition>(json, options);
                if (def != null)
                    _cache.Add(def);
            }
            catch
            {
                // Skip malformed files silently; could log here
            }
        }

        _cache = _cache.OrderBy(d => d.Designation).ToList();
        return _cache;
    }

    /// <summary>Force a reload on next GetAll() call (e.g. after adding a new file).</summary>
    public void Invalidate() => _cache = null;
}
