using System.Text.Json;

namespace LocoCalcAvalonia.Services;

/// <summary>Persists per-locomotive-class UIC number history (raw digits only).</summary>
public class UicNameHistory
{
    private const int MaxPerClass = 10;
    private readonly string _filePath;
    private readonly Dictionary<string, List<string>> _data = new();

    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public UicNameHistory(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    /// <summary>Returns stored raw-digit entries for the given loco class, most-recent first.</summary>
    public IReadOnlyList<string> GetFor(string definitionId)
    {
        if (_data.TryGetValue(definitionId, out var list))
            return list.AsReadOnly();
        return Array.Empty<string>();
    }

    /// <summary>Returns all history entries as a read-only snapshot.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetAll() =>
        _data.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value.AsReadOnly());

    /// <summary>Removes a specific raw-digit entry for the given loco class and persists.</summary>
    public void Remove(string definitionId, string digits)
    {
        if (!_data.TryGetValue(definitionId, out var list)) return;
        list.Remove(digits);
        if (list.Count == 0) _data.Remove(definitionId);
        Save();
    }

    /// <summary>Adds a raw-digit UIC number to history for the given loco class and persists.</summary>
    public void Add(string definitionId, string digits)
    {
        if (!_data.TryGetValue(definitionId, out var list))
        {
            list = new List<string>();
            _data[definitionId] = list;
        }

        list.Remove(digits);
        list.Insert(0, digits);

        while (list.Count > MaxPerClass)
            list.RemoveAt(list.Count - 1);

        Save();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var loaded = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
                File.ReadAllText(_filePath), _opts);
            if (loaded is not null)
                foreach (var kv in loaded)
                    _data[kv.Key] = kv.Value;
        }
        catch { /* ignore corrupted file */ }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(_data, _opts));
        }
        catch { /* ignore save failures */ }
    }
}
