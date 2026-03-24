using Avalonia.Controls;
using Avalonia.Input;
using LocoCalcAvalonia.ViewModels;

namespace LocoCalcAvalonia.Views;

public partial class MainView : UserControl
{
    private const double MobileBreakpoint = 960;
    private CancellationTokenSource? _toastCts;

    public MainView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                vm.PropertyChanged += OnVmPropertyChanged;
        };

        SizeChanged += (_, e) => ApplyLayout(e.NewSize.Width);

        // Apply layout once attached (Bounds not valid before this)
        AttachedToVisualTree += (_, _) => ApplyLayout(Bounds.Width);
    }

    private void ApplyLayout(double width)
    {
        var desktop = this.FindControl<Grid>("DesktopLayout");
        var mobile  = this.FindControl<Grid>("MobileLayout");
        if (desktop is null || mobile is null) return;

        var isMobile = width < MobileBreakpoint || width == 0;
        desktop.IsVisible = !isMobile;
        mobile.IsVisible  = isMobile;
    }

    // ── Toast ──────────────────────────────────────────────────────────────
    private async void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.StatusMessage)) return;
        if (DataContext is not MainViewModel vm) return;
        if (string.IsNullOrEmpty(vm.StatusMessage)) return;

        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();
        var token = _toastCts.Token;

        var toast = this.FindControl<Border>("ToastBorder");
        if (toast is null) return;

        try
        {
            await AnimateOpacity(toast, toast.Opacity, 1.0, 200, token);
            await Task.Delay(2500, token);
            await AnimateOpacity(toast, 1.0, 0.0, 400, token);
        }
        catch (OperationCanceledException) { }
    }

    private static async Task AnimateOpacity(
        Border target, double from, double to, int durationMs,
        CancellationToken token)
    {
        const int steps = 20;
        var stepMs = durationMs / steps;
        for (int i = 1; i <= steps; i++)
        {
            token.ThrowIfCancellationRequested();
            target.Opacity = from + (to - from) * i / steps;
            await Task.Delay(stepMs, token);
        }
        target.Opacity = to;
    }

    // ── Loco list handlers (desktop + mobile share the same) ───────────────
    private void OnLocoListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox lb || DataContext is not MainViewModel vm) return;
        if (lb.SelectedItem is LocoListItem { IsLoco: true } item && item.Loco is not null)
            vm.SelectedLoco = item.Loco;
        else if (lb.SelectedItem is LocoListItem { IsHeader: true })
            lb.SelectedItem = null;
    }

    private void OnLocoListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not ListBox lb || DataContext is not MainViewModel vm) return;
        if (lb.SelectedItem is LocoListItem { IsLoco: true } item && item.Loco is not null)
        {
            vm.SelectedLoco = item.Loco;
            vm.AddLocoToConsistCommand.Execute(null);
        }
    }

    private void OnConsistEntryDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        var entry = (sender as Avalonia.Controls.Control)?.DataContext as ConsistEntryViewModel;
        if (entry is null) return;
        vm.OpenRenameDialogCommand.Execute(entry);
    }
}
