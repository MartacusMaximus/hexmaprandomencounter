using UnityEngine;
using System.Collections.Generic;


public enum BiomeCategory
{
    Forest,
    Plains,
    Water,
    Mountain,
    City,
    Badlands
}


public enum BiomeType
{
    ConiferousForest,
    DeciduousForest,
    Swampland,
    PoisonousGrounds,

    Savanna,
    Chaparral,
    Desert,

    FreshWater,
    SaltWater,
    BrackishWater,
    Ice,

    HighMountain,
    LowMountain,
    Tundra,

    City,
    TarredGround,
    DeadWood
}

[CreateAssetMenu(fileName = "BiomeData", menuName = "ScriptableObjects/BiomeData")]
public class BiomeData : ScriptableObject
{
    [System.Serializable]
    public class BiomeCategoryList
    {
    public BiomeCategory category;
    public List<BiomeType> biomes;
    }

    [System.Serializable]
    public class HexRow
    {
        public BiomeType[] biomes;
    }

    public HexRow[] biomeMap;
}
