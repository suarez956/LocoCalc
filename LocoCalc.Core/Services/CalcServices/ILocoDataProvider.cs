namespace LocoCalc.Services;

/// <summary>
/// Platform-specific contract for supplying loco JSON strings.
/// Desktop: reads from embedded assembly resources.
/// Android: reads from APK assets.
/// </summary>
public interface ILocoDataProvider
{
    IEnumerable<string> GetLocoJsonFiles();
}
