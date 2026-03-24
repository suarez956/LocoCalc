using System.Reflection;
using LocoCalcAvalonia.Services;

namespace LocoCalcAvalonia;

/// <summary>
/// Reads loco JSONs from embedded resources compiled into LocoCalc.Core.
/// Uses a type from Core to locate the correct assembly — avoids Assembly.Load by name.
/// </summary>
public class DesktopLocoDataProvider : ILocoDataProvider
{
    // Anchor type lives in Core — guarantees we get the right assembly
    private static readonly Assembly _coreAsm =
        typeof(BrakingCalculator).Assembly;

    public IEnumerable<string> GetLocoJsonFiles()
    {
        foreach (var name in _coreAsm.GetManifestResourceNames())
        {
            if (!name.StartsWith("Locos", StringComparison.OrdinalIgnoreCase)) continue;
            using var stream = _coreAsm.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            yield return reader.ReadToEnd();
        }
    }
}
