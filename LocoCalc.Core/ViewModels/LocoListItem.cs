using CommunityToolkit.Mvvm.ComponentModel;
using LocoCalcAvalonia.Models;

namespace LocoCalcAvalonia.ViewModels;

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

    // ── Visibility (loco rows hide when group is collapsed) ───────────────
    public bool RowVisible => IsHeader || (Group?.IsExpanded ?? true);

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
