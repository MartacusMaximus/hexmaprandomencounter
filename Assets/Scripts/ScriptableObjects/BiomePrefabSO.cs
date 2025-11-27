using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BiomePrefabSO", menuName = "ScriptableObjects/Biome Prefab SO")]
public class BiomePrefabSO : ScriptableObject
{
    [System.Serializable]
    public class BiomePrefabEntry
    {
        public BiomeType biome;
        public GameObject prefab;
    }

    public List<BiomePrefabEntry> entries = new List<BiomePrefabEntry>();

    private Dictionary<BiomeType, GameObject> lookup;

    public GameObject GetPrefab(BiomeType biome)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<BiomeType, GameObject>();
            foreach (var e in entries)
                lookup[e.biome] = e.prefab;
        }

        return lookup.ContainsKey(biome) ? lookup[biome] : null;
    }
}
