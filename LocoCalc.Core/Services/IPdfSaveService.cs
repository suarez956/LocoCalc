namespace LocoCalcAvalonia.Services;

/// <summary>
/// Platform-specific save dialog + file open.
/// Implemented in Desktop project; null on Android (PDF not supported).
/// </summary>
public interface IPdfSaveService
{
    Task<string?> PickSavePathAsync(string suggestedName);
    void OpenFile(string path);
}
