using System.Text.RegularExpressions;

namespace KnightsAndGM.Shared
{
    public sealed class DiceNotation
    {
        public int Count { get; set; }
        public int Sides { get; set; }
        public int Modifier { get; set; }
    }

    public static class DiceNotationParser
    {
        private static readonly Regex NotationRegex = new Regex(@"^\s*(\d+)d(\d+)\s*([+-]\s*\d+)?\s*$", RegexOptions.Compiled);

        public static bool TryParse(string notation, out DiceNotation parsed)
        {
            parsed = null;
            if (string.IsNullOrWhiteSpace(notation))
            {
                return false;
            }

            var match = NotationRegex.Match(notation);
            if (!match.Success)
            {
                return false;
            }

            var modifier = 0;
            if (match.Groups[3].Success)
            {
                int.TryParse(match.Groups[3].Value.Replace(" ", string.Empty), out modifier);
            }

            parsed = new DiceNotation
            {
                Count = int.Parse(match.Groups[1].Value),
                Sides = int.Parse(match.Groups[2].Value),
                Modifier = modifier
            };

            return true;
        }
    }
}
