namespace KnightsAndGM.Shared
{
    public sealed class TravelResolution
    {
        public static TravelResolution Fail(string errorCode, string errorMessage)
        {
            return new TravelResolution
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }

        public static TravelResolution Succeed(HexCoordinate from, HexCoordinate to, int effortCost, int remainingEffort)
        {
            return new TravelResolution
            {
                Success = true,
                From = from,
                To = to,
                EffortCost = effortCost,
                RemainingEffort = remainingEffort
            };
        }

        public bool Success { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public HexCoordinate From { get; set; }
        public HexCoordinate To { get; set; }
        public int EffortCost { get; set; }
        public int RemainingEffort { get; set; }
    }

    public sealed class TravelRuleSet
    {
        public TravelRuleSet(int baseEffortCost = 1)
        {
            BaseEffortCost = baseEffortCost < 1 ? 1 : baseEffortCost;
        }

        public int BaseEffortCost { get; }

        public TravelResolution ResolveMove(HexCoordinate from, HexCoordinate to, int availableEffort)
        {
            if (!HexAdjacency.AreNeighbors(from, to))
            {
                return TravelResolution.Fail("not_adjacent", "Target hex is not adjacent to the current party position.");
            }

            if (availableEffort < BaseEffortCost)
            {
                return TravelResolution.Fail("insufficient_effort", "Party does not have enough effort remaining to travel.");
            }

            return TravelResolution.Succeed(from, to, BaseEffortCost, availableEffort - BaseEffortCost);
        }
    }
}
