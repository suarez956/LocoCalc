using System.Text.Json;
using LocoCalcAvalonia.Models;

namespace LocoCalcAvalonia.Services;

public class LocoRepository
{
    private readonly ILocoDataProvider _provider;
    private List<LocomotiveDefinition>? _cache;

    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LocoRepository(ILocoDataProvider provider)
    {
        _provider = provider;
    }

    public IReadOnlyList<LocomotiveDefinition> GetAll()
    {
        if (_cache != null) return _cache;
        _cache = _provider.GetLocoJsonFiles()
            .Select(json =>
            {
                try { return JsonSerializer.Deserialize<LocomotiveDefinition>(json, _opts); }
                catch { return null; }
            })
            .Where(d => d != null)
            .Cast<LocomotiveDefinition>()
            .OrderBy(d => d.Designation)
            .ToList();
        return _cache;
    }

    public void Invalidate() => _cache = null;
}
