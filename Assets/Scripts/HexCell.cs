using UnityEngine;

public class HexCell : MonoBehaviour
{
    public int q;
    public int r;
    public BiomeType biome;

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
    public void SetHighlight(bool on)
    {
        if (highlightRenderer != null)
            highlightRenderer.enabled = on;
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
