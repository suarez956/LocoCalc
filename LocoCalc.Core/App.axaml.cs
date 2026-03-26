using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LocoCalcAvalonia.ViewModels;
using LocoCalcAvalonia.Views;

namespace LocoCalcAvalonia;

public class App : Application
{
    // Set by platform projects to supply a configured ViewModel
    public static Func<MainViewModel>? ViewModelFactory { get; set; }

    // Set by Desktop project to create the main window
    public static Func<Window>? WindowFactory { get; set; }

    // Set by Desktop project to wire up services after window is created
    public static Action<Window, MainViewModel>? PostWindowInit { get; set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var vm = ViewModelFactory?.Invoke();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = WindowFactory?.Invoke() ?? throw new InvalidOperationException("WindowFactory must be set for desktop.");
            if (vm is not null)
            {
                window.DataContext = vm;
                PostWindowInit?.Invoke(window, vm);
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
