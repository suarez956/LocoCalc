using Avalonia.Controls;
using LocoCalc.ViewModels;

namespace LocoCalc.Views;

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
