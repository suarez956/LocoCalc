using System.Reflection;
using System.Text.Json;
using LocoCalcAvalonia.Models;

namespace LocoCalcAvalonia.Services;

public class StationRepository
{
    public static readonly StationRepository Instance = new();

    public IReadOnlyList<Station> All { get; }

    private StationRepository()
    {
        var asm  = typeof(StationRepository).Assembly;
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("stations.json", StringComparison.OrdinalIgnoreCase));

        if (name is null) { All = []; return; }

        using var stream = asm.GetManifestResourceStream(name)!;
        All = JsonSerializer.Deserialize<List<Station>>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }
}
