using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{
    public float hexSize = 1f;
    public BiomeData biomeData;

    public BiomePrefabSO prefabTable;
    


    void Start()
    {
        Generate();
    }

    void Generate()
    {
        GameObject root = new GameObject("HexMap");

        int rows = biomeData.biomeMap.Length;

        // find widest row for centering
        int maxWidth = 0;
        foreach (var row in biomeData.biomeMap)
            if (row.biomes.Length > maxWidth)
                maxWidth = row.biomes.Length;

        for (int r = 0; r < rows; r++)
        {
            var row = biomeData.biomeMap[r];
            int width = row.biomes.Length;

            // horizontal center offset
            float offset = (maxWidth - width) * (Mathf.Sqrt(3f) * hexSize * 0.5f);

            for (int q = 0; q < width; q++)
            {
                Vector3 pos = HexToWorld(q, r, offset);

                GameObject cell = new GameObject($"{q},{r}");
                cell.transform.SetParent(root.transform);
                cell.transform.position = pos;

                HexCell cellComponent = cell.AddComponent<HexCell>();
                cellComponent.q = q;
                cellComponent.r = r;
                cellComponent.biome = row.biomes[q];


                GameObject prefab = GetPrefab(row.biomes[q]);
                if (prefab != null)
                {
                    var tileObj = Instantiate(prefab, cell.transform);
                    tileObj.transform.localPosition = Vector3.zero;

                    var highlight = tileObj.transform.Find("Highlight");
                    if (highlight != null)
                    {
                    cellComponent.highlightRenderer = highlight.GetComponent<Renderer>();
                    }
                }

            }
            FindObjectOfType<HexPopulator>()?.PopulateLandmarks();
            RandomEncounterManager.Instance?.AssignRandomMyths();
        }

    HexCell center = FindCenterHex();
    PartyCursorController.Instance.cursor.SetStartCell(center);
    CameraController.Instance.CenterOn(center.transform.position);
    }
    GameObject GetPrefab(BiomeType biome)
    {
        return prefabTable.GetPrefab(biome);
    }




    HexCell FindCenterHex()
    {
        int midR = biomeData.biomeMap.Length / 2;
        int midQ = biomeData.biomeMap[midR].biomes.Length / 2;

        string name = $"{midQ},{midR}";
        return GameObject.Find(name).GetComponent<HexCell>();
    }




    Vector3 HexToWorld(int q, int r, float offset)
    {
        float x = (Mathf.Sqrt(3f) * hexSize) * q + offset;
        float z = 1.5f * hexSize * r;
        return new Vector3(x, 0f, z);
    }
}
