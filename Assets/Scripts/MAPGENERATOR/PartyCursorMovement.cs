using System.Collections;
using System.Collections.Generic;
using KnightsAndGM.Shared;
using UnityEngine;

public class PartyCursorMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public int totalEffort = 6;
    [SerializeField] private int remainingEffort = 6;

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

        return HexGridRegistry.Instance.GetNeighbors(currentCell);
    }

    public bool TryMoveTo(HexCell target)
    {
        if (target == null || currentCell == null || isMoving || !isMoveSelectionActive)
        {
            return false;
        }

        var resolution = travelRules.ResolveMove(currentCell.Coordinate, target.Coordinate, remainingEffort);
        if (!resolution.Success)
        {
            Debug.LogWarning($"PartyCursorMovement: {resolution.ErrorMessage}");
            return false;
        }

        remainingEffort = resolution.RemainingEffort;
        isMoveSelectionActive = false;
        StopAllCoroutines();
        StartCoroutine(MoveTo(target, resolution));
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
        Debug.Log($"PartyCursorMovement: effort reset to {remainingEffort}.");
    }
}
