using System.Collections.Generic;
using System.Linq;
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

    public List<HexCell> FindPath(HexCell start, HexCell target, int maxDistance)
    {
        if (start == null || target == null)
        {
            return new List<HexCell>();
        }

        if (start == target)
        {
            return new List<HexCell> { start };
        }

        var frontier = new Queue<HexCell>();
        var cameFrom = new Dictionary<HexCell, HexCell>();
        frontier.Enqueue(start);
        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current.Coordinate.DistanceTo(start.Coordinate) >= maxDistance)
            {
                continue;
            }

            foreach (var neighbor in GetNeighbors(current))
            {
                if (cameFrom.ContainsKey(neighbor))
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                if (neighbor == target)
                {
                    return ReconstructPath(cameFrom, target);
                }

                frontier.Enqueue(neighbor);
            }
        }

        return new List<HexCell>();
    }

    private static List<HexCell> ReconstructPath(IDictionary<HexCell, HexCell> cameFrom, HexCell target)
    {
        var path = new List<HexCell>();
        var current = target;
        while (current != null)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }
}
