using Avalonia;
using LocoCalcAvalonia;
using LocoCalcAvalonia.Services;
using LocoCalcAvalonia.ViewModels;
using LocoCalcAvalonia.Views;

App.WindowFactory = () => new MainWindow();

App.ViewModelFactory = () =>
{
    var locoProvider  = new DesktopLocoDataProvider();
    var consistFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LocoCalc", "Consists");
    var vm = new MainViewModel(locoProvider, consistFolder);
    vm.PdfGenerator = new PdfReportService();   // QuestPDF implementation
    return vm;
};

// PdfSaveService needs the Window — injected after it's created
App.PostWindowInit = (window, vm) =>
{
    vm.PdfSaveService = new DesktopPdfSaveService(window);
};

AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);
