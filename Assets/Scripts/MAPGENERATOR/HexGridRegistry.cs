using System.Collections.Generic;
using KnightsAndGM.Shared;
using UnityEngine;

public class HexGridRegistry : MonoBehaviour
{
    public static HexGridRegistry Instance { get; private set; }

    private readonly Dictionary<HexCoordinate, HexCell> cellsByCoordinate = new Dictionary<HexCoordinate, HexCell>();
    private readonly ExplorationState explorationState = new ExplorationState();

    public IEnumerable<HexCell> Cells => cellsByCoordinate.Values;
    public ExplorationState Exploration => explorationState;

    private void Awake()
    {
        Instance = this;
    }

    public void Register(HexCell cell)
    {
        cellsByCoordinate[cell.Coordinate] = cell;
    }

    public bool TryGetCell(HexCoordinate coordinate, out HexCell cell)
    {
        return cellsByCoordinate.TryGetValue(coordinate, out cell);
    }

    public List<HexCell> GetNeighbors(HexCell cell)
    {
        var neighbors = new List<HexCell>();
        foreach (var neighborCoordinate in cell.Coordinate.GetNeighbors())
        {
            if (cellsByCoordinate.TryGetValue(neighborCoordinate, out var neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    public void Clear()
    {
        cellsByCoordinate.Clear();
    }

    public bool MarkExplored(HexCell cell)
    {
        var isNewExploration = explorationState.MarkExplored(cell.Coordinate);
        cell.SetExplored(true);
        return isNewExploration;
    }
}
