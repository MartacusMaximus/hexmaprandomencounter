using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public sealed class MythCastEntry
{
    public string name;
    [TextArea] public string statBlock;
    [TextArea] public string notes;
}

[Serializable]
public sealed class MythicTableColumn
{
    public string header;
    [TextArea] public List<string> values = new List<string>();
}

[Serializable]
public sealed class MythicRollTable
{
    public string title;
    public List<MythicTableColumn> columns = new List<MythicTableColumn>();
}

[Serializable]
public sealed class MythicResolvedEquipmentData
{
    [TextArea] public string rulesText = string.Empty;
    public string damageDiceNotation = string.Empty;
    public int armorValue = -1;
    public int requiredHands = -1;
    public List<string> traitNames = new List<string>();
    public List<string> generatedTags = new List<string>();
}

public static class MythicEquipmentTableResolver
{
    public static bool HasTable(MythicRollTable table)
    {
        if (table?.columns == null || table.columns.Count == 0)
        {
            return false;
        }

        for (var columnIndex = 0; columnIndex < table.columns.Count; columnIndex++)
        {
            var column = table.columns[columnIndex];
            if (column?.values != null && column.values.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    public static int GetRowCount(MythicRollTable table)
    {
        if (!HasTable(table))
        {
            return 0;
        }

        var maxRows = 0;
        for (var columnIndex = 0; columnIndex < table.columns.Count; columnIndex++)
        {
            var rowCount = table.columns[columnIndex]?.values?.Count ?? 0;
            if (rowCount > maxRows)
            {
                maxRows = rowCount;
            }
        }

        return maxRows;
    }

    public static string FormatRow(MythicRollTable table, int rowIndex)
    {
        if (!HasTable(table) || rowIndex < 0)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        for (var columnIndex = 0; columnIndex < table.columns.Count; columnIndex++)
        {
            var column = table.columns[columnIndex];
            if (column?.values == null || rowIndex >= column.values.Count)
            {
                continue;
            }

            var value = column.values[rowIndex]?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var header = column.header?.Trim().TrimEnd(':');
            parts.Add(string.IsNullOrWhiteSpace(header) ? value : $"{header}: {value}");
        }

        return string.Join(", ", parts).Trim();
    }

    public static string ResolveSeeBelowText(string baseRulesText, MythicRollTable table, int rowIndex)
    {
        var rowText = FormatRow(table, rowIndex);
        if (string.IsNullOrWhiteSpace(rowText))
        {
            return baseRulesText ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(baseRulesText))
        {
            return rowText;
        }

        var resolved = Regex.Replace(baseRulesText, @"\bsee below\b\.?", rowText, RegexOptions.IgnoreCase);
        resolved = Regex.Replace(resolved, @"\s{2,}", " ");
        return resolved.Trim();
    }

    public static MythicResolvedEquipmentData ResolveEquipment(EquipmentData equipment, MythicRollTable table, int rowIndex)
    {
        var resolved = new MythicResolvedEquipmentData
        {
            rulesText = ResolveSeeBelowText(equipment != null ? equipment.rulesText : string.Empty, table, rowIndex)
        };

        var rowText = FormatRow(table, rowIndex);
        if (string.IsNullOrWhiteSpace(rowText))
        {
            return resolved;
        }

        foreach (var traitName in ExtractTraitNames(rowText))
        {
            if (!resolved.traitNames.Contains(traitName))
            {
                resolved.traitNames.Add(traitName);
            }
        }

        if (resolved.traitNames.Count > 0)
        {
            resolved.requiredHands = ComputeRequiredHands(resolved.traitNames);
        }

        foreach (var tag in ExtractGeneratedTags(rowText))
        {
            if (!resolved.generatedTags.Contains(tag))
            {
                resolved.generatedTags.Add(tag);
            }
        }

        if (!CanTableDefinePrimaryProfile(equipment))
        {
            return resolved;
        }

        if (TryExtractPrimaryDamageDice(rowText, out var damageDiceNotation))
        {
            resolved.damageDiceNotation = damageDiceNotation;
        }

        if (TryExtractPrimaryArmorValue(rowText, out var armorValue))
        {
            resolved.armorValue = armorValue;
        }

        return resolved;
    }

    private static bool CanTableDefinePrimaryProfile(EquipmentData equipment)
    {
        if (equipment == null)
        {
            return false;
        }

        var hasBaseProfile = !string.IsNullOrWhiteSpace(equipment.damageDiceNotation) || equipment.armorValue > 0;
        if (hasBaseProfile)
        {
            return false;
        }

        return Regex.IsMatch(equipment.rulesText ?? string.Empty, @"^\s*\(?see below\b", RegexOptions.IgnoreCase);
    }

    private static bool TryExtractPrimaryDamageDice(string rowText, out string damageDiceNotation)
    {
        damageDiceNotation = string.Empty;
        var match = Regex.Match(rowText ?? string.Empty, @"\((\d*)d(\d+)([^)]*)\)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return false;
        }

        var count = string.IsNullOrWhiteSpace(match.Groups[1].Value) ? "1" : match.Groups[1].Value;
        damageDiceNotation = $"{count}d{match.Groups[2].Value}";
        return true;
    }

    private static bool TryExtractPrimaryArmorValue(string rowText, out int armorValue)
    {
        armorValue = 0;
        var match = Regex.Match(rowText ?? string.Empty, @"\bA(\d+)\b", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return false;
        }

        armorValue = int.Parse(match.Groups[1].Value);
        return true;
    }

    private static List<string> ExtractTraitNames(string rowText)
    {
        var traits = new List<string>();
        AddTraitIfPresent(traits, rowText, "Long");
        AddTraitIfPresent(traits, rowText, "Hefty");
        AddTraitIfPresent(traits, rowText, "Slow");
        AddTraitIfPresent(traits, rowText, "Deadly");
        return traits;
    }

    private static void AddTraitIfPresent(ICollection<string> traits, string rowText, string traitName)
    {
        if (Regex.IsMatch(rowText ?? string.Empty, $@"\b{Regex.Escape(traitName)}\b", RegexOptions.IgnoreCase))
        {
            traits.Add(traitName);
        }
    }

    private static int ComputeRequiredHands(IEnumerable<string> traitNames)
    {
        var total = 0;
        foreach (var traitName in traitNames ?? Array.Empty<string>())
        {
            if (string.Equals(traitName, "Long", StringComparison.OrdinalIgnoreCase))
            {
                total += 2;
            }
            else if (string.Equals(traitName, "Hefty", StringComparison.OrdinalIgnoreCase))
            {
                total += 1;
            }
        }

        return total <= 0 ? -1 : total;
    }

    private static List<string> ExtractGeneratedTags(string rowText)
    {
        var tags = new List<string>();

        foreach (Match match in Regex.Matches(rowText ?? string.Empty, @"\+d\d+\s+vs\s+[^,]+", RegexOptions.IgnoreCase))
        {
            tags.Add($"conditional_damage:{match.Value.Trim()}");
        }

        foreach (Match match in Regex.Matches(rowText ?? string.Empty, @"can be thrown\s+\((\d*d\d+)\)", RegexOptions.IgnoreCase))
        {
            var notation = match.Groups[1].Value;
            tags.Add($"thrown_damage:{(notation.StartsWith("d", StringComparison.OrdinalIgnoreCase) ? $"1{notation}" : notation)}");
        }

        foreach (Match match in Regex.Matches(rowText ?? string.Empty, @"A\d+\s+when\s+[^,]+", RegexOptions.IgnoreCase))
        {
            tags.Add($"conditional_armor:{match.Value.Trim()}");
        }

        return tags;
    }
}
