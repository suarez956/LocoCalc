using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocoCalcAvalonia.Services;

public class PdfTheme
{
    public string BgPage     { get; set; } = "#ffffff";
    public string BgCard     { get; set; } = "#f8f8f8";
    public string BgEtcs     { get; set; } = "#1a1a2e";
    public string BgTableHdr { get; set; } = "#1a1a2e";
    public string BgRowEven  { get; set; } = "#ffffff";
    public string BgRowOdd   { get; set; } = "#f9f9f9";
    public string BgTotals   { get; set; } = "#f0f0f0";
    public string BgWarn     { get; set; } = "#fdecea";
    public string TxtMain    { get; set; } = "#1a1a2e";
    public string TxtSub     { get; set; } = "#666666";
    public string TxtMeta    { get; set; } = "#888888";
    public string TxtCell    { get; set; } = "#111111";
    public string TxtEtcs    { get; set; } = "#aaaaaa";
    public string TxtWarn    { get; set; } = "#922b21";
    public string Orange     { get; set; } = "#f97316";
    public string Green      { get; set; } = "#27ae60";
    public string Red        { get; set; } = "#c0392b";
    public string WarnBorder { get; set; } = "#e74c3c";
    public string FooterLine { get; set; } = "#dddddd";
    public string FooterText { get; set; } = "#999999";
}

internal class ThemeConfig
{
    [JsonPropertyName("light")] public PdfTheme Light { get; set; } = new();
    [JsonPropertyName("dark")]  public PdfTheme Dark  { get; set; } = new();
}

public static class PdfThemeService
{
    private static ThemeConfig _config = new();

    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    static PdfThemeService()
    {
        var asm = typeof(PdfThemeService).Assembly;
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("theme.json", StringComparison.OrdinalIgnoreCase));
        if (name is null) return;

        using var stream = asm.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        _config = JsonSerializer.Deserialize<ThemeConfig>(reader.ReadToEnd(), _opts) ?? new();
    }

    public static PdfTheme Get(bool darkMode) => darkMode ? _config.Dark : _config.Light;
}
