using LocoCalc.Models;

namespace LocoCalc.Services;

public static class ConsistNameGenerator
{
    /// <summary>
    /// Generates a human-readable consist name from entries.
    /// Groups by designation, e.g. "2× BR 193 Vectron, BR 232 lst."
    /// The last loco (Rear) gets the "lst." / "posl." suffix.
    /// </summary>
    public static string Generate(IList<ConsistEntryViewModel_Snapshot> entries, AppLanguage lang)
    {
        if (entries.Count == 0)
            return lang == AppLanguage.Czech ? "Prázdná souprava" : "Empty consist";

        // Group by designation preserving order of first appearance
        var groups = new List<(string Designation, int Count)>();
        foreach (var e in entries)
        {
            var existing = groups.FindIndex(g => g.Designation == e.Designation);
            if (existing >= 0)
                groups[existing] = (groups[existing].Designation, groups[existing].Count + 1);
            else
                groups.Add((e.Designation, 1));
        }

        string lastSuffix = lang == AppLanguage.Czech ? " posl." : " lst.";

        var parts = groups.Select((g, idx) =>
        {
            bool isLast = idx == groups.Count - 1 && entries.Last().Position == ConsistPosition.Rear;
            string prefix = g.Count > 1 ? $"{g.Count}× " : "";
            string suffix = isLast ? lastSuffix : "";
            return $"{prefix}{g.Designation}{suffix}";
        });

        return string.Join(", ", parts);
    }
}

/// <summary>Minimal snapshot passed to the generator to avoid circular deps.</summary>
public record ConsistEntryViewModel_Snapshot(
    string Designation,
    ConsistPosition Position
);
