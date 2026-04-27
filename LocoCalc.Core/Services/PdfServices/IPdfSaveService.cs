namespace LocoCalc.Services.PdfServices;

/// <summary>
/// Platform-specific save dialog + file open.
/// Implemented in Desktop project; null on Android (PDF not supported).
/// </summary>
public interface IPdfSaveService
{
    Task<string?> PickSavePathAsync(string suggestedName);
    /// <summary>Opens the file and returns the path to display to the user (may differ from the temp write path on Android).</summary>
    string? OpenFile(string path);
}
