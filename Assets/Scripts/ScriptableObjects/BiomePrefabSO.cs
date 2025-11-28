using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BiomePrefabTable", menuName = "ScriptableObjects/BiomePrefabTable")]
public class BiomePrefabSO : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public BiomeType biome;
        public GameObject visualPrefab;                 // the tile/prefab spawned on the hex
        [TextArea] public List<string> locationTexts; // 10 flavor texts
        [TextArea] public List<string> eventTexts;    // 10 flavor texts
    }

    public List<Entry> entries = new List<Entry>();

    Dictionary<BiomeType, Entry> lookup;

    void EnsureLookup()
    {
        if (lookup != null) return;
        lookup = new Dictionary<BiomeType, Entry>();
        foreach (var e in entries) lookup[e.biome] = e;
    }

    public Entry GetEntry(BiomeType b)
    {
        EnsureLookup();
        return lookup.ContainsKey(b) ? lookup[b] : null;
    }

    public GameObject GetPrefab(BiomeType b)
    {
        var e = GetEntry(b);
        return e != null ? e.visualPrefab : null;
    }
}
