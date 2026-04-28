using KnightsAndGM.Shared;

public static class BiomeTypeMapper
{
    public static TerrainType ToTerrainType(BiomeType biomeType)
    {
        switch (biomeType)
        {
            case BiomeType.City:
                return TerrainType.City;
            case BiomeType.ConiferousForest:
            case BiomeType.DeciduousForest:
                return TerrainType.Forest;
            case BiomeType.Swampland:
                return TerrainType.Swamp;
            case BiomeType.FreshWater:
            case BiomeType.SaltWater:
            case BiomeType.BrackishWater:
                return TerrainType.Water;
            case BiomeType.HighMountain:
            case BiomeType.LowMountain:
                return TerrainType.Mountain;
            case BiomeType.Tundra:
                return TerrainType.Tundra;
            case BiomeType.Desert:
                return TerrainType.Desert;
            case BiomeType.PoisonousGrounds:
                return TerrainType.Badlands;
            case BiomeType.TarredGround:
                return TerrainType.Tar;
            case BiomeType.DeadWood:
                return TerrainType.Deadwood;
            case BiomeType.Ice:
                return TerrainType.Ice;
            case BiomeType.Savanna:
            case BiomeType.Chaparral:
                return TerrainType.Plains;
            default:
                return TerrainType.Unknown;
        }
    }
}
