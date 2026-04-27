using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using LocoCalc.Services.PdfServices;
using LocoCalc.ViewModels;

namespace LocoCalc;

[Application]
public class MainApplication : AvaloniaAndroidApplication<App>
{
    protected MainApplication(nint handle, JniHandleOwnership transfer) : base(handle, transfer) { }

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
