using System.Collections.Generic;
using UnityEngine;

public class PartyCursorController : MonoBehaviour
{
    public static PartyCursorController Instance;

    public PartyCursorMovement cursor;

    private readonly HashSet<HexCell> highlightedCells = new HashSet<HexCell>();

    void Awake()
    {
        Instance = this;
    }

    public void TryClickCursor()
    {
        if (cursor == null)
        {
            return;
        }

        cursor.ToggleMoveSelection();
        RefreshHighlights();
    }

    public void TryClickCell(HexCell cell)
    {
        if (cursor == null)
        {
            return;
        }

        if (!cursor.IsMoveSelectionActive)
        {
            if (cursor.CurrentCell == cell)
            {
                TryClickCursor();
            }

            return;
        }

        var moved = cursor.TryMoveTo(cell);
        RefreshHighlights();

        if (moved)
        {
            CameraController.Instance?.CenterOn(cursor.transform.position);
        }
    }

    public bool ShouldKeepHighlighted(HexCell cell)
    {
        return highlightedCells.Contains(cell);
    }

    public void RefreshHighlights()
    {
        foreach (var highlightedCell in highlightedCells)
        {
            if (highlightedCell != null)
            {
                highlightedCell.SetHighlight(false);
            }
        }

        highlightedCells.Clear();

        if (cursor == null || !cursor.IsMoveSelectionActive)
        {
            return;
        }

        foreach (var cell in cursor.GetReachableCells())
        {
            cell.SetHighlight(true);
            highlightedCells.Add(cell);
        }
    }
}
