using System.Collections.Generic;
using KnightsAndGM.Shared;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{
    public float hexSize = 1f;
    public BiomeData biomeData;
    public BiomePrefabSO prefabTable;

    private GameObject root;
    private HexGridRegistry registry;

    void Awake()
    {
        registry = GetComponent<HexGridRegistry>();
        if (registry == null)
        {
            registry = gameObject.AddComponent<HexGridRegistry>();
        }
    }

    void Start()
    {
        Generate();
    }

    void Generate()
    {
        if (biomeData == null || prefabTable == null)
        {
            Debug.LogError("HexMapGenerator requires biome data and a prefab table.");
            return;
        }

        if (root != null)
        {
            Destroy(root);
        }

        root = new GameObject("HexMap");
        registry.Clear();

        var rows = biomeData.biomeMap.Length;
        var maxWidth = 0;
        var generatedCells = new List<HexCell>();

        foreach (var row in biomeData.biomeMap)
        {
            if (row.biomes.Length > maxWidth)
            {
                maxWidth = row.biomes.Length;
            }
        }

        for (var rowIndex = 0; rowIndex < rows; rowIndex++)
        {
            var row = biomeData.biomeMap[rowIndex];
            var rowWidth = row.biomes.Length;
            var startColumn = Mathf.FloorToInt((maxWidth - rowWidth) * 0.5f);

            for (var localColumn = 0; localColumn < rowWidth; localColumn++)
            {
                var offsetCoordinate = new OffsetCoordinate(startColumn + localColumn, rowIndex);
                var worldPosition = UnityHexLayout.OddRowOffsetToWorld(offsetCoordinate, hexSize);

                var cellObject = new GameObject($"{localColumn},{rowIndex}");
                cellObject.transform.SetParent(root.transform);
                cellObject.transform.position = worldPosition;

                var cell = cellObject.AddComponent<HexCell>();
                cell.ConfigureCoordinates(localColumn, rowIndex, offsetCoordinate.Column);
                cell.biome = row.biomes[localColumn];
                registry.Register(cell);
                generatedCells.Add(cell);

                var prefab = prefabTable.GetPrefab(row.biomes[localColumn]);
                if (prefab == null)
                {
                    continue;
                }

                var tileObject = Instantiate(prefab, cell.transform);
                tileObject.transform.localPosition = Vector3.zero;

                var highlight = tileObject.transform.Find("Highlight");
                if (highlight != null)
                {
                    cell.highlightRenderer = highlight.GetComponent<Renderer>();
                }
            }
        }

        FindFirstObjectByType<HexPopulator>()?.PopulateLandmarks();
        RandomEncounterManager.Instance?.AssignRandomMyths();

        var center = FindStartHex(generatedCells);
        if (center == null)
        {
            return;
        }

        registry.MarkExplored(center);

        if (PartyCursorController.Instance != null && PartyCursorController.Instance.cursor != null)
        {
            PartyCursorController.Instance.cursor.SetStartCell(center);
            PartyCursorController.Instance.RefreshHighlights();
        }

        CameraController.Instance?.CenterOn(center.transform.position);
    }

    private HexCell FindStartHex(List<HexCell> cells)
    {
        foreach (var cell in cells)
        {
            if (cell.biome == BiomeType.City)
            {
                return cell;
            }
        }

        if (cells.Count == 0)
        {
            return null;
        }

        var worldCenter = Vector3.zero;
        foreach (var cell in cells)
        {
            worldCenter += cell.transform.position;
        }

        worldCenter /= cells.Count;

        HexCell closest = null;
        var closestDistance = float.MaxValue;
        foreach (var cell in cells)
        {
            var distance = Vector3.Distance(cell.transform.position, worldCenter);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = cell;
            }
        }

        return closest;
    }
}
