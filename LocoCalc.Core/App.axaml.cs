using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LocoCalcAvalonia.ViewModels;
using LocoCalcAvalonia.Views;

namespace LocoCalcAvalonia;

public class App : Application
{
    // Set by platform projects to supply a configured ViewModel
    public static Func<MainViewModel>? ViewModelFactory { get; set; }

    // Set by Desktop project to wire up PDF service after window is available
    public static Action<MainWindow, MainViewModel>? PostWindowInit { get; set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var vm = ViewModelFactory?.Invoke();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            if (vm is not null)
            {
                window.Configure(vm);
                PostWindowInit?.Invoke(window, vm);
                // PdfSaveService is set inside PostWindowInit — notify UI
                vm.NotifyPdfSupportChanged();
            }
            desktop.MainWindow = window;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            // Android: UserControl — Window is not valid for ISingleViewApplicationLifetime
            var view = new MainView();
            if (vm is not null) view.DataContext = vm;
            singleView.MainView = view;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
