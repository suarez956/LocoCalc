namespace LocoCalc.Services;

/// <summary>
/// Carries platform-reported system bar insets to platform-agnostic views.
/// Android sets StatusBarTop; other platforms leave it at 0.
/// </summary>
public static class PlatformInsets
{
    private static double _statusBarTop;

    public static double StatusBarTop
    {
        get => _statusBarTop;
        set
        {
            if (_statusBarTop == value) return;
            _statusBarTop = value;
            Changed?.Invoke();
        }
    }

    public static event Action? Changed;
}
