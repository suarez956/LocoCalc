using Android.Content.PM;
using Android.Views;
using Avalonia;
using Avalonia.Android;
using LocoCalc;
using LocoCalc.Services;
using LocoCalc.ViewModels;

namespace LocoCalc;

[Activity(
    Label = "LocoCalc",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@mipmap/ic_launcher",
    MainLauncher = true,
    // AdjustNothing: keyboard doesn't resize the layout; stateHidden: keyboard hidden on start
    WindowSoftInputMode = SoftInput.StateHidden | SoftInput.AdjustNothing,
    ConfigurationChanges =
        ConfigChanges.Orientation        |
        ConfigChanges.ScreenSize         |
        ConfigChanges.UiMode             |
        ConfigChanges.ScreenLayout       |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        AndroidPdfSaveService.CurrentActivity = this;
        base.OnCreate(savedInstanceState);

        // Read the status bar height from Android resources and expose it to Avalonia views
        int resId = Resources!.GetIdentifier("status_bar_height", "dimen", "android");
        if (resId > 0)
            PlatformInsets.StatusBarTop = Resources.GetDimensionPixelSize(resId) / Resources.DisplayMetrics!.Density;

        // Tablets (sw >= 600dp) allow all orientations; phones are portrait-only
        bool isTablet = Resources!.Configuration!.SmallestScreenWidthDp >= 600;
        RequestedOrientation = isTablet
            ? ScreenOrientation.FullSensor
            : ScreenOrientation.Portrait;

        // Belt-and-suspenders: explicitly hide soft keyboard after window is ready
        Window?.SetSoftInputMode(SoftInput.StateAlwaysHidden | SoftInput.AdjustNothing);
    }

    protected override void OnDestroy()
    {
        AndroidPdfSaveService.CurrentActivity = null;
        base.OnDestroy();
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        App.ViewModelFactory = () =>
        {
            var ctx           = global::Android.App.Application.Context;
            var locoProvider  = new AndroidLocoDataProvider();
            var consistFolder = Path.Combine(ctx.FilesDir!.AbsolutePath, "Consists");
            var vm            = new MainViewModel(locoProvider, consistFolder);

            vm.PdfSaveService = new AndroidPdfSaveService(ctx);
            vm.ZoBGenerator   = new SkiaZoBGenerator();

            return vm;
        };

        return base.CustomizeAppBuilder(builder).WithInterFont();
    }
}
