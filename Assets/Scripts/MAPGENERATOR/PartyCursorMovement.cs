using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KnightsAndGM.Shared;
using UnityEngine;

public class PartyCursorMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public int totalEffort = 6;
    [SerializeField] private int remainingEffort = 6;
    public TravelMethod SelectedTravelMethod = TravelMethod.Trek;
    public bool TravelAtNight;
    public bool CampOutdoors = true;
    public bool SleptIndoors;
    public bool IsWinter;
    public bool TravelingBlind;
    public bool HasSteed = true;
    public bool SteedExhausted;

    private readonly TravelRuleSet travelRules = new TravelRuleSet(1);
    private HexCell currentCell;
    private bool isMoveSelectionActive;
    private bool isMoving;

    public HexCell CurrentCell => currentCell;
    public bool IsMoveSelectionActive => isMoveSelectionActive;
    public bool IsMoving => isMoving;
    public int RemainingEffort => remainingEffort;

    public void SetStartCell(HexCell cell)
    {
        if (cell == null)
        {
            return;
        }

        currentCell = cell;
        transform.position = cell.transform.position;
        remainingEffort = Mathf.Clamp(remainingEffort, 0, totalEffort);
    }

    public void ToggleMoveSelection()
    {
        if (isMoving || currentCell == null)
        {
            return;
        }

        if (remainingEffort < travelRules.BaseEffortCost)
        {
            Debug.Log("PartyCursorMovement: no effort remaining.");
            isMoveSelectionActive = false;
            return;
        }

        isMoveSelectionActive = !isMoveSelectionActive;
        Debug.Log(isMoveSelectionActive
            ? $"PartyCursorMovement: move selection armed. Remaining effort {remainingEffort}."
            : "PartyCursorMovement: move selection cancelled.");
    }

    public List<HexCell> GetReachableCells()
    {
        if (currentCell == null || HexGridRegistry.Instance == null || remainingEffort < travelRules.BaseEffortCost)
        {
            return new List<HexCell>();
        }

        var maxDistance = SelectedTravelMethod switch
        {
            TravelMethod.Trek => 1,
            TravelMethod.Gallop => 2,
            TravelMethod.Cruise => 3,
            _ => 1
        };

        return HexGridRegistry.Instance.Cells
            .Where(cell => cell != null && cell != currentCell && cell.Coordinate.DistanceTo(currentCell.Coordinate) <= maxDistance)
            .ToList();
    }

    public bool TryMoveTo(HexCell target)
    {
        if (target == null || currentCell == null || isMoving || !isMoveSelectionActive)
        {
            return false;
        }

        var path = HexGridRegistry.Instance != null
            ? HexGridRegistry.Instance.FindPath(currentCell, target, SelectedTravelMethod == TravelMethod.Cruise ? 3 : SelectedTravelMethod == TravelMethod.Gallop ? 2 : 1)
            : new List<HexCell>();
        if (path.Count == 0)
        {
            Debug.LogWarning("PartyCursorMovement: no valid path found.");
            return false;
        }

        var blocked = PathBlocked(path);
        var context = new TravelPhaseContext
        {
            From = currentCell.Coordinate,
            To = target.Coordinate,
            AvailableEffort = remainingEffort,
            PathLength = path.Count - 1,
            Method = SelectedTravelMethod,
            HasSteed = HasSteed,
            SteedExhausted = SteedExhausted,
            OnProperRoad = path.All(cell => cell.hasProperRoad),
            ByBoat = path.All(cell => cell.hasBoatRoute),
            TravelingBlind = TravelingBlind,
            IsNight = TravelAtNight,
            CampingOutdoors = CampOutdoors,
            SleptIndoors = SleptIndoors || target.hasIndoorShelter,
            IsWinter = IsWinter,
            DireWeatherRegion = target.direWeatherRegion,
            EndsInMythHex = target.HasMyth(),
            EndsAtLandmark = target.landmarkPrefab != null,
            EndsAtHolding = target.isHolding,
            BarrierBlocksRoute = blocked,
            GallopLossRoll = Random.Range(1, 7),
            WildernessRoll = Random.Range(1, 7),
            BlindRoll = Random.Range(1, 7),
            WeatherRoll = Random.Range(1, 7),
            MoodRoll = Random.Range(1, 7),
            NightSpiritLossRoll = Random.Range(1, 7),
            WinterVigorLossRoll = Random.Range(1, 7),
            SleepClarityLossRoll = Random.Range(1, 7)
        };

        var phase = PhaseTravelRules.Resolve(context);
        TravelHud.Instance?.Append(phase);
        if (!phase.Success && !string.IsNullOrWhiteSpace(phase.ErrorMessage))
        {
            Debug.LogWarning($"PartyCursorMovement: {phase.ErrorMessage}");
            return false;
        }

        remainingEffort = phase.RemainingEffort;
        if (SelectedTravelMethod == TravelMethod.Gallop)
        {
            SteedExhausted = true;
        }

        isMoveSelectionActive = false;
        StopAllCoroutines();
        StartCoroutine(MoveTo(target, travelRules.ResolveMove(currentCell.Coordinate, target.Coordinate, remainingEffort + 1)));
        return true;
    }

    private IEnumerator MoveTo(HexCell target, TravelResolution resolution)
    {
        isMoving = true;

        var start = transform.position;
        var end = target.transform.position;
        var t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        currentCell = target;
        HexGridRegistry.Instance?.MarkExplored(currentCell);
        Debug.Log(
            $"Party moved from {resolution.From} to {resolution.To}. " +
            $"Cost {resolution.EffortCost} effort. Remaining {resolution.RemainingEffort}.");

        CameraController.Instance?.CenterOn(transform.position);
        RandomEncounterManager.Instance?.OnEnterHex(currentCell);

        isMoving = false;
        PartyCursorController.Instance?.RefreshHighlights();
    }

    public void ResetEffort()
    {
        remainingEffort = totalEffort;
        SteedExhausted = false;
        Debug.Log($"PartyCursorMovement: effort reset to {remainingEffort}.");
    }

    public void SetTravelMethod(TravelMethod method)
    {
        SelectedTravelMethod = method;
        PartyCursorController.Instance?.RefreshHighlights();
    }

    private static bool PathBlocked(IReadOnlyList<HexCell> path)
    {
        for (var index = 0; index < path.Count - 1; index++)
        {
            var current = path[index];
            var next = path[index + 1];
            for (var direction = 0; direction < 6; direction++)
            {
                if (current.Coordinate.Neighbor(direction) == next.Coordinate && current.BlocksDirection(direction))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
