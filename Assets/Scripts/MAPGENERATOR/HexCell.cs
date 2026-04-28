using KnightsAndGM.Shared;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public int q;
    public int r;
    public int offsetColumn;
    public int offsetRow;
    public BiomeType biome;
    public bool isExploredPublic;

    [Header("Landmark")]
    public GameObject landmarkPrefab;   // assigned by HexPopulator
    [HideInInspector] public GameObject landmarkInstance;

    [Header("Myth (optional)")]
    public MythSO myth;                 
    public int mythOmenIndex = 0;       
    public bool mythVisible = false;    
    [HideInInspector] public GameObject mythMarkerInstance;

    [Header("Highlight")]
    public Renderer highlightRenderer;

    public HexCoordinate Coordinate { get; private set; }
    public TerrainType Terrain => BiomeTypeMapper.ToTerrainType(biome);

    public void ConfigureCoordinates(int localColumn, int rowIndex, int globalOffsetColumn)
    {
        q = localColumn;
        r = rowIndex;
        offsetColumn = globalOffsetColumn;
        offsetRow = rowIndex;
        Coordinate = HexCoordinate.FromOddRowOffset(offsetColumn, offsetRow);
    }

    public void SetHighlight(bool on)
    {
        if (highlightRenderer != null)
            highlightRenderer.enabled = on;
    }

    public void SetExplored(bool explored)
    {
        isExploredPublic = explored;
    }

    public bool HasMyth() => myth != null;
    public bool MythExhausted() => myth == null || mythOmenIndex >= myth.omens.Count;

    public string TriggerNextOmen()
    {
        if (myth == null || mythOmenIndex >= myth.omens.Count) return null;
        string omen = myth.omens[mythOmenIndex];
        mythOmenIndex++;
        return omen;
    }
}
