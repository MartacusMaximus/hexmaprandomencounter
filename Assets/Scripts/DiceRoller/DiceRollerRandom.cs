using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class DiceRollerRandom : IDiceRoller
{
    private static readonly Regex notationRegex = new Regex(@"^\s*(\d+)d(\d+)\s*([+-]\s*\d+)?\s*$", RegexOptions.Compiled);

    public List<int> RollNotation(string notation, out int total)
    {
        total = 0;
        var m = notationRegex.Match(notation);
        if (!m.Success)
        {
            Debug.LogError($"DiceRoller: invalid notation '{notation}'");
            return new List<int>();
        }

        int count = int.Parse(m.Groups[1].Value);
        int sides = int.Parse(m.Groups[2].Value);
        int modifier = 0;
        if (m.Groups[3].Success)
            int.TryParse(m.Groups[3].Value.Replace(" ", ""), out modifier);

        var rolls = Roll(count, sides, out int sum);
        total = sum + modifier;

        Debug.Log($"DiceRoller: notation '{notation}' → rolls [{string.Join(", ", rolls)}] modifier {modifier} → total {total}");
        return rolls;
    }

    public List<int> Roll(int count, int sides, out int total)
    {
        total = 0;
        var rolls = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int r = UnityEngine.Random.Range(1, sides + 1); // inclusive lower bound, exclusive upper bound so +1
            rolls.Add(r);
            total += r;
            Debug.Log($"DiceRoller: rolled d{sides} → {r}");
        }
        Debug.Log($"DiceRoller: sum of {count}d{sides} = {total}");
        return rolls;
    }

    public int RollAndTakeHighest(int count, int sides)
    {
        var rolls = Roll(count, sides, out _);
        int highest = int.MinValue;
        foreach (var r in rolls) if (r > highest) highest = r;
        Debug.Log($"DiceRoller: taking highest of [{string.Join(", ", rolls)}] = {highest}");
        return highest;
    }

}
