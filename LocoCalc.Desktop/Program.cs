using System.Runtime;
using Avalonia;
using Avalonia.Threading;
using LocoCalc;
using LocoCalc.Services;
using LocoCalc.ViewModels;
using MainWindow = LocoCalc.Views.MainWindow;

App.WindowFactory = () => new MainWindow();

App.ViewModelFactory = () =>
{
    var locoProvider  = new DesktopLocoDataProvider();
    var consistFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LocoCalc", "Consists");
    var vm = new MainViewModel(locoProvider, consistFolder);
    vm.ZoBGenerator  = new SkiaZoBGenerator();
    return vm;
};

// PdfSaveService needs the Window — injected after it's created
App.PostWindowInit = (window, vm) =>
{
    vm.PdfSaveService = new DesktopPdfSaveService(window);

    // Release startup allocations (XAML parsing, font init, etc.) after first render
    Dispatcher.UIThread.InvokeAsync(CompactGc, DispatcherPriority.Background);
};

static void CompactGc()
{
    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
}

AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);
