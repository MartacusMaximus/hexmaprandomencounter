using UnityEngine;
using System.Collections.Generic;

public class RandomEncounterManager : MonoBehaviour
{
    public static RandomEncounterManager Instance;
    void Awake() { Instance = this; }

    [Header("Data")]
    public BiomePrefabSO biomeTable;
    public List<MythSO> mythPool;

    [Header("Settings")]
    public int mythCount = 6;
    [Range(0f,1f)] public float visibleMythChance = 0.2f;

    private List<HexCell> mythHexes = new List<HexCell>();

    public void AssignRandomMyths()
    {
        mythHexes.Clear();

        var allCells = new List<HexCell>(FindObjectsOfType<HexCell>());
        if (allCells.Count == 0) return;
        if (mythPool == null || mythPool.Count == 0) return;

        List<MythSO> mythChoices = new List<MythSO>(mythPool);
        List<MythSO> selectedMyths = new List<MythSO>();

        for (int i = 0; i < mythCount; i++)
        {
            MythSO chosen;

            if (mythChoices.Count > 0)
            {
                int idx = Random.Range(0, mythChoices.Count);
                chosen = mythChoices[idx];
                mythChoices.RemoveAt(idx);
            }
            else
            {
                chosen = mythPool[Random.Range(0, mythPool.Count)];
            }

            selectedMyths.Add(chosen);
        }


        List<HexCell> cellChoices = new List<HexCell>(allCells);

        for (int i = 0; i < mythCount && cellChoices.Count > 0; i++)
        {
            int idx = Random.Range(0, cellChoices.Count);
            var chosenCell = cellChoices[idx];
            cellChoices.RemoveAt(idx);

            var mythSo = selectedMyths[i];

            chosenCell.myth = mythSo;
            chosenCell.mythOmenIndex = 0;
            chosenCell.mythVisible = Random.value < visibleMythChance;

            if (chosenCell.mythVisible && mythSo.visibleMarkerPrefab != null)
            {
                chosenCell.mythMarkerInstance =
                    Instantiate(mythSo.visibleMarkerPrefab, chosenCell.transform);
                chosenCell.mythMarkerInstance.transform.localPosition = Vector3.zero;
            }

            mythHexes.Add(chosenCell);
        }
    }

    int CubeDistance(HexCell a, HexCell b)
    {
        int ax = a.q, az = a.r, ay = -ax - az;
        int bx = b.q, bz = b.r, by = -bx - bz;
        return (Mathf.Abs(ax - bx) + Mathf.Abs(ay - by) + Mathf.Abs(az - bz)) / 2;
    }

    HexCell GetNearestMyth(HexCell from)
    {
        HexCell best = null;
        int bestd = int.MaxValue;

        foreach (var h in mythHexes)
        {
            if (h == null || h.myth == null) continue;
            int d = CubeDistance(from, h);
            if (d < bestd) { bestd = d; best = h; }
        }
        return best;
    }

    HexCell GetRandomMyth()
    {
        if (mythHexes.Count == 0) return null;
        return mythHexes[Random.Range(0, mythHexes.Count)];
    }

    public void OnEnterHex(HexCell cell)
    {
        int roll = Random.Range(1, 7);

        switch (roll)
        {
            case 1:
                TriggerMythOmen(GetRandomMyth());
                break;

            case 2:
                TriggerMythOmen(GetNearestMyth(cell));
                break;

            case 3:
            case 4:
                TriggerBiomeEncounter(cell);
                break;

            case 5:
            case 6:
                if (cell.landmarkPrefab != null)
                {
                    Debug.Log($"Landmark encounter at ({cell.q},{cell.r})");
                }
                else Debug.Log("No landmark here.");
                break;
        }
    }

    void TriggerMythOmen(HexCell mythCell)
    {
        if (mythCell == null || mythCell.myth == null)
        {
            Debug.Log("No myth found.");
            return;
        }

        string omen = mythCell.TriggerNextOmen();

        if (omen != null)
            Debug.Log($"Myth '{mythCell.myth.mythName}' omen: {omen}");
        else
            Debug.Log($"Myth '{mythCell.myth.mythName}' has no remaining omens.");
    }


    void TriggerBiomeEncounter(HexCell cell)
    {
        var entry = biomeTable.GetEntry(cell.biome);
        if (entry == null)
        {
            Debug.Log($"No biome entry for {cell.biome}");
            return;
        }

        string loc = "";
        string evt = "";

        if (entry.locationTexts != null && entry.locationTexts.Count > 0)
            loc = entry.locationTexts[Random.Range(0, entry.locationTexts.Count)];

        if (entry.eventTexts != null && entry.eventTexts.Count > 0)
            evt = entry.eventTexts[Random.Range(0, entry.eventTexts.Count)];

        Debug.Log(
            $"Biome Encounter ({cell.biome}) @ {cell.q},{cell.r}: \n" +
            $"Location: {loc}\nEvent: {evt}"
        );
    }
}
