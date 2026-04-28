using System;
using System.Collections.Generic;

namespace KnightsAndGM.Shared
{
    public sealed class HexTileOverride
    {
        public HexCoordinate Coordinate { get; set; }
        public TerrainType? Terrain { get; set; }
        public string PublicLandmarkName { get; set; }
        public string SecretLandmarkName { get; set; }
        public string SecretDetails { get; set; }
        public bool? IsExploredPublic { get; set; }
    }

    public sealed class DeterministicMapGenerator
    {
        public List<HexTileModel> Generate(Guid campaignId, string seed, int radius, IEnumerable<HexTileOverride> overrides = null)
        {
            var tiles = new List<HexTileModel>();
            var overridesByCoordinate = new Dictionary<HexCoordinate, HexTileOverride>();

            if (overrides != null)
            {
                foreach (var entry in overrides)
                {
                    overridesByCoordinate[entry.Coordinate] = entry;
                }
            }

            for (var q = -radius; q <= radius; q++)
            {
                var rMin = Math.Max(-radius, -q - radius);
                var rMax = Math.Min(radius, -q + radius);

                for (var r = rMin; r <= rMax; r++)
                {
                    var coordinate = new HexCoordinate(q, r);
                    overridesByCoordinate.TryGetValue(coordinate, out var overrideEntry);

                    var terrain = coordinate == new HexCoordinate(0, 0)
                        ? TerrainType.City
                        : overrideEntry?.Terrain ?? DetermineTerrain(seed, coordinate);

                    tiles.Add(new HexTileModel
                    {
                        Id = CreateStableGuid(campaignId, coordinate.ToString()),
                        Coordinate = coordinate,
                        Terrain = terrain,
                        IsExploredPublic = overrideEntry?.IsExploredPublic ?? coordinate == new HexCoordinate(0, 0),
                        PublicLandmarkName = overrideEntry?.PublicLandmarkName,
                        SecretLandmarkName = overrideEntry?.SecretLandmarkName,
                        SecretDetails = overrideEntry?.SecretDetails
                    });
                }
            }

            return tiles;
        }

        private static TerrainType DetermineTerrain(string seed, HexCoordinate coordinate)
        {
            var value = Math.Abs(StableHash(seed, coordinate)) % 100;

            if (value < 20) return TerrainType.Plains;
            if (value < 35) return TerrainType.Forest;
            if (value < 45) return TerrainType.Mountain;
            if (value < 55) return TerrainType.Water;
            if (value < 65) return TerrainType.Swamp;
            if (value < 75) return TerrainType.Desert;
            if (value < 82) return TerrainType.Tundra;
            if (value < 89) return TerrainType.Badlands;
            if (value < 95) return TerrainType.Deadwood;
            return TerrainType.Tar;
        }

        private static int StableHash(string seed, HexCoordinate coordinate)
        {
            unchecked
            {
                var hash = 23;
                var normalizedSeed = seed ?? string.Empty;
                for (var i = 0; i < normalizedSeed.Length; i++)
                {
                    hash = (hash * 31) + normalizedSeed[i];
                }

                hash = (hash * 31) + coordinate.GetHashCode();
                return hash;
            }
        }

        private static Guid CreateStableGuid(Guid campaignId, string key)
        {
            unchecked
            {
                var hash = 19;
                var input = campaignId.ToString("N") + key;
                for (var index = 0; index < input.Length; index++)
                {
                    hash = (hash * 31) + input[index];
                }

                var bytes = new byte[16];
                var random = new Random(hash);
                random.NextBytes(bytes);
                return new Guid(bytes);
            }
        }
    }
}
