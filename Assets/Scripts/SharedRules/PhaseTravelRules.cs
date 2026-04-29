using System;
using System.Collections.Generic;

namespace KnightsAndGM.Shared
{
    public enum TravelMethod
    {
        Trek = 0,
        Gallop = 1,
        Cruise = 2
    }

    public enum BlindTravelOutcome
    {
        AsPlanned = 0,
        CircleBack = 1,
        DriftLeft = 2,
        DriftRight = 3
    }

    public sealed class TravelPhaseContext
    {
        public HexCoordinate From { get; set; }
        public HexCoordinate To { get; set; }
        public int AvailableEffort { get; set; }
        public int PathLength { get; set; }
        public TravelMethod Method { get; set; }
        public bool HasSteed { get; set; }
        public bool SteedExhausted { get; set; }
        public bool OnProperRoad { get; set; }
        public bool ByBoat { get; set; }
        public bool TravelingBlind { get; set; }
        public bool IsNight { get; set; }
        public bool CampingOutdoors { get; set; }
        public bool SleptIndoors { get; set; }
        public bool IsWinter { get; set; }
        public bool DireWeatherRegion { get; set; }
        public bool ConsecutiveLoomingThreat { get; set; }
        public bool EndsInMythHex { get; set; }
        public bool EndsAtLandmark { get; set; }
        public bool EndsAtHolding { get; set; }
        public bool BarrierBlocksRoute { get; set; }
        public int WildernessRoll { get; set; } = 6;
        public int BlindRoll { get; set; } = 6;
        public int WeatherRoll { get; set; } = 6;
        public int MoodRoll { get; set; } = 6;
        public int GallopLossRoll { get; set; } = 1;
        public int NightSpiritLossRoll { get; set; } = 1;
        public int WinterVigorLossRoll { get; set; } = 1;
        public int SleepClarityLossRoll { get; set; } = 1;
    }

    public sealed class TravelPhaseResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int EffortCost { get; set; }
        public int RemainingEffort { get; set; }
        public bool TriggerRandomMythOmen { get; set; }
        public bool TriggerNearestMythOmen { get; set; }
        public bool TriggerLandmarkEncounter { get; set; }
        public BlindTravelOutcome BlindOutcome { get; set; }
        public bool DireWeather { get; set; }
        public bool LoomingThreat { get; set; }
        public int SteedVigorLoss { get; set; }
        public int NightSpiritLoss { get; set; }
        public int WinterVigorLoss { get; set; }
        public int SleepClarityLoss { get; set; }
        public string HoldingMood { get; set; }
        public List<string> LogEntries { get; } = new List<string>();
    }

    public static class PhaseTravelRules
    {
        public static TravelPhaseResult Resolve(TravelPhaseContext context)
        {
            var result = new TravelPhaseResult();
            if (context == null)
            {
                result.ErrorMessage = "Travel context is required.";
                return result;
            }

            if (context.BarrierBlocksRoute)
            {
                result.ErrorMessage = "A barrier blocks the chosen route.";
                result.LogEntries.Add("Travel through a barrier fails and the phase is wasted.");
                return result;
            }

            var allowedDistance = context.Method switch
            {
                TravelMethod.Trek => 1,
                TravelMethod.Gallop => 2,
                TravelMethod.Cruise => 3,
                _ => 1
            };

            if (context.PathLength <= 0 || context.PathLength > allowedDistance)
            {
                result.ErrorMessage = $"The chosen route exceeds the limit for {context.Method}.";
                return result;
            }

            if (context.AvailableEffort < 1)
            {
                result.ErrorMessage = "Party does not have enough effort remaining to travel.";
                return result;
            }

            if (context.Method == TravelMethod.Gallop && (!context.HasSteed || context.SteedExhausted))
            {
                result.ErrorMessage = "Gallop requires a non-exhausted steed.";
                return result;
            }

            if (context.Method == TravelMethod.Cruise && !context.ByBoat && !context.OnProperRoad)
            {
                result.ErrorMessage = "Cruise requires a boat or proper road.";
                return result;
            }

            result.Success = true;
            result.EffortCost = 1;
            result.RemainingEffort = context.AvailableEffort - 1;
            result.LogEntries.Add($"{context.Method} consumes one phase.");

            if (context.Method == TravelMethod.Gallop)
            {
                result.SteedVigorLoss = Math.Max(1, context.GallopLossRoll);
                result.LogEntries.Add($"Gallop exhausts the steed for {result.SteedVigorLoss} VIG.");
            }

            if (context.TravelingBlind)
            {
                result.BlindOutcome = context.BlindRoll switch
                {
                    1 => BlindTravelOutcome.CircleBack,
                    2 => BlindTravelOutcome.DriftLeft,
                    3 => BlindTravelOutcome.DriftRight,
                    _ => BlindTravelOutcome.AsPlanned
                };
                result.LogEntries.Add($"Blind travel outcome: {result.BlindOutcome}.");
            }

            if (context.DireWeatherRegion)
            {
                if (context.WeatherRoll == 1 || (context.WeatherRoll <= 3 && context.ConsecutiveLoomingThreat))
                {
                    result.DireWeather = true;
                    result.LogEntries.Add("Dire weather prevents leaving the current hex.");
                    result.Success = false;
                    result.ErrorMessage = "Dire weather prevents travel.";
                    return result;
                }

                result.LoomingThreat = context.WeatherRoll is 2 or 3;
            }

            if (context.EndsInMythHex)
            {
                result.TriggerNearestMythOmen = true;
                result.LogEntries.Add("Ending in a myth hex reveals the next omen.");
            }
            else if (!context.SleptIndoors || !context.CampingOutdoors)
            {
                switch (context.WildernessRoll)
                {
                    case 1:
                        result.TriggerRandomMythOmen = true;
                        break;
                    case 2:
                    case 3:
                        result.TriggerNearestMythOmen = true;
                        break;
                    case 4:
                    case 5:
                    case 6:
                        result.TriggerLandmarkEncounter = context.EndsAtLandmark;
                        break;
                }
            }

            if (context.IsNight)
            {
                result.NightSpiritLoss = Math.Max(1, context.NightSpiritLossRoll);
                result.LogEntries.Add($"Night travel costs {result.NightSpiritLoss} SPI.");
            }

            if (context.IsWinter && (context.CampingOutdoors || context.IsNight))
            {
                result.WinterVigorLoss = Math.Max(1, context.WinterVigorLossRoll);
                result.LogEntries.Add($"Winter exposure costs {result.WinterVigorLoss} VIG.");
            }

            if (!context.SleptIndoors && context.CampingOutdoors)
            {
                result.SleepClarityLoss = Math.Max(1, context.SleepClarityLossRoll);
                result.LogEntries.Add($"Improper sleep costs {result.SleepClarityLoss} CLA.");
            }

            if (context.EndsAtHolding)
            {
                result.HoldingMood = context.MoodRoll switch
                {
                    1 => "Occupied by a looming or recent woe.",
                    2 => "There is a sense of things in decline.",
                    3 => "There is a sense of things in decline.",
                    _ => "A fine mood and all seems well enough."
                };
                result.LogEntries.Add($"Holding mood: {result.HoldingMood}");
            }

            return result;
        }
    }
}
