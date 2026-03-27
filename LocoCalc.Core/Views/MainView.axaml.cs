using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using LocoCalcAvalonia.Models;
using LocoCalcAvalonia.Services;
using LocoCalcAvalonia.ViewModels;

namespace LocoCalcAvalonia.Views;

public partial class MainView : UserControl
{
    private const double MobileBreakpoint = 960;
    private CancellationTokenSource? _toastCts;
    private bool _suppressRenameTextChange = false;

    public MainView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                vm.PropertyChanged += OnVmPropertyChanged;
        };

        SizeChanged += (_, e) => ApplyLayout(e.NewSize.Width);

        AttachedToVisualTree += (_, _) =>
        {
            ApplyLayout(Bounds.Width);
            ApplyStatusBarPadding();
            PlatformInsets.Changed += ApplyStatusBarPadding;

            foreach (var name in new[] { "StartStationBox", "EndStationBox",
                                         "StartStationBoxMobile", "EndStationBoxMobile" })
            {
                if (this.FindControl<AutoCompleteBox>(name) is { } box)
                    box.ItemFilter = StationFilter;
            }
        };

        DetachedFromVisualTree += (_, _) =>
        {
            PlatformInsets.Changed -= ApplyStatusBarPadding;
        };
    }

    // ── Station search ────────────────────────────────────────────────────────
    private static bool StationFilter(string? search, object? item)
    {
        if (item is not Station station || search is null) return false;
        var q = Normalize(search);
        return Normalize(station.Name).Contains(q) || station.Id.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string text)
    {
        var d = text.Normalize(NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(d.Length);
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().ToLowerInvariant();
    }

    private void ApplyStatusBarPadding()
    {
        Padding = new Thickness(0, PlatformInsets.StatusBarTop, 0, 0);
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

    // ── Toast / property-change dispatch ─────────────────────────────────────
    private async void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        if (e.PropertyName == nameof(MainViewModel.RenameDialogVisible))
        {
            if (vm.RenameDialogVisible)
            {
                SyncRenameTextBox(vm);
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    this.FindControl<TextBox>("RenameTextBox")?.Focus());
            }
            return;
        }


        if (e.PropertyName != nameof(MainViewModel.StatusMessage)) return;
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

    // ── Loco list handlers ─────────────────────────────────────────────────
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

    // ── Rename dialog ─────────────────────────────────────────────────────────
    private void SyncRenameTextBox(MainViewModel vm)
    {
        var tb = this.FindControl<TextBox>("RenameTextBox");
        if (tb is null) return;

        _suppressRenameTextChange = true;
        tb.Text = vm.RenameInput;
        tb.CaretIndex = tb.Text?.Length ?? 0;
        _suppressRenameTextChange = false;
    }

    private void OnRenameTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressRenameTextChange) return;
        if (sender is not TextBox tb || DataContext is not MainViewModel vm) return;

        vm.RenameInput = UicFormatter.StripToDigits(tb.Text ?? string.Empty);
    }

    private void OnRenameHistorySelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox lb || DataContext is not MainViewModel vm) return;
        if (lb.SelectedItem is not string formatted) return;

        lb.SelectedItem = null;

        vm.RenameInput = UicFormatter.StripToDigits(formatted);
    }
}
