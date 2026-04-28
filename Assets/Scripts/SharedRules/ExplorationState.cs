using System.Collections.Generic;

namespace KnightsAndGM.Shared
{
    public sealed class ExplorationState
    {
        private readonly HashSet<HexCoordinate> exploredCoordinates = new HashSet<HexCoordinate>();

        public IEnumerable<HexCoordinate> ExploredCoordinates => exploredCoordinates;

        public bool IsExplored(HexCoordinate coordinate)
        {
            return exploredCoordinates.Contains(coordinate);
        }

        public bool MarkExplored(HexCoordinate coordinate)
        {
            return exploredCoordinates.Add(coordinate);
        }
    }
}
