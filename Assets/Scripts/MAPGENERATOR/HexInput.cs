using UnityEngine;
using UnityEngine.InputSystem;

public class HexInput : MonoBehaviour
{
    private HexCell hoveredCell;
    private PartyCursorMovement hoveredCursor;

    void Update()
    {
        HandleHover();
        HandleClick();
    }

    void HandleHover()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            hoveredCursor = hit.collider.GetComponentInParent<PartyCursorMovement>();
            UpdateHoveredCell(hit.collider.GetComponentInParent<HexCell>());
            return;
        }

        hoveredCursor = null;
        UpdateHoveredCell(null);
    }

    void HandleClick()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame || PartyCursorController.Instance == null)
        {
            return;
        }

        if (hoveredCursor != null)
        {
            PartyCursorController.Instance.TryClickCursor();
            return;
        }

        if (hoveredCell != null)
        {
            PartyCursorController.Instance.TryClickCell(hoveredCell);
        }
    }

    private void UpdateHoveredCell(HexCell nextCell)
    {
        var controller = PartyCursorController.Instance;
        if (nextCell == hoveredCell)
        {
            return;
        }

        if (hoveredCell != null && (controller == null || !controller.ShouldKeepHighlighted(hoveredCell)))
        {
            hoveredCell.SetHighlight(false);
        }

        hoveredCell = nextCell;

        if (hoveredCell != null)
        {
            hoveredCell.SetHighlight(true);
        }
    }
}
