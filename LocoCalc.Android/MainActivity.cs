using Android.Content.PM;
using Android.Views;
using Avalonia.Android;
using LocoCalc.Services.PlatformServices;

namespace LocoCalc;

[Activity(
    Label = "LocoCalc",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@mipmap/ic_launcher",
    MainLauncher = true,
    WindowSoftInputMode = SoftInput.StateHidden | SoftInput.AdjustNothing,
    ConfigurationChanges =
        ConfigChanges.Orientation        |
        ConfigChanges.ScreenSize         |
        ConfigChanges.UiMode             |
        ConfigChanges.ScreenLayout       |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        AndroidPdfSaveService.CurrentActivity = this;
        base.OnCreate(savedInstanceState);

        int resId = Resources!.GetIdentifier("status_bar_height", "dimen", "android");
        if (resId > 0)
            PlatformInsets.StatusBarTop = Resources.GetDimensionPixelSize(resId) / Resources.DisplayMetrics!.Density;

        bool isTablet = Resources!.Configuration!.SmallestScreenWidthDp >= 600;
        RequestedOrientation = isTablet
            ? ScreenOrientation.FullSensor
            : ScreenOrientation.Portrait;

        Window?.SetSoftInputMode(SoftInput.StateAlwaysHidden | SoftInput.AdjustNothing);
    }

    protected override void OnDestroy()
    {
        AndroidPdfSaveService.CurrentActivity = null;
        base.OnDestroy();
    }
}
