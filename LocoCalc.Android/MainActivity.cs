using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Avalonia;
using Avalonia.Android;
using LocoCalcAvalonia.ViewModels;

namespace LocoCalcAvalonia.Android;

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

            vm.PdfGenerator   = new AndroidPdfGenerator();
            vm.PdfSaveService = new AndroidPdfSaveService(ctx);

            return vm;
        };

        return base.CustomizeAppBuilder(builder).WithInterFont();
    }
}
