using CommunityToolkit.Mvvm.ComponentModel;
using LocoCalc.Models;

namespace LocoCalc.ViewModels;

/// <summary>
/// Flat list item — either a group header or a loco row.
/// Loco items reference their parent group so they can react to IsExpanded.
/// </summary>
public partial class LocoListItem : ObservableObject
{
    public bool IsHeader => Loco is null;
    public bool IsLoco   => Loco is not null;

    // ── Header fields ──────────────────────────────────────────────────────
    public TractionGroup? Group { get; }

    public string HeaderLabel    => Group?.Label    ?? string.Empty;
    public string HeaderDotColor => Group?.DotColor ?? "#888888";
    public int    HeaderCount    => Group?.Locos.Count ?? 0;
    public string ArrowText      => Group?.ArrowText ?? "▸";
    public bool   IsExpanded     => Group?.IsExpanded ?? true;

    // ── Loco field ─────────────────────────────────────────────────────────
    public LocomotiveDefinition? Loco { get; }

    // ── Search filter ─────────────────────────────────────────────────────
    private bool _isFiltered;

    public void UpdateFilter(string q)
    {
        bool filtered;
        if (IsHeader)
            filtered = !string.IsNullOrEmpty(q) &&
                       !Group!.Locos.Any(l => l.Designation.Contains(q, StringComparison.OrdinalIgnoreCase));
        else
            filtered = !string.IsNullOrEmpty(q) &&
                       !(Loco!.Designation.Contains(q, StringComparison.OrdinalIgnoreCase));

        if (_isFiltered == filtered) return;
        _isFiltered = filtered;
        OnPropertyChanged(nameof(RowVisible));
    }

    // ── Visibility (loco rows hide when group is collapsed or filtered) ───
    public bool RowVisible => !_isFiltered && (IsHeader || (Group?.IsExpanded ?? true));

    // ── Constructors ───────────────────────────────────────────────────────
    public LocoListItem(TractionGroup group)
    {
        Group = group;
        Loco  = null;
        group.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(TractionGroup.IsExpanded) or nameof(TractionGroup.ArrowText))
            {
                OnPropertyChanged(nameof(IsExpanded));
                OnPropertyChanged(nameof(ArrowText));
                OnPropertyChanged(nameof(RowVisible));
            }
        };
    }

    public LocoListItem(TractionGroup group, LocomotiveDefinition loco)
    {
        Group = group;
        Loco  = loco;
        group.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(TractionGroup.IsExpanded))
                OnPropertyChanged(nameof(RowVisible));
        };
    }
}
