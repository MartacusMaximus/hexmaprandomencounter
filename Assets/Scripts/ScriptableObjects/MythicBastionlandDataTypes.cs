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
}
