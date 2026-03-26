using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LocoCalcAvalonia.Models;

namespace LocoCalcAvalonia.ViewModels;

public partial class TractionGroup : ObservableObject
{
    public string Key      { get; }
    public string Label    { get; set; }
    public string DotColor { get; }
    public ObservableCollection<LocomotiveDefinition> Locos { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ArrowText))]
    private bool _isExpanded = false;

    public string ArrowText => IsExpanded ? "▾" : "▸";

    public TractionGroup(string key, string label, string dotColor)
    {
        Key      = key;
        Label    = label;
        DotColor = dotColor;
    }
}
