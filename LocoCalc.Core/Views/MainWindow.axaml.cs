using Avalonia.Controls;
using LocoCalcAvalonia.ViewModels;

namespace LocoCalcAvalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void Configure(MainViewModel vm)
    {
        // DataContext propagates from Window down to the hosted MainView
        DataContext = vm;
    }
}
