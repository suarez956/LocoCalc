using LocoCalc.Models;

namespace LocoCalc.Services;

public static class ConsistNameGenerator
{
    public static string Generate(IReadOnlyList<ConsistEntry> entries, AppLanguage lang)
    {
        if (entries.Count == 0) return string.Empty;

        var groups = new List<(string Name, int Count)>();
        foreach (var e in entries)
        {
            var idx = groups.FindIndex(g => g.Name == e.Designation);
            if (idx >= 0) groups[idx] = (groups[idx].Name, groups[idx].Count + 1);
            else groups.Add((e.Designation, 1));
        }

        var lastIsRear = entries[^1].Position == ConsistPosition.Rear;
        var sfx = lang == AppLanguage.Czech ? " posl." : " lst.";

        var parts = groups.Select((g, i) =>
        {
            var isLast = i == groups.Count - 1 && lastIsRear;
            var prefix = g.Count > 1 ? $"{g.Count}× " : "";
            return prefix + g.Name + (isLast ? sfx : "");
        });

        return string.Join(", ", parts);
    }
}
