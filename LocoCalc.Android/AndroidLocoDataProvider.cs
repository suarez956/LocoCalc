using LocoCalc.Services;

namespace LocoCalc;

/// <summary>Reads loco JSONs from APK assets (Locos/ folder).</summary>
public class AndroidLocoDataProvider : ILocoDataProvider
{
    public IEnumerable<string> GetLocoJsonFiles()
    {
        var assets = global::Android.App.Application.Context.Assets!;
        foreach (var name in assets.List("Locos") ?? Array.Empty<string>())
        {
            if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
            using var stream = assets.Open($"Locos/{name}");
            using var reader = new StreamReader(stream);
            yield return reader.ReadToEnd();
        }
    }
}
